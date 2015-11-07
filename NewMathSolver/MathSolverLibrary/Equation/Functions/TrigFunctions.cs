using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Information_Helpers;
using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class ACosFunction : InverseTrigFunction
    {
        private bool b_skipDomain = false;

        public bool SkipDomain
        {
            set { b_skipDomain = value; }
        }

        public ACosFunction(ExComp innerEx)
            : base(innerEx, "arccos", FunctionType.Sinusodal, typeof(ACosFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (!b_skipDomain && !InStandardDomain(ref pEvalData))
                return ExNumber.GetUndefined();

            ExComp innerEx = GetInnerEx();
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).MakeFormattingCorrect();
            UnitCirclePoint point = UnitCircle.GetAngleForPoint_X(innerEx);

            if (point != null)
            {
                return TrigFunction.FinalEvalAngle(point.GetAngle(), pEvalData.GetUseRad());
            }

            if (!(innerEx is ExNumber))
                return this;

            ExNumber nInnerEx = innerEx as ExNumber;

            if (nInnerEx.HasImaginaryComp())
                return this;

            double dInner = nInnerEx.GetRealComp();

            double dACos = Math.Acos(dInner);

            if (harshEval)
            {
                return TrigFunction.FinalEvalAngle(new ExNumber(dACos), pEvalData.GetUseRad());
            }

            if (DoubleHelper.IsInteger(dACos))
                return TrigFunction.FinalEvalAngle(new ExNumber(dACos), pEvalData.GetUseRad());

            return this;
        }

        public static ExComp CreateDerivativeOf(ExComp ex)
        {
            return MulOp.Negate(AlgebraTerm.FromFraction(
                ExNumber.GetOne(),
                PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(ExNumber.GetOne(), MulOp.Negate(PowOp.StaticWeakCombine(ex, new ExNumber(2.0)))))));
        }

        public override ExComp GetDerivativeOf()
        {
            return CreateDerivativeOf(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "-(1)/(sqrt(1-x^2))";
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "The domain of " + WorkMgr.STM + "\\arccos(x)" +
                WorkMgr.EDM + " is " + WorkMgr.STM + "-1 \\le x \\le 1" + WorkMgr.EDM);

            AlgebraComp varForCmp = varFor.ToAlgebraComp();

            ExComp lower = ExNumber.GetNegOne();
            ExComp upper = ExNumber.GetOne();

            if (!GetInnerEx().IsEqualTo(varForCmp))
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "As the inside of " + WorkMgr.STM + "\\arccos" +
                    WorkMgr.EDM + " is not " + WorkMgr.STM + varForCmp.ToDispString() + WorkMgr.EDM + " solve for the domain.");

                List<ExComp> sides = new List<ExComp>();
                sides.Add(lower);
                sides.Add(GetInnerTerm());
                sides.Add(upper);

                List<Parsing.LexemeType> comparisons = new List<Parsing.LexemeType>();
                comparisons.Add(Parsing.LexemeType.LessEqual);
                comparisons.Add(Parsing.LexemeType.LessEqual);

                SolveResult result = agSolver.SolveEquationInequality(sides, comparisons, varFor, ref pEvalData);
                if (!result.Success)
                    return null;

                return result.Restrictions;
            }

            List<Restriction> rests = new List<Restriction>();

            rests.Add(new AndRestriction(lower, Parsing.LexemeType.LessEqual, varForCmp, Parsing.LexemeType.LessEqual,
                upper, ref pEvalData));
            return rests;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerTermStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerTermStr == null)
                return null;
            return "Math.acos(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerTermStr + ")";
        }
    }

    internal class ACotFunction : InverseTrigFunction
    {
        public ACotFunction(ExComp innerEx)
            : base(innerEx, "arccot", FunctionType.Sinusodal, typeof(ACotFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (ExNumber.GetZero().IsEqualTo(GetInnerEx()))
            {
                return AlgebraTerm.FromFraction(Constant.GetPi(), new ExNumber(2.0));
            }

            AlgebraTerm recipInner = AlgebraTerm.FromFraction(ExNumber.GetOne(), GetInnerEx());
            recipInner = recipInner.MakeFormattingCorrect();

            ATanFunction asin = new ATanFunction(recipInner.RemoveRedundancies(false));
            ExComp evalASin = asin.Evaluate(harshEval, ref pEvalData);
            return evalASin;
        }

        public static ExComp CreateDerivativeOf(ExComp ex)
        {
            return MulOp.Negate(AlgebraTerm.FromFraction(
                ExNumber.GetOne(),
                AddOp.StaticWeakCombine(ExNumber.GetOne(), PowOp.StaticWeakCombine(ex, new ExNumber(2.0)))));
        }

        public override ExComp GetDerivativeOf()
        {
            return CreateDerivativeOf(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "-(1)/(1+x^2)";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerTermStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerTermStr == null)
                return null;
            return "(1.0/Math.atan(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerTermStr + "))";
        }
    }

    internal class ACscFunction : InverseTrigFunction
    {
        public ACscFunction(ExComp innerEx)
            : base(innerEx, "arccsc", FunctionType.Sinusodal, typeof(ACscFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (InStandardDomain(ref pEvalData) && GetInnerEx() is ExNumber)
                return ExNumber.GetUndefined();

            AlgebraTerm recipInner = AlgebraTerm.FromFraction(ExNumber.GetOne(), GetInnerEx());
            recipInner = recipInner.MakeFormattingCorrect();

            ASinFunction asin = new ASinFunction(recipInner.RemoveRedundancies(false));
            asin.SkipDomain = true;
            ExComp evalASin = asin.Evaluate(harshEval, ref pEvalData);
            return evalASin;
        }

        public static ExComp CreateDerivativeOf(ExComp ex)
        {
            return MulOp.Negate(
                AlgebraTerm.FromFraction(
                ExNumber.GetOne(),
                MulOp.StaticWeakCombine(new AbsValFunction(ex), PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(PowOp.StaticWeakCombine(ex, new ExNumber(2.0)), MulOp.Negate(ExNumber.GetOne())))))
                );
        }

        public override ExComp GetDerivativeOf()
        {
            return CreateDerivativeOf(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "-(1)/(|x|sqrt(x^2-1))";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerTermStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerTermStr == null)
                return null;
            return "(1.0/Math.asin(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerTermStr + "))";
        }
    }

    internal class ASecFunction : InverseTrigFunction
    {
        public ASecFunction(ExComp innerEx)
            : base(innerEx, "arcsec", FunctionType.Sinusodal, typeof(ASecFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (InStandardDomain(ref pEvalData) && GetInnerEx() is ExNumber)
                return ExNumber.GetUndefined();

            AlgebraTerm recipInner = AlgebraTerm.FromFraction(ExNumber.GetOne(), GetInnerEx());
            recipInner = recipInner.MakeFormattingCorrect();

            ACosFunction asin = new ACosFunction(recipInner.RemoveRedundancies(false));
            asin.SkipDomain = true;
            ExComp evalASin = asin.Evaluate(harshEval, ref pEvalData);
            return evalASin;
        }

        public static ExComp CreateDerivativeOf(ExComp ex)
        {
            return AlgebraTerm.FromFraction(
                ExNumber.GetOne(),
                MulOp.StaticWeakCombine(new AbsValFunction(ex), PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(PowOp.StaticWeakCombine(ex, new ExNumber(2.0)), MulOp.Negate(ExNumber.GetOne())))));
        }

        public override ExComp GetDerivativeOf()
        {
            return CreateDerivativeOf(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "1/(|x|sqrt(x^2-1))";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerTermSTr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerTermSTr == null)
                return null;
            return "(1.0/Math.acos(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerTermSTr + ")";
        }
    }

    internal class ASinFunction : InverseTrigFunction
    {
        private bool b_skipDomain = false;

        public bool SkipDomain
        {
            set { b_skipDomain = value; }
        }

        public ASinFunction(ExComp innerEx)
            : base(innerEx, "arcsin", FunctionType.Sinusodal, typeof(ASinFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (!b_skipDomain && !InStandardDomain(ref pEvalData))
                return ExNumber.GetUndefined();

            ExComp innerEx = GetInnerEx();
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).MakeFormattingCorrect();
            UnitCirclePoint point = UnitCircle.GetAngleForPoint_Y(innerEx);

            if (point != null)
            {
                return TrigFunction.FinalEvalAngle(point.GetAngle(), pEvalData.GetUseRad());
            }

            if (!(innerEx is ExNumber))
                return this;

            ExNumber nInnerEx = innerEx as ExNumber;

            if (nInnerEx.HasImaginaryComp())
                return this;

            double dInner = nInnerEx.GetRealComp();

            double dASin = Math.Asin(dInner);

            if (harshEval)
            {
                return TrigFunction.FinalEvalAngle(new ExNumber(dASin), pEvalData.GetUseRad());
            }

            if (DoubleHelper.IsInteger(dASin))
                return TrigFunction.FinalEvalAngle(new ExNumber(dASin), pEvalData.GetUseRad());

            return this;
        }

        public static ExComp CreateDerivativeOf(ExComp ex)
        {
            return AlgebraTerm.FromFraction(
                ExNumber.GetOne(),
                PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(ExNumber.GetOne(), MulOp.Negate(PowOp.StaticWeakCombine(ex, new ExNumber(2.0))))));
        }

        public override ExComp GetDerivativeOf()
        {
            return CreateDerivativeOf(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "1/(sqrt(1-x^2))";
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> rests = new List<Restriction>();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "The domain of " + WorkMgr.STM + "\\arcsin(x)" +
                WorkMgr.EDM + " is " + WorkMgr.STM + "-1 \\le x \\le 1" + WorkMgr.EDM);

            AlgebraComp varForCmp = varFor.ToAlgebraComp();

            ExComp lower = ExNumber.GetNegOne();
            ExComp upper = ExNumber.GetOne();

            if (!GetInnerEx().IsEqualTo(varForCmp))
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "As the inside of " + WorkMgr.STM + "\\arcsin" +
                    WorkMgr.EDM + " is not " + WorkMgr.STM + varForCmp.ToDispString() + WorkMgr.EDM + " solve for the domain.");

                List<ExComp> sides = new List<ExComp>();
                sides.Add(lower);
                sides.Add(GetInnerTerm());
                sides.Add(upper);

                List<Parsing.LexemeType> comparisons = new List<Parsing.LexemeType>();
                comparisons.Add(Parsing.LexemeType.LessEqual);
                comparisons.Add(Parsing.LexemeType.LessEqual);

                SolveResult result = agSolver.SolveEquationInequality(sides, comparisons, varFor, ref pEvalData);
                if (!result.Success)
                    return null;

                return result.Restrictions;
            }

            rests.Add(new AndRestriction(lower, Parsing.LexemeType.LessEqual, varForCmp, Parsing.LexemeType.LessEqual,
                upper, ref pEvalData));
            return rests;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerTermStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerTermStr == null)
                return null;
            return "Math.asin(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerTermStr + ")";
        }
    }

    internal class ATanFunction : InverseTrigFunction
    {
        public ATanFunction(ExComp innerEx)
            : base(innerEx, "arctan", FunctionType.Sinusodal, typeof(ATanFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            ExComp innerEx = GetInnerEx();

            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).MakeFormattingCorrect();

            if (ExNumber.IsUndef(innerEx))
            {
                return pEvalData.GetUseRad() ? (ExComp)AlgebraTerm.FromFraction(Constant.GetPi(), new ExNumber(2.0)) : new ExNumber(90.0);
            }

            UnitCirclePoint point = UnitCircle.GetAngleForPoint_Y_over_X(innerEx);

            if (point != null)
            {
                return TrigFunction.FinalEvalAngle(point.GetAngle(), pEvalData.GetUseRad());
            }

            if (!(innerEx is ExNumber))
                return this;

            ExNumber nInnerEx = innerEx as ExNumber;

            if (nInnerEx.HasImaginaryComp())
                return this;

            double dInner = nInnerEx.GetRealComp();

            double dATan = Math.Atan(dInner);

            if (harshEval)
            {
                return TrigFunction.FinalEvalAngle(new ExNumber(dATan), pEvalData.GetUseRad());
            }

            if (DoubleHelper.IsInteger(dATan))
                return TrigFunction.FinalEvalAngle(new ExNumber(dATan), pEvalData.GetUseRad());

            return this;
        }

        public static ExComp CreateDerivativeOf(ExComp ex)
        {
            return AlgebraTerm.FromFraction(
                ExNumber.GetOne(),
                AddOp.StaticWeakCombine(ExNumber.GetOne(), PowOp.StaticWeakCombine(ex, new ExNumber(2.0))));
        }

        public override ExComp GetDerivativeOf()
        {
            return CreateDerivativeOf(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "1/(1+x^2)";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerTermStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerTermStr == null)
                return null;
            return "Math.atan(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerTermStr + ")";
        }
    }

    internal class CosFunction : TrigFunction
    {
        public const string IDEN = "cos";

        public CosFunction(ExComp innerEx)
            : base(innerEx, IDEN, FunctionType.Sinusodal, typeof(CosFunction))
        {
        }

        public override ExComp CancelWith(TrigFunction tf)
        {
            if (tf is SecFunction && tf.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                return ExNumber.GetOne();
            if (tf is TanFunction)
            {
                TanFunction tanFunc = tf as TanFunction;
                if (tanFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new SinFunction(GetInnerEx());
            }
            else if (tf is CscFunction)
            {
                CscFunction cscFunc = tf as CscFunction;
                if (cscFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new CotFunction(GetInnerEx());
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.GetUseRad()).RemoveRedundancies(false);
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp())
                {
                    double cos = Math.Cos((innerEx as ExNumber).GetRealComp());
                    return new ExNumber(cos);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.GetUseRad())))
                return this;
            ExNumber num = null, den = null;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, false))
                return this;

            UnitCirclePoint unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.X;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.Negate(new SinFunction(GetInnerEx()));
        }

        public override string GetDerivativeOfStr()
        {
            return "-sin(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ACosFunction(GetInnerEx());
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = GetInnerTerm().GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = GetInnerTerm().GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.GetTwoPi() : (ExComp)(new ExNumber(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new SecFunction(GetInnerEx());
        }
    }

    internal class CotFunction : TrigFunction
    {
        public const string IDEN = "cot";

        public CotFunction(ExComp innerEx)
            : base(innerEx, IDEN, FunctionType.Sinusodal, typeof(CotFunction))
        {
        }

        public override ExComp CancelWith(TrigFunction tf)
        {
            if (tf is TanFunction && tf.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                return ExNumber.GetOne();
            if (tf is SinFunction)
            {
                SinFunction sinFunc = tf as SinFunction;
                if (sinFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new CosFunction(GetInnerEx());
            }
            else if (tf is SecFunction)
            {
                SecFunction cosFunc = tf as SecFunction;
                if (cosFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new CscFunction(this.GetInnerEx());
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.GetUseRad()).RemoveRedundancies(false);
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp())
                {
                    double tan = Math.Tan((innerEx as ExNumber).GetRealComp());
                    if (tan == 0.0)
                        return ExNumber.GetUndefined();
                    double recipTan = 1.0 / tan;
                    return new ExNumber(recipTan);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.GetUseRad())))
                return this;
            ExNumber num = null, den = null;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, true))
                return this;

            UnitCirclePoint unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.X_over_Y;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.Negate(PowOp.StaticWeakCombine(new CscFunction(GetInnerEx()), new ExNumber(2.0)));
        }

        public override string GetDerivativeOfStr()
        {
            return "-csc^2(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ACotFunction(GetInnerEx());
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = GetInnerTerm().GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = GetInnerTerm().GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.GetPi() : (ExComp)(new ExNumber(180.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new TanFunction(GetInnerEx());
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = ExNumber.GetZero().ToAlgTerm();
            ExComp interval = Constant.GetPi();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + GetInnerTerm().FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of cot cannot equal " + WorkMgr.STM + "\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, GetInnerTerm(), neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, GetInnerTerm(), interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerStr == null)
                return null;
            return "(1.0/Math.tan(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerStr + "))";
        }
    }

    internal class CscFunction : TrigFunction
    {
        public const string IDEN = "csc";

        public CscFunction(ExComp innerEx)
            : base(innerEx, IDEN, FunctionType.Sinusodal, typeof(CscFunction))
        {
        }

        public override ExComp CancelWith(TrigFunction tf)
        {
            if (tf is SinFunction && tf.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                return ExNumber.GetOne();
            if (tf is TanFunction)
            {
                TanFunction tanFunc = tf as TanFunction;
                if (tanFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new SecFunction(GetInnerEx());
            }
            else if (tf is CosFunction)
            {
                CosFunction cosFunc = tf as CosFunction;
                if (cosFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new CotFunction(GetInnerEx());
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.GetUseRad()).RemoveRedundancies(false);
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp())
                {
                    double sin = Math.Sin((innerEx as ExNumber).GetRealComp());
                    if (sin == 0.0)
                        return ExNumber.GetUndefined();
                    double recipSin = 1.0 / sin;
                    return new ExNumber(recipSin);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.GetUseRad())))
                return this;
            ExNumber num = null, den = null;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, true))
                return this;

            UnitCirclePoint unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.over_Y;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.StaticWeakCombine(MulOp.Negate(new CscFunction(GetInnerEx())), new CotFunction(GetInnerEx()));
        }

        public override string GetDerivativeOfStr()
        {
            return "-csc(x)cot(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ACscFunction(GetInnerEx());
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // All numbers except for x = pin.
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = ExNumber.GetZero().ToAlgTerm();
            ExComp interval = Constant.GetPi();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + GetInnerTerm().FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of csc cannot equal " + WorkMgr.STM + "\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, GetInnerTerm(), neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, GetInnerTerm(), interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = GetInnerTerm().GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = GetInnerTerm().GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.GetTwoPi() : (ExComp)(new ExNumber(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new SinFunction(GetInnerEx());
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerStr == null)
                return null;
            return "(1.0/Math.sin(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerStr + "))";
        }
    }

    internal abstract class InverseTrigFunction : BasicAppliedFunc
    {
        public InverseTrigFunction(ExComp innerEx, string iden, FunctionType ft, Type type)
            : base(innerEx, iden, ft, type)
        {
        }

        public static bool IsValidType(string tt)
        {
            if (tt == "acos" || tt == "arccos" ||
                tt == "asin" || tt == "arcsin" ||
                tt == "atan" || tt == "arctan" ||
                tt == "acsc" || tt == "arccsc" ||
                tt == "asec" || tt == "arcsec" ||
                tt == "acot" || tt == "arccot")
                return true;

            return false;
        }

        public abstract ExComp GetDerivativeOf();

        public abstract string GetDerivativeOfStr();

        protected bool InStandardDomain(ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = GetInnerEx().CloneEx();
            innerEx = Simplifier.HarshSimplify(innerEx.ToAlgTerm(), ref pEvalData, false);

            if (innerEx is ExNumber)
            {
                ExNumber nInner = innerEx as ExNumber;
                if (ExNumber.OpGT(nInner, 1.0))
                    return false;
                if (ExNumber.OpLT(nInner, -1.0))
                    return false;
            }

            return true;
        }
    }

    internal class SecFunction : TrigFunction
    {
        public const string IDEN = "sec";

        public SecFunction(ExComp innerEx)
            : base(innerEx, IDEN, FunctionType.Sinusodal, typeof(SecFunction))
        {
        }

        public override ExComp CancelWith(TrigFunction tf)
        {
            if (tf is CosFunction && tf.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                return ExNumber.GetOne();
            if (tf is CotFunction)
            {
                CotFunction cotFunc = tf as CotFunction;
                if (cotFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new CscFunction(GetInnerEx());
            }
            else if (tf is SinFunction)
            {
                SinFunction sinFunc = tf as SinFunction;
                if (sinFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new TanFunction(GetInnerEx());
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.GetUseRad()).RemoveRedundancies(false);
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp())
                {
                    double cos = Math.Cos((innerEx as ExNumber).GetRealComp());
                    if (cos == 0.0)
                        return ExNumber.GetUndefined();
                    double recipCos = 1.0 / cos;
                    return new ExNumber(recipCos);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.GetUseRad())))
                return this;
            ExNumber num = null, den = null;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, false))
                return this;

            UnitCirclePoint unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.over_X;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.StaticWeakCombine(new SecFunction(GetInnerEx()), new TanFunction(GetInnerEx()));
        }

        public override string GetDerivativeOfStr()
        {
            return "sec(x)tan(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ASecFunction(GetInnerEx());
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = GetInnerTerm().GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = GetInnerTerm().GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.GetTwoPi() : (ExComp)(new ExNumber(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new CosFunction(GetInnerEx());
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // All numbers except for x = pi / 2 + pin.
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = AlgebraTerm.FromFraction(Constant.GetPi(), new ExNumber(2.0));
            ExComp interval = Constant.GetPi();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + GetInnerTerm().FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of sec cannot equal " + WorkMgr.STM + "\\frac{\\pi}{2}+\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, GetInnerTerm(), neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, GetInnerTerm(), interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerStr == null)
                return null;

            return "(1.0/Math.cos(" + (useRad ? "" : "(180.0 / Math.PI)*") + innerStr + "))";
        }
    }

    internal class SinFunction : TrigFunction
    {
        public const string IDEN = "sin";

        public SinFunction(ExComp innerEx)
            : base(innerEx, IDEN, FunctionType.Sinusodal, typeof(SinFunction))
        {
        }

        public override ExComp CancelWith(TrigFunction tf)
        {
            if (tf is CscFunction && tf.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                return ExNumber.GetOne();
            if (tf is CotFunction)
            {
                CotFunction cotFunc = tf as CotFunction;
                if (cotFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new CosFunction(GetInnerEx());
            }
            else if (tf is SecFunction)
            {
                SecFunction secFunc = tf as SecFunction;
                if (secFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new TanFunction(GetInnerEx());
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.GetUseRad()).RemoveRedundancies(false);
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp())
                {
                    double sin = Math.Sin((innerEx as ExNumber).GetRealComp());
                    return new ExNumber(sin);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.GetUseRad())))
                return this;
            ExNumber num = null, den = null;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, true))
                return this;

            UnitCirclePoint unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.Y;
        }

        public override ExComp GetDerivativeOf()
        {
            return new CosFunction(GetInnerEx());
        }

        public override string GetDerivativeOfStr()
        {
            return "cos(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ASinFunction(GetInnerEx());
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = GetInnerTerm().GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = GetInnerTerm().GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.GetTwoPi() : (ExComp)(new ExNumber(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new CscFunction(GetInnerEx());
        }
    }

    internal class TanFunction : TrigFunction
    {
        public const string IDEN = "tan";

        public TanFunction(ExComp innerEx)
            : base(innerEx, IDEN, FunctionType.Sinusodal, typeof(TanFunction))
        {
        }

        public override ExComp CancelWith(TrigFunction tf)
        {
            if (tf is CotFunction && tf.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                return ExNumber.GetOne();
            if (tf is CosFunction)
            {
                CosFunction cosFunc = tf as CosFunction;
                if (cosFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new SinFunction(GetInnerEx());
            }
            else if (tf is CscFunction)
            {
                CscFunction cscFunc = tf as CscFunction;
                if (cscFunc.GetInnerEx().IsEqualTo(this.GetInnerEx()))
                    return new SecFunction(this.GetInnerEx());
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.GetUseRad()).RemoveRedundancies(false);
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp())
                {
                    double tan = Math.Tan((innerEx as ExNumber).GetRealComp());
                    return new ExNumber(tan);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.GetUseRad())))
                return this;
            ExNumber num = null, den = null;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, true))
                return this;

            UnitCirclePoint unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.Y_over_X;
        }

        public override ExComp GetDerivativeOf()
        {
            return PowOp.StaticWeakCombine(new SecFunction(GetInnerEx()), new ExNumber(2.0));
        }

        public override string GetDerivativeOfStr()
        {
            return "sec^2(x)";
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // All numbers except for x = pi / 2 + pin.
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = AlgebraTerm.FromFraction(Constant.GetPi(), new ExNumber(2.0));
            ExComp interval = Constant.GetPi();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + GetInnerTerm().FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of tan cannot equal " + WorkMgr.STM + "\\frac{\\pi}{2}+\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, GetInnerTerm(), neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, GetInnerTerm(), interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ATanFunction(GetInnerEx());
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = GetInnerTerm().GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = GetInnerTerm().GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.GetPi() : (ExComp)(new ExNumber(180))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new CotFunction(GetInnerEx());
        }
    }

    internal abstract class TrigFunction : BasicAppliedFunc
    {
        public const int POW_COMPLEX = 3;
        public const int FRAC_COMPLEX = 2;
        public const int RECIP_COMPLEX = 1;
        public const int REG_COMPLEX = 0;

        public TrigFunction(ExComp innerEx, string iden, FunctionType ft, Type type)
            : base(innerEx, iden, ft, type)
        {
        }

        public static ExComp DegToRad(ExComp deg)
        {
            deg = Operators.MulOp.StaticCombine(deg, Constant.GetPi());
            deg = Operators.DivOp.StaticCombine(deg, new ExNumber(180.0));

            return deg;
        }

        public static ExComp FinalEvalAngle(ExComp radAngle, bool useRad)
        {
            if (useRad)
                return radAngle;
            return RadToDeg(radAngle);
        }

        public static ExComp FinalGetAngle(ExComp angle, bool useRad)
        {
            if (useRad)
                return angle;
            return DegToRad(angle);
        }

        public static ExComp GetFullRot(bool useRad)
        {
            if (useRad)
                return Constant.GetTwoPi();
            else
                return new ExNumber(360);
        }

        public static ExComp GetHalfRot(bool useRad)
        {
            if (useRad)
                return Constant.GetPi();
            else
                return new ExNumber(180);
        }

        public static int GetTrigFuncComplexity(ExComp trigFunc)
        {
            if (trigFunc is PowerFunction)
                return POW_COMPLEX;
            else if (trigFunc is TanFunction || trigFunc is CotFunction)
                return FRAC_COMPLEX;
            else if (trigFunc is CscFunction || trigFunc is SecFunction)
                return RECIP_COMPLEX;
            else
                return REG_COMPLEX;
        }

        public static string GetTrigType(ExComp ex)
        {
            if (ex is PowerFunction)
            {
                return GetTrigType((ex as PowerFunction).GetBase());
            }

            if (ex is SinFunction)
                return SinFunction.IDEN;
            else if (ex is CosFunction)
                return CosFunction.IDEN;
            else if (ex is TanFunction)
                return TanFunction.IDEN;
            else if (ex is CscFunction)
                return CscFunction.IDEN;
            else if (ex is SecFunction)
                return SecFunction.IDEN;
            else if (ex is CotFunction)
                return CotFunction.IDEN;

            return null;
        }

        public static bool IsRecipTrigFunc(TrigFunction trigFunc)
        {
            string typeIden = GetTrigType(trigFunc);
            if (typeIden == CscFunction.IDEN || typeIden == SecFunction.IDEN || typeIden == CotFunction.IDEN)
                return true;
            return false;
        }

        public static bool IsValidType(string tt)
        {
            if (tt == SinFunction.IDEN ||
                tt == CosFunction.IDEN ||
                tt == TanFunction.IDEN ||
                tt == CscFunction.IDEN ||
                tt == SecFunction.IDEN ||
                tt == CotFunction.IDEN)
                return true;

            return false;
        }

        public static ExComp RadToDeg(ExComp rad)
        {
            rad = Operators.MulOp.StaticCombine(rad, new ExNumber(180.0));
            rad = Operators.DivOp.StaticCombine(rad, Constant.GetPi());

            if (rad is AlgebraTerm)
                (rad as AlgebraTerm).HarshEvaluation();

            return rad;
        }

        public static ExComp TrigCancel(ExComp ex0, ExComp ex1, bool isEx1Den)
        {
            if (ex0 is AlgebraTerm)
                ex0 = (ex0 as AlgebraTerm).RemoveRedundancies(false);
            if (isEx1Den && ExNumber.GetOne().IsEqualTo(ex0))
            {
                if (ex1 is TrigFunction)
                    return (ex1 as TrigFunction).GetReciprocalOf();
                else if (ex1 is PowerFunction && (ex1 as PowerFunction).GetBase() is TrigFunction)
                {
                    TrigFunction baseTrigFunc = (ex1 as PowerFunction).GetBase() as TrigFunction;
                    return new PowerFunction(baseTrigFunc.GetReciprocalOf(), (ex1 as PowerFunction).GetPower());
                }
                else
                    return null;
            }
            if (ex1 is TrigFunction)
            {
                return isEx1Den ? (ex1 as TrigFunction).GetReciprocalOf().CancelWith(ex0) :
                    (ex1 as TrigFunction).CancelWith(ex0);
            }
            else if (ex0 is TrigFunction)
            {
                return (ex0 as TrigFunction).CancelWith(ex1);
            }
            if (ex0 is TrigFunction || ex1 is TrigFunction)
            {
                TrigFunction tf = ex0 is TrigFunction ? ex0 as TrigFunction : ex1 as TrigFunction;
                ExComp ex = ex0 is TrigFunction ? ex1 : ex0;

                return tf.CancelWith(ex);
            }
            else if (ex0 is PowerFunction && ex1 is PowerFunction)
            {
                PowerFunction pf0 = ex0 as PowerFunction;
                PowerFunction pf1 = ex1 as PowerFunction;

                ExComp pf0Base = pf0.GetBase();
                ExComp pf1Base = pf1.GetBase();

                if (pf0Base is TrigFunction && pf1Base is TrigFunction)
                {
                    TrigFunction tf0 = pf0Base as TrigFunction;
                    TrigFunction tf1 = pf1Base as TrigFunction;
                    if (isEx1Den)
                        tf1 = tf1.GetReciprocalOf();
                    ExComp cancelResult = tf0.CancelWith(tf1);
                    if (cancelResult != null)
                    {
                        if (pf0.GetPower().IsEqualTo(pf1))
                        {
                            return new PowerFunction(cancelResult, pf0.GetPower());
                        }
                        else if (pf0.GetPower() is ExNumber && pf1.GetPower() is ExNumber)
                        {
                            ExNumber nPf0Pow = pf0.GetPower() as ExNumber;
                            ExNumber nPf1Pow = pf1.GetPower() as ExNumber;

                            AlgebraTerm resultingTerm = new AlgebraTerm();

                            ExNumber nMinPow;
                            if (ExNumber.OpGT(nPf0Pow, nPf1Pow))
                            {
                                resultingTerm.Add(new PowerFunction(pf0Base, ExNumber.OpSub(nPf0Pow, nPf1Pow)));
                                nMinPow = nPf1Pow;
                            }
                            else
                            {
                                resultingTerm.Add(new PowerFunction(pf1Base, ExNumber.OpSub(nPf1Pow, nPf0Pow)));
                                nMinPow = nPf0Pow;
                            }

                            if (!ExNumber.GetOne().IsEqualTo(cancelResult))
                            {
                                resultingTerm.Add(new Operators.MulOp(), new PowerFunction(cancelResult, nMinPow));
                            }

                            return resultingTerm;
                        }
                        // Something like tan(x)^m * cos(x)^n cannot be simplified at all.
                    }
                }
            }

            return null;
        }

        public abstract ExComp CancelWith(TrigFunction tf);

        public ExComp CancelWith(ExComp ex)
        {
            if (ex is TrigFunction)
                return CancelWith(ex as TrigFunction);
            else if (ex is PowerFunction)
            {
                PowerFunction pf = ex as PowerFunction;
                if (pf.GetBase() is TrigFunction)
                {
                    ExComp cancelResult = (pf.GetBase() as TrigFunction).CancelWith(this);
                    if (cancelResult == null)
                        return null;
                    pf.SetPower(Operators.SubOp.StaticCombine(pf.GetPower(), ExNumber.GetOne()));

                    if (ExNumber.GetOne().IsEqualTo(cancelResult))
                    {
                        return pf.GetBase();
                    }
                    else
                    {
                        return new AlgebraTerm(cancelResult, new Operators.MulOp(), pf);
                    }
                }
            }

            return null;
        }

        public abstract ExComp GetDerivativeOf();

        public abstract string GetDerivativeOfStr();

        public abstract InverseTrigFunction GetInverseOf();

        public abstract ExComp GetPeriod(AlgebraComp varFor, bool useRad);

        public abstract TrigFunction GetReciprocalOf();

        public bool InStandardDomain()
        {
            ExComp innerEx = GetInnerEx();

            if (innerEx is ExNumber)
            {
                ExNumber nInner = innerEx as ExNumber;

                if (ExNumber.OpLT(nInner, -1.0))
                    return false;
                else if (ExNumber.OpGT(nInner, 1.0))
                    return false;
            }

            return true;
        }

        protected AlgebraTerm GetInnerTerm(bool useRad)
        {
            return FinalGetAngle(GetInnerTerm(), useRad).ToAlgTerm();
        }
    }
}