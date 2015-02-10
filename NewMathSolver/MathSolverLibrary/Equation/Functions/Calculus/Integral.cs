using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    class Integral : AppliedFunction
    {
        private bool _failure = false;
        private AlgebraComp _dVar = null;
        private ExComp _lower = null;
        private ExComp _upper = null;

        public Integral(ExComp innerEx)
            : base(innerEx, FunctionType.AntiDerivative, typeof(Integral))
        {

        }


        public override ExComp Clone()
        {
            return ConstructIntegral(InnerTerm, _dVar, _lower, _upper);
        }

        public static Integral ConstructIntegral(ExComp innerEx, AlgebraComp dVar)
        {
            return ConstructIntegral(innerEx, dVar, null, null);
        }

        public static Integral ConstructIntegral(ExComp innerEx, AlgebraComp dVar, ExComp lower, ExComp upper)
        {

            Integral integral = new Integral(innerEx);
            integral._dVar = dVar;
            integral._lower = lower;
            integral._upper = upper;

            return integral;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm indefinite = Indefinite(ref pEvalData);
            if (_failure)
                return indefinite;      // Just 'this'

            if (_lower == null || _upper == null)
            {
                // Add the constant.
                return AddOp.StaticWeakCombine(indefinite, new CalcConstant());
            }

            AlgebraTerm upperEval = indefinite.Clone().ToAlgTerm().Substitute(_dVar, _upper);
            if (upperEval.Contains(_dVar))
            {
                pEvalData.AddFailureMsg("Internal error evaluating antiderivative");
                return this;
            }
            ExComp upperEx = Simplifier.Simplify(upperEval, ref pEvalData);

            AlgebraTerm lowerEval = indefinite.Clone().ToAlgTerm().Substitute(_dVar, _lower);
            if (lowerEval.Contains(_dVar))
            {
                pEvalData.AddFailureMsg("Internal error evaluating antiderivative");
                return this;
            }
            ExComp lowerEx = Simplifier.Simplify(lowerEval, ref pEvalData);

            string integralStr = FinalToDispStr();
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=F(" +
                WorkMgr.ExFinalToAsciiStr(_upper) + ")-F(" + WorkMgr.ExFinalToAsciiStr(_lower) + ")" + WorkMgr.EDM,
                "Evaluate the definite integral where F is the antiderivative.");

            string resultStr0 = SubOp.StaticWeakCombine(upperEx, lowerEx).ToAlgTerm().FinalToDispStr();

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=" +
                resultStr0 + WorkMgr.EDM);

            ExComp result = SubOp.StaticCombine(upperEx, lowerEx);

            string resultStr1 = WorkMgr.ExFinalToAsciiStr(result);
            if (resultStr0 != resultStr1)
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + integralStr + "=" + resultStr1 + WorkMgr.EDM);

            return result;
        }

        private AlgebraTerm Indefinite(ref TermType.EvalData pEvalData)
        {
            string thisStr = FinalToDispStr();
            // Split the integral up by groups.
            List<ExComp[]> gps = InnerTerm.Clone().ToAlgTerm().GetGroupsNoOps();

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
                ExComp aderiv = AntiDerivativeHelper.TakeAntiDerivativeGp(gps[i], _dVar, ref pEvalData);
                if (aderiv == null)
                {
                    pEvalData.AddMsg("At this time only very simple integration works");
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
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + thisStr + "=" + finalTerm.FinalToDispStr() + WorkMgr.EDM, 
                    "Add all together.");

            return finalTerm;
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return ConstructIntegral(args[0], this._dVar, _lower, _upper);
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return "\\int(" + InnerTerm.FinalToAsciiKeepFormatting() + ")\\d" + _dVar.ToMathAsciiString();
        }

        public override string FinalToAsciiString()
        {
            return "\\int(" + InnerTerm.FinalToAsciiKeepFormatting() + ")\\d" + _dVar.ToMathAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            return "\\int(" + InnerTerm.FinalToTexKeepFormatting() + ")\\d" + _dVar.ToTexString();
        }

        public override string FinalToTexString()
        {
            return "\\int(" + InnerTerm.FinalToTexString() + ")\\d" + _dVar.ToTexString();
        }
        
        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Integral)
            {
                Integral integral = ex as Integral;
                return integral._dVar.IsEqualTo(this._dVar);
            }

            return false;
        }

        public override string ToMathAsciiString()
        {
            return "\\int(" + InnerTerm.ToMathAsciiString() + ")\\d" + _dVar.ToMathAsciiString();
        }

        public override string ToSearchString()
        {
            return "\\int(" + InnerTerm.ToSearchString() + ")\\d" + _dVar.ToSearchString();
        }

        public override string ToString()
        {
            return "\\int(" + InnerTerm.ToString() + ")\\d" + _dVar.ToString();
        }

        public override string ToTexString()
        {
            return "\\int(" + InnerTerm.ToTexString() + ")\\d" + _dVar.ToTexString();
        }
    }
}
