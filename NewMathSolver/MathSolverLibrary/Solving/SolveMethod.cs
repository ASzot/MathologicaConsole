using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;
using System.Linq;

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
                left = left.RemoveRedundancies().ToAlgTerm();
            }

            bool fracCombine1 = false;
            if (right != null)
            {
                right = right.CompoundFractions(out fracCombine1);
                right = right.RemoveRedundancies().ToAlgTerm();
            }

            if (fracCombine0 || fracCombine1)
            {
                pEvalData.WorkMgr.FromSides(left, right, "Combine fractions");
            }
        }

        public static void ConstantsToRight(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            List<AlgebraGroup> constantGroupsLeft = left.GetGroupsConstantTo(solveForComp);

            if (constantGroupsLeft.Count != 0)
                pEvalData.WorkMgr.FromAlgGpSubtraction(constantGroupsLeft, left, right);

            bool displaySimpStep = constantGroupsLeft.Count != 0 && !(constantGroupsLeft.Count == 1 && constantGroupsLeft[0].IsZero());

            foreach (AlgebraGroup constantGroup in constantGroupsLeft)
            {
                ExComp subTerm = constantGroup.ToTerm().Clone();
                left = Equation.Operators.SubOp.StaticCombine(left, subTerm).ToAlgTerm();
                right = Equation.Operators.SubOp.StaticCombine(right, subTerm).ToAlgTerm();
            }

            left = left.RemoveZeros().RemoveRedundancies().ToAlgTerm();
            right = right.RemoveZeros().RemoveRedundancies().ToAlgTerm();

            if (right.TermCount == 0)
                right = Number.Zero.ToAlgTerm();

            if (left.TermCount == 0)
                left = Number.Zero.ToAlgTerm();

            if (displaySimpStep)
                pEvalData.WorkMgr.FromSides(left, right, "Simplify");
        }

        public static void DivideByVariableCoeffs(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            if (right.SubComps.Count == 0)
                right.Add(Number.Zero);
            if (left.TermCount != 1)
            {
                var groups = left.GetGroupsNoOps();
                var unrelatedGroups = from gp in groups
                                      select gp.GetUnrelatableTermsOfGroup(solveForComp);

                // Combine all of the unrelated terms.
                AlgebraTerm factoredOutUnrelatedTerm = new AlgebraTerm(unrelatedGroups.ToArray());

                bool displayFactoringWork = false;
                if (factoredOutUnrelatedTerm.GroupCount > 1)
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

                if (pEvalData.NegDivCount > -1)
                {
                    factoredOutUnrelatedTerm = factoredOutUnrelatedTerm.RemoveRedundancies().ToAlgTerm();
                    foreach (ExComp subComp in factoredOutUnrelatedTerm.SubComps)
                    {
                        if (Number.NegOne.IsEqualTo(subComp) || (subComp is Number && !(subComp as Number).HasImaginaryComp() && (subComp as Number) < 0.0))
                        {
                            pEvalData.NegDivCount++;
                            break;
                        }
                    }
                }

                if (pEvalData.WorkMgr.AllowWork)
                {
                    if (displayFactoringWork)
                    {
                        ExComp tmpRight = right;

                        left = Equation.Operators.DivOp.StaticCombine(left, factoredOutUnrelatedTerm.Clone()).ToAlgTerm();
                        right = Equation.Operators.DivOp.StaticCombine(right, factoredOutUnrelatedTerm.Clone()).ToAlgTerm();

                        //string divStr = WorkMgr.CG_TXT_TG("{0}");
                        string divStr = "{0}";
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "(({0})*({2}))/(" + divStr + ")=({1})/(" + divStr + ")" + WorkMgr.EDM, "Divide both sides by " + WorkMgr.STM + "{0}" + WorkMgr.EDM,
                            factoredOutUnrelatedTerm, tmpRight, left);

                        pEvalData.WorkMgr.FromSides(left, right, "Cancel and simplify");

                        return;
                    }
                    else
                        pEvalData.WorkMgr.FromDivision(factoredOutUnrelatedTerm, left, right);
                }

                left = Equation.Operators.DivOp.StaticCombine(left, factoredOutUnrelatedTerm.Clone()).ToAlgTerm();
                right = Equation.Operators.DivOp.StaticCombine(right, factoredOutUnrelatedTerm.Clone()).ToAlgTerm();

                pEvalData.WorkMgr.FromSides(left, right, "Cancel and simplify");
            }
        }

        public static void EvaluateEntirely(ref AlgebraTerm term)
        {
            term = term.ApplyOrderOfOperations();
            ExComp workableTerm = term.MakeWorkable();
            if (workableTerm is AlgebraTerm)
            {
                AlgebraTerm compactedTerm = (workableTerm as AlgebraTerm).CompoundFractions();
                workableTerm = compactedTerm.RemoveRedundancies();
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
                if (pEvalData.NegDivCount != -1)
                    pEvalData.NegDivCount++;
            }

            if (left != null)
            {
                if (left.TermCount == 0)
                    left.Add(Number.Zero);
                left = left.ApplyOrderOfOperations();
            }
            if (right != null)
            {
                if (right.TermCount == 0)
                    right.Add(Number.Zero);
                right = right.ApplyOrderOfOperations();
            }

            if (pEvalData.IsWorkable)
                return;

            if (left != null)
            {
                ExComp workableLeft = left.MakeWorkable();
                if (workableLeft is AlgebraTerm)
                    workableLeft = (workableLeft as AlgebraTerm).RemoveRedundancies();
                left = workableLeft.ToAlgTerm();
            }
            if (right != null)
            {
                ExComp workableRight = right.MakeWorkable();
                if (workableRight is AlgebraTerm)
                    workableRight = (workableRight as AlgebraTerm).RemoveRedundancies();
                right = workableRight.ToAlgTerm();
            }

            pEvalData.IsWorkable = true;
        }

        public static void VariableFractionsToLeft(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            List<AlgebraGroup> varFracGroups = right.GetVariableFractionGroups(solveForComp);

            if (varFracGroups.Count != 0)
                pEvalData.WorkMgr.FromAlgGpSubtraction(varFracGroups, left, right);

            foreach (AlgebraGroup varFracGroup in varFracGroups)
            {
                ExComp subTerm = varFracGroup.ToTerm().Clone();

                left = Equation.Operators.SubOp.StaticCombine(left, subTerm).ToAlgTerm();
                right = Equation.Operators.SubOp.StaticCombine(right, subTerm).ToAlgTerm();
            }

            left = left.RemoveZeros();
            right = right.RemoveZeros();

            if (varFracGroups.Count != 0)
                pEvalData.WorkMgr.FromSides(left, right, "Simplify");
        }

        public static void VariablesToLeft(ref AlgebraTerm left, ref AlgebraTerm right, AlgebraComp solveForComp, ref TermType.EvalData pEvalData)
        {
            List<AlgebraGroup> variableGroupsRight = right.GetGroupsVariableTo(solveForComp);

            if (variableGroupsRight.Count != 0)
                pEvalData.WorkMgr.FromAlgGpSubtraction(variableGroupsRight, left, right);

            foreach (AlgebraGroup variableGroup in variableGroupsRight)
            {
                left = left - variableGroup;
                right = right - variableGroup;
            }

            left = left.RemoveZeros();
            right = right.RemoveZeros();

            if (right.TermCount == 0)
                right = Number.Zero.ToAlgTerm();

            if (variableGroupsRight.Count != 0)
                pEvalData.WorkMgr.FromSides(left, right, "Simplify");
        }

        public abstract ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData);
    }
}