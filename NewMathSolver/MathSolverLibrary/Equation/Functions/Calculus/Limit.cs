using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class Limit : AppliedFunction
    {
        private const int MAX_LE_HOPITAL_COUNT = 3;

        private bool _evalFail = false;
        private string _limStr;
        private ExComp _reducedInner = null;
        private string _thisDispStr;
        private ExComp _valTo;
        private int _leHopitalCount = 0;
        private AlgebraComp _varFor;

        public Limit(ExComp inner)
            : base(inner, FunctionType.Limit, typeof(Limit))
        {
        }

        public static Limit Create(ExComp innerEx, AlgebraComp varFor, ExComp valTo)
        {
            Limit lim = new Limit(innerEx);
            lim._valTo = valTo;
            lim._varFor = varFor;

            return lim;
        }

        public override ExComp CloneEx()
        {
            Limit lim = new Limit(GetInnerTerm());
            lim._reducedInner = this._reducedInner == null ? null : this._reducedInner.CloneEx();
            lim._valTo = this._valTo == null ? null : this._valTo.CloneEx();
            lim._varFor = this._varFor == null ? null : (AlgebraComp)this._varFor.CloneEx();
            lim._evalFail = this._evalFail;

            return lim;
        }

        public static ExComp TakeLim(ExComp innerEx, AlgebraComp varFor, ExComp valueTo, ref EvalData pEvalData, int leHopitalCount)
        {
            Limit lim = new Limit(innerEx);
            lim._valTo = valueTo;
            lim._varFor = varFor;
            lim._leHopitalCount = leHopitalCount;

            ExComp retVal = lim.Evaluate(false, ref pEvalData);
            return retVal;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (_evalFail)
                return this;

            _thisDispStr = pEvalData.GetWorkMgr().GetAllowWork() ? this.FinalToDispStr() : "";
            _limStr = "\\lim_(" + _varFor.ToAsciiString() + "\\to" + _valTo.ToAsciiString() + ")";

            int stepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

            // Is the point defined?
            if (_reducedInner == null)
                _reducedInner = TermType.SimplifyGenTermType.BasicSimplify(GetInnerTerm(), ref pEvalData, false);

            AlgebraTerm reduced = _reducedInner.ToAlgTerm();
            if (!reduced.Contains(_varFor))
                return reduced;

            bool infEval = false;
            if (ExNumber.GetNegInfinity().IsEqualTo(_valTo) || ExNumber.GetPosInfinity().IsEqualTo(_valTo))
            {
                PolynomialExt poly = new PolynomialExt();
                ExComp harshSimp = Simplifier.HarshSimplify(reduced.CloneEx().ToAlgTerm(), ref pEvalData, true);
                if (poly.Init(reduced) || poly.Init(harshSimp.ToAlgTerm()))
                {
                    ExComp evalPoly = EvaluatePoly(poly, ref pEvalData);
                    return evalPoly;
                }

                infEval = true;
            }

            // Split the limit and evaluate each part independently.
            List<ExComp[]> reducedGps = reduced.GetGroupsNoOps();

            if (reducedGps.Count > 1)
            {
                string limStr = "";
                for (int i = 0; i < reducedGps.Count; ++i)
                {
                    limStr += "lim_{" + _varFor.ToDispString() + " \\to " + _valTo.ToDispString() + "}(" + GroupHelper.ToAlgTerm(reducedGps[i]).FinalToDispStr() + ")";
                    if (i + 1 < reducedGps.Count)
                        limStr += "+";
                }

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + limStr + WorkMgr.EDM, "Split the limit up and evaluate each part independently.");
            }

            ExComp overall = null;
            for (int i = 0; i < reducedGps.Count; ++i)
            {
                AlgebraTerm reducedGpTerm = GroupHelper.ToAlgTerm(reducedGps[i]);
                _reducedInner = reducedGpTerm;

                ExComp eval = infEval ? EvaluateLimGpInf(reducedGpTerm, ref pEvalData) : EvaluateLimGp(reducedGpTerm, ref pEvalData);

                if (eval == null)
                    eval = Limit.Create(reducedGpTerm, _varFor, _valTo);
                if (overall == null)
                    overall = eval;
                else
                    overall = AddOp.StaticCombine(overall, eval);
            }
            if (overall is AlgebraTerm)
                overall = (overall as AlgebraTerm).RemoveRedundancies(false);

            if (overall is Limit)
            {
                _evalFail = true;
                pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - stepCount);
            }

            return overall;
        }

        private ExComp EvaluateLimGpInf(AlgebraTerm eval, ref TermType.EvalData pEvalData)
        {
            if (!eval.Contains(_varFor))
                return eval;

            int stepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

            ExComp attempt = EvaluateInfinity(eval, ref pEvalData);
            if (attempt == null)
            {
                pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - stepCount);
            }

            if (attempt == null)
            {
                attempt = EvalInfinitySpecialFunc(eval, ref pEvalData);
            }

            if (attempt == null)
            {
                int preHopitalsRuleStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                attempt = AttemptLeHopitals(eval, ref pEvalData);

                if (attempt == null)
                {
                    _leHopitalCount = 0;
                    pEvalData.GetWorkMgr().PopStepsCount(ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - preHopitalsRuleStepCount);
                    return attempt;
                }
            }

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + _thisDispStr + "={0}" + WorkMgr.EDM, attempt);

            pEvalData.AttemptSetInputType(TermType.InputType.Limits);

            return attempt;
        }

        private ExComp EvaluateLimGp(AlgebraTerm eval, ref TermType.EvalData pEvalData)
        {
            if (!eval.Contains(_varFor))
                return eval;

            ExComp plugIn = PlugIn(eval, ref pEvalData);
            if (plugIn != null)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "={0}`", plugIn);

                pEvalData.AttemptSetInputType(TermType.InputType.Limits);
                return plugIn;
            }

            ExComp attempt = EvalSpecialFunc(eval);
            if (attempt != null)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "={0}`", attempt);

                pEvalData.AttemptSetInputType(TermType.InputType.Limits);
                return attempt;
            }

            if (CheckForLimitDivergence(eval, ref pEvalData))
            {
                pEvalData.AddMsg("Limit Diverges");
                return ExNumber.GetUndefined();
            }

            attempt = TryRadicalConjugate(eval, ref pEvalData);
            if (attempt != null)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "={0}`", attempt);

                pEvalData.AttemptSetInputType(TermType.InputType.Limits);
                return attempt;
            }

            attempt = AttemptLeHopitals(eval, ref pEvalData);

            if (attempt != null)
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + _thisDispStr + "={0}" + WorkMgr.EDM, attempt);

                pEvalData.AttemptSetInputType(TermType.InputType.LeHopital);
                return attempt;
            }

            return null;
        }

        private ExComp EvalInfinitySpecialFunc(ExComp ex, ref TermType.EvalData pEvalData)
        {
            bool posInfinity = ExNumber.GetPosInfinity().IsEqualTo(_valTo);

            if (ex is AlgebraTerm)
                ex = (ex as AlgebraTerm).RemoveRedundancies(false);

            if (ex is PowerFunction)
            {
                PowerFunction pf = ex as PowerFunction;
                ExComp baseVal = pf.GetBase() is Constant ? (pf.GetBase() as Constant).GetValue() : pf.GetBase();
                if (baseVal is ExNumber && !(baseVal as ExNumber).HasImaginaryComp())
                {
                    ExNumber baseNum = baseVal as ExNumber;
                    if (ExNumber.GetOne().IsEqualTo(baseNum))
                        return ExNumber.GetOne();
                    bool ltOne = ExNumber.OpLT(baseNum, 1.0);

                    // Get the coefficient of the power.
                    AlgebraTerm power = pf.GetPower().ToAlgTerm();
                    List<ExComp> powers = power.GetPowersOfVar(_varFor);
                    if (powers.Count != 1 || !powers[0].ToAlgTerm().RemoveRedundancies(false).IsEqualTo(ExNumber.GetOne()))
                        return null;

                    ExComp powCoeff = power.GetCoeffOfVar(_varFor);
                    if (powCoeff == null)
                        return null;

                    if (powCoeff is AlgebraTerm)
                        powCoeff = (powCoeff as AlgebraTerm).RemoveRedundancies(false);

                    if (!(powCoeff is ExNumber) || (powCoeff as ExNumber).HasImaginaryComp())
                        return null;

                    ExNumber nPowCoeff = powCoeff as ExNumber;

                    ExComp result;

                    if (ExNumber.OpLT(nPowCoeff, 0.0))
                        posInfinity = !posInfinity;

                    if (posInfinity)
                        result = ltOne ? ExNumber.GetZero() : ExNumber.GetPosInfinity();
                    else
                        result = ltOne ? ExNumber.GetPosInfinity() : ExNumber.GetZero();

                    return result;
                }
                else if (baseVal is AlgebraTerm || baseVal is AlgebraComp)
                {
                    ExComp flippedPow = DivOp.StaticCombine(ExNumber.GetOne(), pf.GetPower());
                    if (!(flippedPow is ExNumber))
                        return null;

                    ExNumber rootIndex = flippedPow as ExNumber;

                    if (!rootIndex.IsRealInteger())
                        return null;

                    int iRootIndex = (int)(rootIndex.GetRealComp());

                    AlgebraTerm baseTerm = baseVal.ToAlgTerm();
                    List<ExComp> varPows = baseTerm.GetPowersOfVar(_varFor);
                    if (varPows.Count != 1 || !(varPows[0] is ExNumber))
                        return null;

                    List<ExComp[]> gps = baseTerm.GetGroupsNoOps();
                    ExComp coeff = baseTerm.GetCoeffOfVar(_varFor);

                    if (coeff is AlgebraTerm)
                        coeff = (coeff as AlgebraTerm).RemoveRedundancies(false);

                    if (!(coeff is ExNumber) || (coeff as ExNumber).HasImaginaryComp())
                        return null;

                    ExNumber nCoeff = coeff as ExNumber;

                    if (ExNumber.OpLT(nCoeff, 0))
                        posInfinity = !posInfinity;

                    bool isEven = iRootIndex % 2 == 0;

                    if (posInfinity && isEven)
                        return ExNumber.GetPosInfinity();
                    else if (posInfinity && !isEven)
                        return ExNumber.GetPosInfinity();
                    else if (!posInfinity && isEven)
                        return ExNumber.GetUndefined();
                    else if (!posInfinity && !isEven)
                        return ExNumber.GetNegInfinity();
                }
            }
            else if (ex is LogFunction)
            {
                AlgebraTerm innerTerm = (ex as LogFunction).GetInnerTerm();
                List<ExComp> powers = innerTerm.GetPowersOfVar(_varFor);
                if (powers.Count != 1 || !ExNumber.GetOne().IsEqualTo(powers[0]))
                    return null;
                ExComp coeff = innerTerm.GetCoeffOfVar(_varFor);

                if (coeff is AlgebraTerm)
                    coeff = (coeff as AlgebraTerm).RemoveRedundancies(false);

                if (coeff == null || !(coeff is ExNumber) || (coeff as ExNumber).HasImaginaryComp())
                    return null;

                ExNumber nCoeff = coeff as ExNumber;
                bool isNeg = ExNumber.OpLT(nCoeff, 0.0);

                if (posInfinity && isNeg)
                    return ExNumber.GetUndefined();
                else if (posInfinity && !isNeg)
                    return ExNumber.GetPosInfinity();
                else if (!posInfinity && isNeg)
                    return ExNumber.GetPosInfinity();
                else
                    return ExNumber.GetUndefined();
            }
            else if (ex is TrigFunction)
            {
                return ExNumber.GetUndefined();
            }
            else if (ex is AlgebraTerm && !(ex is AlgebraFunction))
            {
                AlgebraTerm term = ex as AlgebraTerm;
                List<ExComp[]> gps = term.GetGroupsNoOps();
                if (gps.Count == 1)
                {
                    // Remove the constants
                    ExComp[] gp = gps[0];
                    ExComp[] varTo, constTo;
                    GroupHelper.GetConstVarTo(gp, out varTo, out constTo, _varFor);

                    if (varTo.Length != 1)
                        return null;

                    ExNumber coeff = null;
                    if (constTo.Length == 1 && constTo[0] is ExNumber && !(constTo[0] as ExNumber).HasImaginaryComp())
                        coeff = constTo[0] as ExNumber;
                    else if (constTo.Length == 0)
                        coeff = ExNumber.GetOne();
                    else if (constTo.Length == 1)
                    {
                        ExComp harshEvalAtmpt = Simplifier.HarshSimplify(constTo[0].ToAlgTerm(), ref pEvalData, false);
                        if (!(harshEvalAtmpt is ExNumber))
                            return null;
                        coeff = harshEvalAtmpt as ExNumber;
                    }
                    else
                        return null;

                    List<ExNumber> imaginaryNumbers = new List<ExNumber>();
                    foreach (ExComp exConst in constTo)
                    {
                        if (exConst is ExNumber && (exConst as ExNumber).GetRealComp() == 0.0 && (exConst as ExNumber).GetImagComp() != 0.0)
                        {
                            imaginaryNumbers.Add(exConst as ExNumber);
                            GroupHelper.RemoveEx(constTo, exConst);
                        }
                    }

                    // Put all of the imaginary numbers back under the radical.
                    if (varTo.Length == 1 && varTo[0] is PowerFunction && (varTo[0] as PowerFunction).IsRadical())
                    {
                        if (imaginaryNumbers.Count != 0)
                        {
                            PowerFunction innerPf = varTo[0] as PowerFunction;
                            ExComp flipped = DivOp.StaticCombine(ExNumber.GetOne(), innerPf.GetPower().CloneEx());

                            if (flipped is ExNumber)
                            {
                                ExComp[] raised = new ExComp[imaginaryNumbers.Count];
                                for (int i = 0; i < raised.Length; ++i)
                                {
                                    raised[i] = PowOp.StaticCombine(imaginaryNumbers[i], flipped);
                                }

                                foreach (ExComp raisedEx in raised)
                                {
                                    innerPf = new PowerFunction(MulOp.StaticCombine(innerPf.GetBase(), raisedEx), innerPf.GetPower());
                                }

                                varTo[0] = innerPf;

                                imaginaryNumbers.Clear();
                            }
                        }
                    }

                    if (imaginaryNumbers.Count != 0)
                        return null;

                    ExComp tmpEval = EvalInfinitySpecialFunc(varTo[0], ref pEvalData);
                    if (tmpEval == null)
                        return null;

                    if (ExNumber.OpLT(coeff, 0.0))
                        return MulOp.Negate(tmpEval);
                    return tmpEval;
                }
            }

            return null;
        }

        private ExComp EvalSpecialFunc(ExComp ex)
        {
            if (ex is TrigFunction)
            {
                return ExNumber.GetUndefined();
            }
            else if (ex is LogFunction)
            {
                LogFunction lf = ex as LogFunction;
                if (ExNumber.GetZero().IsEqualTo(_valTo))
                    return ExNumber.GetNegInfinity();
                if (ExNumber.GetPosInfinity().IsEqualTo(_valTo))
                    return ExNumber.GetPosInfinity();
            }
            else if (ex is AlgebraTerm)
            {
                AlgebraTerm[] numDen = (ex as AlgebraTerm).GetNumDenFrac();
                if (numDen == null)
                    return null;

                ExNumber numNum = numDen[0].RemoveRedundancies(false) as ExNumber;
                if (numNum == null || numNum.HasImaginaryComp())
                    return null;

                ExComp den = numDen[1].RemoveRedundancies(false);

                bool isNeg = ExNumber.OpLT(numNum, 0.0);
                if (den is AlgebraTerm && !(den is PowerFunction))
                {
                    AlgebraTerm denTerm = den as AlgebraTerm;
                    List<AlgebraGroup> varGps = denTerm.GetGroupsVariableToNoOps(_varFor);
                    if (varGps.Count != 1)
                        return null;

                    List<AlgebraGroup> constGps = denTerm.GetGroupsConstantTo(_varFor);
                    ExComp constEx = ExNumber.GetZero();
                    foreach (AlgebraGroup constGp in constGps)
                    {
                        constEx = AddOp.StaticCombine(constEx, constGp.ToTerm());
                    }

                    if (constEx is AlgebraTerm)
                        constEx = (ex as AlgebraTerm).RemoveRedundancies(false);

                    if (!constEx.IsEqualTo(MulOp.Negate(_valTo)))
                        return null;

                    if (varGps[0].GetGroupCount() > 1)
                    {
                        // There might be a coefficient.
                        if (varGps[0].GetGroupCount() != 2)
                            return null;
                        ExNumber coeff = varGps[0].GetItem(0) is ExNumber ? varGps[0].GetItem(0) as ExNumber : varGps[0].GetItem(1) as ExNumber;
                        den = varGps[0].GetItem(0) is ExNumber ? varGps[0].GetItem(1) : varGps[0].GetItem(0);

                        if (coeff.HasImaginaryComp())
                            return null;

                        if (ExNumber.OpLT(coeff, 0.0))
                            isNeg = !isNeg;
                    }
                    else
                        den = varGps[0].ToTerm().RemoveRedundancies(false);
                }
                else if (!ExNumber.GetZero().IsEqualTo(_valTo))
                    return null;

                if (den is AlgebraComp)
                    den = new PowerFunction(den, ExNumber.GetOne());

                if (!(den is PowerFunction))
                    return null;

                PowerFunction pf = den as PowerFunction;
                if (!pf.GetBase().IsEqualTo(_varFor) || !(pf.GetPower() is ExNumber))
                    return null;

                ExNumber powNum = pf.GetPower() as ExNumber;
                if (!powNum.IsRealInteger())
                    return null;

                int iPow = (int)powNum.GetRealComp();

                if (iPow % 2 != 0)
                    return ExNumber.GetUndefined();
                else
                    return isNeg ? ExNumber.GetNegInfinity() : ExNumber.GetPosInfinity();
            }

            return null;
        }

        private ExComp AttemptLeHopitals(ExComp ex, ref TermType.EvalData pEvalData)
        {
            if (_leHopitalCount >= MAX_LE_HOPITAL_COUNT)
                return null;

            _leHopitalCount++;

            if (!(ex is AlgebraTerm))
                return null;

            AlgebraTerm term = ex as AlgebraTerm;
            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen == null)
                return null;

            ExComp num = numDen[0];
            ExComp den = numDen[1];

            string numStr = WorkMgr.ToDisp(num);
            string denStr = WorkMgr.ToDisp(den);

            string limStr = "\\lim_{" + _varFor.ToDispString() + " \\to " + _valTo.ToAlgTerm().FinalToDispStr() + "}";

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\frac{" + limStr + "(" + numStr + ")}{" + limStr + "(" + denStr + ")}" + WorkMgr.EDM,
                "Check if in the indeterminate form.");

            // Is this an indefinite form.
            pEvalData.GetWorkMgr().FromFormatted("",
                "Take the limit of the numerator.");
            WorkStep last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp numEval = TakeLim(num, _varFor, _valTo, ref pEvalData, _leHopitalCount);
            if (numEval is Limit)
                return null;
            last.GoUp(ref pEvalData);
            string numEvalStr = WorkMgr.ToDisp(numEval);

            last.SetWorkHtml(WorkMgr.STM + limStr + "(" + numStr + ")=" + numEvalStr + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("",
                "Take the limit of the denominator");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp denEval = TakeLim(den, _varFor, _valTo, ref pEvalData, _leHopitalCount);
            if (denEval is Limit)
                return null;
            last.GoUp(ref pEvalData);
            string denEvalStr = WorkMgr.ToDisp(denEval);

            last.SetWorkHtml(WorkMgr.STM + limStr + "(" + denStr + ")=" + denEvalStr + WorkMgr.EDM);

            // Check the conditions for applying L'Hopitals rule.
            if (!ExNumber.GetZero().IsEqualTo(numEval) && !ExNumber.GetNegInfinity().IsEqualTo(numEval) && !ExNumber.GetPosInfinity().IsEqualTo(numEval))
                return null;

            if (!numEval.IsEqualTo(denEval))
                return null;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + limStr + "\\frac{" + numStr + "}{" + denStr +
                "}=\\frac{" + numEvalStr + "}{" + denEvalStr + "}" + WorkMgr.EDM,
                "The limit is in the form to apply L'Hospital's Rule.");

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + limStr + "(\\frac{" + numStr + "}{" + denStr +
                "})=\\frac{\\frac{d}{d" + _varFor.ToDispString() + "}[" + numStr + "]}{\\frac{d}{d" + _varFor.ToDispString() + "}[" + denStr + "]}" + WorkMgr.EDM,
                "Apply L'Hospital's rule.");

            // This is in indefinite form.
            pEvalData.GetWorkMgr().FromFormatted("", "Take the derivative of the numerator");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp numDeriv = Derivative.TakeDeriv(num, _varFor, ref pEvalData, false, false);
            last.GoUp(ref pEvalData);

            last.SetWorkHtml(WorkMgr.STM + "\\frac{d}{d" + _varFor.ToDispString() + "}[" + numStr + "]=" + WorkMgr.ToDisp(numDeriv) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("", "Take the derivative of the denominator");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp denDeriv = Derivative.TakeDeriv(den, _varFor, ref pEvalData, false, false);
            last.GoUp(ref pEvalData);

            last.SetWorkHtml(WorkMgr.STM + "\\frac{d}{d" + _varFor.ToDispString() + "}[" + denStr + "]=" + WorkMgr.ToDisp(denDeriv) + WorkMgr.EDM);

            ExComp frac = DivOp.StaticCombine(numDeriv, denDeriv);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + limStr + "(\\frac{" + WorkMgr.ToDisp(numDeriv) + "}{" + WorkMgr.ToDisp(denDeriv) + "})" + WorkMgr.EDM,
                "Divide the derivatives.");

            ExComp evalLim = TakeLim(frac, _varFor, _valTo, ref pEvalData, _leHopitalCount);

            if (evalLim is Limit)
                return null;
            return evalLim;
        }

        private AlgebraTerm ConvertAbsVal(AlgebraTerm term, ExNumber valApproaching, bool pos, ref bool changed)
        {
            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is AbsValFunction)
                {
                    AbsValFunction absValFunc = term[i] as AbsValFunction;
                    ExComp compareEx = SubOp.StaticCombine(_varFor, valApproaching);

                    if (absValFunc.GetInnerEx().IsEqualTo(compareEx))
                    {
                        changed = true;
                        term[i] = pos ? absValFunc.GetInnerTerm() : MulOp.Negate(absValFunc.GetInnerTerm());
                    }
                }
                if (term[i] is AlgebraTerm)
                {
                    term[i] = ConvertAbsVal(term[i] as AlgebraTerm, valApproaching, pos, ref changed);
                }
            }

            return term;
        }

        private bool CheckForLimitDivergence(ExComp ex, ref TermType.EvalData pEvalData)
        {
            if (!(_valTo is ExNumber))
                return false;
            ExNumber nValTo = _valTo as ExNumber;
            // Are the limits the same going from both sides?

            bool changed = false;
            AlgebraTerm posSimp = ConvertAbsVal(ex.CloneEx().ToAlgTerm(), nValTo, true, ref changed);
            if (!changed)
                return false;

            changed = false;
            AlgebraTerm negSimp = ConvertAbsVal(ex.CloneEx().ToAlgTerm(), nValTo, false, ref changed);
            if (!changed)
                return false;

            string innerStr = WorkMgr.ToDisp(ex);

            pEvalData.GetWorkMgr().FromFormatted("",
                "Take the limit from the positive direction.");
            WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "^{+}}" +
                "(" + innerStr + ")=lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "}(" + WorkMgr.ToDisp(posSimp) + ")" + WorkMgr.EDM,
                "As the limit approaches from the positive direction.");
            ExComp posEval = Limit.TakeLim(posSimp.CloneEx(), _varFor, nValTo, ref pEvalData, 0);
            if (posEval is AlgebraTerm)
                posEval = (posEval as AlgebraTerm).RemoveRedundancies(false);
            if (!(posEval is ExNumber))
                return false;
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "^{+}}" +
                                 "(" + innerStr + ")=" + WorkMgr.ToDisp(posEval) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("",
                "Take the limit from the negative direction.");
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "^{-}}" +
                "(" + innerStr + ")=lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "}(" + WorkMgr.ToDisp(negSimp) + ")" + WorkMgr.EDM,
                "As the limit approaches from the negative direction.");
            ExComp negEval = Limit.TakeLim(negSimp.CloneEx(), _varFor, nValTo, ref pEvalData, 0);
            if (negEval is AlgebraTerm)
                negEval = (negEval as AlgebraTerm).RemoveRedundancies(false);
            if (!(negEval is ExNumber))
                return false;
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "^{-}}" +
                                 "(" + innerStr + ")=" + WorkMgr.ToDisp(negEval) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "^{-}}(" +
                innerStr + ") \\ne lim_{" + _varFor.ToDispString() + "=" + nValTo.ToDispString() + "^{+}}(" + innerStr + ")" + WorkMgr.EDM,
                "The limit is not equal from both directions and therefore is divergent.");

            return !posEval.IsEqualTo(negEval);
        }

        public override string FinalToAsciiString()
        {
            return "\\lim_(" + _varFor.ToAsciiString() + "\\to" + _valTo.ToAsciiString() + ")(" + GetInnerTerm().FinalToAsciiString() + ")";
        }

        public override string FinalToDispStr()
        {
            if (USE_TEX)
                return FinalToTexString();
            return FinalToAsciiString();
        }

        public override string FinalToTexString()
        {
            return "\\lim_(" + _varFor.ToTexString() + "\\to" + _valTo.ToTexString() + ")(" + GetInnerTerm().FinalToTexString() + ")";
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Limit)
            {
                Limit lim = ex as Limit;
                return (this.GetInnerEx().IsEqualTo(lim.GetInnerEx()) && this._varFor.IsEqualTo(lim._varFor) && this._valTo.IsEqualTo(lim._valTo));
            }

            return false;
        }

        public override string ToAsciiString()
        {
            return "\\lim_(" + _varFor.ToAsciiString() + "\\to" + _valTo.ToAsciiString() + ")(" + GetInnerTerm().ToAsciiString() + ")";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return "\\lim_(" + _varFor.ToString() + "\\to" + _valTo.ToString() + ")(" + GetInnerTerm().ToString() + ")";
        }

        public override string ToTexString()
        {
            return "\\lim_(" + _varFor.ToTexString() + "\\to" + _valTo.ToTexString() + ")(" + GetInnerTerm().ToTexString() + ")";
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            Limit lim = new Limit(args[0]);
            lim._reducedInner = this._reducedInner == null ? null : this._reducedInner.CloneEx();
            lim._valTo = this._valTo == null ? null : this._valTo.CloneEx();
            lim._varFor = this._varFor == null ? null : (AlgebraComp)this._varFor.CloneEx();

            return lim;
        }

        private ExComp ComponentWiseDiv(AlgebraTerm term, ExComp dividend)
        {
            return ComponentWiseDiv(term, dividend, _varFor);
        }

        public static ExComp ComponentWiseDiv(AlgebraTerm term, ExComp dividend, AlgebraComp varFor)
        {
            if (term is PowerFunction)
            {
                if ((term as PowerFunction).GetBase().IsEqualTo(varFor))
                    return DivOp.StaticCombine(term, dividend);

                PowerFunction pf = term as PowerFunction;
                ExComp root = DivOp.StaticCombine(ExNumber.GetOne(), pf.GetPower());

                return new PowerFunction(ComponentWiseDiv((term as PowerFunction).GetBase().ToAlgTerm(),
                    PowOp.StaticCombine(dividend, root), varFor), pf.GetPower());
            }

            if (term.GetTermCount() == 1)
            {
                ExComp singular = term[0];
                if (singular is PowerFunction)
                {
                    if ((term as PowerFunction).GetBase().IsEqualTo(varFor))
                        return DivOp.StaticCombine(term, dividend);

                    return ComponentWiseDiv((term as PowerFunction).GetBase().ToAlgTerm(),
                        PowOp.StaticCombine(dividend, (term as PowerFunction).GetPower()), varFor);
                }
                else
                    return DivOp.StaticCombine(singular, dividend);
            }

            List<ExComp[]> groups = term.GetGroupsNoOps();

            AlgebraTerm finalTerm = new AlgebraTerm();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (i != 0)
                    finalTerm.Add(new AddOp());

                ExComp[] group = groups[i];
                ExComp[] constTo, varTo;
                GroupHelper.GetConstVarTo(group, out varTo, out constTo, varFor);
                if (varTo.Length == 0)
                {
                    finalTerm.Add(DivOp.StaticCombine(GroupHelper.ToAlgTerm(group), dividend));
                    continue;
                }

                // Only pay attention to the variable terms.
                if (varTo.Length != 1)
                    return null;

                ExComp varToEx = varTo[0];
                ExComp compDiv = ComponentWiseDiv(varToEx.ToAlgTerm(), dividend, varFor);
                ExComp finalAdd = constTo.Length == 0 ? compDiv : MulOp.StaticCombine(GroupHelper.ToAlgTerm(constTo), compDiv);
                finalTerm.Add(finalAdd);
            }

            return finalTerm;
        }

        private ExComp EvaluatePoly(PolynomialExt poly, ref TermType.EvalData pEvalData)
        {
            bool evenFunc = poly.GetMaxPow() % 2 == 0;
            ExComp leadingCoeffEx = poly.GetLeadingCoeff();
            if (!(leadingCoeffEx is ExNumber))
                return null;
            bool neg = ExNumber.OpLT((leadingCoeffEx as ExNumber), 0.0);

            ExComp infRet = null;
            if (ExNumber.GetPosInfinity().IsEqualTo(_valTo))
            {
                infRet = ExNumber.GetPosInfinity();
            }
            else if (ExNumber.GetNegInfinity().IsEqualTo(_valTo))
            {
                infRet = evenFunc ? ExNumber.GetPosInfinity() : ExNumber.GetNegInfinity();
            }

            if (infRet == null)
                return this;

            string explainStr = "The maximum power of the polynomial is " + (evenFunc ? "even" : "odd") + ", therefore, the function approaches `" + infRet.ToDispString() + "`.";

            if (neg)
            {
                infRet = MulOp.Negate(infRet);
                explainStr += " However the leading coefficient of the polynomial is negative so the function ends are reflected resulting in the limit becoming `" + infRet.ToDispString() + "`.";
            }

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + this.FinalToDispStr() + "=" + WorkMgr.ToDisp(infRet) + WorkMgr.EDM, explainStr);

            return infRet;
        }

        private ExComp EvaluateInfinity(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            ExComp attempt = TryNumDenDivide(term, ref pEvalData);
            if (attempt != null)
                return attempt;

            //else if (Number.PosInfinity.IsEqualTo(_valTo))
            //{
            //    List<ExComp> varPowers = term.GetPowersOfVar(_varFor);
            //    if (varPowers.Count == 1 && varPowers[0] is Number)
            //    {
            //        return (varPowers[0] as Number) > 0.0 ? Number.PosInfinity : Number.Zero;
            //    }
            //}

            return null;
        }

        private ExNumber GetHighestPower(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            if (term is PowerFunction)
            {
                ExComp pow = Simplifier.HarshSimplify((term as PowerFunction).GetPower().ToAlgTerm(), ref pEvalData, false);
                if (!(pow is ExNumber))
                    return null;

                ExNumber nPow = pow as ExNumber;
                ExNumber highestInnerPow = GetHighestPower((term as PowerFunction).GetBase().ToAlgTerm(), ref pEvalData);
                if (highestInnerPow == null)
                    return null;

                return ExNumber.OpMul(nPow, highestInnerPow);
            }

            ExNumber max = ExNumber.GetZero();

            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is PowerFunction && (term[i] as PowerFunction).GetBase().IsEqualTo(_varFor))
                {
                    if (!((term[i] as PowerFunction).GetPower() is ExNumber))
                        return null;
                    max = ExNumber.Maximum(max, (term[i] as PowerFunction).GetPower() as ExNumber);
                }
                else if (term[i] is AlgebraFunction && !(term[i] is PowerFunction))
                    return null;
                else if (term[i] is AlgebraComp)
                {
                    if (_varFor.IsEqualTo(term[i]))
                        max = ExNumber.Maximum(max, ExNumber.GetOne());
                    else
                        return null;
                }
                else if (term[i] is AlgebraTerm)
                {
                    ExNumber highestPower = GetHighestPower(term[i] as AlgebraTerm, ref pEvalData);
                    if (highestPower == null)
                        return null;
                    max = ExNumber.Maximum(max, highestPower);
                }
            }

            return max;
        }

        private ExComp PlugIn(ExComp ex, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm subbedIn = _reducedInner.ToAlgTerm().Substitute(_varFor, _valTo);
            ExComp evaluated = TermType.SimplifyGenTermType.BasicSimplify(subbedIn.CloneEx(), ref pEvalData, true);

            if (evaluated != null && !ExNumber.IsUndef(evaluated) && !(evaluated is AlgebraTerm && (evaluated as AlgebraTerm).IsUndefined()))
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + _limStr + "{0}=" + _limStr + "{1}={2}" + WorkMgr.EDM,
                    "As " + WorkMgr.STM + _varFor.ToDispString() + "=" + _valTo.ToDispString() + WorkMgr.EDM + " is defined in this function just plug in the value to evaluate the limit.",
                    ex, subbedIn, evaluated);
                return evaluated;
            }
            return null;
        }

        private AlgebraTerm RemoveOverVarTerms(AlgebraTerm term)
        {
            if (term is PowerFunction)
            {
                PowerFunction pf = term as PowerFunction;
                return new PowerFunction(RemoveOverVarTerms(pf.GetBase().ToAlgTerm()), pf.GetPower());
            }

            List<ExComp[]> groups = term.GetGroups();
            for (int i = 0; i < groups.Count; ++i)
            {
                AlgebraTerm[] numDen = GroupHelper.ToAlgTerm(groups[i]).GetNumDenFrac();

                if (numDen != null && !numDen[0].Contains(_varFor) && numDen[1].Contains(_varFor))
                    ArrayFunc.RemoveIndex(groups, i--);
                else
                {
                    for (int j = 0; j < groups[i].Length; ++j)
                    {
                        if (groups[i][j] is AlgebraTerm)
                        {
                            groups[i][j] = RemoveOverVarTerms(groups[i][j].ToAlgTerm());
                        }
                    }
                }
            }

            return new AlgebraTerm(groups.ToArray());
        }

        private ExComp TryNumDenDivide(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            // Harsh evaluation doesn't matter.
            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen == null)
                return null;

            bool numContains = numDen[0].Contains(_varFor);
            bool denContains = numDen[1].Contains(_varFor);

            if (!numContains && denContains)
                return ExNumber.GetZero();

            if (numDen == null || !numContains || !denContains)
                return null;

            numDen[0] = numDen[0].RemoveRedundancies(false).ToAlgTerm();
            numDen[1] = numDen[1].RemoveRedundancies(false).ToAlgTerm();

            ExNumber numPow = GetHighestPower(numDen[0], ref pEvalData);
            if (numPow == null)
                return null;
            ExNumber denPow = GetHighestPower(numDen[1], ref pEvalData);
            if (denPow == null)
                return null;

            pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "`", "The maximum power of the numerator is `" + numPow.ToDispString() + "` and the maximum power of the denominator is `" + denPow.ToDispString() + "`.");

            if (ExNumber.OpLT(numPow, denPow))
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "`", "As the maximum power of the numerator is less than the maximum power of the denominator this limit evaluates to zero. This comes from `\\lim_(x\\to\\pmoo)\\frac{1}{x^k}=0`");
                return ExNumber.GetZero();
            }

            if (ExNumber.OpGT(numPow, denPow))
            {
                PolynomialExt numPoly = new PolynomialExt();
                PolynomialExt denPoly = new PolynomialExt();

                if (numPoly.Init(numDen[0]) && denPoly.Init(numDen[1]) && numPoly.GetLeadingCoeff() is ExNumber && denPoly.GetLeadingCoeff() is ExNumber)
                {
                    ExNumber numCoeff = numPoly.GetLeadingCoeff() as ExNumber;
                    ExNumber denCoeff = denPoly.GetLeadingCoeff() as ExNumber;

                    bool powDiffEven = (numPoly.GetMaxPow() - denPoly.GetMaxPow()) % 2 == 0;

                    ExComp dividedCoeffEx = ExNumber.OpDiv(numCoeff, denCoeff);

                    if (!(dividedCoeffEx is ExNumber))
                        return null;

                    ExComp infRet = null;
                    if (ExNumber.GetPosInfinity().IsEqualTo(_valTo))
                    {
                        infRet = ExNumber.GetPosInfinity();
                    }
                    else if (ExNumber.GetNegInfinity().IsEqualTo(_valTo))
                    {
                        infRet = powDiffEven ? ExNumber.GetPosInfinity() : ExNumber.GetNegInfinity();
                    }

                    if (ExNumber.OpLT((dividedCoeffEx as ExNumber), 0.0))
                        infRet = MulOp.Negate(infRet);

                    pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "`",
                        "As the maximum power of the numerator is greater than the denominator, the numerator will increase at a faster rate than the denominator making the function go the an infinity." +
                        "The difference between the maximum power of the numerator and denominator is `" + (numPoly.GetMaxPow() - denPoly.GetMaxPow()).ToString() + "` making the function act like an " +
                        (powDiffEven ? "even" : "odd") + " function making the limit be `" + infRet.ToDispString() + "`.");

                    return infRet;
                }

                return null;
            }

            pEvalData.GetWorkMgr().FromFormatted("`" + _thisDispStr + "`", "The maximum power of the denominator is greater than the numerator meaning the limit will converge on a value. Use the statement `\\lim_(x\\to\\pmoo)\\frac{1}{x^k}=0` to evaluate this limit.");

            ExComp dividend = PowOp.StaticCombine(_varFor, denPow);
            ExComp dividedNum = ComponentWiseDiv(numDen[0].CloneEx().ToAlgTerm(), dividend);
            ExComp dividedDen = ComponentWiseDiv(numDen[1].CloneEx().ToAlgTerm(), dividend);

            pEvalData.GetWorkMgr().FromFormatted("`" + _limStr + "({0})/({1})`", "Divide all terms of the numerator and denominator by `" + dividend.ToDispString() + "` to cancel some terms to zero.", dividedNum, dividedDen);

            if (dividedNum == null || dividedDen == null)
                return null;

            // Cancel all of the terms that are in the form 1/(x^n)

            dividedNum = RemoveOverVarTerms(dividedNum.ToAlgTerm());
            dividedDen = RemoveOverVarTerms(dividedDen.ToAlgTerm());

            pEvalData.GetWorkMgr().FromFormatted("`" + _limStr + "({0})/({1})`", "Cancel terms to zero from the statement `\\lim_(x\\to\\pmoo)(1)/(x^k)=0`", dividedNum, dividedDen);

            if (dividedNum is AlgebraTerm)
            {
                dividedNum = (dividedNum as AlgebraTerm).ApplyOrderOfOperations();
                dividedNum = (dividedNum as AlgebraTerm).MakeWorkable();
            }

            if (dividedDen is AlgebraTerm)
            {
                dividedDen = (dividedDen as AlgebraTerm).ApplyOrderOfOperations();
                dividedDen = (dividedDen as AlgebraTerm).MakeWorkable();
            }

            ExComp frac = DivOp.StaticCombine(dividedNum, dividedDen);
            if (frac != null && !ExNumber.IsUndef(frac) && !(frac is AlgebraTerm && (frac as AlgebraTerm).IsUndefined()) &&
                !frac.ToAlgTerm().Contains(_varFor))
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + _limStr + "{0}`", "Simplify.", frac);
                return frac;
            }

            return null;
        }

        private ExComp TryRadicalConjugate(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen == null || numDen[0].Contains(_varFor) || numDen[1].Contains(_varFor))
                return null;

            List<ExComp[]> numGps = numDen[0].GetGroups();
            if (numGps.Count != 2)
                return null;
            ExComp numGp0 = GroupHelper.ToAlgTerm(numGps[0]).RemoveRedundancies(false);
            ExComp numGp1 = GroupHelper.ToAlgTerm(numGps[1]).RemoveRedundancies(false);

            bool numGp0IsRadical = (numGp0 is PowerFunction && (numGp0 as PowerFunction).IsRadical());
            bool numGp1IsRadical = (numGp1 is PowerFunction && (numGp1 as PowerFunction).IsRadical());

            if (!numGp0IsRadical && !numGp1IsRadical)
                return null;

            // Get the conjugate of this radical expression.
            if (numGp0IsRadical)
                numGp0 = MulOp.Negate(numGp0);
            else if (numGp1IsRadical)
                numGp1 = MulOp.Negate(numGp1);

            AlgebraTerm conjugate = new AlgebraTerm(numGp0, new AddOp(), numGp1);

            pEvalData.GetWorkMgr().FromFormatted("`" + _limStr + "({0})/({1})*({2})/({2})`", numDen[0], numDen[1], conjugate, "Multiply the top and bottom by the conjugate.");

            ExComp mulNum = MulOp.StaticCombine(numDen[0], conjugate);
            ExComp mulDen = MulOp.StaticCombine(numDen[1], conjugate);

            ExComp conjugateDiv = DivOp.StaticCombine(mulNum, mulDen);
            pEvalData.GetWorkMgr().FromFormatted("`" + _limStr + "{0}`", "Simplify.", conjugateDiv);
            ExComp plugIn = PlugIn(conjugateDiv, ref pEvalData);
            if (plugIn != null)
                return plugIn;

            return null;
        }
    }
}