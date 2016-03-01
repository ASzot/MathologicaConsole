using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.TermType;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class TrigSubTech
    {
        public TrigSubTech()
        {
        }

        public ExComp TrigSubstitution(ExComp[] group, AlgebraComp dVar, ref EvalData pEvalData)
        {
            AlgebraComp subVar;
            AlgebraTerm subbedResult;
            ExComp subOut;

            ExComp subIn = TrigSubstitutionGetSub(GroupHelper.CloneGroup(group), dVar, out subVar, out subOut, out subbedResult, ref pEvalData);
            if (subIn == null || subbedResult == null || subVar == null || subOut == null)
                return null;

            if (!subOut.IsEqualTo(dVar))
            {
                AlgebraSolver agSolver = new AlgebraSolver();
                subIn = agSolver.Solve(dVar.GetVar(), subOut.ToAlgTerm(), subIn.ToAlgTerm(), ref pEvalData);
            }

            subbedResult = subbedResult.Substitute(dVar, subIn.CloneEx());
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + WorkMgr.ToDisp(subbedResult) + ") d" + dVar.ToDispString() + WorkMgr.EDM, "Substitute " + WorkMgr.STM + dVar.ToDispString() +
                " = " + WorkMgr.ToDisp(subIn) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("");
            WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp differential = Derivative.TakeDeriv(subIn.CloneEx(), subVar, ref pEvalData, false, false);
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "d" + dVar.ToDispString() + " = "
                                 + WorkMgr.ToDisp(differential) + "d" + subVar.ToDispString() + WorkMgr.EDM);

            ExComp trigSubbed = MulOp.StaticCombine(subbedResult, differential);
            trigSubbed = Simplifier.Simplify(trigSubbed.ToAlgTerm(), ref pEvalData);
            if (trigSubbed is AlgebraTerm)
                trigSubbed = (trigSubbed as AlgebraTerm).RemoveRedundancies(false);

            string trigSubbedStr = WorkMgr.ToDisp(trigSubbed);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + trigSubbedStr + ") d" + subVar.ToDispString() + WorkMgr.EDM,
                "Substitute in " + WorkMgr.STM + "d" + dVar.ToDispString() + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("", "Evaluate the integral.");
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp antiDerivResult = Integral.TakeAntiDeriv(trigSubbed, subVar, ref pEvalData);
            if (antiDerivResult is Integral)
                return null;
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "\\int (" + trigSubbedStr + ") d" + subVar.ToDispString() + " = " + WorkMgr.ToDisp(antiDerivResult) + WorkMgr.EDM);

            // Sub back in the appropriate variables.
            ExComp subbedBackIn = TrigSubBackIn(antiDerivResult, subVar, subIn, dVar, ref pEvalData);
            if (subbedBackIn == null)
                return null;

            return subbedBackIn;
        }

        private static ExComp TrigSubBackIn(ExComp ex, AlgebraComp subVar, ExComp subVal, AlgebraComp dVar, ref TermType.EvalData pEvalData)
        {
            int workStepStart = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            pEvalData.GetWorkMgr().FromSides(dVar, subVal, "Solve for " + WorkMgr.STM + subVar.ToDispString() + WorkMgr.EDM);

            AlgebraTerm left = subVal.ToAlgTerm();
            AlgebraTerm right = dVar.ToAlgTerm();
            Solving.SolveMethod.ConstantsToRight(ref left, ref right, subVar, ref pEvalData);
            Solving.SolveMethod.DivideByVariableCoeffs(ref left, ref right, subVar, ref pEvalData, false);

            List<WorkStep> stepRange = pEvalData.GetWorkMgr().GetWorkSteps().GetRange(workStepStart, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - workStepStart);
            pEvalData.GetWorkMgr().GetWorkSteps().RemoveRange(workStepStart, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - workStepStart);

            // It should now just be the isolated trig function.
            ExComp leftEx = left.RemoveRedundancies(false);
            if (!(leftEx is TrigFunction))
                return null;
            TrigFunction trigFunc = leftEx as TrigFunction;

            ExComp hyp = null;
            ExComp opp = null;
            ExComp adj = null;

            AlgebraTerm[] numDen = right.GetNumDenFrac();
            if (numDen == null)
                numDen = new AlgebraTerm[] { right, ExNumber.GetOne().ToAlgTerm() };

            if (trigFunc is SinFunction)
            {
                opp = numDen[0];
                hyp = numDen[1];
            }
            else if (trigFunc is CosFunction)
            {
                adj = numDen[0];
                hyp = numDen[1];
            }
            else if (trigFunc is TanFunction)
            {
                opp = numDen[0];
                adj = numDen[1];
            }
            else if (trigFunc is CscFunction)
            {
                hyp = numDen[0];
                opp = numDen[1];
            }
            else if (trigFunc is SecFunction)
            {
                hyp = numDen[0];
                adj = numDen[1];
            }
            else if (trigFunc is CotFunction)
            {
                adj = numDen[0];
                opp = numDen[1];
            }

            // Find the last side.
            if (hyp == null)
            {
                hyp = PowOp.StaticCombine(
                    AddOp.StaticCombine(PowOp.StaticCombine(adj, new ExNumber(2.0)), PowOp.StaticCombine(opp, new ExNumber(2.0))),
                    AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)));
            }
            else if (opp == null)
            {
                opp = PowOp.StaticCombine(
                    SubOp.StaticCombine(PowOp.StaticCombine(hyp, new ExNumber(2.0)), PowOp.StaticCombine(adj, new ExNumber(2.0))),
                    AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)));
            }
            else if (adj == null)
            {
                adj = PowOp.StaticCombine(
                    SubOp.StaticCombine(PowOp.StaticCombine(hyp, new ExNumber(2.0)), PowOp.StaticCombine(opp, new ExNumber(2.0))),
                    AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)));
            }

            List<string> defDispStrs = new List<string>();

            ExComp trigSubIn = RecursiveTrigSubIn(ex.ToAlgTerm(), hyp, opp, adj, subVar, ref defDispStrs, ref pEvalData);
            if (trigSubIn == null)
                return null;

            string summed = WorkMgr.STM;
            for (int i = 0; i < defDispStrs.Count; ++i)
            {
                summed += defDispStrs[i];
                if (i != defDispStrs.Count - 1)
                    summed += ",";
            }

            summed += WorkMgr.EDM;

            pEvalData.GetWorkMgr().FromSides(trigSubIn, null, "Make the substitutions " + summed);

            pEvalData.GetWorkMgr().GetWorkSteps().AddRange(stepRange);

            InverseTrigFunction itf = trigFunc.GetInverseOf();
            itf.SetSubComps(right.GetSubComps());

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + itf.GetFuncName() + "(" + WorkMgr.ToDisp(trigFunc) + ") = " + WorkMgr.ToDisp(itf) + WorkMgr.EDM,
                "Take the inverse " + trigFunc.GetFuncName() + " of both sides.");

            ExComp dVarSubIn = itf.Evaluate(false, ref pEvalData);

            pEvalData.GetWorkMgr().FromSides(subVar, dVarSubIn);

            // Now do the easy subs.
            trigSubIn = trigSubIn.ToAlgTerm().Substitute(subVar, dVarSubIn);

            if (trigSubIn.ToAlgTerm().Contains(subVar))
                return null;

            pEvalData.GetWorkMgr().FromSides(trigSubIn, null, "Substitute in " + WorkMgr.STM + subVar.ToDispString() + " = " + WorkMgr.ToDisp(dVarSubIn) + WorkMgr.EDM);

            return trigSubIn;
        }

        private static ExComp GetAppropriateSub(TrigFunction trigFunc, ExComp hyp, ExComp opp, ExComp adj)
        {
            if (trigFunc is SinFunction)
                return DivOp.StaticCombine(opp, hyp);
            else if (trigFunc is CosFunction)
                return DivOp.StaticCombine(adj, hyp);
            else if (trigFunc is TanFunction)
                return DivOp.StaticCombine(opp, adj);
            else if (trigFunc is CscFunction)
                return DivOp.StaticCombine(hyp, opp);
            else if (trigFunc is SecFunction)
                return DivOp.StaticCombine(hyp, adj);
            else if (trigFunc is CotFunction)
                return DivOp.StaticCombine(adj, opp);

            return null;
        }

        private static ExComp RecursiveTrigSubIn(AlgebraTerm term, ExComp hyp, ExComp opp, ExComp adj, AlgebraComp subInVar, ref List<string> defDispStrs,
            ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is TrigFunction && (term[i] as TrigFunction).GetInnerEx().IsEqualTo(subInVar))
                {
                    TrigFunction tfTerm = term[i] as TrigFunction;
                    ExComp trigSubIn = GetAppropriateSub(tfTerm, hyp, opp, adj);
                    if (trigSubIn == null)
                        return null;

                    bool contains = false;
                    foreach (string defDispStr in defDispStrs)
                    {
                        if (defDispStr.StartsWith(tfTerm.GetFuncName()))
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (!contains)
                        defDispStrs.Add(tfTerm.GetFuncName() + "(" + subInVar.ToDispString() + ")=" + WorkMgr.ToDisp(trigSubIn));

                    term[i] = trigSubIn;
                }
                else if (term[i] is AlgebraTerm)
                {
                    term[i] = RecursiveTrigSubIn(term[i] as AlgebraTerm, hyp, opp, adj, subInVar, ref defDispStrs, ref pEvalData);
                }
            }

            return term;
        }

        private static ExComp SumTerm(List<ExComp[]> subGps, int subIndex, ExComp[] subInGp)
        {
            ExComp summedTerm = null;
            for (int j = 0; j < subGps.Count; ++j)
            {
                ExComp addTerm = (j != subIndex ? GroupHelper.ToAlgTerm(subGps[j]) : GroupHelper.ToAlgTerm(subInGp));
                if (summedTerm == null)
                    summedTerm = addTerm;
                else
                    summedTerm = AddOp.StaticCombine(summedTerm, addTerm);
            }

            return summedTerm;
        }

        private static ExComp TrigSubstitutionGetSub(ExComp[] group, AlgebraComp dVar, out AlgebraComp subVar, out ExComp subOut, out AlgebraTerm subbedResult, ref EvalData pEvalData)
        {
            ExComp trigSubResult = TrigSubstitutionGetSub(group, dVar, out subVar, out subOut, out subbedResult, ref pEvalData, null, null, -1, -1);
            return trigSubResult;
        }

        private static void SubstituteIn(ref ExComp[] dispGp, List<ExComp[]> dispSubGps, ExComp[] group, int index, int subIndex)
        {
            if (dispGp != null && dispSubGps != null && index != -1 && subIndex != -1)
            {
                ExComp termSummed = SumTerm(dispSubGps, subIndex, group);
                if (dispGp[index] is AlgebraTerm)
                    (dispGp[index] as AlgebraTerm).SetSubComps(termSummed.ToAlgTerm().GetSubComps());
                else
                    dispGp[index] = termSummed;
            }
            else
                dispGp = group;
        }

        /// <summary>
        /// Null is returned in the case of there being no substitution to make.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="dVar"></param>
        /// <param name="subVar"></param>
        /// <param name="subOut"></param>
        /// <param name="subbedResult"></param>
        /// <param name="pEvalData"></param>
        /// <param name="dispGp"></param>
        /// <param name="dispSubGps"></param>
        /// <param name="index"></param>
        /// <param name="subIndex"></param>
        /// <returns></returns>
        private static ExComp TrigSubstitutionGetSub(ExComp[] group, AlgebraComp dVar, out AlgebraComp subVar, out ExComp subOut, out AlgebraTerm subbedResult, ref EvalData pEvalData,
            ExComp[] dispGp, List<ExComp[]> dispSubGps, int index, int subIndex)
        {
            string subVarStr = "$theta";
            subVar = new AlgebraComp(subVarStr);
            subbedResult = null;
            subOut = null;

            for (int i = 0; i < group.Length; ++i)
            {
                if (group[i] is AlgebraTerm)
                {
                    // Apply on all sub levels.
                    AlgebraTerm subTerm = group[i] as AlgebraTerm;

                    List<ExComp[]> subGps = subTerm.GetGroupsNoOps();
                    if (subGps.Count == 1 && subGps[0].Length == 1 && subGps[0][0].IsEqualTo(subTerm))
                    {
                        // This is something that returns itself as a group.
                        subGps = (new AlgebraTerm(subTerm.GetSubComps().ToArray())).GetGroupsNoOps();
                        if (subGps.Count > 1)
                            return null;
                    }
					//if (subGps.Count == 1 && subGps[0] == subTerm)
					//{
					//	// This will result in a stack overflow if it is not stopped. 
					//}

                    ExComp subSubbedResult = null;
                    int subSubbedIndex = -1;
                    for (int j = 0; j < subGps.Count; ++j)
                    {
                        subSubbedResult = TrigSubstitutionGetSub(subGps[j], dVar, out subVar, out subOut, out subbedResult, ref pEvalData, GroupHelper.CloneGroup(group),
                            GroupHelper.CloneGpList(subGps), i, j);
                        if (subSubbedResult != null)
                        {
                            subSubbedIndex = j;
                            break;
                        }
                    }

                    if (subSubbedIndex != -1)
                    {
                        ExComp summedTerm = null;
                        for (int j = 0; j < subGps.Count; ++j)
                        {
                            ExComp addTerm = (j == subSubbedIndex ? GroupHelper.ToAlgTerm(subGps[j]) : subbedResult);
                            if (summedTerm == null)
                                summedTerm = addTerm;
                            else
                                summedTerm = AddOp.StaticCombine(summedTerm, addTerm);
                        }

                        (group[i] as AlgebraTerm).SetSubComps(summedTerm.ToAlgTerm().GetSubComps());

                        subbedResult = GroupHelper.ToAlgTerm(group);
                        return subSubbedResult;
                    }
                }
                // Is this a sqrt function?
                if (!(group[i] is PowerFunction))
                {
                    continue;
                }

                PowerFunction powFunc = group[i] as PowerFunction;
                if (!(powFunc.GetPower() is AlgebraTerm))
                    continue;

                AlgebraTerm powTerm = powFunc.GetPower() as AlgebraTerm;
                AlgebraTerm[] numDen = powTerm.GetNumDenFrac();
                if (numDen == null)
                    continue;
                if (!numDen[1].RemoveRedundancies(false).IsEqualTo(new ExNumber(2.0)))
                    continue;

                if (!ExNumber.GetOne().IsEqualTo(numDen[0].RemoveRedundancies(false)))
                {
                    PowerFunction nestedPf = new PowerFunction(new PowerFunction(powFunc.GetBase(), AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0))), numDen[0]);

                    // Go back over this element.
                    group[i] = nestedPf;
                    i--;
                    continue;
                }

                AlgebraTerm baseTerm = (group[i] as PowerFunction).GetBase().ToAlgTerm();

                // Is it x^2?
                List<ExComp> basePows = baseTerm.GetPowersOfVar(dVar);
                if (basePows.Count != 1)
                    continue;
                ExComp singlePow = basePows[0];
                if (!(singlePow is ExNumber) && ExNumber.OpEqual((singlePow as ExNumber), 2.0))
                    continue;

                subOut = dVar;

                // Find the b value.
                List<AlgebraGroup> varGroups = baseTerm.GetGroupsVariableToNoOps(dVar);
                if (varGroups.Count != 1)
                    continue;

                AlgebraTerm bSqTerm = AlgebraGroup.GetConstantTo(varGroups, dVar);
                ExComp bSq = bSqTerm.RemoveRedundancies(false);

                // Find the a value.
                List<AlgebraGroup> constGroups = baseTerm.GetGroupsConstantTo(dVar);
                if (constGroups.Count != 1)
                    continue;

                AlgebraTerm aSqTerm = AlgebraGroup.ToTerm(constGroups);
                ExComp aSq = aSqTerm.RemoveRedundancies(false);

                ExNumber aCoeff = null;
                if (bSq is ExNumber)
                    aCoeff = aSq as ExNumber;
                else if (aSq is AlgebraTerm)
                {
                    ExComp[] aValGp = GroupHelper.GetUnrelatableTermsOfGroup(varGroups[0].GetGroup(), dVar);
                    aCoeff = GroupHelper.GetCoeff(aValGp);
                }

                if (aCoeff == null)
                    return null;

                ExNumber bCoeff = null;
                if (bSq is ExNumber)
                    bCoeff = bSq as ExNumber;
                else if (bSq is AlgebraTerm)
                {
                    bCoeff = GroupHelper.GetCoeff(constGroups[0].GetGroup());
                }

                if (bCoeff == null)
                    return null;

                if (aCoeff.HasImaginaryComp() || bCoeff.HasImaginaryComp())
                    return null;

                bool aNeg = ExNumber.OpLT(aCoeff, 0.0);
                bool bNeg = ExNumber.OpLT(bCoeff, 0.0);

                if (aNeg)
                    aSq = (new AbsValFunction(aSq)).Evaluate(false, ref pEvalData);

                if (aSq is AbsValFunction)
                    return null;

                if (bNeg)
                    bSq = (new AbsValFunction(bSq)).Evaluate(false, ref pEvalData);

                if (bSq is AbsValFunction)
                    return null;

                ExComp usedTrigFunc = null;
                if (!aNeg && bNeg)
                    usedTrigFunc = new SinFunction(new AlgebraComp(subVarStr));
                else if (aNeg && !bNeg)
                    usedTrigFunc = new SecFunction(new AlgebraComp(subVarStr));
                else if (!aNeg && !bNeg)
                    usedTrigFunc = new TanFunction(new AlgebraComp(subVarStr));

                if (usedTrigFunc == null)
                    return null;

                ExComp aVal = PowOp.StaticCombine(aSq, AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)));
                ExComp bVal = PowOp.StaticCombine(bSq, AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)));

                if (aVal is AlgebraTerm)
                    aVal = (aVal as AlgebraTerm).RemoveRedundancies(false);
                if (bVal is AlgebraTerm)
                    bVal = (bVal as AlgebraTerm).RemoveRedundancies(false);

                ExComp subIn = MulOp.StaticCombine(DivOp.StaticCombine(aVal, bVal), usedTrigFunc);

                // Replace the value in the group itself.
                ExComp baseTermSubbed = AddOp.StaticCombine(new AlgebraTerm(bSq, new MulOp(), PowOp.StaticWeakCombine(subIn, new ExNumber(2.0))), aSq);
                ExComp pfPow = (group[i] as PowerFunction).GetPower();

                group[i] = new PowerFunction(baseTermSubbed, pfPow);
                SubstituteIn(ref dispGp, dispSubGps, group, index, subIndex);

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + WorkMgr.ToDisp(GroupHelper.ToAlgTerm(dispGp)) + ") d" + dVar.ToDispString() + WorkMgr.EDM, "Substitute " + WorkMgr.STM + subVar.ToDispString() +
                    "=" + WorkMgr.ToDisp(subIn) + WorkMgr.EDM);

                AlgebraTerm dispIdenStep = new AlgebraTerm();
                AlgebraTerm useTerm = dispIdenStep;
                if (!aSqTerm.IsOne())
                {
                    useTerm = new AlgebraTerm();
                    dispIdenStep.Add(aSqTerm, new MulOp(), useTerm);
                }

                ExComp simpTo = null;

                if (usedTrigFunc is SinFunction)
                {
                    useTerm.Add(ExNumber.GetOne(), new SubOp(), PowOp.StaticWeakCombine(usedTrigFunc, new ExNumber(2.0)));
                    simpTo = new CosFunction((usedTrigFunc as AppliedFunction).GetInnerTerm());
                }
                else if (usedTrigFunc is SecFunction)
                {
                    useTerm.Add(PowOp.StaticWeakCombine(usedTrigFunc, new ExNumber(2.0)), new SubOp(), ExNumber.GetOne());
                    simpTo = new TanFunction((usedTrigFunc as AppliedFunction).GetInnerTerm());
                }
                else if (usedTrigFunc is TanFunction)
                {
                    useTerm.Add(PowOp.StaticWeakCombine(usedTrigFunc, new ExNumber(2.0)), new AddOp(), ExNumber.GetOne());
                    simpTo = new SecFunction((usedTrigFunc as AppliedFunction).GetInnerTerm());
                }
                else
                    return null;

                group[i] = new PowerFunction(dispIdenStep, pfPow);
                SubstituteIn(ref dispGp, dispSubGps, group, index, subIndex);

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + WorkMgr.ToDisp(GroupHelper.ToAlgTerm(dispGp)) + ")d" + dVar.ToDispString() + WorkMgr.EDM,
                    "Simplify.");

                dispIdenStep = new AlgebraTerm();
                if (aVal is ExNumber && ExNumber.OpNotEquals((aVal as ExNumber), 1.0))
                {
                    dispIdenStep.Add(aVal, new MulOp());
                }

                dispIdenStep.Add(new PowerFunction(PowOp.StaticWeakCombine(simpTo, new ExNumber(2.0)), pfPow));

                group[i] = dispIdenStep;
                SubstituteIn(ref dispGp, dispSubGps, group, index, subIndex);

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + WorkMgr.ToDisp(GroupHelper.ToAlgTerm(dispGp)) + ") d" + dVar.ToDispString() + WorkMgr.EDM, "Use the trig identity " +
                    WorkMgr.STM + WorkMgr.ToDisp(PowOp.StaticWeakCombine(simpTo, new ExNumber(2.0))) + " = " + useTerm.FinalToDispStr() + WorkMgr.EDM);

                group[i] = MulOp.StaticCombine(aVal, simpTo);
                SubstituteIn(ref dispGp, dispSubGps, group, index, subIndex);

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + WorkMgr.ToDisp(GroupHelper.ToAlgTerm(dispGp)) + ") d" + dVar.ToDispString() + WorkMgr.EDM);

                subbedResult = GroupHelper.ToAlgTerm(group);

                return subIn;
            }

            return null;
        }
    }
}