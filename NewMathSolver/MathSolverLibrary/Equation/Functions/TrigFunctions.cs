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
            if (!b_skipDomain && !InStandardDomain(ref pEvalData))
                return Number.Undefined;

            ExComp innerEx = InnerEx;
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).MakeFormattingCorrect();
            UnitCirclePoint point = UnitCircle.GetAngleForPoint_X(innerEx);

            if (point != null)
            {
                return TrigFunction.FinalEvalAngle(point.GetAngle(), pEvalData.UseRad);
            }

            if (!(innerEx is Number))
                return this;

            Number nInnerEx = innerEx as Number;

            if (nInnerEx.HasImaginaryComp())
                return this;

            double dInner = nInnerEx.RealComp;

            double dACos = Math.Acos(dInner);

            if (harshEval)
            {
                return TrigFunction.FinalEvalAngle(new Number(dACos), pEvalData.UseRad);
            }

            if (dACos.IsInteger())
                return TrigFunction.FinalEvalAngle(new Number(dACos), pEvalData.UseRad);

            return this;
        }

		public static ExComp CreateDerivativeOf(ExComp ex)
		{
			return MulOp.Negate(AlgebraTerm.FromFraction(
				Number.One,
				PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(Number.One, MulOp.Negate(PowOp.StaticWeakCombine(ex, new Number(2.0)))))));
		}

        public override ExComp GetDerivativeOf()
        {
			return CreateDerivativeOf(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "-(1)/(sqrt(1-x^2))";
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "The domain of " + WorkMgr.STM + "\\arccos(x)" +
                WorkMgr.EDM + " is " + WorkMgr.STM + "-1 \\le x \\le 1" + WorkMgr.EDM);

            AlgebraComp varForCmp = varFor.ToAlgebraComp();

            ExComp lower = Number.NegOne;
            ExComp upper = Number.One;

            if (!InnerEx.IsEqualTo(varForCmp))
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "As the inside of " + WorkMgr.STM + "\\arccos" +
                    WorkMgr.EDM + " is not " + WorkMgr.STM + varForCmp.ToDispString() + WorkMgr.EDM + " solve for the domain.");

                List<ExComp> sides = new List<ExComp>();
                sides.Add(lower);
                sides.Add(InnerTerm);
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
    }

    internal class ACotFunction : InverseTrigFunction
    {
        public ACotFunction(ExComp innerEx)
            : base(innerEx, "arccot", FunctionType.Sinusodal, typeof(ACotFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (Number.Zero.IsEqualTo(InnerEx))
            {
                return AlgebraTerm.FromFraction(Constant.Pi, new Number(2.0));
            }

            AlgebraTerm recipInner = AlgebraTerm.FromFraction(Number.One, InnerEx);
            recipInner = recipInner.MakeFormattingCorrect();

            ATanFunction asin = new ATanFunction(recipInner.RemoveRedundancies());
            return asin.Evaluate(harshEval, ref pEvalData);
        }

		public static ExComp CreateDerivativeOf(ExComp ex)
		{
			return MulOp.Negate(AlgebraTerm.FromFraction(
				Number.One,
				AddOp.StaticWeakCombine(Number.One, PowOp.StaticWeakCombine(ex, new Number(2.0)))));
		}

        public override ExComp GetDerivativeOf()
        {
			return CreateDerivativeOf(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "-(1)/(1+x^2)";
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
            if (InStandardDomain(ref pEvalData) && InnerEx is Number)
                return Number.Undefined;

            AlgebraTerm recipInner = AlgebraTerm.FromFraction(Number.One, InnerEx);
            recipInner = recipInner.MakeFormattingCorrect();

            ASinFunction asin = new ASinFunction(recipInner.RemoveRedundancies());
            asin.SkipDomain = true;
            return asin.Evaluate(harshEval, ref pEvalData);
        }

		public static ExComp CreateDerivativeOf(ExComp ex)
		{
			return MulOp.Negate(
				AlgebraTerm.FromFraction(
				Number.One,
				MulOp.StaticWeakCombine(new AbsValFunction(ex), PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(PowOp.StaticWeakCombine(ex, new Number(2.0)), MulOp.Negate(Number.One)))))
				);
		}

        public override ExComp GetDerivativeOf()
        {
			return CreateDerivativeOf(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "-(1)/(|x|sqrt(x^2-1))";
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
            if (InStandardDomain(ref pEvalData) && InnerEx is Number)
                return Number.Undefined;

            AlgebraTerm recipInner = AlgebraTerm.FromFraction(Number.One, InnerEx);
            recipInner = recipInner.MakeFormattingCorrect();

            ACosFunction asin = new ACosFunction(recipInner.RemoveRedundancies());
            asin.SkipDomain = true;
            return asin.Evaluate(harshEval, ref pEvalData);
        }

		public static ExComp CreateDerivativeOf(ExComp ex)
		{
			return AlgebraTerm.FromFraction(
				Number.One,
				MulOp.StaticWeakCombine(new AbsValFunction(ex), PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(PowOp.StaticWeakCombine(ex, new Number(2.0)), MulOp.Negate(Number.One)))));
		}

        public override ExComp GetDerivativeOf()
        {
			return CreateDerivativeOf(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "1/(|x|sqrt(x^2-1))";
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
            if (!b_skipDomain && !InStandardDomain(ref pEvalData))
                return Number.Undefined;

            ExComp innerEx = InnerEx;
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).MakeFormattingCorrect();
            UnitCirclePoint point = UnitCircle.GetAngleForPoint_Y(innerEx);

            if (point != null)
            {
                return TrigFunction.FinalEvalAngle(point.GetAngle(), pEvalData.UseRad);
            }

            if (!(innerEx is Number))
                return this;

            Number nInnerEx = innerEx as Number;

            if (nInnerEx.HasImaginaryComp())
                return this;

            double dInner = nInnerEx.RealComp;

            double dASin = Math.Asin(dInner);

            if (harshEval)
            {
                return TrigFunction.FinalEvalAngle(new Number(dASin), pEvalData.UseRad);
            }

            if (dASin.IsInteger())
                return TrigFunction.FinalEvalAngle(new Number(dASin), pEvalData.UseRad);

            return this;
        }

		public static ExComp CreateDerivativeOf(ExComp ex)
		{
			return AlgebraTerm.FromFraction(
				Number.One,
				PowOp.WeakTakeSqrt(AddOp.StaticWeakCombine(Number.One, MulOp.Negate(PowOp.StaticWeakCombine(ex, new Number(2.0))))));
		}

        public override ExComp GetDerivativeOf()
        {
			return CreateDerivativeOf(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "1/(sqrt(1-x^2))";
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> rests = new List<Restriction>();

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "The domain of " + WorkMgr.STM + "\\arcsin(x)" +
                WorkMgr.EDM + " is " + WorkMgr.STM + "-1 \\le x \\le 1" + WorkMgr.EDM);

            AlgebraComp varForCmp = varFor.ToAlgebraComp();

            ExComp lower = Number.NegOne;
            ExComp upper = Number.One;

            if (!InnerEx.IsEqualTo(varForCmp))
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "As the inside of " + WorkMgr.STM + "\\arcsin" +
                    WorkMgr.EDM + " is not " + WorkMgr.STM + varForCmp.ToDispString() + WorkMgr.EDM + " solve for the domain.");

                List<ExComp> sides = new List<ExComp>();
                sides.Add(lower);
                sides.Add(InnerTerm);
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
    }

    internal class ATanFunction : InverseTrigFunction
    {

        public ATanFunction(ExComp innerEx)
            : base(innerEx, "arctan", FunctionType.Sinusodal, typeof(ATanFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = InnerEx;

            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).MakeFormattingCorrect();

            if (Number.IsUndef(innerEx))
            {
                return pEvalData.UseRad ? (ExComp)AlgebraTerm.FromFraction(Constant.Pi, new Number(2.0)) : new Number(90.0);
            }

            UnitCirclePoint point = UnitCircle.GetAngleForPoint_Y_over_X(innerEx);

            if (point != null)
            {
                return TrigFunction.FinalEvalAngle(point.GetAngle(), pEvalData.UseRad);
            }

            if (!(innerEx is Number))
                return this;

            Number nInnerEx = innerEx as Number;

            if (nInnerEx.HasImaginaryComp())
                return this;

            double dInner = nInnerEx.RealComp;

            double dATan = Math.Atan(dInner);

            if (harshEval)
            {
                return TrigFunction.FinalEvalAngle(new Number(dATan), pEvalData.UseRad);
            }

            if (dATan.IsInteger())
                return TrigFunction.FinalEvalAngle(new Number(dATan), pEvalData.UseRad);

            return this;
        }

		public static ExComp CreateDerivativeOf(ExComp ex)
		{
			return AlgebraTerm.FromFraction(
				Number.One,
				AddOp.StaticWeakCombine(Number.One, PowOp.StaticWeakCombine(ex, new Number(2.0))));
		}

        public override ExComp GetDerivativeOf()
        {
			return CreateDerivativeOf(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "1/(1+x^2)";
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
            if (tf is SecFunction)
                return Number.One;
            if (tf is TanFunction)
            {
                TanFunction tanFunc = tf as TanFunction;
                if (tanFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new SinFunction(InnerEx);
            }
            else if (tf is CscFunction)
            {
                CscFunction cscFunc = tf as CscFunction;
                if (cscFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new CotFunction(InnerEx);
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.UseRad).RemoveRedundancies();
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is Number && !(innerEx as Number).HasImaginaryComp())
                {
                    double cos = Math.Cos((innerEx as Number).RealComp);
                    return new Number(cos);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.UseRad)))
                return this;
            Number num, den;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, false))
                return this;

            var unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.X;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.Negate(new SinFunction(InnerEx));
        }

        public override string GetDerivativeOfStr()
        {
            return "-sin(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ACosFunction(InnerEx);
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = InnerTerm.GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = InnerTerm.GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.TwoPi : (ExComp)(new Number(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new SecFunction(InnerEx);
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
            if (tf is TanFunction)
                return Number.One;
            if (tf is SinFunction)
            {
                SinFunction sinFunc = tf as SinFunction;
                if (sinFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new CosFunction(InnerEx);
            }
            else if (tf is SecFunction)
            {
                SecFunction cosFunc = tf as SecFunction;
                if (cosFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new CscFunction(this.InnerEx);
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.UseRad).RemoveRedundancies();
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is Number && !(innerEx as Number).HasImaginaryComp())
                {
                    double tan = Math.Tan((innerEx as Number).RealComp);
                    if (tan == 0.0)
                        return Number.Undefined;
                    double recipTan = 1.0 / tan;
                    return new Number(recipTan);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.UseRad)))
                return this;
            Number num, den;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den))
                return this;

            var unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.X_over_Y;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.Negate(PowOp.StaticWeakCombine(new CscFunction(InnerEx), new Number(2.0)));
        }

        public override string GetDerivativeOfStr()
        {
            return "-csc^2(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ACotFunction(InnerEx);
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = InnerTerm.GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = InnerTerm.GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.Pi : (ExComp)(new Number(180.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new TanFunction(InnerEx);
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = Number.Zero.ToAlgTerm();
            ExComp interval = Constant.Pi;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + InnerTerm.FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of cot cannot equal " + WorkMgr.STM + "\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, InnerTerm, neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, InnerTerm, interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
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
            if (tf is SinFunction)
                return Number.One;
            if (tf is TanFunction)
            {
                TanFunction tanFunc = tf as TanFunction;
                if (tanFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new SecFunction(InnerEx);
            }
            else if (tf is CosFunction)
            {
                CosFunction cosFunc = tf as CosFunction;
                if (cosFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new CotFunction(InnerEx);
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.UseRad).RemoveRedundancies();
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is Number && !(innerEx as Number).HasImaginaryComp())
                {
                    double sin = Math.Sin((innerEx as Number).RealComp);
                    if (sin == 0.0)
                        return Number.Undefined;
                    double recipSin = 1.0 / sin;
                    return new Number(recipSin);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.UseRad)))
                return this;
            Number num, den;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den))
                return this;

            var unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.over_Y;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.StaticWeakCombine(MulOp.Negate(new CscFunction(InnerEx)), new CotFunction(InnerEx));
        }

        public override string GetDerivativeOfStr()
        {
            return "-csc(x)cot(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ACscFunction(InnerEx);
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // All numbers except for x = pin.
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = Number.Zero.ToAlgTerm();
            ExComp interval = Constant.Pi;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + InnerTerm.FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of csc cannot equal " + WorkMgr.STM + "\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, InnerTerm, neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, InnerTerm, interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = InnerTerm.GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = InnerTerm.GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.TwoPi : (ExComp)(new Number(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new SinFunction(InnerEx);
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
            ExComp innerEx = InnerEx.Clone();
            innerEx = Simplifier.HarshSimplify(innerEx.ToAlgTerm(), ref pEvalData, false);

            if (innerEx is Number)
            {
                Number nInner = innerEx as Number;
                if (nInner > 1.0)
                    return false;
                if (nInner < -1.0)
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
            if (tf is CosFunction)
                return Number.One;
            if (tf is CotFunction)
            {
                CotFunction cotFunc = tf as CotFunction;
                if (cotFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new CscFunction(InnerEx);
            }
            else if (tf is SinFunction)
            {
                SinFunction sinFunc = tf as SinFunction;
                if (sinFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new TanFunction(InnerEx);
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.UseRad).RemoveRedundancies();
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is Number && !(innerEx as Number).HasImaginaryComp())
                {
                    double cos = Math.Cos((innerEx as Number).RealComp);
                    if (cos == 0.0)
                        return Number.Undefined;
                    double recipCos = 1.0 / cos;
                    return new Number(recipCos);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.UseRad)))
                return this;
            Number num, den;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den, false))
                return this;

            var unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.over_X;
        }

        public override ExComp GetDerivativeOf()
        {
            return MulOp.StaticWeakCombine(new SecFunction(InnerEx), new TanFunction(InnerEx));
        }

        public override string GetDerivativeOfStr()
        {
            return "sec(x)tan(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ASecFunction(InnerEx);
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = InnerTerm.GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = InnerTerm.GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.TwoPi : (ExComp)(new Number(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new CosFunction(InnerEx);
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // All numbers except for x = pi / 2 + pin.
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = AlgebraTerm.FromFraction(Constant.Pi, new Number(2.0));
            ExComp interval = Constant.Pi;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + InnerTerm.FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM,
                "The inside of sec cannot equal " + WorkMgr.STM + "\\frac{\\pi}{2}+\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, InnerTerm, neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, InnerTerm, interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
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
            if (tf is CscFunction)
                return Number.One;
            if (tf is CotFunction)
            {
                CotFunction cotFunc = tf as CotFunction;
                if (cotFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new CosFunction(InnerEx);
            }
            else if (tf is SecFunction)
            {
                SecFunction secFunc = tf as SecFunction;
                if (secFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new TanFunction(InnerEx);
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.UseRad).RemoveRedundancies();
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is Number && !(innerEx as Number).HasImaginaryComp())
                {
                    double sin = Math.Sin((innerEx as Number).RealComp);
                    return new Number(sin);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.UseRad)))
                return this;
            Number num, den;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den))
                return this;

            var unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.Y;
        }

        public override ExComp GetDerivativeOf()
        {
            return new CosFunction(InnerEx);
        }

        public override string GetDerivativeOfStr()
        {
            return "cos(x)";
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ASinFunction(InnerEx);
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = InnerTerm.GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = InnerTerm.GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.TwoPi : (ExComp)(new Number(360.0))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new CscFunction(InnerEx);
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
            if (tf is CotFunction)
                return Number.One;
            if (tf is CosFunction)
            {
                CosFunction cosFunc = tf as CosFunction;
                if (cosFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new SinFunction(InnerEx);
            }
            else if (tf is CscFunction)
            {
                CscFunction cscFunc = tf as CscFunction;
                if (cscFunc.InnerEx.IsEqualTo(this.InnerEx))
                    return new SecFunction(this.InnerEx);
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (harshEval)
            {
                ExComp innerEx = GetInnerTerm(pEvalData.UseRad).RemoveRedundancies();
                if (innerEx is AlgebraTerm)
                    innerEx = Simplifier.HarshSimplify(innerEx as AlgebraTerm, ref pEvalData, false);
                if (innerEx is Number && !(innerEx as Number).HasImaginaryComp())
                {
                    double tan = Math.Tan((innerEx as Number).RealComp);
                    return new Number(tan);
                }
            }

            Term.SimpleFraction simplFrac = new Term.SimpleFraction();
            if (!simplFrac.LooseInit(GetInnerTerm(pEvalData.UseRad)))
                return this;
            Number num, den;
            if (!simplFrac.IsSimpleUnitCircleAngle(out num, out den))
                return this;

            var unitCirclePoint = UnitCircle.GetPointForAngle(num, den);
            if (unitCirclePoint == null)
                return this;

            return unitCirclePoint.Y_over_X;
        }

        public override ExComp GetDerivativeOf()
        {
            return PowOp.StaticWeakCombine(new SecFunction(InnerEx), new Number(2.0));
        }

        public override string GetDerivativeOfStr()
        {
            return "sec^2(x)";
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // All numbers except for x = pi / 2 + pin.
            List<Restriction> rests = new List<Restriction>();

            AlgebraTerm neTerm = AlgebraTerm.FromFraction(Constant.Pi, new Number(2.0));
            ExComp interval = Constant.Pi;

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + InnerTerm.FinalToDispStr() + "\\ne" + neTerm.FinalToDispStr() + WorkMgr.EDM, 
                "The inside of tan cannot equal " + WorkMgr.STM + "\\frac{\\pi}{2}+\\pi*n" + WorkMgr.EDM);

            ExComp neEx = agSolver.Solve(varFor, InnerTerm, neTerm, ref pEvalData);
            ExComp intervalEx = agSolver.Solve(varFor, InnerTerm, interval.ToAlgTerm(), ref pEvalData);
            AlgebraComp iterVar = new AlgebraComp("n");
            intervalEx = MulOp.StaticWeakCombine(intervalEx, iterVar);

            rests.Add(new NotRestriction(varFor.ToAlgebraComp(), new GeneralSolution(neEx, intervalEx, iterVar)));

            return rests;
        }

        public override InverseTrigFunction GetInverseOf()
        {
            return new ATanFunction(InnerEx);
        }

        public override ExComp GetPeriod(AlgebraComp varFor, bool useRad)
        {
            List<ExComp> innerPowers = InnerTerm.GetPowersOfVar(varFor);
            if (innerPowers.Count != 1)
                return null;

            if (!innerPowers[0].ToAlgTerm().IsOne())
                return null;

            ExComp coeff = InnerTerm.GetCoeffOfVar(varFor);
            if (coeff == null)
                return null;

            ExComp interval = Operators.DivOp.StaticCombine((useRad ? Constant.Pi : (ExComp)(new Number(180))), coeff);

            return interval;
        }

        public override TrigFunction GetReciprocalOf()
        {
            return new CotFunction(InnerEx);
        }
    }

    internal abstract class TrigFunction : BasicAppliedFunc
    {
        public enum TrigFuncComplexity { PowComplex = 3, FracComplex = 2, RecipComplex = 1, RegComplex = 0 };

        public TrigFunction(ExComp innerEx, string iden, FunctionType ft, Type type)
            : base(innerEx, iden, ft, type)
        {
        }

        public static ExComp DegToRad(ExComp deg)
        {
            deg = Operators.MulOp.StaticCombine(deg, Constant.Pi);
            deg = Operators.DivOp.StaticCombine(deg, new Number(180.0));

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
                return Constant.TwoPi;
            else
                return new Number(360);
        }

        public static ExComp GetHalfRot(bool useRad)
        {
            if (useRad)
                return Constant.Pi;
            else
                return new Number(180);
        }

        public static TrigFuncComplexity GetTrigFuncComplexity(ExComp trigFunc)
        {
            if (trigFunc is PowerFunction)
                return TrigFuncComplexity.PowComplex;
            else if (trigFunc is TanFunction || trigFunc is CotFunction)
                return TrigFuncComplexity.FracComplex;
            else if (trigFunc is CscFunction || trigFunc is SecFunction)
                return TrigFuncComplexity.RecipComplex;
            else
                return TrigFuncComplexity.RegComplex;
        }

        public static string GetTrigType(ExComp ex)
        {
            if (ex is PowerFunction)
            {
                return GetTrigType((ex as PowerFunction).Base);
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
            rad = Operators.MulOp.StaticCombine(rad, new Number(180.0));
            rad = Operators.DivOp.StaticCombine(rad, Constant.Pi);

            if (rad is AlgebraTerm)
                (rad as AlgebraTerm).HarshEvaluation();

            return rad;
        }

        public static ExComp TrigCancel(ExComp ex0, ExComp ex1, bool isEx1Den = false)
        {
            if (ex0 is AlgebraTerm)
                ex0 = (ex0 as AlgebraTerm).RemoveRedundancies();
            if (isEx1Den && Number.One.IsEqualTo(ex0))
            {
                if (ex1 is TrigFunction)
                    return (ex1 as TrigFunction).GetReciprocalOf();
                else if (ex1 is PowerFunction && (ex1 as PowerFunction).Base is TrigFunction)
                {
                    TrigFunction baseTrigFunc = (ex1 as PowerFunction).Base as TrigFunction;
                    return new PowerFunction(baseTrigFunc.GetReciprocalOf(), (ex1 as PowerFunction).Power);
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

                ExComp pf0Base = pf0.Base;
                ExComp pf1Base = pf1.Base;

                if (pf0Base is TrigFunction && pf1Base is TrigFunction)
                {
                    TrigFunction tf0 = pf0Base as TrigFunction;
                    TrigFunction tf1 = pf1Base as TrigFunction;
                    if (isEx1Den)
                        tf1 = tf1.GetReciprocalOf();
                    ExComp cancelResult = tf0.CancelWith(tf1);
                    if (cancelResult != null)
                    {
                        if (pf0.Power.IsEqualTo(pf1))
                        {
                            return new PowerFunction(cancelResult, pf0.Power);
                        }
                        else if (pf0.Power is Number && pf1.Power is Number)
                        {
                            Number nPf0Pow = pf0.Power as Number;
                            Number nPf1Pow = pf1.Power as Number;

                            AlgebraTerm resultingTerm = new AlgebraTerm();

                            Number nMinPow;
                            if (nPf0Pow > nPf1Pow)
                            {
                                resultingTerm.Add(new PowerFunction(pf0Base, nPf0Pow - nPf1Pow));
                                nMinPow = nPf1Pow;
                            }
                            else
                            {
                                resultingTerm.Add(new PowerFunction(pf1Base, nPf1Pow - nPf0Pow));
                                nMinPow = nPf0Pow;
                            }

                            if (!Number.One.IsEqualTo(cancelResult))
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
                if (pf.Base is TrigFunction)
                {
                    ExComp cancelResult = CancelWith(pf.Base as TrigFunction);
                    if (cancelResult == null)
                        return null;
                    pf.Power = Operators.SubOp.StaticCombine(pf.Power, Number.One);

                    if (Number.One.IsEqualTo(cancelResult))
                    {
                        return pf;
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
            ExComp innerEx = InnerEx;

            if (innerEx is Number)
            {
                Number nInner = innerEx as Number;

                if (nInner < -1.0)
                    return false;
                else if (nInner > 1.0)
                    return false;
            }

            return true;
        }

        protected AlgebraTerm GetInnerTerm(bool useRad)
        {
            return FinalGetAngle(InnerTerm, useRad).ToAlgTerm();
        }
    }
}