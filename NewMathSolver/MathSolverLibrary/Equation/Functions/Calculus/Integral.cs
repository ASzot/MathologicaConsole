using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class Integral : AppliedFunction
    {
        private bool _failure = false;
        protected AlgebraComp _dVar = null;
        private bool _addConstant = true;
        private IntegrationInfo _integralInfo = null;
        protected ExComp _upper = null;
        protected ExComp _lower = null;
        private bool _isInnerIntegral = false;

        public AlgebraTerm GetUpperLimitTerm()
        {
            return _upper.ToAlgTerm();
        }

        public AlgebraTerm GetLowerLimitTerm()
        {
            return _lower.ToAlgTerm();
        }

        public void SetUpperLimit(ExComp value)
        {
            _upper = value;
        }

        public ExComp GetUpperLimit()
        {
            return _upper;
        }

        public void SetLowerLimit(ExComp value)
        {
            _lower = value;
        }

        public ExComp GetLowerLimit()
        {
            return _lower;
        }

        public void SetAddConstant(bool value)
        {
            _addConstant = value;
        }

        public void SetInfo(IntegrationInfo value)
        {
            _integralInfo = value;
        }

        public void SetDVar(AlgebraComp value)
        {
            _dVar = value;
        }

        public AlgebraComp GetDVar()
        {
            return _dVar;
        }

        public bool GetIsDefinite()
        {
            return GetLowerLimit() != null && GetUpperLimit() != null;
        }

        public Integral(ExComp innerEx)
            : base(innerEx, FunctionType.AntiDerivative, typeof(Integral))
        {
        }

        public Integral(ExComp innerEx, bool isInnerIntegral)
            : this(innerEx)
        {
            _isInnerIntegral = isInnerIntegral;
        }

        public override ExComp CloneEx()
        {
            return ConstructIntegral(GetInnerTerm(), _dVar, GetLowerLimit(), GetUpperLimit(), _isInnerIntegral, true);
        }

        public static Integral ConstructIntegral(ExComp innerEx, AlgebraComp dVar)
        {
            return ConstructIntegral(innerEx, dVar, null, null, false, true);
        }

        private static Dictionary<string, Integral> GetIntegralDepths(Integral integral, ref Dictionary<string, Integral> dict, out ExComp baseValue)
        {
            if (integral.GetDVar() == null || ArrayFunc.ContainsKey(dict, integral.GetDVar().GetVar().GetVar()) || !integral.GetIsDefinite())
            {
                baseValue = integral.GetInnerTerm();
                return dict;
            }

            dict[integral.GetDVar().GetVar().GetVar()] = integral;

            ExComp innerEx = integral.GetInnerEx();
            if (innerEx is Integral)
            {
                Dictionary<string, Integral> intDepths = GetIntegralDepths(innerEx as Integral, ref dict, out baseValue);
                return intDepths;
            }

            baseValue = innerEx;
            return dict;
        }

        private static ExComp RearrangeIntegral(Integral inputIntegral)
        {
            ExComp baseValue;
            Dictionary<string, Integral> dict = new Dictionary<string, Integral>();

            dict = GetIntegralDepths(inputIntegral, ref dict, out baseValue);

            bool switched = false;

            foreach (KeyValuePair<string, Integral> kvPair in dict)
            {
                Integral integral = kvPair.Value;
                if (integral.GetLowerLimit().ToAlgTerm().Contains(integral.GetDVar()) || integral.GetUpperLimit().ToAlgTerm().Contains(integral.GetDVar()))
                {
                    // The variables need to be switched.
                    // Find a suitable place to switch the variable where the var is not the integration boundary.
                    foreach (KeyValuePair<string, Integral> compareKvPair in dict)
                    {
                        if (compareKvPair.Key != integral.GetDVar().GetVar().GetVar() &&
                            compareKvPair.Value.GetLowerLimit().ToAlgTerm().Contains(integral.GetDVar()) &&
                            compareKvPair.Value.GetUpperLimit().ToAlgTerm().Contains(integral.GetDVar()) &&
                            integral.GetLowerLimit().ToAlgTerm().Contains(compareKvPair.Value.GetDVar()) &&
                            integral.GetUpperLimit().ToAlgTerm().Contains(compareKvPair.Value.GetDVar()))
                        {
                            AlgebraComp tmp = kvPair.Value.GetDVar();
                            kvPair.Value.SetDVar(compareKvPair.Value.GetDVar());
                            switched = true;
                            break;
                        }
                    }
                }
            }

            if (!switched)
                return inputIntegral;

            // Go back to the regular integral form.
            ExComp overallIntegral = baseValue;
            foreach (Integral value in dict.Values)
            {
                overallIntegral = ConstructIntegral(overallIntegral, value.GetDVar(), value.GetLowerLimit(), value.GetUpperLimit(), false, false);
            }

            return overallIntegral;
        }

        public static Integral ConstructIntegral(ExComp innerEx, AlgebraComp dVar, ExComp lower, ExComp upper, bool isInner, bool rearrange)
        {
            Integral integral = new Integral(innerEx);
            integral._dVar = dVar;
            integral.SetLowerLimit(lower);
            integral.SetUpperLimit(upper);
            integral._isInnerIntegral = isInner;

            // In the case of multidimensional integrals variable boundaries will potentially have to be rearranged.
            if (innerEx is Integral)
            {
                Integral innerInt = innerEx as Integral;
                innerInt._isInnerIntegral = true;
                if (lower != null && upper != null && rearrange)
                    return RearrangeIntegral(integral) as Integral;
            }

            return integral;
        }

        public static ExComp TakeAntiDeriv(ExComp innerEx, AlgebraComp dVar, ref TermType.EvalData pEvalData)
        {
            Integral integral = ConstructIntegral(innerEx, dVar);
            integral._addConstant = false;
            ExComp evalIntegral = integral.Evaluate(false, ref pEvalData);

            return evalIntegral;
        }

        public override ExComp CancelWith(ExComp innerEx, ref TermType.EvalData evalData)
        {
            if (innerEx is Derivative)
            {
                Derivative innerDeriv = innerEx as Derivative;
                if (innerDeriv.GetWithRespectTo().IsEqualTo(_dVar) && innerDeriv.GetDerivOf() == null && innerDeriv.GetOrderInt() == 1)
                {
                    evalData.GetWorkMgr().FromSides(this, null, "The integral and the derivative cancel.");
                    return innerDeriv.GetInnerTerm();
                }
            }

            return null;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            AlgebraTerm upperLim = GetUpperLimit() == null ? null : GetUpperLimitTerm().Substitute(subOut, subIn);
            AlgebraTerm lowerLim = GetLowerLimit() == null ? null : GetLowerLimitTerm().Substitute(subOut, subIn);
            AlgebraTerm innerTerm = GetInnerTerm().Substitute(subOut, subIn);

            return ConstructIntegral(innerTerm, _dVar, lowerLim, upperLim, _isInnerIntegral, true);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            AlgebraTerm innerTerm = GetInnerTerm();
            ExComp innerEx = Simplifier.Simplify(innerTerm, ref pEvalData);
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).RemoveRedundancies(false);

            if (ExNumber.IsUndef(innerEx))
                return ExNumber.GetUndefined();

            if (innerEx is ExVector && !GetIsDefinite())
            {
                ExVector vec = innerEx as ExVector;

                // Take the anti derivative of each component separately.

                ExVector antiDerivVec = vec.CreateEmptyBody();

                for (int i = 0; i < vec.GetLength(); ++i)
                {
                    ExComp antiDeriv = Indefinite(vec.Get(i), ref pEvalData);
                    antiDerivVec.Set(i, antiDeriv);
                }

                return antiDerivVec;
            }
            else if (innerEx is ExMatrix)
            {
                // Don't know if this works.
                return ExNumber.GetUndefined();
            }

            string integralStr = FinalToDispStr();

            ExComp useUpper;
            ExComp useLower;

            if (GetUpperLimit() is ExNumber && (GetUpperLimit() as ExNumber).IsInfinity())
                useUpper = new AlgebraComp("$n");
            else
                useUpper = GetUpperLimit();

            if (GetLowerLimit() is ExNumber && (GetLowerLimit() as ExNumber).IsInfinity())
                useLower = new AlgebraComp("$n");
            else
                useLower = GetLowerLimit();

            if (useUpper != null && useLower != null && !useUpper.IsEqualTo(GetUpperLimit()) && !useLower.IsEqualTo(GetLowerLimit()))
            {
                // Evaluating from infinity in both directions.
                // Split the integral up.
                Integral upperInt = Integral.ConstructIntegral(GetInnerTerm(), _dVar, ExNumber.GetZero(), ExNumber.GetPosInfinity(), false, true);
                Integral lowerInt = Integral.ConstructIntegral(GetInnerTerm(), _dVar, ExNumber.GetNegInfinity(), ExNumber.GetZero(), false, true);
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + "=" + upperInt.FinalToDispStr() + "+" +
                    lowerInt.FinalToDispStr() + WorkMgr.EDM,
                    "Split the integral.");

                pEvalData.GetWorkMgr().FromFormatted("", "Evaluate the upper integral.");
                WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

                lastStep.GoDown(ref pEvalData);
                ExComp upperSideEval = upperInt.Evaluate(harshEval, ref pEvalData);
                lastStep.GoUp(ref pEvalData);

                lastStep.SetWorkHtml(WorkMgr.STM + upperInt.FinalToDispStr() + "=" + WorkMgr.ToDisp(upperSideEval) + WorkMgr.EDM);

                pEvalData.GetWorkMgr().FromFormatted("", "Evaluate the lower integral.");
                lastStep = pEvalData.GetWorkMgr().GetLast();

                lastStep.GoDown(ref pEvalData);
                ExComp lowerSideEval = lowerInt.Evaluate(harshEval, ref pEvalData);
                lastStep.GoUp(ref pEvalData);

                lastStep.SetWorkHtml(WorkMgr.STM + lowerInt.FinalToDispStr() + "=" + WorkMgr.ToDisp(lowerSideEval) + WorkMgr.EDM);

                ExComp added = AddOp.StaticCombine(upperSideEval, lowerSideEval);

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + WorkMgr.ToDisp(upperSideEval) + "+" + WorkMgr.ToDisp(lowerSideEval) +
                    "=" + WorkMgr.ToDisp(added) + WorkMgr.EDM, "Combine the integral back together.");

                return added;
            }

            AlgebraTerm indefinite = Indefinite(innerEx, ref pEvalData);
            if (_failure)
                return indefinite;      // Just 'this'

            ExComp indefiniteEx = indefinite.RemoveRedundancies(false);

            if (GetLowerLimit() == null || GetUpperLimit() == null)
            {
                if (_addConstant && !_isInnerIntegral && !(indefiniteEx is Integral))
                {
                    // Add the constant.
                    ExComp retEx = AddOp.StaticWeakCombine(indefinite, new CalcConstant());
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + retEx.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM,
                        "Add the constant of integration.");
                    return retEx;
                }
                else
                    return indefinite;
            }

            AlgebraTerm upperEval = indefinite.CloneEx().ToAlgTerm().Substitute(_dVar, useUpper);
            ExComp upperEx = Simplifier.Simplify(new AlgebraTerm(upperEval), ref pEvalData);

            AlgebraTerm lowerEval = indefinite.CloneEx().ToAlgTerm().Substitute(_dVar, useLower);
            ExComp lowerEx = Simplifier.Simplify(new AlgebraTerm(lowerEval), ref pEvalData);

            AlgebraComp subVar = null;
            ExComp limVal = null;
            if (!useUpper.IsEqualTo(GetUpperLimit()))
            {
                subVar = useUpper as AlgebraComp;
                limVal = ExNumber.GetPosInfinity();
            }
            else if (!useLower.IsEqualTo(GetLowerLimit()))
            {
                subVar = useLower as AlgebraComp;
                limVal = ExNumber.GetNegInfinity();
            }

            integralStr = "\\int_{" + WorkMgr.ToDisp(useLower) + "}^{" + WorkMgr.ToDisp(useUpper) + "}(" + GetInnerTerm().FinalToDispStr() + ")d" + _dVar.ToDispString();

            if (subVar != null)
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + "=\\lim_{" + subVar.ToDispString() +
                    " \\to \\infty}" + integralStr + WorkMgr.EDM);
                integralStr = "\\lim_{" + subVar.ToDispString() +
                    " \\to \\infty}" + integralStr;
            }

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + "=F(" +
                WorkMgr.ToDisp(useUpper) + ")-F(" + WorkMgr.ToDisp(useLower) + ")" + WorkMgr.EDM,
                "Evaluate the definite integral where F is the antiderivative.");

            string resultStr0 = SubOp.StaticWeakCombine(upperEx, lowerEx).ToAlgTerm().FinalToDispStr();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + "=" +
                resultStr0 + WorkMgr.EDM);

            ExComp result = SubOp.StaticCombine(upperEx, lowerEx);
            if (result is AlgebraTerm)
                result = (result as AlgebraTerm).CompoundFractions();

            result = TermType.SimplifyGenTermType.BasicSimplify(result, ref pEvalData, true);

            if (subVar != null)
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\lim_{" + subVar.ToDispString() + " \\to \\infty}" +
                    WorkMgr.ToDisp(result) + WorkMgr.EDM, "Take the limit to infinity.");
                result = Limit.TakeLim(result, subVar, limVal, ref pEvalData, 0);
            }

            pEvalData.AddInputType(TermType.InputAddType.IntDef);

            string resultStr1 = WorkMgr.ToDisp(result);
            integralStr = this.FinalToDispStr();
            if (resultStr0 != resultStr1)
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + "=" + resultStr1 + WorkMgr.EDM);

            return result;
        }

        private AlgebraTerm Indefinite(ExComp takeEx, ref TermType.EvalData pEvalData)
        {
            string thisStr = takeEx.ToAlgTerm().FinalToDispStr();

            // Split the integral up by groups.
            List<ExComp[]> gps = takeEx.CloneEx().ToAlgTerm().GetGroupsNoOps();

            if (gps.Count == 0)
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int\\d" + _dVar.ToDispString() + "=" + _dVar.ToDispString() + WorkMgr.EDM,
                    "Use the antiderivative power rule.");
                return _dVar.ToAlgTerm();
            }

            string[] intStrs = new string[gps.Count];

            if (gps.Count > 1)
            {
                string overallStr = "";
                for (int i = 0; i < gps.Count; ++i)
                {
                    intStrs[i] = "\\int" + GroupHelper.ToAlgTerm(gps[i]).FinalToDispStr() + "\\d" + _dVar.ToDispString();
                    overallStr += intStrs[i];
                    if (i != gps.Count - 1)
                        overallStr += "+";
                }

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + overallStr + WorkMgr.EDM, "Split the integral up.");
            }

            // Independently take the derivative of each group.
            ExComp[] adGps = new ExComp[gps.Count];
            for (int i = 0; i < gps.Count; ++i)
            {
                IntegrationInfo integrationInfo = _integralInfo ?? new IntegrationInfo();
                int prevStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

                string lowerStr = GetLowerLimit() == null ? "" : GetLowerLimit().ToAlgTerm().FinalToDispStr();
                string upperStr = GetUpperLimit() == null ? "" : GetUpperLimit().ToAlgTerm().FinalToDispStr();

                WorkStep last = null;
                if (gps.Count > 1)
                {
                    pEvalData.GetWorkMgr().FromFormatted("");
                    last = pEvalData.GetWorkMgr().GetLast();
                    last.GoDown(ref pEvalData);
                }

                ExComp aderiv = AntiDerivativeHelper.TakeAntiDerivativeGp(gps[i], _dVar, ref integrationInfo, ref pEvalData, "", "");

                if (gps.Count > 1)
                    last.GoUp(ref pEvalData);

                if (aderiv == null)
                {
                    pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - prevStepCount);
                    _failure = true;
                    return this;
                }

                if (gps.Count > 1)
                    last.SetWorkHtml(WorkMgr.STM + intStrs[i] + "=" + WorkMgr.ToDisp(aderiv) + WorkMgr.EDM);
                adGps[i] = aderiv;
            }

            // Convert to a term.
            ExComp finalEx = adGps[0];
            for (int i = 1; i < adGps.Length; ++i)
            {
                finalEx = AddOp.StaticCombine(finalEx, adGps[i].ToAlgTerm());
            }

            AlgebraTerm finalTerm = finalEx.ToAlgTerm();
            finalTerm = finalTerm.Order();
            if (adGps.Length > 1)
            {
                string definiteStr = GetIsDefinite() ? "|_{" + _lower.ToAlgTerm().FinalToDispStr() + "}^{" + _upper.ToAlgTerm().FinalToDispStr() + "}" : "";
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + thisStr + "=" + finalTerm.FinalToDispStr() + definiteStr + WorkMgr.EDM,
                    "Add all together.");
            }

            return finalTerm;
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return ConstructIntegral(args[0], this._dVar, GetLowerLimit(), GetUpperLimit(), false, true);
        }

        public override string FinalToAsciiString()
        {
            string boundariesStr = "";
            if (GetIsDefinite())
                boundariesStr = "_{" + GetLowerLimit().ToAsciiString() + "}^{" + GetUpperLimit().ToAsciiString() + "}";

            return "\\int" + boundariesStr + (GetInnerEx() is Integral ? GetInnerTerm().FinalToAsciiString() : "(" + GetInnerTerm().FinalToAsciiString() + ")") + "\\d" + _dVar.ToAsciiString();
        }

        public override string FinalToTexString()
        {
            string boundariesStr = "";
            if (GetIsDefinite())
                boundariesStr = "_{" + GetLowerLimit().ToTexString() + "}^{" + GetUpperLimit().ToTexString() + "}";
            return "\\int" + boundariesStr + (GetInnerEx() is Integral ? GetInnerTerm().FinalToTexString() : "(" + GetInnerTerm().FinalToTexString() + ")") + "\\d" + _dVar.ToTexString();
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Integral)
            {
                Integral integral = ex as Integral;
                return integral._dVar.IsEqualTo(this._dVar) && integral.GetInnerEx().IsEqualTo(this.GetInnerEx());
            }

            return false;
        }

        public override string ToAsciiString()
        {
            string boundariesStr = "";
            if (GetIsDefinite())
                boundariesStr = "_{" + GetLowerLimit().ToAsciiString() + "}^{" + GetUpperLimit().ToAsciiString() + "}";
            return "\\int" + boundariesStr + "(" + GetInnerTerm().ToAsciiString() + ")\\d" + _dVar.ToAsciiString();
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            string boundariesStr = "";
            if (GetIsDefinite())
                boundariesStr = "_{" + GetLowerLimit().ToString() + "}^{" + GetUpperLimit().ToString() + "}";
            return "\\int" + boundariesStr + "(" + GetInnerTerm().ToString() + ")\\d" + _dVar.ToString();
        }

        public override string ToTexString()
        {
            string boundariesStr = "";
            if (GetIsDefinite())
                boundariesStr = "_{" + GetLowerLimit().ToTexString() + "}^{" + GetUpperLimit().ToTexString() + "}";
            return "\\int" + boundariesStr + "(" + GetInnerTerm().ToTexString() + ")\\d" + _dVar.ToTexString();
        }
    }
}