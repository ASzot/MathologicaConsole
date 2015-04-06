using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.TermType;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

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
                    (varTo.Length == 0 ? "1" : varTo.ToAlgTerm().FinalToDispStr()) + ")\\d" + dVar.ToDispString();
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
                if (gp[0] is PowerFunction)
                {
                    PowerFunction gpPf = gp[0] as PowerFunction;
                    if (gpPf.Power.IsEqualTo(Number.NegOne) && gpPf.Base is PowerFunction)
                    {
                        PowerFunction baseGpPf = gpPf.Base as PowerFunction;
                        baseGpPf.Power = MulOp.StaticCombine(baseGpPf.Power, gpPf.Power);
                        if (baseGpPf.Power is AlgebraTerm)
                            baseGpPf.Power = (baseGpPf.Power as AlgebraTerm).RemoveRedundancies();
                        gp[0] = baseGpPf;
                    }
                }

                atmpt = GetIsSingleFunc(gp[0], dVar, ref pEvalData);
                if (atmpt != null)
                {
                    pEvalData.AttemptSetInputType(InputType.IntBasicFunc);
                    return atmpt;
                }

                if (pIntInfo.ByPartsCount < IntegrationInfo.MAX_BY_PARTS_COUNT && (gp[0] is LogFunction || gp[0] is InverseTrigFunction))
                {
                    string thisStr = gp.ToAlgTerm().FinalToDispStr();
                    pIntInfo.IncPartsCount();
                    atmpt = SingularIntByParts(gp[0], dVar, thisStr, ref pIntInfo, ref pEvalData);
                    if (atmpt != null)
                    {
                        pEvalData.AttemptSetInputType(InputType.IntParts);
                        return atmpt;
                    }
                }
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
                    pEvalData.AttemptSetInputType(InputType.IntBasicFunc);
                    return ad;
                }
            }

            if (pIntInfo.USubCount < IntegrationInfo.MAX_U_SUB_COUNT)
            {
                pIntInfo.IncSubCount();
                atmpt = AttemptUSub(gp, dVar, ref pIntInfo, ref pEvalData);

                if (atmpt != null)
                {
                    pEvalData.AttemptSetInputType(InputType.IntUSub);
                    return atmpt;
                }
            }

            if (gp.Length == 2)
            {
                //ExComp[] den = gp.GetDenominator();
                //if (den != null && den.Length == 1)
                //{
                //    ExComp[] num = gp.GetNumerator();
                //    if (num.Length == 1)
                //    {
                //        atmpt = AttemptPartialFractions(num[0], den[0], dVar, ref pIntInfo, ref pEvalData);
                //        if (atmpt != null)
                //            return atmpt;
                //    }
                //}

                if (pIntInfo.ByPartsCount < IntegrationInfo.MAX_BY_PARTS_COUNT)
                {
                    int prevWorkStepCount = pEvalData.WorkMgr.WorkSteps.Count;
                    string thisStr = gp.ToAlgTerm().FinalToDispStr();
                    pIntInfo.IncPartsCount();
                    atmpt = IntByParts(gp[0], gp[1], dVar, thisStr, ref pIntInfo, ref pEvalData);
                    if (atmpt != null)
                    {
                        pEvalData.AttemptSetInputType(InputType.IntParts);
                        return atmpt;
                    }
                    else
                        pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - prevWorkStepCount);
                }
            }
            

            return null;
        }

        private static ExComp AttemptPartialFractions(ExComp num, ExComp den, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            if (!(num is AlgebraTerm) || !(den is AlgebraTerm))
                return null;

            AlgebraTerm numTerm = num as AlgebraTerm;
            AlgebraTerm denTerm = den as AlgebraTerm;

            PolynomialExt numPoly = new PolynomialExt();
            PolynomialExt denPoly = new PolynomialExt();

            if (!numPoly.Init(numTerm) || !denPoly.Init(denTerm))
                return null;

            if (denPoly.MaxPow < 2 || numPoly.MaxPow >= denPoly.MaxPow)
                return null;

            ExComp atmpt = PartialFracs.Evaluate(numTerm, denTerm, numPoly, dVar, ref pIntInfo, ref pEvalData);
            if (atmpt == null)
                return null;

            return atmpt;
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

                            if (groups[0].Length < 7)
                            {
                                for (int j = 0; j < groups[0].Length; ++j)
                                {
                                    if (groups[0][j] is AgOp || groups[0][j] is AlgebraComp || !groups[0][j].ToAlgTerm().Contains(dVar))
                                        continue;
                                    potentialU.Add(groups[0][j]);
                                }
                            }
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
                int prevWorkStepCount = pEvalData.WorkMgr.WorkSteps.Count;
                ExComp attempt = TryU(group, potentialU, dVar, ref pIntInfo, ref pEvalData);
                if (attempt != null)
                    return attempt;
                else
                    pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - prevWorkStepCount);
            }

            return null;
        }

        private static ExComp TryU(ExComp[] group, ExComp uatmpt, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            string groupStr = group.CloneGroup().ToArray().ToAlgTerm().FinalToDispStr();
            string thisStr = "\\int(" + groupStr + ")" + dVar.ToDispString();
            string atmptStr = uatmpt.ToAlgTerm().FinalToDispStr();

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int (" + groupStr + ") \\d" + dVar.ToDispString() + WorkMgr.EDM, 
                "Do the u-substitution integration method.");

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
            
            Derivative derivative = Derivative.ConstructDeriv(uatmpt, dVar, null);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + thisStr + WorkMgr.EDM,
                "Substitute " + WorkMgr.STM + subInVar.ToDispString() + "=" + uatmpt.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\frac{d}{d" + dVar.ToDispString() + "}" + subInVar.ToDispString() + "=\\frac{d}{d" + dVar.ToDispString() + "}[" + atmptStr + "]" + WorkMgr.EDM, 
                "Find " + WorkMgr.STM + "d" + subInVar.ToDispString() + WorkMgr.EDM);

            ExComp evaluated = derivative.Evaluate(false, ref pEvalData);

            if (evaluated is Derivative)
                return null;

            group = updatedGroups[0];

            var groups = evaluated.ToAlgTerm().GetGroupsNoOps();
            ExComp constEx = null;

            if (groups.Count == 1)
            {
                ExComp[] singularGp = groups[0];
                ExComp[] varTo, constTo;
                singularGp.GetConstVarTo(out varTo, out constTo, dVar);

                constEx = constTo.Length == 0 ? Number.One : (ExComp)AlgebraTerm.FromFraction(Number.One, constTo.ToAlgTerm());

                if (varTo.Length == 0)
                {
                    if (group.GroupContains(dVar))
                        return null;

                    pEvalData.WorkMgr.WorkSteps.Add(new WorkStep(WorkMgr.STM + thisStr +
                        WorkMgr.EDM, "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=" + 
                        atmptStr + WorkMgr.EDM + " and " + WorkMgr.STM + "d" + subInVar.ToDispString() + "=" + 
                        evaluated.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM));

                    
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + constEx.ToAlgTerm().FinalToDispStr() + "\\int (" + group.ToArray().ToAlgTerm().FinalToDispStr() + ") d" + subInVar.ToDispString() + WorkMgr.EDM);

                    ExComp innerAntiDeriv = TakeAntiDerivativeGp(group.ToArray(), subInVar, ref pIntInfo, ref pEvalData);
                    if (innerAntiDeriv == null)
                        return null;

                    pEvalData.WorkMgr.FromSides(MulOp.StaticWeakCombine(constEx, innerAntiDeriv), null, "Substitute back in " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                        atmptStr + WorkMgr.EDM);

                    // Sub back in the appropriate values.
                    innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                    ExComp retEx = MulOp.StaticCombine(innerAntiDeriv, constEx);
                    pEvalData.WorkMgr.FromSides(retEx, null);
                    return retEx;
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

                    pEvalData.WorkMgr.WorkSteps.Add(new WorkStep(WorkMgr.STM + thisStr +
                        WorkMgr.EDM, "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                        atmptStr + WorkMgr.EDM + " and " + WorkMgr.STM + "d" + subInVar.ToDispString() + "=" +
                        evaluated.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM));

                    bool mulInCost = constEx != null && !Number.One.IsEqualTo(constEx);
                    string mulInCostStr = (mulInCost ? constEx.ToAlgTerm().FinalToDispStr() : "");

                    group = groupList.ToArray();

                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + mulInCostStr + 
                        "\\int (" + group.ToAlgTerm().FinalToDispStr() + ") d" + subInVar.ToDispString() + WorkMgr.EDM);

                    ExComp innerAntiDeriv = TakeAntiDerivativeGp(group, subInVar, ref pIntInfo, ref pEvalData);
                    if (innerAntiDeriv == null)
                        return null;

                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + mulInCostStr + "(" + innerAntiDeriv.ToAlgTerm().FinalToDispStr() + ")" + WorkMgr.EDM, "Substitute back in " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                    uatmpt.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

                    // Sub back in the appropriate values.
                    innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                    ExComp retEx;

                    if (mulInCost)
                        retEx = MulOp.StaticCombine(constEx, innerAntiDeriv);
                    else
                        retEx = innerAntiDeriv;

                    pEvalData.WorkMgr.FromSides(retEx, null);
                    return retEx;
                }
                else if (group[j] is PowerFunction && evaluated is PowerFunction && (group[j] as PowerFunction).Power.IsEqualTo((evaluated as PowerFunction).Power))
                {
                    PowerFunction groupPf = group[j] as PowerFunction;
                    PowerFunction evaluatedPf = evaluated as PowerFunction;

                    var baseGps = groupPf.Base.ToAlgTerm().GetGroupsNoOps();
                    if (baseGps.Count == 1)
                    {
                        // Search the base for like terms. 
                        for (int k = 0; k < baseGps[0].Length; ++k)
                        {
                            if (baseGps[0][k].IsEqualTo(evaluatedPf.Base))
                            {
                                List<ExComp> baseGpsList = baseGps[0].ToList();
                                baseGpsList.RemoveAt(k);

                                group[j] = new PowerFunction(baseGpsList.ToArray().ToAlgTerm(), evaluatedPf.Power);

                                pEvalData.WorkMgr.WorkSteps.Add(new WorkStep(WorkMgr.STM + thisStr +
                                    WorkMgr.EDM, "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                    atmptStr + WorkMgr.EDM + " and " + WorkMgr.STM + "d" + subInVar.ToDispString() + "=" +
                                    evaluated.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM));

                                bool mulInCost = constEx != null && !Number.One.IsEqualTo(constEx);
                                string mulInCostStr = (mulInCost ? constEx.ToAlgTerm().FinalToDispStr() : "");

                                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + mulInCostStr +
                                    "\\int (" + group.ToAlgTerm().FinalToDispStr() + ") d" + subInVar.ToDispString() + WorkMgr.EDM);

                                ExComp innerAntiDeriv = TakeAntiDerivativeGp(group, subInVar, ref pIntInfo, ref pEvalData);
                                if (innerAntiDeriv == null)
                                    return null;

                                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + mulInCostStr + "(" + innerAntiDeriv.ToAlgTerm().FinalToDispStr() + ")" + WorkMgr.EDM, "Substitute back in " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                    uatmpt.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

                                // Sub back in the appropriate values.
                                innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                                ExComp retEx;

                                if (mulInCost)
                                    retEx = MulOp.StaticCombine(constEx, innerAntiDeriv);
                                else
                                    retEx = innerAntiDeriv;

                                pEvalData.WorkMgr.FromSides(retEx, null);
                                return retEx;
                            }
                        }
                    }

                }
            }

            return null;
        }

        private static ExComp SingularIntByParts(ExComp ex0, AlgebraComp dVar, string thisSTr, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            return IntByParts(ex0, Number.One, dVar, thisSTr, ref pIntInfo, ref pEvalData);
        }

        private static ExComp IntByParts(ExComp ex0, ExComp ex1, AlgebraComp dVar, string thisStr, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            // Integration by parts states \int{uv'}=uv-\int{vu'}
            // Choose either ex0 or ex1 to be a suitable u and v'

            //if (ex0 is PowerFunction && (ex0 as PowerFunction).Power.IsEqualTo(Number.NegOne) && (ex0 as PowerFunction).Base is PowerFunction)
            //{
            //    PowerFunction ex0Pf = ex0 as PowerFunction;

            //    ex0 = new PowerFunction((ex0Pf.Base as PowerFunction).Base, MulOp.StaticCombine((ex0Pf.Base as PowerFunction).Power, ex0Pf.Power));
            //}

            //if (ex1 is PowerFunction && (ex1 as PowerFunction).Power.IsEqualTo(Number.NegOne) && (ex1 as PowerFunction).Base is PowerFunction)
            //{
            //    PowerFunction ex1Pf = ex0 as PowerFunction;

            //    ex1 = new PowerFunction((ex1Pf.Base as PowerFunction).Base, MulOp.StaticCombine((ex1Pf.Base as PowerFunction).Power, ex1Pf.Power));
            //}

            ExComp u, dv;
            if (ex0 is Number)
            {
                u = ex1;
                dv = ex0;
            }
            else if (ex1 is Number)
            {
                u = ex0;
                dv = ex1;
            }
            else if ((ex0 is PowerFunction && (ex0 as PowerFunction).Power is Number && 
                !((ex0 as PowerFunction).Base is PowerFunction && !(((ex0 as PowerFunction).Base as PowerFunction).Power is Number))) || 
                ex0 is AlgebraComp)
            {

                u = ex0;
                dv = ex1;
            }
            else if ((ex1 is PowerFunction && (ex1 as PowerFunction).Power is Number &&
                !((ex1 as PowerFunction).Base is PowerFunction && !(((ex1 as PowerFunction).Base as PowerFunction).Power is Number))) ||
                ex1 is AlgebraComp)
            {
                u = ex1;
                dv = ex0;
            }
            else if (ex1 is AppliedFunction)
            {
                u = ex0;
                dv = ex1;
            }
            else if (ex0 is AppliedFunction)
            {
                u = ex1;
                dv = ex0;
            }
            else
                return null;

            int stepCount = pEvalData.WorkMgr.WorkSteps.Count;
            Integral antiderivativeOfDV = Integral.ConstructIntegral(dv.Clone(), dVar);
            antiderivativeOfDV.Info = pIntInfo;
            antiderivativeOfDV.AddConstant = false;
            ExComp v = antiderivativeOfDV.Evaluate(false, ref pEvalData);

            if (v is Integral)
            {
                pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - stepCount);
                // Try to switch the variables.
                ExComp tmp = u;
                u = dv;
                dv = tmp;

                antiderivativeOfDV = Integral.ConstructIntegral(dv, dVar);
                antiderivativeOfDV.Info = pIntInfo;
                antiderivativeOfDV.AddConstant = false;
                v = antiderivativeOfDV.Evaluate(false, ref pEvalData);
                if (v is Integral)
                    return null;
            }

            var stepRange = pEvalData.WorkMgr.WorkSteps.GetRange(stepCount, pEvalData.WorkMgr.WorkSteps.Count - stepCount);
            pEvalData.WorkMgr.WorkSteps.RemoveRange(stepCount, pEvalData.WorkMgr.WorkSteps.Count - stepCount);

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + thisStr + ")d" + dVar.ToDispString() + WorkMgr.EDM,
                "Integrate by parts using the formula " + WorkMgr.STM + "\\int u v' = uv - \\int v u' " + WorkMgr.EDM + " where " +
                WorkMgr.STM + "u=" + u.ToAlgTerm().FinalToDispStr() + ", dv = " + dv.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

            Derivative derivativeOfU = Derivative.ConstructDeriv(u, dVar, null);
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "du=" + derivativeOfU.FinalToDispStr() + WorkMgr.EDM, "Find " + WorkMgr.STM +
                "du" + WorkMgr.EDM);
            ExComp du = derivativeOfU.Evaluate(false, ref pEvalData);

            if (du is Derivative)
                return null;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "v=" + antiderivativeOfDV.FinalToDispStr() + WorkMgr.EDM, "Find " + WorkMgr.STM +
                "v" + WorkMgr.EDM);

            pEvalData.WorkMgr.WorkSteps.AddRange(stepRange);


            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0})({1})-\\int ({1}) ({2}) d" + dVar.ToDispString() + WorkMgr.EDM, 
                "Substitute the values into the integration by parts formula.", u, v, du);

            ExComp uv = MulOp.StaticCombine(u, v.Clone());
            ExComp vDu = MulOp.StaticCombine(v, du);

            Integral antiDerivVDU = Integral.ConstructIntegral(vDu, dVar);
            antiDerivVDU.Info = pIntInfo;
            antiDerivVDU.AddConstant = false;
            ExComp antiDerivVDUEval = antiDerivVDU.Evaluate(false, ref pEvalData);
            if (antiDerivVDUEval is Integral)
                return null;

            ExComp final = SubOp.StaticCombine(uv, antiDerivVDUEval);
            pEvalData.WorkMgr.FromSides(final, null);

            return final;
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
                else if (!pf.Base.ToAlgTerm().Contains(dVar) && pf.Power.IsEqualTo(dVar))
                {
                    ExComp finalEx = DivOp.StaticWeakCombine(pf, LogFunction.Ln(pf.Base));
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() +
                        "=" + finalEx.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative law.");

                    return finalEx;
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

                    string changedPowStr = WorkMgr.ExFinalToAsciiStr(AddOp.StaticWeakCombine(pf.Power, Number.One));
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "\\int(" + WorkMgr.ExFinalToAsciiStr(single) + ")\\d" + dVar.ToDispString() +
                        "=\\frac{" + dVar.ToDispString() + "^{" + changedPowStr + "}}{" + changedPowStr + "}" + WorkMgr.EDM,
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

            return null;
        }
    }
}
