using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Term
{
    internal static class AdvAlgebraTerm
    {
        public static AlgebraTerm AbsValToParas(this AlgebraTerm term)
        {
            for (int i = 0; i < term.TermCount; ++i)
            {
                if (term[i] is AbsValFunction)
                {
                    term[i] = new AlgebraTerm((term[i] as AbsValFunction).InnerEx);
                }
                else if (term[i] is AlgebraTerm && !(term is AlgebraFunction))
                {
                    term[i] = (term[i] as AlgebraTerm).AbsValToParas();
                }
            }

            return term;
        }


        public static bool ContainsOneOfFuncs(this AlgebraTerm term, params Type[] types)
        {
            if (types.Contains(term.GetType()))
                return true;

            for (int i = 0; i < term.SubComps.Count; ++i)
            {
                if (types.Contains(term.SubComps[i].GetType()))
                    return true;

                if (term.SubComps[i] is AlgebraTerm && (term.SubComps[i] as AlgebraTerm).ContainsOneOfFuncs(types))
                    return true;
            }

            return false;
        }

        public static AlgebraTerm CompoundLogs(this AlgebraTerm term, AlgebraComp solveForComp = null)
        {
            bool garbage;
            return CompoundLogs(term, out garbage, solveForComp);
        }

        public static AlgebraTerm CompoundLogs(this AlgebraTerm term, out bool hasCombined, AlgebraComp solveForComp = null)
        {
            hasCombined = false;
            int varLogCount = -1;
            if (solveForComp != null)
            {
                varLogCount = term.GetAppliedFuncCount(solveForComp, FunctionType.Logarithm);
            }

            term = term.RemoveRedundancies().ToAlgTerm();

            List<ExComp[]> finalGroups = new List<ExComp[]>();

            List<LogFunction> logFuncs = new List<LogFunction>();
            List<ExComp[]> totalGroups = term.GetGroupsNoOps();
            for (int i = 0; i < term.GroupCount; ++i)
            {
                ExComp[] curGroup = totalGroups[i];
                if (curGroup.Count() == 1 && curGroup[0] is LogFunction)
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
                for (int j = 0; j < curGroup.Count(); ++j)
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
                    ExComp innerEx = logFunc.InnerEx;

                    if (solveForComp != null &&
                        (innerEx.IsEqualTo(solveForComp) || (innerEx is AlgebraTerm && (innerEx as AlgebraTerm).Contains(solveForComp)))
                        && varLogCount == 1)
                    {
                        // We also know that this log is alone and shouldn't combine with other logs.
                        finalGroups.Add(curGroup);
                        continue;
                    }

                    // The coeff doesn't include the var term.
                    ExComp raisedInnerEx = coeff.IsEqualTo(Number.One) ? innerEx : PowOp.StaticCombine(innerEx, coeff);

                    LogFunction addLog = new LogFunction(raisedInnerEx);
                    addLog.Base = logFunc.Base;

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
                combinedLogs.RemoveAt(varLogIndex);

            foreach (LogFunction logFunc in logFuncs)
            {
                bool found = false;
                for (int i = 0; i < combinedLogs.Count; ++i)
                {
                    LogFunction combinedLog = combinedLogs[i];
                    if (combinedLog.Base.IsEqualTo(logFunc.Base))
                    {
                        found = true;
                        ExComp combInner = MulOp.StaticCombine(logFunc.InnerEx, combinedLog.InnerEx);
                        hasCombined = true;
                        combinedLogs[i] = new LogFunction(combInner);
                        combinedLogs[i].Base = logFunc.Base;
                    }
                }

                if (!found)
                {
                    combinedLogs.Add(logFunc);
                }
            }

            foreach (LogFunction combinedLog in combinedLogs)
            {
                ExComp[] addGroup = { combinedLog };
                finalGroups.Add(addGroup);
            }

            return new AlgebraTerm(finalGroups.ToArray());
        }

        public static ExComp EvaluateExponentsCompletely(this AlgebraTerm term)
        {
            if (term is PowerFunction)
            {
                PowerFunction powFunc = term as PowerFunction;
                AlgebraTerm baseTerm = powFunc.Base.ToAlgTerm();
                var groups = baseTerm.GetGroupsNoOps();
                if (groups.Count == 1)
                {
                    var group = groups[0];
                    AlgebraTerm reconstructedTerm = new AlgebraTerm();
                    for (int i = 0; i < group.Length; ++i)
                    {
                        reconstructedTerm.Add(PowOp.StaticWeakCombine(group[i], powFunc.Power));
                        if (i != group.Length - 1)
                            reconstructedTerm.Add(new MulOp());
                    }

                    return reconstructedTerm;
                }
            }

            for (int i = 0; i < term.TermCount; ++i)
            {
                if (term[i] is AlgebraTerm)
                {
                    term[i] = (term[i] as AlgebraTerm).EvaluateExponentsCompletely();
                }
            }

            return term;
        }

        public static AlgebraTerm EvaluatePowers(this AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            term = term.RemoveRedundancies().ToAlgTerm();

            if (term is PowerFunction)
            {
                PowerFunction powFunc = term as PowerFunction;

                if (powFunc.Power is Number && powFunc.Base is AlgebraTerm)
                {
                    if (powFunc.Base.ToAlgTerm().TermCount == 1)
                    {
                        if (powFunc.Base is PowerFunction && !(powFunc.Base as PowerFunction).Power.IsEqualTo(Number.NegOne) &&
                            !(powFunc.Power.IsEqualTo(Number.NegOne)))
                        {
                            term = new PowerFunction((powFunc.Base as PowerFunction).Base,
                                MulOp.StaticCombine(powFunc.Power, (powFunc.Base as PowerFunction).Power));
                            return term;
                        }
                    }

                    Number powNum = powFunc.Power as Number;
                    bool makeDen = false;
                    if (powNum.IsRealInteger() && powNum < 0.0)
                    {
                        makeDen = true;
                    }
                    powNum = Number.Abs(powNum);

                    term = powFunc.Base.RaiseToPow(powNum, ref pEvalData).ToAlgTerm();

                    if (makeDen)
                    {
                        term = PowOp.StaticWeakCombine(term, Number.NegOne).ToAlgTerm();
                    }
                }
                else if (powFunc.Power is Number && powFunc.Base is Number && !(powFunc.Power as Number).HasImaginaryComp() && !(powFunc.Base as Number).HasImaginaryComp())
                {
                    Number nPowFuncPow = powFunc.Power as Number;
                    Number nPowFuncBase = powFunc.Base as Number;

                    Number result = nPowFuncBase ^ nPowFuncPow;
                    if (result.IsRealInteger())
                        term = result.ToAlgTerm();
                }
                else if (powFunc.Power is Number && powFunc.Base is Number && !(powFunc.Power as Number).HasImaginaryComp())
                    return PowOp.StaticCombine(powFunc.Base, powFunc.Power).ToAlgTerm();
            }

            for (int i = 0; i < term.TermCount; ++i)
            {
                if (term[i] is AlgebraTerm)
                {
                    term[i] = (term[i] as AlgebraTerm).EvaluatePowers(ref pEvalData);
                }
            }

            return term;
        }

        //TODO:
        // There's a good chance this won't work.
        public static AlgebraTerm ExpandLogs(this AlgebraTerm term)
        {
            term = term.RemoveRedundancies().ToAlgTerm();

            List<ExComp[]> finalGroups = new List<ExComp[]>();

            List<LogFunction> logFuncs = new List<LogFunction>();
            List<ExComp[]> totalGroups = term.GetGroupsNoOps();
            for (int i = 0; i < term.GroupCount; ++i)
            {
                ExComp[] curGroup = totalGroups[i];
                for (int j = 0; j < curGroup.Count(); ++j)
                {
                    ExComp curGroupComp = curGroup[j];
                    if (curGroupComp is LogFunction)
                    {
                        LogFunction toExpand = curGroupComp as LogFunction;
                        List<ExComp[]> expanded = ExpandLogFunctionInners(toExpand.InnerEx, toExpand.Base);
                        AlgebraTerm expandedTerm = new AlgebraTerm(expanded.ToArray());
                        curGroup[j] = expandedTerm;
                    }
                }

                finalGroups.Add(curGroup);
            }

            return new AlgebraTerm(finalGroups.ToArray());
        }

        public static AlgebraTerm[] Factorize(Number a, Number b, Number c, Number d, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            // Check if we have a perfect cube.
            if (a != 0.0 && b == 0.0 && c == 0.0 && d != 0.0)
            {
                AlgebraTerm cubicRootPow = AlgebraTerm.FromFraction(Number.One, new Number(3.0));
                ExComp rootA = PowOp.StaticCombine(a, cubicRootPow);

                bool isNeg = d < 0.0;
                d = Number.Abs(d);

                ExComp rootD = PowOp.StaticCombine(d, cubicRootPow);

                ExComp aEx = MulOp.StaticCombine(rootA, solveForComp.ToPow(3.0));

                AgOp op1 = isNeg ? (AgOp)new SubOp() : (AgOp)new AddOp();
                AlgebraTerm cubeFactor1 = new AlgebraTerm(aEx, op1, rootD);

                ExComp aExSq = PowOp.RaiseToPower(aEx, new Number(2.0), ref pEvalData);
                ExComp dExSq = PowOp.RaiseToPower(rootD, new Number(2.0), ref pEvalData);
                ExComp adEx = MulOp.StaticCombine(aEx, rootD);

                AgOp op2 = isNeg ? (AgOp)new AddOp() : (AgOp)new SubOp();
                AlgebraTerm cubeFactor2 = new AlgebraTerm(aExSq, op2, adEx, new AddOp(), dExSq);

                AlgebraTerm[] perfectCubeFactors = { cubeFactor1, cubeFactor2 };

                string dispEq;
                if (!isNeg)
                    dispEq = "a^3+b^3=(a+b)(a^2-ab+b^2)";
                else
                    dispEq = "a^3-b^3=(a-b)(a^2+ab+b^2)";

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}{1}^3+{2}" + WorkMgr.EDM,
                    "The above can be factored from the formula " + WorkMgr.STM + dispEq + WorkMgr.EDM + ".", a, solveFor.Var, d);

                return perfectCubeFactors;
            }

            Number abGcf = Number.GCF(a, b);
            Number cdGcf = Number.GCF(c, d);

            if (abGcf == null || cdGcf == null)
                return null;

            if (a < 0.0)
                abGcf *= -1;
            if (c < 0.0)
                cdGcf *= -1;

            ExComp nfA = a / abGcf;
            ExComp nfB = b / abGcf;

            ExComp nfC = c / cdGcf;
            ExComp nfD = d / cdGcf;

            if (!nfA.IsEqualTo(nfC) || !nfB.IsEqualTo(nfD))
                return null;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0}{1}^3+{2}{1}^2)+({3}{1}+{4})" + WorkMgr.EDM,
                "Split the cubic equations into two groups for factoring.", a, solveForComp, b, c, d);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0}{2}^2)({1}{2}+{3})+({4})({5}{2}+{6})" + WorkMgr.EDM,
                "Factor out the greatest term from each of the groups.", abGcf, nfA, solveForComp, nfB, cdGcf, nfC, nfD);

            ExComp aSq = solveForComp.ToPow(2.0);
            ExComp factor1LeadingTerm = MulOp.StaticCombine(abGcf, aSq);
            ExComp factor2LeadingTerm = MulOp.StaticCombine(nfA, solveForComp);

            AlgebraTerm factor1 = new AlgebraTerm(factor1LeadingTerm, new AddOp(), cdGcf);
            AlgebraTerm factor2 = new AlgebraTerm(factor2LeadingTerm, new AddOp(), nfB);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0})({1})" + WorkMgr.EDM, "The cubic has now been factored.", factor1, factor2);

            AlgebraTerm[] factors = { factor1, factor2 };

            return factors;
        }

        public static AlgebraTerm[] Factorize(ExComp a, ExComp b, ExComp c, AlgebraVar solveFor, ref TermType.EvalData pEvalData, bool showWork = false)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            pEvalData.AttemptSetInputType(TermType.InputType.FactorQuads);

            if (Number.Zero.IsEqualTo(c))
            {
                return new AlgebraTerm[] 
                { 
                    solveForComp.ToAlgTerm(), 
                    AddOp.StaticCombine(MulOp.StaticCombine(a, solveForComp), b).ToAlgTerm() 
                };
            }

            ExComp ac = MulOp.StaticCombine(a, c);
            var divisors = ac.GetDivisorsSignInvariant();
            if (divisors == null)
                return null;

            ExComp n1 = null, n2 = null;
            foreach (var divisor in divisors)
            {
                if (AddOp.StaticCombine(divisor.Data1, divisor.Data2).IsEqualTo(b))
                {
                    n1 = divisor.Data1;
                    n2 = divisor.Data2;
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
                pEvalData.WorkMgr.FromFormatted(null, "Find all the factors of " + WorkMgr.STM + "{0}" + WorkMgr.EDM + " which add to the " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " to factor the quadratic", ac, b);
                pEvalData.WorkMgr.FromFormatted(WorkMgr.EDM + "{0}{1}^2+{2}{1}+{3}{1}+{4}" + WorkMgr.EDM, WorkMgr.STM + "{2}" + WorkMgr.EDM + " and " + WorkMgr.STM + "{3}" + WorkMgr.EDM +
                    " add to the B term of " + WorkMgr.STM + "{5}" + WorkMgr.EDM + ", expand the B value to the two found factors.", a, solveForComp, n1, n2, c, b);
            }

            ExComp gcflfg1 = DivOp.GetCommonFactor(a, n1);
            ExComp gcflfg2 = DivOp.GetCommonFactor(n2, c);

            if (gcflfg1 is Number && (gcflfg1 as Number) < 0.0)
                gcflfg1 = -(gcflfg1 as Number);
            if (gcflfg2 is Number && (gcflfg2 as Number) < 0.0)
                gcflfg2 = -(gcflfg2 as Number);

            if (gcflfg1 == null)
                gcflfg1 = Number.One;
            if (gcflfg2 == null)
                gcflfg2 = Number.One;

            if (a is Number && (a as Number) < 0.0)
            {
                gcflfg1 = MulOp.Negate(gcflfg1);
            }
            if (n2 is Number && (n2 as Number) < 0.0)
            {
                gcflfg2 = MulOp.Negate(gcflfg2);
            }

            ExComp lf1 = DivOp.StaticCombine(a, gcflfg1);
            ExComp lf2 = DivOp.StaticCombine(n1, gcflfg1);
            ExComp lf3 = gcflfg1;
            ExComp lf4 = gcflfg2;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}{1}({2}{1}+{3})+{4}({2}{1}+{3})" + WorkMgr.EDM, "Factor out the greatest common term from the two groups.", gcflfg1, solveForComp, lf1, lf2, gcflfg2);

            AlgebraTerm factor1 = new AlgebraTerm();
            if (!Number.One.IsEqualTo(lf1))
                factor1.Add(lf1, new MulOp());
            factor1.Add(solveForComp, new AddOp(), lf2);

            AlgebraTerm factor2 = new AlgebraTerm();
            if (!Number.One.IsEqualTo(lf3))
                factor2.Add(lf3, new MulOp());
            factor2.Add(solveForComp, new AddOp(), lf4);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0})({1})" + WorkMgr.EDM, "The expression is now factored.", factor1, factor2);

            AlgebraTerm[] factors = { factor1, factor2 };

            return factors;
        }

        public static AlgebraTerm FactorizeTerm(this AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm[] factors = term.GetFactors(ref pEvalData);
            if (factors.Length > 2)
                pEvalData.AttemptSetInputType(TermType.InputType.PolyFactor);

            if (factors != null)
            {
                AlgebraTerm finalTerm = new AlgebraTerm();
                for (int i = 0; i < factors.Length; ++i)
                {
                    finalTerm.Add(factors[i]);
                    if (i != factors.Length - 1)
                        finalTerm.Add(new MulOp());
                }

                return finalTerm;
            }

            return term;
        }

        public static AlgebraTerm ForceLogCoeffToPow(this AlgebraTerm term, AlgebraComp solveForComp = null)
        {
            List<ExComp[]> finalGroups = new List<ExComp[]>();

            List<LogFunction> logFuncs = new List<LogFunction>();
            List<ExComp[]> totalGroups = term.GetGroupsNoOps();
            for (int i = 0; i < term.GroupCount; ++i)
            {
                ExComp[] curGroup = totalGroups[i];
                int logCount = 0;
                LogFunction logFunc = null;
                // List of constant coefficients.
                List<ExComp> coeffs = new List<ExComp>();
                for (int j = 0; j < curGroup.Count(); ++j)
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
                    ExComp innerEx = logFunc.InnerEx;

                    ExComp raisedInnerEx = coeff.IsEqualTo(Number.One) ? innerEx : PowOp.StaticCombine(innerEx, coeff);

                    LogFunction addLog = new LogFunction(raisedInnerEx);
                    addLog.Base = logFunc.Base;
                    ExComp[] addLogGroup = { addLog };
                    finalGroups.Add(addLogGroup);
                    continue;
                }

                finalGroups.Add(curGroup);
            }

            return new AlgebraTerm(finalGroups.ToArray());
        }

        public static AlgebraTerm ForceLogPowToCoeff(this LogFunction log)
        {
            AlgebraTerm innerTerm = log.InnerTerm;
            var groups = innerTerm.GetGroupsNoOps();
            if (groups.Count == 1)
            {
                var group = groups[0];

                List<Number> powers = new List<Number>();
                for (int i = 0; i < group.Length; ++i)
                {
                    var groupComp = group[i];
                    if (groupComp is PowerFunction)
                    {
                        PowerFunction pfGroupComp = groupComp as PowerFunction;
                        if (pfGroupComp.Power is Number)
                        {
                            powers.Add(pfGroupComp.Power as Number);
                        }
                        else
                            return log;
                    }
                    else if (groupComp is Number)
                    {
                        Number nGroupComp = groupComp as Number;

                        Number nBase, nPow;
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

                Number powersGcf = Number.GCF(powers);

                if (powersGcf == null || powersGcf == 1.0 || powersGcf == 0.0)
                    return log;

                for (int i = 0; i < group.Length; ++i)
                {
                    PowerFunction pf = group[i] as PowerFunction;

                    Number nPow = pf.Power as Number;
                    ExComp divResult = nPow / powersGcf;
                    if (Number.One.IsEqualTo(divResult))
                        group[i] = pf.Base;
                    else
                        (group[i] as PowerFunction).Power = divResult;
                }

                LogFunction finalLog = new LogFunction(group.ToAlgTerm());
                finalLog.Base = log.Base;

                return new AlgebraTerm(powersGcf, new MulOp(), finalLog);
            }

            return log;
        }

        public static void GetAdditionalVarFors(this AlgebraTerm term, ref Dictionary<string, int> varFors)
        {
            foreach (ExComp subComp in term.SubComps)
            {
                if (subComp is AlgebraTerm)
                {
                    (subComp as AlgebraTerm).GetAdditionalVarFors(ref varFors);
                }
                if (subComp is FunctionDefinition)
                {
                    AlgebraComp[] inputArgs = (subComp as FunctionDefinition).InputArgs;
                    foreach (AlgebraComp inputArg in inputArgs)
                    {
                        if (varFors.ContainsKey(inputArg.Var.Var))
                            varFors[inputArg.Var.Var] = varFors[inputArg.Var.Var] + 1;
                        else
                            varFors.Add(inputArg.Var.Var, 1);
                    }
                }
            }
        }

        public static List<TypePair<ExComp, ExComp>> GetDivisors(this ExComp ex)
        {
            List<TypePair<ExComp, ExComp>> divisors = null;

            if (ex is PowerFunction)
            {
                PowerFunction fnPow = ex as PowerFunction;
                if (fnPow.Power is Number && (fnPow.Power as Number).IsRealInteger())
                {
                    int pow = (int)(fnPow.Power as Number).RealComp;

                    divisors = new List<TypePair<ExComp, ExComp>>();
                    Number lower = Number.Zero;
                    Number upper = new Number(pow);
                    for (int i = 0; i < pow; ++i)
                    {
                        ExComp div0 = PowOp.StaticWeakCombine(fnPow.Base, lower.Clone());
                        ExComp div1 = PowOp.StaticWeakCombine(fnPow.Base, upper.Clone());

                        lower += 1.0;
                        upper -= 1.0;

                        divisors.Add(new TypePair<ExComp, ExComp>(div0, div1));
                    }
                }
            }
            else if (ex is AlgebraComp)
            {
                divisors = new List<TypePair<ExComp, ExComp>>();
                divisors.Add(new TypePair<ExComp, ExComp>(ex, Number.One));
            }
            else if (ex is AlgebraFunction)
                return null;
            else if (ex is AlgebraTerm)
            {
                return null;

                //AlgebraTerm term = ex as AlgebraTerm;
                //var groups = term.GetGroupsNoOps();
                //if (groups.Count != 1)
                //    return null;

                //var group = groups[0];

                //var seperateDivisors = new List<List<TypePair<ExComp, ExComp>>>();

                //foreach (ExComp gpSubComp in group)
                //{
                //    var addDivisors = gpSubComp.GetDivisors();
                //    if (addDivisors == null)
                //        return null;
                //    seperateDivisors.Add(addDivisors);
                //}

                //for (int i = 0; i < seperateDivisors.Count; ++i)
                //{
                //    var subCompDivs = seperateDivisors[i];

                //}
            }
            else if (ex is Number)
            {
                var numDivisors = (ex as Number).GetDivisors();
                var exDivisors = from divisor in numDivisors
                                 select new TypePair<ExComp, ExComp>(divisor.Data1, divisor.Data2);
                divisors = exDivisors.ToList();
            }

            return divisors;
        }

        public static List<TypePair<ExComp, ExComp>> GetDivisorsSignInvariant(this ExComp ex)
        {
            if (ex is Number)
            {
                var numDivisors = (ex as Number).GetDivisorsSignInvariant();
                var exDivisors = from divisor in numDivisors
                                 select new TypePair<ExComp, ExComp>(divisor.Data1, divisor.Data2);
                return exDivisors.ToList();
            }

            var signInvariantDivisors = ex.GetDivisors();
            if (signInvariantDivisors == null)
                return null;
            // We have to account for the negative negative situation.
            for (int i = 0; i < signInvariantDivisors.Count; ++i)
            {
                TypePair<ExComp, ExComp> ip = signInvariantDivisors.ElementAt(i);

                // Make it into a negative negative combination.
                // We can safely assume both of these numbers are positive as they are the divisors of a positive number.
                TypePair<ExComp, ExComp> newPair = new TypePair<ExComp, ExComp>();
                newPair.Data1 = MulOp.Negate(ip.Data1);
                newPair.Data2 = MulOp.Negate(ip.Data2);

                signInvariantDivisors.Insert(i, newPair);

                // Skip over the added entry.
                ++i;
            }

            return signInvariantDivisors;
        }

        public static void GetDomain(this AlgebraTerm term, AlgebraSolver agSolver, AlgebraVar varFor, ref List<Restriction> restrictions, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen != null)
            {
                // The denominator can never be zero.
                AlgebraTerm den = numDen[1].Clone().ToAlgTerm();

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "The denominator of this term can never be zero so solve for when " + WorkMgr.STM +
                    WorkMgr.ExFinalToAsciiStr(numDen[1]) + "=0" + WorkMgr.EDM, term);

                ExComp sol = agSolver.SolveEq(varFor, den, Number.Zero.ToAlgTerm(), ref pEvalData);
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

                numDen[0].GetDomain(agSolver, varFor, ref restrictions, ref pEvalData);
                if (restrictions == null)
                    return;
                numDen[1].GetDomain(agSolver, varFor, ref restrictions, ref pEvalData);
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

                foreach (ExComp subComp in term.SubComps)
                {
                    if (subComp is AlgebraTerm)
                    {
                        (subComp as AlgebraTerm).GetDomain(agSolver, varFor, ref restrictions, ref pEvalData);
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
                restrictions.Clear();

                restrictions.Add(OrRestriction.GetNoRealNumsRestriction(varFor.ToAlgebraComp(), ref pEvalData));
            }

            var notRests = (from rest in restrictions
                            where rest is NotRestriction
                            select rest as NotRestriction).ToList();

            restrictions.Clear();

            // Ensure that all the not restrictions are actually included in the compounded if they aren't then the information is just redundnant.
            foreach (var notRest in notRests)
            {
                if (compounded.IsValidValue(notRest.NotVal, ref pEvalData))
                    restrictions.Add(notRest);
            }

            restrictions.Add(compounded);
        }

        public static AlgebraTerm[] GetFactors(this AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            if (term.RemoveRedundancies() is Number)
                return null;

            var polyInfos = term.GetPolynomialInfo();
            if (polyInfos != null)
            {
                polyInfos.FillInPowRanges();

                AlgebraTerm[] factors = null;
                if (polyInfos.HasOnlyPowers(2, 1, 0))
                {
                    Number a = polyInfos.GetCoeffForPow(2);
                    Number b = polyInfos.GetCoeffForPow(1);
                    Number c = polyInfos.GetCoeffForPow(0);

                    factors = Factorize(a, b, c, polyInfos.Var.Var, ref pEvalData);
                }
                else if (polyInfos.HasOnlyPowers(3, 2, 1, 0))
                {
                    Number a = polyInfos.GetCoeffForPow(3);
                    Number b = polyInfos.GetCoeffForPow(2);
                    Number c = polyInfos.GetCoeffForPow(1);
                    Number d = polyInfos.GetCoeffForPow(0);

                    factors = Factorize(a, b, c, d, polyInfos.Var.Var, ref pEvalData);
                }

                if (factors != null)
                    return factors;
            }

            ExComp[] gcfGp = term.GetGroupGCF();
            if (gcfGp != null && gcfGp.Length != 0 && !(gcfGp.Length == 1 && (Number.One.IsEqualTo(gcfGp[0]) || Number.Zero.IsEqualTo(gcfGp[0]))))
            {
                AlgebraTerm gcfTerm = gcfGp.ToAlgNoRedunTerm();
                if (!term.IsEqualTo(gcfTerm))
                {
                    term = DivOp.FactorOutTerm(term, gcfTerm.Clone()).ToAlgTerm();
                    if (!term.IsOne())
                    {
                        AlgebraTerm[] otherFactors = GetFactors(term, ref pEvalData);

                        if (otherFactors != null)
                        {
                            List<AlgebraTerm> factors = otherFactors.ToList();
                            factors.Add(gcfTerm);

                            return factors.ToArray();
                        }
                        else
                        {
                            AlgebraTerm[] factors = { gcfTerm, term };
                            return factors;
                        }
                    }
                    else
                        term = gcfTerm;
                }
            }

            var groups = term.GetGroupsNoOps();
            if (groups.Count == 2)
            {
                #region Perfect Powers

                Number squarePow = new Number(2);
                Number cubePow = new Number(3);

                // Do we have a sum/difference of squares?
                if (IsRaisedToPower(groups[0], squarePow) && IsRaisedToPower(groups[1], squarePow))
                {
                    ExComp[] aGp;
                    ExComp[] bGp;

                    if (groups[0].IsNeg())
                    {
                        aGp = groups[1];
                        bGp = groups[0];
                    }
                    else
                    {
                        aGp = groups[0];
                        bGp = groups[1];
                    }

                    if (bGp.IsNeg())
                    {
                        bGp = AbsValFunction.MakePositive(bGp);

                        // As in the power of 2.
                        ExComp aExTo2 = aGp.ToAlgTerm().RemoveRedundancies();
                        ExComp bExTo2 = bGp.ToAlgTerm().RemoveRedundancies();

                        ExComp aEx = PowOp.TakeRoot(aExTo2, squarePow, ref pEvalData);
                        if (aEx is AlgebraTermArray)
                            aEx = (aEx as AlgebraTermArray)[0];

                        ExComp bEx = PowOp.TakeRoot(bExTo2, squarePow, ref pEvalData);
                        if (bEx is AlgebraTermArray)
                            bEx = (bEx as AlgebraTermArray)[0];

                        AlgebraTerm[] factors =
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

                    if (groups[0].IsNeg())
                    {
                        aGp = groups[1];
                        bGp = groups[0];
                    }
                    else
                    {
                        aGp = groups[0];
                        bGp = groups[1];
                    }

                    bool isNeg = bGp.IsNeg();
                    if (isNeg)
                        bGp = AbsValFunction.MakePositive(bGp);

                    // As in the power of 3.
                    ExComp aExTo3 = aGp.ToAlgTerm().RemoveRedundancies();
                    ExComp bExTo3 = bGp.ToAlgTerm().RemoveRedundancies();

                    ExComp aEx = PowOp.TakeRoot(aExTo3, cubePow, ref pEvalData);
                    if (aEx is AlgebraTermArray)
                        aEx = (aEx as AlgebraTermArray)[0];

                    ExComp bEx = PowOp.TakeRoot(bExTo3, cubePow, ref pEvalData);
                    if (bEx is AlgebraTermArray)
                        bEx = (bEx as AlgebraTermArray)[0];

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
                ExComp ex0 = groups[0].ToAlgTerm().RemoveRedundancies();
                ExComp ex1 = groups[1].ToAlgTerm().RemoveRedundancies();
                ExComp ex2 = groups[2].ToAlgTerm().RemoveRedundancies();
                ExComp ex3 = groups[3].ToAlgTerm().RemoveRedundancies();

                bool is0Neg = groups[0].IsNeg();
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
                            is1Neg = groups[1].IsNeg();

                            ExComp ex1Orig = ex1.Clone();
                            ExComp ex2Orig = ex2.Clone();
                            ExComp ex3Orig = ex3.Clone();

                            ex1 = ex3Orig;
                            ex3 = ex2Orig;
                            ex2 = ex1Orig;
                        }
                    }
                    else
                    {
                        is1Neg = groups[3].IsNeg();

                        ExComp ex1Orig = ex1.Clone();
                        ExComp ex2Orig = ex2.Clone();
                        ExComp ex3Orig = ex3.Clone();

                        ex1 = ex2Orig;
                        ex2 = ex3Orig;
                        ex3 = ex1Orig;
                    }
                }
                else
                {
                    is1Neg = groups[2].IsNeg();
                }

                if (is0Neg)
                    gcf0 = MulOp.Negate(gcf0);
                if (is1Neg)
                    gcf1 = MulOp.Negate(gcf1);

                ex0 = DivOp.StaticCombine(ex0, gcf0);
                if (ex0 is AlgebraTerm)
                    ex0 = (ex0 as AlgebraTerm).RemoveRedundancies();

                ex1 = DivOp.StaticCombine(ex1, gcf0);
                if (ex1 is AlgebraTerm)
                    ex1 = (ex1 as AlgebraTerm).RemoveRedundancies();

                ex2 = DivOp.StaticCombine(ex2, gcf1);
                if (ex2 is AlgebraTerm)
                    ex2 = (ex2 as AlgebraTerm).RemoveRedundancies();

                ex3 = DivOp.StaticCombine(ex3, gcf1);
                if (ex3 is AlgebraTerm)
                    ex3 = (ex3 as AlgebraTerm).RemoveRedundancies();

                if (ex0.IsEqualTo(ex2) && ex1.IsEqualTo(ex3))
                {
                    AlgebraTerm[] factors =
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
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Using the rational roots theorem attempt to factor the polynomial by dividing by the zeros of the the polynomial.", term);
                Solving.PolynomialSolve polySolve = new Solving.PolynomialSolve(new AlgebraSolver());

                ExComp solutions = polySolve.SolveEquation(term, Number.Zero.ToAlgTerm(), polyInfos.Var.Var, ref pEvalData);

                if (solutions == null)
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Couldn't factor the polynomial through using synthetic division.", term);
                    return null;
                }

                if (solutions is AlgebraTermArray)
                {
                    AlgebraTermArray solutionArray = solutions as AlgebraTermArray;

                    if (pEvalData.PartialSolutions != null && pEvalData.PartialSolutions.Count != 0)
                    {
                        var partialSolutions = (from partialSol in pEvalData.PartialSolutions
                                                select partialSol.ToAlgTerm()).ToArray();

                        pEvalData.PartialSolutions.Clear();

                        return partialSolutions;
                    }

                    AlgebraTerm[] factorArray = new AlgebraTerm[solutionArray.TermCount];

                    for (int i = 0; i < solutionArray.TermCount; ++i)
                    {
                        ExComp solution = solutionArray[i];
                        if (solution is AlgebraTerm)
                        {
                            AlgebraTerm[] numDen = (solution as AlgebraTerm).GetNumDenFrac();
                            if (numDen != null)
                            {
                                factorArray[i] = SubOp.StaticCombine(MulOp.StaticCombine(numDen[1], polyInfos.Var), numDen[0]).ToAlgTerm();
                                continue;
                            }
                        }

                        factorArray[i] = new AlgebraTerm(polyInfos.Var, new AddOp(), solutionArray[i]);
                    }

                    return factorArray;
                }
            }

            return null;
        }

        public static bool IsSimpleFraction(this AlgebraTerm term)
        {
            if (term.ContainsOnlyFractions() && term.GroupCount == 1)
                return true;
            return false;
        }

        public static AlgebraTerm PythagTrigSimplify(AlgebraTerm term)
        {
            var groups = term.GetGroupsNoOps();

            Func<ExComp[], List<TypePair<PowerFunction, ExComp[]>>> getTrigFuncAndCoeff = (ExComp[] gp) =>
                {
                    List<TypePair<PowerFunction, ExComp[]>> trigCoeffPairs = new List<TypePair<PowerFunction, ExComp[]>>();
                    for (int i = 0; i < gp.Length; ++i)
                    {
                        if (gp[i] is PowerFunction && (gp[i] as PowerFunction).Power is Number && ((gp[i] as PowerFunction).Power as Number) % 2 == 0)
                        {
                            PowerFunction fnPow = gp[i] as PowerFunction;
                            if (!(fnPow.Base is SinFunction) && !(fnPow.Base is CosFunction))
                                continue;
                            // We have a trig function matching pythag substitution.
                            var prevRange = gp.ToList().GetRange(0, i);
                            var afterRange = gp.ToList().GetRange(i + 1, gp.Length - (i + 1));
                            if (prevRange.Count + afterRange.Count == 0)
                                afterRange.Add(Number.One);
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
                        (gp[i] as PowerFunction).Power is Number &&
                        (gp[i] as PowerFunction).Power.IsEqualTo(reqPow) &&
                        ((gp[i] as PowerFunction).Power as Number) % 2 == 0 &&
                        TrigFunction.GetTrigType(gp[i]) == reqTrigIden)
                    {
                        PowerFunction fnPow = gp[i] as PowerFunction;
                        TrigFunction fnBaseTrig = fnPow.Base as TrigFunction;
                        if (!fnBaseTrig.InnerEx.IsEqualTo(innerEx))
                            continue;
                        // We have a trig function matching pythag substitution.
                        var prevRange = gp.ToList().GetRange(0, i);
                        var afterRange = gp.ToList().GetRange(i + 1, gp.Length - (i + 1));
                        if (prevRange.Count + afterRange.Count == 0)
                            afterRange.Add(Number.One);
                        var totalRange = prevRange.Concat(afterRange).ToArray();
                        if (!totalRange.ToAlgNoRedunTerm().IsEqualTo(coeff))
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

                var trigCoeffPairs = getTrigFuncAndCoeff(group);

                for (int j = 0; j < trigCoeffPairs.Count; ++j)
                {
                    var trigCoeffPair = trigCoeffPairs[j];
                    string desiredTrigFunc = TrigFunction.GetTrigType(trigCoeffPair.Data1.Base) == SinFunction.IDEN ? CosFunction.IDEN : SinFunction.IDEN;
                    ExComp innerEx = (trigCoeffPair.Data1.Base as TrigFunction).InnerEx;

                    bool breakLoop = false;

                    // See if we have any matching throughout the groups.
                    for (int k = i + 1; k < groups.Count; ++k)
                    {
                        ExComp[] compareGroup = groups[k];

                        var compareTrigCoeffPair = getTrigFuncAndCoeffRestrictions(compareGroup, trigCoeffPair.Data1.Power, innerEx,
                            trigCoeffPair.Data2.ToAlgNoRedunTerm(), desiredTrigFunc);

                        if (compareTrigCoeffPair == null)
                            continue;

                        groups[i] = trigCoeffPair.Data2;

                        groups.RemoveAt(k);
                        breakLoop = true;
                        break;
                    }

                    if (breakLoop)
                        break;
                }
            }

            // Replace sin^2(x)-1 with cos^2(x)

            // Replace cos^2(x)-1 with sin^2(x)

            return new AlgebraTerm(groups.ToArray());
        }

        public static ExComp RaiseToPow(this ExComp term, Number power, ref TermType.EvalData pEvalData)
        {
            return PowOp.RaiseToPower(term, power, ref pEvalData);
        }

        public static AlgebraTerm TrigSimplify(this AlgebraTerm term)
        {
            term = TrigSimplifyCombine(term);
            term = PythagTrigSimplify(term);

            return term;
        }

        private static ExComp[] CancelTrigTerms(ExComp[] group)
        {
            ExComp[] den = group.GetDenominator();
            if (den != null && den.Length != 0)
            {
                // There is a denominator in this group.
                ExComp[] num = group.GetNumerator();
                AlgebraTerm numDenResult = AlgebraTerm.FromFraction(num.ToAlgTerm(), den.ToAlgTerm()).TrigSimplify();
                var numDenResultGroups = numDenResult.GetGroups();
                if (numDenResultGroups.Count == 1)
                    return numDenResultGroups[0];
                else
                {
                    ExComp[] singularGp = { numDenResult };
                    return singularGp;
                }
            }

            List<ExComp> checkGroup = group.ToList();
            List<ExComp> finalGroup = new List<ExComp>();
            for (int i = 0; i < checkGroup.Count; ++i)
            {
                ExComp groupComp = group[i];

                int j;
                for (j = i + 1; j < checkGroup.Count; ++j)
                {
                    ExComp compareGroupComp = group[j];
                    ExComp compareResult = TrigFunction.TrigCancel(groupComp, compareGroupComp);
                    if (compareResult != null)
                    {
                        finalGroup.Add(compareResult);
                        checkGroup.RemoveAt(i--);
                        checkGroup.RemoveAt(--j);
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
                        groups[i][j] = AlgebraTerm.FromFraction(Number.One, (groups[i][j] as TrigFunction).GetReciprocalOf());
                }
            }

            AlgebraTerm totalTerm = new AlgebraTerm(groups.ToArray());
            totalTerm = totalTerm.CompoundFractions();
            return totalTerm.RemoveRedundancies();
        }

        private static List<ExComp[]> ExpandLogFunctionInners(ExComp logInnerEx, ExComp baseEx)
        {
            List<ExComp[]> expandedLogs = new List<ExComp[]>();

            if (logInnerEx is AlgebraTerm)
            {
                AlgebraTerm innerTerm = logInnerEx as AlgebraTerm;
                var groups = innerTerm.GetGroupsNoOps();
                if (groups.Count == 1)
                {
                    var group = groups[0];
                    foreach (var groupComp in group)
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
                                negLog.Base = baseEx;
                                addGroup[0] = Number.NegOne;
                                addGroup[1] = negLog;
                                expandedLogs.Add(addGroup);
                                continue;
                            }
                        }
                        LogFunction posLog = new LogFunction(groupComp);
                        posLog.Base = baseEx;

                        addGroup = new ExComp[1];
                        addGroup[0] = posLog;

                        expandedLogs.Add(addGroup);
                    }
                }

                return expandedLogs;
            }
            LogFunction singleLogFunc = new LogFunction(logInnerEx);
            singleLogFunc.Base = baseEx;

            ExComp[] singleGroup = { singleLogFunc };
            expandedLogs.Add(singleGroup);
            return expandedLogs;
        }

        private static bool IsRaisedToPower(ExComp[] gp, Number pow)
        {
            if (pow < 2.0)
                throw new ArgumentException();

            foreach (ExComp gpCmp in gp)
            {
                if (gpCmp is AlgebraComp)
                    return false;
                else if (gpCmp is PowerFunction)
                {
                    PowerFunction pfGpCmp = gpCmp as PowerFunction;
                    if (pfGpCmp.Power is Number)
                    {
                        Number pfGpCmpPow = pfGpCmp.Power as Number;
                        if (pfGpCmpPow % pow != 0)
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

        private static AlgebraTerm TrigSimplifyCombine(AlgebraTerm term)
        {
            AlgebraTerm numTerm;

            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen != null)
            {
                numTerm = numDen[0];
            }
            else
                numTerm = term;

            var numGroups = numTerm.GetGroupsNoOps();

            for (int i = 0; i < numGroups.Count; ++i)
            {
                numGroups[i] = CancelTrigTerms(numGroups[i]);
            }

            if (numDen != null)
            {
                var denGroups = numDen[1].GetGroupsNoOps();

                for (int i = 0; i < denGroups.Count; ++i)
                {
                    denGroups[i] = CancelTrigTerms(denGroups[i]);
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

                            List<ExComp> useDenGroup = modifiedDens[i] == null ? denGroups[0].ToList() : modifiedDens[i];
                            for (int k = 0; k < useDenGroup.Count; ++k)
                            {
                                if (useDenGroup[k] is AgOp)
                                    continue;

                                ExComp cancelResult = TrigFunction.TrigCancel(numGroupComp, useDenGroup[k], true);
                                if (cancelResult != null)
                                {
                                    // The cancel result will stay on the numerator.
                                    numGroups[i][j] = cancelResult;
                                    useDenGroup.RemoveAt(k--);
                                    if (modifiedDens[i] == null)
                                    {
                                        if (useDenGroup.Count == 0)
                                            useDenGroup.Add(new Number(1.0));
                                        modifiedDens[i] = useDenGroup;
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < numGroups.Count; ++i)
                    {
                        numGroups[i] = CancelTrigTerms(numGroups[i]);
                    }

                    // Some of the denominators might have been modified.
                    List<TypePair<ExComp, ExComp>> modifiedNumDenPairs = new List<TypePair<ExComp, ExComp>>();

                    for (int i = 0; i < modifiedDens.Length; ++i)
                    {
                        if (modifiedDens[i] != null)
                        {
                            modifiedNumDenPairs.Add(new TypePair<ExComp, ExComp>(
                                numGroups[i].ToAlgTerm(), modifiedDens[i].ToArray().ToAlgTerm()));
                        }
                    }

                    // Some of these terms might have the same denominators if so combine them.
                    for (int i = 0; i < modifiedNumDenPairs.Count; ++i)
                    {
                        for (int j = i + 1; j < modifiedNumDenPairs.Count; ++j)
                        {
                            if (modifiedNumDenPairs[i].Data2.IsEqualTo(modifiedNumDenPairs[j].Data2))
                            {
                                modifiedNumDenPairs[j].Data1 =
                                    AddOp.StaticCombine(modifiedNumDenPairs[i].Data1, modifiedNumDenPairs[j].Data1);
                                modifiedNumDenPairs.RemoveAt(i--);
                                break;
                            }
                        }
                    }

                    // Move all the trig terms to the numerator.
                    for (int i = 0; i < modifiedNumDenPairs.Count; ++i)
                    {
                        ExComp modDen = modifiedNumDenPairs[i].Data2;
                        AlgebraTerm topMultTerm = new AlgebraTerm();
                        if (modDen is AlgebraTerm)
                        {
                            AlgebraTerm modDenTerm = modDen as AlgebraTerm;
                            var modDenTermGps = modDenTerm.GetGroupsNoOps();
                            if (modDenTermGps.Count == 1)
                            {
                                List<ExComp> gp = modDenTermGps[0].ToList();
                                for (int j = 0; j < gp.Count; ++j)
                                {
                                    if (modDenTerm[j] is TrigFunction)
                                    {
                                        topMultTerm.Add((modDenTerm[j] as TrigFunction).GetReciprocalOf(), new MulOp());
                                        gp.RemoveAt(j);
                                    }
                                }

                                // Remove the tailing operator if there is one.
                                if (gp.Count > 0 && gp[gp.Count - 1] is AgOp)
                                    gp.RemoveAt(gp.Count - 1);

                                modifiedNumDenPairs[i].Data2 = gp.ToArray().ToAlgTerm();
                            }
                        }
                        else if (modDen is TrigFunction)
                        {
                            topMultTerm.Add((modDen as TrigFunction).GetReciprocalOf());
                            modifiedNumDenPairs[i].Data2 = new AlgebraTerm();
                        }

                        if (topMultTerm.TermCount > 0)
                        {
                            modifiedNumDenPairs[i].Data1 = MulOp.StaticWeakCombine(modifiedNumDenPairs[i].Data1, topMultTerm);
                        }
                    }

                    List<ExComp> nonModifiedNums = new List<ExComp>();
                    for (int i = 0; i < modifiedDens.Length; ++i)
                    {
                        if (modifiedDens[i] == null)
                        {
                            nonModifiedNums.Add(numGroups[i].ToAlgTerm());
                        }
                    }

                    AlgebraTerm final = new AlgebraTerm();
                    AlgebraTerm nonModNum = new AlgebraTerm();
                    foreach (ExComp nonModifiedNum in nonModifiedNums)
                    {
                        nonModNum.Add(nonModifiedNum, new AddOp());
                    }

                    // Remove the last '+' operator.
                    if (nonModNum.TermCount > 0)
                    {
                        nonModNum.SubComps.RemoveAt(nonModNum.TermCount - 1);

                        AlgebraTerm topMultTerm = new AlgebraTerm();

                        List<ExComp> singleDenGp = denGroups[0].ToList();
                        for (int i = 0; i < singleDenGp.Count; ++i)
                        {
                            if (singleDenGp[i] is TrigFunction)
                            {
                                topMultTerm.Add((denGroups[0][i] as TrigFunction).GetReciprocalOf(), new MulOp());
                                singleDenGp.RemoveAt(i);
                            }
                        }

                        if (topMultTerm.TermCount > 0)
                        {
                            topMultTerm.SubComps.RemoveAt(topMultTerm.TermCount - 1);
                            nonModNum = MulOp.StaticWeakCombine(topMultTerm, nonModNum).ToAlgTerm();
                        }

                        if (singleDenGp.Count > 0)
                            final.Add(AlgebraTerm.FromFraction(nonModNum, singleDenGp.ToArray().ToAlgTerm()));
                        else
                            final.Add(nonModNum);
                    }

                    foreach (var modNumDenPair in modifiedNumDenPairs)
                    {
                        ExComp addEx;
                        if (modNumDenPair.Data2 is AlgebraTerm && (modNumDenPair.Data2 as AlgebraTerm).TermCount == 0)
                            addEx = modNumDenPair.Data1;
                        else
                            addEx = AlgebraTerm.FromFraction(modNumDenPair.Data1, modNumDenPair.Data2);
                        if (final.TermCount > 0)
                            final.Add(new AddOp());
                        final.Add(addEx);
                    }

                    final = final.ApplyOrderOfOperations();
                    final = final.MakeWorkable().ToAlgTerm();

                    return final;

                    #endregion Cancel Numerator denominator
                }
                else
                {
                    ExComp num = numDen[0];
                    if (numGroups.Count > 1)
                        num = CompoundTrigFuncs(numGroups);
                    ExComp den = CompoundTrigFuncs(denGroups);
                    if (!(den is AlgebraTerm && (den as AlgebraTerm).GroupCount != 1) &&
                        !(num is AlgebraTerm && (num as AlgebraTerm).GroupCount != 1))
                    {
                        return AlgebraTerm.FromFraction(num, den).TrigSimplify();
                    }
                }

                return AlgebraTerm.FromFraction(new AlgebraTerm(numGroups.ToArray()), new AlgebraTerm(denGroups.ToArray()));
            }

            return new AlgebraTerm(numGroups.ToArray());
        }
    }
}