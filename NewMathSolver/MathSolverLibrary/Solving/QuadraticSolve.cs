using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Linq;
using System.Collections.Generic;

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
            if (!ExNumber.GetOne().IsEqualTo(a))
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})/({1})=({0})/({1})" + WorkMgr.EDM, "Factor out the A term of " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " to make completing the square easier.", overall, a);

                b = DivOp.StaticCombine(b, a);
                c = DivOp.StaticCombine(c, a);
            }

            c = MulOp.Negate(c);

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            pEvalData.GetWorkMgr().FromSides(AddOp.StaticWeakCombine(PowOp.StaticCombine(solveForComp, new ExNumber(2.0)), MulOp.StaticWeakCombine(b, solveForComp)), c, "Move the C value to the other side.");

            ExComp halfB = DivOp.StaticCombine(b, new ExNumber(2.0));
            ExComp completeTheSquareTerm = PowOp.RaiseToPower(halfB, new ExNumber(2.0), ref pEvalData, false);

            pEvalData.GetWorkMgr().FromSides(AddOp.StaticWeakCombine(AddOp.StaticCombine(PowOp.StaticWeakCombine(solveForComp, new ExNumber(2.0)), MulOp.StaticWeakCombine(b, solveForComp)), completeTheSquareTerm),
                AddOp.StaticCombine(c, completeTheSquareTerm),
                "Add " + WorkMgr.STM + "(b^2)/4" + WorkMgr.EDM + " or in this case " + WorkMgr.STM +
                (completeTheSquareTerm is AlgebraTerm ? (completeTheSquareTerm as AlgebraTerm).FinalToDispStr() : completeTheSquareTerm.ToAsciiString()) +
                WorkMgr.EDM + " to both sides to complete the square by making a perfect square quadratic which can be easily factored.");

            ExComp right = AddOp.StaticCombine(c, completeTheSquareTerm);

            if (right is AlgebraTerm)
                right = (right as AlgebraTerm).CompoundFractions();

            ExComp tmpWeakLeftInner = AddOp.StaticWeakCombine(solveForComp, halfB);
            ExComp tmpWeakLeft = PowOp.StaticWeakCombine(tmpWeakLeftInner, new ExNumber(2.0));

            pEvalData.GetWorkMgr().FromSides(tmpWeakLeft, right, "Factor the left side and simplify the right side.");

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\sqrt({0})=\\sqrt({1})" + WorkMgr.EDM, "Take the square root of both sides.", tmpWeakLeft, right);

            AlgebraTermArray solutions = PowOp.TakeSqrt(right, ref pEvalData);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}=\\pm({1})" + WorkMgr.EDM, "The square root has positive and negative roots. Both " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM +
                " and " + WorkMgr.STM + "{0}={2}" + WorkMgr.EDM, tmpWeakLeftInner, solutions.GetItem(0), solutions.GetItem(1));

            for (int i = 0; i < solutions.GetTermCount(); ++i)
            {
                if (!ExNumber.GetZero().IsEqualTo(halfB))
                {
                    pEvalData.GetWorkMgr().FromSubtraction(halfB, tmpWeakLeftInner, solutions.GetItem(i));
                }

                solutions.GetTerms()[i] = SubOp.StaticCombine(solutions.GetItem(i), halfB).ToAlgTerm();
                solutions.GetTerms()[i].ReduceFracs();
                solutions.GetTerms()[i] = solutions.GetTerms()[i].CompoundFractions();

                pEvalData.GetWorkMgr().FromSides(solveForComp, solutions.GetItem(i), "Simplify.");
            }

            return solutions;
        }

        public ExComp Factor(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            AlgebraTerm[] factors = MathSolverLibrary.Equation.Term.AdvAlgebraTerm.Factorize(a, b, c, solveFor, ref pEvalData, pEvalData.GetWorkMgr().GetAllowWork());
            if (factors == null)
                return null;

            AlgebraTerm zeroTerm = ExNumber.GetZero().ToAlgTerm();

            ExComp solution1 = p_agSolver.SolveEq(solveFor, factors[0], zeroTerm, ref pEvalData);
            ExComp solution2 = p_agSolver.SolveEq(solveFor, factors[1], zeroTerm, ref pEvalData);

            AlgebraTermArray termArray = new AlgebraTermArray(solution1.ToAlgTerm(), solution2.ToAlgTerm());

            return termArray;
        }

        public ExComp QuadraticFormulaSolve(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{3}=(-({1})\\pm\\sqrt(({1})^2-4({0})({2})))/(2({0}))" + WorkMgr.EDM,
                "Plug values into the quadratic equation.", a, b, c, solveForComp);

            ExComp term1 = MulOp.Negate(b);

            ExComp term2_1 = PowOp.RaiseToPower(b, new ExNumber(2.0), ref pEvalData, false);

            ExComp term2_2 = MulOp.StaticCombine(a, c);
            term2_2 = MulOp.StaticCombine(term2_2, new ExNumber(4.0));
            ExComp term2 = SubOp.StaticCombine(term2_1, term2_2);
            AlgebraTermArray term2s = PowOp.TakeSqrt(term2, ref pEvalData);
            ExComp term3 = MulOp.StaticCombine(new ExNumber(2.0), a);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{3}=({0}\\pm({1}))/({2})" + WorkMgr.EDM, "Simplify.", term1, term2s.GetItem(0), term3, solveForComp);

            for (int i = 0; i < term2s.GetTermCount(); ++i)
            {
                ExComp sqrtTerm = term2s.GetItem(i);
                term2s.GetTerms()[i] = AddOp.StaticCombine(term1.CloneEx(), term2s.GetItem(i).CloneEx()).ToAlgTerm();
                term2s.GetTerms()[i] = DivOp.StaticCombine(term2s.GetItem(i), term3).ToAlgTerm();

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{4}=({0}+{1})/({2})={3}" + WorkMgr.EDM, "The above term is a root of the quadratic.", term1, sqrtTerm, term3, term2s.GetTerms()[i], solveForComp);
            }

            return term2s;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            left = AdvAlgebraTerm.EvaluatePowers(left, ref pEvalData);
            right = AdvAlgebraTerm.EvaluatePowers(right, ref pEvalData);

            ConstantsToRight(ref right, ref left, solveForComp, ref pEvalData);
            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            left = left.RemoveRedundancies(false).ToAlgTerm();

            List<ExComp> powersOfVar = left.GetPowersOfVar(solveForComp);

            if (!ObjectHelper.ContainsEx(powersOfVar, new ExNumber(2.0)) || !ObjectHelper.ContainsEx(powersOfVar, new ExNumber(1.0)))
                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);

            left = AdvAlgebraTerm.EvaluateExponentsCompletely(left).ToAlgTerm();

            List<ExComp[]> squaredGroups = left.GetGroupContainingTerm(solveForComp.ToPow(2.0));
            List<ExComp[]> linearGroups = left.GetGroupContainingTerm(solveForComp);
            List<AlgebraGroup> constantGroup = left.GetGroupsConstantTo(solveForComp);

            AlgebraTerm[] aTerms = new AlgebraTerm[squaredGroups.Count];
            for (int i = 0; i < squaredGroups.Count; ++i)
                aTerms[i] = GroupHelper.ToAlgTerm(GroupHelper.GetUnrelatableTermsOfGroup(squaredGroups[i], solveForComp));

            AlgebraTerm[] bTerms = new AlgebraTerm[linearGroups.Count];
            for (int i = 0; i < linearGroups.Count; ++i)
                bTerms[i] = GroupHelper.ToAlgTerm(GroupHelper.GetUnrelatableTermsOfGroup(linearGroups[i], solveForComp));

            AlgebraTerm a = new AlgebraTerm();
            foreach (AlgebraTerm aTerm in aTerms)
            {
                a = AlgebraTerm.OpAdd(a, aTerm);
            }
            if (a.GetTermCount() == 0)
                a = ExNumber.GetZero().ToAlgTerm();

            AlgebraTerm b = new AlgebraTerm();
            foreach (AlgebraTerm bTerm in bTerms)
            {
                b = AlgebraTerm.OpAdd(b, bTerm);
            }
            if (b.GetTermCount() == 0)
                b = ExNumber.GetZero().ToAlgTerm();

            AlgebraTerm c = new AlgebraTerm(constantGroup.ToArray());
            if (b.GetTermCount() == 0)
                b = ExNumber.GetZero().ToAlgTerm();

            int leftGroupCount = left.GetGroupCount();
            if (leftGroupCount != 3)
            {
                if (leftGroupCount == 2)
                {
                    // Either solve this with a power method or factor out the solve variable.

                    if (!c.IsZero())
                    {
                        // We have a power solve.
                        PowerSolve powSolve = new PowerSolve(p_agSolver, new ExNumber(2.0));
                        ExComp powSolveResult = powSolve.SolveEquation(left, right, solveFor, ref pEvalData);
                        return powSolveResult;
                    }
                    else
                    {
                        // Factor out solve variable. In the case... (ax^2+bx=0)
                        AlgebraTerm toLinearSolve = new AlgebraTerm(a, new MulOp(), solveForComp, new AddOp(), b);
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}({1})={2}" + WorkMgr.EDM, "Factor out " + WorkMgr.STM + "{0}" + WorkMgr.EDM + " from the term.", solveForComp, toLinearSolve, right);
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}({1})={2}" + WorkMgr.EDM, "Solve for each of the factors independently.", solveForComp, toLinearSolve, right);
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}=0" + WorkMgr.EDM, solveForComp);

                        LinearSolve linearSolve = new LinearSolve(p_agSolver, 0);

                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}=0" + WorkMgr.EDM, toLinearSolve);

                        ExComp linearSolved = linearSolve.SolveEquation(toLinearSolve, ExNumber.GetZero().ToAlgTerm(), solveFor, ref pEvalData);
                        AlgebraTermArray ata = new AlgebraTermArray(ExNumber.GetZero().ToAlgTerm(), linearSolved.ToAlgTerm());
                        return ata;
                    }
                }
            }

            ExComp exA = a.RemoveRedundancies(false);
            ExComp exB = b.RemoveRedundancies(false);
            ExComp exC = c.RemoveRedundancies(false);

            QuadraticSolveMethod originalSolveMethod = pEvalData.GetQuadSolveMethod();
            if (pEvalData.GetQuadSolveMethod() == QuadraticSolveMethod.Factor)
            {
                pEvalData.AttemptSetInputType(TermType.InputType.SolveQuadsFactor);

                pEvalData.GetWorkMgr().FromSides(left, ExNumber.GetZero(), "Solve this quadratic equation by factoring.");
                ExComp factorSolutions = Factor(exA, exB, exC, solveFor, ref pEvalData);
                // Null is returned on factoring not working.
                if (factorSolutions != null)
                    return factorSolutions;
                pEvalData.GetWorkMgr().PopStep();
                pEvalData.GetWorkMgr().FromSides(left, ExNumber.GetZero(), "The quadratic cannot be factored.");
                pEvalData.SetQuadSolveMethod(QuadraticSolveMethod.Formula);
            }
            if (pEvalData.GetQuadSolveMethod() == QuadraticSolveMethod.Formula)
            {
                pEvalData.AttemptSetInputType(TermType.InputType.SolveQuadsQE);

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}=0" + WorkMgr.EDM, "Use the quadratic equation " + WorkMgr.STM + "{4}=(-b&sqrt(b^2-4ac))/(2a)" + WorkMgr.EDM +
                    " where " + WorkMgr.STM + "a={1}" + WorkMgr.EDM + ", " + WorkMgr.STM + "b={2}" + WorkMgr.EDM + ", and " + WorkMgr.STM + "c={3}" + WorkMgr.EDM, left, exA, exB, exC, solveForComp);

                ExComp qfSolutions = QuadraticFormulaSolve(exA, exB, exC, solveFor, ref pEvalData);
                // Restore the selected solve method.
                pEvalData.SetQuadSolveMethod(originalSolveMethod);
                return qfSolutions;
            }
            else if (pEvalData.GetQuadSolveMethod() == QuadraticSolveMethod.CompleteSquare)
            {
                pEvalData.AttemptSetInputType(TermType.InputType.SolveQuadsCTS);

                pEvalData.GetWorkMgr().FromSides(left, ExNumber.GetZero(), "Solve this quadratic equation by completing the square.");

                ExComp ctsSolutions = CompleteTheSquare(exA, exB, exC, solveFor, left, ref pEvalData);
                // Restore the selected solve method.
                pEvalData.SetQuadSolveMethod(originalSolveMethod);
                return ctsSolutions;
            }

            throw new ArgumentException();
        }
    }
}