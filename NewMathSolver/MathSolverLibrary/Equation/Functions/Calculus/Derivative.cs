using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class Derivative : AppliedFunction
    {
        private const int MAX_DERIV = 7;

        private AlgebraComp _derivOf = null;
        private bool _isDefined = true;
        private bool _isPartial = false;
        private ExComp _order = ExNumber.GetOne();
        private AlgebraComp _withRespectTo;
        private string ca_derivSymb = null;
        private AlgebraComp ca_impDeriv = null;

        /// <summary>
        /// Used for notation like f'(2) where a value is to be
        /// evaluated after the derivative has been taken.
        /// No input value is null.
        /// </summary>
        private ExComp _inputVal = null;

        public Derivative(ExComp innerEx)
            : base(innerEx, FunctionType.Derivative, typeof(Derivative))
        {
        }

        public AlgebraComp GetDerivOf()
        {
            return _derivOf;
        }

        public bool GetIsOrderOne()
        {
            return GetOrderInt() == 1;
        }

        public string GetNotationIden()
        {
            return _isPartial ? "\\partial" : "d";
        }

        public int GetOrderInt()
        {
            if (!(_order is ExNumber))
                return -1;
            ExNumber num = _order as ExNumber;
            if (!num.IsRealInteger())
                return -1;

            return (int) num.GetRealComp();
        }

        public void SetWithRespectTo(AlgebraComp value)
        {
            _withRespectTo = value;
        }

        public AlgebraComp GetWithRespectTo()
        {
            return _withRespectTo;
        }

        public bool GetIsPartial()
        {
            return _isPartial;
        }

        public static Derivative ConstructDeriv(ExComp innerEx, AlgebraComp withRespect, AlgebraComp derivOf)
        {
            return ConstructDeriv(derivOf, innerEx, null, withRespect, ExNumber.GetOne());
        }

        public static Derivative ConstructDeriv(AlgebraComp withRespect, AlgebraComp derivOf)
        {
            Derivative deriv = ConstructDeriv(derivOf, ExNumber.GetZero(), null, withRespect, ExNumber.GetOne());
            deriv._isDefined = false;

            return deriv;
        }

        public static Derivative ConstructDeriv(AlgebraComp funcIden, ExComp inputVal, ExComp order)
        {
            return ConstructDeriv(funcIden, ExNumber.GetZero(), inputVal, null, order);
        }

        public static Derivative ConstructDeriv(AlgebraComp derivOf, ExComp innerEx, ExComp inputVal, AlgebraComp withRespect, ExComp order)
        {
            Derivative deriv = new Derivative(innerEx);
            deriv._withRespectTo = withRespect;
            deriv._derivOf = derivOf;
            deriv._order = order;
            deriv._inputVal = inputVal;

            return deriv;
        }

        public static Derivative Parse(string function, string withRespectTo, ExComp order, bool isPartial,
            ref TermType.EvalData pEvalData, bool mustBeSingleVar = false)
        {
            if (withRespectTo != null && withRespectTo.Length == 0)
                return null;

            AlgebraComp respectToCmp = withRespectTo == null ? null : new AlgebraComp(withRespectTo);

            Derivative deriv;
            deriv = new Derivative(ExNumber.GetZero());
            deriv._isDefined = false;
            deriv._derivOf = new AlgebraComp(function);

            deriv.SetWithRespectTo(respectToCmp);
            deriv._order = order;
            deriv._isPartial = isPartial;

            return deriv;
        }

        public static Derivative Parse(string withRespectTo, ExComp inner, ExComp order, bool isPartial)
        {
            if (withRespectTo != null && withRespectTo.Length == 0)
                return null;

            Derivative deriv = new Derivative(inner);
            deriv.SetWithRespectTo(withRespectTo == null ? null : new AlgebraComp(withRespectTo));
            deriv._order = order;
            deriv._isPartial = isPartial;

            return deriv;
        }

        public static ExComp TakeDeriv(ExComp term, AlgebraComp withRespectTo, ref EvalData pEvalData, bool isPartial, bool isFuncDeriv)
        {
            if ((term is AlgebraComp || term is FunctionDefinition) && !term.IsEqualTo(withRespectTo) && isFuncDeriv)
                return ConstructImplicitDerivAgCmp(term, withRespectTo, isPartial);

            Derivative deriv = ConstructDeriv(term, withRespectTo, null);
            deriv._isPartial = isPartial;

            ExComp eval = deriv.Evaluate(false, ref pEvalData);
            return eval;
        }

        public ExComp GetDerivOfFunc(FunctionDefinition funcDef, ExComp def)
        {
            if (_derivOf == null || !funcDef.GetIden().IsEqualTo(_derivOf))
                return null;

            if (_withRespectTo == null)
            {
                if (!funcDef.GetHasValidInputArgs() || funcDef.GetInputArgCount() != 1)
                    return null;
            }

            //else if (_isPartial)
            //{
            //	bool contains = false;
            //	for (int i = 0; i < funcDef.InputArgCount; ++i)
            //	{
            //		if (_withRespectTo.IsEqualTo(funcDef.InputArgs[i]))
            //		{
            //			contains = true;
            //			break;
            //		}
            //	}

            //	if (!contains)
            //		return null;
            //}
            //else if (!_isPartial)
            //{
            //	if (funcDef.InputArgCount != 1)
            //		return null;

            //	if (!_withRespectTo.IsEqualTo(funcDef.InputArgs[0]))
            //		return null;
            //}

            return Derivative.ConstructDeriv(_inputVal == null ? null : _derivOf, def, _inputVal, _withRespectTo == null ? funcDef.GetInputArgs()[0] : _withRespectTo, _order);
        }

        public override ExComp CloneEx()
        {
            Derivative deriv = new Derivative(GetInnerEx().CloneEx());
            deriv._withRespectTo = this._withRespectTo == null ? null : (AlgebraComp)this._withRespectTo.CloneEx();
            deriv._order = this._order.CloneEx();
            deriv._derivOf = this._derivOf == null ? null : (AlgebraComp)this._derivOf.CloneEx();
            deriv._isDefined = this._isDefined;
            deriv._isPartial = this._isPartial;
            deriv._inputVal = this._inputVal == null ? null : _inputVal.CloneEx();
            return deriv;
        }

        public AlgebraComp ConstructImplicitDerivAgCmp()
        {
            if (ca_impDeriv == null)
                ca_impDeriv = ConstructImplicitDerivAgCmp(_derivOf, _withRespectTo, false);
            return ca_impDeriv;
        }

        public ExComp GetOrder()
        {
            return _order;
        }

        public override bool Contains(AlgebraComp varFor)
        {
            return _order.ToAlgTerm().Contains(varFor) || base.Contains(varFor);
        }

        private static AlgebraComp ConstructImplicitDerivAgCmp(ExComp derivOf, ExComp withRespectTo, bool isPartial)
        {
            string iden = isPartial ? "\\partial" : "d";
            return new AlgebraComp("(" + iden + derivOf.ToDispString() + ")/(" + iden + withRespectTo.ToDispString() + ")");
        }

        public override ExComp CancelWith(ExComp innerEx, ref TermType.EvalData pEvalData)
        {
            if (!(_order is ExNumber && (_order as ExNumber).IsRealInteger()))
                return null;

            int order = (int)(_order as ExNumber).GetRealComp();

            if (innerEx is Integral && _derivOf == null && order == 1)
            {
                Integral finalInt = innerEx as Integral;
                if (finalInt.GetDVar().IsEqualTo(_withRespectTo) && !finalInt.GetIsDefinite())
                {
                    pEvalData.GetWorkMgr().FromSides(this, null, "The derivative and the integral cancel.");
                    return finalInt.GetInnerTerm();
                }
                else if (finalInt.GetIsDefinite())
                {
                    bool upperContains = finalInt.GetUpperLimitTerm().ToAlgTerm().Contains(_withRespectTo);
                    bool lowerContains = finalInt.GetLowerLimitTerm().ToAlgTerm().Contains(_withRespectTo);
                    if (upperContains || lowerContains)
                    {
                        pEvalData.GetWorkMgr().FromSides(this, null, "Use the fundemental theorem of calculus.");
                        // Fundemental Theorem of calculus should be applied here.
                        Integral[] ints;
                        if (upperContains && lowerContains)
                        {
                            AlgebraComp tmpBoundryVar = new AlgebraComp("a");
                            Integral otherInt = Integral.ConstructIntegral(finalInt.GetInnerTerm(), finalInt.GetDVar(), finalInt.GetLowerLimit(), tmpBoundryVar, false, true);
                            finalInt.SetLowerLimit(tmpBoundryVar);

                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + ca_derivSymb + "[" + finalInt.FinalToDispStr() + "+" + otherInt.FinalToDispStr() + "]" + WorkMgr.EDM, "Split the integral");
                            ExComp tmp = otherInt.GetUpperLimit();
                            otherInt.SetUpperLimit(otherInt.GetLowerLimit());
                            otherInt.SetLowerLimit(tmp);
                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + ca_derivSymb + "[" + finalInt.FinalToDispStr() + "-" + otherInt.FinalToDispStr() + "]" + WorkMgr.EDM, "Switch the integral bounds");

                            ints = new Integral[] { finalInt, otherInt };
                        }
                        else if (lowerContains)
                        {
                            Integral tmpInt = Integral.ConstructIntegral(MulOp.Negate(finalInt.GetInnerEx()), finalInt.GetDVar(), finalInt.GetUpperLimit(), finalInt.GetLowerLimit(), false, true);
                            pEvalData.GetWorkMgr().FromSides(tmpInt, null, "Switch the integral bounds.");
                            ints = new Integral[] { tmpInt };
                        }
                        else
                            ints = new Integral[] { finalInt };

                        ExComp[] modified = new ExComp[ints.Length];
                        Derivative[] chainRules = new Derivative[ints.Length];
                        for (int i = 0; i < ints.Length; ++i)
                        {
                            modified[i] = ints[i].GetInnerTerm().Substitute(ints[i].GetDVar(), ints[i].GetUpperLimit());
                            chainRules[i] = ints[i].GetUpperLimit().IsEqualTo(_withRespectTo) ? null : Derivative.ConstructDeriv(ints[i].GetUpperLimit(), _withRespectTo, null);
                        }

                        string dispStr = "";
                        bool useChainRule = false;
                        for (int i = 0; i < modified.Length; ++i)
                        {
                            if (chainRules[i] != null)
                            {
                                useChainRule = true;
                                dispStr += chainRules[i].FinalToDispStr();
                                dispStr += "(";
                            }

                            dispStr += WorkMgr.ToDisp(modified[i]);

                            if (chainRules[i] != null)
                            {
                                dispStr += ")";
                            }
                        }

                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + dispStr + WorkMgr.EDM, "Substitute in" + (useChainRule ? " and use the chain rule." : "."));

                        ExComp[] chainRuled = new ExComp[chainRules.Length];
                        dispStr = "";
                        for (int i = 0; i < chainRuled.Length; ++i)
                        {
                            chainRuled[i] = chainRules[i] == null ? null : chainRules[i].Evaluate(false, ref pEvalData);
                            if (chainRuled[i] != null)
                            {
                                dispStr += WorkMgr.ToDisp(chainRuled[i]);
                                dispStr += "(";
                            }

                            dispStr += WorkMgr.ToDisp(modified[i]);

                            if (chainRuled[i] != null)
                                dispStr += ")";
                        }

                        if (useChainRule)
                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + dispStr + WorkMgr.EDM);

                        ExComp totalTerm = new AlgebraTerm();
                        for (int i = 0; i < chainRuled.Length; ++i)
                        {
                            totalTerm = AddOp.StaticCombine(totalTerm, chainRuled[i] == null ? modified[i] : (MulOp.StaticCombine(modified[i], chainRuled[i])));
                        }

                        if (useChainRule)
                            pEvalData.GetWorkMgr().FromSides(totalTerm, null);

                        return totalTerm;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Harsh evaluations have no importance here as derivatives are always symbolic.
        /// </summary>
        /// <param name="harshEval">Value doesn't matter.</param>
        /// <returns></returns>
        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (!_isDefined)
                return this;

            if (_withRespectTo == null)
                return this;

            if (!(_order is ExNumber && (_order as ExNumber).IsRealInteger()))
                return this;

            int order = (int)(_order as ExNumber).GetRealComp();
            if (order < 1)
                return ExNumber.GetUndefined();

            if (order > MAX_DERIV)
                return this;

            if (_isPartial)
                pEvalData.AttemptSetInputType(TermType.InputType.PartialDerivative);

            ca_derivSymb = "d/(d" + _withRespectTo.ToDispString() + ")";

            ExComp finalInnerEx = GetInnerEx();

            if (finalInnerEx is ExVector)
            {
                ExVector vec = finalInnerEx as ExVector;
                // Take the derivative of each component separately.
                ExVector derivVec = vec.CreateEmptyBody();

                // Work steps should go here.

                for (int i = 0; i < vec.GetLength(); ++i)
                {
                    ExComp deriv = TakeDerivativeOf(vec.Get(i), ref pEvalData);
                    derivVec.Set(i, deriv);
                }

                // Sub in the point and evaluate.
                if (_inputVal != null)
                {
                    AlgebraTerm subbedTerm = derivVec.Substitute(_withRespectTo, _inputVal);
                    ExComp derivVecSimp = Simplifier.Simplify(subbedTerm, ref pEvalData);
                    return derivVecSimp;
                }

                return derivVec;
            }
            else if (finalInnerEx is ExMatrix)
            {
                // Don't know if this works.
                return ExNumber.GetUndefined();
            }

            pEvalData.GetWorkMgr().FromFormatted("`{0}`", "Find the " + (_order).ToString() + MathHelper.GetCountingPrefix(order)
                + " derivative of the above.", GetInnerEx());
            for (int i = 0; i < order; ++i)
            {
                ExComp tmp = TakeDerivativeOf(finalInnerEx, ref pEvalData);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]={1}`", "The final " + (i + 1).ToString() +
                    MathHelper.GetCountingPrefix(i + 1) + " derivative.", finalInnerEx, tmp);
                finalInnerEx = tmp;
            }

            if (_inputVal != null)
            {
                finalInnerEx = finalInnerEx.ToAlgTerm().Substitute(_withRespectTo, _inputVal);
                AlgebraTerm termWrapper = new AlgebraTerm(finalInnerEx);
                finalInnerEx = Simplifier.Simplify(termWrapper, ref pEvalData);
            }

            return finalInnerEx;
        }

        public override string FinalToAsciiString()
        {
            string orderStr = GetIsOrderOne() ? "" : "^{" + _order.ToAsciiString() + "}";
            string followingStr = null;
            if (_order is ExNumber && (_order as ExNumber).IsRealInteger())
            {
                int order = (int)(_order as ExNumber).GetRealComp();
                if (order < 3)
                {
                    followingStr = "";
                    // Use prime notation.
                    for (int i = 0; i < order; ++i)
                        followingStr += "'";
                }
            }

            if (_withRespectTo == null)
            {
                if (_inputVal == null)
                    return _derivOf.ToAsciiString() + (followingStr == null ? orderStr : followingStr);
                else
                    return _derivOf.ToAsciiString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToAlgTerm().FinalToAsciiString() + ")";
            }

            if (!_isDefined || _inputVal != null)
            {
                if (_inputVal == null)
                    return "(\\frac{" + GetNotationIden() + orderStr + _derivOf.ToAsciiString() + "}{" + GetNotationIden() + _withRespectTo.ToAsciiString() + orderStr + "})";
                else
                    return _derivOf.ToAsciiString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToAlgTerm().FinalToAsciiString() + ")";
            }
            return "(\\frac{" + GetNotationIden() + orderStr + "}{" + GetNotationIden() + _withRespectTo.ToAsciiString() + orderStr + "}[" +
                GetInnerEx().ToAlgTerm().FinalToAsciiString() + "])";
        }

        public override string FinalToTexString()
        {
            string orderStr = GetIsOrderOne() ? "" : "^{" + _order.ToTexString() + "}";
            string followingStr = null;
            if (_order is ExNumber && (_order as ExNumber).IsRealInteger())
            {
                int order = (int)(_order as ExNumber).GetRealComp();
                if (order < 3)
                {
                    followingStr = "";
                    // Use prime notation.
                    for (int i = 0; i < order; ++i)
                        followingStr += "'";
                }
            }

            if (_withRespectTo == null)
            {
                if (_inputVal == null)
                    return _derivOf.ToTexString() + (followingStr == null ? orderStr : followingStr);
                else
                    return _derivOf.ToTexString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToAlgTerm().FinalToTexString() + ")";
            }

            if (!_isDefined || _inputVal != null)
            {
                if (_inputVal == null)
                    return "(\\frac{" + GetNotationIden() + orderStr + _derivOf.ToTexString() + "}{" + GetNotationIden() + _withRespectTo.ToTexString() + orderStr + "})";
                else
                    return _derivOf.ToTexString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToAlgTerm().FinalToTexString() + ")";
            }
            return "(\\frac{" + GetNotationIden() + orderStr + "}{" + GetNotationIden() + _withRespectTo.ToTexString() + orderStr + "}[" +
                GetInnerEx().ToAlgTerm().FinalToTexString() + "])";
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Derivative)
            {
                Derivative deriv = ex as Derivative;

                return deriv.GetInnerEx().IsEqualTo(GetInnerEx()) &&
                    ((deriv._withRespectTo != null && deriv._withRespectTo.IsEqualTo(this._withRespectTo)) ||
                    (deriv._withRespectTo == null && this._withRespectTo == null)) &&
                    ((deriv._derivOf != null && deriv._derivOf.IsEqualTo(this._derivOf)) ||
                    (deriv._derivOf == null && this._derivOf == null)) &&
                    (deriv._isPartial == this._isPartial) &&
                    deriv._order.IsEqualTo(_order);
            }

            return false;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            Derivative deriv = new Derivative(GetInnerTerm().Substitute(subOut, subIn));
            deriv._derivOf = this._derivOf;
            deriv._isDefined = this._isDefined;
            deriv._isPartial = this._isPartial;
            if (this._inputVal != null)
                deriv._inputVal = this._inputVal.IsEqualTo(subOut) ? subIn : this._inputVal;
            if (this._order.IsEqualTo(subOut) && (subIn is AlgebraComp || subIn is ExNumber))
                deriv._order = subIn;
            else
                deriv._order = this._order;
            deriv._withRespectTo = this._withRespectTo;
            deriv.ca_derivSymb = this.ca_derivSymb;
            deriv.ca_impDeriv = this.ca_impDeriv;

            return deriv;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            Derivative deriv = new Derivative(GetInnerTerm().Substitute(subOut, subIn, ref success));
            deriv._derivOf = this._derivOf;
            deriv._isDefined = this._isDefined;
            deriv._isPartial = this._isPartial;
            if (this._order.IsEqualTo(subOut) && subIn is AlgebraComp || subIn is ExNumber)
            {
                deriv._order = subIn;
                success = true;
            }
            else
                deriv._order = this._order;
            deriv._withRespectTo = this._withRespectTo;
            deriv.ca_derivSymb = this.ca_derivSymb;
            deriv.ca_impDeriv = this.ca_impDeriv;

            return deriv;
        }

        public override string ToAsciiString()
        {
            string orderStr = GetIsOrderOne() ? "" : "^{" + _order.ToAsciiString() + "}";
            string followingStr = null;
            if (_order is ExNumber && (_order as ExNumber).IsRealInteger())
            {
                int order = (int)(_order as ExNumber).GetRealComp();
                if (order < 3)
                {
                    followingStr = "";
                    // Use prime notation.
                    for (int i = 0; i < order; ++i)
                        followingStr += "'";
                }
            }

            if (_withRespectTo == null)
            {
                if (_inputVal == null)
                    return _derivOf.ToAsciiString() + (followingStr == null ? orderStr : followingStr);
                else
                    return _derivOf.ToAsciiString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToAsciiString() + ")";
            }

            if (!_isDefined || _inputVal != null)
            {
                if (_inputVal == null)
                    return "(\\frac{" + GetNotationIden() + orderStr + _derivOf.ToAsciiString() + "}{" + GetNotationIden() + _withRespectTo.ToAsciiString() + orderStr + "})";
                else
                    return _derivOf.ToAsciiString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToAsciiString() + ")";
            }
            return "(\\frac{" + GetNotationIden() + orderStr + "}{" + GetNotationIden() + _withRespectTo.ToAsciiString() + orderStr + "}[" +
                GetInnerEx().ToAsciiString() + "])";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return ToTexString();
        }

        public override string ToTexString()
        {
            string orderStr = GetIsOrderOne() ? "" : "^{" + _order.ToTexString() + "}";
            string followingStr = null;
            if (_order is ExNumber && (_order as ExNumber).IsRealInteger())
            {
                int order = (int)(_order as ExNumber).GetRealComp();
                if (order < 3)
                {
                    followingStr = "";
                    // Use prime notation.
                    for (int i = 0; i < order; ++i)
                        followingStr += "'";
                }
            }

            if (_withRespectTo == null)
            {
                if (_inputVal == null)
                    return _derivOf.ToTexString() + (followingStr == null ? orderStr : followingStr);
                else
                    return _derivOf.ToTexString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToTexString() + ")";
            }

            if (!_isDefined || _inputVal != null)
            {
                if (_inputVal == null)
                    return "(\\frac{" + GetNotationIden() + orderStr + _derivOf.ToTexString() + "}{" + GetNotationIden() + _withRespectTo.ToTexString() + orderStr + "})";
                else
                    return _derivOf.ToTexString() + (followingStr == null ? orderStr : followingStr) + "(" + _inputVal.ToTexString() + ")";
            }
            return "(\\frac{" + GetNotationIden() + orderStr + "}{" + GetNotationIden() + _withRespectTo.ToTexString() + orderStr + "}[" +
                GetInnerEx().ToTexString() + "])";
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            Derivative deriv = new Derivative(args[0]);
            deriv._withRespectTo = this._withRespectTo == null ? null : (AlgebraComp)this._withRespectTo.CloneEx();
            deriv._order = this._order.CloneEx();
            deriv._isDefined = this._isDefined;
            deriv._derivOf = this._derivOf == null ? null : (AlgebraComp)this._derivOf.CloneEx();
            deriv._isPartial = this._isPartial;
            deriv._inputVal = this._inputVal == null ? null : this._inputVal.CloneEx();
            return deriv;
        }

        private ExComp ApplyAbsDeriv(AbsValFunction abs, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = abs.GetInnerEx();
            bool useChainRule = ShouldApplyChainRule(innerEx);
            if (useChainRule)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", abs);
            }

            pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=({0})/({1})`", "From the common derivative of the absolute value function.", abs.GetInnerTerm(), abs);

            ExComp deriv = DivOp.StaticCombine(innerEx, abs);

            if (useChainRule)
            {
                pEvalData.AddInputType(TermType.InputAddType.DerivCR);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", abs.GetInnerTerm());
                WorkStep last = pEvalData.GetWorkMgr().GetLast();

                last.GoDown(ref pEvalData);
                ExComp innerDeriv = TakeDerivativeOf(abs.GetInnerTerm(), ref pEvalData);
                last.GoUp(ref pEvalData);

                pEvalData.GetWorkMgr().FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                // Apply the chain rule.
                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private ExComp ApplyInvTrigDeriv(InverseTrigFunction invTrigFunc, ref TermType.EvalData pEvalData)
        {
            bool useChainRule = ShouldApplyChainRule(invTrigFunc.GetInnerEx());
            if (useChainRule)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", invTrigFunc);
            }

            ExComp deriv = invTrigFunc.GetDerivativeOf();
            pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]={1}`",
                "Use the common definition that `d/(dx)[" + invTrigFunc.GetFuncName() + "(x)]=" + invTrigFunc.GetDerivativeOfStr() + "`.", invTrigFunc, deriv);

            pEvalData.AttemptSetInputType(TermType.InputType.DerivInvTrig);

            if (useChainRule)
            {
                pEvalData.AddInputType(TermType.InputAddType.DerivCR);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", invTrigFunc.GetInnerTerm());
                WorkStep last = pEvalData.GetWorkMgr().GetLast();

                last.GoDown(ref pEvalData);
                // Apply the chain rule.
                ExComp innerDeriv = TakeDerivativeOf(invTrigFunc.GetInnerTerm(), ref pEvalData);
                last.GoUp(ref pEvalData);

                pEvalData.GetWorkMgr().FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private ExComp ApplyLogDeriv(LogFunction log, ref TermType.EvalData pEvalData)
        {
            if (log.GetBase().ToAlgTerm().Contains(_withRespectTo))
            {
                // Terms in the form log_x(y) cannot have the derivative be taken with respect to x.
                return ConstructDeriv(log, _withRespectTo, _derivOf);
            }

            bool useChainRule = ShouldApplyChainRule(log.GetInnerEx());
            if (useChainRule)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", log);
            }

            ExComp deriv;
            if (log.GetBase().IsEqualTo(Constant.GetE()))
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=1/({1})`",
                    "Using the definition that `d/(dx)[log_b(x)]=1/(ln(b)x)` which can be extended to natural logs to say `d/(dx)[ln(x)]=1/x`",
                    log, log.GetInnerTerm());
                deriv = DivOp.StaticCombine(ExNumber.GetOne(), log.GetInnerTerm());
            }
            else
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=1/(ln({2})({1}))`",
                    "Using the definition that `d/(dx)[log_b(x)]=1/(ln(b)x)`",
                    log, log.GetInnerTerm(), log.GetBase());
                deriv = DivOp.StaticCombine(ExNumber.GetOne(), MulOp.StaticWeakCombine(log.GetInnerTerm(), LogFunction.Ln(log.GetBase())));
            }

            pEvalData.AttemptSetInputType(TermType.InputType.DerivLog);

            if (useChainRule)
            {
                pEvalData.AddInputType(TermType.InputAddType.DerivCR);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", log.GetInnerTerm());
                WorkStep last = pEvalData.GetWorkMgr().GetLast();

                last.GoDown(ref pEvalData);
                ExComp innerDeriv = TakeDerivativeOf(log.GetInnerTerm(), ref pEvalData);
                last.GoUp(ref pEvalData);

                pEvalData.GetWorkMgr().FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                // Apply the chain rule.
                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private ExComp ApplyPowBaseDeriv(PowerFunction powFunc, ref TermType.EvalData pEvalData)
        {
            ExComp finalEx = MulOp.StaticCombine(powFunc.GetPower(), LogFunction.Ln(powFunc.GetBase()));
            pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=({0})*d/(dx)[{1}]`", "This comes from the definition for the derivative of `d/(dx)[x^x]=x^x*d/(dx)[x*ln(x)]`.", powFunc, finalEx);

            ExComp eval = MulOp.StaticCombine(powFunc, TakeDerivativeOf(finalEx, ref pEvalData));
            return eval;
        }

        private ExComp ApplyPower(PowerFunction pfGpCmp, ref TermType.EvalData pEvalData)
        {
            bool powHas = ContainsVarOfInterest(pfGpCmp.GetPower());
            bool baseHas = ContainsVarOfInterest(pfGpCmp.GetBase());

            if (powHas && baseHas)
            {
                ExComp eval = ApplyPowBaseDeriv(pfGpCmp, ref pEvalData);
                return eval;
            }
            else if (powHas)
            {
                ExComp eval = ApplyPowerRulePower(pfGpCmp, ref pEvalData);
                return eval;
            }
            else if (baseHas)
            {
                ExComp eval = ApplyPowerRuleBase(pfGpCmp, ref pEvalData);
                return eval;
            }
            else
                return ConstructDeriv(pfGpCmp, _withRespectTo, _derivOf);
        }

        private ExComp ApplyPowerRuleBase(PowerFunction powFunc, ref TermType.EvalData pEvalData)
        {
            if (powFunc.GetPower().IsEqualTo(ExNumber.GetNegOne()))
            {
                // Don't include the constants which are still contained under the neg one power.
                if (powFunc.GetBase() is AlgebraTerm)
                {
                    List<ExComp[]> gps = (powFunc.GetBase() as AlgebraTerm).GetGroupsNoOps();
                    if (gps.Count == 1)
                    {
                        ExComp[] gp = gps[0];
                        ExComp[] varTo, constTo;
                        GroupHelper.GetConstVarTo(gp, out varTo, out constTo, _withRespectTo);
                        if (varTo.Length != 0 && constTo.Length != 0)
                        {
                            AlgebraTerm agConst = AlgebraTerm.FromFraction(ExNumber.GetOne(), GroupHelper.ToAlgTerm(constTo));
                            AlgebraTerm agVarTo = GroupHelper.ToAlgNoRedunTerm(varTo);
                            if (agVarTo is PowerFunction)
                                (agVarTo as PowerFunction).SetPower(MulOp.Negate((agVarTo as PowerFunction).GetPower()));
                            else
                                agVarTo = new PowerFunction(agVarTo, ExNumber.GetNegOne());

                            pEvalData.GetWorkMgr().FromFormatted("`" + agConst.ToDispString() + ca_derivSymb + "[" + agVarTo.FinalToDispStr() + "]`",
                                "Bring out all constants as they will have no effect on the derivative. This comes from the derivative property that `d/(dx)[kf(x)]=k*d/(dx)[f(x)]` the constants will be multiplied back in at the end.");

                            ExComp deriv = TakeDerivativeOf(agVarTo, ref pEvalData);
                            return MulOp.StaticCombine(agConst, deriv);
                        }
                    }
                }
            }

            ExComp term = powFunc.GetPower().CloneEx();

            ExComp power = SubOp.StaticCombine(powFunc.GetPower(), ExNumber.GetOne());
            if (power is AlgebraTerm)
                power = (power as AlgebraTerm).CompoundFractions();

            PowerFunction derivPowFunc = new PowerFunction(powFunc.GetBase(), power);
            if (derivPowFunc.GetPower().ToAlgTerm().IsOne())
                term = MulOp.StaticCombine(term, powFunc.GetBase());
            else
                term = MulOp.StaticCombine(term, derivPowFunc);

            bool useChainRule = ShouldApplyChainRule(powFunc.GetBase());
            if (useChainRule)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", powFunc);
            }

            pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]={1}`",
                "Through using the power rule which states `d/(dx)[x^n]=nx^(n-1)`.", powFunc, term);

            pEvalData.AttemptSetInputType(TermType.InputType.DerivPoly);

            if (useChainRule)
            {
                pEvalData.AddInputType(TermType.InputAddType.DerivCR);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", powFunc.GetBase());
                WorkStep last = pEvalData.GetWorkMgr().GetLast();

                last.GoDown(ref pEvalData);
                // The chain rule has to be applied here.
                ExComp innerDeriv = TakeDerivativeOf(powFunc.GetBase(), ref pEvalData);
                last.GoUp(ref pEvalData);

                pEvalData.GetWorkMgr().FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, term));

                return MulOp.StaticCombine(innerDeriv, term);
            }
            return term;
        }

        private ExComp ApplyPowerRulePower(PowerFunction powFunc, ref TermType.EvalData pEvalData)
        {
            bool useChainRule = ShouldApplyChainRule(powFunc.GetPower());

            if (useChainRule)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", powFunc);
            }

            ExComp deriv;
            if (powFunc.GetBase() is Constant && (powFunc.GetBase() as Constant).GetVar().GetVar() == "e")
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]={0}`", "The derivative of `e` raised to anything is always the same.", powFunc);
                deriv = powFunc;
            }
            else
            {
                deriv = MulOp.StaticWeakCombine(LogFunction.Ln(powFunc.GetBase()), powFunc);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]={1}`", "Using the exponent rule which states `d/(dx)[a^x]=ln(a)a^x`", powFunc, deriv);
            }

            pEvalData.AttemptSetInputType(TermType.InputType.DerivExp);

            if (useChainRule)
            {
                pEvalData.AddInputType(TermType.InputAddType.DerivCR);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", powFunc.GetPower());
                WorkStep last = pEvalData.GetWorkMgr().GetLast();

                last.GoDown(ref pEvalData);
                // Apply chain rule.
                ExComp innerDeriv = TakeDerivativeOf(powFunc.GetPower(), ref pEvalData);
                last.GoUp(ref pEvalData);

                pEvalData.GetWorkMgr().FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));
                return MulOp.StaticCombine(deriv, innerDeriv);
            }

            return deriv;
        }

        private ExComp ApplyTrigDeriv(TrigFunction trigFunc, ref TermType.EvalData pEvalData)
        {
            bool useChainRule = ShouldApplyChainRule(trigFunc.GetInnerEx());
            if (useChainRule)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", trigFunc);
            }

            ExComp deriv = trigFunc.GetDerivativeOf();
            pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]={1}`",
                "Use the common definition that `d/(dx)[" + trigFunc.GetFuncName() + "(x)]=" + trigFunc.GetDerivativeOfStr() + "`.", trigFunc, deriv);

            pEvalData.AttemptSetInputType(TermType.InputType.DerivTrig);

            if (useChainRule)
            {
                pEvalData.AddInputType(TermType.InputAddType.DerivCR);
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", trigFunc.GetInnerTerm());
                WorkStep lastWorkStep = pEvalData.GetWorkMgr().GetLast();

                lastWorkStep.GoDown(ref pEvalData);
                ExComp innerDeriv = TakeDerivativeOf(trigFunc.GetInnerTerm(), ref pEvalData);
                lastWorkStep.GoUp(ref pEvalData);

                pEvalData.GetWorkMgr().FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                // Apply the chain rule.
                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private bool ContainsVarOfInterest(ExComp ex)
        {
            AlgebraTerm term = ex.ToAlgTerm();
            return term.Contains(_withRespectTo) || (_derivOf != null && term.Contains(_derivOf));
        }

        private bool ShouldApplyChainRule(ExComp ex)
        {
            return !ex.IsEqualTo(_withRespectTo);
        }

        private ExComp TakeDerivativeOf(ExComp ex, ref TermType.EvalData pEvalData)
        {
            if (ex is AlgebraTerm)
                ex = (ex as AlgebraTerm).RemoveRedundancies(false);

            if (ex.ToAlgTerm().IsUndefined())
                return ExNumber.GetUndefined();

            if (ca_derivSymb == null)
            {
                ca_derivSymb = "d/(" + GetNotationIden() + _withRespectTo.ToDispString() + ")";
            }

            pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`",
                _derivOf == null ? "Take the derivative of this expression with respect to `" + _withRespectTo.ToDispString() + "`." :
                "Find the derivative `" + ConstructImplicitDerivAgCmp().ToDispString() + "`", ex);

            if (!ContainsVarOfInterest(ex))
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=0`", "The entire term is constant therefore the derivative equals `0`", ex);
                return ExNumber.GetZero();
            }

            if (ex is AlgebraComp)
            {
                if (_withRespectTo.IsEqualTo(ex))
                {
                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=1`",
                        "Use the power rule.", ex);
                    return ExNumber.GetOne();
                }
                else if (_derivOf != null && _derivOf.IsEqualTo(ex))
                {
                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]=(d{0})/(d{1})`",
                        "As `{1}` is a function of `{0}` rather than the derivative being `(" +
                        GetNotationIden() + "{1})/(" + GetNotationIden() + "{1})=1` it is `(" + GetNotationIden() +
                        "{0})/(" + GetNotationIden() + "{1})`", _derivOf, _withRespectTo);
                    return ConstructImplicitDerivAgCmp();
                }
            }

            if (ex is PowerFunction)
            {
                ExComp evalEx = ApplyPower(ex as PowerFunction, ref pEvalData);
                return evalEx;
            }
            else if (ex is AbsValFunction)
            {
                ExComp evalEx = ApplyAbsDeriv(ex as AbsValFunction, ref pEvalData);
                return evalEx;
            }
            else if (ex is TrigFunction)
            {
                ExComp evalEx = ApplyTrigDeriv(ex as TrigFunction, ref pEvalData);
                return evalEx;
            }
            else if (ex is InverseTrigFunction)
            {
                ExComp evalEx = ApplyInvTrigDeriv(ex as InverseTrigFunction, ref pEvalData);
                return evalEx;
            }
            else if (ex is LogFunction)
            {
                ExComp evalEx = ApplyLogDeriv(ex as LogFunction, ref pEvalData);
                return evalEx;
            }
            else if (ex is AlgebraFunction)
            {
                // An unaccounted for function.
                return ConstructDeriv(ex, _withRespectTo, _derivOf);
            }
            else if (ex is AlgebraTerm)
            {
                // Split it up by the addition signs.
                AlgebraTerm term = ex as AlgebraTerm;
                AlgebraTerm[] numDen = term.GetNumDenFrac();
                if (numDen != null)
                {
                    ExComp numEx = numDen[0].RemoveRedundancies(false);
                    ExComp denEx = numDen[1].RemoveRedundancies(false);

                    if (ExNumber.GetOne().IsEqualTo(numEx) && denEx is PowerFunction)
                    {
                        PowerFunction pfDen = denEx as PowerFunction;
                        pfDen.SetPower(MulOp.Negate(pfDen.GetPower()));

                        ExComp evalEx = ApplyPower(pfDen, ref pEvalData);
                        return evalEx;
                    }
                }

                List<ExComp[]> gps = term.GetGroupsNoOps();
                AlgebraTerm finalAlgTerm = new AlgebraTerm();

                if (gps.Count != 1 && pEvalData.GetWorkMgr().GetAllowWork())
                {
                    string indvDerivs = "";
                    for (int i = 0; i < gps.Count; ++i)
                    {
                        indvDerivs += ca_derivSymb + "[" + GroupHelper.FinalToMathAsciiString(gps[i]) + "]";
                        if (i != gps.Count - 1)
                            indvDerivs += "+";
                    }

                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[" + WorkMgr.ToDisp(ex) + "]=" + indvDerivs + "`",
                        "Split the derivative up using the property of derivatives that `d/(dx)[f(x)+g(x)]=f'(x)+g'(x)`");
                }

                for (int i = 0; i < gps.Count; ++i)
                {
                    ExComp deriv = TakeDerivativeOfGp(gps[i], ref pEvalData);
                    if (deriv.ToAlgTerm().IsZero())
                        continue;
                    finalAlgTerm.Add(deriv);
                    if (i != gps.Count - 1)
                        finalAlgTerm.Add(new AddOp());
                }

                return finalAlgTerm;
            }

            // Whatever this is, the derivative of it can't be taken.
            return ConstructDeriv(ex, _withRespectTo, _derivOf);
        }

        private ExComp TakeDerivativeOfGp(ExComp[] gp, ref TermType.EvalData pEvalData)
        {
            if (_withRespectTo == null)
                return Derivative.ConstructDeriv(GroupHelper.ToAlgTerm(gp), _withRespectTo, _derivOf);

            ExComp[] varTo, constTo;
            if (_derivOf == null)
                GroupHelper.GetConstVarTo(gp, out varTo, out constTo, _withRespectTo);
            else
                GroupHelper.GetConstVarTo(gp, out varTo, out constTo, _withRespectTo, _derivOf);

            if (constTo.Length == 1 && constTo[0].IsEqualTo(ExNumber.GetOne()))
            {
                ExComp[] empty = new ExComp[] { };
                constTo = empty;
            }

            if (varTo.Length == 0)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[" + GroupHelper.FinalToMathAsciiString(gp) + "]=0`", "The entire term is constant therefore the derivative equals `0`");
                return ExNumber.GetZero();
            }

            string varToStr = GroupHelper.ToAlgTerm(varTo).ToAsciiString();

            ExComp derivTerm = null;

            if (constTo.Length != 0)
            {
                pEvalData.GetWorkMgr().FromFormatted("`" + GroupHelper.ToAlgTerm(constTo).ToDispString() + ca_derivSymb + "[" + varToStr + "]`",
                    "Bring out all constants as they will have no effect on the derivative. This comes from the derivative property that `d/(dx)[kf(x)]=k*d/(dx)[f(x)]` the constants will be multiplied back in at the end.");
            }

            if (varTo.Length == 1)
            {
                ExComp gpCmp = varTo[0];

                derivTerm = TakeDerivativeOf(gpCmp, ref pEvalData);
            }
            else
            {
                ExComp[] num = GroupHelper.GetNumerator(varTo);
                ExComp[] den = GroupHelper.GetDenominator(varTo, false);

                if (den != null && den.Length > 0)
                {
                    ExComp numEx = GroupHelper.ToAlgTerm(num);
                    ExComp denEx = GroupHelper.ToAlgTerm(den);

                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[" + varToStr + "]`",
                        "As the above is a fraction use the quotient rule which states `d/(dx)[u/v]=(u'v-uv')/(v^2)`. In this case `u=" + numEx.ToDispString() + "`, `v=" + denEx.ToDispString() + "`");

                    // Use the quotient rule.
                    pEvalData.GetWorkMgr().FromFormatted("",
                        "First find the derivative of the numerator.");
                    WorkStep last0 = pEvalData.GetWorkMgr().GetLast();

                    last0.GoDown(ref pEvalData);
                    ExComp numDeriv = TakeDerivativeOfGp(num, ref pEvalData);
                    last0.GoUp(ref pEvalData);

                    last0.SetWorkHtml(WorkMgr.STM + ca_derivSymb + "[" + GroupHelper.ToAlgTerm(num).FinalToDispStr() + "]=" + WorkMgr.ToDisp(numDeriv) + WorkMgr.EDM);

                    pEvalData.GetWorkMgr().FromFormatted("",
                        "Find the derivative of the denominator.");
                    WorkStep last1 = pEvalData.GetWorkMgr().GetLast();

                    last1.GoDown(ref pEvalData);
                    ExComp denDeriv = TakeDerivativeOfGp(den, ref pEvalData);
                    last1.GoUp(ref pEvalData);

                    last1.SetWorkHtml(WorkMgr.STM + ca_derivSymb + "[" + GroupHelper.ToAlgTerm(den).FinalToDispStr() + "]=" + WorkMgr.ToDisp(denDeriv) + WorkMgr.EDM);

                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[" + varToStr + "]=(({0})({1})-({2})({3}))/(({1})^2)`",
                        "Plug the values back into the equation for the quotient rule `d/(dx)[u/v]=(u'v-uv')/(v^2)`. In the above case `u={2}`, `u'={0}`, `v={1}`, `v'={3}`", numDeriv, denEx, numEx, denDeriv);

                    ExComp tmpMul0 = MulOp.StaticCombine(numDeriv, denEx.CloneEx());
                    ExComp tmpMul1 = MulOp.StaticCombine(denDeriv.CloneEx(), numEx);

                    ExComp tmpNum = SubOp.StaticCombine(tmpMul0, tmpMul1);
                    ExComp tmpDen = PowOp.StaticCombine(denEx, new ExNumber(2.0));

                    derivTerm = DivOp.StaticCombine(tmpNum, tmpDen);
                }
                else
                {
                    ExComp u = num[0];
                    ExComp v = GroupHelper.ToAlgTerm(ArrayFunc.ToList(num).GetRange(1, num.Length - 1).ToArray());

                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[" + GroupHelper.ToAsciiString(num) + "]`",
                        "Apply the product rule which states `d/(dx)[u*v]=u'v+uv'` in this case `u={0}`, `v={1}`", u, v);

                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Calculate `u'` for the product rule.", u);
                    WorkStep last0 = pEvalData.GetWorkMgr().GetLast();

                    last0.GoDown(ref pEvalData);
                    ExComp uDeriv = TakeDerivativeOf(u, ref pEvalData);
                    last0.GoUp(ref pEvalData);

                    pEvalData.GetWorkMgr().FromFormatted("`" + ca_derivSymb + "[{0}]`", "Calculate `v'` for the product rule.", v);
                    WorkStep last1 = pEvalData.GetWorkMgr().GetLast();

                    last1.GoDown(ref pEvalData);
                    ExComp vDeriv = TakeDerivativeOf(v, ref pEvalData);
                    last1.GoUp(ref pEvalData);

                    derivTerm = AddOp.StaticCombine(MulOp.StaticCombine(uDeriv, v), MulOp.StaticCombine(vDeriv, u));
                }
            }

            if (derivTerm == null)
                return Derivative.ConstructDeriv(GroupHelper.ToAlgTerm(gp), _withRespectTo, _derivOf);

            if (constTo.Length == 0)
                return derivTerm;
            ExComp constToEx = GroupHelper.ToAlgTerm(constTo);

            pEvalData.GetWorkMgr().FromFormatted("`{0}*" + ca_derivSymb + "[" + GroupHelper.ToAsciiString(varTo) + "]={0}*{1}`", "Multiply back in the constants.", constToEx, derivTerm);

            return MulOp.StaticCombine(constToEx, derivTerm);
        }
    }
}