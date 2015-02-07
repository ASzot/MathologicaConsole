using MathSolverWebsite.MathSolverLibrary.Equation;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class SubstitutionSolve : SolveMethod
    {
        private ExComp _subOut;
        private AlgebraSolver p_agSolver;

        public SubstitutionSolve(AlgebraSolver pAgSolver, ExComp subOut)
        {
            p_agSolver = pAgSolver;
            _subOut = subOut;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp subIn = p_agSolver.NextSubVar();

            pEvalData.WorkMgr.FromSides(left, right, "Make the substitution " + WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(_subOut) + "=" + WorkMgr.ExFinalToAsciiStr(subIn) + WorkMgr.EDM);

            left = left.Substitute(_subOut, subIn);
            right = right.Substitute(_subOut, subIn);

            pEvalData.WorkMgr.FromSides(left, right, "Substitute in.");

            ExComp result = p_agSolver.SolveEq(subIn.Var, left, right, ref pEvalData);
            if (result == null)
                return null;

            if (result is AlgebraTermArray)
            {
                AlgebraTermArray resultArray = result as AlgebraTermArray;

                result = resultArray.SimulSolve(_subOut.ToAlgTerm(), solveFor, p_agSolver, ref pEvalData);
                if (result == null)
                    return null;
            }
            else
            {
                left = _subOut.ToAlgTerm();
                right = result.ToAlgTerm();
                result = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            }

            return result;
        }
    }
}