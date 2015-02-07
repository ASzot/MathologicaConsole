using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class CubicSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public CubicSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public override Equation.ExComp SolveEquation(Equation.AlgebraTerm left, Equation.AlgebraTerm right, Equation.AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            //int prevWorkCnt = WorkMgr.WorkSteps.Count;

            ExComp factorResult = Factor(ref left, ref right, solveFor, ref pEvalData);
            if (factorResult != null)
                return factorResult;

            //int rmWrkCnt = WorkMgr.WorkSteps.Count - prevWorkCnt;
            //WorkMgr.PopSteps(rmWrkCnt);

            ExComp polySolveResult = (new PolynomialSolve(p_agSolver)).SolveEquation(left, right, solveFor, ref pEvalData);
            if (polySolveResult != null)
                return polySolveResult;

            pEvalData.AddFailureMsg("Couldn't solve the cubic equation!");
            return null;
        }

        private ExComp Factor(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            left = left.EvaluatePowers(ref pEvalData);
            right = right.EvaluatePowers(ref pEvalData);

            if (!right.IsZero())
            {
                // Move everything to the left side.

                pEvalData.WorkMgr.FromSides(SubOp.StaticWeakCombine(left, right), Number.Zero, "Move everything to the left side.");

                left = SubOp.StaticCombine(left, right).ToAlgTerm();
                right = Number.Zero.ToAlgTerm();

                if (!left.Contains(solveFor.ToAlgebraComp()))
                {
                    if (Simplifier.AreEqual(left, right, ref pEvalData))
                        return new AllSolutions();
                    else
                        return new NoSolutions();
                }

                pEvalData.WorkMgr.FromSides(left, right, "Simplify.");
            }

            var powersOfVar = left.GetPowersOfVar(solveFor.ToAlgebraComp());

            if (powersOfVar.Count == 1)
                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);

            bool allValid = true;
            foreach (var pow in powersOfVar)
            {
                if (!(pow is Number))
                {
                    allValid = false;
                    break;
                }

                Number nPow = pow as Number;
                if (nPow > (new Number(3.0)))
                {
                    allValid = false;
                    break;
                }
            }

            if (!powersOfVar.Contains(new Number(3.0)) || !allValid)
                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);

            ExComp[] groupGcf = left.GetGroupGCF();
            if (groupGcf != null && groupGcf.Length != 0)
            {
                AlgebraTerm factorOut = groupGcf.ToAlgTerm();
                if (!factorOut.IsOne())
                {
                    left = DivOp.StaticCombine(left, factorOut).ToAlgTerm();

                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "(" + factorOut.FinalToDispStr() + ")(" + left.FinalToDispStr() + ")=" + right.FinalToDispStr() + WorkMgr.EDM,
                        "Factor " + WorkMgr.STM + factorOut.FinalToDispStr() + WorkMgr.EDM + " from the expression.");

                    if (factorOut.Contains(solveFor.ToAlgebraComp()))
                    {
                        AlgebraTerm solveFactorsTerm = AlgebraTerm.FromFactors(factorOut, left);
                        FactorSolve factorSolve = new FactorSolve(p_agSolver);
                        return factorSolve.SolveEquation(solveFactorsTerm, right, solveFor, ref pEvalData);
                    }
                }
            }

            pEvalData.WorkMgr.FromSides(left, right, "Solve this problem by factoring.");
            int startCount = pEvalData.WorkMgr.WorkSteps.Count;

            AlgebraTerm[] factors = left.GetFactors(ref pEvalData);
            if (factors == null)
            {
                // Remove all the steps that were associated with factoring.
                pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - startCount);
                pEvalData.WorkMgr.FromSides(left, right, "This cubic doesn't factor.");
                return null;
            }

            if (pEvalData.WorkMgr.AllowWork)
            {
                string factorsStr = "";
                foreach (AlgebraTerm factor in factors)
                {
                    factorsStr += "(" + factor.FinalToDispStr() + ")";
                }

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + factorsStr + "=0" + WorkMgr.EDM, "Solve for each of the factors independently.");
            }

            AlgebraTerm zeroTerm = Number.Zero.ToAlgTerm();

            AlgebraTermArray factorsArray = new AlgebraTermArray(factors);
            AlgebraTermArray solutions = factorsArray.SimulSolve(zeroTerm, solveFor, p_agSolver, ref pEvalData, true);
            if (solutions == null)
                return null;

            solutions.RemoveDuplicateTerms();

            return solutions;
        }
    }
}