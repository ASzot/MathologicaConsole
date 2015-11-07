using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal abstract class SolveMethod
    {
        public static void CombineFractions(ref AlgebraTerm left, ref AlgebraTerm right, ref TermType.EvalData pEvalData)
        {
            bool fracCombine0 = false;
            if (left != null)
            {
                left = left.CompoundFractions(out fracCombine0);
                left = left.RemoveRedundancies(false).ToAlgTerm();
            }

            bool fracCombine1 = false;
            if (right != null)
            {
                right = right.CompoundFractions(out fracCombine1);
                right = right.RemoveRedundancies(false).ToAlgTerm();
            }

            if (fracCombine0 || fracCombine1)
            {
                pEvalData.GetWorkMgr().FromSides(left, right, "Combine fractions");
            }
        }

        public static void ConstantsToRight(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            ConstantsToRight(ref left, ref right, new AlgebraComp[] { solveForComp }, ref pEvalData);
        }

        public static void ConstantsToRight(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp[] solveForComps, ref TermType.EvalData pEvalData)
        {
            // Only find the groups that are constant to all the comps.
            List<AlgebraGroup> constantGroupsLeft = null;
            for (int i = 0; i < solveForComps.Length; ++i)
            {
                List<AlgebraGroup> gps = left.GetGroupsConstantTo(solveForComps[i]);
                if (constantGroupsLeft == null)
                    constantGroupsLeft = gps;
                else
                {
                    // Intersect with all of the gps.
                    for (int j = 0; j < constantGroupsLeft.Count; ++j)
                    {
                        AlgebraGroup gp = constantGroupsLeft[j];
                        bool matchFound = false;
                        for (int k = 0; k < gps.Count; ++k)
                        {
                            if (gp.IsEqualTo(gps[k]))
                            {
                                matchFound = true;
                                break;
                            }
                        }

                        if (!matchFound)
                            ArrayFunc.RemoveIndex(constantGroupsLeft, j--);
                    }
                }

                if (constantGroupsLeft.Count == 0)
                    break;
            }

            if (constantGroupsLeft == null || (constantGroupsLeft.Count == 1 && constantGroupsLeft[0].IsZero()))
                return;

            if (constantGroupsLeft.Count != 0)
                pEvalData.GetWorkMgr().FromAlgGpSubtraction(constantGroupsLeft, left, right);

            bool displaySimpStep = constantGroupsLeft.Count != 0 && !(constantGroupsLeft.Count == 1 && constantGroupsLeft[0].IsZero());

            foreach (AlgebraGroup constantGroup in constantGroupsLeft)
            {
                ExComp subTerm = constantGroup.ToTerm().CloneEx();
                left = Equation.Operators.SubOp.StaticCombine(left, subTerm).ToAlgTerm();
                right = Equation.Operators.SubOp.StaticCombine(right, subTerm).ToAlgTerm();
            }

            left = left.RemoveZeros().RemoveRedundancies(false).ToAlgTerm();
            right = right.RemoveZeros().RemoveRedundancies(false).ToAlgTerm();

            if (right.GetTermCount() == 0)
                right = ExNumber.GetZero().ToAlgTerm();

            if (left.GetTermCount() == 0)
                left = ExNumber.GetZero().ToAlgTerm();

            if (displaySimpStep)
                pEvalData.GetWorkMgr().FromSides(left, right, "Simplify");
        }

        /// <summary>
        /// Divide by the coefficients of a variable.
        /// Factoring is performed automatically.
        /// Strong divide refers the breaking an exponent up to divide by the exponent.
        /// For instance, e^(xy) will be split to e^y*e^x with strong divide.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="solveForComp"></param>
        /// <param name="pEvalData"></param>
        /// <param name="strongDivide"></param>
        public static void DivideByVariableCoeffs(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref EvalData pEvalData, bool strongDivide)
        {
            if (right.GetSubComps().Count == 0)
                right.Add(ExNumber.GetZero());
            if (left.GetTermCount() != 1)
            {
                List<ExComp[]> groups = left.GetGroupsNoOps();

                ExComp[][] unrelatedGroupsArr = new ExComp[groups.Count][];
                for (int i = 0; i < groups.Count; ++i)
                    unrelatedGroupsArr[i] = GroupHelper.GetUnrelatableTermsOfGroup(groups[i], solveForComp);

                // Combine all of the unrelated terms.
                AlgebraTerm factoredOutUnrelatedTerm = new AlgebraTerm(unrelatedGroupsArr);

                bool displayFactoringWork = false;
                if (factoredOutUnrelatedTerm.GetGroupCount() > 1)
                {
                    factoredOutUnrelatedTerm = factoredOutUnrelatedTerm.ApplyOrderOfOperations();
                    factoredOutUnrelatedTerm = factoredOutUnrelatedTerm.MakeWorkable().ToAlgTerm();
                    displayFactoringWork = true;
                }

                if (factoredOutUnrelatedTerm.IsZero() || factoredOutUnrelatedTerm.IsOne())
                    return;

                //if (displayFactoringWork)
                //{
                //    WorkMgr.FromFormatted(WorkMgr.STM + "({0})*{1}={2}" + WorkMgr.EDM, "Factor out the variable from the expression.", factoredOutUnrelatedTerm, solveForComp, right);
                //}

                if (pEvalData.GetNegDivCount() > -1)
                {
                    factoredOutUnrelatedTerm = factoredOutUnrelatedTerm.RemoveRedundancies(false).ToAlgTerm();
                    foreach (ExComp subComp in factoredOutUnrelatedTerm.GetSubComps())
                    {
                        if (ExNumber.GetNegOne().IsEqualTo(subComp) || (subComp is ExNumber && !(subComp as ExNumber).HasImaginaryComp() && ExNumber.OpLT((subComp as ExNumber), 0.0)))
                        {
                            pEvalData.SetNegDivCount(pEvalData.GetNegDivCount() + 1);
                            break;
                        }
                    }
                }

                if (pEvalData.GetWorkMgr().GetAllowWork())
                {
                    if (displayFactoringWork)
                    {
                        ExComp tmpRight = right;

                        left = Equation.Operators.DivOp.StaticCombine(left, factoredOutUnrelatedTerm.CloneEx()).ToAlgTerm();

                        right = Equation.Operators.DivOp.StaticCombine(right, factoredOutUnrelatedTerm.CloneEx()).ToAlgTerm();

                        //string divStr = WorkMgr.CG_TXT_TG("{0}");
                        string divStr = "{0}";
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "(({0})*({2}))/(" + divStr + ")=({1})/(" + divStr + ")" + WorkMgr.EDM, "Divide both sides by " + WorkMgr.STM + "{0}" + WorkMgr.EDM,
                            factoredOutUnrelatedTerm, tmpRight, left);

                        pEvalData.GetWorkMgr().FromSides(left, right, "Cancel and simplify");

                        return;
                    }
                    else
                        pEvalData.GetWorkMgr().FromDivision(factoredOutUnrelatedTerm, left, right);
                }

                left = Equation.Operators.DivOp.StaticCombine(left, factoredOutUnrelatedTerm.CloneEx()).ToAlgTerm();

                right = Equation.Operators.DivOp.StaticCombine(right, factoredOutUnrelatedTerm.CloneEx()).ToAlgTerm();

                pEvalData.GetWorkMgr().FromSides(left, right, "Cancel and simplify");
            }
            else if (left is PowerFunction && strongDivide)
            {
                PowerFunction leftPf = left as PowerFunction;
                if (leftPf.GetBase().ToAlgTerm().Contains(solveForComp) || !(leftPf.GetPower() is AlgebraTerm))
                    return;
                AlgebraTerm leftPow = leftPf.GetPower() as AlgebraTerm;
                List<AlgebraGroup> constGps = leftPow.GetGroupsConstantTo(solveForComp);
                if (constGps.Count != 0)
                {
                    List<AlgebraGroup> varGps = leftPow.GetGroupsVariableTo(solveForComp);
                    AlgebraTerm constTerm = new AlgebraTerm(constGps.ToArray());
                    AlgebraTerm varTerm = new AlgebraTerm(varGps.ToArray());
                    ExComp divide = new PowerFunction(leftPf.GetBase(), constTerm);

                    left = new PowerFunction(leftPf.GetBase(), varTerm);
                    right = Equation.Operators.DivOp.StaticCombine(right, divide).ToAlgTerm();
                }
            }
        }

        public static void EvaluateEntirely(ref AlgebraTerm term)
        {
            term = term.ApplyOrderOfOperations();
            ExComp workableTerm = term.MakeWorkable();
            if (workableTerm is AlgebraTerm)
            {
                AlgebraTerm compactedTerm = (workableTerm as AlgebraTerm).CompoundFractions();
                workableTerm = compactedTerm.RemoveRedundancies(false);
            }

            term = workableTerm.ToAlgTerm();
        }

        public static void PrepareForSolving(ref AlgebraTerm left, ref AlgebraTerm right, ref TermType.EvalData pEvalData)
        {
            PrepareForSolving(ref left, ref right, null, ref pEvalData);
        }

        public static void PrepareForSolving(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            if (solveForComp != null && left != null && right != null && !left.Contains(solveForComp) && right.Contains(solveForComp))
            {
                AlgebraTerm tmp = left;
                left = right;
                right = tmp;
                if (pEvalData.GetNegDivCount() != -1)
                    pEvalData.SetNegDivCount(pEvalData.GetNegDivCount() + 1);
            }

            if (left != null)
            {
                if (left.GetTermCount() == 0)
                    left.Add(ExNumber.GetZero());
                left = left.ApplyOrderOfOperations();
            }
            if (right != null)
            {
                if (right.GetTermCount() == 0)
                    right.Add(ExNumber.GetZero());
                right = right.ApplyOrderOfOperations();
            }

            if (pEvalData.GetIsWorkable())
                return;

            if (left != null)
            {
                ExComp workableLeft = left.MakeWorkable();
                if (workableLeft is AlgebraTerm)
                    workableLeft = (workableLeft as AlgebraTerm).RemoveRedundancies(false);
                left = workableLeft.ToAlgTerm();
            }
            if (right != null)
            {
                ExComp workableRight = right.MakeWorkable();
                if (workableRight is AlgebraTerm)
                    workableRight = (workableRight as AlgebraTerm).RemoveRedundancies(false);
                right = workableRight.ToAlgTerm();
            }

            pEvalData.SetIsWorkable(true);
        }

        public static void VariableFractionsToLeft(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            List<AlgebraGroup> varFracGroups = right.GetVariableFractionGroups(solveForComp);

            if (varFracGroups.Count != 0)
                pEvalData.GetWorkMgr().FromAlgGpSubtraction(varFracGroups, left, right);

            foreach (AlgebraGroup varFracGroup in varFracGroups)
            {
                ExComp subTerm = varFracGroup.ToTerm().CloneEx();

                left = Equation.Operators.SubOp.StaticCombine(left, subTerm).ToAlgTerm();
                right = Equation.Operators.SubOp.StaticCombine(right, subTerm).ToAlgTerm();
            }

            left = left.RemoveZeros();
            right = right.RemoveZeros();

            if (varFracGroups.Count != 0)
                pEvalData.GetWorkMgr().FromSides(left, right, "Simplify");
        }

        public static void VariablesToLeft(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            List<AlgebraGroup> variableGroupsRight = right.GetGroupsVariableTo(solveForComp);

            if (variableGroupsRight.Count != 0)
                pEvalData.GetWorkMgr().FromAlgGpSubtraction(variableGroupsRight, left, right);

            foreach (AlgebraGroup variableGroup in variableGroupsRight)
            {
                left = AlgebraTerm.OpSub(left, variableGroup);
                right = AlgebraTerm.OpSub(right, variableGroup);
            }

            left = left.RemoveZeros();
            right = right.RemoveZeros();

            if (right.GetTermCount() == 0)
                right = ExNumber.GetZero().ToAlgTerm();

            if (variableGroupsRight.Count != 0)
                pEvalData.GetWorkMgr().FromSides(left, right, "Simplify");
        }

        public abstract ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData);
    }
}