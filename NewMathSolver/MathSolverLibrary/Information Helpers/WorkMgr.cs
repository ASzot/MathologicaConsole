using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal class WorkMgr
    {
        public const string EDM = "`";
        public const string STM = "`";
        private string _useComparison = "=";
        private List<WorkStep> _workSteps = new List<WorkStep>();
        private bool b_allowWork = true;
        private string _workLabel = null;

        public void SetAllowWork(bool value)
        {
            b_allowWork = value;
        }

        public bool GetAllowWork()
        {
            return b_allowWork;
        }

        public void SetWorkLabel(string value)
        {
            _workLabel = value;
        }

        public string GetWorkLabel()
        {
            return _workLabel ?? "";
        }

        public void SetUseComparison(string value)
        {
            _useComparison = value;
        }

        public List<WorkStep> GetWorkSteps()
        {
            return _workSteps;
        }

        public WorkMgr()
        {
        }

        public WorkMgr(List<WorkStep> workSteps)
        {
            _workSteps = workSteps;
        }

        public static string CG_TXT_TG(string inStr)
        {
            return "<span class='changeText'>" + inStr + "</span>";
        }

        public static string ToDisp(ExComp ex)
        {
            if (ex is AlgebraTerm)
                return (ex as AlgebraTerm).FinalToDispStr();
            if (ex is ExNumber)
                return (ex as ExNumber).FinalToDispString();
            return ex.ToAsciiString();
        }

        public static string WorkFromSynthDivTable(ExComp root, IEnumerable<ExComp> poly, IEnumerable<ExComp> muls, IEnumerable<ExComp> results)
        {
            if (ArrayFunc.GetCount(poly) - 1 != ArrayFunc.GetCount(muls) || ArrayFunc.GetCount(poly) - 1 != ArrayFunc.GetCount(results))
                return null;

            string finalHtml = "";
            finalHtml += "<table>";

            finalHtml += "<tr>";
            finalHtml += "<td><span class='changeText'>" + STM + WorkMgr.ToDisp(root) + EDM + "</span></td>";
            foreach (ExComp polyEx in poly)
                finalHtml += "<td>" + STM + WorkMgr.ToDisp(polyEx) + EDM + "</td>";
            finalHtml += "</tr>";

            finalHtml += "<tr>";
            finalHtml += "<td></td>";
            finalHtml += "<td></td>";
            foreach (ExComp resultEx in results)
                finalHtml += "<td>" + STM + WorkMgr.ToDisp(resultEx) + EDM + "</td>";
            finalHtml += "</tr>";

            finalHtml += "<tr>";
            finalHtml += "<td></td>";
            finalHtml += "<td></td>";
            foreach (ExComp mulEx in muls)
                finalHtml += "<td>" + STM + WorkMgr.ToDisp(mulEx) + EDM + "</td>";
            finalHtml += "</tr>";

            finalHtml += "</table>";

            return finalHtml;
        }

        public void FromAlgGpSubtraction(List<AlgebraGroup> groups, ExComp left, ExComp right)
        {
            if (!GetAllowWork())
                return;

            string algGpStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                algGpStr += groups[i].ToTerm().FinalToDispStr();
                if (i != groups.Count - 1)
                    algGpStr += "+";
            }

            if (algGpStr == "0")
                return;

            bool isNeg = false;
            if (groups.Count == 1)
            {
                int negCount = 0;
                foreach (char algGpStrChar in algGpStr)
                {
                    if (algGpStrChar == '-')
                        negCount++;
                }

                isNeg = negCount == 1;
            }
            else
            {
                algGpStr = StringFunc.Ins(algGpStr, 0, "(");
                algGpStr = StringFunc.Ins(algGpStr, algGpStr.Length, ")");
            }

            string workDescStr = "";
            if (isNeg)
                workDescStr = "Add  " + WorkMgr.STM + "-{2}" + WorkMgr.EDM + " to both sides";
            else
                workDescStr = "Subtract " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " from both sides";

            _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "{0}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM +
                "</span>" + WorkMgr.STM + _useComparison + "{1}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM + "</span>", workDescStr + GetWorkLabel(), left, right, algGpStr));
        }

        public void FromArraySides(string desc, params ExComp[] sides)
        {
            if (!GetAllowWork() || sides.Length % 2 != 0)
                return;

            string finalStr = "";
            for (int i = 0; i < sides.Length; i += 2)
            {
                finalStr += STM + "{" + i.ToString() + "}={" + (i + 1).ToString() + "}" + EDM;
                if (i != sides.Length - 2)
                    finalStr += "<br />";
            }

            _workSteps.Add(WorkStep.Formatted(finalStr, desc + GetWorkLabel(), sides));
        }

        public void FromArraySides(params ExComp[] sides)
        {
            if (!GetAllowWork() || sides.Length % 2 != 0)
                return;

            string finalStr = "";
            for (int i = 0; i < sides.Length; i += 2)
            {
                finalStr += STM + "{" + i.ToString() + "}={" + (i + 1).ToString() + "}" + EDM;
                if (i != sides.Length - 2)
                    finalStr += "<br />";
            }

            _workSteps.Add(WorkStep.Formatted(finalStr, null, sides));
        }

        public void FromDivision(AlgebraTerm divBy, ExComp left, ExComp right)
        {
            if (!GetAllowWork())
                return;

            AlgebraTerm[] numDen = divBy.GetNumDenFrac();
            if (numDen != null && numDen[0].IsOne())
            {
                ExComp tmpleft = Equation.Operators.MulOp.StaticWeakCombine(left, numDen[1]);
                ExComp tmpright = Equation.Operators.MulOp.StaticWeakCombine(right, numDen[1]);
                _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "{0} " + _useComparison + " {1}" + WorkMgr.EDM, "Multiply both sides by " + WorkMgr.STM + WorkMgr.ToDisp(numDen[1]) + WorkMgr.EDM + GetWorkLabel(),
                    tmpleft, tmpright));
            }
            else
            {
                //string divStr = CG_TXT_TG("{1}");
                string divStr = "{1}";
                _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "({0})/(" + divStr + ") " + _useComparison + " ({2})/(" + divStr + ")" + WorkMgr.EDM, "Divide both sides by " + WorkMgr.STM +
                    WorkMgr.ToDisp(divBy) + WorkMgr.EDM + GetWorkLabel(), left, divBy, right));
            }
        }

        public void FromFormatted(string work, string workDesc, params object[] args)
        {
            if (!GetAllowWork())
                return;

            _workSteps.Add(WorkStep.Formatted(work, workDesc + GetWorkLabel(), args));
        }

        public WorkStep GetLast()
        {
            return _workSteps[_workSteps.Count - 1];
        }

        public void FromFormatted(string work, params object[] args)
        {
            if (!GetAllowWork())
                return;

            _workSteps.Add(WorkStep.Formatted(work, null, args));
        }

        public void FromSides(ExComp left, ExComp right)
        {
            if (!GetAllowWork())
                return;

            _workSteps.Add(WorkStep.Formatted(STM + "{0} " + ((left == null || right == null) ? "" : _useComparison) + " {1}" + EDM, null, (left == null ? (object)"" : left), (right == null ? (object)"" : right)));
        }

        public void FromSides(ExComp left, ExComp right, string desc)
        {
            if (!GetAllowWork())
                return;

            _workSteps.Add(WorkStep.Formatted(STM + "{0} " + ((left == null || right == null) ? "" : _useComparison) + " {1}" + EDM, desc + GetWorkLabel(),
                (left == null ? (object)"" : left),
                (right == null ? (object)"" : right)));
        }

        public void FromSidesAndComps(IEnumerable<ExComp> enumSides, IEnumerable<LexemeType> enumComparisons, string desc)
        {
            if (!GetAllowWork())
                return;

            ExComp[] sides = ArrayFunc.ToArray(enumSides);
            LexemeType[] comparisons = ArrayFunc.ToArray(enumComparisons);

            if (sides.Length + 1 != comparisons.Length)
                return;

            string finalFormatted = ToDisp(sides[0]);

            for (int i = 0; i < comparisons.Length; ++i)
            {
                finalFormatted += Restriction.ComparisonOpToStr(comparisons[i]);
                finalFormatted += ToDisp(sides[i + 1]);
            }

            _workSteps.Add(new WorkStep(finalFormatted + GetWorkLabel(), desc, false));
        }

        /// <summary>
        /// Doesn't actually do the subtraction just outputs the work for it. Like subtract ___ from ___.
        /// </summary>
        /// <param name="sub"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void FromSubtraction(ExComp sub, ExComp left, ExComp right)
        {
            if (!GetAllowWork())
                return;

            bool isNeg = false;

            string subStr = sub is AlgebraTerm ? (sub as AlgebraTerm).FinalToDispStr() : sub.ToDispString();

            if (sub is ExNumber && ExNumber.OpLT((sub as ExNumber), 0.0))
                isNeg = true;

            if (sub is AlgebraTerm)
            {
                if ((sub as AlgebraTerm).GetGroupCount() == 1)
                {
                    int negCount = 0;
                    foreach (char subStrChar in subStr)
                    {
                        if (subStrChar == '-')
                            negCount++;
                    }

                    isNeg = negCount == 1;
                }
                else
                {
                    subStr = StringFunc.Ins(subStr, 0, "(");
                    subStr = StringFunc.Ins(subStr, subStr.Length, ")");
                }
            }

            string workDescStr;

            if (isNeg)
                workDescStr = "Add " + WorkMgr.STM + "-{2}" + WorkMgr.EDM + " to both sides";
            else
                workDescStr = "Subtract " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " from both sides";

            _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "{0}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM + "</span>" + WorkMgr.STM +
                "={1}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM + "</span>", workDescStr + GetWorkLabel(), left, right, subStr));
        }

        public void PopStep()
        {
            ArrayFunc.RemoveIndex(_workSteps, _workSteps.Count - 1);
        }

        public void PopStepsCount(int count)
        {
            for (int i = 0; i < count; ++i)
                PopStep();
        }

        /// <summary>
        /// Pop the steps from the start index to the current index.
        /// </summary>
        /// <param name="startIndex"></param>
        public void PopSteps(int startIndex)
        {
            PopStepsCount(GetWorkSteps().Count - startIndex);
        }
    }

    internal class WorkStep
    {
        private string _work;
        private string _workDesc;
        private List<WorkStep> _subWorkSteps = null;
        private WorkMgr _origWorkMgr = null;

        public void SetWorkDesc(string value)
        {
            _workDesc = value;
        }

        public string GetWorkDesc()
        {
            return _workDesc;
        }

        public void SetWorkHtml(string value)
        {
            _work = value;
            _work = CorrectString(_work);
        }

        public string GetWorkHtml()
        {
            return _work;
        }

        public void SetSubWorkSteps(List<WorkStep> value)
        {
            _subWorkSteps = value;
        }

        public List<WorkStep> GetSubWorkSteps()
        {
            return _subWorkSteps;
        }

        public WorkStep(string work, string workDesc, bool correctOutput)
        {
            _work = work;
            _workDesc = workDesc;

            if (correctOutput)
            {
                _work = MathSolver.FinalizeOutput(_work);
                _workDesc = MathSolver.FinalizeOutput(_workDesc);
            }
        }

        public void GoDown(ref TermType.EvalData pEvalData)
        {
            _subWorkSteps = new List<WorkStep>();
            _origWorkMgr = pEvalData.GetWorkMgr();
            pEvalData.SetWorkMgr(new WorkMgr(_subWorkSteps));
        }

        public void GoUp(ref TermType.EvalData pEvalData)
        {
            pEvalData.SetWorkMgr(_origWorkMgr);
            _origWorkMgr = null;
        }

        private static string CorrectString(string str)
        {
            str = MathSolver.FinalizeOutput(str);
            str = str.Replace("--", "+");
            str = str.Replace(WorkMgr.STM + "+", WorkMgr.STM);
            str = str.Replace("&", "+-");
            str = str.Replace("-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "-", WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "+");

            return str;
        }

        public static string FormatStr(string str, params object[] args)
        {
            string[] argStrs = new string[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] is ExComp)
                {
                    if (args[i] is AlgebraTerm)
                    {
                        args[i] = (args[i] as AlgebraTerm).Order();
                        argStrs[i] = (args[i] as AlgebraTerm).FinalToDispStr();
                    }
                    else
                        argStrs[i] = (args[i] as ExComp).ToAsciiString();
                }
                else
                    argStrs[i] = args[i].ToString();
            }

            if (str != null)
            {
                str = StringFunc.Format(str, argStrs);

                str = CorrectString(str);
            }

            return str;
        }

        public static WorkStep Formatted(string work, string workDesc, params object[] args)
        {
            string[] argStrs = new string[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] is ExComp)
                {
                    if (args[i] is AlgebraTerm)
                    {
                        args[i] = (args[i] as AlgebraTerm).Order();

                        if (args[i] is Equation.Functions.PowerFunction)
                        {
                            string powStr = (args[i] as Equation.Functions.PowerFunction).GetPower().ToDispString();
                            if (powStr.StartsWith("-1"))
                            {
                                argStrs[i] = "\\frac{1}{" + (args[i] as AlgebraTerm).FinalToDispStr() + "}";
                                continue;
                            }
                        }
                        argStrs[i] = (args[i] as AlgebraTerm).FinalToDispStr();
                    }
                    else
                        argStrs[i] = (args[i] as ExComp).ToAsciiString();
                }
                else
                    argStrs[i] = args[i].ToString();
            }

            if (work != null)
            {
                // The LaTex text command will cause problems in the string formatter as it has the '{' and '}' grouping characters.
                if (argStrs.Length != 0 && !work.Contains("{Undefined}"))
                    work = StringFunc.Format(work, argStrs);

                work = MathSolver.FinalizeOutput(work);
                work = work.Replace("--", "+");
                work = work.Replace(WorkMgr.STM + "+", WorkMgr.STM);
                work = work.Replace("&", "+-");
                work = work.Replace("-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "-", WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "+");
                work = work.Replace(WorkMgr.STM + "1x", WorkMgr.STM + "x");
            }

            if (workDesc != null)
            {
                if (argStrs.Length != 0)
                {
                    try
                    {
                        workDesc = StringFunc.Format(workDesc, argStrs);
                    }
                    catch (Exception)
                    {
                    }
                }

                workDesc = MathSolver.FinalizeOutput(workDesc);
                workDesc = workDesc.Replace("--", "+");
                workDesc = workDesc.Replace(WorkMgr.STM + "+", WorkMgr.STM);
                workDesc = workDesc.Replace("&", "+-");
                workDesc = workDesc.Replace("-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "-", WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "+");
            }

            return new WorkStep(work, workDesc, false);
        }
    }
}