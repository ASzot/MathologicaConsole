using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class Derivative : AppliedFunction
    {
        private const int MAX_DERIV = 3;

        private AlgebraComp _derivOf = null;
        private bool _isDefined = true;
        private int _order = 1;
        private AlgebraComp _withRespectTo;
        private string ca_derivSymb = null;
        private AlgebraComp ca_impDeriv = null;

        public AlgebraComp WithRespectTo
        {
            get { return _withRespectTo; }
            set
            {
                _withRespectTo = value;
            }
        }

        public Derivative(ExComp innerEx)
            : base(innerEx, FunctionType.Derivative, typeof(Derivative))
        {
        }

        public static Derivative CreateDeriv(ExComp innerEx, AlgebraComp withRespect, AlgebraComp derivOf)
        {
            Derivative deriv = new Derivative(innerEx);
            deriv._withRespectTo = withRespect;
            deriv._derivOf = derivOf;

            return deriv;
        }

        public static Derivative Parse(string function, string withRespectTo, int order, ref TermType.EvalData pEvalData)
        {
            if (order < 1)
                order = 1;

            if (withRespectTo.Length == 0)
                return null;

            AlgebraComp respectToCmp = new AlgebraComp(withRespectTo);

            FunctionDefinition funcDef = new FunctionDefinition(new AlgebraComp(function), null, null);

            ExComp innerEx = pEvalData.FuncDefs.GetDefinition(funcDef).Value;
            Derivative deriv;
            if (innerEx == null)
            {
                deriv = new Derivative(Number.Zero);
                deriv._isDefined = false;
                deriv._derivOf = new AlgebraComp(function);
            }
            else
            {
                deriv = new Derivative(innerEx);
            }

            deriv.WithRespectTo = respectToCmp;
            deriv._order = order;

            return deriv;
        }

        public static Derivative Parse(string withRespectTo, ExComp inner, int order)
        {
            if (order < 1)
                order = 1;

            if (withRespectTo.Length == 0)
                return null;

            Derivative deriv = new Derivative(inner);
            deriv.WithRespectTo = new AlgebraComp(withRespectTo);
            deriv._order = order;

            return deriv;
        }

        public override ExComp Clone()
        {
            Derivative deriv = new Derivative(InnerEx.Clone());
            deriv._withRespectTo = this._withRespectTo;
            deriv._order = this._order;
            deriv._derivOf = this._derivOf;
            deriv._isDefined = this._isDefined;
            return deriv;
        }

        public AlgebraComp ConstructImplicitDerivAgCmp()
        {
            if (ca_impDeriv == null)
                ca_impDeriv = new AlgebraComp("(d" + _derivOf.ToDispString() + ")/(d" + _withRespectTo.ToDispString() + ")");
            return ca_impDeriv;
        }

        /// <summary>
        /// Harsh evaluations have no importance here as derivatives are always symbolic.
        /// </summary>
        /// <param name="harshEval">Value doesn't matter.</param>
        /// <returns></returns>
        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (!_isDefined)
                return this;

            if (_order > MAX_DERIV)
                return this;

            ca_derivSymb = "d/(d" + _withRespectTo.ToDispString() + ")";

            ExComp final = InnerEx;
            pEvalData.WorkMgr.FromFormatted("`{0}`", "Find the " + (_order).ToString() + MathHelper.GetCountingPrefix(_order) + " derivative of the above.", InnerEx);
            for (int i = 0; i < _order; ++i)
            {
                ExComp tmp = TakeDerivativeOf(final, ref pEvalData);
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]={1}`", "The final " + (i + 1).ToString() + MathHelper.GetCountingPrefix(i + 1) + " derivative.", final, tmp);
                final = tmp;
            }

            return final;
        }

        public override string FinalDispKeepFormatting()
        {
            if (USE_TEX)
                return FinalToTexKeepFormatting();
            return FinalToAsciiKeepFormatting();
        }

        public override string FinalToAsciiKeepFormatting()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();

            if (!_isDefined)
            {
                return "(d" + orderStr + _derivOf.ToMathAsciiString() + ")/(d" + _withRespectTo.ToMathAsciiString() + orderStr + ")";
            }

            return "(d" + orderStr + ")/(d" + _withRespectTo.ToMathAsciiString() + orderStr + ")[" +
                (InnerEx is AlgebraTerm ? (InnerEx as AlgebraTerm).FinalToAsciiKeepFormatting() : InnerEx.ToMathAsciiString()) + "]";
        }

        public override string FinalToAsciiString()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "(d" + orderStr + _derivOf.ToMathAsciiString() + ")/(d" + _withRespectTo.ToMathAsciiString() + orderStr + ")";
            }
            return "(d" + orderStr + ")/(d" + _withRespectTo.ToMathAsciiString() + orderStr + ")[" +
                (InnerEx is AlgebraTerm ? (InnerEx as AlgebraTerm).FinalToAsciiString() : InnerEx.ToMathAsciiString()) + "]";
        }

        public override string FinalToDispStr()
        {
            if (USE_TEX)
                return FinalToTexString();
            return FinalToAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "\\frac{d" + orderStr + _derivOf.ToTexString() + "}{d" + _withRespectTo.ToTexString() + orderStr + "}";
            }
            return "\\frac{d" + orderStr + "}{d" + _withRespectTo.ToTexString() + orderStr + "}[" +
                (InnerEx is AlgebraTerm ? (InnerEx as AlgebraTerm).FinalToTexKeepFormatting() : InnerEx.ToTexString()) + "]";
        }

        public override string FinalToTexString()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "\\frac{d" + orderStr + _derivOf.ToTexString() + "}{d" + _withRespectTo.ToTexString() + orderStr + "}";
            }
            return "\\frac{d" + orderStr + "}{d" + _withRespectTo.ToTexString() + orderStr + "}[" +
                (InnerEx is AlgebraTerm ? (InnerEx as AlgebraTerm).FinalToTexString() : InnerEx.ToTexString()) + "]";
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is Derivative)
            {
                Derivative deriv = ex as Derivative;

                return deriv.InnerEx.IsEqualTo(InnerEx) &&
                    ((deriv._withRespectTo != null && deriv._withRespectTo.IsEqualTo(this._withRespectTo)) ||
                    (deriv._withRespectTo == null && this._withRespectTo == null)) &&
                    ((deriv._derivOf != null && deriv._derivOf.IsEqualTo(this._derivOf)) ||
                    (deriv._derivOf == null && this._derivOf == null)) &&
                    deriv._order == _order;
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
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "((d" + orderStr + _derivOf.ToMathAsciiString() + ")/(d" + _withRespectTo.ToMathAsciiString() + orderStr + "))";
            }
            return "((d" + orderStr + ")/(d" + _withRespectTo.ToMathAsciiString() + orderStr + ")[" +
                InnerEx.ToMathAsciiString() + "])";
        }

        public override string ToSearchString()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "(d" + orderStr + _derivOf.ToSearchString() + ")/(d" + _withRespectTo.ToSearchString() + orderStr + ")";
            }
            return "(d" + orderStr + ")/(d" + _withRespectTo.ToSearchString() + orderStr + ")[" +
                InnerEx.ToSearchString() + "]";
        }

        public override string ToString()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "((d" + orderStr + _derivOf.ToTexString() + ")/(d" + _withRespectTo.ToTexString() + orderStr + "))";
            }
            return "((d" + orderStr + ")/(d" + _withRespectTo.ToTexString() + orderStr + ")[" +
                InnerEx.ToString() + "])";
        }

        public override string ToTexString()
        {
            string orderStr = _order == 1 ? "" : "^" + _order.ToString();
            if (!_isDefined)
            {
                return "((d" + orderStr + _derivOf.ToTexString() + ")/(d" + _withRespectTo.ToTexString() + orderStr + "))";
            }
            return "((d" + orderStr + ")/(d" + _withRespectTo.ToTexString() + orderStr + ")[" +
                InnerEx.ToTexString() + "])";
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            Derivative deriv = new Derivative(args[0]);
            deriv._withRespectTo = (AlgebraComp)this._withRespectTo.Clone();
            deriv._order = this._order;
            deriv._isDefined = this._isDefined;
            deriv._derivOf = this._derivOf;
            return deriv;
        }

        private ExComp ApplyAbsDeriv(AbsValFunction abs, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = abs.InnerEx;
            bool useChainRule = ShouldApplyChainRule(innerEx);
            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", abs);
            }

            pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=({0})/({1})`", "From the common derivative of the absolute value function.", abs.InnerTerm, abs);

            ExComp deriv = DivOp.StaticCombine(innerEx, abs);

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", abs.InnerTerm);

                ExComp innerDeriv = TakeDerivativeOf(abs.InnerTerm, ref pEvalData);

                pEvalData.WorkMgr.FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                // Apply the chain rule.
                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private ExComp ApplyInvTrigDeriv(InverseTrigFunction invTrigFunc, ref TermType.EvalData pEvalData)
        {
            bool useChainRule = ShouldApplyChainRule(invTrigFunc.InnerEx);
            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", invTrigFunc);
            }

            ExComp deriv = invTrigFunc.GetDerivativeOf();
            pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]={1}`",
                "Use the common definition that `d/(dx)[" + invTrigFunc.FuncName + "(x)]=" + invTrigFunc.GetDerivativeOfStr() + "`.", invTrigFunc, deriv);

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", invTrigFunc.InnerTerm);

                // Apply the chain rule.
                ExComp innerDeriv = TakeDerivativeOf(invTrigFunc.InnerTerm, ref pEvalData);

                pEvalData.WorkMgr.FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private ExComp ApplyLogDeriv(LogFunction log, ref TermType.EvalData pEvalData)
        {
            if (log.Base.ToAlgTerm().Contains(_withRespectTo))
            {
                // Terms in the form log_x(y) cannot have the derivative be taken with respect to x.
                return CreateDeriv(log, _withRespectTo, _derivOf);
            }

            bool useChainRule = ShouldApplyChainRule(log.InnerEx);
            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", log);
            }

            ExComp deriv;
            if (log.Base.IsEqualTo(Constant.E))
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=1/({1})`",
                    "Using the definition that `d/(dx)[log_b(x)]=1/(ln(b)x)` which can be extended to natural logs to say `d/(dx)[ln(x)]=1/x`",
                    log, log.InnerTerm);
                deriv = DivOp.StaticCombine(Number.One, log.InnerTerm);
            }
            else
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=1/(ln({2})({1}))`",
                    "Using the definition that `d/(dx)[log_b(x)]=1/(ln(b)x)`",
                    log, log.InnerTerm, log.Base);
                deriv = DivOp.StaticCombine(Number.One, MulOp.StaticWeakCombine(log.InnerTerm, LogFunction.Ln(log.Base)));
            }

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", log.InnerTerm);

                ExComp innerDeriv = TakeDerivativeOf(log.InnerTerm, ref pEvalData);

                pEvalData.WorkMgr.FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));

                // Apply the chain rule.
                return MulOp.StaticCombine(innerDeriv, deriv);
            }

            return deriv;
        }

        private ExComp ApplyPowBaseDeriv(PowerFunction powFunc, ref TermType.EvalData pEvalData)
        {
            ExComp final = MulOp.StaticCombine(powFunc.Power, LogFunction.Ln(powFunc.Base));
            pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=({0})*d/(dx)[{1}]`", "This comes from the definition for the derivative of `d/(dx)[x^x]=x^x*d/(dx)[x*ln(x)]`.", powFunc, final);

            return MulOp.StaticCombine(powFunc, TakeDerivativeOf(final, ref pEvalData));
        }

        private ExComp ApplyPowerRuleBase(PowerFunction powFunc, ref TermType.EvalData pEvalData)
        {
            if (powFunc.Power.IsEqualTo(Number.NegOne))
            {
                // Don't include the constants which are still contained under the neg one power.
                if (powFunc.Base is AlgebraTerm)
                {
                    var gps = (powFunc.Base as AlgebraTerm).GetGroupsNoOps();
                    if (gps.Count == 1)
                    {
                        var gp = gps[0];
                        ExComp[] varTo, constTo;
                        gp.GetConstVarTo(out varTo, out constTo, _withRespectTo);
                        if (varTo.Length != 0 && constTo.Length != 0)
                        {
                            AlgebraTerm agConst = AlgebraTerm.FromFraction(Number.One, constTo.ToAlgTerm());
                            AlgebraTerm agVarTo = varTo.ToAlgNoRedunTerm();
                            if (agVarTo is PowerFunction)
                                (agVarTo as PowerFunction).Power = MulOp.Negate((agVarTo as PowerFunction).Power);
                            else
                                agVarTo = new PowerFunction(agVarTo, Number.NegOne);

                            pEvalData.WorkMgr.FromFormatted("`" + agConst.ToDispString() + ca_derivSymb + "[" + agVarTo.FinalToDispStr() + "]`",
                                "Bring out all constants as they will have no effect on the derivative. This comes from the derivative property that `d/(dx)[kf(x)]=k*d/(dx)[f(x)]` the constants will be multiplied back in at the end.");

                            ExComp deriv = TakeDerivativeOf(agVarTo, ref pEvalData);
                            return MulOp.StaticCombine(agConst, deriv);
                        }
                    }
                }
            }

            ExComp term = powFunc.Power.Clone();

            ExComp power = SubOp.StaticCombine(powFunc.Power, Number.One);
            if (power is AlgebraTerm)
                power = (power as AlgebraTerm).CompoundFractions();

            PowerFunction derivPowFunc = new PowerFunction(powFunc.Base, power);
            if (derivPowFunc.Power.ToAlgTerm().IsOne())
                term = MulOp.StaticCombine(term, powFunc.Base);
            else
                term = MulOp.StaticCombine(term, derivPowFunc);

            bool useChainRule = ShouldApplyChainRule(powFunc.Base);
            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", powFunc);
            }

            pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]={1}`",
                "Through using the power rule which states `d/(dx)[x^n]=nx^(n-1)`.", powFunc, term);

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", powFunc.Base);
                // The chain rule has to be applied here.
                ExComp innerDeriv = TakeDerivativeOf(powFunc.Base, ref pEvalData);

                pEvalData.WorkMgr.FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, term));

                return MulOp.StaticCombine(innerDeriv, term);
            }
            return term;
        }

        private ExComp ApplyPowerRulePower(PowerFunction powFunc, ref TermType.EvalData pEvalData)
        {
            bool useChainRule = ShouldApplyChainRule(powFunc.Power);

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", powFunc);
            }

            ExComp deriv;
            if (powFunc.Base is Constant && (powFunc.Base as Constant).Var.Var == "e")
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]={0}`", "The derivative of `e` raised to anything is always the same.", powFunc);
                deriv = powFunc;
            }
            else
            {
                deriv = MulOp.StaticWeakCombine(LogFunction.Ln(powFunc.Base), powFunc);
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]={1}`", "Using the exponent rule which states `d/(dx)[a^x]=ln(a)a^x`", powFunc, deriv);
            }

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", powFunc.Power);

                // Apply chain rule.
                ExComp innerDeriv = TakeDerivativeOf(powFunc.Power, ref pEvalData);

                pEvalData.WorkMgr.FromFormatted("`{0}`",
                    "According to the chain rule multiply the derivative of the inner function by the derivative of the outer function.",
                    MulOp.StaticWeakCombine(innerDeriv, deriv));
                return MulOp.StaticCombine(deriv, innerDeriv);
            }

            return deriv;
        }

        private ExComp ApplyTrigDeriv(TrigFunction trigFunc, ref TermType.EvalData pEvalData)
        {
            bool useChainRule = ShouldApplyChainRule(trigFunc.InnerEx);
            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                    "The above is a composition of functions `f(g(" + _withRespectTo.ToDispString() +
                    "))`. Use the chain rule which states `d/(dx)[f(g(x))]=f'(g(x))*g'(x)` First take the derivative of the outside function.", trigFunc);
            }

            ExComp deriv = trigFunc.GetDerivativeOf();
            pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]={1}`",
                "Use the common definition that `d/(dx)[" + trigFunc.FuncName + "(x)]=" + trigFunc.GetDerivativeOfStr() + "`.", trigFunc, deriv);

            if (useChainRule)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Next take the derivative of the inner function.", trigFunc.InnerTerm);

                ExComp innerDeriv = TakeDerivativeOf(trigFunc.InnerTerm, ref pEvalData);

                pEvalData.WorkMgr.FromFormatted("`{0}`",
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
                ex = (ex as AlgebraTerm).RemoveRedundancies();

            if (ca_derivSymb == null)
            {
                ca_derivSymb = "d/(d" + _withRespectTo.ToDispString() + ")";
            }

            pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`",
                _derivOf == null ? "Take the derivative of this expression with respect to `" + _withRespectTo.ToDispString() + "`." :
                "Find the derivative `" + ConstructImplicitDerivAgCmp().ToDispString() + "`", ex);

            if (!ContainsVarOfInterest(ex))
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=0`", "The entire term is constant therefore the derivative equals `0`", ex);
                return Number.Zero;
            }

            if (ex is AlgebraComp)
            {
                if (_withRespectTo.IsEqualTo(ex))
                {
                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=1`",
                        "Using the power rule the above is one. This is because `{0}` has an exponent of `1` and by the power rule `(d)/(d{0})[{0}^1]=1*{0}^(0)=1`", ex);
                    return Number.One;
                }
                else if (_derivOf != null && _derivOf.IsEqualTo(ex))
                {
                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]=(d{0})/(d{1})`",
                        "As `{1}` is a function of `{0}` rather than the derivative being `(d{1})/(d{1})=1` it is `(d{0})/(d{1})`", _derivOf, _withRespectTo);
                    return ConstructImplicitDerivAgCmp();
                }
            }

            if (ex is PowerFunction)
            {
                PowerFunction pfGpCmp = ex as PowerFunction;
                bool powHas = ContainsVarOfInterest(pfGpCmp.Power);
                bool baseHas = ContainsVarOfInterest(pfGpCmp.Base);

                if (powHas && baseHas)
                {
                    return ApplyPowBaseDeriv(pfGpCmp, ref pEvalData);
                }
                else if (powHas)
                {
                    return ApplyPowerRulePower(pfGpCmp, ref pEvalData);
                }
                else if (baseHas)
                {
                    return ApplyPowerRuleBase(pfGpCmp, ref pEvalData);
                }
            }
            else if (ex is AbsValFunction)
            {
                return ApplyAbsDeriv(ex as AbsValFunction, ref pEvalData);
            }
            else if (ex is TrigFunction)
            {
                return ApplyTrigDeriv(ex as TrigFunction, ref pEvalData);
            }
            else if (ex is InverseTrigFunction)
            {
                return ApplyInvTrigDeriv(ex as InverseTrigFunction, ref pEvalData);
            }
            else if (ex is LogFunction)
            {
                return ApplyLogDeriv(ex as LogFunction, ref pEvalData);
            }
            else if (ex is AlgebraFunction)
            {
                // An unaccounted for function.
                return CreateDeriv(ex, _withRespectTo, _derivOf);
            }
            else if (ex is AlgebraTerm)
            {
                // Split it up by the addition signs.
                AlgebraTerm term = ex as AlgebraTerm;
                var gps = term.GetGroupsNoOps();
                AlgebraTerm final = new AlgebraTerm();

                if (gps.Count != 1 && pEvalData.WorkMgr.AllowWork)
                {
                    string indvDerivs = "";
                    for (int i = 0; i < gps.Count; ++i)
                    {
                        indvDerivs += ca_derivSymb + "[" + gps[i].FinalToMathAsciiString() + "]";
                        if (i != gps.Count - 1)
                            indvDerivs += "+";
                    }

                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + WorkMgr.ExFinalToAsciiStr(ex) + "]=" + indvDerivs + "`",
                        "Split the derivative up using the property of derivatives that `d/(dx)[f(x)+g(x)]=f'(x)+g'(x)`");
                }

                for (int i = 0; i < gps.Count; ++i)
                {
                    ExComp deriv = TakeDerivativeOfGp(gps[i], ref pEvalData);
                    if (deriv.ToAlgTerm().IsZero())
                        continue;
                    final.Add(deriv);
                    if (i != gps.Count - 1)
                        final.Add(new AddOp());
                }

                return final;
            }

            // Whatever this is, the derivative of it can't be taken.
            return CreateDeriv(ex, _withRespectTo, _derivOf);
        }

        private ExComp TakeDerivativeOfGp(ExComp[] gp, ref TermType.EvalData pEvalData)
        {
            if (_withRespectTo == null)
                return Derivative.CreateDeriv(gp.ToAlgTerm(), _withRespectTo, _derivOf);

            ExComp[] varTo, constTo;
            if (_derivOf == null)
                gp.GetConstVarTo(out varTo, out constTo, _withRespectTo);
            else
                gp.GetConstVarTo(out varTo, out constTo, _withRespectTo, _derivOf);

            if (constTo.Length == 1 && constTo[0].IsEqualTo(Number.One))
            {
                ExComp[] empty = { };
                constTo = empty;
            }

            if (varTo.Length == 0)
            {
                pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + gp.FinalToMathAsciiString() + "]=0`", "The entire term is constant therefore the derivative equals `0`");
                return Number.Zero;
            }

            string varToStr = varTo.ToAlgTerm().ToMathAsciiString();

            ExComp derivTerm = null;

            if (constTo.Length != 0)
            {
                pEvalData.WorkMgr.FromFormatted("`" + constTo.ToAlgTerm().ToDispString() + ca_derivSymb + "[" + varToStr + "]`",
                    "Bring out all constants as they will have no effect on the derivative. This comes from the derivative property that `d/(dx)[kf(x)]=k*d/(dx)[f(x)]` the constants will be multiplied back in at the end.");
            }

            if (varTo.Length == 1)
            {
                ExComp gpCmp = varTo[0];

                derivTerm = TakeDerivativeOf(gpCmp, ref pEvalData);
            }
            else
            {
                ExComp[] num = varTo.GetNumerator();
                ExComp[] den = varTo.GetDenominator();

                if (den != null && den.Length > 0)
                {
                    ExComp numEx = num.ToAlgTerm();
                    ExComp denEx = den.ToAlgTerm();

                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + varToStr + "]`",
                        "As the above is a fraction use the quotient rule which states `d/(dx)[u/v]=(u'v-uv')/(v^2)`. In this case `u=" + numEx.ToDispString() + "`, `v=" + denEx.ToDispString() + "`");

                    // Use the quotient rule.
                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + varToStr + "]`",
                        "First find the derivative of the numerator.");
                    ExComp numDeriv = TakeDerivativeOfGp(num, ref pEvalData);

                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + varToStr + "]`",
                        "Find the derivative of the denominator.");
                    ExComp denDeriv = TakeDerivativeOfGp(den, ref pEvalData);

                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + varToStr + "]=(({0})({1})-({2})({3}))/(({1})^2)`",
                        "Plug the values back into the equation for the quotient rule `d/(dx)[u/v]=(u'v-uv')/(v^2)`. In the above case `u={2}`, `u'={0}`, `v={1}`, `v'={3}`", numDeriv, denEx, numEx, denDeriv);

                    ExComp tmpMul0 = MulOp.StaticCombine(numDeriv, denEx.Clone());
                    ExComp tmpMul1 = MulOp.StaticCombine(denDeriv.Clone(), numEx);

                    ExComp tmpNum = SubOp.StaticCombine(tmpMul0, tmpMul1);
                    ExComp tmpDen = PowOp.StaticCombine(denEx, new Number(2.0));

                    derivTerm = DivOp.StaticCombine(tmpNum, tmpDen);
                }
                else
                {
                    ExComp u = num[0];
                    ExComp v = num.ToList().GetRange(1, num.Length - 1).ToArray().ToAlgTerm();

                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[" + num.ToMathAsciiString() + "]`",
                        "Apply the product rule which states `d/(dx)[u*v]=u'v+uv'` in this case `u={0}`, `v={1}`", u, v);

                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Calculate `u'` for the product rule.", u);
                    ExComp uDeriv = TakeDerivativeOf(u, ref pEvalData);
                    pEvalData.WorkMgr.FromFormatted("`" + ca_derivSymb + "[{0}]`", "Calculate `v'` for the product rule.", v);
                    ExComp vDeriv = TakeDerivativeOf(v, ref pEvalData);

                    derivTerm = AddOp.StaticCombine(MulOp.StaticCombine(uDeriv, v), MulOp.StaticCombine(vDeriv, u));
                }
            }

            if (derivTerm == null)
                return Derivative.CreateDeriv(gp.ToAlgTerm(), _withRespectTo, _derivOf);

            if (constTo.Length == 0)
                return derivTerm;
            ExComp constToEx = constTo.ToAlgTerm();

            pEvalData.WorkMgr.FromFormatted("`{0}*" + ca_derivSymb + "[" + varTo.ToMathAsciiString() + "]={0}*{1}`", "Multiply back in the constants.", constToEx, derivTerm);

            return MulOp.StaticCombine(constToEx, derivTerm);
        }
    }
}