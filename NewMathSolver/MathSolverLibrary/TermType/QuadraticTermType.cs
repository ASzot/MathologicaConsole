using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class QuadraticTermType : TermType
    {
        private ExComp _a;
        private ExComp _b;
        private ExComp _c;
        private AlgebraTerm _eq;
        private ExComp _funcIden;
        private AlgebraVar _solveFor;
        private FunctionTermType tt_func = null;
        private SolveTermType tt_solve = null;
        private string _graphStr = null;

        public QuadraticTermType()
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
                    return SolveResult.SolvedForceFormatting(_funcIden, vertexForm);
                return SolveResult.SimplifiedForceFormatting(vertexForm);
            }
            else if (command == "Find the vertex")
            {
                // Find the vertex.
                ExComp vertex = FindVertex(_a, _b, _c);
                return SolveResult.Simplified(vertex);
            }
            else if (command == "Factor")
            {
                AlgebraTerm[] factors = AdvAlgebraTerm.Factorize(_a, _b, _c, _solveFor, ref pEvalData);
                if (factors == null)
                {
                    pEvalData.AddFailureMsg("Couldn't factor.");
                    return SolveResult.Failure();
                }
                // Factor.
                AlgebraTerm factored = AlgebraTerm.FromFactors(factors);
                if (_funcIden != null)
                    return SolveResult.Solved(_funcIden, factored, ref pEvalData);
                else
                    return SolveResult.Simplified(factored);
            }
            else if (command == "Find the discriminant")
            {
                // Discriminant.
                pEvalData.WorkMgr.FromFormatted("`" + _eq.FinalToDispStr() + "`", "In the quadratic above `a=" + _a.ToDispString() + ", b=" + _b.ToDispString() + ",c=" + _c.ToDispString() +
                    "`. The equation for getting the discriminant of a quadratic is `b^2-4ac`.");

                pEvalData.WorkMgr.FromFormatted("`({0})^2-4({1})({2})`", "Plug the values into the equation.", _a, _b, _c);

                ExComp subVal = MulOp.StaticCombine(_a, _c);
                subVal = MulOp.StaticCombine(subVal, new Number(4.0));
                ExComp discriminant = SubOp.StaticCombine(PowOp.StaticCombine(_b, new Number(2.0)), subVal);

                pEvalData.WorkMgr.FromFormatted("`" + WorkMgr.ExFinalToAsciiStr(discriminant) + "`", "Simplify getting the final discriminant.");

                return SolveResult.Simplified(discriminant);
            }
            else if (command == "Find axis of symmetry")
            {
                // Axis of symmetry.
                ExComp aos = FindAOS(_a, _b);
                return SolveResult.Solved(_solveFor, aos, ref pEvalData);
            }
            else if (command == "Graph")
            {
                if (pEvalData.AttemptSetGraphData(_graphStr, _solveFor.Var))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            if (tt_func != null && tt_func.IsValidCommand(command))
                return tt_func.ExecuteCommand(command, ref pEvalData);
            else if (tt_solve != null && tt_solve.IsValidCommand(command))
            {
                if (command.Contains("completing the square"))
                    pEvalData.QuadSolveMethod = Solving.QuadraticSolveMethod.CompleteSquare;
                else if (command.Contains("quadratic equation"))
                    pEvalData.QuadSolveMethod = Solving.QuadraticSolveMethod.Formula;
                else
                {
                    // The default is factoring as this will go on to the quadratic formula method if necessary.
                    pEvalData.QuadSolveMethod = Solving.QuadraticSolveMethod.Factor;
                }
                return tt_solve.ExecuteCommand(command, ref pEvalData);
            }

            return SolveResult.InvalidCmd(ref pEvalData);
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

            if (eqInfo.AppliedFunctions.Count != 0 || !eqInfo.HasOnlyPowers(new Number(2.0), new Number(1.0)))
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
                tt_func = new FunctionTermType();
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
                tt_solve = new SolveTermType(new EqSet(_eq, Number.Zero, LexemeType.EqualsOp), lexemeTable, solveVars, probSolveVar, promptStrs,
                    _funcIden is AlgebraComp ? (_funcIden as AlgebraComp).Var.Var : "");

            AlgebraTerm nullTerm = null;

            Solving.SolveMethod.PrepareForSolving(ref _eq, ref nullTerm, ref pEvalData);

            _eq = _eq.RemoveRedundancies().ToAlgTerm();

            var squaredGroups = _eq.GetGroupContainingTerm(solveForComp.ToPow(2.0));
            var linearGroups = _eq.GetGroupContainingTerm(solveForComp);
            var constantGroup = _eq.GetGroupsConstantTo(solveForComp);

            var aTerms = from squaredGroup in squaredGroups
                         select squaredGroup.GetUnrelatableTermsOfGroup(solveForComp).ToAlgTerm();

            var bTerms = from linearGroup in linearGroups
                         select linearGroup.GetUnrelatableTermsOfGroup(solveForComp).ToAlgTerm();

            if (aTerms.Count() == 0)
                return false;

            AlgebraTerm a = new AlgebraTerm();
            foreach (AlgebraTerm aTerm in aTerms)
            {
                a = a + aTerm;
            }

            AlgebraTerm b = new AlgebraTerm();
            foreach (AlgebraTerm bTerm in bTerms)
            {
                b = b + bTerm;
            }

            AlgebraTerm c = new AlgebraTerm(constantGroup.ToArray());

            _a = a;
            _b = b;
            if (c.TermCount == 0)
                c.Add(Number.Zero);
            _c = c;

            if (_a is AlgebraTerm)
                _a = (_a as AlgebraTerm).RemoveRedundancies();
            if (_b is AlgebraTerm)
                _b = (_b as AlgebraTerm).RemoveRedundancies();
            if (_c is AlgebraTerm)
                _c = (_c as AlgebraTerm).RemoveRedundancies();

            List<string> tmpCmds = new List<string>();
            if (tt_solve != null)
            {
                tmpCmds.AddRange(tt_solve.GetCommands());
            }
            tmpCmds.Add("To vertex form");
            tmpCmds.Add("Find the vertex");
            tmpCmds.Add("Factor");
            tmpCmds.Add("Find the discriminant");
            tmpCmds.Add("Find axis of symmetry");

            if (tt_func != null)
            {
                if (tt_solve != null)
                    tmpCmds.AddRange(tt_func.GetCommands().ToList().GetRange(0, 2));
                else
                    tmpCmds.AddRange(tt_func.GetCommands());
            }

            _graphStr = _eq.ToJavaScriptString(pEvalData.UseRad);
            if (_graphStr != null)
                tmpCmds.Add("Graph");

            _cmds = tmpCmds.ToArray();

            return true;
        }

        private static ExComp FindAOS(ExComp a, ExComp b)
        {
            ExComp num = MulOp.Negate(b);
            ExComp den = MulOp.StaticCombine(new Number(2.0), a);

            return DivOp.StaticCombine(num, den);
        }

        private static ExVector FindVertex(ExComp a, ExComp b, ExComp c, ExComp aos)
        {
            ExComp ex0 = MulOp.StaticCombine(aos, aos);
            ex0 = MulOp.StaticCombine(ex0, a);
            ExComp ex1 = MulOp.StaticCombine(b, aos);

            ExComp final = AddOp.StaticCombine(ex0, ex1);
            if (final is AlgebraTerm)
                final = (final as AlgebraTerm).CompoundFractions();
            final = AddOp.StaticCombine(final, c);

            if (final is AlgebraTerm)
                final = (final as AlgebraTerm).CompoundFractions();

            return new ExVector(aos, final);
        }

        private static ExComp FindVertex(ExComp a, ExComp b, ExComp c)
        {
            ExComp aos = FindAOS(a, b);

            return FindVertex(a, b, c, aos);
        }

        private static ExComp ToVertexForm(ExComp a, ExComp b, ExComp c, AlgebraComp solveFor, ref EvalData pEvalData)
        {
            if (!Number.One.IsEqualTo(a))
            {
                b = DivOp.StaticCombine(b, a);
                c = DivOp.StaticCombine(c, a);
            }

            ExComp halfB = DivOp.StaticCombine(b, new Number(2.0));
            ExComp completeTheSquareTerm = PowOp.RaiseToPower(halfB, new Number(2.0), ref pEvalData);

            ExComp hVal = AddOp.StaticCombine(c, MulOp.Negate(completeTheSquareTerm));

            if (hVal is AlgebraTerm)
            {
                hVal = (hVal as AlgebraTerm).CompoundFractions();
            }

            AlgebraTerm final = new AlgebraTerm();
            if (!Number.One.IsEqualTo(a))
                final.Add(a, new MulOp());

            if (!Number.Zero.IsEqualTo(halfB))
                final.Add(new Equation.Functions.PowerFunction(new AlgebraTerm(solveFor, new AddOp(), halfB), new Number(2.0)));
            else
                final.Add(solveFor);

            if (!Number.Zero.IsEqualTo(hVal))
                final.Add(new AddOp(), hVal);

            return final;
        }
    }
}