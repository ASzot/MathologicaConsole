using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class Limit : AppliedFunction
    {
        private bool _evalFail = false;
        private string _limStr;
        private ExComp _reducedInner = null;
        private string _thisDispStr;
        private ExComp _valTo;
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

        public override ExComp Clone()
        {
            Limit lim = new Limit(InnerTerm);
            lim._reducedInner = this._reducedInner == null ? null : this._reducedInner.Clone();
            lim._valTo = this._valTo == null ? null : this._valTo.Clone();
            lim._varFor = this._varFor == null ? null : (AlgebraComp)this._varFor.Clone();
            lim._evalFail = this._evalFail;

            return lim;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (_evalFail)
                return this;

            _thisDispStr = pEvalData.WorkMgr.AllowWork ? this.FinalToDispStr() : "";
            _limStr = "\\lim_(" + _varFor.ToMathAsciiString() + "\\to" + _valTo.ToMathAsciiString() + ")";

            int stepCount = pEvalData.WorkMgr.WorkSteps.Count;

            // Is the point defined?
            if (_reducedInner == null)
                _reducedInner = TermType.SimplifyTermType.BasicSimplify(InnerTerm, ref pEvalData);

            AlgebraTerm reduced = _reducedInner.ToAlgTerm();
            ExComp attempt;
            if (Number.NegInfinity.IsEqualTo(_valTo) || Number.PosInfinity.IsEqualTo(_valTo))
            {
                attempt = EvaluateInfinity(reduced, ref pEvalData);
                if (attempt == null)
                {
                    _evalFail = true;
                    pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - stepCount);
                    return this;
                }

                pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "={0}`", attempt);

                return attempt;
            }

            ExComp plugIn = PlugIn(_reducedInner, ref pEvalData);
            if (plugIn != null)
            {
                pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "={0}`", plugIn);
                return plugIn;
            }

            attempt = TryRadicalConjugate(reduced, ref pEvalData);
            if (attempt != null)
            {
                pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "={0}`", attempt);
                return attempt;
            }

            _evalFail = true;
            pEvalData.WorkMgr.PopSteps(pEvalData.WorkMgr.WorkSteps.Count - stepCount);
            return this;
        }

        public override string FinalDispKeepFormatting()
        {
            if (USE_TEX)
                return FinalToTexKeepFormatting();
            return FinalToAsciiKeepFormatting();
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return "\\lim_(" + _varFor.ToMathAsciiString() + "\\to" + _valTo.ToMathAsciiString() + ")(" + InnerTerm.FinalToAsciiKeepFormatting() + ")";
        }

        public override string FinalToAsciiString()
        {
            return "\\lim_(" + _varFor.ToMathAsciiString() + "\\to" + _valTo.ToMathAsciiString() + ")(" + InnerTerm.FinalToAsciiString() + ")";
        }

        public override string FinalToDispStr()
        {
            if (USE_TEX)
                return FinalToTexString();
            return FinalToAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            return "\\lim_(" + _varFor.ToTexString() + "\\to" + _valTo.ToTexString() + ")(" + InnerTerm.FinalToTexKeepFormatting() + ")";
        }

        public override string FinalToTexString()
        {
            return "\\lim_(" + _varFor.ToTexString() + "\\to" + _valTo.ToTexString() + ")(" + InnerTerm.FinalToTexString() + ")";
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Limit)
            {
                Limit lim = ex as Limit;
                return (this.InnerEx.IsEqualTo(lim.InnerEx) && this._varFor.IsEqualTo(lim._varFor) && this._valTo.IsEqualTo(lim._valTo));
            }

            return false;
        }

        public override string ToDispString()
        {
            if (USE_TEX)
                return ToTexString();
            return ToMathAsciiString();
        }

        public override string ToMathAsciiString()
        {
            return "\\lim_(" + _varFor.ToMathAsciiString() + "\\to" + _valTo.ToMathAsciiString() + ")(" + InnerTerm.ToMathAsciiString() + ")";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return "\\lim_(" + _varFor.ToString() + "\\to" + _valTo.ToString() + ")(" + InnerTerm.ToString() + ")";
        }

        public override string ToTexString()
        {
            return "\\lim_(" + _varFor.ToTexString() + "\\to" + _valTo.ToTexString() + ")(" + InnerTerm.ToTexString() + ")";
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            Limit lim = new Limit(args[0]);
            lim._reducedInner = this._reducedInner == null ? null : this._reducedInner.Clone();
            lim._valTo = this._valTo == null ? null : this._valTo.Clone();
            lim._varFor = this._varFor == null ? null : (AlgebraComp)this._varFor.Clone();

            return lim;
        }

        private ExComp ComponentWiseDiv(AlgebraTerm term, ExComp dividend)
        {
            if (term is PowerFunction)
            {
                if ((term as PowerFunction).Base.IsEqualTo(_varFor))
                    return DivOp.StaticCombine(term, dividend);

                PowerFunction pf = term as PowerFunction;
                ExComp root = DivOp.StaticCombine(Number.One, pf.Power);

                return new PowerFunction(ComponentWiseDiv((term as PowerFunction).Base.ToAlgTerm(),
                    PowOp.StaticCombine(dividend, root)), pf.Power);
            }

            if (term.TermCount == 1)
            {
                ExComp singular = term[0];
                if (singular is PowerFunction)
                {
                    if ((term as PowerFunction).Base.IsEqualTo(_varFor))
                        return DivOp.StaticCombine(term, dividend);

                    return ComponentWiseDiv((term as PowerFunction).Base.ToAlgTerm(),
                        PowOp.StaticCombine(dividend, (term as PowerFunction).Power));
                }
                else
                    return DivOp.StaticCombine(singular, dividend);
            }

            var groups = term.GetGroupsNoOps();

            AlgebraTerm finalTerm = new AlgebraTerm();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (i != 0)
                    finalTerm.Add(new AddOp());

                var group = groups[i];
                ExComp[] constTo, varTo;
                group.GetConstVarTo(out varTo, out constTo, _varFor);
                if (varTo.Length == 0)
                {
                    finalTerm.Add(DivOp.StaticCombine(group.ToAlgTerm(), dividend));
                    continue;
                }

                // Only pay attention to the variable terms.
                if (varTo.Length != 1)
                    return null;

                ExComp varToEx = varTo[0];
                ExComp compDiv = ComponentWiseDiv(varToEx.ToAlgTerm(), dividend);
                ExComp finalAdd = MulOp.StaticCombine(constTo.ToAlgTerm(), compDiv);
                finalTerm.Add(finalAdd);
            }

            return finalTerm;
        }

        private ExComp EvaluateInfinity(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            ExComp attempt = TryNumDenDivide(term, ref pEvalData);
            if (attempt != null)
                return attempt;

            PolynomialExt poly = new PolynomialExt();
            if (poly.Init(term))
            {
                bool evenFunc = poly.MaxPow % 2 == 0;
                ExComp leadingCoeffEx = poly.LeadingCoeff;
                if (!(leadingCoeffEx is Number))
                    return null;
                bool neg = (leadingCoeffEx as Number) < 0.0;

                ExComp infRet = null;
                if (Number.PosInfinity.IsEqualTo(_valTo))
                {
                    infRet = Number.PosInfinity;
                }
                else if (Number.NegInfinity.IsEqualTo(_valTo))
                {
                    infRet = evenFunc ? Number.PosInfinity : Number.NegInfinity;
                }

                if (infRet == null)
                    return null;

                string explainStr = "The maximum power of the polynomial is " + (evenFunc ? "even" : "odd") + ", therefore, the function approaches `" + infRet.ToDispString() + "`.";

                if (neg)
                {
                    infRet = MulOp.Negate(infRet);
                    explainStr += " However the leading coefficient of the polynomial is negative so the function ends are reflected resulting in the limit becoming `" + infRet.ToDispString() + "`.";
                }

                pEvalData.WorkMgr.FromFormatted("`" + this.FinalToDispStr() + "`", explainStr);

                return infRet;
            }

            return null;
        }

        private Number GetHighestPower(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            if (term is PowerFunction)
            {
                ExComp pow = Simplifier.HarshSimplify((term as PowerFunction).Power.ToAlgTerm(), ref pEvalData, false);
                if (!(pow is Number))
                    return null;

                Number nPow = pow as Number;
                Number highestInnerPow = GetHighestPower((term as PowerFunction).Base.ToAlgTerm(), ref pEvalData);
                if (highestInnerPow == null)
                    return null;

                return nPow * highestInnerPow;
            }

            Number max = Number.Zero;

            for (int i = 0; i < term.TermCount; ++i)
            {
                if (term[i] is PowerFunction && (term[i] as PowerFunction).Base.IsEqualTo(_varFor))
                {
                    if (!((term[i] as PowerFunction).Power is Number))
                        return null;
                    max = Number.Maximum(max, (term[i] as PowerFunction).Power as Number);
                }
                else if (term[i] is AlgebraFunction && !(term[i] is PowerFunction))
                    return null;
                else if (term[i] is AlgebraComp)
                {
                    if (_varFor.IsEqualTo(term[i]))
                        max = Number.Maximum(max, Number.One);
                    else
                        return null;
                }
                else if (term[i] is AlgebraTerm)
                {
                    Number highestPower = GetHighestPower(term[i] as AlgebraTerm, ref pEvalData);
                    if (highestPower == null)
                        return null;
                    max = Number.Maximum(max, highestPower);
                }
            }

            return max;
        }

        private ExComp PlugIn(ExComp ex, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm subbedIn = _reducedInner.ToAlgTerm().Substitute(_varFor, _valTo);
            ExComp evaluated = TermType.SimplifyTermType.BasicSimplify(subbedIn, ref pEvalData);

            if (evaluated != null && !Number.IsUndef(evaluated) && !(evaluated is AlgebraTerm && (evaluated as AlgebraTerm).IsUndefined()))
            {
                pEvalData.WorkMgr.FromFormatted("`" + _limStr + "{0}=" + _limStr + "{1}={2}`",
                    "As `" + _varFor.ToDispString() + "=" + _valTo.ToDispString() + "`" + " is defined in this function just plug in the value to evaluate the limit.",
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
                return new PowerFunction(RemoveOverVarTerms(pf.Base.ToAlgTerm()), pf.Power);
            }

            var groups = term.GetGroups();
            for (int i = 0; i < groups.Count; ++i)
            {
                var numDen = groups[i].ToAlgTerm().GetNumDenFrac();

                if (numDen != null && !numDen[0].Contains(_varFor) && numDen[1].Contains(_varFor))
                    groups.RemoveAt(i--);
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
                return Number.Zero;

            if (numDen == null || !numContains || !denContains)
                return null;

            numDen[0] = numDen[0].RemoveRedundancies().ToAlgTerm();
            numDen[1] = numDen[1].RemoveRedundancies().ToAlgTerm();

            Number numPow = GetHighestPower(numDen[0], ref pEvalData);
            if (numPow == null)
                return null;
            Number denPow = GetHighestPower(numDen[1], ref pEvalData);
            if (denPow == null)
                return null;

            pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "`", "The maximum power of the numerator is `" + numPow.ToDispString() + "` and the maximum power of the denominator is `" + denPow.ToDispString() + "`.");

            if (numPow < denPow)
            {
                pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "`", "As the maximum power of the numerator is less than the maximum power of the denominator this limit evaluates to zero. This comes from `\\lim_(x\\to\\pmoo)\\frac{1}{x^k}=0`");
                return Number.Zero;
            }

            if (numPow > denPow)
            {
                PolynomialExt numPoly = new PolynomialExt();
                PolynomialExt denPoly = new PolynomialExt();

                if (numPoly.Init(numDen[0]) && denPoly.Init(numDen[1]) && numPoly.LeadingCoeff is Number && denPoly.LeadingCoeff is Number)
                {
                    Number numCoeff = numPoly.LeadingCoeff as Number;
                    Number denCoeff = denPoly.LeadingCoeff as Number;

                    bool powDiffEven = (numPoly.MaxPow - denPoly.MaxPow) % 2 == 0;

                    ExComp dividedCoeffEx = numCoeff / denCoeff;

                    if (!(dividedCoeffEx is Number))
                        return null;

                    ExComp infRet = null;
                    if (Number.PosInfinity.IsEqualTo(_valTo))
                    {
                        infRet = Number.PosInfinity;
                    }
                    else if (Number.NegInfinity.IsEqualTo(_valTo))
                    {
                        infRet = powDiffEven ? Number.PosInfinity : Number.NegInfinity;
                    }

                    if ((dividedCoeffEx as Number) < 0.0)
                        infRet = MulOp.Negate(infRet);

                    pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "`",
                        "As the maximum power of the numerator is greater than the denominator, the numerator will increase at a faster rate than the denominator making the function go the an infinity." +
                        "The difference between the maximum power of the numerator and denominator is `" + (numPoly.MaxPow - denPoly.MaxPow).ToString() + "` making the function act like an " +
                        (powDiffEven ? "even" : "odd") + " function making the limit be `" + infRet.ToDispString() + "`.");

                    return infRet;
                }

                return null;
            }

            pEvalData.WorkMgr.FromFormatted("`" + _thisDispStr + "`", "The maximum power of the denominator is greater than the numerator meaning the limit will converge on a value. Use the statement `\\lim_(x\\to\\pmoo)\\frac{1}{x^k}=0` to evaluate this limit.");

            ExComp dividend = PowOp.StaticCombine(_varFor, denPow);
            ExComp dividedNum = ComponentWiseDiv(numDen[0].Clone().ToAlgTerm(), dividend);
            ExComp dividedDen = ComponentWiseDiv(numDen[1].Clone().ToAlgTerm(), dividend);

            pEvalData.WorkMgr.FromFormatted("`" + _limStr + "({0})/({1})`", "Divide all terms of the numerator and denominator by `" + dividend.ToDispString() + "` to cancel some terms to zero.", dividedNum, dividedDen);

            if (dividedNum == null || dividedDen == null)
                return null;

            // Cancel all of the terms that are in the form 1/(x^n)

            dividedNum = RemoveOverVarTerms(dividedNum.ToAlgTerm());
            dividedDen = RemoveOverVarTerms(dividedDen.ToAlgTerm());

            pEvalData.WorkMgr.FromFormatted("`" + _limStr + "({0})/({1})`", "Cancel terms to zero from the statement `\\lim_(x\\to\\pmoo)(1)/(x^k)=0`", dividedNum, dividedDen);

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
            if (frac != null && !Number.IsUndef(frac) && !(frac is AlgebraTerm && (frac as AlgebraTerm).IsUndefined()) &&
                !frac.ToAlgTerm().Contains(_varFor))
            {
                pEvalData.WorkMgr.FromFormatted("`" + _limStr + "{0}`", "Simplify.", frac);
                return frac;
            }

            return null;
        }

        private ExComp TryRadicalConjugate(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen == null || numDen[0].Contains(_varFor) || numDen[1].Contains(_varFor))
                return null;

            var numGps = numDen[0].GetGroups();
            if (numGps.Count != 2)
                return null;
            ExComp numGp0 = numGps[0].ToAlgTerm().RemoveRedundancies();
            ExComp numGp1 = numGps[1].ToAlgTerm().RemoveRedundancies();

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

            pEvalData.WorkMgr.FromFormatted("`" + _limStr + "({0})/({1})*({2})/({2})`", numDen[0], numDen[1], conjugate, "Multiply the top and bottom by the conjugate.");

            ExComp mulNum = MulOp.StaticCombine(numDen[0], conjugate);
            ExComp mulDen = MulOp.StaticCombine(numDen[1], conjugate);

            ExComp conjugateDiv = DivOp.StaticCombine(mulNum, mulDen);
            pEvalData.WorkMgr.FromFormatted("`" + _limStr + "{0}`", "Simplify.", conjugateDiv);
            ExComp plugIn = PlugIn(conjugateDiv, ref pEvalData);
            if (plugIn != null)
                return plugIn;

            return null;
        }
    }
}