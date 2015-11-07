using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class QuadraticGenTermType : GenTermType
    {
        private ExComp _a;
        private ExComp _b;
        private ExComp _c;
        private AlgebraTerm _eq;
        private ExComp _funcIden;
        private AlgebraVar _solveFor;
        private FunctionGenTermType tt_func = null;
        private SolveGenTermType _ttSolveGen = null;
        private string _graphStr = null;

        public QuadraticGenTermType()
            : base()
        {
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            if (command == "To vertex form")
            {
                // To vertex form.
                ExComp vertexForm = ToVertexForm(_a, _b, _c, _solveFor.ToAlgebraComp(), ref pEvalData);
                if (_funcIden != null)
                {
                    SolveResult solvedResult = SolveResult.Solved(_funcIden, vertexForm, ref pEvalData);
                    return solvedResult;
                }
                return SolveResult.Simplified(vertexForm);
            }
            else if (command == "Find the vertex")
            {
                // Find the vertex.
                ExComp vertex = FindVertex(_a, _b, _c);
                return SolveResult.Simplified(vertex);
            }
            else if (command == "Factor")
            {
                AlgebraTerm[] factors = AdvAlgebraTerm.Factorize(_a, _b, _c, _solveFor, ref pEvalData, false);
                if (factors == null)
                {
                    pEvalData.AddFailureMsg("Couldn't factor.");
                    return SolveResult.Failure();
                }
                // Factor.
                AlgebraTerm factored = AlgebraTerm.FromFactors(factors);
                if (_funcIden != null)
                {
                    SolveResult solvedResult = SolveResult.Solved(_funcIden, factored, ref pEvalData);
                    return solvedResult;
                }
                else
                    return SolveResult.Simplified(factored);
            }
            else if (command == "Find the discriminant")
            {
                // Discriminant.
                pEvalData.GetWorkMgr().FromFormatted("`" + _eq.FinalToDispStr() + "`", "In the quadratic above `a=" + _a.ToDispString() + ", b=" + _b.ToDispString() + ",c=" + _c.ToDispString() +
                    "`. The equation for getting the discriminant of a quadratic is `b^2-4ac`.");

                pEvalData.GetWorkMgr().FromFormatted("`({0})^2-4({1})({2})`", "Plug the values into the equation.", _a, _b, _c);

                ExComp subVal = MulOp.StaticCombine(_a, _c);
                subVal = MulOp.StaticCombine(subVal, new ExNumber(4.0));
                ExComp discriminant = SubOp.StaticCombine(PowOp.StaticCombine(_b, new ExNumber(2.0)), subVal);

                pEvalData.GetWorkMgr().FromFormatted("`" + WorkMgr.ToDisp(discriminant) + "`", "Simplify getting the final discriminant.");

                return SolveResult.Simplified(discriminant);
            }
            else if (command == "Find axis of symmetry")
            {
                // Axis of symmetry.
                ExComp aos = FindAOS(_a, _b);
                SolveResult aosSolveResult = SolveResult.Solved(_solveFor, aos, ref pEvalData);
                return aosSolveResult;
            }
            else if (command == "Graph")
            {
                if (pEvalData.AttemptSetGraphData(_graphStr, _solveFor.GetVar()))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            if (tt_func != null && tt_func.IsValidCommand(command))
            {
                SolveResult funcSolveResult = tt_func.ExecuteCommand(command, ref pEvalData);
                return funcSolveResult;
            }
            else if (_ttSolveGen != null && _ttSolveGen.IsValidCommand(command))
            {
                if (command.Contains("completing the square"))
                    pEvalData.SetQuadSolveMethod(Solving.QuadraticSolveMethod.CompleteSquare);
                else if (command.Contains("quadratic equation"))
                    pEvalData.SetQuadSolveMethod(Solving.QuadraticSolveMethod.Formula);
                else
                {
                    // The default is factoring as this will go on to the quadratic formula method if necessary.
                    pEvalData.SetQuadSolveMethod(Solving.QuadraticSolveMethod.Factor);
                }
                return _ttSolveGen.ExecuteCommand(command, ref pEvalData);
            }

            SolveResult invalidSolveResult = SolveResult.InvalidCmd(ref pEvalData);
            return invalidSolveResult;
        }

        public bool Init(EquationInformation eqInfo, ExComp left, ExComp right, List<TypePair<LexemeType, string>> lexemeTable,
            Dictionary<string, int> solveVars, string probSolveVar, ref EvalData pEvalData)
        {
            if (eqInfo.OnlyFactors)
                return false;

            if (probSolveVar == null)
                return false;

            _solveFor = new AlgebraVar(probSolveVar);
            AlgebraComp solveForComp = _solveFor.ToAlgebraComp();

            if (eqInfo.AppliedFunctions.Count != 0 || !eqInfo.HasOnlyPowers(new ExNumber(2.0), new ExNumber(1.0)))
                return false;

            if ((left is AlgebraComp && !solveForComp.IsEqualTo(left)) || left is FunctionDefinition)
            {
                _funcIden = left;
                left = null;
            }
            if ((right is AlgebraComp && !solveForComp.IsEqualTo(right)) || right is FunctionDefinition)
            {
                _funcIden = right;
                right = null;
            }

            if (_funcIden is FunctionDefinition)
            {
                tt_func = new FunctionGenTermType();
                if (!tt_func.Init(new EqSet(_funcIden, left == null ? right : left, LexemeType.EqualsOp), lexemeTable, solveVars, probSolveVar))
                    tt_func = null;
            }

            if (right == null && left == null)
                return false;
            else if (right == null)
                _eq = left.ToAlgTerm();
            else if (left == null)
                _eq = right.ToAlgTerm();
            else
                _eq = SubOp.StaticCombine(left, right).ToAlgTerm();

            string[] promptStrs = null;
            if (_funcIden != null || left != null || right != null)
            {
                promptStrs = new string[3];
                promptStrs[0] = "Find zeros for ";
                promptStrs[1] = "By completing the square find zeros for ";
                promptStrs[2] = "By quadratic equation find zeros for ";
            }
            else if (left != null && right != null)
            {
                promptStrs = new string[3];
                promptStrs[0] = "Solve for ";
                promptStrs[1] = "By completing the square solve for ";
                promptStrs[2] = "By quadratic equation solve for ";
            }

            if (promptStrs != null)
                _ttSolveGen = new SolveGenTermType(new EqSet(_eq, ExNumber.GetZero(), LexemeType.EqualsOp), lexemeTable, solveVars, probSolveVar, promptStrs,
                    _funcIden is AlgebraComp ? (_funcIden as AlgebraComp).GetVar().GetVar() : "");

            AlgebraTerm nullTerm = null;

            Solving.SolveMethod.PrepareForSolving(ref _eq, ref nullTerm, ref pEvalData);

            _eq = _eq.RemoveRedundancies(false).ToAlgTerm();

            List<ExComp[]> squaredGroups = _eq.GetGroupContainingTerm(solveForComp.ToPow(2.0));
            List<ExComp[]> linearGroups = _eq.GetGroupContainingTerm(solveForComp);
            List<AlgebraGroup> constantGroup = _eq.GetGroupsConstantTo(solveForComp);

            AlgebraTerm[] aTermsArr = new AlgebraTerm[squaredGroups.Count];
            for (int i = 0; i < squaredGroups.Count; ++i)
            {
                aTermsArr[i] =
                    GroupHelper.ToAlgTerm(GroupHelper.GetUnrelatableTermsOfGroup(squaredGroups[i], solveForComp));
            }

            AlgebraTerm[] bTermsArr = new AlgebraTerm[linearGroups.Count];
            for (int i = 0; i < linearGroups.Count; ++i)
            {
                bTermsArr[i] =
                    GroupHelper.ToAlgTerm(GroupHelper.GetUnrelatableTermsOfGroup(linearGroups[i], solveForComp));
            }

            if (aTermsArr.Length == 0)
                return false;

            AlgebraTerm a = new AlgebraTerm();
            foreach (AlgebraTerm aTerm in aTermsArr)
            {
                a = AlgebraTerm.OpAdd(a, aTerm);
            }

            AlgebraTerm b = new AlgebraTerm();
            foreach (AlgebraTerm bTerm in bTermsArr)
            {
                b = AlgebraTerm.OpAdd(b, bTerm);
            }

            AlgebraTerm c = new AlgebraTerm(constantGroup.ToArray());

            _a = a;
            _b = b;
            if (c.GetTermCount() == 0)
                c.Add(ExNumber.GetZero());
            _c = c;

            if (_a is AlgebraTerm)
                _a = (_a as AlgebraTerm).RemoveRedundancies(false);
            if (_b is AlgebraTerm)
                _b = (_b as AlgebraTerm).RemoveRedundancies(false);
            if (_c is AlgebraTerm)
                _c = (_c as AlgebraTerm).RemoveRedundancies(false);

            List<string> tmpCmds = new List<string>();
            if (_ttSolveGen != null)
            {
                tmpCmds.AddRange(_ttSolveGen.GetCommands());
            }
            tmpCmds.Add("To vertex form");
            tmpCmds.Add("Find the vertex");
            tmpCmds.Add("Factor");
            tmpCmds.Add("Find the discriminant");
            tmpCmds.Add("Find axis of symmetry");

            if (tt_func != null)
            {
                if (_ttSolveGen != null)
                    tmpCmds.AddRange(ArrayFunc.ToList(tt_func.GetCommands()).GetRange(0, 2));
                else
                    tmpCmds.AddRange(tt_func.GetCommands());
            }

            if (_eq.GetAllAlgebraCompsStr().Count == 1)
            {
                _graphStr = _eq.ToJavaScriptString(pEvalData.GetUseRad());
                if (_graphStr != null)
                    tmpCmds.Add("Graph");
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }

        private static ExComp FindAOS(ExComp a, ExComp b)
        {
            ExComp num = MulOp.Negate(b);
            ExComp den = MulOp.StaticCombine(new ExNumber(2.0), a);

            return DivOp.StaticCombine(num, den);
        }

        private static ExVector FindVertex(ExComp a, ExComp b, ExComp c, ExComp aos)
        {
            ExComp ex0 = MulOp.StaticCombine(aos, aos);
            ex0 = MulOp.StaticCombine(ex0, a);
            ExComp ex1 = MulOp.StaticCombine(b, aos);

            ExComp finalCombined = AddOp.StaticCombine(ex0, ex1);
            if (finalCombined is AlgebraTerm)
                finalCombined = (finalCombined as AlgebraTerm).CompoundFractions();
            finalCombined = AddOp.StaticCombine(finalCombined, c);

            if (finalCombined is AlgebraTerm)
                finalCombined = (finalCombined as AlgebraTerm).CompoundFractions();

            return new ExVector(aos, finalCombined);
        }

        private static ExComp FindVertex(ExComp a, ExComp b, ExComp c)
        {
            ExComp aos = FindAOS(a, b);

            return FindVertex(a, b, c, aos);
        }

        private static ExComp ToVertexForm(ExComp a, ExComp b, ExComp c, AlgebraComp solveFor, ref EvalData pEvalData)
        {
            if (!ExNumber.GetOne().IsEqualTo(a))
            {
                b = DivOp.StaticCombine(b, a);
                c = DivOp.StaticCombine(c, a);
            }

            ExComp halfB = DivOp.StaticCombine(b, new ExNumber(2.0));
            ExComp completeTheSquareTerm = PowOp.RaiseToPower(halfB, new ExNumber(2.0), ref pEvalData, false);

            ExComp hVal = AddOp.StaticCombine(c, MulOp.Negate(completeTheSquareTerm));

            if (hVal is AlgebraTerm)
            {
                hVal = (hVal as AlgebraTerm).CompoundFractions();
            }

            AlgebraTerm finalTerm = new AlgebraTerm();
            if (!ExNumber.GetOne().IsEqualTo(a))
                finalTerm.Add(a, new MulOp());

            if (!ExNumber.GetZero().IsEqualTo(halfB))
                finalTerm.Add(new Equation.Functions.PowerFunction(new AlgebraTerm(solveFor, new AddOp(), halfB), new ExNumber(2.0)));
            else
                finalTerm.Add(solveFor);

            if (!ExNumber.GetZero().IsEqualTo(hVal))
                finalTerm.Add(new AddOp(), hVal);

            return finalTerm;
        }
    }
}