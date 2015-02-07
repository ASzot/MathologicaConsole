using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class LogBaseSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public LogBaseSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            // Convert everything to exponential form.
            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);
            VariableFractionsToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }
            left = left.CompoundLogs();

            if (left.GroupCount != 1)
                return null;

            ExComp leftLogEx = left.RemoveRedundancies();

            if (!(leftLogEx is LogFunction))
            {
                pEvalData.AddFailureMsg("Couldn't isolate logarithm.");
                return null;
            }

            LogFunction log = leftLogEx as LogFunction;

            PowerFunction powFunc = new PowerFunction(log.Base, right);

            pEvalData.WorkMgr.FromSides(powFunc, log.InnerEx, "Convert from logarithm form to exponential form.");

            return p_agSolver.SolveEq(solveFor, powFunc, log.InnerTerm, ref pEvalData);
        }
    }
}