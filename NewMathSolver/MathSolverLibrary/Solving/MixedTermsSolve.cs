using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class MixedTermsSolve : SolveMethod
    {
        private ExComp _pow;
        private ExComp _root;
        private AlgebraSolver p_solver;

        public MixedTermsSolve(ExComp root, ExComp pow, AlgebraSolver pSolver)
        {
            _root = root;
            _pow = pow;
            p_solver = pSolver;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            ConstantsToRight(ref right, ref left, solveForComp, ref pEvalData);
            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);

            if (_root is AlgebraTerm)
            {
                _root = (_root as AlgebraTerm).RemoveRedundancies(false);
            }

            System.Collections.Generic.List<ExComp> rootTerms = left.GetGroupPow(_root);
            if (rootTerms == null || rootTerms.Count == 0)
                return null;

            AlgebraTerm rootTerm = new AlgebraTerm();
            foreach (ExComp rt in rootTerms)
            {
                rootTerm = AlgebraTerm.OpAdd(rootTerm, rt.ToAlgTerm());
            }

            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            pEvalData.SetCheckSolutions(true);

            CombineFractions(ref left, ref right, ref pEvalData);

            left = SubOp.StaticCombine(left, rootTerm).ToAlgTerm();
            right = SubOp.StaticCombine(right, rootTerm).ToAlgTerm();

            pEvalData.GetWorkMgr().FromSides(left, right, "Isolate the radical term to the right side.");

            SimpleFraction fracPow = new SimpleFraction();
            if (!fracPow.Init(_root as AlgebraTerm))
                return null;

            ExComp rootEx = fracPow.GetReciprocal();
            if (!(rootEx is ExNumber))
            {
                pEvalData.AddFailureMsg("Cannot solve with the expression due to non integer roots");
                return null;
            }

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Raise both sides to the " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " power.",
                PowOp.StaticWeakCombine(left, rootEx), PowOp.StaticWeakCombine(right, rootEx), rootEx);

            left = PowOp.RaiseToPower(left, rootEx as ExNumber, ref pEvalData, true).ToAlgTerm();
            right = PowOp.RaiseToPower(right, rootEx as ExNumber, ref pEvalData, true).ToAlgTerm();

            if (right is Equation.Functions.PowerFunction)
                return null;

            pEvalData.GetWorkMgr().FromSides(left, right, "Simplify.");

            ExComp agSolveResult = p_solver.SolveEq(solveFor, left, right, ref pEvalData);
            return agSolveResult;
        }
    }
}