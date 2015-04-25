using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Solving.Diff_Eqs
{
    class DiffAgSolver
    {
        private static ExComp RemoveDiff(AlgebraTerm term)
        {
            var gps = term.GetGroupsNoOps();
            if (gps.Count != 1)
                return null;
            ExComp[] removeDiff = RemoveDiff(gps[0]);
            if (removeDiff == null)
                return null;

            return removeDiff.ToAlgTerm().RemoveRedundancies();
        }

        private static ExComp[] RemoveDiff(ExComp[] gp)
        {
            bool succ = false;
            for (int i = 0; i < gp.Length; ++i)
            {
                if (gp[i] is Derivative)
                {
                    gp = gp.RemoveEx(gp[i]);
                    succ = true;
                    break;
                }
            }

            if (!succ)
                return null;

            if (gp.Length == 0)
                gp = new ExComp[] { Number.One };

            return gp;
        }

        private static ExComp[] SimpleSeperable(ExComp left, ExComp right)
        {
            left = RemoveDiff(left.ToAlgTerm());
            if (left == null)
                return null;
            return new ExComp[] { left, right };
        }

        private static ExComp[] Seperable(AlgebraTerm left, AlgebraTerm right, AlgebraComp solveFunc, AlgebraComp withRespect, 
            ref TermType.EvalData pEvalData)
        {
            var leftGps = left.GetGroups();
            var rightGps = right.GetGroups();

            if (leftGps.Count != 1 || rightGps.Count != 1)
                return null;

            ExComp[] leftGp = null;
            ExComp[] rightGp = null;
            if (ContainsDerivative(leftGps[0]))
            {
                leftGp = leftGps[0];
                rightGp = rightGps[0];
            }
            if (ContainsDerivative(rightGps[0]))
            {
                // There cannot  be derivatives on both sides.
                if (leftGp != null)
                    return null;
                leftGp = rightGps[0];
                rightGp = leftGps[0];
            }

            if (leftGp == null)
                return null;

            leftGp = RemoveDiff(leftGp);
            if (leftGp == null)
                return null;

            // Make sure all of the x's are in the right and the y's in the left.
            left = leftGp.ToAlgNoRedunTerm();
            right = rightGp.ToAlgNoRedunTerm();
            SolveMethod.DivideByVariableCoeffs(ref left, ref right, solveFunc, ref pEvalData, true);
            SolveMethod.DivideByVariableCoeffs(ref right, ref left, withRespect, ref pEvalData, true);

            if (left.Contains(withRespect) || right.Contains(solveFunc))
                return null;

            return new ExComp[] { left, right };
        }

        public static bool ContainsDerivative(ExComp ex)
        {
            if (ex is Derivative)
                return true;
            else if (ex is AlgebraTerm)
            {
                AlgebraTerm term = ex as AlgebraTerm;
                foreach (ExComp subEx in term.SubComps)
                {
                    if (ContainsDerivative(subEx))
                        return true;
                }
            }

            return false;
        }

        public static bool ContainsDerivative(ExComp[] gp)
        {
            foreach (ExComp ex in gp)
            {
                if (ContainsDerivative(ex))
                    return true;
            }

            return false;
        }

        private static ExComp[] SolveDiffEq(AlgebraTerm ex0Term, AlgebraTerm ex1Term, AlgebraComp solveForFunc,
            AlgebraComp withRespect, int order, ref TermType.EvalData pEvalData)
        {
            // Try seperable differential equations.
            // Get to the form N(y)y'=M(x)
            // Are we already in that form?
            ExComp left = null;
            ExComp right = null;
            if (!ex0Term.Contains(withRespect) && !ex1Term.Contains(solveForFunc) && ContainsDerivative(ex0Term))
            {
                left = ex0Term;
                right = ex1Term;
            }
            else if (!ex1Term.Contains(withRespect) && !ex0Term.Contains(solveForFunc) && ContainsDerivative(ex1Term))
            {
                left = ex1Term;
                right = ex0Term;
            }

            ExComp[] leftRight = null;
            if (left != null && right != null)
                leftRight = SimpleSeperable(left, right);
            else if (leftRight == null)
                leftRight = Seperable(ex0Term, ex1Term, solveForFunc, withRespect, ref pEvalData);

            if (leftRight != null)
            {
                leftRight[0] = Integral.TakeAntiDeriv(leftRight[0], solveForFunc, ref pEvalData);
                leftRight[1] = Integral.TakeAntiDeriv(leftRight[1], withRespect, ref pEvalData);

                return leftRight;
            }

            return null;
        }

        public static SolveResult Solve(ExComp ex0, ExComp ex1, AlgebraComp solveForFunc, AlgebraComp withRespect, int order, ref TermType.EvalData pEvalData)
        {
            if (order > 1)
                return SolveResult.Failure("Cannot solve differential equations with an order greater than one", ref pEvalData);

            ExComp[] leftRight = SolveDiffEq(ex0.ToAlgTerm(), ex1.ToAlgTerm(), solveForFunc, withRespect, order, ref pEvalData);
            if (leftRight == null)
                return SolveResult.Failure();

            AlgebraSolver agSolver = new AlgebraSolver();
            ExComp solved = agSolver.SolveEq(solveForFunc.Var, leftRight[0].Clone().ToAlgTerm(), leftRight[1].Clone().ToAlgTerm(), ref pEvalData);
            if (solved == null)
                return SolveResult.Solved(leftRight[0], leftRight[1], ref pEvalData);

            return SolveResult.Solved(solveForFunc, solved, ref pEvalData);
        }
    }
}
