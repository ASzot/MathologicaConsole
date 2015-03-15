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
        public static ExComp TakeAntiDerivativeGp(ExComp[] gp, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + gp.ToAlgTerm().FinalToDispStr() + ")\\d" + dVar.ToDispString() + WorkMgr.EDM,
                "Evaluate the integral.");

            // Take out all of the constants.
            ExComp[] varTo, constTo;
            gp.GetConstVarTo(out varTo, out constTo, dVar);

            string constOutStr = "";
            string constToStr = constTo.ToAlgTerm().FinalToDispStr();
            if (constTo.Length > 0)
            {
                constOutStr = constToStr + "\\int(" +
                    varTo.ToAlgTerm().FinalToDispStr() + ")\\d" + dVar.ToDispString();
                if (constToStr != "1")
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + constOutStr + WorkMgr.EDM, "Take out the constants.");
            }

            ExComp antiDeriv = TakeAntiDerivativeVarGp(varTo, dVar, ref pIntInfo, ref pEvalData);
            if (antiDeriv == null) 
                return null;

            if (constTo.Length != 0)
            {
                if (constToStr != "1")
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + constOutStr + "=" + WorkMgr.ExFinalToAsciiStr(constTo.ToAlgTerm()) +
                        WorkMgr.ExFinalToAsciiStr(antiDeriv) + WorkMgr.EDM, "Multiply the constants back in.");
                }
                return MulOp.StaticCombine(antiDeriv, constTo.ToAlgTerm());
            }
            else
                return antiDeriv;
        }

        private static ExComp TakeAntiDerivativeVarGp(ExComp[] gp, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            // For later make a method that makes the appropriate substitutions.
            // If the inside of a function isn't just a variable and the derivative
            // isn't variable, make the substitution. 


            // Derivative of nothing is just the variable being integrated with respect to.
            if (gp.Length == 0)
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int\\d" + dVar.ToDispString() + "=" + dVar.ToDispString() + WorkMgr.EDM,
                    "Use the antiderivative power rule.");
                return dVar;
            }

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
                    "Use the antiderivative power rule.", gp[0]);
                return AlgebraTerm.FromFraction(new PowerFunction(gp[0], new Number(2.0)), new Number(2.0));
            }
            else if (gp.Length == 2)      // Is this two functions multiplied together?
            {
				ExComp ad = null;
                // Are they two of the common antiderivatives?
                if (gp[0] is TrigFunction && gp[1] is TrigFunction && (gp[0] as TrigFunction).InnerEx.IsEqualTo(dVar) && 
                    (gp[0] as TrigFunction).InnerEx.IsEqualTo(dVar))
                {
                    if ((gp[0] is SecFunction && gp[1] is TanFunction) ||
                        (gp[0] is TanFunction && gp[1] is SecFunction))
                        ad = new SecFunction(dVar);
                    else if ((gp[0] is CscFunction && gp[1] is CotFunction) ||
                        (gp[0] is CotFunction && gp[1] is CscFunction))
                        ad = MulOp.Negate(new CscFunction(dVar));
                }

                if (ad != null)
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + gp[0].ToAlgTerm().FinalToDispStr() +
                        gp[1].ToAlgTerm().FinalToDispStr() + ")\\d" + dVar.ToDispString() + "=" +
                        ad.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM,
                        "Use the common antiderivative.");
                }
            }

            if (pIntInfo.USubCount < IntegrationInfo.MAX_U_SUB_COUNT)
            {
                atmpt = AttemptUSub(gp, dVar, ref pIntInfo, ref pEvalData);
                if (atmpt != null)
                    return atmpt;
            }

            if (gp.Length == 2)
            {
                ExComp[] den = gp.GetDenominator();
                if (den != null && den.Length == 1)
                {
                    ExComp[] num = gp.GetNumerator();
                    if (num.Length == 1)
                    {
                        atmpt = AttemptPartialFractions(num[0], den[0], dVar, ref pEvalData);
                        if (atmpt != null)
                            return atmpt;
                    }
                }

                atmpt = IntByParts(gp[0], gp[1], dVar, ref pEvalData);
                if (atmpt != null)
                    return atmpt;
            }
            

            return null;
        }

        private static ExComp AttemptPartialFractions(ExComp num, ExComp den, AlgebraComp dVar, ref EvalData pEvalData)
        {
            return null;
        }

        private static List<ExComp> GetPotentialU(ExComp[] group, AlgebraComp dVar)
        {
            List<ExComp> potentialU = new List<ExComp>();

            for (int i = 0; i < group.Length; ++i)
            {
                ExComp innerEx = null;
                ExComp gpCmp = group[i];
                if (gpCmp is PowerFunction)
                {
                    ExComp baseEx = ((PowerFunction)gpCmp).Base;
                    if (!(baseEx is AlgebraComp) && baseEx.ToAlgTerm().Contains(dVar))
                    {
                        potentialU.Add(baseEx);
                        var groups = baseEx.ToAlgTerm().GetGroups();
                        if (groups.Count == 1)
                        {
                            List<ExComp> additionalBase = GetPotentialU(groups[0], dVar);
                            potentialU.AddRange(additionalBase);
                        }
                    }

                    ExComp powerEx = ((PowerFunction)gpCmp).Power;
                    if (!(powerEx is AlgebraComp) && powerEx.ToAlgTerm().Contains(dVar))
                    {
                        potentialU.Add(powerEx);
                        var groups = powerEx.ToAlgTerm().GetGroups();
                        if (groups.Count == 1)
                        {
                            List<ExComp> additionalPower = GetPotentialU(powerEx.ToAlgTerm().SubComps.ToArray(), dVar);
                            potentialU.AddRange(additionalPower);
                        }
                    }

                }
                else if (gpCmp is BasicAppliedFunc)
                    innerEx = ((BasicAppliedFunc)gpCmp).InnerEx;

                if (innerEx == null || innerEx is AlgebraComp || !innerEx.ToAlgTerm().Contains(dVar))
                    continue;

                potentialU.Add(innerEx);
            }

            return potentialU;
        }

        private static ExComp AttemptUSub(ExComp[] group, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            // looking for f'(x)g(f(x))

            List<ExComp> potentialUs = GetPotentialU(group, dVar);

            foreach (ExComp potentialU in potentialUs)
            {
                ExComp attempt = TryU(group, potentialU, dVar, ref pIntInfo, ref pEvalData);
                if (attempt != null)
                    return attempt;
            }

            return null;
        }

        private static ExComp TryU(ExComp[] group, ExComp uatmpt, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            Derivative derivative = Derivative.CreateDeriv(uatmpt, dVar, null);
            ExComp evaluated = derivative.Evaluate(false, ref pEvalData);

            if (evaluated is Derivative)
                return null;

            AlgebraTerm term = group.ToAlgNoRedunTerm();
            AlgebraComp subInVar;
            if (term.Contains(new AlgebraComp("u")))
            {
                if (term.Contains(new AlgebraComp("w")))
                    subInVar = new AlgebraComp("v");
                else
                    subInVar = new AlgebraComp("w");
            }
            else
                subInVar = new AlgebraComp("u");

            bool success = false;
            term = term.Substitute(uatmpt, subInVar, ref success);
            if (!success)
                return null;
            var updatedGroups = term.GetGroupsNoOps();
            // The group count started as one and should not have been altered by substitutions.
            if (updatedGroups.Count != 1)
                return null;

            group = updatedGroups[0];

            var groups = evaluated.ToAlgTerm().GetGroupsNoOps();
            ExComp constEx = null;

            if (groups.Count == 1)
            {
                ExComp[] singularGp = groups[0];
                ExComp[] varTo, constTo;
                singularGp.GetConstVarTo(out varTo, out constTo, dVar);

                constEx = AlgebraTerm.FromFraction(Number.One, constTo.ToAlgTerm());

                if (varTo.Length == 0)
                {
                    ExComp innerAntiDeriv = TakeAntiDerivativeGp(group.ToArray(), subInVar, ref pIntInfo, ref pEvalData);
                    if (innerAntiDeriv == null)
                        return null;

                    // Sub back in the appropriate values.
                    innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                    return MulOp.StaticCombine(innerAntiDeriv, constEx);
                }

                evaluated = varTo.ToAlgTerm().RemoveRedundancies();
            }
            else
            {
                ExComp[] groupGcf = evaluated.ToAlgTerm().GetGroupGCF();
                ExComp[] varTo, constTo;
                groupGcf.GetConstVarTo(out varTo, out constTo, dVar);

                AlgebraTerm constToAg = constTo.ToAlgTerm();
                evaluated = DivOp.StaticCombine(evaluated, constToAg.Clone());
                constEx = AlgebraTerm.FromFraction(Number.One, constToAg);
            }

            for (int j = 0; j < group.Length; ++j)
            {
                if (group[j].IsEqualTo(evaluated))
                {
                    List<ExComp> groupList = group.ToList();
                    groupList.RemoveAt(j);

                    pIntInfo.IncSubCount();
                    ExComp innerAntiDeriv = TakeAntiDerivativeGp(groupList.ToArray(), subInVar, ref pIntInfo, ref pEvalData);
                    if (innerAntiDeriv == null)
                        return null;

                    // Sub back in the appropriate values.
                    innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                    if (constEx != null)
                        return MulOp.StaticCombine(constEx, innerAntiDeriv);
                    else
                        return innerAntiDeriv;
                }
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


				if (pf.Power.IsEqualTo(Number.NegOne))
				{
					ExComp pfBase = pf.Base;

					if (pfBase is PowerFunction)
					{
						PowerFunction pfBasePf = pfBase as PowerFunction;
						if (pfBasePf.Power.Equals(new Number(0.5)) || pfBasePf.Power.Equals(AlgebraTerm.FromFraction(Number.One, new Number(2.0))))
						{
							// Is this arcsin or arccos?
                            ExComp compare = AddOp.StaticCombine(MulOp.Negate(PowOp.StaticCombine(dVar, new Number(2.0))), Number.One).ToAlgTerm().RemoveRedundancies();


							ExComp useBase;
							if (pfBasePf.Base is AlgebraTerm)
								useBase = (pfBasePf.Base as AlgebraTerm).RemoveRedundancies();
							else
								useBase = pfBasePf.Base;

                            if (useBase.IsEqualTo(compare))
                            {
                                ASinFunction asin = new ASinFunction(dVar);
                                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(\\frac{1}{sqrt{1-" + dVar.ToDispString() + "^2}})\\d" + dVar.ToDispString() +
                                    "=" + asin.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                                return asin;
                            }
						}

                        // See if it is just the regular (1/x^(1/2)) or something like that.
                        if (IsDerivAcceptable(pfBasePf, dVar) && !pfBasePf.Power.ToAlgTerm().Contains(dVar))
                        {
                            ExComp power = MulOp.Negate(pfBasePf.Power);
                            ExComp powChange = AddOp.StaticCombine(power, Number.One);
                            if (powChange is AlgebraTerm)
                                powChange = (powChange as AlgebraTerm).CompoundFractions();

                            pfBasePf.Power = powChange;
                            return DivOp.StaticCombine(pfBasePf, powChange);
                        }
					}

                    if (pfBase.IsEqualTo(AddOp.StaticCombine(Number.One, PowOp.StaticCombine(dVar, new Number(2.0)))))
                    {
                        ATanFunction atan = new ATanFunction(dVar);
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(\\frac{1}{" + dVar.ToDispString() + "^2+1})\\d" + dVar.ToDispString() +
                            "=" + atan.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                        return atan;
                    }
				}

                if (pf.Base.IsEqualTo(Constant.E) && pf.Power.IsEqualTo(dVar))
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() +
                        "=" + pf.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                    return pf;
                }

                if (IsDerivAcceptable(pf, dVar) && !pf.Power.ToAlgTerm().Contains(dVar))
                {
                    // The special case for the power function anti-dervivative.
                    if (Number.NegOne.Equals(pf.Power))
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() +
                            "=ln(|" + dVar + "|)" + WorkMgr.EDM, "This comes from the known derivative " + WorkMgr.STM + 
                            "\\frac{d}{dx}ln(x)=\\frac{1}{x}" + WorkMgr.EDM);
                        return LogFunction.Ln(new AbsValFunction(dVar));
                    }

                    ExComp powChange = AddOp.StaticCombine(pf.Power, Number.One);
                    if (powChange is AlgebraTerm)
                        powChange = (powChange as AlgebraTerm).CompoundFractions();

                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + WorkMgr.ExFinalToAsciiStr(single) + ")\\d" + dVar.ToDispString() +
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
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() + "=" +
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

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + (single as TrigFunction).FinalToDispStr() + ")\\d" + dVar.ToDispString() + 
                    WorkMgr.EDM, 
                    "Use the common antiderivative.");

                return ad;
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
