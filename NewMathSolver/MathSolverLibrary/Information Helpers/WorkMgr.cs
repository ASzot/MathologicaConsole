using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public bool AllowWork
        {
            get
            {
                return b_allowWork;
            }
            set
            {
                b_allowWork = value;
            }
        }

        public string WorkLabel
        {
            set { _workLabel = value; }
            get { return _workLabel ?? ""; }
        }

        public string UseComparison
        {
            set { _useComparison = value; }
        }

        public List<WorkStep> WorkSteps
        {
            get { return _workSteps; }
        }

        public WorkMgr()
        {
        }

        public static string CG_TXT_TG(string inStr)
        {
            return "<span class='changeText'>" + inStr + "</span>";
        }

        public static string ExFinalToAsciiStr(ExComp ex)
        {
            if (ex is AlgebraTerm)
                return (ex as AlgebraTerm).FinalToDispStr();
            if (ex is Number)
                return (ex as Number).FinalToDispString();
            return ex.ToAsciiString();
        }

        public static string WorkFromSynthDivTable(ExComp root, IEnumerable<ExComp> poly, IEnumerable<ExComp> muls, IEnumerable<ExComp> results)
        {
            if (poly.Count() - 1 != muls.Count() || poly.Count() - 1 != results.Count())
                return null;

            string finalHtml = "";
            finalHtml += "<table>";

            finalHtml += "<tr>"; 
            finalHtml += "<td><span class='changeText'>" + STM + WorkMgr.ExFinalToAsciiStr(root) + EDM + "</span></td>";
            foreach (ExComp polyEx in poly)
                finalHtml += "<td>" + STM + WorkMgr.ExFinalToAsciiStr(polyEx) + EDM + "</td>";
            finalHtml += "</tr>";

            finalHtml += "<tr>";
            finalHtml += "<td></td>";
            finalHtml += "<td></td>";
            foreach (ExComp resultEx in results)
                finalHtml += "<td>" + STM + WorkMgr.ExFinalToAsciiStr(resultEx) + EDM + "</td>";
            finalHtml += "</tr>";

            finalHtml += "<tr>";
            finalHtml += "<td></td>";
            finalHtml += "<td></td>";
            foreach (ExComp mulEx in muls)
                finalHtml += "<td>" + STM + WorkMgr.ExFinalToAsciiStr(mulEx) + EDM + "</td>";
            finalHtml += "</tr>";

            finalHtml += "</table>";

            return finalHtml;
        }

        public void FromAlgGpSubtraction(List<AlgebraGroup> groups, ExComp left, ExComp right)
        {
            if (!AllowWork)
                return;

            string algGpStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                algGpStr += groups[i].ToTerm().FinalDispKeepFormatting();
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
                algGpStr = algGpStr.Insert(0, "(");
                algGpStr = algGpStr.Insert(algGpStr.Length, ")");
            }

            string workDescStr = "";
            if (isNeg)
                workDescStr = "Add  " + WorkMgr.STM + "-{2}" + WorkMgr.EDM + " to both sides";
            else
                workDescStr = "Subtract " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " from both sides";

            _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "{0}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM +
                "</span>" + WorkMgr.STM + _useComparison + "{1}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM + "</span>", workDescStr + WorkLabel, left, right, algGpStr));
        }

        public void FromArraySides(string desc, params ExComp[] sides)
        {
            if (!AllowWork || sides.Length % 2 != 0)
                return;

            string finalStr = "";
            for (int i = 0; i < sides.Length; i += 2)
            {
                finalStr += STM + "{" + i.ToString() + "}={" + (i + 1).ToString() + "}" + EDM;
                if (i != sides.Length - 2)
                    finalStr += "<br />";
            }

            _workSteps.Add(WorkStep.Formatted(finalStr, desc + WorkLabel, sides));
        }

        public void FromArraySides(params ExComp[] sides)
        {
            if (!AllowWork || sides.Length % 2 != 0)
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
            if (!AllowWork)
                return;

            AlgebraTerm[] numDen = divBy.GetNumDenFrac();
            if (numDen != null && numDen[0].IsOne())
            {
                ExComp tmpleft = Equation.Operators.MulOp.StaticWeakCombine(left, numDen[1]);
                ExComp tmpright = Equation.Operators.MulOp.StaticWeakCombine(right, numDen[1]);
                _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "{0}" + _useComparison + "{1}" + WorkMgr.EDM, "Multiply both sides by " + WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(numDen[1]) + WorkMgr.EDM + WorkLabel,
                    tmpleft, tmpright));
            }
            else
            {
                //string divStr = CG_TXT_TG("{1}");
                string divStr = "{1}";
                _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "({0})/(" + divStr + ")" + _useComparison + "({2})/(" + divStr + ")" + WorkMgr.EDM, "Divide both sides by " + WorkMgr.STM +
                    WorkMgr.ExFinalToAsciiStr(divBy) + WorkMgr.EDM + WorkLabel, left, divBy, right));
            }
        }

        public void FromFormatted(string work, string workDesc, params object[] args)
        {
            if (!AllowWork)
                return;

            _workSteps.Add(WorkStep.Formatted(work, workDesc + WorkLabel, args));
        }

        public void FromFormatted(string work, params object[] args)
        {
            if (!AllowWork)
                return;

            _workSteps.Add(WorkStep.Formatted(work, null, args));
        }

        public void FromSides(ExComp left, ExComp right)
        {
            if (!AllowWork)
                return;

            _workSteps.Add(WorkStep.Formatted(STM + "{0}" + ((left == null || right == null) ? "" : _useComparison) + "{1}" + EDM, null, (left == null ? (object)"" : left), (right == null ? (object)"" : right)));
        }

        public void FromSides(ExComp left, ExComp right, string desc)
        {
            if (!AllowWork)
                return;

            _workSteps.Add(WorkStep.Formatted(STM + "{0}" + ((left == null || right == null) ? "" : _useComparison) + "{1}" + EDM, desc + WorkLabel, 
                (left == null ? (object)"" : left), 
                (right == null ? (object)"" : right)));
        }

        public void FromSidesAndComps(IEnumerable<ExComp> enumSides, IEnumerable<LexemeType> enumComparisons, string desc)
        {
            if (!AllowWork)
                return;

            ExComp[] sides = enumSides.ToArray();
            LexemeType[] comparisons = enumComparisons.ToArray();

            if (sides.Length + 1 != comparisons.Length)
                return;

            string finalFormatted = ExFinalToAsciiStr(sides[0]);

            for (int i = 0; i < comparisons.Length; ++i)
            {
                finalFormatted += Restriction.ComparisonOpToStr(comparisons[i]);
                finalFormatted += ExFinalToAsciiStr(sides[i + 1]);
            }

            _workSteps.Add(new WorkStep(finalFormatted + WorkLabel, desc));
        }

        /// <summary>
        /// Doesn't actually do the subtraction just outputs the work for it. Like subtract ___ from ___.
        /// </summary>
        /// <param name="sub"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void FromSubtraction(ExComp sub, ExComp left, ExComp right)
        {
            if (!AllowWork)
                return;

            bool isNeg = false;

            string subStr = sub is AlgebraTerm ? (sub as AlgebraTerm).FinalDispKeepFormatting() : sub.ToAsciiString();

            if (sub is Number && (sub as Number) < 0.0)
                isNeg = true;

            if (sub is AlgebraTerm)
            {
                if ((sub as AlgebraTerm).GroupCount == 1)
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
                    subStr = subStr.Insert(0, "(");
                    subStr = subStr.Insert(subStr.Length, ")");
                }
            }

            string workDescStr;

            if (isNeg)
                workDescStr = "Add " + WorkMgr.STM + "-{2}" + WorkMgr.EDM + " to both sides";
            else
                workDescStr = "Subtract " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " from both sides";

            _workSteps.Add(WorkStep.Formatted(WorkMgr.STM + "{0}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM + "</span>" + WorkMgr.STM +
                "={1}-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "{2}" + WorkMgr.EDM + "</span>", workDescStr + WorkLabel, left, right, subStr));
        }

        public void PopStep()
        {
            _workSteps.RemoveAt(_workSteps.Count - 1);
        }

        public void PopSteps(int count)
        {
            for (int i = 0; i < count; ++i)
                PopStep();
        }
    }

    internal class WorkStep
    {
        private string _work;
        private string _workDesc;

        public string WorkDesc
        {
            get { return _workDesc; }
            set { _workDesc = value; }
        }

        public string WorkHtml
        {
            get { return _work; }
            set { _work = value; }
        }

        public WorkStep(string work, string workDesc)
        {
            _work = work;
            _workDesc = workDesc;
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
                str = String.Format(str, argStrs);

                str = MathSolver.FinalizeOutput(str);
                str = str.Replace("--", "+");
                str = str.Replace(WorkMgr.STM + "+", WorkMgr.STM);
                str = str.Replace("&", "+-");
                str = str.Replace("-" + WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "-", WorkMgr.EDM + "<span class='changeText'>" + WorkMgr.STM + "+");
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
                            string powStr = (args[i] as Equation.Functions.PowerFunction).Power.ToDispString();
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
                    work = String.Format(work, argStrs);

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
                        workDesc = String.Format(workDesc, argStrs);
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

            return new WorkStep(work, workDesc);
        }
    }
}