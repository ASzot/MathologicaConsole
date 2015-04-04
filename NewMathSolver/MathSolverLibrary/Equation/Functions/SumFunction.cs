using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class SumFunction : AppliedFunction_NArgs
    {
        private const int MAX_SUM_COUNT = 50;

        public ExComp IterCount
        {
            get { return _args[3]; }
        }

        public ExComp IterStart
        {
            get { return _args[2]; }
        }

        public AlgebraComp IterVar
        {
            get { return (AlgebraComp)_args[1]; }
        }

        public SumFunction(ExComp term, AlgebraComp iterVar, ExComp iterStart, ExComp iterCount)
            : base(FunctionType.Summation, typeof(SumFunction), (iterVar.Var.Var == "i" && term is AlgebraTerm) ? (term as AlgebraTerm).ConvertImaginaryToVar() : term, iterVar, iterStart, iterCount)
        {
        }

        public override ExComp Clone()
        {
            return new SumFunction(InnerEx.Clone(), (AlgebraComp)IterVar.Clone(), IterStart.Clone(), IterCount.Clone());
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (!InnerTerm.Contains(IterVar))
            {
                ExComp sumTotal = Number.Zero;
                if (!Number.One.IsEqualTo(IterStart))
                    sumTotal = SubOp.StaticCombine(IterCount, SubOp.StaticCombine(IterStart, Number.One));
                else
                    sumTotal = IterCount;
                return MulOp.StaticCombine(sumTotal, InnerTerm);
            }

            ExComp innerEx = InnerEx;
            if (innerEx.IsEqualTo(IterVar) && IterStart.IsEqualTo(Number.One))
            {
                return DivOp.StaticCombine(MulOp.StaticCombine(IterCount, AddOp.StaticCombine(IterCount, Number.One)), new Number(2.0));
            }

            if (IterCount is Number && (IterCount as Number).IsRealInteger() &&
                IterStart is Number && (IterStart as Number).IsRealInteger())
            {
                int count = (int)(IterCount as Number).RealComp;
                int start = (int)(IterStart as Number).RealComp;

                AlgebraTerm totalTerm = new AlgebraTerm(Number.Zero);

                if (count > MAX_SUM_COUNT)
                    return this;

                ExComp iterVal;

                for (int i = start; i <= count; ++i)
                {
                    iterVal = new Number(i);

                    AlgebraTerm innerTerm = InnerTerm.Clone().ToAlgTerm();

                    innerTerm = innerTerm.Substitute(IterVar, iterVal);

                    ExComp simpInnerEx = TermType.SimplifyTermType.BasicSimplify(innerTerm.RemoveRedundancies(), ref pEvalData);

                    totalTerm = AddOp.StaticCombine(totalTerm, simpInnerEx).ToAlgTerm();
                }

                return totalTerm.ForceCombineExponents();
            }

            return this;
        }

        public override string ToMathAsciiString()
        {
            return "\\Sigma_{" + IterVar + "=" + IterStart + "}^{" + IterCount + "}" +
                InnerEx.ToMathAsciiString();
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return string.Format("Sum({0},{1},{2},{3})", InnerTerm.ToString(),
                IterCount.ToString(), IterStart.ToString(),
                IterVar.ToString());
        }

        public override string ToTexString()
        {
            return "\\Sigma_{" + IterVar + "=" + IterStart + "}^{" + IterCount + "}" +
                InnerEx.ToTexString();
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            ExComp useArg1;
            if (args[1] is AlgebraTerm)
                useArg1 = (args[1] as AlgebraTerm).RemoveRedundancies();
            else
                useArg1 = args[1];

            return new SumFunction(args[0], (AlgebraComp)useArg1, args[2], args[3]);
        }
    }
}