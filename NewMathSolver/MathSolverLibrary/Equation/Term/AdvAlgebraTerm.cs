using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Term
{
    internal static class AdvAlgebraTerm
    {
        public static AlgebraTerm AbsValToParas(AlgebraTerm term)
        {
            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is AbsValFunction)
                {
                    term[i] = new AlgebraTerm((term[i] as AbsValFunction).GetInnerEx());
                }
                else if (term[i] is AlgebraTerm && !(term is AlgebraFunction))
                {
                    term[i] = AbsValToParas((term[i] as AlgebraTerm));
                }
            }

            return term;
        }

        public static bool ContainsOneOfFuncs(AlgebraTerm term, params Type[] types)
        {
            if (ArrayFunc.Contains(types, term.GetType()))
                return true;

            for (int i = 0; i < term.GetSubComps().Count; ++i)
            {
                if (ArrayFunc.Contains(types, term.GetSubComps()[i].GetType()))
                    return true;

                if (term.GetSubComps()[i] is AlgebraTerm && ContainsOneOfFuncs((term.GetSubComps()[i] as AlgebraTerm), types))
                    return true;
            }

            return false;
        }

        public static AlgebraTerm CompoundLogs(AlgebraTerm term, AlgebraComp solveForComp)
        {
            bool garbage;
            return CompoundLogs(term, out garbage, solveForComp);
        }

        public static AlgebraTerm CompoundLogs(AlgebraTerm term, out bool hasCombined, AlgebraComp solveForComp)
        {
            hasCombined = false;
            int varLogCount = -1;
            if (solveForComp != null)
            {
                varLogCount = term.GetAppliedFuncCount(solveForComp, FunctionType.Logarithm);
            }

            term = term.RemoveRedundancies(false).ToAlgTerm();

            List<ExComp[]> finalGroups = new List<ExComp[]>();

            List<LogFunction> logFuncs = new List<LogFunction>();
            List<ExComp[]> totalGroups = term.GetGroupsNoOps();
            for (int i = 0; i < term.GetGroupCount(); ++i)
            {
                ExComp[] curGroup = totalGroups[i];
                if (curGroup.Length == 1 && curGroup[0] is LogFunction)
                {
                    logFuncs.Add(curGroup[0] as LogFunction);
                    continue;
                }

                int logCount = 0;
                LogFunction logFunc = null;
                // List of variable coefficients.
                // ( there will be none if solveFor is null ).
                List<ExComp> varTerms = new List<ExComp>();
                // List of constant coefficients.
                List<ExComp> coeffs = new List<ExComp>();
                for (int j = 0; j < curGroup.Length; ++j)
                {
                    ExComp curGroupComp = curGroup[j];
                    if (curGroupComp is LogFunction)
                    {
                        logCount++;
                        if (logFunc != null)
                            break;
                        logFunc = (LogFunction)curGroupComp;
                    }
                    else if (solveForComp != null && (solveForComp.IsEqualTo(curGroupComp) || (curGroupComp is AlgebraTerm && (curGroupComp as AlgebraTerm).Contains(solveForComp))))
                    {
                        varTerms.Add(curGroupComp);
                        continue;
                    }
                    else
                    {
                        coeffs.Add(curGroupComp);
                    }
                }
                if (logCount == 1 && coeffs.Count == 1)
                {
                    AlgebraTerm coeff = new AlgebraTerm();
                    coeff.AddGroup(coeffs.ToArray());

                    // Move the coefficient to the log's power.
                    ExComp innerEx = logFunc.GetInnerEx();

                    if (solveForComp != null &&
                        (innerEx.IsEqualTo(solveForComp) || (innerEx is AlgebraTerm && (innerEx as AlgebraTerm).Contains(solveForComp)))
                        && varLogCount == 1)
                    {
                        // We also know that this log is alone and shouldn't combine with other logs.
                        finalGroups.Add(curGroup);
                        continue;
                    }

                    // The coeff doesn't include the var term.
                    ExComp raisedInnerEx = coeff.IsEqualTo(ExNumber.GetOne()) ? innerEx : PowOp.StaticCombine(innerEx, coeff);

                    LogFunction addLog = new LogFunction(raisedInnerEx);
                    addLog.SetBase(logFunc.GetBase());

                    // We have a solve for variable in this term it won't be expanded.
                    if (varTerms.Count > 0)
                    {
                        curGroup = new ExComp[2];
                        AlgebraTerm varTerm = new AlgebraTerm();
                        varTerm.AddGroup(varTerms.ToArray());

                        curGroup[0] = varTerm;
                        curGroup[1] = addLog;
                    }
                    else
                    {
                        logFuncs.Add(addLog);
                        continue;
                    }
                }

                finalGroups.Add(curGroup);
            }

            // Combine logs which have like bases.
            List<LogFunction> combinedLogs = new List<LogFunction>();
            int variableLogCount = 0;
            int varLogIndex = -1;
            for (int i = 0; i < combinedLogs.Count; ++i)
            {
                LogFunction log = combinedLogs[i];
                if (log.Contains(solveForComp))
                {
                    variableLogCount++;
                    varLogIndex = i;
                }
            }

            if (variableLogCount == 1)
                ArrayFunc.RemoveIndex(combinedLogs, varLogIndex);

            foreach (LogFunction logFunc in logFuncs)
            {
                bool found = false;
                for (int i = 0; i < combinedLogs.Count; ++i)
                {
                    LogFunction combinedLog = combinedLogs[i];
                    if (combinedLog.GetBase().IsEqualTo(logFunc.GetBase()))
                    {
                        found = true;
                        ExComp combInner = MulOp.StaticCombine(logFunc.GetInnerEx(), combinedLog.GetInnerEx());
                        hasCombined = true;
                        combinedLogs[i] = new LogFunction(combInner);
                        combinedLogs[i].SetBase(logFunc.GetBase());
                    }
                }

                if (!found)
                {
                    combinedLogs.Add(logFunc);
                }
            }

            foreach (LogFunction combinedLog in combinedLogs)
            {
                ExComp[] addGroup = new ExComp[] { combinedLog };
                finalGroups.Add(addGroup);
            }

            return new AlgebraTerm(finalGroups.ToArray());
        }

        public static ExComp EvaluateExponentsCompletely(AlgebraTerm term)
        {
            if (term is PowerFunction)
            {
                PowerFunction powFunc = term as PowerFunction;
                AlgebraTerm baseTerm = powFunc.GetBase().ToAlgTerm();
                List<ExComp[]> groups = baseTerm.GetGroupsNoOps();
                if (groups.Count == 1)
                {
                    ExComp[] group = groups[0];
                    AlgebraTerm reconstructedTerm = new AlgebraTerm();
                    for (int i = 0; i < group.Length; ++i)
                    {
                        reconstructedTerm.Add(PowOp.StaticWeakCombine(group[i], powFunc.GetPower()));
                        if (i != group.Length - 1)
                            reconstructedTerm.Add(new MulOp());
                    }

                    return reconstructedTerm;
                }
            }

            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is AlgebraTerm)
                {
                    term[i] = EvaluateExponentsCompletely((term[i] as AlgebraTerm));
                }
            }

            return term;
        }

        public static AlgebraTerm EvaluatePowers(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            term = term.RemoveRedundancies(false).ToAlgTerm();

            if (term is PowerFunction)
            {
                PowerFunction powFunc = term as PowerFunction;

                if (powFunc.GetPower() is ExNumber && powFunc.GetBase() is AlgebraTerm)
                {
                    if (powFunc.GetBase().ToAlgTerm().GetTermCount() == 1)
                    {
                        if (powFunc.GetBase() is PowerFunction && !(powFunc.GetBase() as PowerFunction).GetPower().IsEqualTo(ExNumber.GetNegOne()) &&
                            !(powFunc.GetPower().IsEqualTo(ExNumber.GetNegOne())))
                        {
                            term = new PowerFunction((powFunc.GetBase() as PowerFunction).GetBase(),
                                MulOp.StaticCombine(powFunc.GetPower(), (powFunc.GetBase() as PowerFunction).GetPower()));
                            return term;
                        }
                    }

                    ExNumber powNum = powFunc.GetPower() as ExNumber;
                    bool makeDen = false;
                    if (powNum.IsRealInteger() && ExNumber.OpLT(powNum, 0.0))
                    {
                        makeDen = true;
                    }
                    powNum = ExNumber.Abs(powNum);

                    term = RaiseToPow(powFunc.GetBase(), powNum, ref pEvalData).ToAlgTerm();

                    if (makeDen)
                    {
                        term = PowOp.StaticWeakCombine(term, ExNumber.GetNegOne()).ToAlgTerm();
                    }
                }
                else if (powFunc.GetPower() is ExNumber && powFunc.GetBase() is ExNumber && !(powFunc.GetPower() as ExNumber).HasImaginaryComp() && !(powFunc.GetBase() as ExNumber).HasImaginaryComp())
                {
                    ExNumber nPowFuncPow = powFunc.GetPower() as ExNumber;
                    ExNumber nPowFuncBase = powFunc.GetBase() as ExNumber;

                    ExNumber result = ExNumber.OpPow(nPowFuncBase, nPowFuncPow);
                    if (result.IsRealInteger())
                        term = result.ToAlgTerm();
                }
                else if (powFunc.GetPower() is ExNumber && powFunc.GetBase() is ExNumber && !(powFunc.GetPower() as ExNumber).HasImaginaryComp())
                    return PowOp.StaticCombine(powFunc.GetBase(), powFunc.GetPower()).ToAlgTerm();
            }

            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is AlgebraTerm)
                {
                    term[i] = EvaluatePowers((term[i] as AlgebraTerm), ref pEvalData);
                }
            }

            return term;
        }

        //TODO:
        // There's a good chance this won't work.
        public static AlgebraTerm ExpandLogs(AlgebraTerm term)
        {
            term = term.RemoveRedundancies(false).ToAlgTerm();

            List<ExComp[]> finalGroups = new List<ExComp[]>();

            List<LogFunction> logFuncs = new List<LogFunction>();
            List<ExComp[]> totalGroups = term.GetGroupsNoOps();
            for (int i = 0; i < term.GetGroupCount(); ++i)
            {
                ExComp[] curGroup = totalGroups[i];
                for (int j = 0; j < curGroup.Length; ++j)
                {
                    ExComp curGroupComp = curGroup[j];
                    if (curGroupComp is LogFunction)
                    {
                        LogFunction toExpand = curGroupComp as LogFunction;
                        List<ExComp[]> expanded = ExpandLogFunctionInners(toExpand.GetInnerEx(), toExpand.GetBase());
                        AlgebraTerm expandedTerm = new AlgebraTerm(expanded.ToArray());
                        curGroup[j] = expandedTerm;
                    }
                }

                finalGroups.Add(curGroup);
            }

            return new AlgebraTerm(finalGroups.ToArray());
        }

        public static AlgebraTerm[] Factorize(ExNumber a, ExNumber b, ExNumber c, ExNumber d, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            // Check if we have a perfect cube.
            if (ExNumber.OpNotEquals(a, 0.0) && ExNumber.OpEqual(b, 0.0) && ExNumber.OpEqual(c, 0.0) && ExNumber.OpNotEquals(d, 0.0))
            {
                AlgebraTerm cubicRootPow = AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(3.0));
                ExComp rootA = PowOp.StaticCombine(a, cubicRootPow);

                bool isNeg = ExNumber.OpLT(d, 0.0);
                d = ExNumber.Abs(d);

                ExComp rootD = PowOp.StaticCombine(d, cubicRootPow);

                ExComp aEx = MulOp.StaticCombine(rootA, solveForComp.ToPow(3.0));

                AgOp op1 = isNeg ? (AgOp)new SubOp() : (AgOp)new AddOp();
                AlgebraTerm cubeFactor1 = new AlgebraTerm(aEx, op1, rootD);

                ExComp aExSq = PowOp.RaiseToPower(aEx, new ExNumber(2.0), ref pEvalData, false);
                ExComp dExSq = PowOp.RaiseToPower(rootD, new ExNumber(2.0), ref pEvalData, false);
                ExComp adEx = MulOp.StaticCombine(aEx, rootD);

                AgOp op2 = isNeg ? (AgOp)new AddOp() : (AgOp)new SubOp();
                AlgebraTerm cubeFactor2 = new AlgebraTerm(aExSq, op2, adEx, new AddOp(), dExSq);

                AlgebraTerm[] perfectCubeFactors = new AlgebraTerm[] { cubeFactor1, cubeFactor2 };

                string dispEq;
                if (!isNeg)
                    dispEq = "a^3+b^3=(a+b)(a^2-ab+b^2)";
                else
                    dispEq = "a^3-b^3=(a-b)(a^2+ab+b^2)";

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}{1}^3+{2}" + WorkMgr.EDM,
                    "The above can be factored from the formula " + WorkMgr.STM + dispEq + WorkMgr.EDM + ".", a, solveFor.GetVar(), d);

                return perfectCubeFactors;
            }

            ExNumber abGcf = ExNumber.GCF(a, b);
            ExNumber cdGcf = ExNumber.GCF(c, d);

            if (abGcf == null || cdGcf == null)
                return null;

            if (ExNumber.OpLT(a, 0.0))
                abGcf = ExNumber.OpMul(abGcf, -1);
            if (ExNumber.OpLT(c, 0.0))
                cdGcf = ExNumber.OpMul(cdGcf, -1);

            ExComp nfA = ExNumber.OpDiv(a, abGcf);
            ExComp nfB = ExNumber.OpDiv(b, abGcf);

            ExComp nfC = ExNumber.OpDiv(c, cdGcf);
            ExComp nfD = ExNumber.OpDiv(d, cdGcf);

            if (!nfA.IsEqualTo(nfC) || !nfB.IsEqualTo(nfD))
                return null;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0}{1}^3+{2}{1}^2)+({3}{1}+{4})" + WorkMgr.EDM,
                "Split the cubic equations into two groups for factoring.", a, solveForComp, b, c, d);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0}{2}^2)({1}{2}+{3})+({4})({5}{2}+{6})" + WorkMgr.EDM,
                "Factor out the greatest term from each of the groups.", abGcf, nfA, solveForComp, nfB, cdGcf, nfC, nfD);

            ExComp aSq = solveForComp.ToPow(2.0);
            ExComp factor1LeadingTerm = MulOp.StaticCombine(abGcf, aSq);
            ExComp factor2LeadingTerm = MulOp.StaticCombine(nfA, solveForComp);

            AlgebraTerm factor1 = new AlgebraTerm(factor1LeadingTerm, new AddOp(), cdGcf);
            AlgebraTerm factor2 = new AlgebraTerm(factor2LeadingTerm, new AddOp(), nfB);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})({1})" + WorkMgr.EDM, "The cubic has now been factored.", factor1, factor2);

            AlgebraTerm[] factors = new AlgebraTerm[] { factor1, factor2 };

            return factors;
        }

        public static AlgebraTerm[] Factorize(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ref EvalData pEvalData, bool showWork)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            pEvalData.AttemptSetInputType(TermType.InputType.FactorQuads);

            if (ExNumber.GetZero().IsEqualTo(c))
            {
                return new AlgebraTerm[]
                {
                    solveForComp.ToAlgTerm(),
                    AddOp.StaticCombine(MulOp.StaticCombine(a, solveForComp), b).ToAlgTerm()
                };
            }

            ExComp ac = MulOp.StaticCombine(a, c);
            List<TypePair<ExComp, ExComp>> divisors = GetDivisorsSignInvariant(ac);
            if (divisors == null)
                return null;

            ExComp n1 = null, n2 = null;
            foreach (TypePair<ExComp, ExComp> divisor in divisors)
            {
                if (AddOp.StaticCombine(divisor.GetData1(), divisor.GetData2()).IsEqualTo(b))
                {
                    n1 = divisor.GetData1();
                    n2 = divisor.GetData2();
                    break;
                }
            }

            if (n1 == null)
            {
                // We didn't find our factors.
                // If the factoring wasn't sucessful be sure not to display any work.
                return null;
            }

            if (showWork)
            {
                pEvalData.GetWorkMgr().FromFormatted(null, "Find all the factors of " + WorkMgr.STM + "{0}" + WorkMgr.EDM + " which add to the " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " to factor the quadratic", ac, b);
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.EDM + "{0}{1}^2+{2}{1}+{3}{1}+{4}" + WorkMgr.EDM, WorkMgr.STM + "{2}" + WorkMgr.EDM + " and " + WorkMgr.STM + "{3}" + WorkMgr.EDM +
                    " add to the B term of " + WorkMgr.STM + "{5}" + WorkMgr.EDM + ", expand the B value to the two found factors.", a, solveForComp, n1, n2, c, b);
            }

            ExComp gcflfg1 = DivOp.GetCommonFactor(a, n1);
            ExComp gcflfg2 = DivOp.GetCommonFactor(n2, c);

            if (gcflfg1 is ExNumber && ExNumber.OpLT((gcflfg1 as ExNumber), 0.0))
                gcflfg1 = ExNumber.OpSub(gcflfg1 as ExNumber);
            if (gcflfg2 is ExNumber && ExNumber.OpLT((gcflfg2 as ExNumber), 0.0))
                gcflfg2 = ExNumber.OpSub(gcflfg2 as ExNumber);

            if (gcflfg1 == null)
                gcflfg1 = ExNumber.GetOne();
            if (gcflfg2 == null)
                gcflfg2 = ExNumber.GetOne();

            if (a is ExNumber && ExNumber.OpLT((a as ExNumber), 0.0))
            {
                gcflfg1 = MulOp.Negate(gcflfg1);
            }
            if (n2 is ExNumber && ExNumber.OpLT((n2 as ExNumber), 0.0))
            {
                gcflfg2 = MulOp.Negate(gcflfg2);
            }

            ExComp lf1 = DivOp.StaticCombine(a, gcflfg1);
            ExComp lf2 = DivOp.StaticCombine(n1, gcflfg1);
            ExComp lf3 = gcflfg1;
            ExComp lf4 = gcflfg2;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}{1}({2}{1}+{3})+{4}({2}{1}+{3})" + WorkMgr.EDM, "Factor out the greatest common term from the two groups.", gcflfg1, solveForComp, lf1, lf2, gcflfg2);

            AlgebraTerm factor1 = new AlgebraTerm();
            if (!ExNumber.GetOne().IsEqualTo(lf1))
                factor1.Add(lf1, new MulOp());
            factor1.Add(solveForComp, new AddOp(), lf2);

            AlgebraTerm factor2 = new AlgebraTerm();
            if (!ExNumber.GetOne().IsEqualTo(lf3))
                factor2.Add(lf3, new MulOp());
            factor2.Add(solveForComp, new AddOp(), lf4);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})({1})" + WorkMgr.EDM, "The expression is now factored.", factor1, factor2);

            AlgebraTerm[] factors = new AlgebraTerm[] { factor1, factor2 };

            return factors;
        }

        public static AlgebraTerm FactorizeTerm(AlgebraTerm term, ref EvalData pEvalData, bool allowComplex)
        {
            if (term.GetAllAlgebraCompsStr().Count == 0)
                return term;

            int startWorkSteps = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            AlgebraTerm[] factors = GetFactors(term, ref pEvalData);

            if (factors != null)
            {
                if (!allowComplex)
                {
                    foreach (AlgebraTerm factor in factors)
                    {
                        if (factor.IsComplex())
                        {
                            pEvalData.GetWorkMgr().PopSteps(startWorkSteps);
                            return term;
                        }
                    }
                }

                if (factors.Length > 2)
                    pEvalData.AttemptSetInputType(TermType.InputType.PolyFactor);
                AlgebraTerm finalTerm = new AlgebraTerm();
                for (int i = 0; i < factors.Length; ++i)
                {
                    finalTerm.Add(factors[i]);
                    if (i != factors.Length - 1)
                        finalTerm.Add(new MulOp());
                }

                return finalTerm;
            }

            pEvalData.GetWorkMgr().PopSteps(startWorkSteps);
            return term;
        }

        public static AlgebraTerm ForceLogCoeffToPow(AlgebraTerm term, AlgebraComp solveForComp)
        {
            List<ExComp[]> finalGroups = new List<ExComp[]>();

            List<ExComp[]> totalGroups = term.GetGroupsNoOps();
            for (int i = 0; i < term.GetGroupCount(); ++i)
            {
                ExComp[] curGroup = totalGroups[i];
                int logCount = 0;
                LogFunction logFunc = null;
                // List of constant coefficients.
                List<ExComp> coeffs = new List<ExComp>();
                for (int j = 0; j < curGroup.Length; ++j)
                {
                    ExComp curGroupComp = curGroup[j];
                    if (curGroupComp is LogFunction)
                    {
                        logCount++;
                        if (logFunc != null)
                            break;
                        logFunc = (LogFunction)curGroupComp;
                    }
                    else
                    {
                        coeffs.Add(curGroupComp);
                    }
                }
                if (logCount == 1 && coeffs.Count != 0)
                {
                    AlgebraTerm coeff = new AlgebraTerm();
                    coeff.AddGroup(coeffs.ToArray());

                    // Move the coefficient to the log's power.
                    ExComp innerEx = logFunc.GetInnerEx();

                    ExComp raisedInnerEx = coeff.IsEqualTo(ExNumber.GetOne()) ? innerEx : PowOp.StaticCombine(innerEx, coeff);

                    LogFunction addLog = new LogFunction(raisedInnerEx);
                    addLog.SetBase(logFunc.GetBase());
                    ExComp[] addLogGroup = new ExComp[] { addLog };
                    finalGroups.Add(addLogGroup);
                    continue;
                }

                finalGroups.Add(curGroup);
            }

            return new AlgebraTerm(finalGroups.ToArray());
        }

        public static AlgebraTerm ForceLogPowToCoeff(LogFunction log)
        {
            AlgebraTerm innerTerm = log.GetInnerTerm();
            List<ExComp[]> groups = innerTerm.GetGroupsNoOps();
            if (groups.Count == 1)
            {
                ExComp[] group = groups[0];

                List<ExNumber> powers = new List<ExNumber>();
                for (int i = 0; i < group.Length; ++i)
                {
                    ExComp groupComp = group[i];
                    if (groupComp is PowerFunction)
                    {
                        PowerFunction pfGroupComp = groupComp as PowerFunction;
                        if (pfGroupComp.GetPower() is ExNumber)
                        {
                            powers.Add(pfGroupComp.GetPower() as ExNumber);
                        }
                        else
                            return log;
                    }
                    else if (groupComp is ExNumber)
                    {
                        ExNumber nGroupComp = groupComp as ExNumber;

                        ExNumber nBase, nPow;
                        nGroupComp.ConvertToLowestBase(out nBase, out nPow);
                        if (nBase != null && nPow != null)
                        {
                            group[i] = new PowerFunction(nBase, nPow);
                            powers.Add(nPow);
                        }
                        else
                            return log;
                    }
                    else
                        return log;
                }

                ExNumber powersGcf = ExNumber.GCF(powers);

                if (powersGcf == null || ExNumber.OpEqual(powersGcf, 1.0) || ExNumber.OpEqual(powersGcf, 0.0))
                    return log;

                for (int i = 0; i < group.Length; ++i)
                {
                    PowerFunction pf = group[i] as PowerFunction;

                    ExNumber nPow = pf.GetPower() as ExNumber;
                    ExComp divResult = ExNumber.OpDiv(nPow, powersGcf);
                    if (ExNumber.GetOne().IsEqualTo(divResult))
                        group[i] = pf.GetBase();
                    else
                        (group[i] as PowerFunction).SetPower(divResult);
                }

                LogFunction finalLog = new LogFunction(GroupHelper.ToAlgTerm(group));
                finalLog.SetBase(log.GetBase());

                return new AlgebraTerm(powersGcf, new MulOp(), finalLog);
            }

            return log;
        }

        public static void GetAdditionalVarFors(AlgebraTerm term, ref Dictionary<string, int> varFors)
        {
            foreach (ExComp subComp in term.GetSubComps())
            {
                if (subComp is AlgebraTerm)
                {
                    GetAdditionalVarFors((subComp as AlgebraTerm), ref varFors);
                }
                if (subComp is FunctionDefinition)
                {
                    AlgebraComp[] inputArgs = (subComp as FunctionDefinition).GetInputArgs();
                    foreach (AlgebraComp inputArg in inputArgs)
                    {
                        if (varFors.ContainsKey(inputArg.GetVar().GetVar()))
                            varFors[inputArg.GetVar().GetVar()] = varFors[inputArg.GetVar().GetVar()] + 1;
                        else
                            varFors.Add(inputArg.GetVar().GetVar(), 1);
                    }
                }
            }
        }

        public static List<TypePair<ExComp, ExComp>> GetDivisors(ExComp ex)
        {
            List<TypePair<ExComp, ExComp>> divisors = null;

            if (ex is PowerFunction)
            {
                PowerFunction fnPow = ex as PowerFunction;
                if (fnPow.GetPower() is ExNumber && (fnPow.GetPower() as ExNumber).IsRealInteger())
                {
                    int pow = (int)(fnPow.GetPower() as ExNumber).GetRealComp();

                    divisors = new List<TypePair<ExComp, ExComp>>();
                    ExNumber lower = ExNumber.GetZero();
                    ExNumber upper = new ExNumber(pow);
                    for (int i = 0; i < pow; ++i)
                    {
                        ExComp div0 = PowOp.StaticWeakCombine(fnPow.GetBase(), lower.CloneEx());
                        ExComp div1 = PowOp.StaticWeakCombine(fnPow.GetBase(), upper.CloneEx());

                        lower = ExNumber.OpAdd(lower, 1.0);
                        upper = ExNumber.OpSub(upper, 1.0);

                        divisors.Add(new TypePair<ExComp, ExComp>(div0, div1));
                    }
                }
            }
            else if (ex is AlgebraComp)
            {
                divisors = new List<TypePair<ExComp, ExComp>>();
                divisors.Add(new TypePair<ExComp, ExComp>(ex, ExNumber.GetOne()));
            }
            else if (ex is AlgebraFunction)
                return null;
            else if (ex is AlgebraTerm)
            {
                return null;
            }
            else if (ex is ExNumber)
            {
                List<TypePair<ExNumber, ExNumber>> numDivisors = (ex as ExNumber).GetDivisors();

                divisors = new List<TypePair<ExComp, ExComp>>();
                for (int i = 0; i < numDivisors.Count; ++i)
                    divisors.Add(new TypePair<ExComp, ExComp>(numDivisors[i].GetData1(), numDivisors[i].GetData2()));
            }

            return divisors;
        }

        public static List<TypePair<ExComp, ExComp>> GetDivisorsSignInvariant(ExComp ex)
        {
            if (ex is ExNumber)
            {
                List<TypePair<ExNumber, ExNumber>> numDivisors = (ex as ExNumber).GetDivisorsSignInvariant();
                List<TypePair<ExComp, ExComp>> exDivisors = new List<TypePair<ExComp, ExComp>>();
                for (int i = 0; i < numDivisors.Count; ++i)
                    exDivisors.Add(new TypePair<ExComp, ExComp>(numDivisors[i].GetData1(), numDivisors[i].GetData2()));
                return exDivisors.ToList();
            }

            List<TypePair<ExComp, ExComp>> signInvariantDivisors = GetDivisors(ex);
            if (signInvariantDivisors == null)
                return null;
            // We have to account for the negative negative situation.
            for (int i = 0; i < signInvariantDivisors.Count; ++i)
            {
                TypePair<ExComp, ExComp> ip = signInvariantDivisors.ElementAt(i);

                // Make it into a negative negative combination.
                // We can safely assume both of these numbers are positive as they are the divisors of a positive number.
                TypePair<ExComp, ExComp> newPair = new TypePair<ExComp, ExComp>();
                newPair.SetData1(MulOp.Negate(ip.GetData1()));
                newPair.SetData2(MulOp.Negate(ip.GetData2()));

                signInvariantDivisors.Insert(i, newPair);

                // Skip over the added entry.
                ++i;
            }

            return signInvariantDivisors;
        }

        public static void GetDomain(AlgebraTerm term, AlgebraSolver agSolver, AlgebraVar varFor, ref List<Restriction> restrictions, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen != null)
            {
                // The denominator can never be zero.
                AlgebraTerm den = numDen[1].CloneEx().ToAlgTerm();

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "The denominator of this term can never be zero so solve for when " + WorkMgr.STM +
                    WorkMgr.ToDisp(numDen[1]) + "=0" + WorkMgr.EDM, term);

                ExComp sol = agSolver.SolveEq(varFor, den, ExNumber.GetZero().ToAlgTerm(), ref pEvalData);
                if (sol == null)
                {
                    restrictions = null;
                    return;
                }

                Restriction[] notIncluding = Restriction.FromAllBut(sol, varFor.ToAlgebraComp());
                // If the restrictions don't already include the notIncluding restriction add it.
                foreach (Restriction potentialAddRest in notIncluding)
                {
                    bool add = true;
                    foreach (Restriction rest in restrictions)
                    {
                        if (Restriction.AreEqualTo(potentialAddRest, rest))
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add)
                        restrictions.Add(potentialAddRest);
                }

                GetDomain(numDen[0], agSolver, varFor, ref restrictions, ref pEvalData);
                if (restrictions == null)
                    return;
                GetDomain(numDen[1], agSolver, varFor, ref restrictions, ref pEvalData);
                if (restrictions == null)
                    return;
            }
            else
            {
                if (term is AlgebraFunction)
                {
                    List<Restriction> addRests = (term as AlgebraFunction).GetDomain(varFor, agSolver, ref pEvalData);
                    if (addRests != null)
                        restrictions.AddRange(addRests);
                    else
                    {
                        // There was a problem with solving.
                        restrictions = null;
                        return;
                    }
                }

                foreach (ExComp subComp in term.GetSubComps())
                {
                    if (subComp is AlgebraTerm)
                    {
                        GetDomain((subComp as AlgebraTerm), agSolver, varFor, ref restrictions, ref pEvalData);
                        if (restrictions == null)
                            return;
                    }
                }
            }

            if (restrictions == null)
                return;

            Restriction compounded = Restriction.CompoundDomains(restrictions, varFor, ref pEvalData);
            if (compounded == null)
            {
                //restrictions.Clear();

                //restrictions.Add(OrRestriction.GetNoRealNumsRestriction(varFor.ToAlgebraComp(), ref pEvalData));
                return;
            }

            List<NotRestriction> notRests = (from rest in restrictions
                            where rest is NotRestriction
                            select rest as NotRestriction).ToList();

            restrictions.Clear();

            // Ensure that all the not restrictions are actually included in the compounded if they aren't then the information is just redundnant.
            foreach (NotRestriction notRest in notRests)
            {
                if (compounded.IsValidValue(notRest.NotVal, ref pEvalData))
                    restrictions.Add(notRest);
            }

            restrictions.Add(compounded);
        }

        public static AlgebraTerm[] GetFactors(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            if (term.RemoveRedundancies(false) is ExNumber)
                return null;

            PolyInfo polyInfos = term.GetPolynomialInfo();
            if (polyInfos != null)
            {
                polyInfos.FillInPowRanges();

                AlgebraTerm[] factors = null;
                if (polyInfos.HasOnlyPowers(2, 1, 0))
                {
                    ExNumber a = polyInfos.GetCoeffForPow(2);
                    ExNumber b = polyInfos.GetCoeffForPow(1);
                    ExNumber c = polyInfos.GetCoeffForPow(0);

                    factors = Factorize(a, b, c, polyInfos.GetVar().GetVar(), ref pEvalData, false);
                }
                else if (polyInfos.HasOnlyPowers(3, 2, 1, 0))
                {
                    ExNumber a = polyInfos.GetCoeffForPow(3);
                    ExNumber b = polyInfos.GetCoeffForPow(2);
                    ExNumber c = polyInfos.GetCoeffForPow(1);
                    ExNumber d = polyInfos.GetCoeffForPow(0);

                    factors = Factorize(a, b, c, d, polyInfos.GetVar().GetVar(), ref pEvalData);
                }

                if (factors != null)
                    return factors;
            }

            ExComp[] gcfGp = term.GetGroupGCF();
            if (gcfGp != null && gcfGp.Length != 0 && !(gcfGp.Length == 1 && (ExNumber.GetOne().IsEqualTo(gcfGp[0]) || ExNumber.GetZero().IsEqualTo(gcfGp[0]))))
            {
                AlgebraTerm gcfTerm = GroupHelper.ToAlgNoRedunTerm(gcfGp);
                if (!term.IsEqualTo(gcfTerm))
                {
                    term = DivOp.FactorOutTerm(term, gcfTerm.CloneEx()).ToAlgTerm();
                    if (!term.IsOne())
                    {
                        AlgebraTerm[] otherFactors = GetFactors(term, ref pEvalData);

                        if (otherFactors != null)
                        {
                            List<AlgebraTerm> factors = ArrayFunc.ToList(otherFactors);
                            factors.Add(gcfTerm);

                            return factors.ToArray();
                        }
                        else
                        {
                            AlgebraTerm[] factors = new AlgebraTerm[] { gcfTerm, term };
                            return factors;
                        }
                    }
                    else
                        term = gcfTerm;
                }
            }

            List<ExComp[]> groups = term.GetGroupsNoOps();
            if (groups.Count == 2)
            {
                #region Perfect Powers

                ExNumber squarePow = new ExNumber(2);
                ExNumber cubePow = new ExNumber(3);

                // Do we have a sum/difference of squares?
                if (IsRaisedToPower(groups[0], squarePow) && IsRaisedToPower(groups[1], squarePow))
                {
                    ExComp[] aGp;
                    ExComp[] bGp;

                    if (GroupHelper.IsNeg(groups[0]))
                    {
                        aGp = groups[1];
                        bGp = groups[0];
                    }
                    else
                    {
                        aGp = groups[0];
                        bGp = groups[1];
                    }

                    if (GroupHelper.IsNeg(bGp))
                    {
                        bGp = AbsValFunction.MakePositive(bGp);

                        // As in the power of 2.
                        ExComp aExTo2 = GroupHelper.ToAlgTerm(aGp).RemoveRedundancies(false);
                        ExComp bExTo2 = GroupHelper.ToAlgTerm(bGp).RemoveRedundancies(false);

                        ExComp aEx = PowOp.TakeRoot(aExTo2, squarePow, ref pEvalData, false);
                        if (aEx is AlgebraTermArray)
                            aEx = (aEx as AlgebraTermArray).GetItem(0);

                        ExComp bEx = PowOp.TakeRoot(bExTo2, squarePow, ref pEvalData, false);
                        if (bEx is AlgebraTermArray)
                            bEx = (bEx as AlgebraTermArray).GetItem(0);

                        AlgebraTerm[] factors =
                            new AlgebraTerm[]
                        {
                            (new AlgebraTerm(aEx, new AddOp(), MulOp.Negate(bEx))),
                            (new AlgebraTerm(aEx, new AddOp(), bEx))
                        };

                        return factors;
                    }
                }
                // Do we have a sum/difference of cubes?
                else if (IsRaisedToPower(groups[0], cubePow) && IsRaisedToPower(groups[1], cubePow))
                {
                    ExComp[] aGp;
                    ExComp[] bGp;

                    if (GroupHelper.IsNeg(groups[0]))
                    {
                        aGp = groups[1];
                        bGp = groups[0];
                    }
                    else
                    {
                        aGp = groups[0];
                        bGp = groups[1];
                    }

                    bool isNeg = GroupHelper.IsNeg(bGp);
                    if (isNeg)
                        bGp = AbsValFunction.MakePositive(bGp);

                    // As in the power of 3.
                    ExComp aExTo3 = GroupHelper.ToAlgTerm(aGp).RemoveRedundancies(false);
                    ExComp bExTo3 = GroupHelper.ToAlgTerm(bGp).RemoveRedundancies(false);

                    ExComp aEx = PowOp.TakeRoot(aExTo3, cubePow, ref pEvalData, false);
                    if (aEx is AlgebraTermArray)
                        aEx = (aEx as AlgebraTermArray).GetItem(0);

                    ExComp bEx = PowOp.TakeRoot(bExTo3, cubePow, ref pEvalData, false);
                    if (bEx is AlgebraTermArray)
                        bEx = (bEx as AlgebraTermArray).GetItem(0);

                    AlgebraTerm[] factors = new AlgebraTerm[2];

                    if (isNeg)
                    {
                        factors[0] = new AlgebraTerm(aEx, new AddOp(), MulOp.Negate(bEx));
                        factors[1] = new AlgebraTerm(PowOp.StaticCombine(aEx, squarePow), new AddOp(), MulOp.StaticCombine(aEx, bEx), new AddOp(), PowOp.StaticCombine(bEx, squarePow));
                    }
                    else
                    {
                        factors[0] = new AlgebraTerm(aEx, new AddOp(), bEx);
                        factors[1] = new AlgebraTerm(PowOp.StaticCombine(aEx, squarePow), new AddOp(), MulOp.Negate(MulOp.StaticCombine(aEx, bEx)), new AddOp(), PowOp.StaticCombine(bEx, squarePow));
                    }

                    return factors;
                }

                #endregion Perfect Powers
            }
            else if (groups.Count == 4)
            {
                // Can we factor by grouping.
                ExComp ex0 = GroupHelper.ToAlgTerm(groups[0]).RemoveRedundancies(false);
                ExComp ex1 = GroupHelper.ToAlgTerm(groups[1]).RemoveRedundancies(false);
                ExComp ex2 = GroupHelper.ToAlgTerm(groups[2]).RemoveRedundancies(false);
                ExComp ex3 = GroupHelper.ToAlgTerm(groups[3]).RemoveRedundancies(false);

                bool is0Neg = GroupHelper.IsNeg(groups[0]);
                bool is1Neg;

                ExComp gcf0 = DivOp.GetCommonFactor(ex0, ex1);
                ExComp gcf1 = DivOp.GetCommonFactor(ex2, ex3);
                if (gcf0 == null || gcf1 == null)
                {
                    gcf0 = DivOp.GetCommonFactor(ex0, ex2);
                    gcf1 = DivOp.GetCommonFactor(ex3, ex1);

                    if (gcf0 == null || gcf1 == null)
                    {
                        gcf0 = DivOp.GetCommonFactor(ex0, ex3);
                        gcf1 = DivOp.GetCommonFactor(ex1, ex2);

                        if (gcf0 == null || gcf1 == null)
                            return null;
                        else
                        {
                            is1Neg = GroupHelper.IsNeg(groups[1]);

                            ExComp ex1Orig = ex1.CloneEx();
                            ExComp ex2Orig = ex2.CloneEx();
                            ExComp ex3Orig = ex3.CloneEx();

                            ex1 = ex3Orig;
                            ex3 = ex2Orig;
                            ex2 = ex1Orig;
                        }
                    }
                    else
                    {
                        is1Neg = GroupHelper.IsNeg(groups[3]);

                        ExComp ex1Orig = ex1.CloneEx();
                        ExComp ex2Orig = ex2.CloneEx();
                        ExComp ex3Orig = ex3.CloneEx();

                        ex1 = ex2Orig;
                        ex2 = ex3Orig;
                        ex3 = ex1Orig;
                    }
                }
                else
                {
                    is1Neg = GroupHelper.IsNeg(groups[2]);
                }

                if (is0Neg)
                    gcf0 = MulOp.Negate(gcf0);
                if (is1Neg)
                    gcf1 = MulOp.Negate(gcf1);

                ex0 = DivOp.StaticCombine(ex0, gcf0);
                if (ex0 is AlgebraTerm)
                    ex0 = (ex0 as AlgebraTerm).RemoveRedundancies(false);

                ex1 = DivOp.StaticCombine(ex1, gcf0);
                if (ex1 is AlgebraTerm)
                    ex1 = (ex1 as AlgebraTerm).RemoveRedundancies(false);

                ex2 = DivOp.StaticCombine(ex2, gcf1);
                if (ex2 is AlgebraTerm)
                    ex2 = (ex2 as AlgebraTerm).RemoveRedundancies(false);

                ex3 = DivOp.StaticCombine(ex3, gcf1);
                if (ex3 is AlgebraTerm)
                    ex3 = (ex3 as AlgebraTerm).RemoveRedundancies(false);

                if (ex0.IsEqualTo(ex2) && ex1.IsEqualTo(ex3))
                {
                    AlgebraTerm[] factors =
                        new AlgebraTerm[]
                    {
                        new AlgebraTerm(gcf0, new AddOp(), gcf1),
                        new AlgebraTerm(ex0, new AddOp(), ex1)
                    };

                    return factors;
                }
            }

            // As a last ditch effort try to synthetic divide
            if (polyInfos != null && polyInfos.GetMaxPow() > 3)
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Using the rational roots theorem attempt to factor the polynomial by dividing by the zeros of the the polynomial.", term);
                Solving.PolynomialSolve polySolve = new Solving.PolynomialSolve(new AlgebraSolver());

                ExComp solutions = polySolve.SolveEquation(term, ExNumber.GetZero().ToAlgTerm(), polyInfos.GetVar().GetVar(), ref pEvalData);

                if (solutions == null)
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Couldn't factor the polynomial through using synthetic division.", term);
                    return null;
                }

                if (solutions is AlgebraTermArray)
                {
                    AlgebraTermArray solutionArray = solutions as AlgebraTermArray;

                    if (pEvalData.GetPartialSolutions() != null && pEvalData.GetPartialSolutions().Count != 0)
                    {
                        AlgebraTerm[] partialSolutions = (from partialSol in pEvalData.GetPartialSolutions()
                                                select partialSol.ToAlgTerm()).ToArray();

                        pEvalData.GetPartialSolutions().Clear();

                        return partialSolutions;
                    }

                    AlgebraTerm[] factorArray = new AlgebraTerm[solutionArray.GetTermCount()];

                    for (int i = 0; i < solutionArray.GetTermCount(); ++i)
                    {
                        ExComp solution = solutionArray.GetItem(i);
                        if (solution is AlgebraTerm)
                        {
                            AlgebraTerm[] numDen = (solution as AlgebraTerm).GetNumDenFrac();
                            if (numDen != null)
                            {
                                factorArray[i] = SubOp.StaticCombine(MulOp.StaticCombine(numDen[1], polyInfos.GetVar()), numDen[0]).ToAlgTerm();
                                continue;
                            }
                        }

                        factorArray[i] = new AlgebraTerm(polyInfos.GetVar(), new AddOp(), solutionArray.GetItem(i));
                    }

                    return factorArray;
                }
            }

            return null;
        }

        public static bool IsSimpleFraction(AlgebraTerm term)
        {
            if (term.ContainsOnlyFractions() && term.GetGroupCount() == 1)
                return true;
            return false;
        }

        public static AlgebraTerm PythagTrigSimplify(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm origTerm = term;
            if (term is PowerFunction)
                term = (term as PowerFunction).GetBase().ToAlgTerm();
            else if (term is AppliedFunction)
                term = (term as AppliedFunction).GetInnerTerm();

            // Apply to all subterms.
            for (int i = 0; i < term.GetSubComps().Count; ++i)
            {
                if (term.GetSubComps()[i] is AlgebraTerm)
                    term.GetSubComps()[i] = PythagTrigSimplify(term.GetSubComps()[i] as AlgebraTerm, ref pEvalData);
            }

            List<ExComp[]> groups = term.GetGroupsNoOps();

            Func<ExComp[], List<TypePair<PowerFunction, ExComp[]>>> getTrigFuncAndCoeff = (ExComp[] gp) =>
                {
                    List<TypePair<PowerFunction, ExComp[]>> trigCoeffPairs = new List<TypePair<PowerFunction, ExComp[]>>();
                    for (int i = 0; i < gp.Length; ++i)
                    {
                        if (gp[i] is PowerFunction && (gp[i] as PowerFunction).GetPower() is ExNumber && ExNumber.OpEqual(((gp[i] as PowerFunction).GetPower() as ExNumber), 2.0))
                        {
                            PowerFunction fnPow = gp[i] as PowerFunction;
                            if (!(fnPow.GetBase() is SinFunction) && !(fnPow.GetBase() is CosFunction))
                                continue;
                            // We have a trig function matching pythag substitution.
                            List<ExComp> prevRange = ArrayFunc.ToList(gp).GetRange(0, i);
                            List<ExComp> afterRange = ArrayFunc.ToList(gp).GetRange(i + 1, gp.Length - (i + 1));
                            if (prevRange.Count + afterRange.Count == 0)
                                afterRange.Add(ExNumber.GetOne());
                            trigCoeffPairs.Add(new TypePair<PowerFunction, ExComp[]>(gp[i] as PowerFunction, prevRange.Concat(afterRange).ToArray()));
                        }
                    }

                    return trigCoeffPairs;
                };

            Func<ExComp[], ExComp, ExComp, AlgebraTerm, string, TypePair<PowerFunction, ExComp[]>> getTrigFuncAndCoeffRestrictions = (ExComp[] gp, ExComp reqPow,
                ExComp innerEx, AlgebraTerm coeff, string reqTrigIden) =>
            {
                for (int i = 0; i < gp.Length; ++i)
                {
                    if (gp[i] is PowerFunction &&
                        (gp[i] as PowerFunction).GetPower() is ExNumber &&
                        (gp[i] as PowerFunction).GetPower().IsEqualTo(reqPow) &&
                        ExNumber.OpEqual(ExNumber.OpMod(((gp[i] as PowerFunction).GetPower() as ExNumber), 2), 0) &&
                        TrigFunction.GetTrigType(gp[i]) == reqTrigIden)
                    {
                        PowerFunction fnPow = gp[i] as PowerFunction;
                        TrigFunction fnBaseTrig = fnPow.GetBase() as TrigFunction;
                        if (!fnBaseTrig.GetInnerEx().IsEqualTo(innerEx))
                            continue;
                        // We have a trig function matching pythag substitution.
                        List<ExComp> prevRange = ArrayFunc.ToList(gp).GetRange(0, i);
                        List<ExComp> afterRange = ArrayFunc.ToList(gp).GetRange(i + 1, gp.Length - (i + 1));
                        if (prevRange.Count + afterRange.Count == 0)
                            afterRange.Add(ExNumber.GetOne());
                        ExComp[] totalRange = prevRange.Concat(afterRange).ToArray();
                        if (!GroupHelper.ToAlgNoRedunTerm(totalRange).IsEqualTo(coeff))
                            continue;

                        return new TypePair<PowerFunction, ExComp[]>(gp[i] as PowerFunction, prevRange.Concat(afterRange).ToArray());
                    }
                }

                return null;
            };

            // Replace sin^2(x)+cos^2(x) with 1

            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];

                List<TypePair<PowerFunction, ExComp[]>> trigCoeffPairs = getTrigFuncAndCoeff(group);

                for (int j = 0; j < trigCoeffPairs.Count; ++j)
                {
                    TypePair<PowerFunction, ExComp[]> trigCoeffPair = trigCoeffPairs[j];
                    string desiredTrigFunc = TrigFunction.GetTrigType(trigCoeffPair.GetData1().GetBase()) == SinFunction.IDEN ? CosFunction.IDEN : SinFunction.IDEN;
                    ExComp innerEx = (trigCoeffPair.GetData1().GetBase() as TrigFunction).GetInnerEx();

                    bool breakLoop = false;

                    // See if we have any matching throughout the groups.
                    for (int k = i + 1; k < groups.Count; ++k)
                    {
                        ExComp[] compareGroup = groups[k];

                        TypePair<PowerFunction, ExComp[]> compareTrigCoeffPair = getTrigFuncAndCoeffRestrictions(compareGroup, trigCoeffPair.GetData1().GetPower(), innerEx,
                            GroupHelper.ToAlgNoRedunTerm(trigCoeffPair.GetData2()), desiredTrigFunc);

                        if (compareTrigCoeffPair == null)
                            continue;

                        groups[i] = trigCoeffPair.GetData2();

                        ArrayFunc.RemoveIndex(groups, k);
                        breakLoop = true;
                        break;
                    }

                    if (breakLoop)
                        break;
                }
            }

            // Replace sin^2(x)-1 with cos^2(x)

            // Replace cos^2(x)-1 with sin^2(x)

            if (origTerm is PowerFunction)
                return PowOp.StaticCombine(new AlgebraTerm(groups.ToArray()), (origTerm as PowerFunction).GetPower()).ToAlgTerm();

            origTerm.SetSubCompsGps(groups);

            return origTerm;
        }

        public static ExComp RaiseToPow(ExComp term, ExNumber power, ref TermType.EvalData pEvalData)
        {
            return PowOp.RaiseToPower(term, power, ref pEvalData, false);
        }

        public static AlgebraTerm TrigSimplify(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            term = TrigSimplifyCombine(term, ref pEvalData);
            term = PythagTrigSimplify(term, ref pEvalData);

            return term;
        }

        private static ExComp[] CancelTrigTerms(ExComp[] group, ref TermType.EvalData pEvalData)
        {
            ExComp[] den = GroupHelper.GetDenominator(group, false);
            if (den != null && den.Length != 0)
            {
                // There is a denominator in this group.
                ExComp[] num = GroupHelper.GetNumerator(group);
                AlgebraTerm numDenResult = TrigSimplify(AlgebraTerm.FromFraction(GroupHelper.ToAlgTerm(num), GroupHelper.ToAlgTerm(den)), ref pEvalData);
                List<ExComp[]> numDenResultGroups = numDenResult.GetGroups();
                if (numDenResultGroups.Count == 1)
                    return numDenResultGroups[0];
                else
                {
                    ExComp[] singularGp = new ExComp[] { numDenResult };
                    return singularGp;
                }
            }

            List<ExComp> checkGroup = ArrayFunc.ToList(group);
            List<ExComp> finalGroup = new List<ExComp>();
            for (int i = 0; i < checkGroup.Count; ++i)
            {
                ExComp groupComp = group[i];

                int j;
                for (j = i + 1; j < checkGroup.Count; ++j)
                {
                    ExComp compareGroupComp = group[j];
                    ExComp compareResult = TrigFunction.TrigCancel(groupComp, compareGroupComp, false);
                    if (compareResult != null)
                    {
                        finalGroup.Add(compareResult);
                        ArrayFunc.RemoveIndex(checkGroup, i--);
                        ArrayFunc.RemoveIndex(checkGroup, --j);
                        break;
                    }
                }

                if (j == group.Length)
                {
                    // A match was not found.
                    finalGroup.Add(group[i]);
                }
            }

            return finalGroup.ToArray();
        }

        private static ExComp CompoundTrigFuncs(List<ExComp[]> groups)
        {
            // Convert all of the trig functions to non reciprocal trig funcs.
            for (int i = 0; i < groups.Count; ++i)
            {
                for (int j = 0; j < groups[i].Length; ++j)
                {
                    if (TrigFunction.IsRecipTrigFunc(groups[i][j] as TrigFunction))
                        groups[i][j] = AlgebraTerm.FromFraction(ExNumber.GetOne(), (groups[i][j] as TrigFunction).GetReciprocalOf());
                }
            }

            AlgebraTerm totalTerm = new AlgebraTerm(groups.ToArray());
            totalTerm = totalTerm.CompoundFractions();
            return totalTerm.RemoveRedundancies(false);
        }

        private static List<ExComp[]> ExpandLogFunctionInners(ExComp logInnerEx, ExComp baseEx)
        {
            List<ExComp[]> expandedLogs = new List<ExComp[]>();

            ExComp mulIn = null;
            if (logInnerEx is AlgebraTerm)
            {
                AlgebraTerm innerTerm = logInnerEx as AlgebraTerm;
                List<ExComp[]> groups = innerTerm.GetGroupsNoOps();
                if (groups.Count == 1)
                {
                    ExComp[] group = groups[0];
                    foreach (ExComp groupComp in group)
                    {
                        ExComp[] addGroup;
                        if (groupComp is PowerFunction)
                        {
                            PowerFunction pfGroupComp = groupComp as PowerFunction;
                            if (pfGroupComp.IsDenominator())
                            {
                                ExComp flipped = pfGroupComp.FlipFrac();
                                addGroup = new ExComp[2];
                                LogFunction negLog = new LogFunction(flipped);
                                negLog.SetBase(baseEx);
                                addGroup[0] = ExNumber.GetNegOne();
                                addGroup[1] = negLog;
                                expandedLogs.Add(addGroup);
                                continue;
                            }
                        }
                        else if (groupComp is ExNumber && !(groupComp as ExNumber).HasImaginaryComp() && ExNumber.OpLT((groupComp as ExNumber), 0.0))
                        {
                            if (expandedLogs.Count != 0)
                            {
                                ExComp[] tmpGp = expandedLogs[0];
                                for (int i = 0; i < tmpGp.Length; ++i)
                                {
                                    if (tmpGp[i] is LogFunction)
                                    {
                                        (tmpGp[i] as LogFunction).SetBase(MulOp.StaticCombine((tmpGp[i] as LogFunction).GetBase(), groupComp));
                                        break;
                                    }
                                }
                            }
                            else
                                mulIn = groupComp;

                            continue;
                        }

                        ExComp innerEx;
                        if (mulIn != null)
                        {
                            innerEx = MulOp.StaticCombine(groupComp, mulIn);
                            mulIn = null;
                        }
                        else
                            innerEx = groupComp;

                        LogFunction posLog = new LogFunction(innerEx);
                        posLog.SetBase(baseEx);

                        addGroup = new ExComp[1];
                        addGroup[0] = posLog;

                        expandedLogs.Add(addGroup);
                    }
                }

                if (mulIn != null)
                {
                    expandedLogs.Add(new ExComp[] { LogFunction.Create(mulIn, baseEx) });
                }

                return expandedLogs;
            }
            LogFunction singleLogFunc = new LogFunction(logInnerEx);
            singleLogFunc.SetBase(baseEx);

            ExComp[] singleGroup = new ExComp[] { singleLogFunc };
            expandedLogs.Add(singleGroup);
            return expandedLogs;
        }

        private static bool IsRaisedToPower(ExComp[] gp, ExNumber pow)
        {
            if (ExNumber.OpLT(pow, 2.0))
                throw new ArgumentException();

            foreach (ExComp gpCmp in gp)
            {
                if (gpCmp is AlgebraComp)
                    return false;
                else if (gpCmp is PowerFunction)
                {
                    PowerFunction pfGpCmp = gpCmp as PowerFunction;
                    if (pfGpCmp.GetPower() is ExNumber)
                    {
                        ExNumber pfGpCmpPow = pfGpCmp.GetPower() as ExNumber;
                        if (ExNumber.OpNotEquals(ExNumber.OpMod(pfGpCmpPow, pow), 0))
                            return false;
                    }
                    else
                        return false;
                }
                else if (gpCmp is AlgebraTerm)
                    return false;
            }

            return true;
        }

        private static AlgebraTerm TrigSimplifyCombine(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm numTerm;

            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen != null)
            {
                numTerm = numDen[0];
            }
            else
                numTerm = term;

            List<ExComp[]> numGroups = numTerm.GetGroupsNoOps();

            for (int i = 0; i < numGroups.Count; ++i)
            {
                numGroups[i] = CancelTrigTerms(numGroups[i], ref pEvalData);
            }

            if (numDen != null)
            {
                List<ExComp[]> denGroups = numDen[1].GetGroupsNoOps();

                for (int i = 0; i < denGroups.Count; ++i)
                {
                    denGroups[i] = CancelTrigTerms(denGroups[i], ref pEvalData);
                }

                if (denGroups.Count == 1)
                {
                    #region Cancel Numerator denominator

                    // There is a possibility some of the terms will cancel.
                    List<ExComp>[] modifiedDens = new List<ExComp>[numGroups.Count];

                    for (int i = 0; i < numGroups.Count; ++i)
                    {
                        modifiedDens[i] = null;

                        for (int j = 0; j < numGroups[i].Length; ++j)
                        {
                            ExComp numGroupComp = numGroups[i][j];

                            if (numGroupComp is AgOp)
                                continue;

                            List<ExComp> useDenGroup = modifiedDens[i] == null ? ArrayFunc.ToList(denGroups[0]) : modifiedDens[i];
                            for (int k = 0; k < useDenGroup.Count; ++k)
                            {
                                if (useDenGroup[k] is AgOp)
                                    continue;

                                ExComp cancelResult = TrigFunction.TrigCancel(numGroupComp, useDenGroup[k], true);
                                if (cancelResult != null)
                                {
                                    // The cancel result will stay on the numerator.
                                    numGroups[i][j] = cancelResult;
                                    ArrayFunc.RemoveIndex(useDenGroup, k--);
                                    if (modifiedDens[i] == null)
                                    {
                                        if (useDenGroup.Count == 0)
                                            useDenGroup.Add(new ExNumber(1.0));
                                        modifiedDens[i] = useDenGroup;
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < numGroups.Count; ++i)
                    {
                        numGroups[i] = CancelTrigTerms(numGroups[i], ref pEvalData);
                    }

                    // Some of the denominators might have been modified.
                    List<TypePair<ExComp, ExComp>> modifiedNumDenPairs = new List<TypePair<ExComp, ExComp>>();

                    for (int i = 0; i < modifiedDens.Length; ++i)
                    {
                        if (modifiedDens[i] != null)
                        {
                            modifiedNumDenPairs.Add(new TypePair<ExComp, ExComp>(
                                GroupHelper.ToAlgTerm(numGroups[i]), GroupHelper.ToAlgTerm(modifiedDens[i].ToArray())));
                        }
                    }

                    // Some of these terms might have the same denominators if so combine them.
                    for (int i = 0; i < modifiedNumDenPairs.Count; ++i)
                    {
                        for (int j = i + 1; j < modifiedNumDenPairs.Count; ++j)
                        {
                            if (modifiedNumDenPairs[i].GetData2().IsEqualTo(modifiedNumDenPairs[j].GetData2()))
                            {
                                modifiedNumDenPairs[j].SetData1(AddOp.StaticCombine(modifiedNumDenPairs[i].GetData1(), modifiedNumDenPairs[j].GetData1()));
                                ArrayFunc.RemoveIndex(modifiedNumDenPairs, i--);
                                break;
                            }
                        }
                    }

                    // Move all the trig terms to the numerator.
                    for (int i = 0; i < modifiedNumDenPairs.Count; ++i)
                    {
                        ExComp modDen = modifiedNumDenPairs[i].GetData2();
                        AlgebraTerm topMultTerm = new AlgebraTerm();
                        if (modDen is AlgebraTerm)
                        {
                            AlgebraTerm modDenTerm = modDen as AlgebraTerm;
                            List<ExComp[]> modDenTermGps = modDenTerm.GetGroupsNoOps();
                            if (modDenTermGps.Count == 1)
                            {
                                List<ExComp> gp = ArrayFunc.ToList(modDenTermGps[0]);
                                for (int j = 0; j < gp.Count; ++j)
                                {
                                    if (modDenTerm[j] is TrigFunction)
                                    {
                                        topMultTerm.Add((modDenTerm[j] as TrigFunction).GetReciprocalOf(), new MulOp());
                                        ArrayFunc.RemoveIndex(gp, j);
                                    }
                                }

                                // Remove the tailing operator if there is one.
                                if (gp.Count > 0 && gp[gp.Count - 1] is AgOp)
                                    ArrayFunc.RemoveIndex(gp, gp.Count - 1);

                                modifiedNumDenPairs[i].SetData2(GroupHelper.ToAlgTerm(gp.ToArray()));
                            }
                        }
                        else if (modDen is TrigFunction)
                        {
                            topMultTerm.Add((modDen as TrigFunction).GetReciprocalOf());
                            modifiedNumDenPairs[i].SetData2(new AlgebraTerm());
                        }

                        if (topMultTerm.GetTermCount() > 0)
                        {
                            modifiedNumDenPairs[i].SetData1(MulOp.StaticWeakCombine(modifiedNumDenPairs[i].GetData1(), topMultTerm));
                        }
                    }

                    List<ExComp> nonModifiedNums = new List<ExComp>();
                    for (int i = 0; i < modifiedDens.Length; ++i)
                    {
                        if (modifiedDens[i] == null)
                        {
                            nonModifiedNums.Add(GroupHelper.ToAlgTerm(numGroups[i]));
                        }
                    }

                    AlgebraTerm finalAlgTerm = new AlgebraTerm();
                    AlgebraTerm nonModNum = new AlgebraTerm();
                    foreach (ExComp nonModifiedNum in nonModifiedNums)
                    {
                        nonModNum.Add(nonModifiedNum, new AddOp());
                    }

                    // Remove the last '+' operator.
                    if (nonModNum.GetTermCount() > 0)
                    {
                        ArrayFunc.RemoveIndex(nonModNum.GetSubComps(), nonModNum.GetTermCount() - 1);

                        AlgebraTerm topMultTerm = new AlgebraTerm();

                        List<ExComp> singleDenGp = ArrayFunc.ToList(denGroups[0]);
                        for (int i = 0; i < singleDenGp.Count; ++i)
                        {
                            if (singleDenGp[i] is TrigFunction)
                            {
                                topMultTerm.Add((denGroups[0][i] as TrigFunction).GetReciprocalOf(), new MulOp());
                                ArrayFunc.RemoveIndex(singleDenGp, i);
                            }
                        }

                        if (topMultTerm.GetTermCount() > 0)
                        {
                            ArrayFunc.RemoveIndex(topMultTerm.GetSubComps(), topMultTerm.GetTermCount() - 1);
                            nonModNum = MulOp.StaticWeakCombine(topMultTerm, nonModNum).ToAlgTerm();
                        }

                        if (singleDenGp.Count > 0)
                            finalAlgTerm.Add(AlgebraTerm.FromFraction(nonModNum, GroupHelper.ToAlgTerm(singleDenGp.ToArray())));
                        else
                            finalAlgTerm.Add(nonModNum);
                    }

                    foreach (TypePair<ExComp, ExComp> modNumDenPair in modifiedNumDenPairs)
                    {
                        ExComp addEx;
                        if (modNumDenPair.GetData2() is AlgebraTerm && (modNumDenPair.GetData2() as AlgebraTerm).GetTermCount() == 0)
                            addEx = modNumDenPair.GetData1();
                        else
                            addEx = AlgebraTerm.FromFraction(modNumDenPair.GetData1(), modNumDenPair.GetData2());
                        if (finalAlgTerm.GetTermCount() > 0)
                            finalAlgTerm.Add(new AddOp());
                        finalAlgTerm.Add(addEx);
                    }

                    finalAlgTerm = finalAlgTerm.ApplyOrderOfOperations();
                    finalAlgTerm = finalAlgTerm.MakeWorkable().ToAlgTerm();

                    return finalAlgTerm;

                    #endregion Cancel Numerator denominator
                }
                else
                {
                    ExComp num = numDen[0];
                    if (numGroups.Count > 1)
                        num = CompoundTrigFuncs(numGroups);
                    ExComp den = CompoundTrigFuncs(denGroups);
                    if (!(den is AlgebraTerm && (den as AlgebraTerm).GetGroupCount() != 1) &&
                        !(num is AlgebraTerm && (num as AlgebraTerm).GetGroupCount() != 1))
                    {
                        return TrigSimplify(AlgebraTerm.FromFraction(num, den), ref pEvalData);
                    }
                }

                return AlgebraTerm.FromFraction(new AlgebraTerm(numGroups.ToArray()), new AlgebraTerm(denGroups.ToArray()));
            }

            return new AlgebraTerm(numGroups.ToArray());
        }
    }
}