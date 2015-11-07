using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class SumFunction : AppliedFunction_NArgs
    {
        private const int MAX_SUM_COUNT = 50;
        private const int MAX_WORK_STEP_COUNT = 5;

        public ExComp GetIterCount()
        {
            return _args[3];
        }

        public ExComp GetIterStart()
        {
            return _args[2];
        }

        public AlgebraComp GetIterVar()
        {
            return (AlgebraComp) _args[1];
        }

        public bool GetIsInfiniteSeries()
        {
            return GetIterCount().IsEqualTo(ExNumber.GetPosInfinity());
        }

        public SumFunction(ExComp term, AlgebraComp iterVar, ExComp iterStart, ExComp iterCount)
            : base(FunctionType.Summation, typeof(SumFunction), (iterVar.GetVar().GetVar() == "i" && term is AlgebraTerm) ? (term as AlgebraTerm).ConvertImaginaryToVar() : term, iterVar, iterStart, iterCount)
        {
        }

        public override ExComp CloneEx()
        {
            return new SumFunction(GetInnerEx().CloneEx(), (AlgebraComp)GetIterVar().CloneEx(), GetIterStart().CloneEx(), GetIterCount().CloneEx());
        }

        private bool? Converges(ref TermType.EvalData pEvalData, out ExComp result, ExComp[] gp)
        {
            result = null;
            if (!GroupHelper.GroupContains(gp, GetIterVar()))
            {
                return false;
            }

            // Take out the constants.
            ExComp[] varGp;
            ExComp[] constGp;
            GroupHelper.GetConstVarTo(gp, out varGp, out constGp, GetIterVar());

            if (constGp.Length != 0)
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + GroupHelper.ToAlgTerm(constGp).FinalToDispStr() + "*\\sum_{" + GetIterVar().ToDispString() + "=" +
                    GetIterStart().ToAlgTerm().FinalToDispStr() + "}^{" + GetIterCount().ToAlgTerm().ToDispString() + "}" + GroupHelper.ToAlgTerm(varGp).FinalToDispStr() + WorkMgr.EDM,
                    "Take constants out.");
            }

            AlgebraTerm innerTerm = GroupHelper.ToAlgTerm(varGp);
            string innerExStr = innerTerm.FinalToDispStr();
            string thisStrNoMathMark = "\\sum_{" + GetIterVar().ToDispString() + "=" + GetIterStart().ToAlgTerm().FinalToDispStr() + "}^{" +
                GetIterCount().ToAlgTerm().ToDispString() + "}" + innerExStr;
            string thisStr = WorkMgr.STM + thisStrNoMathMark + WorkMgr.EDM;

            // The basic divergence test.
            pEvalData.GetWorkMgr().FromFormatted(thisStr, "If " + WorkMgr.STM +
                "\\lim_{" + GetIterVar().ToDispString() + " \\to \\infty}" + GroupHelper.ToAlgTerm(gp).FinalToDispStr() + "\\ne 0 " + WorkMgr.EDM + " then the series is divergent");

            ExComp divTest = Limit.TakeLim(innerTerm, GetIterVar(), ExNumber.GetPosInfinity(), ref pEvalData, 0);
            if (divTest is Limit)
                return null;
            if (divTest is AlgebraTerm)
                divTest = (divTest as AlgebraTerm).RemoveRedundancies(false);
            if (!divTest.IsEqualTo(ExNumber.GetZero()))
            {
                pEvalData.GetWorkMgr().FromFormatted(thisStr, "The limit did not equal zero, the series is divergent.");
                return false;
            }

            // The p-series test.
            AlgebraTerm[] frac = innerTerm.GetNumDenFrac();
            if (frac != null && frac[0].RemoveRedundancies(false).IsEqualTo(ExNumber.GetOne()))
            {
                ExComp den = frac[1].RemoveRedundancies(false);
                if (den is PowerFunction)
                {
                    PowerFunction powFunc = den as PowerFunction;
                    if (powFunc.GetBase().IsEqualTo(GetIterVar()) && powFunc.GetPower() is ExNumber && ExNumber.OpGT((powFunc.GetPower() as ExNumber), 0.0) &&
                        GetIterStart() is ExNumber && ExNumber.OpGE((GetIterStart() as ExNumber), 1.0))
                    {
                        ExNumber nPow = powFunc.GetPower() as ExNumber;
                        bool isConvergent = ExNumber.OpGT(nPow, 1.0);
                        pEvalData.GetWorkMgr().FromFormatted(thisStr, "In the form " + WorkMgr.STM + "1/n^p" + WorkMgr.EDM + " if " +
                            WorkMgr.STM + "p \\gt 1" + WorkMgr.EDM + " then the series is convergent.");
                        if (isConvergent)
                            pEvalData.GetWorkMgr().FromFormatted(thisStr, WorkMgr.STM + "p > 1" + WorkMgr.EDM + " so the series converges");
                        else
                            pEvalData.GetWorkMgr().FromFormatted(thisStr, WorkMgr.STM + "p <= 1" + WorkMgr.EDM + " so the series diverges");
                        return isConvergent;
                    }
                }
            }

            // Geometric series test.
            if (varGp.Length == 2 || varGp.Length == 1)
            {
                ExComp ele0 = varGp[0];
                ExComp ele1 = varGp.Length > 1 ? varGp[1] : ExNumber.GetOne();
                bool ele0Den = false;
                bool ele1Den = false;

                if (ele0 is PowerFunction && (ele0 as PowerFunction).GetPower().IsEqualTo(ExNumber.GetNegOne()))
                {
                    ele0Den = true;
                    ele0 = (ele0 as PowerFunction).GetBase();
                }
                if (ele1 is PowerFunction && (ele1 as PowerFunction).GetPower().IsEqualTo(ExNumber.GetNegOne()))
                {
                    ele1Den = true;
                    ele1 = (ele1 as PowerFunction).GetBase();
                }

                // Needs to be in the form a_n=a*r^n
                ExComp a = null;
                PowerFunction pf = null;
                bool pfDen = false;
                if (ele0 is PowerFunction && (ele0 as PowerFunction).GetPower().ToAlgTerm().Contains(GetIterVar()))
                {
                    pf = ele0 as PowerFunction;
                    a = ele1;
                    pfDen = ele0Den;
                }
                else if (ele1 is PowerFunction && (ele1 as PowerFunction).GetPower().ToAlgTerm().Contains(GetIterVar()))
                {
                    pf = ele1 as PowerFunction;
                    a = ele0;
                    pfDen = ele1Den;
                }

                if (a != null && pf != null && !a.ToAlgTerm().Contains(GetIterVar()) && pf.GetBase() is ExNumber && !(pf.GetBase() as ExNumber).HasImaginaryComp() &&
                    GetIterStart() is ExNumber && ExNumber.OpGE((GetIterStart() as ExNumber), 0.0))
                {
                    ExNumber nBase = pf.GetBase() as ExNumber;
                    ExComp exBase = nBase;
                    if (pfDen)
                        exBase = ExNumber.OpDiv(ExNumber.GetOne(), nBase);

                    if (!(exBase is ExNumber))
                        return null;

                    nBase = exBase as ExNumber;
                    if (pfDen)
                        exBase = DivOp.StaticCombine(ExNumber.GetOne(), pf.GetBase() as ExNumber);

                    nBase = ExNumber.Abs(nBase);
                    AlgebraTerm pfPow = pf.GetPower().ToAlgTerm();
                    List<ExComp> powers = pfPow.GetPowersOfVar(GetIterVar());
                    if (powers.Count == 1 && powers[0].IsEqualTo(ExNumber.GetOne()))
                    {
                        pEvalData.GetWorkMgr().FromFormatted(thisStr, "In the geometric series form " + WorkMgr.STM + "ar^{n-1}" + WorkMgr.EDM + " if " +
                            WorkMgr.STM + "|r| \\lt 1 " + WorkMgr.EDM + " than the series is convergent");
                        if (ExNumber.OpGE(nBase, 1.0))
                        {
                            pEvalData.GetWorkMgr().FromFormatted(thisStr, WorkMgr.STM + "|r| \\ge 1" + WorkMgr.EDM + ", the series is divergent.");
                            return false;
                        }

                        if (pfPow.RemoveRedundancies(false).IsEqualTo(SubOp.StaticCombine(GetIterVar(), GetIterStart())))
                        {
                            ExComp tmpDen = SubOp.StaticCombine(ExNumber.GetOne(), exBase);
                            tmpDen = tmpDen.ToAlgTerm().CompoundFractions();

                            result = DivOp.StaticCombine(a, tmpDen);

                            string resultStr = result.ToAlgTerm().FinalToDispStr();
                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + thisStrNoMathMark + "=" + resultStr + WorkMgr.EDM, "Use the formula " + WorkMgr.STM + "\\sum_{n=1}^{\\infty}ar^{n-1}=\\frac{a}{1-r}" +
                                WorkMgr.EDM);

                            if (constGp.Length != 0)
                            {
                                result = MulOp.StaticCombine(result, GroupHelper.ToAlgTerm(constGp));
                                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + GroupHelper.ToAlgTerm(constGp).FinalToDispStr() + "*\\sum_{" + GetIterVar().ToDispString() + "=" +
                                    GetIterStart().ToAlgTerm().FinalToDispStr() + "}^{" + GetIterCount().ToAlgTerm().ToDispString() + "}" + GroupHelper.ToAlgTerm(varGp).FinalToDispStr() + "=" +
                                    WorkMgr.ToDisp(result) + WorkMgr.EDM,
                                    "Multiply the by the constants.");
                            }
                        }

                        pEvalData.GetWorkMgr().FromFormatted(thisStr, WorkMgr.STM + "|r| \\lt 1 " + WorkMgr.EDM + ", the series is convergent.");
                        return true;
                    }
                }
            }

            return null;
        }

        public bool? Converges(ref TermType.EvalData pEvalData, out ExComp result)
        {
            result = null;
            if (!GetIsInfiniteSeries())
                return true;

            if (!GetInnerTerm().Contains(GetIterVar()))
            {
                pEvalData.GetWorkMgr().FromSides(this, null, "The above diverges");
                return false;
            }

            // Split into groups and take the summation of each group independently.
            List<ExComp[]> gps = GetInnerTerm().GetGroupsNoOps();
            ExComp overallResult = ExNumber.GetZero();
            for (int i = 0; i < gps.Count; ++i)
            {
                ExComp gpResult;
                bool? gpConverges = Converges(ref pEvalData, out gpResult, gps[0]);
                if (gpConverges == null || !gpConverges.Value)
                    return gpConverges;

                if (gpResult != null && overallResult != null)
                    overallResult = AddOp.StaticCombine(overallResult, gpResult);
                if (gpResult == null)
                    overallResult = null;
            }

            if (overallResult != null)
                result = overallResult;

            return true;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            // No negative numbers with summations.
            if ((GetIterCount() is ExNumber && ExNumber.OpLT((GetIterCount() as ExNumber), 0.0)) ||
                (GetIterStart() is ExNumber && ExNumber.OpLT((GetIterStart() as ExNumber), 0.0)))
                return this;

            bool toInfinity = GetIterCount() is ExNumber && GetIterCount().IsEqualTo(ExNumber.GetPosInfinity());
            if (!GetInnerTerm().Contains(GetIterVar()))
            {
                // Is this just a sum of numbers.
                if (toInfinity)
                    return ExNumber.GetPosInfinity();

                ExComp sumTotal = ExNumber.GetZero();
                if (!ExNumber.GetOne().IsEqualTo(GetIterStart()))
                    sumTotal = SubOp.StaticCombine(GetIterCount(), SubOp.StaticCombine(GetIterStart(), ExNumber.GetOne()));
                else
                    sumTotal = GetIterCount();
                return MulOp.StaticCombine(sumTotal, GetInnerTerm());
            }

            if (toInfinity)
            {
                ExComp result = null;

                int stepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                bool? converges = Converges(ref pEvalData, out result);
                pEvalData.GetWorkMgr().PopSteps(stepCount);

                if (converges != null)
                {
                    if (result != null)
                        return result;
                    if (!converges.Value)
                        return ExNumber.GetPosInfinity();
                }

                return this;
            }

            ExComp innerEx = GetInnerEx();
            if (innerEx.IsEqualTo(GetIterVar()) && GetIterStart().IsEqualTo(ExNumber.GetOne()))
            {
                return DivOp.StaticCombine(MulOp.StaticCombine(GetIterCount(), AddOp.StaticCombine(GetIterCount(), ExNumber.GetOne())), new ExNumber(2.0));
            }

            if (GetIterCount() is ExNumber && (GetIterCount() as ExNumber).IsRealInteger() &&
                GetIterStart() is ExNumber && (GetIterStart() as ExNumber).IsRealInteger())
            {
                int count = (int)(GetIterCount() as ExNumber).GetRealComp();
                int start = (int)(GetIterStart() as ExNumber).GetRealComp();

                AlgebraTerm totalTerm = new AlgebraTerm(ExNumber.GetZero());

                if (count > MAX_SUM_COUNT)
                    return this;

                ExComp iterVal;

                for (int i = start; i <= count; ++i)
                {
                    iterVal = new ExNumber(i);

                    AlgebraTerm innerTerm = GetInnerTerm().CloneEx().ToAlgTerm();

                    innerTerm = innerTerm.Substitute(GetIterVar(), iterVal);

                    WorkStep lastStep = null;
                    if (count < MAX_WORK_STEP_COUNT)
                    {
                        pEvalData.GetWorkMgr().FromFormatted("", "Evaluate the " + (i + 1).ToString() + MathHelper.GetCountingPrefix((i + 1)) + " term");
                        lastStep = pEvalData.GetWorkMgr().GetLast();
                    }

                    if (lastStep != null)
                        lastStep.GoDown(ref pEvalData);

                    ExComp simpInnerEx = TermType.SimplifyGenTermType.BasicSimplify(innerTerm.CloneEx().ToAlgTerm().RemoveRedundancies(false), ref pEvalData, true);

                    if (lastStep != null)
                        lastStep.GoUp(ref pEvalData);

                    if (lastStep != null)
                        lastStep.SetWorkHtml(WorkMgr.STM + innerTerm.FinalToDispStr() + "=" + WorkMgr.ToDisp(simpInnerEx) + WorkMgr.EDM);

                    totalTerm = AddOp.StaticCombine(totalTerm, simpInnerEx).ToAlgTerm();
                }

                return totalTerm.ForceCombineExponents();
            }

            return this;
        }

        public override string ToAsciiString()
        {
            return "\\sum_{" + GetIterVar() + "=" + GetIterStart() + "}^{" + GetIterCount() + "}" +
                GetInnerEx().ToAsciiString();
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return ToTexString();
        }

        public override string ToTexString()
        {
            return "\\Sigma_{" + GetIterVar() + "=" + GetIterStart() + "}^{" + GetIterCount() + "}" +
                GetInnerEx().ToTexString();
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            return new SumFunction(GetInnerTerm().Substitute(subOut, subIn),
                GetIterVar().IsEqualTo(subOut) ? (AlgebraComp)subIn : GetIterVar(),
                GetIterStart().ToAlgTerm().Substitute(subOut, subIn).RemoveRedundancies(false),
                GetIterCount().ToAlgTerm().Substitute(subOut, subIn).RemoveRedundancies(false));
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            ExComp useArg1;
            if (args[1] is AlgebraTerm)
                useArg1 = (args[1] as AlgebraTerm).RemoveRedundancies(false);
            else
                useArg1 = args[1];

            return new SumFunction(args[0], (AlgebraComp)useArg1, args[2], args[3]);
        }
    }
}