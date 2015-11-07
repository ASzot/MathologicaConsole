namespace MathSolverWebsite.MathSolverLibrary.Parsing
{
    public enum LexemeType
    {
        Operator,
        EqualsOp,
        Function,
        Identifier,
        Number,
        I_Number,
        StartPara,
        EndPara,
        StartBracket,
        EndBracket,
        Bar,
        StartBar,
        EndBar,
        Constant,
        EquationSeperator,
        FunctionDef,

        Comma,

        Derivative,
        FuncDeriv,
        Integral,
        Summation,
        Limit,
        Differential,

        VectorStore,
        MultiVarFuncStore,

        Greater,
        GreaterEqual,
        Less,
        LessEqual,

        FuncIden,
        FuncArgStart,
        FuncArgEnd,

        Infinity,

        ErrorType,
    };

    internal static class LexemeTypeHelper
    {
        public static LexemeType GetOpposite(LexemeType lt)
        {
            switch (lt)
            {
                case LexemeType.StartPara:
                    return LexemeType.EndPara;

                case LexemeType.StartBracket:
                    return LexemeType.EndBracket;

                case LexemeType.EndBracket:
                    return LexemeType.StartBracket;

                case LexemeType.EndPara:
                    return LexemeType.StartPara;

                case LexemeType.Bar:
                    return LexemeType.Bar;

                case LexemeType.FuncArgStart:
                    return LexemeType.FuncArgEnd;

                case LexemeType.FuncArgEnd:
                    return LexemeType.FuncArgStart;

                default:
                    return LexemeType.ErrorType;
            }
        }
    }
}