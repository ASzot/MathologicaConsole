using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    class Integral : AppliedFunction
    {
        private bool _failure = false;
        private AlgebraComp _dVar = null;
        private bool _addConstant = true;
        private IntegrationInfo _integralInfo = null;
        private ExComp _upper = null;
        private ExComp _lower = null;


        private ExComp UpperLimit
        {
            get { return _upper; }
            set 
            {
                _upper = value;
            }
        }
        private ExComp LowerLimit
        {
            get { return _lower; }
            set
            {
                _lower = value;
            }
        }

        public bool AddConstant
        {
            set { _addConstant = value; }
        }

        public IntegrationInfo Info
        {
            set { _integralInfo = value; }
        }

        public AlgebraComp DVar
        {
            get { return _dVar; }
        }

        public bool IsDefinite
        {
            get { return LowerLimit != null && UpperLimit != null; }
        }

        public Integral(ExComp innerEx)
            : base(innerEx, FunctionType.AntiDerivative, typeof(Integral))
        {

        }


        public override ExComp Clone()
        {
            return ConstructIntegral(InnerTerm, _dVar, LowerLimit, UpperLimit);
        }

        public static Integral ConstructIntegral(ExComp innerEx, AlgebraComp dVar)
        {
            return ConstructIntegral(innerEx, dVar, null, null);
        }

        public static Integral ConstructIntegral(ExComp innerEx, AlgebraComp dVar, ExComp lower, ExComp upper)
        {
            Integral integral = new Integral(innerEx);
            integral._dVar = dVar;
            integral.LowerLimit = lower;
            integral.UpperLimit = upper;

            return integral;
        }

        public static ExComp TakeAntiDeriv(ExComp innerEx, AlgebraComp dVar, ref TermType.EvalData pEvalData)
        {
            Integral integral = ConstructIntegral(innerEx, dVar);
            integral._addConstant = false;
            return integral.Evaluate(false, ref pEvalData);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = InnerEx;
            if (innerEx is Derivative)
            {
                Derivative innerDeriv = innerEx as Derivative;
                if (innerDeriv.WithRespectTo.IsEqualTo(_dVar) && innerDeriv.DerivOf == null && innerDeriv.OrderInt == 1)
                {
                    pEvalData.WorkMgr.FromSides(this, null, "The integral and the derivative cancel.");
                    return innerDeriv.InnerTerm;
                }
            }
            else if (innerEx is ExVector && !IsDefinite)
            {
                ExVector vec = innerEx as ExVector;
                // Take the anti derivative of each component seperately.

                ExVector antiDerivVec = new ExVector(vec.Length);

                // Work steps should go here.

                for (int i = 0; i < vec.Length; ++i)
                {
                    ExComp antiDeriv = Indefinite(vec.Get(i), ref pEvalData);
                    antiDerivVec.Set(i, antiDeriv);
                }

                return antiDerivVec;
            }
            else if (innerEx is ExMatrix)
            {
                // Don't know if this works.
                return Number.Undefined;
            }

            string integralStr = FinalToDispStr();

            ExComp useUpper;
            ExComp useLower;

            if (UpperLimit is Number && (UpperLimit as Number).IsInfinity())
                useUpper = new AlgebraComp("$n");
            else
                useUpper = UpperLimit;

            if (LowerLimit is Number && (LowerLimit as Number).IsInfinity())
                useLower = new AlgebraComp("$n");
            else
                useLower = LowerLimit;


            if (useUpper != UpperLimit && useLower != LowerLimit)
            {
                // Evaluating from infinity in both directions. 
                // Split the integral up.
                Integral upperInt = Integral.ConstructIntegral(InnerTerm, _dVar, Number.Zero, Number.PosInfinity);
                Integral lowerInt = Integral.ConstructIntegral(InnerTerm, _dVar, Number.NegInfinity, Number.Zero);
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=" + lowerInt.FinalToDispStr() + "=" + 
                    upperInt.FinalToDispStr() + WorkMgr.EDM,
                    "Split the integral.");

                ExComp upperSideEval = upperInt.Evaluate(harshEval, ref pEvalData);
                ExComp lowerSideEval = lowerInt.Evaluate(harshEval, ref pEvalData);

                ExComp added = AddOp.StaticCombine(upperSideEval, lowerSideEval);

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(upperSideEval) + "+" + WorkMgr.ExFinalToAsciiStr(lowerSideEval) +
                    "=" + WorkMgr.ExFinalToAsciiStr(added) + WorkMgr.EDM, "Combine the integral back together.");

                return added;
            }

            AlgebraTerm indefinite = Indefinite(InnerTerm, ref pEvalData);
            if (_failure)
                return indefinite;      // Just 'this'

            if (LowerLimit == null || UpperLimit == null)
            {
                if (_addConstant)
                {
                    // Add the constant.
                    ExComp retEx = AddOp.StaticWeakCombine(indefinite, new CalcConstant());
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + retEx.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM,
                        "Add the constant of integration.");
                    return retEx;
                }
                else
                    return indefinite;
            }

            AlgebraTerm upperEval = indefinite.Clone().ToAlgTerm().Substitute(_dVar, useUpper);
            if (upperEval.Contains(_dVar))
            {
                pEvalData.AddFailureMsg("Internal error evaluating antiderivative");
                return this;
            }
            ExComp upperEx = Simplifier.Simplify(new AlgebraTerm(upperEval), ref pEvalData);

            AlgebraTerm lowerEval = indefinite.Clone().ToAlgTerm().Substitute(_dVar, useLower);
            if (lowerEval.Contains(_dVar))
            {
                pEvalData.AddFailureMsg("Internal error evaluating antiderivative");
                return this;
            }
            ExComp lowerEx = Simplifier.Simplify(new AlgebraTerm(lowerEval), ref pEvalData);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=F(" +
                WorkMgr.ExFinalToAsciiStr(UpperLimit) + ")-F(" + WorkMgr.ExFinalToAsciiStr(LowerLimit) + ")" + WorkMgr.EDM,
                "Evaluate the definite integral where F is the antiderivative.");

            string resultStr0 = SubOp.StaticWeakCombine(upperEx, lowerEx).ToAlgTerm().FinalToDispStr();

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=" +
                resultStr0 + WorkMgr.EDM);

            ExComp result = SubOp.StaticCombine(upperEx, lowerEx);
            AlgebraComp subVar = null;
            ExComp limVal = null;
            if (useUpper != UpperLimit)
            {
                subVar = useUpper as AlgebraComp;
                limVal = Number.PosInfinity;
            }
            else if (useLower != LowerLimit)
            {
                subVar = useLower as AlgebraComp;
                limVal = Number.NegInfinity;
            }

            if (subVar != null)
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=\\lim_{" + subVar.ToDispString() +
                    " \\to \\infty} \\int_{" + subVar.ToDispString() + "}^{" + WorkMgr.ExFinalToAsciiStr(result) + "} + " +
                    InnerTerm.FinalToDispStr() + "d" + _dVar.ToDispString() + WorkMgr.EDM);

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\lim_{" + subVar.ToDispString() + " \\to \\infty}" +
                    WorkMgr.ExFinalToAsciiStr(result) + WorkMgr.EDM, "Take the limit to infinity.");

                result = Limit.TakeLim(result, subVar, limVal, ref pEvalData);
            }

            pEvalData.AddInputType(TermType.InputAddType.IntDef);

            string resultStr1 = WorkMgr.ExFinalToAsciiStr(result);
            if (resultStr0 != resultStr1)
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=" + resultStr1 + WorkMgr.EDM);

            return result;
        }

        private AlgebraTerm Indefinite(ExComp takeEx, ref TermType.EvalData pEvalData)
        {
            string thisStr = takeEx.ToAlgTerm().FinalToDispStr();

            // Split the integral up by groups.
            List<ExComp[]> gps = takeEx.Clone().ToAlgTerm().GetGroupsNoOps();

            if (gps.Count == 0)
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int\\d" + _dVar.ToDispString() + "=" + _dVar.ToDispString() + WorkMgr.EDM,
                    "Use the antiderivative power rule.");
                return _dVar.ToAlgTerm();
            }

            if (gps.Count > 1)
            {
                string overallStr = "";
                for (int i = 0; i < gps.Count; ++i)
                {
                    overallStr += "\\int" + gps[i].FinalToMathAsciiString() + "\\d" + _dVar.ToDispString();
                    if (i != gps.Count - 1)
                        overallStr += "+";
                }

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + overallStr + WorkMgr.EDM, "Split the integral up.");
            }

            // Independantly take the derivative of each group.
            ExComp[] adGps = new ExComp[gps.Count];
            for (int i = 0; i < gps.Count; ++i) 
            {
                IntegrationInfo integrationInfo = _integralInfo ?? new IntegrationInfo();
                int prevStepCount = pEvalData.WorkMgr.WorkSteps.Count;

                ExComp aderiv = AntiDerivativeHelper.TakeAntiDerivativeGp(gps[i], _dVar, ref integrationInfo, ref pEvalData);

                if (aderiv == null)
                {
                    pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - prevStepCount);
                    _failure = true;
                    return this;
                }
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
                string definiteStr = IsDefinite ? "|_{" + _lower.ToAlgTerm().FinalToDispStr() + "}^{" + _upper.ToAlgTerm().FinalToDispStr() + "}" : "";
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + thisStr + "=" + finalTerm.FinalToDispStr() + definiteStr + WorkMgr.EDM,
                    "Add all together.");
            }

            return finalTerm;
        } 

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return ConstructIntegral(args[0], this._dVar, LowerLimit, UpperLimit);
        }

        public override string FinalToAsciiKeepFormatting()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToAsciiString() + "}^{" + UpperLimit.ToAsciiString() +"}";
            return "\\int" + boundariesStr + "(" + InnerTerm.FinalToAsciiKeepFormatting() + ")\\d" + _dVar.ToAsciiString();
        }

        public override string FinalToAsciiString()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToAsciiString() + "}^{" + UpperLimit.ToAsciiString() + "}";
            return "\\int" + boundariesStr + "(" + InnerTerm.FinalToAsciiKeepFormatting() + ")\\d" + _dVar.ToAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToTexString() + "}^{" + UpperLimit.ToTexString() + "}";
            return "\\int" + boundariesStr + "(" + InnerTerm.FinalToTexKeepFormatting() + ")\\d" + _dVar.ToTexString();
        }

        public override string FinalToTexString()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToTexString() + "}^{" + UpperLimit.ToTexString() + "}";
            return "\\int" + boundariesStr + "(" + InnerTerm.FinalToTexString() + ")\\d" + _dVar.ToTexString();
        }
        
        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Integral)
            {
                Integral integral = ex as Integral;
                return integral._dVar.IsEqualTo(this._dVar) && integral.InnerEx.IsEqualTo(this.InnerEx);
            }

            return false;
        }

        public override string ToAsciiString()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToAsciiString() + "}^{" + UpperLimit.ToAsciiString() + "}";
            return "\\int" + boundariesStr + "(" + InnerTerm.ToAsciiString() + ")\\d" + _dVar.ToAsciiString();
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToString() + "}^{" + UpperLimit.ToString() + "}";
            return "\\int" + boundariesStr + "(" + InnerTerm.ToString() + ")\\d" + _dVar.ToString();
        }

        public override string ToTexString()
        {
            string boundariesStr = "";
            if (IsDefinite)
                boundariesStr = "_{" + LowerLimit.ToTexString() + "}^{" + UpperLimit.ToTexString()+ "}";
            return "\\int" + boundariesStr + "(" + InnerTerm.ToTexString() + ")\\d" + _dVar.ToTexString();
        }

    }
}
