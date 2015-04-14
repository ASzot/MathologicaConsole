namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class FactorialFunction : AppliedFunction
    {
        public FactorialFunction(ExComp ex)
            : base(ex, FunctionType.Factorial, typeof(FactorialFunction))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = InnerEx;

            if (innerEx is Number && (innerEx as Number).IsRealInteger())
            {
                int num = (int)(innerEx as Number).RealComp;

                long factorialResult = 1;

                if (num < 0)
                {
                    return Number.Undefined;
                }

                if (num == 0)
                    return Number.One;

                for (int i = 1; i <= num; ++i)
                {
                    factorialResult *= i;
                }

                return new Number(factorialResult);
            }

            return this;
        }

        public override string ToAsciiString()
        {
            return InnerEx.ToAsciiString() + "!";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return InnerEx.ToString() + "!";
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return ToAsciiString();
        }

        public override string FinalToAsciiString()
        {
            return ToAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            return ToTexString();
        }

        public override string FinalToTexString()
        {
            return ToTexString();
        }

        public override string ToTexString()
        {
            return InnerEx.ToTexString() + "!";
        }
    }
}