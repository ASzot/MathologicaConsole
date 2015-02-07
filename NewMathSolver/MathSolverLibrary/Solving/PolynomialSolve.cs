using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class PolynomialSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public PolynomialSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public override Equation.ExComp SolveEquation(Equation.AlgebraTerm left, Equation.AlgebraTerm right, Equation.AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            PrepareForSolving(ref left, ref right, ref pEvalData);

            left = left.EvaluatePowers(ref pEvalData);
            right = right.EvaluatePowers(ref pEvalData);

            bool isRightZero = right.IsZero();

            if (!isRightZero)
            {
                pEvalData.WorkMgr.FromSubtraction(right, left, right);
            }

            left = SubOp.StaticCombine(left, right).ToAlgTerm();
            right = Number.Zero.ToAlgTerm();

            if (!left.Contains(solveFor.ToAlgebraComp()))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            if (!isRightZero)
            {
                pEvalData.WorkMgr.FromSides(left, right);
            }

            ExComp[] groupGcf = left.GetGroupGCF();
            if (groupGcf != null && groupGcf.Length != 0)
            {
                AlgebraTerm factorOut = groupGcf.ToAlgTerm();
                if (!factorOut.IsOne())
                {
                    left = DivOp.StaticCombine(left, factorOut).ToAlgTerm();
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Factor out " + WorkMgr.STM + "{2}" + WorkMgr.EDM, MulOp.StaticWeakCombine(factorOut, left), right, factorOut);
                    AlgebraTerm solveFactorsTerm = AlgebraTerm.FromFactors(factorOut, left);
                    FactorSolve factorSolve = new FactorSolve(p_agSolver);
                    return factorSolve.SolveEquation(solveFactorsTerm, right, solveFor, ref pEvalData);
                }
            }

            PolynomialExt poly = new PolynomialExt();
            if (!poly.Init(left))
                return null;

            List<ExComp> possibleRoots = poly.GetRationalPossibleRoots();

            List<ExComp> successfulRoots = new List<ExComp>();
            if (successfulRoots == null)
                return null;
            ExComp additionalSol = null;

            foreach (ExComp possibleRoot in possibleRoots)
            {
                int prevMaxPow = poly.MaxPow;
                List<ExComp> results;
                List<ExComp> muls;
                PolynomialExt attemptPoly = poly.AttemptSynthDiv(possibleRoot, out muls, out results);

                if (attemptPoly.MaxPow != prevMaxPow)
                {
                    string workStr = WorkMgr.WorkFromSynthDivTable(possibleRoot, poly.GetCoeffs(), muls, results);
                    if (workStr == null)
                    {
                        pEvalData.AddFailureMsg("Work formatting error");
                        return null;
                    }

                    pEvalData.WorkMgr.FromFormatted(workStr, "The root " + WorkMgr.STM + "{0}" + WorkMgr.EDM + " works with synthetic division, therefore " + WorkMgr.STM + "{2}={0}" + WorkMgr.EDM +
                        ". The polynomial " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " remains based on the remainder of the division.", possibleRoot, attemptPoly.ToAlgTerm(), solveFor);

                    poly = attemptPoly;
                    // The synthetic division was successful.
                    successfulRoots.Add(possibleRoot);

                    if (poly.MaxPow <= 2)
                    {
                        // We can solve this elseways.
                        AlgebraTerm tmpLeft = poly.ToAlgTerm();
                        additionalSol = p_agSolver.SolveEq(solveFor, tmpLeft, right, ref pEvalData);
                        break;
                    }
                }
            }

            if (additionalSol == null)
            {
                if (successfulRoots.Count == 0)
                {
                    // Couldn't solve the equation at all.
                    pEvalData.AddFailureMsg("Polynomial contains no real rational roots.");
                    return null;
                }
                else
                {
                    // We got somewhere.
                    AlgebraTerm partialSol = new AlgebraTerm();
                    partialSol.Add(poly.ToAlgTerm());
                    AlgebraComp solveForComp = solveFor.ToAlgebraComp();
                    foreach (ExComp successfulRoot in successfulRoots)
                    {
                        AlgebraTerm factor = AlgebraTerm.FromFactor(solveForComp, successfulRoot);
                        partialSol.Add(new MulOp(), factor);
                    }

                    pEvalData.AddPartialSol(partialSol);

                    return new AlgebraTermArray(successfulRoots);
                }
            }

            if (additionalSol is AlgebraTermArray)
                successfulRoots.AddRange((additionalSol as AlgebraTermArray).Terms);
            else
                successfulRoots.Add(additionalSol);
            AlgebraTermArray sols = new AlgebraTermArray(successfulRoots);
            //sols.RemoveDuplicateTerms();

            return sols;
        }
    }
}