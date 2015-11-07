using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.TermType;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal static class AntiDerivativeHelper
    {
        public static ExComp TakeAntiDerivativeGp(ExComp[] gp, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData, string lowerStr, string upperStr)
        {
            string boundaryStr = (lowerStr == "" ? "" : "_" + lowerStr) + (upperStr == "" ? "" : "^" + upperStr);
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int" + boundaryStr + "(" + GroupHelper.ToAlgTerm(gp).FinalToDispStr() + ")\\d" + dVar.ToDispString() + WorkMgr.EDM,
                "Evaluate the integral.");

            // Take out all of the constants.
            ExComp[] varTo, constTo;
            gp = GroupHelper.ForceDistributeExponent(gp);
            GroupHelper.GetConstVarTo(gp, out varTo, out constTo, dVar);

            if (varTo.Length == 1 && varTo[0] is PowerFunction && (varTo[0] as PowerFunction).GetPower().IsEqualTo(ExNumber.GetNegOne()))
            {
                PowerFunction pf = varTo[0] as PowerFunction;

                List<ExComp[]> denGps = pf.GetBase().ToAlgTerm().GetGroupsNoOps();
                if (denGps.Count == 1)
                {
                    // To be able to find the coefficient on a term like (28x)^4
                    ExComp[] denGp = GroupHelper.ForceDistributeExponent(denGps[0]);
                    ExComp[] denVarTo, denConstTo;

                    GroupHelper.GetConstVarTo(denGp, out denVarTo, out denConstTo, dVar);

                    if (denConstTo.Length != 0 && !(denConstTo.Length == 1 && denConstTo[0].IsEqualTo(ExNumber.GetOne())))
                    {
                        ExComp[] tmpConstTo = new ExComp[constTo.Length + 1];
                        for (int i = 0; i < constTo.Length; ++i)
                        {
                            tmpConstTo[i] = constTo[i];
                        }

                        tmpConstTo[tmpConstTo.Length - 1] = new PowerFunction(GroupHelper.ToAlgTerm(denConstTo), ExNumber.GetNegOne());
                        constTo = tmpConstTo;
                        varTo = new ExComp[] { new PowerFunction(GroupHelper.ToAlgTerm(denVarTo), ExNumber.GetNegOne()) };
                    }
                }
            }

            string constOutStr = "";
            string constToStr = GroupHelper.ToAlgTerm(constTo).FinalToDispStr();
            if (constTo.Length > 0)
            {
                constOutStr = constToStr + "\\int(" +
                    (varTo.Length == 0 ? "1" : GroupHelper.ToAlgTerm(varTo).FinalToDispStr()) + ")\\d" + dVar.ToDispString();
                if (constToStr.Trim() != "1")
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + constOutStr + WorkMgr.EDM, "Take out the constants.");
            }

            if (varTo.Length == 1 && varTo[0] is AlgebraTerm)
            {
                AlgebraTerm varToTerm = varTo[0] as AlgebraTerm;
                List<ExComp[]> gps = varToTerm.GetGroupsNoOps();
                if (gps.Count > 1)
                {
                    // This integral should be split up even further.
                    string overallStr = "";
                    string[] gpsStrs = new string[gps.Count];

                    for (int i = 0; i < gps.Count; ++i)
                    {
                        gpsStrs[i] = "\\int" + GroupHelper.ToAlgTerm(gps[i]).FinalToDispStr() + "\\d" + dVar.ToDispString();
                        overallStr += gpsStrs[i];
                        if (i != gps.Count - 1)
                            overallStr += "+";
                    }

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + overallStr + WorkMgr.EDM, "Split the integral up.");

                    // Independently take the derivative of each group.
                    ExComp[] adGps = new ExComp[gps.Count];
                    for (int i = 0; i < gps.Count; ++i)
                    {
                        IntegrationInfo integrationInfo = new IntegrationInfo();
                        int prevStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

                        pEvalData.GetWorkMgr().FromFormatted("");
                        WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

                        lastStep.GoDown(ref pEvalData);
                        ExComp aderiv = AntiDerivativeHelper.TakeAntiDerivativeGp(gps[i], dVar, ref integrationInfo, ref pEvalData, lowerStr, upperStr);
                        lastStep.GoUp(ref pEvalData);

                        if (aderiv == null)
                        {
                            pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - prevStepCount);
                            return null;
                        }

                        lastStep.SetWorkHtml(WorkMgr.STM + gpsStrs[i] + "=" + WorkMgr.ToDisp(aderiv) + WorkMgr.EDM);

                        adGps[i] = aderiv;
                    }

                    // Convert to a term.
                    ExComp finalEx = adGps[0];
                    for (int i = 1; i < adGps.Length; ++i)
                    {
                        finalEx = AddOp.StaticCombine(finalEx, adGps[i].ToAlgTerm());
                    }

                    if (adGps.Length > 1)
                        pEvalData.GetWorkMgr().FromSides(finalEx, null, "Add back together.");

                    if (constTo.Length > 0)
                    {
                        finalEx = MulOp.StaticCombine(finalEx, GroupHelper.ToAlgTerm(constTo));
                        pEvalData.GetWorkMgr().FromSides(finalEx, null, "Multiply the constants back in.");
                    }

                    return finalEx;
                }
            }

            ExComp antiDeriv = TakeAntiDerivativeVarGp(varTo, dVar, ref pIntInfo, ref pEvalData);
            if (antiDeriv == null)
                return null;

            if (constTo.Length != 0)
            {
                if (constToStr.Trim() != "1")
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + constOutStr + "=" + WorkMgr.ToDisp(GroupHelper.ToAlgTerm(constTo)) +
                        WorkMgr.ToDisp(antiDeriv) + WorkMgr.EDM, "Multiply the constants back in.");
                }
                return MulOp.StaticCombine(antiDeriv, GroupHelper.ToAlgTerm(constTo));
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
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int\\d" + dVar.ToDispString() + "=" + dVar.ToDispString() + WorkMgr.EDM,
                    "Use the antiderivative power rule.");
                return dVar;
            }

            ExComp atmpt = null;

            // Is this a single function? (power included)
            if (gp.Length == 1 && gp[0] is AlgebraFunction)
            {
                if (gp[0] is PowerFunction)
                    gp[0] = (gp[0] as PowerFunction).ForceCombineExponents();

                atmpt = GetIsSingleFunc(gp[0], dVar, ref pEvalData);
                if (atmpt != null)
                {
                    pEvalData.AttemptSetInputType(InputType.IntBasicFunc);
                    return atmpt;
                }

                if (pIntInfo.ByPartsCount < IntegrationInfo.MAX_BY_PARTS_COUNT && (gp[0] is LogFunction || gp[0] is InverseTrigFunction))
                {
                    string thisStr = GroupHelper.ToAlgTerm(gp).FinalToDispStr();
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
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0}^(1+1))/(1+1)=({0}^2)/(2)" + WorkMgr.EDM,
                    "Use the antiderivative power rule.", gp[0]);
                return AlgebraTerm.FromFraction(new PowerFunction(gp[0], new ExNumber(2.0)), new ExNumber(2.0));
            }
            else if (gp.Length == 2)      // Is this two functions multiplied together?
            {
                ExComp ad = null;
                // Are they two of the common antiderivatives?
                if (gp[0] is TrigFunction && gp[1] is TrigFunction && (gp[0] as TrigFunction).GetInnerEx().IsEqualTo(dVar) &&
                    (gp[0] as TrigFunction).GetInnerEx().IsEqualTo(dVar))
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
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + gp[0].ToAlgTerm().FinalToDispStr() +
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

            if (gp.Length == 1 || gp.Length == 2)
            {
                ExComp[] den = GroupHelper.GetDenominator(gp, false);
                if (den != null && den.Length == 1)
                {
                    ExComp[] num = GroupHelper.GetNumerator(gp);
                    if (num.Length == 1)
                    {
                        int prePFWorkStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                        atmpt = AttemptPartialFractions(num[0], den[0], dVar, ref pIntInfo, ref pEvalData);
                        if (atmpt != null)
                            return atmpt;
                        pEvalData.GetWorkMgr().PopStepsCount(prePFWorkStepCount);
                    }
                }
            }

            // Trig substitutions.
            int prevTrigSubWorkCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            atmpt = (new TrigSubTech()).TrigSubstitution(gp, dVar, ref pEvalData);
            if (atmpt != null)
                return atmpt;
            else
                pEvalData.GetWorkMgr().PopSteps(prevTrigSubWorkCount);

            if (gp.Length == 2)
            {
                // Using trig identities to make substitutions.
                atmpt = TrigFuncIntegration(gp[0], gp[1], dVar, ref pIntInfo, ref pEvalData);
                if (atmpt != null)
                    return atmpt;

                // Integration by parts.
                if (pIntInfo.ByPartsCount < IntegrationInfo.MAX_BY_PARTS_COUNT)
                {
                    int prevWorkStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                    string thisStr = GroupHelper.ToAlgTerm(gp).FinalToDispStr();
                    pIntInfo.IncPartsCount();
                    // Is one of these a denominator because if so don't do integration by parts.
                    if (!(gp[0] is PowerFunction && (gp[0] as PowerFunction).IsDenominator()) && !(gp[1] is PowerFunction && (gp[1] as PowerFunction).IsDenominator()))
                    {
                        atmpt = IntByParts(gp[0], gp[1], dVar, thisStr, ref pIntInfo, ref pEvalData);
                        if (atmpt != null)
                        {
                            pEvalData.AttemptSetInputType(InputType.IntParts);
                            return atmpt;
                        }
                        else
                            pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - prevWorkStepCount);
                    }
                }
            }

            return null;
        }

        private static ExComp TrigFuncIntegration(ExComp ex0, ExComp ex1, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            int tf0Pow = -1;
            int tf1Pow = -1;
            TrigFunction tf0 = null;
            TrigFunction tf1 = null;

            if (ex0 is TrigFunction)
            {
                tf0Pow = 1;
                tf0 = ex0 as TrigFunction;
            }
            else if (ex0 is PowerFunction)
            {
                PowerFunction pf0 = ex0 as PowerFunction;
                if (pf0.GetBase() is TrigFunction && pf0.GetPower() is ExNumber && (pf0.GetPower() as ExNumber).IsRealInteger())
                {
                    tf0 = pf0.GetBase() as TrigFunction;
                    tf0Pow = (int)(pf0.GetPower() as ExNumber).GetRealComp();
                }
            }
            else
                return null;

            if (ex1 is TrigFunction)
            {
                tf1Pow = 1;
                tf1 = ex1 as TrigFunction;
            }
            else if (ex1 is PowerFunction)
            {
                PowerFunction pf1 = ex1 as PowerFunction;
                if (pf1.GetBase() is TrigFunction && pf1.GetPower() is ExNumber && (pf1.GetPower() as ExNumber).IsRealInteger())
                {
                    tf1 = pf1.GetBase() as TrigFunction;
                    tf1Pow = (int)(pf1.GetPower() as ExNumber).GetRealComp();
                }
            }
            else
                return null;

            if (tf1Pow < 0 || tf0Pow < 0)
                return null;

            ExComp simplified = null;

            if (tf1Pow == 0 && tf0Pow == 0)
                simplified = SingularPowTrig(tf0, tf1);
            else if ((tf0 is SinFunction && tf1 is CosFunction) ||
                (tf0 is CosFunction && tf1 is SinFunction))
            {
                int sp, cp;
                SinFunction sf;
                CosFunction cf;
                if (tf0 is SinFunction)
                {
                    sf = tf0 as SinFunction;
                    cf = tf1 as CosFunction;
                    sp = tf0Pow;
                    cp = tf1Pow;
                }
                else
                {
                    sf = tf1 as SinFunction;
                    cf = tf0 as CosFunction;
                    sp = tf1Pow;
                    cp = tf0Pow;
                }

                string dispStr = "\\int ( " + WorkMgr.ToDisp(ex0) + WorkMgr.ToDisp(ex1) + " ) d" + dVar.ToDispString();

                simplified = SinCosTrig(sf, cf, sp, cp, dispStr, dVar, ref pEvalData);
            }
            else if ((tf0 is SecFunction && tf1 is TanFunction) ||
                (tf0 is TanFunction && tf1 is SecFunction))
            {
                int sp, tp;
                SecFunction sf;
                TanFunction tf;
                if (tf0 is SecFunction)
                {
                    sf = tf0 as SecFunction;
                    tf = tf1 as TanFunction;
                    sp = tf0Pow;
                    tp = tf1Pow;
                }
                else
                {
                    sf = tf1 as SecFunction;
                    tf = tf0 as TanFunction;
                    sp = tf1Pow;
                    tp = tf0Pow;
                }

                // This gets the actual anti-derivative not just evaluable form.
                ExComp secTanTrigEval = SecTanTrig(sf, tf, sp, tp, dVar, ref pIntInfo, ref pEvalData);

                return secTanTrigEval;
            }

            if (simplified != null)
            {
                ExComp intEval = Integral.TakeAntiDeriv(simplified, dVar, ref pEvalData);
                return intEval;
            }

            return null;
        }

        private static ExComp SecTanTrig(SecFunction sf, TanFunction tf, int sp, int tp, AlgebraComp dVar,
            ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            if (!sf.GetInnerEx().IsEqualTo(tf.GetInnerEx()))
                return null;

            bool spEven = sp % 2 == 0;
            bool tpEven = tp % 2 == 0;
            if (!spEven && tp == 1)
            {
                //ExComp[] gp = new ExComp[] { PowOp.StaticCombine(sf, new Number(sp - 1)), new AlgebraTerm(sf, new MulOp(), tf) };
                //return AttemptUSub(gp, dVar, ref pIntInfo, ref pEvalData);

                AlgebraComp subInVar = null;

                if (sf.Contains(new AlgebraComp("u")) || tf.Contains(new AlgebraComp("u")))
                {
                    if (sf.Contains(new AlgebraComp("w")) || tf.Contains(new AlgebraComp("u")))
                        subInVar = new AlgebraComp("v");
                    else
                        subInVar = new AlgebraComp("w");
                }
                else
                    subInVar = new AlgebraComp("u");

                string innerStr = "(" + WorkMgr.ToDisp(sf.GetInnerTerm()) + ")";
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int \\sec^{" + (sp - 1).ToString() + "}" + innerStr +
                    "sec" + innerStr + "tan" + innerStr + " d" + dVar.ToDispString() + WorkMgr.EDM, "Split the term up.");

                ExComp subbedIn = PowOp.StaticCombine(subInVar, new ExNumber(sp - 1));
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int " + subInVar.ToDispString() + "^{" + (sp - 1).ToString() + "} d" + subInVar.ToDispString() + WorkMgr.EDM,
                    "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=\\sec" + innerStr + ", d" + subInVar.ToDispString() +
                    "=sec" + innerStr + "tan" + innerStr + "d" + subInVar.ToDispString());
                ExComp antiDeriv = TakeAntiDerivativeVarGp(new ExComp[] { subbedIn }, subInVar, ref pIntInfo, ref pEvalData);

                AlgebraTerm subbedBack = antiDeriv.ToAlgTerm().Substitute(subInVar, sf);
                pEvalData.GetWorkMgr().FromSides(subbedBack, null, "Sub back in.");

                return subbedBack;
            }
            //else if (!spEven && tpEven && sp > 2)
            //{
            //    ExComp secSubed = PowOp.StaticCombine(
            //        AddOp.StaticCombine(
            //            Number.One,
            //            PowOp.StaticCombine(new TanFunction(sf.InnerEx), new Number(2.0))),
            //        new Number((sp - 2) / 2));
            //    ExComp[] gp = new ExComp[] { PowOp.StaticCombine(tf, new Number(tp)), secSubed, PowOp.StaticCombine(sf, new Number(2.0)) };
            //    return AttemptUSub(gp, dVar, ref pIntInfo, ref pEvalData);
            //}

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sf">sin function</param>
        /// <param name="cf">cos function</param>
        /// <param name="sp">sin power</param>
        /// <param name="cp">cos power</param>
        /// <returns></returns>
        private static ExComp SinCosTrig(SinFunction sf, CosFunction cf, int sp, int cp, string dispStr, AlgebraComp dVar, ref EvalData pEvalData)
        {
            if (!sf.GetInnerEx().IsEqualTo(cf.GetInnerEx()))
                return null;
            bool spEven = sp % 2 == 0;
            bool cpEven = cp % 2 == 0;

            if (spEven && !cpEven)
            {
                // Use cos^2(x) = 1 - sin^2(x)
                ExComp subbedCos = PowOp.StaticCombine(
                    SubOp.StaticCombine(
                    ExNumber.GetOne(),
                    PowOp.StaticCombine(new SinFunction(cf.GetInnerEx()), new ExNumber(2.0))),
                    new ExNumber((cp - 1) / 2));

                subbedCos = MulOp.StaticCombine(cf, MulOp.StaticCombine(PowOp.StaticCombine(sf, new ExNumber(sp)), subbedCos));

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + dispStr + " =  \\int ( " + WorkMgr.ToDisp(subbedCos) + " ) d" + dVar.ToDispString() + WorkMgr.EDM,
                    "Use the identity " + WorkMgr.STM + "cos^{2}(x)=1-sin^{2}(x)" + WorkMgr.EDM);

                return subbedCos;
            }
            else if (!spEven && cpEven)
            {
                // Using sin^2(x) = 1 - cos^2(x)
                ExComp subbedCos = PowOp.StaticCombine(
                    SubOp.StaticCombine(
                    ExNumber.GetOne(),
                    PowOp.StaticCombine(new CosFunction(sf.GetInnerEx()), new ExNumber(2.0))),
                    new ExNumber((sp - 1) / 2));

                subbedCos = MulOp.StaticCombine(sf, MulOp.StaticCombine(PowOp.StaticCombine(cf, new ExNumber(cp)), subbedCos));

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + dispStr + " = \\int ( " + WorkMgr.ToDisp(subbedCos) + " ) d" + dVar.ToDispString() + WorkMgr.EDM,
                    "Use the identity " + WorkMgr.STM + "sin^{2}(x)=1-cos^{2}(x)" + WorkMgr.EDM);

                return subbedCos;
            }
            else if (spEven && cpEven)
            {
                // Using sin^2(x) = (1/2)(1-cos(2x))
                // Using cos^2(x) = (1/2)(1+cos(2x))
                ExComp sinSub = MulOp.StaticCombine(
                    AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)),
                    SubOp.StaticCombine(ExNumber.GetOne(), new CosFunction(MulOp.StaticCombine(new ExNumber(2.0), sf.GetInnerEx()))));
                ExComp cosSub = MulOp.StaticCombine(
                    AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)),
                    AddOp.StaticCombine(ExNumber.GetOne(), new CosFunction(MulOp.StaticCombine(new ExNumber(2.0), sf.GetInnerEx()))));
                ExComp finalEx = MulOp.StaticCombine(
                    PowOp.StaticCombine(sinSub, new ExNumber(sp / 2)),
                    PowOp.StaticCombine(cosSub, new ExNumber(cp / 2)));

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + dispStr + "=" + WorkMgr.ToDisp(finalEx) + WorkMgr.EDM,
                    "Use the identities " + WorkMgr.STM + "sin^2(x)=\\frac{1}{2}(1-cos(2x))" + WorkMgr.EDM + " and " + WorkMgr.STM +
                    "cos^2(x)=\\frac{1}{2}(1+cos(2x))" + WorkMgr.EDM);

                return finalEx;
            }

            return null;
        }

        private static ExComp SingularPowTrig(TrigFunction tf0, TrigFunction tf1)
        {
            ExComp a = tf0.GetInnerEx();
            ExComp b = tf1.GetInnerEx();
            AlgebraTerm half = AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0));
            if (tf0 is SinFunction && tf1 is SinFunction)
                return MulOp.StaticCombine(half,
                    SubOp.StaticCombine(
                    new CosFunction(SubOp.StaticCombine(a, b)),
                    new CosFunction(AddOp.StaticCombine(a, b))));
            else if (tf0 is CosFunction && tf1 is CosFunction)
                return MulOp.StaticCombine(half,
                    AddOp.StaticCombine(
                    new CosFunction(SubOp.StaticCombine(a, b)),
                    new CosFunction(AddOp.StaticCombine(a, b))));

            if (tf0 is CosFunction && tf1 is SinFunction)
            {
                ExComp tmp = a;
                a = b;
                b = tmp;
            }

            if (tf0 is SinFunction && tf1 is CosFunction)
                return MulOp.StaticCombine(half,
                    AddOp.StaticCombine(
                    new SinFunction(SubOp.StaticCombine(a, b)),
                    new SinFunction(AddOp.StaticCombine(a, b))));

            return null;
        }

        private static ExComp AttemptPartialFractions(ExComp num, ExComp den, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            if (!(den is AlgebraTerm))
                return null;

            AlgebraTerm numTerm = num.ToAlgTerm();
            AlgebraTerm denTerm = den.ToAlgTerm();

            PolynomialExt numPoly = new PolynomialExt();
            PolynomialExt denPoly = new PolynomialExt();

            if (!numPoly.Init(numTerm) || !denPoly.Init(denTerm))
                return null;

            if (denPoly.GetMaxPow() < 2)
                return null;

            if (numPoly.GetMaxPow() > denPoly.GetMaxPow())
            {
                // First do a synthetic division.
                ExComp synthDivResult = DivOp.AttemptPolyDiv(numPoly.Clone(), denPoly.Clone(), ref pEvalData);
                if (synthDivResult == null)
                    return null;

                ExComp intEval = Integral.TakeAntiDeriv(synthDivResult, dVar, ref pEvalData);
                return intEval;
            }

            ExComp atmpt = PartialFracs.Split(numTerm, denTerm, numPoly, dVar, ref pEvalData);
            if (atmpt == null)
                return null;

            ExComp antiDerivEval = Integral.TakeAntiDeriv(atmpt, dVar, ref pEvalData);
            return antiDerivEval;
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
                    ExComp baseEx = ((PowerFunction)gpCmp).GetBase();
                    if (!(baseEx is AlgebraComp) && baseEx.ToAlgTerm().Contains(dVar))
                    {
                        potentialU.Add(baseEx);
                        List<ExComp[]> groups = baseEx.ToAlgTerm().GetGroups();
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

                    ExComp powerEx = ((PowerFunction)gpCmp).GetPower();
                    if (!(powerEx is AlgebraComp) && powerEx.ToAlgTerm().Contains(dVar))
                    {
                        potentialU.Add(powerEx);
                        List<ExComp[]> groups = powerEx.ToAlgTerm().GetGroups();
                        if (groups.Count == 1)
                        {
                            List<ExComp> additionalPower = GetPotentialU(powerEx.ToAlgTerm().GetSubComps().ToArray(), dVar);
                            potentialU.AddRange(additionalPower);
                        }
                    }
                }
                else if (gpCmp is BasicAppliedFunc)
                {
                    innerEx = ((BasicAppliedFunc)gpCmp).GetInnerEx();
                    potentialU.Add(gpCmp);
                }

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
                int prevWorkStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                ExComp attempt = TryU(group, potentialU, dVar, ref pIntInfo, ref pEvalData);
                if (attempt != null)
                    return attempt;
                else
                    pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - prevWorkStepCount);
            }

            return null;
        }

        private static ExComp AttemptUSub(ExComp[] group, AlgebraComp dVar, ExComp forcedU, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            int prevWorkStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            ExComp attempt = TryU(group, forcedU, dVar, ref pIntInfo, ref pEvalData);
            if (attempt != null)
                return attempt;
            else
                pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - prevWorkStepCount);

            return null;
        }

        private static ExComp TryU(ExComp[] group, ExComp uatmpt, AlgebraComp dVar, ref IntegrationInfo pIntInfo, ref EvalData pEvalData)
        {
            string groupStr = GroupHelper.ToAlgTerm(GroupHelper.CloneGroup(group)).FinalToDispStr();
            string thisStr = "\\int(" + groupStr + ")" + dVar.ToDispString();
            string atmptStr = uatmpt.ToAlgTerm().FinalToDispStr();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int (" + groupStr + ") \\d" + dVar.ToDispString() + WorkMgr.EDM,
                "Use u-substitution.");

            AlgebraTerm term = GroupHelper.ToAlgNoRedunTerm(group);
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
            List<ExComp[]> updatedGroups = term.GetGroupsNoOps();
            // The group count started as one and should not have been altered by substitutions.
            if (updatedGroups.Count != 1)
                return null;

            Derivative derivative = Derivative.ConstructDeriv(uatmpt, dVar, null);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + thisStr + WorkMgr.EDM,
                "Substitute " + WorkMgr.STM + subInVar.ToDispString() + "=" + uatmpt.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("",
                "Find " + WorkMgr.STM + "d" + subInVar.ToDispString() + WorkMgr.EDM);
            WorkStep last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp evaluated = derivative.Evaluate(false, ref pEvalData);
            last.GoUp(ref pEvalData);

            if (evaluated is Derivative)
                return null;

            last.SetWorkHtml(WorkMgr.STM + "\\frac{d}{d" + dVar.ToDispString() + "}[" + atmptStr + "]=" + WorkMgr.ToDisp(evaluated) + WorkMgr.EDM);

            group = updatedGroups[0];

            List<ExComp[]> groups = evaluated.ToAlgTerm().GetGroupsNoOps();
            ExComp constEx = null;

            if (groups.Count == 1)
            {
                ExComp[] singularGp = groups[0];
                ExComp[] varTo, constTo;
                GroupHelper.GetConstVarTo(singularGp, out varTo, out constTo, dVar);

                constEx = constTo.Length == 0 ? ExNumber.GetOne() : (ExComp)AlgebraTerm.FromFraction(ExNumber.GetOne(), GroupHelper.ToAlgTerm(constTo));

                if (varTo.Length == 0)
                {
                    if (GroupHelper.GroupContains(group, dVar))
                        return null;

                    pEvalData.GetWorkMgr().GetWorkSteps().Add(new WorkStep(WorkMgr.STM + thisStr +
                                                                           WorkMgr.EDM, "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                                                                        atmptStr + WorkMgr.EDM + " and " + WorkMgr.STM + "d" + subInVar.ToDispString() + "=" +
                                                                                        evaluated.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM, true));

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + constEx.ToAlgTerm().FinalToDispStr() + "\\int (" + GroupHelper.ToAlgTerm(group.ToArray()).FinalToDispStr() + ") d" + subInVar.ToDispString() + WorkMgr.EDM);

                    ExComp innerAntiDeriv = TakeAntiDerivativeGp(group, subInVar, ref pIntInfo, ref pEvalData, "", "");
                    if (innerAntiDeriv == null)
                        return null;

                    pEvalData.GetWorkMgr().FromSides(MulOp.StaticWeakCombine(constEx, innerAntiDeriv), null, "Substitute back in " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                        atmptStr + WorkMgr.EDM);

                    // Sub back in the appropriate values.
                    innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                    ExComp retEx = MulOp.StaticCombine(innerAntiDeriv, constEx);
                    pEvalData.GetWorkMgr().FromSides(retEx, null);
                    return retEx;
                }

                evaluated = GroupHelper.ToAlgTerm(varTo).RemoveRedundancies(false);
            }
            else
            {
                ExComp[] groupGcf = evaluated.ToAlgTerm().GetGroupGCF();
                ExComp[] varTo, constTo;
                GroupHelper.GetConstVarTo(groupGcf, out varTo, out constTo, dVar);

                AlgebraTerm constToAg = GroupHelper.ToAlgTerm(constTo);
                evaluated = DivOp.StaticCombine(evaluated, constToAg.CloneEx());
                constEx = AlgebraTerm.FromFraction(ExNumber.GetOne(), constToAg);
            }

            for (int j = 0; j < group.Length; ++j)
            {
                if (group[j].IsEqualTo(evaluated))
                {
                    List<ExComp> groupList = ArrayFunc.ToList(group);
                    ArrayFunc.RemoveIndex(groupList, j);

                    pEvalData.GetWorkMgr().GetWorkSteps().Add(new WorkStep(WorkMgr.STM + thisStr +
                                                                           WorkMgr.EDM, "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                                                                        atmptStr + WorkMgr.EDM + " and " + WorkMgr.STM + "d" + subInVar.ToDispString() + "=" +
                                                                                        evaluated.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM, false));

                    bool mulInCost = constEx != null && !ExNumber.GetOne().IsEqualTo(constEx);
                    string mulInCostStr = (mulInCost ? constEx.ToAlgTerm().FinalToDispStr() : "");

                    group = groupList.ToArray();

                    if (GroupHelper.GroupContains(group, dVar))
                        return null;

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + mulInCostStr +
                        "\\int (" + GroupHelper.ToAlgTerm(group).FinalToDispStr() + ") d" + subInVar.ToDispString() + WorkMgr.EDM);

                    ExComp innerAntiDeriv = TakeAntiDerivativeGp(group, subInVar, ref pIntInfo, ref pEvalData, "", "");
                    if (innerAntiDeriv == null)
                        return null;

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + mulInCostStr + "(" + innerAntiDeriv.ToAlgTerm().FinalToDispStr() + ")" + WorkMgr.EDM, "Substitute back in " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                    uatmpt.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

                    // Sub back in the appropriate values.
                    innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                    ExComp retEx;

                    if (mulInCost)
                        retEx = MulOp.StaticCombine(constEx, innerAntiDeriv);
                    else
                        retEx = innerAntiDeriv;

                    pEvalData.GetWorkMgr().FromSides(retEx, null);
                    return retEx;
                }
                else if (group[j] is PowerFunction && evaluated is PowerFunction && (group[j] as PowerFunction).GetPower().IsEqualTo((evaluated as PowerFunction).GetPower()))
                {
                    PowerFunction groupPf = group[j] as PowerFunction;
                    PowerFunction evaluatedPf = evaluated as PowerFunction;

                    List<ExComp[]> baseGps = groupPf.GetBase().ToAlgTerm().GetGroupsNoOps();
                    if (baseGps.Count == 1)
                    {
                        // Search the base for like terms.
                        for (int k = 0; k < baseGps[0].Length; ++k)
                        {
                            if (baseGps[0][k].IsEqualTo(evaluatedPf.GetBase()))
                            {
                                List<ExComp> baseGpsList = ArrayFunc.ToList(baseGps[0]);
                                ArrayFunc.RemoveIndex(baseGpsList, k);

                                group[j] = new PowerFunction(GroupHelper.ToAlgTerm(baseGpsList.ToArray()), evaluatedPf.GetPower());

                                pEvalData.GetWorkMgr().GetWorkSteps().Add(new WorkStep(WorkMgr.STM + thisStr +
                                                                                       WorkMgr.EDM, "Make the substitution " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                                                                                    atmptStr + WorkMgr.EDM + " and " + WorkMgr.STM + "d" + subInVar.ToDispString() + "=" +
                                                                                                    evaluated.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM, false));

                                bool mulInCost = constEx != null && !ExNumber.GetOne().IsEqualTo(constEx);
                                string mulInCostStr = (mulInCost ? constEx.ToAlgTerm().FinalToDispStr() : "");

                                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + mulInCostStr +
                                    "\\int (" + GroupHelper.ToAlgTerm(group).FinalToDispStr() + ") d" + subInVar.ToDispString() + WorkMgr.EDM);

                                ExComp innerAntiDeriv = TakeAntiDerivativeGp(@group, subInVar, ref pIntInfo, ref pEvalData, "", "");
                                if (innerAntiDeriv == null)
                                    return null;

                                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + mulInCostStr + "(" + innerAntiDeriv.ToAlgTerm().FinalToDispStr() + ")" + WorkMgr.EDM, "Substitute back in " + WorkMgr.STM + subInVar.ToDispString() + "=" +
                                    uatmpt.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

                                // Sub back in the appropriate values.
                                innerAntiDeriv = innerAntiDeriv.ToAlgTerm().Substitute(subInVar, uatmpt);

                                ExComp retEx;

                                if (mulInCost)
                                    retEx = MulOp.StaticCombine(constEx, innerAntiDeriv);
                                else
                                    retEx = innerAntiDeriv;

                                pEvalData.GetWorkMgr().FromSides(retEx, null);
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
            ExComp intByParts = IntByParts(ex0, ExNumber.GetOne(), dVar, thisSTr, ref pIntInfo, ref pEvalData);
            return intByParts;
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
            if (ex0 is ExNumber)
            {
                u = ex1;
                dv = ex0;
            }
            else if (ex1 is ExNumber)
            {
                u = ex0;
                dv = ex1;
            }
            else if ((ex0 is PowerFunction && (ex0 as PowerFunction).GetPower() is ExNumber &&
                !((ex0 as PowerFunction).GetBase() is PowerFunction && !(((ex0 as PowerFunction).GetBase() as PowerFunction).GetPower() is ExNumber))) ||
                ex0 is AlgebraComp)
            {
                u = ex0;
                dv = ex1;
            }
            else if ((ex1 is PowerFunction && (ex1 as PowerFunction).GetPower() is ExNumber &&
                !((ex1 as PowerFunction).GetBase() is PowerFunction && !(((ex1 as PowerFunction).GetBase() as PowerFunction).GetPower() is ExNumber))) ||
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

            int stepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            Integral antiderivativeOfDV = Integral.ConstructIntegral(dv.CloneEx(), dVar);
            antiderivativeOfDV.SetInfo(pIntInfo);
            antiderivativeOfDV.SetAddConstant(false);
            ExComp v = antiderivativeOfDV.Evaluate(false, ref pEvalData);

            if (v is Integral)
            {
                pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - stepCount);
                // Try to switch the variables.
                ExComp tmp = u;
                u = dv;
                dv = tmp;

                antiderivativeOfDV = Integral.ConstructIntegral(dv, dVar);
                antiderivativeOfDV.SetInfo(pIntInfo);
                antiderivativeOfDV.SetAddConstant(false);
                v = antiderivativeOfDV.Evaluate(false, ref pEvalData);
                if (v is Integral)
                    return null;
            }

            List<WorkStep> stepRange = pEvalData.GetWorkMgr().GetWorkSteps().GetRange(stepCount, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - stepCount);
            pEvalData.GetWorkMgr().GetWorkSteps().RemoveRange(stepCount, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - stepCount);

            pEvalData.GetWorkMgr().FromFormatted("",
                "Integrate by parts using the formula " + WorkMgr.STM + "\\int u v' = uv - \\int v u' " + WorkMgr.EDM + " where " +
                WorkMgr.STM + "u=" + u.ToAlgTerm().FinalToDispStr() + ", dv = " + dv.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM);

            Derivative derivativeOfU = Derivative.ConstructDeriv(u, dVar, null);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "du=" + derivativeOfU.FinalToDispStr() + WorkMgr.EDM, "Find " + WorkMgr.STM +
                "du" + WorkMgr.EDM);
            WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp du = derivativeOfU.Evaluate(false, ref pEvalData);
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "\\int(" + thisStr + ")d" + dVar.ToDispString() + "=" + WorkMgr.ToDisp(du) + WorkMgr.EDM);

            if (du is Derivative)
                return null;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "v=" + antiderivativeOfDV.FinalToDispStr() + "=" + WorkMgr.ToDisp(v) + WorkMgr.EDM,
            "Find " + WorkMgr.STM +
                "v" + WorkMgr.EDM);
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            pEvalData.GetWorkMgr().GetWorkSteps().AddRange(stepRange);
            lastStep.GoUp(ref pEvalData);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})({1})-\\int ({1}) ({2}) d" + dVar.ToDispString() + WorkMgr.EDM,
                "Substitute the values into the integration by parts formula.", u, v, du);

            ExComp uv = MulOp.StaticCombine(u, v.CloneEx());
            ExComp vDu = MulOp.StaticCombine(v, du);

            Integral antiDerivVDU = Integral.ConstructIntegral(vDu, dVar);
            antiDerivVDU.SetInfo(pIntInfo);
            antiDerivVDU.SetAddConstant(false);
            ExComp antiDerivVDUEval = antiDerivVDU.Evaluate(false, ref pEvalData);
            if (antiDerivVDUEval is Integral)
                return null;

            ExComp finalEx = SubOp.StaticCombine(uv, antiDerivVDUEval);
            pEvalData.GetWorkMgr().FromSides(finalEx, null);

            return finalEx;
        }

        private static bool IsDerivAcceptable(ExComp ex, AlgebraComp dVar)
        {
            if (ex is PowerFunction)
                return (ex as PowerFunction).GetBase().IsEqualTo(dVar);
            else if (ex is AppliedFunction)
                return (ex as AppliedFunction).GetInnerEx().IsEqualTo(dVar);

            return false;
        }

        private static ExComp GetIsSingleFunc(ExComp single, AlgebraComp dVar, ref EvalData pEvalData)
        {
            if (single is PowerFunction)
            {
                // Add one to the power and then divide by the power.
                PowerFunction pf = single as PowerFunction;

                if (pf.GetPower().IsEqualTo(ExNumber.GetNegOne()))
                {
                    ExComp pfBase = pf.GetBase();

                    if (pfBase is PowerFunction)
                    {
                        PowerFunction pfBasePf = pfBase as PowerFunction;
                        if (pfBasePf.GetPower().IsEqualTo(new ExNumber(0.5)) || pfBasePf.GetPower().IsEqualTo(AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0))))
                        {
                            // Is this arcsin or arccos?
                            ExComp compare = AddOp.StaticCombine(MulOp.Negate(PowOp.StaticCombine(dVar, new ExNumber(2.0))), ExNumber.GetOne()).ToAlgTerm().RemoveRedundancies(false);

                            ExComp useBase;
                            if (pfBasePf.GetBase() is AlgebraTerm)
                                useBase = (pfBasePf.GetBase() as AlgebraTerm).RemoveRedundancies(false);
                            else
                                useBase = pfBasePf.GetBase();

                            if (useBase.IsEqualTo(compare))
                            {
                                ASinFunction asin = new ASinFunction(dVar);
                                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(\\frac{1}{sqrt{1-" + dVar.ToDispString() + "^2}})\\d" + dVar.ToDispString() +
                                    "=" + asin.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                                return asin;
                            }
                        }

                        // See if it is just the regular (1/x^(1/2)) or something like that.
                        if (IsDerivAcceptable(pfBasePf, dVar) && !pfBasePf.GetPower().ToAlgTerm().Contains(dVar))
                        {
                            ExComp power = MulOp.Negate(pfBasePf.GetPower());
                            ExComp powChange = AddOp.StaticCombine(power, ExNumber.GetOne());
                            if (powChange is AlgebraTerm)
                                powChange = (powChange as AlgebraTerm).CompoundFractions();

                            pfBasePf.SetPower(powChange);
                            return DivOp.StaticCombine(pfBasePf, powChange);
                        }
                    }

                    if (pfBase.IsEqualTo(AddOp.StaticCombine(ExNumber.GetOne(), PowOp.StaticCombine(dVar, new ExNumber(2.0)))))
                    {
                        ATanFunction atan = new ATanFunction(dVar);
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(\\frac{1}{" + dVar.ToDispString() + "^2+1})\\d" + dVar.ToDispString() +
                            "=" + atan.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                        return atan;
                    }
                }

                if (pf.GetBase().IsEqualTo(Constant.GetE()) && pf.GetPower().IsEqualTo(dVar))
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() +
                        "=" + pf.FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative.");
                    return pf;
                }
                else if (!pf.GetBase().ToAlgTerm().Contains(dVar) && pf.GetPower().IsEqualTo(dVar))
                {
                    ExComp finalEx = DivOp.StaticWeakCombine(pf, LogFunction.Ln(pf.GetBase()));
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() +
                        "=" + finalEx.ToAlgTerm().FinalToDispStr() + WorkMgr.EDM, "Use the common antiderivative law.");

                    return finalEx;
                }

                if (pf.GetBase() is TrigFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger())
                {
                    int pow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    if (pow < 0)
                    {
                        // Convert to the reciprocal.
                        pf = new PowerFunction((pf.GetBase() as TrigFunction).GetReciprocalOf(), new ExNumber(-pow));
                    }
                }

                if (IsDerivAcceptable(pf, dVar) && !pf.GetPower().ToAlgTerm().Contains(dVar))
                {
                    // The special case for the power function anti-dervivative.
                    if (ExNumber.GetNegOne().IsEqualTo(pf.GetPower()))
                    {
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() +
                            "=ln(|" + dVar + "|)" + WorkMgr.EDM, "This comes from the known derivative " + WorkMgr.STM +
                            "\\frac{d}{dx}ln(x)=\\frac{1}{x}" + WorkMgr.EDM);

                        // The absolute value function was removed here.
                        return LogFunction.Ln(dVar);
                    }

                    ExComp powChange = AddOp.StaticCombine(pf.GetPower(), ExNumber.GetOne());
                    if (powChange is AlgebraTerm)
                        powChange = (powChange as AlgebraTerm).CompoundFractions();

                    string changedPowStr = WorkMgr.ToDisp(AddOp.StaticWeakCombine(pf.GetPower(), ExNumber.GetOne()));
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + WorkMgr.ToDisp(single) + ")\\d" + dVar.ToDispString() +
                        "=\\frac{" + dVar.ToDispString() + "^{" + changedPowStr + "}}{" + changedPowStr + "}" + WorkMgr.EDM,
                        "Use the power rule of antiderivatives.");

                    pf.SetPower(powChange);
                    return DivOp.StaticCombine(pf, powChange);
                }
                else if ((new ExNumber(2.0)).IsEqualTo(pf.GetPower()) && pf.GetBase() is TrigFunction && (pf.GetBase() as TrigFunction).GetInnerEx().IsEqualTo(dVar) &&
                    (pf.GetBase() is SecFunction || pf.GetBase() is CscFunction))
                {
                    ExComp ad = null;
                    if (pf.GetBase() is SecFunction)
                        ad = new TanFunction(dVar);
                    else if (pf.GetBase() is CscFunction)
                        ad = MulOp.Negate(new CotFunction(dVar));

                    if (ad != null)
                    {
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + pf.FinalToDispStr() + ")\\d" + dVar.ToDispString() + "=" +
                            WorkMgr.ToDisp(ad) + WorkMgr.EDM, "Use the common antiderivative.");

                        return ad;
                    }
                }
                else if (pf.GetBase() is SinFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 == 0)
                {
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    SinFunction sf = pf.GetBase() as SinFunction;
                    ExComp subbed = MulOp.StaticCombine(
                        AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)),
                        SubOp.StaticCombine(ExNumber.GetOne(), new CosFunction(MulOp.StaticCombine(new ExNumber(2.0), sf.GetInnerEx()))));

                    subbed = PowOp.StaticCombine(subbed, new ExNumber(iPow / 2));

                    pEvalData.GetWorkMgr().FromSides(pf, subbed,
                        "Use the trig identity " + WorkMgr.STM + "sin^{2}(x) = \\frac{1}{2}(1-cos(2x))" + WorkMgr.EDM);

                    return Integral.TakeAntiDeriv(subbed, dVar, ref pEvalData);
                }
                else if (pf.GetBase() is SinFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 != 0)
                {
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    SinFunction sf = pf.GetBase() as SinFunction;
                    ExComp finalEx = MulOp.StaticCombine(sf,
                        PowOp.StaticCombine(
                        SubOp.StaticCombine(
                        ExNumber.GetOne(),
                        PowOp.StaticCombine(new CosFunction(sf.GetInnerEx()), new ExNumber(2.0))),
                        new ExNumber((iPow - 1) / 2)));

                    pEvalData.GetWorkMgr().FromSides(pf, finalEx,
                        "Use the trig identity " + WorkMgr.STM + "sin^{2}(x) = \\frac{1}{2}(1-cos(2x))" + WorkMgr.EDM);

                    ExComp finalEval = Integral.TakeAntiDeriv(finalEx, dVar, ref pEvalData);
                    return finalEval;
                }
                else if (pf.GetBase() is CosFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 == 0)
                {
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    CosFunction cf = pf.GetBase() as CosFunction;
                    ExComp subbed = MulOp.StaticCombine(
                        AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)),
                        AddOp.StaticCombine(ExNumber.GetOne(), new CosFunction(MulOp.StaticCombine(new ExNumber(2.0), cf.GetInnerEx()))));

                    subbed = PowOp.StaticCombine(subbed, new ExNumber(iPow / 2));

                    pEvalData.GetWorkMgr().FromSides(pf, subbed,
                        "Use the trig identity " + WorkMgr.STM + "cos^{2}(x) = \\frac{1}{2}(1+cos(2x))" + WorkMgr.EDM);

                    ExComp finalEval = Integral.TakeAntiDeriv(subbed, dVar, ref pEvalData);
                    return finalEval;
                }
                else if (pf.GetBase() is CosFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 != 0)
                {
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    CosFunction cf = pf.GetBase() as CosFunction;
                    ExComp finalEx = MulOp.StaticCombine(cf,
                        PowOp.StaticCombine(
                        SubOp.StaticCombine(
                        ExNumber.GetOne(),
                        PowOp.StaticCombine(new SinFunction(cf.GetInnerEx()), new ExNumber(2.0))),
                        new ExNumber((iPow - 1) / 2)));

                    pEvalData.GetWorkMgr().FromSides(pf, finalEx,
                        "Use the trig identity " + WorkMgr.STM + "cos^{2}(x) = \\frac{1}{2}(1+cos(2x))" + WorkMgr.EDM);

                    ExComp intEval = Integral.TakeAntiDeriv(finalEx, dVar, ref pEvalData);
                    return intEval;
                }
                else if (pf.GetBase() is CscFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 == 0)
                {
                    // In the form csc^n(x) where n is even.
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    CscFunction cf = pf.GetBase() as CscFunction;
                    ExComp finalEx = MulOp.StaticCombine(new PowerFunction(cf, new ExNumber(2.0)),
                        PowOp.StaticCombine(
                            AddOp.StaticCombine(
                                ExNumber.GetOne(),
                                new PowerFunction(new CotFunction(cf.GetInnerTerm()), new ExNumber(2.0))),
                            new ExNumber((iPow / 2) - 1)));

                    pEvalData.GetWorkMgr().FromSides(pf, finalEx,
                        "Use the trig identity " + WorkMgr.STM + "csc^2(x) = 1 + cot^2(x)" + WorkMgr.EDM);

                    ExComp intEval = Integral.TakeAntiDeriv(finalEx, dVar, ref pEvalData);
                    return intEval;
                }
                else if (pf.GetBase() is TanFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 == 0)
                {
                    // In the form tan^n where n is even.
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    if (iPow > 2)
                        return null;
                    TanFunction tf = pf.GetBase() as TanFunction;
                    ExComp finalEx = SubOp.StaticCombine(PowOp.StaticCombine(new SecFunction(tf.GetInnerTerm()), new ExNumber(2.0)), ExNumber.GetOne());

                    pEvalData.GetWorkMgr().FromSides(pf, finalEx,
                        "Use the trig identity " + WorkMgr.STM + "tan^2(x) = sec^2(x)-1" + WorkMgr.EDM);

                    ExComp intEval = Integral.TakeAntiDeriv(finalEx, dVar, ref pEvalData);
                    return intEval;
                }
                else if (pf.GetBase() is TanFunction && pf.GetPower() is ExNumber && (pf.GetPower() as ExNumber).IsRealInteger() &&
                    (int)((pf.GetPower() as ExNumber).GetRealComp()) > 1 && (int)((pf.GetPower() as ExNumber).GetRealComp()) % 2 != 0)
                {
                    // In the form tan^k where k is odd.
                    int iPow = (int)(pf.GetPower() as ExNumber).GetRealComp();
                    TanFunction tf = pf.GetBase() as TanFunction;

                    ExComp finalEx = PowOp.StaticCombine(
                        SubOp.StaticCombine(PowOp.StaticCombine(new SecFunction(tf.GetInnerTerm()), new ExNumber(2.0)), ExNumber.GetOne()),
                        new ExNumber((iPow - 1) / 2));

                    pEvalData.GetWorkMgr().FromSides(pf, finalEx,
                        "Use the trig identity " + WorkMgr.STM + "tan^2(x) = sec^2(x)-1" + WorkMgr.EDM);

                    ExComp intEval = Integral.TakeAntiDeriv(finalEx, dVar, ref pEvalData);
                    return intEval;
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
                    ad = LogFunction.Ln(new SecFunction(dVar));
                }
                else if (single is CscFunction)
                {
                    ad = LogFunction.Ln(SubOp.StaticCombine(new CscFunction(dVar), new CotFunction(dVar)));
                }
                else if (single is SecFunction)
                {
                    ad = LogFunction.Ln(SubOp.StaticCombine(new SecFunction(dVar), new TanFunction(dVar)));
                }
                else if (single is CotFunction)
                {
                    ad = LogFunction.Ln(new SinFunction(dVar));
                }

                if (ad == null)
                    return null;

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int(" + (single as TrigFunction).FinalToDispStr() + ")\\d" + dVar.ToDispString() + "=" + WorkMgr.ToDisp(ad) +
                    WorkMgr.EDM,
                    "Use the common antiderivative.");

                return ad;
            }

            return null;
        }
    }
}