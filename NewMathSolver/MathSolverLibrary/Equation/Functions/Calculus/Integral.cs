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

            if (_lower == null || _upper == null)
            {
                //// Add the constant.
                //return AddOp.StaticWeakCombine(indefinite, new CalcConstant());
                return indefinite;
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

            return SubOp.StaticCombine(upperEx, lowerEx);
        }

        private AlgebraTerm Indefinite(ref TermType.EvalData pEvalData)
        {
            // Split the integral up by groups.
            List<ExComp[]> gps = InnerTerm.GetGroupsNoOps();

            string overallStr = "";
            for (int i = 0; i < gps.Count; ++i)
            {
                overallStr += "\\int" + gps[i].FinalToMathAsciiString() + _dVar.ToDispString();
                if (i != gps.Count - 1)
                    overallStr += "+";
            }

            pEvalData.WorkMgr.FromFormatted(overallStr, "Split the integral up.");

            // Independantly take the derivative of each group.
            ExComp[] adGps = new ExComp[gps.Count];
            for (int i = 0; i < gps.Count; ++i)
            {
                ExComp aderiv = AntiDerivativeHelper.TakeAntiDerivativeGp(gps[i], _dVar, ref pEvalData);
                if (aderiv == null)
                    return this;
                adGps[i] = aderiv;
            }

            // Convert to a term.
            AlgebraTerm finalTerm = new AlgebraTerm();
            for (int i = 0; i < adGps.Length; ++i)
            {
                finalTerm.Add(adGps[i].ToAlgTerm());
                if (i != adGps.Length - 1)
                    finalTerm.Add(new AddOp());
            }

            return finalTerm;
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return ConstructIntegral(args[0], this._dVar, _lower, _upper);
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return "\\int" + InnerTerm.FinalToAsciiKeepFormatting() + "d" + _dVar.ToMathAsciiString();
        }

        public override string FinalToAsciiString()
        {
            return "\\int" + InnerTerm.FinalToAsciiKeepFormatting() + "d" + _dVar.ToMathAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            return "\\int" + InnerTerm.FinalToTexKeepFormatting() + "d" + _dVar.ToTexString();
        }

        public override string FinalToTexString()
        {
            return "\\int" + InnerTerm.FinalToTexString() + "d" + _dVar.ToTexString();
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
            return "\\int" + InnerTerm.ToMathAsciiString() + "d" + _dVar.ToMathAsciiString();
        }

        public override string ToSearchString()
        {
            return "\\int" + InnerTerm.ToSearchString() + "d" + _dVar.ToSearchString();
        }

        public override string ToString()
        {
            return "\\int" + InnerTerm.ToString() + "d" + _dVar.ToString();
        }

        public override string ToTexString()
        {
            return "\\int" + InnerTerm.ToTexString() + "d" + _dVar.ToTexString();
        }
    }
}
