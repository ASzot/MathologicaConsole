namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class ChooseFunction : AppliedFunction_NArgs
    {
        private const string IDEN = "C";

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

        public ChooseFunction(ExComp top, ExComp bottom)
            : base(FunctionType.ChooseFunction, typeof(ChooseFunction),
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

                FactorialFunction kFactorial = new FactorialFunction(k);

                FactorialFunction nMinusKFactorial = new FactorialFunction(Operators.SubOp.StaticCombine(n, k));

                ExComp nFactEval = nFactorial.Evaluate(harshEval, ref pEvalData);
                if (ExNumber.IsUndef(nFactEval))
                    return ExNumber.GetUndefined();

                ExComp kFactEval = kFactorial.Evaluate(harshEval, ref pEvalData);
                if (ExNumber.IsUndef(kFactEval))
                    return ExNumber.GetUndefined();

                ExComp nMinusKFactEval = nMinusKFactorial.Evaluate(harshEval, ref pEvalData);
                if (ExNumber.IsUndef(nMinusKFactEval))
                    return ExNumber.GetUndefined();

                ExComp divBy = Operators.MulOp.StaticCombine(kFactEval, nMinusKFactEval);
                return Operators.DivOp.StaticCombine(nFactEval, divBy);
            }

            return this;
        }

        public override string ToAsciiString()
        {
            return "((" + GetTop().ToAsciiString() + "), (" + GetBottom().ToAsciiString() + "))";
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
            return "((" + GetTop().ToTexString() + "), (" + GetBottom().ToTexString() + "))";
        }

        public override string FinalToAsciiString()
        {
            return "((" + GetTopTerm().FinalToAsciiString() + "), (" + GetBottomTerm().FinalToAsciiString() + "))";
        }

        public override string FinalToTexString()
        {
            return "((" + GetTopTerm().FinalToTexString() + "), (" + GetBottomTerm().FinalToTexString() + "))";
        }
    }
}