using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

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

            pEvalData.AttemptSetInputType(TermType.InputType.Cubic);

            pEvalData.AddFailureMsg("Couldn't solve the cubic equation!");
            return null;
        }

        private ExComp Factor(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            left = AdvAlgebraTerm.EvaluatePowers(left, ref pEvalData);
            right = AdvAlgebraTerm.EvaluatePowers(right, ref pEvalData);

            if (!right.IsZero())
            {
                // Move everything to the left side.

                pEvalData.GetWorkMgr().FromSides(SubOp.StaticWeakCombine(left, right), ExNumber.GetZero(), "Move everything to the left side.");

                left = SubOp.StaticCombine(left, right).ToAlgTerm();
                right = ExNumber.GetZero().ToAlgTerm();

                if (!left.Contains(solveFor.ToAlgebraComp()))
                {
                    if (Simplifier.AreEqual(left, right, ref pEvalData))
                        return new AllSolutions();
                    else
                        return new NoSolutions();
                }

                pEvalData.GetWorkMgr().FromSides(left, right, "Simplify.");
            }

            System.Collections.Generic.List<ExComp> powersOfVar = left.GetPowersOfVar(solveFor.ToAlgebraComp());

            if (powersOfVar.Count == 1)
            {
                ExComp agSolved = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                return agSolved;
            }

            bool allValid = true;
            foreach (ExComp pow in powersOfVar)
            {
                if (!(pow is ExNumber))
                {
                    allValid = false;
                    break;
                }

                ExNumber nPow = pow as ExNumber;
                if (ExNumber.OpGT(nPow, (new ExNumber(3.0))))
                {
                    allValid = false;
                    break;
                }
            }

            if (!powersOfVar.Contains(new ExNumber(3.0)) || !allValid)
            {
                ExComp agSolved = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                return agSolved;
            }

            ExComp[] groupGcf = left.GetGroupGCF();
            if (groupGcf != null && groupGcf.Length != 0)
            {
                AlgebraTerm factorOut = GroupHelper.ToAlgTerm(groupGcf);
                if (!factorOut.IsOne())
                {
                    left = DivOp.StaticCombine(left, factorOut).ToAlgTerm();

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "(" + factorOut.FinalToDispStr() + ")(" + left.FinalToDispStr() + ")=" + right.FinalToDispStr() + WorkMgr.EDM,
                        "Factor " + WorkMgr.STM + factorOut.FinalToDispStr() + WorkMgr.EDM + " from the expression.");

                    if (factorOut.Contains(solveFor.ToAlgebraComp()))
                    {
                        AlgebraTerm solveFactorsTerm = AlgebraTerm.FromFactors(factorOut, left);
                        FactorSolve factorSolve = new FactorSolve(p_agSolver);
                        ExComp factorSolvedResult = factorSolve.SolveEquation(solveFactorsTerm, right, solveFor, ref pEvalData);
                        return factorSolvedResult;
                    }
                }
            }

            pEvalData.GetWorkMgr().FromSides(left, right, "Solve this problem by factoring.");
            int startCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

            AlgebraTerm[] factors = AdvAlgebraTerm.GetFactors(left, ref pEvalData);
            if (factors == null)
            {
                // Remove all the steps that were associated with factoring.
                pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - startCount);
                pEvalData.GetWorkMgr().FromSides(left, right, "This cubic doesn't factor.");
                return null;
            }

            if (pEvalData.GetWorkMgr().GetAllowWork())
            {
                string factorsStr = "";
                foreach (AlgebraTerm factor in factors)
                {
                    factorsStr += "(" + factor.FinalToDispStr() + ")";
                }

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + factorsStr + "=0" + WorkMgr.EDM, "Solve for each of the factors independently.");
            }

            AlgebraTerm zeroTerm = ExNumber.GetZero().ToAlgTerm();

            AlgebraTermArray factorsArray = new AlgebraTermArray(factors);
            bool allSols = false;
            AlgebraTermArray solutions = factorsArray.SimulSolve(zeroTerm, solveFor, p_agSolver, ref pEvalData, out allSols, true);

            if (allSols)
                return new AllSolutions();

            if (solutions == null)
                return null;

            solutions.RemoveDuplicateTerms();

            return solutions;
        }
    }
}