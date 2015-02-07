using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    public enum QuadraticSolveMethod { Factor, CompleteSquare, Formula };

    internal class QuadraticSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public QuadraticSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public ExComp CompleteTheSquare(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ExComp overall, ref TermType.EvalData pEvalData)
        {
            if (!Number.One.IsEqualTo(a))
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0})/({1})=({0})/({1})" + WorkMgr.EDM, "Factor out the A term of " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " to make completing the square easier.", overall, a);

                b = DivOp.StaticCombine(b, a);
                c = DivOp.StaticCombine(c, a);
            }

            c = MulOp.Negate(c);

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            pEvalData.WorkMgr.FromSides(AddOp.StaticWeakCombine(PowOp.StaticCombine(solveForComp, new Number(2.0)), MulOp.StaticWeakCombine(b, solveForComp)), c, "Move the C value to the other side.");

            ExComp halfB = DivOp.StaticCombine(b, new Number(2.0));
            ExComp completeTheSquareTerm = PowOp.RaiseToPower(halfB, new Number(2.0), ref pEvalData);

            pEvalData.WorkMgr.FromSides(AddOp.StaticWeakCombine(AddOp.StaticCombine(PowOp.StaticWeakCombine(solveForComp, new Number(2.0)), MulOp.StaticWeakCombine(b, solveForComp)), completeTheSquareTerm),
                AddOp.StaticCombine(c, completeTheSquareTerm),
                "Add " + WorkMgr.STM + "(b^2)/4" + WorkMgr.EDM + " or in this case " + WorkMgr.STM +
                (completeTheSquareTerm is AlgebraTerm ? (completeTheSquareTerm as AlgebraTerm).FinalToDispStr() : completeTheSquareTerm.ToMathAsciiString()) +
                WorkMgr.EDM + " to both sides to complete the square by making a perfect square quadratic which can be easily factored.");

            ExComp right = AddOp.StaticCombine(c, completeTheSquareTerm);

            if (right is AlgebraTerm)
                right = (right as AlgebraTerm).CompoundFractions();

            ExComp tmpWeakLeftInner = AddOp.StaticWeakCombine(solveForComp, halfB);
            ExComp tmpWeakLeft = PowOp.StaticWeakCombine(tmpWeakLeftInner, new Number(2.0));

            pEvalData.WorkMgr.FromSides(tmpWeakLeft, right, "Factor the left side and simplify the right side.");

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\sqrt({0})=\\sqrt({1})" + WorkMgr.EDM, "Take the square root of both sides.", tmpWeakLeft, right);

            AlgebraTermArray solutions = PowOp.TakeSqrt(right, ref pEvalData);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}=\\pm({1})" + WorkMgr.EDM, "The square root has positive and negative roots. Both " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM +
                " and " + WorkMgr.STM + "{0}={2}" + WorkMgr.EDM, tmpWeakLeftInner, solutions[0], solutions[1]);

            for (int i = 0; i < solutions.TermCount; ++i)
            {
                if (!Number.Zero.IsEqualTo(halfB))
                {
                    pEvalData.WorkMgr.FromSubtraction(halfB, tmpWeakLeftInner, solutions[i]);
                }

                solutions.Terms[i] = SubOp.StaticCombine(solutions[i], halfB).ToAlgTerm();
                solutions.Terms[i].ReduceFracs();
                solutions.Terms[i] = solutions.Terms[i].CompoundFractions();

                pEvalData.WorkMgr.FromSides(solveForComp, solutions[i], "Simplify.");
            }

            return solutions;
        }

        public ExComp Factor(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            AlgebraTerm[] factors = MathSolverLibrary.Equation.Term.AdvAlgebraTerm.Factorize(a, b, c, solveFor, ref pEvalData, pEvalData.WorkMgr.AllowWork);
            if (factors == null)
                return null;

            AlgebraTerm zeroTerm = Number.Zero.ToAlgTerm();

            ExComp solution1 = p_agSolver.SolveEq(solveFor, factors[0], zeroTerm, ref pEvalData);
            ExComp solution2 = p_agSolver.SolveEq(solveFor, factors[1], zeroTerm, ref pEvalData);

            AlgebraTermArray termArray = new AlgebraTermArray(solution1.ToAlgTerm(), solution2.ToAlgTerm());

            return termArray;
        }

        public ExComp QuadraticFormulaSolve(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{3}=(-({1})\\pm\\sqrt(({1})^2-4({0})({2})))/(2({0}))" + WorkMgr.EDM,
                "Plug values into the quadratic equation.", a, b, c, solveForComp);

            ExComp term1 = MulOp.Negate(b);

            ExComp term2_1 = PowOp.RaiseToPower(b, new Number(2.0), ref pEvalData);

            ExComp term2_2 = MulOp.StaticCombine(a, c);
            term2_2 = MulOp.StaticCombine(term2_2, new Number(4.0));
            ExComp term2 = SubOp.StaticCombine(term2_1, term2_2);
            AlgebraTermArray term2s = PowOp.TakeSqrt(term2, ref pEvalData);
            ExComp term3 = MulOp.StaticCombine(new Number(2.0), a);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{3}=({0}\\pm({1}))/({2})" + WorkMgr.EDM, "Simplify.", term1, term2s[0], term3, solveForComp);

            for (int i = 0; i < term2s.TermCount; ++i)
            {
                ExComp sqrtTerm = term2s[i];
                term2s.Terms[i] = AddOp.StaticCombine(term1.Clone(), term2s[i].Clone()).ToAlgTerm();
                term2s.Terms[i] = DivOp.StaticCombine(term2s[i], term3).ToAlgTerm();

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{4}=({0}+{1})/({2})={3}" + WorkMgr.EDM, "The above term is a root of the quadratic.", term1, sqrtTerm, term3, term2s.Terms[i], solveForComp);
            }

            return term2s;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            left = left.EvaluatePowers(ref pEvalData);
            right = right.EvaluatePowers(ref pEvalData);

            ConstantsToRight(ref right, ref left, solveForComp, ref pEvalData);
            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            left = left.RemoveRedundancies().ToAlgTerm();

            var powersOfVar = left.GetPowersOfVar(solveForComp);

            if (!powersOfVar.ContainsEx(new Number(2.0)) || !powersOfVar.ContainsEx(new Number(1.0)))
                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);

            left = left.EvaluateExponentsCompletely().ToAlgTerm();

            var squaredGroups = left.GetGroupContainingTerm(solveForComp.ToPow(2.0));
            var linearGroups = left.GetGroupContainingTerm(solveForComp);
            var constantGroup = left.GetGroupsConstantTo(solveForComp);

            var aTerms = from squaredGroup in squaredGroups
                         select squaredGroup.GetUnrelatableTermsOfGroup(solveForComp).ToAlgTerm();

            var bTerms = from linearGroup in linearGroups
                         select linearGroup.GetUnrelatableTermsOfGroup(solveForComp).ToAlgTerm();

            AlgebraTerm a = new AlgebraTerm();
            foreach (AlgebraTerm aTerm in aTerms)
            {
                a = a + aTerm;
            }
            if (a.TermCount == 0)
                a = Number.Zero.ToAlgTerm();

            AlgebraTerm b = new AlgebraTerm();
            foreach (AlgebraTerm bTerm in bTerms)
            {
                b = b + bTerm;
            }
            if (b.TermCount == 0)
                b = Number.Zero.ToAlgTerm();

            AlgebraTerm c = new AlgebraTerm(constantGroup.ToArray());
            if (b.TermCount == 0)
                b = Number.Zero.ToAlgTerm();

            int leftGroupCount = left.GroupCount;
            if (leftGroupCount != 3)
            {
                if (leftGroupCount == 2)
                {
                    // Either solve this with a power method or factor out the solve variable.

                    if (!c.IsZero())
                    {
                        // We have a power solve.
                        PowerSolve powSolve = new PowerSolve(p_agSolver, new Number(2.0));
                        return powSolve.SolveEquation(left, right, solveFor, ref pEvalData);
                    }
                    else
                    {
                        // Factor out solve variable. In the case... (ax^2+bx=0)
                        AlgebraTerm toLinearSolve = new AlgebraTerm(a, new MulOp(), solveForComp, new AddOp(), b);
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}({1})={2}" + WorkMgr.EDM, "Factor out " + WorkMgr.STM + "{0}" + WorkMgr.EDM +" from the term.", solveForComp, toLinearSolve, right);
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}({1})={2}" + WorkMgr.EDM, "Solve for each of the factors independently.", solveForComp, toLinearSolve, right);
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}=0" + WorkMgr.EDM, solveForComp);

                        LinearSolve linearSolve = new LinearSolve(p_agSolver, 0);

                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}=0" + WorkMgr.EDM, toLinearSolve);

                        ExComp linearSolved = linearSolve.SolveEquation(toLinearSolve, Number.Zero.ToAlgTerm(), solveFor, ref pEvalData);
                        AlgebraTermArray ata = new AlgebraTermArray(Number.Zero.ToAlgTerm(), linearSolved.ToAlgTerm());
                        return ata;
                    }
                }
            }

            ExComp exA = a.RemoveRedundancies();
            ExComp exB = b.RemoveRedundancies();
            ExComp exC = c.RemoveRedundancies();

            QuadraticSolveMethod originalSolveMethod = pEvalData.QuadSolveMethod;
            if (pEvalData.QuadSolveMethod == QuadraticSolveMethod.Factor)
            {
                pEvalData.WorkMgr.FromSides(left, Number.Zero, "Solve this quadratic equation by factoring.");
                ExComp factorSolutions = Factor(exA, exB, exC, solveFor, ref pEvalData);
                // Null is returned on factoring not working.
                if (factorSolutions != null)
                    return factorSolutions;
                pEvalData.WorkMgr.PopStep();
                pEvalData.WorkMgr.FromSides(left, Number.Zero, "The quadratic cannot be factored.");
                pEvalData.QuadSolveMethod = QuadraticSolveMethod.Formula;
            }
            if (pEvalData.QuadSolveMethod == QuadraticSolveMethod.Formula)
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}=0" + WorkMgr.EDM, "Use the quadratic equation " + WorkMgr.STM + "{4}=(-b&sqrt(b^2-4ac))/(2a)" + WorkMgr.EDM +
                    " where " + WorkMgr.STM + "a={1}" + WorkMgr.EDM + ", " + WorkMgr.STM + "b={2}" + WorkMgr.EDM + ", and " + WorkMgr.STM + "c={3}" + WorkMgr.EDM, left, exA, exB, exC, solveForComp);

                ExComp qfSolutions = QuadraticFormulaSolve(exA, exB, exC, solveFor, ref pEvalData);
                // Restore the selected solve method.
                pEvalData.QuadSolveMethod = originalSolveMethod;
                return qfSolutions;
            }
            else if (pEvalData.QuadSolveMethod == QuadraticSolveMethod.CompleteSquare)
            {
                pEvalData.WorkMgr.FromSides(left, Number.Zero, "Solve this quadratic equation by completing the square.");

                ExComp ctsSolutions = CompleteTheSquare(exA, exB, exC, solveFor, left, ref pEvalData);
                // Restore the selected solve method.
                pEvalData.QuadSolveMethod = originalSolveMethod;
                return ctsSolutions;
            }

            throw new ArgumentException();
        }
    }
}