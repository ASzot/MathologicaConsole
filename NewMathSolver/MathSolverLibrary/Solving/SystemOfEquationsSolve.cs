﻿using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;
using MathSolverWebsite.MathSolverLibrary.TermType;

//TODO:
// Make sure no inequalities are used in systems of equations.

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    public enum EquationSystemSolveMethod
    {
        Substitution,
        Elimination,
    };

    internal class EquationSystemSolve
    {
        private const int MAX_EQ_COUNT = 3;

        private List<string> _solveFors;
        private EquationSystemSolveMethod _solveMethod = EquationSystemSolveMethod.Substitution;
        private AlgebraSolver p_agSolver;

        public void SetSolveFors(List<string> value)
        {
            _solveFors = value;
        }

        public void SetSolvingMethod(EquationSystemSolveMethod value)
        {
            _solveMethod = value;
        }

        public EquationSystemSolveMethod GetSolvingMethod()
        {
            return _solveMethod;
        }

        public EquationSystemSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public static bool DoAssignments(ref List<EqSet> equations)
        {
            if (equations.Count == 1)
                return false;

            // Go through all the equations and if there is something like x=2 do that assignment.
            // Then remove the equation set and do the substitution for all the other equations.

            bool anySubbed = false;

            for (int i = 0; i < equations.Count; ++i)
            {
                EqSet eqSet = equations[i];
                if (eqSet.GetSides().Count != 2)
                    continue;

                if (eqSet.GetLeft() is AlgebraComp || eqSet.GetRight() is AlgebraComp)
                {
                    ExComp other = eqSet.GetLeft() is AlgebraComp ? eqSet.GetRight() : eqSet.GetLeft();

                    //TODO:
                    // Allow more complex assignments other than just numbers.
                    if (other is ExNumber)
                    {
                        AlgebraComp varFor = eqSet.GetLeft() is AlgebraComp ? eqSet.GetLeft() as AlgebraComp : eqSet.GetRight() as AlgebraComp;

                        ArrayFunc.RemoveIndex(equations, i--);

                        for (int j = 0; j < equations.Count; ++j)
                        {
                            equations[j].Substitute(varFor, other);
                        }

                        anySubbed = true;
                    }
                }
            }

            return anySubbed;
        }

        public SolveResult SolveEquationArray(List<EqSet> equations, List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>> lexemeTables, Dictionary<string, int> allIdens, ref TermType.EvalData pEvalData)
        {
            DoAssignments(ref equations);

            if (equations.Count > MAX_EQ_COUNT)
                return SolveResult.Failure();

            if (_solveMethod == EquationSystemSolveMethod.Substitution)
            {
                if (equations.Count == 2)
                    pEvalData.AttemptSetInputType(TermType.InputType.SOE_Sub_2Var);
                else if (equations.Count == 3)
                    pEvalData.AttemptSetInputType(TermType.InputType.SOE_Sub_3Var);

                SolveResult solvedSub = SolveEquationArraySubstitution(equations, lexemeTables, ref pEvalData);
                return solvedSub;
            }
            else if (_solveMethod == EquationSystemSolveMethod.Elimination)
            {
                if (equations.Count == 2)
                    pEvalData.AttemptSetInputType(TermType.InputType.SOE_Elim_2Var);
                else if (equations.Count == 3)
                    pEvalData.AttemptSetInputType(TermType.InputType.SOE_Elim_3Var);

                SolveResult solvedElim = SolveEquationArrayElimination(equations, allIdens, ref pEvalData);
                return solvedElim;
            }
            else
                throw new ArgumentException("Solve method is an impossible type.");
        }

        private EqSet? CombineEquations(AlgebraVar eliminate, EqSet eq0, EqSet eq1, ref TermType.EvalData pEvalData)
        {
            AlgebraComp elimAgComp = eliminate.ToAlgebraComp();

            AlgebraTerm eq0Left = eq0.GetLeftTerm();
            AlgebraTerm eq0Right = eq0.GetRightTerm();

            AlgebraTerm eq1Left = eq1.GetLeftTerm();
            AlgebraTerm eq1Right = eq1.GetRightTerm();

            SolveMethod.VariablesToLeft(ref eq0Left, ref eq0Right, elimAgComp, ref pEvalData);
            SolveMethod.VariablesToLeft(ref eq1Left, ref eq1Right, elimAgComp, ref pEvalData);

            ExComp elimCoeff0 = eq0Left.GetCoeffOfVar(elimAgComp);
            ExComp elimCoeff1 = eq1Left.GetCoeffOfVar(elimAgComp);

            if (elimCoeff0 == null || elimCoeff1 == null)
            {
                pEvalData.AddFailureMsg("Couldn't solve system of equations.");
                return null;
            }

            if (ExNumber.GetZero().IsEqualTo(elimCoeff0) || (elimCoeff0 is AlgebraTerm && (elimCoeff0 as AlgebraTerm).IsZero()))
                elimCoeff0 = ExNumber.GetOne();
            if (ExNumber.GetZero().IsEqualTo(elimCoeff1) || (elimCoeff1 is AlgebraTerm && (elimCoeff1 as AlgebraTerm).IsZero()))
                elimCoeff1 = ExNumber.GetOne();

            ExComp gcf = GroupHelper.LCF(elimCoeff0, elimCoeff1);

            ExComp mul0 = DivOp.StaticCombine(gcf, elimCoeff0);
            ExComp mul1 = DivOp.StaticCombine(gcf, elimCoeff1);

            mul0 = AbsValFunction.MakePositive(mul0);
            mul1 = AbsValFunction.MakePositive(mul1);

            if (pEvalData.GetWorkMgr().GetAllowWork() && (!ExNumber.GetOne().IsEqualTo(mul0) || !ExNumber.GetOne().IsEqualTo(mul1)))
            {
                ExComp tmpEq0Left = ExNumber.GetOne().IsEqualTo(mul0) ? eq0Left : MulOp.StaticWeakCombine(mul0, eq0Left);
                ExComp tmpEq0Right = ExNumber.GetOne().IsEqualTo(mul0) ? eq0Right : MulOp.StaticWeakCombine(mul0, eq0Right);

                ExComp tmpEq1Left = ExNumber.GetOne().IsEqualTo(mul1) ? eq1Left : MulOp.StaticWeakCombine(mul1, eq1Left);
                ExComp tmpEq1Right = ExNumber.GetOne().IsEqualTo(mul1) ? eq1Right : MulOp.StaticWeakCombine(mul1, eq1Right);

                pEvalData.GetWorkMgr().FromArraySides("Eliminate the variable " + elimAgComp.ToAsciiString() + ". To do so make the variable in the equations have equal coefficients.",
                    tmpEq0Left, tmpEq0Right,
                    tmpEq1Left, tmpEq1Right);
            }

            eq0Left = MulOp.StaticCombine(mul0, eq0Left).ToAlgTerm();
            eq0Right = MulOp.StaticCombine(mul0, eq0Right).ToAlgTerm();

            eq1Left = MulOp.StaticCombine(mul1, eq1Left).ToAlgTerm();
            eq1Right = MulOp.StaticCombine(mul1, eq1Right).ToAlgTerm();

            if (pEvalData.GetWorkMgr().GetAllowWork() && (!ExNumber.GetOne().IsEqualTo(mul0) || !ExNumber.GetOne().IsEqualTo(mul1)))
            {
                pEvalData.GetWorkMgr().FromArraySides(eq0Left, eq0Right,
                    eq1Left, eq1Right);
            }

            AgOp combineOp;
            elimCoeff0 = MulOp.StaticCombine(mul0, elimCoeff0);
            elimCoeff1 = MulOp.StaticCombine(mul1, elimCoeff1);

            AlgebraTerm added = AddOp.StaticCombine(elimCoeff0, elimCoeff1).ToAlgTerm();
            if (added.IsZero())
                combineOp = new AddOp();
            else
                combineOp = new SubOp();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM + "</br>" + WorkMgr.STM + "{4}({2}={3})" + WorkMgr.EDM,
                ((combineOp is AddOp) ? "Add" : "Subtract") + " the equations eliminating " + WorkMgr.STM + elimAgComp.ToAsciiString() + WorkMgr.EDM + " from the resulting equation.",
                eq0Left, eq0Right, eq1Left, eq1Right, combineOp);

            AlgebraTerm finalLeft = combineOp.Combine(eq0Left, eq1Left).ToAlgTerm();
            AlgebraTerm finalRight = combineOp.Combine(eq0Right, eq1Right).ToAlgTerm();

            if (finalLeft.Contains(elimAgComp))
                return null;

            pEvalData.GetWorkMgr().FromSides(finalLeft, finalRight, "The resulting combined equation.");

            return new EqSet(finalLeft, finalRight, Parsing.LexemeType.EqualsOp);
        }

        private void DoSub(int index, ref List<EqSet> completeEqs, ref TermType.EvalData pEvalData, AlgebraComp solveForComp, ExComp result)
        {
            AlgebraTerm data1 = completeEqs[index].GetLeftTerm();
            AlgebraTerm data2 = completeEqs[index].GetRightTerm();

            pEvalData.GetWorkMgr().FromSides(data1, data2,
                "Substitute " + WorkMgr.STM +
                solveForComp.ToAsciiString() + "=" +
                (result is AlgebraTerm ? (result as AlgebraTerm).FinalToDispStr() : result.ToAsciiString()) +
                WorkMgr.EDM + " into the above equation. <i>(EQ" + (index + 1).ToString() + ")</i>");

            data1 = data1.Substitute(solveForComp, result.CloneEx());
            data2 = data2.Substitute(solveForComp, result.CloneEx());

            pEvalData.GetWorkMgr().FromSides(data1, data2);

            pEvalData.SetIsWorkable(false);
            SolveMethod.PrepareForSolving(ref data1, ref data2, ref pEvalData);

            pEvalData.GetWorkMgr().FromSides(data1, data2, "Simplify.");

            completeEqs[index].SetLeft(data1);
            completeEqs[index].SetRight(data2);
        }

        private AlgebraVar GetLowestComplexityVar(AlgebraTerm left, AlgebraTerm right)
        {
            List<string> leftIdens = left.GetAllAlgebraCompsStr();
            List<string> rightIdens = right.GetAllAlgebraCompsStr();
            List<string> allIdens = new List<string>();
            allIdens.AddRange(leftIdens);
            allIdens.AddRange(rightIdens);

            allIdens = allIdens.Distinct().ToList();

            List<TypePair<AlgebraComp, int>> eqVarComplexity = new List<TypePair<AlgebraComp, int>>();
            foreach (string iden in allIdens)
            {
                AlgebraComp varFor = new AlgebraComp(iden);

                if (_solveFors != null && !_solveFors.Contains(iden))
                    continue;

                int complexity0 = left.GetComplexityOfVar(varFor);
                int complexity1 = right.GetComplexityOfVar(varFor);
                eqVarComplexity.Add(new TypePair<AlgebraComp, int>(varFor, complexity0 + complexity1));
            }

            int lowestComplexity = int.MaxValue;
            AlgebraComp lowestComplexityVar = null;
            // Find the lowest complexity var that hasn't already been choosen as a solve variable.
            foreach (TypePair<AlgebraComp, int> complexity in eqVarComplexity)
            {
                if (complexity.GetData2() < lowestComplexity)
                {
                    lowestComplexity = complexity.GetData2();
                    lowestComplexityVar = complexity.GetData1();
                }
            }

            if (lowestComplexityVar == null)
                return AlgebraVar.GetGarbageVar();

            return lowestComplexityVar.GetVar();
        }

        private bool SolveEliminationRecur(List<EqSet> equations, AlgebraSolver agSolver, AlgebraVar eliminate, ref List<EqSet> iterGenerations, ref TermType.EvalData pEvalData)
        {
            iterGenerations.Add(equations[0]);

            if (equations.Count == 1)
                return true;

            List<EqSet> finalEquationSets = new List<EqSet>();
            for (int i = 1; i < equations.Count; ++i)
            {
                EqSet eq0 = equations[i - 1];
                EqSet eq1 = equations[i];

                EqSet? combined = CombineEquations(eliminate, eq0, eq1, ref pEvalData);
                if (!combined.HasValue)
                {
                    pEvalData.AddFailureMsg("Error on combining system of equations.");
                    return false;
                }

                finalEquationSets.Add(combined.Value);
            }

            if (finalEquationSets.Count > 1)
                eliminate = GetLowestComplexityVar(finalEquationSets[0].GetLeftTerm(), finalEquationSets[0].GetRightTerm());

            bool result = SolveEliminationRecur(finalEquationSets, agSolver, eliminate, ref iterGenerations, ref pEvalData);
            return result;
        }

        private SolveResult SolveEquationArrayElimination(List<EqSet> equations, Dictionary<string, int> allIdens, ref TermType.EvalData pEvalData)
        {
            p_agSolver.CreateUSubTable(allIdens);

            AlgebraVar eliminate = GetLowestComplexityVar(equations[0].GetLeftTerm(), equations[0].GetRightTerm());

            if (pEvalData.GetWorkMgr().GetAllowWork())
            {
                List<AlgebraTerm> sides = EqSet.GetSides(equations).ToList();
                string finalStr = "";
                for (int i = 0; i < sides.Count; i += 2)
                {
                    finalStr += WorkMgr.STM + "{" + i.ToString() + "}={" + (i + 1).ToString() + "}" + WorkMgr.EDM + " <br />";
                }

                pEvalData.GetWorkMgr().FromFormatted(finalStr, "Solve this system of equations by elimination.", sides.ToArray());
            }

            List<EqSet> iterGenerations = new List<EqSet>();

            if (!SolveEliminationRecur(equations, p_agSolver, eliminate, ref iterGenerations, ref pEvalData))
                return SolveResult.Failure();

            SolveResult solveResult = SolveResult.Solved();
            solveResult.Solutions = new List<Solution>();

            for (int i = iterGenerations.Count - 1; i >= 0; --i)
            {
                EqSet eqSet = iterGenerations[i];

                AlgebraVar solveFor = GetLowestComplexityVar(eqSet.GetLeftTerm(), eqSet.GetRightTerm());
                if (solveFor.IsGarbage())
                {
                    return SolveResult.Simplified(new NoSolutions());
                }

                pEvalData.GetWorkMgr().FromSides(eqSet.GetLeft(), eqSet.GetRight(), "Solve for " + WorkMgr.STM + solveFor.ToMathAsciiString() + WorkMgr.EDM + " in this equation.");

                ExComp result = p_agSolver.SolveEq(solveFor, eqSet.GetLeftTerm(), eqSet.GetRightTerm(), ref pEvalData, true);
                if (result == null)
                    return SolveResult.Failure();
                if (result is AlgebraTermArray || result is GeneralSolution || result is NoSolutions || result is AllSolutions)
                {
                    pEvalData.AddFailureMsg("Can't solve equations due to the complexity of the equations. The ability to solve this type of problem might be added sometime in the future.");
                    return SolveResult.Failure();
                }

                AlgebraComp solveForComp = solveFor.ToAlgebraComp();

                if (iterGenerations.Count > 1)
                    pEvalData.GetWorkMgr().FromSides(solveForComp, result, "Using the solved equation substitute the result into the other equations.");

                solveResult.Solutions.Add(new Solution(solveForComp, result.CloneEx()));

                ArrayFunc.RemoveIndex(iterGenerations, i);

                foreach (EqSet subInEqSet in iterGenerations)
                {
                    pEvalData.GetWorkMgr().FromSides(subInEqSet.GetLeft(), subInEqSet.GetRight(),
                        "Substitute in " + WorkMgr.STM + solveForComp.ToAsciiString() + "=" + result.ToAsciiString() + WorkMgr.EDM + " into this equation.");

                    AlgebraTerm leftSub = subInEqSet.GetLeftTerm().Substitute(solveForComp, result);
                    AlgebraTerm rightSub = subInEqSet.GetRightTerm().Substitute(solveForComp, result);

                    leftSub = leftSub.ApplyOrderOfOperations();
                    rightSub = rightSub.ApplyOrderOfOperations();

                    subInEqSet.SetLeft(leftSub.MakeWorkable());
                    subInEqSet.SetRight(rightSub.MakeWorkable());

                    pEvalData.GetWorkMgr().FromSides(subInEqSet.GetLeft(), subInEqSet.GetRight(), "Substitute in and simplify.");
                }
            }

            return solveResult;
        }

        private SolveResult SolveEquationArraySubstitution(List<EqSet> equations, List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>> lexemeTables, ref TermType.EvalData pEvalData)
        {
            List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>> eqLexemeTables = new List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>>();
            for (int i = 0; i < lexemeTables.Count; i += 2)
            {
                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt0 = lexemeTables[i];
                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt1 = lexemeTables[i + 1];

                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> compoundedLexemeTable = Parsing.LexicalParser.CompoundLexemeTable(lt0, lt1);
                eqLexemeTables.Add(compoundedLexemeTable);
            }

            for (int i = 0; i < equations.Count; i += 2)
            {
                // So we ensure our terms are made workable.
                pEvalData.SetIsWorkable(false);
                AlgebraTerm left = equations[i].GetLeft().ToAlgTerm();
                AlgebraTerm right = equations[i].GetRight().ToAlgTerm();

                SolveMethod.PrepareForSolving(ref left, ref right, ref pEvalData);

                equations[i].SetLeft(left);
                equations[i].SetRight(right);
            }

            SolveResult result = SolveEquationSystemRecur(equations, eqLexemeTables, ref pEvalData, true);
            return result;
        }

        private SolveResult SolveEquationSystemRecur(List<EqSet> completeEqs, List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>> eqLexemeTables, ref EvalData pEvalData, bool ascending)
        {

            // Really this whole ascending thing really doesn't do much.
            int startVal;
            int endVal;

            if (ascending)
            {
                startVal = 0;
                endVal = completeEqs.Count - 1;
            }
            else
            {
                startVal = completeEqs.Count - 1;
                endVal = 0;
            }

            if (pEvalData.GetWorkMgr().GetAllowWork())
            {
                string idensDisp = "";
                for (int j = 0; j < completeEqs.Count; ++j)
                {
                    idensDisp += (WorkMgr.STM + completeEqs[j].FinalToDispStr() + WorkMgr.EDM + " <i>(EQ" + (j + 1).ToString() + ")</i>");
                    if (j != completeEqs.Count - 1)
                        idensDisp += "<br />";
                }

                pEvalData.GetWorkMgr().FromFormatted(idensDisp, "The equations will be referred to by the identifier in parentheses.");
            }

            int i = startVal;
            while (true)
            {
                if (ascending && i > endVal)
                    break;
                else if (!ascending && i < endVal)
                    break;

                AlgebraTerm term0 = completeEqs[i].GetLeftTerm();
                AlgebraTerm term1 = completeEqs[i].GetRightTerm();

                AlgebraSolver agSolver = new AlgebraSolver();
                AlgebraVar solveFor = GetLowestComplexityVar(term0, term1);
                if (solveFor.IsGarbage() && ascending)
                {
                    return SolveResult.Simplified(new NoSolutions());
                }

                string eqIdStr = " <i>(EQ" + (i + 1).ToString() + ")</i>";

                agSolver.CreateUSubTable(eqLexemeTables[i]);
                AlgebraComp solveForComp = solveFor.ToAlgebraComp();

                pEvalData.GetWorkMgr().FromSides(term0, term1, "Solve this equation" + eqIdStr + " for " + WorkMgr.STM + solveForComp.ToAsciiString() + WorkMgr.EDM);

                pEvalData.GetWorkMgr().SetWorkLabel(eqIdStr);
                ExComp result = agSolver.SolveEq(solveFor, term0, term1, ref pEvalData, false);
                pEvalData.GetWorkMgr().SetWorkLabel(null);

                // I have not really though about how this would work with three systems of equations. So for now I will just keep it at two.
                if (result is SpecialSolution && completeEqs.Count == 2)
                {
                    pEvalData.AddMsg("The lines are parallel.");
                    return SolveResult.NoSolutions();
                }

                if (result is AlgebraTermArray || result is GeneralSolution || result is NoSolutions || result is AllSolutions)
                {
                    pEvalData.AddFailureMsg("Can't solve equations due to the complexity of the equations. The ability to solve this type of problem might be added sometime in the future.");
                    return SolveResult.Failure();
                }

                completeEqs[i].SetLeft(solveForComp.ToAlgTerm());
                completeEqs[i].SetRight(result.ToAlgTerm());

                // Substitute the result into the next equations.
                if (ascending)
                {
                    for (int j = i + 1; j < completeEqs.Count; ++j)
                        DoSub(j, ref completeEqs, ref pEvalData, solveForComp, result);
                }
                else
                {
                    for (int j = 0; j < i; ++j)
                        DoSub(j, ref completeEqs, ref pEvalData, solveForComp, result);
                }

                if (ascending)
                    ++i;
                else
                    --i;
            }

            SolveResult solveResult = SubstituteInResults(completeEqs, ref pEvalData);
            return solveResult;
        }

        private SolveResult SubstituteInResults(List<EqSet> completeEqs, ref TermType.EvalData pEvalData)
        {
            SolveResult solveResult;
            solveResult.Solutions = new List<Solution>();
            solveResult.Success = true;
            solveResult.Restrictions = null;

            for (int i = completeEqs.Count - 1; i >= 0; --i)
            {
                AlgebraTerm term0 = completeEqs[i].GetLeftTerm();
                AlgebraTerm term1 = completeEqs[i].GetRightTerm();

                ExComp ex0 = term0.RemoveRedundancies(false);
                if (!(ex0 is AlgebraComp))
                {
                    pEvalData.AddFailureMsg("In solving systems of equations one of the solved terms didn't only contain the variable.");
                    return SolveResult.Failure();
                }

                AlgebraComp varFor = ex0 as AlgebraComp;

                solveResult.Solutions.Add(new Solution(varFor, term1));

                if (i == 0)
                    continue;

                for (int j = i - 1; j >= 0; --j)
                {
                    AlgebraTerm subTerm1 = completeEqs[j].GetRightTerm();

                    pEvalData.GetWorkMgr().FromSides(completeEqs[j].GetLeft(), subTerm1, "Substitute " + WorkMgr.STM + varFor.ToAsciiString() + "=" + term1.FinalToDispStr() + WorkMgr.EDM + " into the above equation.");
                    AlgebraTerm tmpRight = subTerm1.Substitute(varFor, term1);
                    SolveMethod.EvaluateEntirely(ref tmpRight);
                    completeEqs[j].SetRight(tmpRight);
                    pEvalData.GetWorkMgr().FromSides(completeEqs[j].GetLeft(), completeEqs[j].GetRight(), "Substitute in and simplify.");
                }

                AlgebraTerm before = completeEqs[i - 1].GetRightTerm();
                SolveMethod.EvaluateEntirely(ref before);

                before = before.CompoundFractions();

                pEvalData.GetWorkMgr().FromSides(completeEqs[i - 1].GetLeft(), before, "Simplify, getting a solution for one of the variables.");

                completeEqs[i - 1].SetRight(before);
            }

            return solveResult;
        }
    }
}