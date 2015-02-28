using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class FractionalSolve : SolveMethod
    {
        private bool b_hasOnlyFrac;
        private AlgebraSolver p_agSolver;

        public FractionalSolve(AlgebraSolver solver, bool hasOnlyFrac)
        {
            p_agSolver = solver;
            b_hasOnlyFrac = hasOnlyFrac;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, 
            ref TermType.EvalData pEvalData)
        {
            pEvalData.CheckSolutions = true;

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();
            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            //if (!left.Contains(solveForComp))
            //{
            //	if (Simplifier.AreEqual(left, right))
            //		return new AllSolutions();
            //	else
            //		return new NoSolutions();
            //}

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);

            List<ExComp[]> gps = left.GetGroups();

            AlgebraTerm[] leftNumDen;
            AlgebraTerm[] rightNumDen;

            if (gps.Count == 2 && right.IsZero())
            {
                AlgebraTerm gp0 = gps[0].ToAlgTerm();
                AlgebraTerm gp1 = MulOp.Negate(gps[1].ToAlgTerm()).ToAlgTerm();

                left = gp0;
                right = gp1;

                pEvalData.WorkMgr.FromSubtraction(gps[1].ToAlgTerm(), left, right);
                pEvalData.WorkMgr.FromSides(left, right);

                AlgebraTerm[] gp0NumDen = gp0.GetNumDenFrac();
                AlgebraTerm[] gp1NumDen = gp1.GetNumDenFrac();

                if (gp0NumDen == null || gp1NumDen == null)
                {
                    AlgebraTerm[] useNumDen = gp0NumDen == null ? gp1NumDen : gp0NumDen;

                    pEvalData.WorkMgr.FromSides(MulOp.StaticWeakCombine(left, useNumDen[1]), useNumDen[0], 
                        "Get rid of the denominator by multiplying both sides by it");

                    left = useNumDen[0];
                    right = MulOp.StaticCombine(right, useNumDen[1]).ToAlgTerm();

                    return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                }
                else
                {
                    // They both have numerators and denominators.
                    // Cross multiply.

                    leftNumDen = gp0NumDen;
                    rightNumDen = gp1NumDen;

                    pEvalData.WorkMgr.FromSides(MulOp.StaticWeakCombine(leftNumDen[0], rightNumDen[1]), 
                        MulOp.StaticWeakCombine(leftNumDen[1], rightNumDen[0]), "Cross multiply.");

                    left = MulOp.StaticCombine(leftNumDen[0], rightNumDen[1]).ToAlgTerm();
                    right = MulOp.StaticCombine(leftNumDen[1], rightNumDen[0]).ToAlgTerm();

                    pEvalData.WorkMgr.FromSides(left, right, "Simplify.");

                    return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                }
            }

            leftNumDen = left.GetNumDenFrac();
            rightNumDen = right.GetNumDenFrac();
            if (leftNumDen != null && rightNumDen != null)
            {
                // Cross multiply.

                pEvalData.WorkMgr.FromSides(MulOp.StaticWeakCombine(leftNumDen[0], rightNumDen[1]), MulOp.StaticWeakCombine(leftNumDen[1], rightNumDen[0]), "Cross multiply.");

                left = MulOp.StaticCombine(leftNumDen[0], rightNumDen[1]).ToAlgTerm();
                right = MulOp.StaticCombine(leftNumDen[1], rightNumDen[0]).ToAlgTerm();

                pEvalData.WorkMgr.FromSides(left, right, "Simplify.");

                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            }
            else if (leftNumDen == null && rightNumDen != null && !left.ContainsFractions())
            {
                pEvalData.WorkMgr.FromSides(MulOp.StaticWeakCombine(left, rightNumDen[1]), rightNumDen[0], 
                    "Get rid of the denominator by multiplying both sides by it");

                left = MulOp.StaticCombine(left, rightNumDen[1]).ToAlgTerm();
                right = rightNumDen[0];

                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            }
            else if (leftNumDen != null && rightNumDen == null && !right.ContainsFractions())
            {
                pEvalData.WorkMgr.FromSides(leftNumDen[0], MulOp.StaticWeakCombine(right, leftNumDen[1]), "Get rid of the denominator by multiplying both sides by it");

                right = MulOp.StaticCombine(right, leftNumDen[1]).ToAlgTerm();
                left = leftNumDen[0];

                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            }

            if (!b_hasOnlyFrac)
            {
                // Move everything to the right.
                right = right - left;
                left = new AlgebraTerm();

                right = right.RemoveRedundancies().ToAlgTerm();

                // Only have the variable fractions on the left side.
                // Move the variable fractions from the right side to the left side.
                VariableFractionsToLeft(ref left, ref right, solveForComp, ref pEvalData);
            }

            // Get the fractional groups.
            List<ExComp[]> leftGroups = left.GetGroupsNoOps();
            List<ExComp[]> rightGroups = right.GetGroupsNoOps();

            List<ExComp[]> leftDens = (from leftGroup in leftGroups
                                       select leftGroup.GetDenominator(true)).ToList();

            List<ExComp[]> rightDens = (from rightGroup in rightGroups
                                        select rightGroup.GetDenominator(true)).ToList();

            List<ExComp[]> leftNums = (from leftGroup in leftGroups
                                       select leftGroup.GetNumerator()).ToList();

            List<ExComp[]> rightNums = (from rightGroup in rightGroups
                                        select rightGroup.GetNumerator()).ToList();

            if (leftDens.Count != leftNums.Count)
                return null;
            if (rightDens.Count != rightNums.Count)
                return null;

            foreach (ExComp[] leftDen in leftDens)
            {
                for (int i = 0; i < leftDen.Length; ++i)
                {
                    if (leftDen[i] is AlgebraTerm)
                        leftDen[i] = (leftDen[i] as AlgebraTerm).FactorizeTerm(ref pEvalData);
                }
            }

            foreach (ExComp[] rightDen in rightDens)
            {
                for (int i = 0; i < rightDen.Length; ++i)
                {
                    if (rightDen[i] is AlgebraTerm)
                        rightDen[i] = (rightDen[i] as AlgebraTerm).FactorizeTerm(ref pEvalData);
                }
            }

            ExComp[] leftDensLcf = GroupHelper.LCF(leftDens);
            ExComp[] rightDensLcf = GroupHelper.LCF(rightDens);

            ExComp[] overallDensLcf = GroupHelper.LCF(leftDensLcf, rightDensLcf);

            AlgebraTerm lcfTerm = overallDensLcf.ToAlgTerm();

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "The least common denominator of all the terms is " + WorkMgr.STM + "{2}" + WorkMgr.EDM, left, right, lcfTerm);

            List<ExComp> leftTerms = new List<ExComp>();

            string overallWork = "";
            for (int i = 0; i < leftDens.Count; ++i)
            {
                ExComp[] leftDen = leftDens[i];
                AlgebraTerm leftDenTerm = leftDen.ToAlgTerm();

                ExComp mulTerm = DivOp.StaticCombine(lcfTerm.Clone(), leftDenTerm);

                string mulWork = WorkStep.FormatStr("({0})*({1})", DivOp.StaticWeakCombine(mulTerm, mulTerm),
                    DivOp.StaticWeakCombine(leftNums[i].ToAlgTerm(), leftDenTerm));

                overallWork += mulWork;

                ExComp termToAdd = MulOp.StaticCombine(mulTerm, leftNums[i].ToAlgTerm());

                leftTerms.Add(termToAdd);

                if (i != leftDens.Count - 1)
                    overallWork += "+";
            }

            lcfTerm = overallDensLcf.ToAlgTerm();

            overallWork += "=";

            // We have problems here.
            List<ExComp> rightTerms = new List<ExComp>();
            for (int i = 0; i < rightDens.Count; ++i)
            {
                ExComp[] rightDen = rightDens[i];
                AlgebraTerm rightDenTerm = rightDen.ToAlgTerm();

                ExComp mulTerm = DivOp.StaticCombine(lcfTerm.Clone(), rightDenTerm);

                string mulWork = WorkStep.FormatStr("({0})*({1})", DivOp.StaticWeakCombine(mulTerm, mulTerm),
                    DivOp.StaticWeakCombine(rightNums[i].ToAlgTerm(), rightDenTerm));

                overallWork += mulWork;

                ExComp termToAdd = MulOp.StaticCombine(mulTerm, rightNums[i].ToAlgTerm());
                rightTerms.Add(termToAdd);

                if (i != rightDens.Count - 1)
                    overallWork += "+";
            }

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + overallWork + WorkMgr.EDM, "Convert all fractions to have the same denominator so they can be combined");

            if (pEvalData.WorkMgr.AllowWork)
            {
                string simpWork = "";

                for (int i = 0; i < leftTerms.Count; ++i)
                {
                    ExComp leftTerm = leftTerms[i];
                    simpWork += WorkStep.FormatStr("{0}", DivOp.StaticWeakCombine(leftTerm, lcfTerm));

                    if (i != leftTerms.Count - 1)
                        simpWork += "+";
                }

                simpWork += "=";

                for (int i = 0; i < rightTerms.Count; ++i)
                {
                    ExComp rightTerm = rightTerms[i];
                    simpWork += WorkStep.FormatStr("{0}", DivOp.StaticWeakCombine(rightTerm, lcfTerm));

                    if (i != rightTerms.Count - 1)
                        simpWork += "+";
                }

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + simpWork + WorkMgr.EDM, "Simplify the terms");
            }

            AlgebraTerm finalLeftTerm = new AlgebraTerm();
            foreach (ExComp leftTerm in leftTerms)
            {
                ExComp[] leftTermGroup = { leftTerm };
                finalLeftTerm.AddGroup(leftTermGroup);
            }

            AlgebraTerm finalRightTerm = new AlgebraTerm();
            foreach (ExComp rightTerm in rightTerms)
            {
                ExComp[] rightTermGroup = { rightTerm };
                finalRightTerm.AddGroup(rightTermGroup);
            }

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Add the fractions together as they have equal denominators.",
                DivOp.StaticWeakCombine(finalLeftTerm, lcfTerm), DivOp.StaticWeakCombine(finalRightTerm, lcfTerm));

            finalLeftTerm = finalLeftTerm.ApplyOrderOfOperations();
            finalRightTerm = finalRightTerm.ApplyOrderOfOperations();
            ExComp finalLeft = finalLeftTerm.MakeWorkable();
            ExComp finalRight = finalRightTerm.MakeWorkable();

            pEvalData.WorkMgr.FromSides(DivOp.StaticWeakCombine(finalLeft, lcfTerm), DivOp.StaticWeakCombine(finalRight, lcfTerm), "Simplify.");

            finalLeftTerm = finalLeft.ToAlgTerm();
            finalRightTerm = finalRight.ToAlgTerm();

            pEvalData.WorkMgr.FromSides(finalLeft, finalRight, "Cancel the denominators from both sides");

            return p_agSolver.SolveEq(solveFor, finalLeftTerm, finalRightTerm, ref pEvalData);
        }
    }
}