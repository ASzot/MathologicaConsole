using MathSolverWebsite.MathSolverLibrary.Equation;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class LinearSolve : SolveMethod
    {
        private const int MAX_LINEAR_REPEAT_COUNT = 3;

        private int _linearSolveRepeatCount = 0;

        private AlgebraSolver p_agSolver;

        public LinearSolve(AlgebraSolver pAgSolver, int linearSolveRepeatCount)
        {
            p_agSolver = pAgSolver;
            _linearSolveRepeatCount = linearSolveRepeatCount;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            if (_linearSolveRepeatCount > MAX_LINEAR_REPEAT_COUNT)
                return null;

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);
            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }
            CombineFractions(ref left, ref right, ref pEvalData);
            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData, false);

            right = right.Order();

            if (!left.RemoveRedundancies(false).IsEqualTo(solveForComp))
            {
                pEvalData.SetIsWorkable(false);
                // We may have a fraction which hasn't been entirely solved.
                // The linear solve repeat count is to ensure an overflow exception doesn't occur
                // with potential recursive linear solves.
                ExComp solveResult = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                return solveResult;
            }

            pEvalData.AttemptSetInputType(TermType.InputType.Linear);

            return right;
        }
    }
}