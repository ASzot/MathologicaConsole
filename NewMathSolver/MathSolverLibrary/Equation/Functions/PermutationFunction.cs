namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class PermutationFunction : AppliedFunction_NArgs
    {
        private const string IDEN = "P";

        public void SetBottom(ExComp value)
        {
            _args[1] = value;
        }

        public ExComp GetBottom()
        {
            return _args[1];
        }

        public void SetTop(ExComp value)
        {
            _args[0] = value;
        }

        public ExComp GetTop()
        {
            return _args[0];
        }

        public AlgebraTerm GetTopTerm()
        {
            return GetTop().ToAlgTerm();
        }

        public AlgebraTerm GetBottomTerm()
        {
            return GetBottom().ToAlgTerm();
        }

        public PermutationFunction(ExComp top, ExComp bottom)
            : base(FunctionType.Permutation, typeof(PermutationFunction),
            top is AlgebraTerm ? (top as AlgebraTerm).RemoveRedundancies(false) : top,
            bottom is AlgebraTerm ? (bottom as AlgebraTerm).RemoveRedundancies(false) : bottom)
        {
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            ExComp bottom, top;
            if (GetBottom() is AlgebraTerm)
                bottom = (GetBottom() as AlgebraTerm).ConvertImaginaryToVar();
            else
                bottom = GetBottom();
            if (GetTop() is AlgebraTerm)
                top = (GetTop() as AlgebraTerm).ConvertImaginaryToVar();
            else
                top = GetTop();

            return new ChooseFunction(top, bottom);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            ExComp n = GetTop();
            ExComp k = GetBottom();

            if (n is ExNumber && k is ExNumber && (n as ExNumber).IsRealInteger() && (k as ExNumber).IsRealInteger())
            {
                FactorialFunction nFactorial = new FactorialFunction(n);

                FactorialFunction nMinusKFactorial = new FactorialFunction(Operators.SubOp.StaticCombine(n, k));

                ExComp nFactEval = nFactorial.Evaluate(harshEval, ref pEvalData);
                if (ExNumber.IsUndef(nFactEval))
                    return ExNumber.GetUndefined();

                ExComp nMinusKFactEval = nMinusKFactorial.Evaluate(harshEval, ref pEvalData);
                if (ExNumber.IsUndef(nMinusKFactEval))
                    return ExNumber.GetUndefined();

                return Operators.DivOp.StaticCombine(nFactEval, nMinusKFactEval);
            }

            return this;
        }

        public override string ToAsciiString()
        {
            return IDEN + "(" + GetTop().ToAsciiString() + ", " + GetBottom().ToAsciiString() + ")";
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
            return IDEN + "(" + GetTop().ToTexString() + ", " + GetBottom().ToTexString() + ")";
        }

        public override string FinalToAsciiString()
        {
            return IDEN + "(" + GetTopTerm().FinalToAsciiString() + ", " + GetBottomTerm().FinalToAsciiString() + ")";
        }

        public override string FinalToTexString()
        {
            return IDEN + "( " + GetTopTerm().FinalToTexString() + ", " + GetBottomTerm().FinalToTexString() + ")";
        }
    }
}