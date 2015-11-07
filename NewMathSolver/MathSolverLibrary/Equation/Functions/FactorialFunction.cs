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
            CallChildren(harshEval, ref pEvalData);

            ExComp innerEx = GetInnerEx();

            if (innerEx is ExNumber && (innerEx as ExNumber).IsRealInteger())
            {
                int num = (int)(innerEx as ExNumber).GetRealComp();

                long factorialResult = 1;

                if (num < 0)
                {
                    return ExNumber.GetUndefined();
                }

                if (num == 0)
                    return ExNumber.GetOne();

                for (int i = 1; i <= num; ++i)
                {
                    factorialResult *= i;
                }

                return new ExNumber(factorialResult);
            }

            return this;
        }

        public override string ToAsciiString()
        {
            return GetInnerEx().ToAsciiString() + "! ";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return GetInnerEx().ToString() + "! ";
        }

        public override string FinalToAsciiString()
        {
            return ToAsciiString();
        }

        public override string FinalToTexString()
        {
            return ToTexString();
        }

        public override string ToTexString()
        {
            return GetInnerEx().ToTexString() + "! ";
        }
    }
}