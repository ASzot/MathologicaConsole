using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    static class AntiDerivativeHelper
    {
        public static ExComp TakeAntiDerivativeGp(ExComp[] gp, AlgebraComp dVar, ref EvalData pEvalData)
        {
            // Take out all of the constants.
            ExComp[] varTo, constTo;
            gp.GetConstVarTo(out varTo, out constTo, dVar);
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + constTo.FinalToMathAsciiString() + "\\int" + 
                varTo.FinalToMathAsciiString() + WorkMgr.EDM, "Take out the constants.");
            ExComp antiDeriv = TakeAntiDerivativeVarGp(varTo, dVar, ref pEvalData);
            if (antiDeriv == null) 
                return null;

            if (constTo.Length != 0)
                return MulOp.StaticCombine(antiDeriv, constTo.ToAlgTerm());
            else
                return antiDeriv;
        }

        private static ExComp TakeAntiDerivativeVarGp(ExComp[] gp, AlgebraComp dVar, ref EvalData pEvalData)
        {
            // For later make a method that makes the appropriate substitutions.
            // If the inside of a function isn't just a variable and the derivative
            // isn't variable, make the substitution. 


            // Derivative of nothing is just the variable being integrated with respect to.
            if (gp.Length == 0)
                return dVar;

            ExComp atmpt = null;

            // Is this a single function? (power included)
            if (gp.Length == 1 && gp[0] is AlgebraFunction)
            {
                atmpt = GetIsSingleFunc(gp[0], dVar, ref pEvalData);
                if (atmpt != null)
                    return atmpt;
            }
            else if (gp.Length == 1 && gp[0] is AlgebraComp)
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0}^(1+1))/(1+1)=({0}^2)/(2)" + WorkMgr.EDM,
                    "Use the antiderivative power rule stating " + WorkMgr.STM + "\\intx^n\\dx=(x^(n+1))/(n+1)" + WorkMgr.EDM, gp[0]);
                return AlgebraTerm.FromFraction(new PowerFunction(gp[0], new Number(2.0)), new Number(2.0));
            }
            else if (gp.Length == 2)      // Is this two functions multiplied together?
            {
                // Are they two of the common antiderivatives?
                if (gp[0] is TrigFunction && gp[1] is TrigFunction && (gp[0] as TrigFunction).InnerEx.IsEqualTo(dVar) && 
                    (gp[0] as TrigFunction).InnerEx.IsEqualTo(dVar))
                {
                    TrigFunction ad = null;
                    if ((gp[0] is SecFunction && gp[1] is TanFunction) ||
                        (gp[0] is TanFunction && gp[1] is SecFunction))
                        ad = new SecFunction(dVar);
                    else if ((gp[0] is CscFunction && gp[1] is CotFunction) ||
                        (gp[0] is CotFunction && gp[1] is CscFunction))
                        ad = new CscFunction(dVar);

                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int" + (gp[0] as TrigFunction).FinalToDispStr() +
                        (gp[1] as TrigFunction).FinalToDispStr() + "\\d" + dVar.ToDispString() + "=" + ad.FinalToDispStr() + WorkMgr.EDM, 
                        "Use the common antiderivative.");
                }

                atmpt = IntByParts(gp[0], gp[1], dVar, ref pEvalData);
                if (atmpt != null)
                    return atmpt;
            }

            return null;
        }

        private static ExComp IntByParts(ExComp ex0, ExComp ex1, AlgebraComp dVar, ref EvalData pEvalData)
        {
            // Integration by parts states \int{uv'}=uv-\int{vu'}


            return null;
        }

        private static bool IsDerivAcceptable(ExComp ex, AlgebraComp dVar)
        {
            if (ex is PowerFunction)
                return (ex as PowerFunction).Base.IsEqualTo(dVar);
            else if (ex is AppliedFunction)
                return (ex as AppliedFunction).InnerEx.IsEqualTo(dVar);

            return false;
        }

        private static ExComp GetIsSingleFunc(ExComp single, AlgebraComp dVar, ref EvalData pEvalData)
        {
            if (single is PowerFunction)
            {
                // Add one to the power and then divide by the power.
                PowerFunction pf = single as PowerFunction;

                if (pf.Base.IsEqualTo(Constant.E) && pf.Power.IsEqualTo(dVar))
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int" + pf.FinalToDispStr() + "\\d" + dVar.ToDispString() +
                        "=" + pf.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                    return pf;
                }

                if (!(pf.Power is Number))
                    return null;       
                Number power = pf.Power as Number;

                if (IsDerivAcceptable(pf, dVar))
                {
                    // The special case for the power function anti-dervivative.
                    if (Number.NegOne.Equals(power))
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int" + pf.FinalToDispStr() + "\\d" + dVar.ToDispString() +
                            "=ln(|" + dVar + "|)" + WorkMgr.EDM, "This comes from the known derivative " + WorkMgr.STM + 
                            "\\frac{d}{dx}ln(x)=\\frac{1}{x}" + WorkMgr.EDM);
                        return LogFunction.Ln(new AbsValFunction(dVar));
                    }

                    ExComp powChange = AddOp.StaticCombine(pf.Power, Number.One);

                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int" + WorkMgr.ExFinalToAsciiStr(single) + "\\d" + dVar.ToDispString() +
                        "=" + dVar.ToDispString() + "^{" + WorkMgr.ExFinalToAsciiStr(AddOp.StaticWeakCombine(pf.Power, Number.One)) + "}" + WorkMgr.EDM,
                        "Use the power rule of antiderivatives.");

                    pf.Power = powChange;
                    return DivOp.StaticCombine(pf, powChange);
                }
                else if ((new Number(2.0)).IsEqualTo(pf.Power) && pf.Base is TrigFunction && (pf.Base as TrigFunction).InnerEx.IsEqualTo(dVar))
                {
                    ExComp ad = null;
                    if (pf.Base is SecFunction)
                        ad = new TanFunction(dVar);
                    else if (pf.Base is CscFunction)
                        ad = MulOp.Negate(new CotFunction(dVar));

                    if (ad != null)
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int" + pf.FinalToDispStr() + "\\d" + dVar.ToDispString() + "=" +
                            WorkMgr.ExFinalToAsciiStr(ad) + WorkMgr.EDM, "Use the common antiderivative.");

                        return ad;
                    }
                }
            }
            else if (single is TrigFunction)
            {
                if (!IsDerivAcceptable(single, dVar))
                    return null;

                ExComp ad = null;
                if (single is SinFunction)
                {
                    ad = MulOp.Negate(new CosFunction(dVar));
                }
                else if (single is CosFunction)
                {
                    ad = new SinFunction(dVar);
                }
                else if (single is TanFunction)
                {
                    ad = LogFunction.Ln(new AbsValFunction(new SecFunction(dVar)));
                }
                else if (single is CscFunction)
                {
                    ad = LogFunction.Ln(new AbsValFunction(SubOp.StaticCombine(new CscFunction(dVar), new CotFunction(dVar))));
                }
                else if (single is SecFunction)
                {
                    ad = LogFunction.Ln(new AbsValFunction(SubOp.StaticCombine(new SecFunction(dVar), new TanFunction(dVar))));
                }
                else if (single is CotFunction)
                {
                    ad = LogFunction.Ln(new AbsValFunction(new SinFunction(dVar)));
                }

                if (ad == null)
                    return null;

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int" + (single as TrigFunction).FinalToDispStr() + "\\d" + dVar.ToDispString() + 
                    WorkMgr.EDM, 
                    "Use the common antiderivative.");
            }
            else if (single is InverseTrigFunction)
            {

            }
            else if (single is LogFunction)
            {

            }

            return null;
        }
    }
}
