namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal enum InputType
    {
        Linear = 0,
        Cubic = 1,
        AbsoluteValue = 2,
        ExponentSolve = 3,
        PowerSolve = 4,
        TrigSolve = 5,
        RationalSolve = 6,
        PolySolve = 7,
        LogSolve = 8,

        LinearInequalities = 9,
        QuadInequalities = 10,
        PolyInequalities = 11,
        CompoundInequalities = 12,
        AbsoluteValueInequalites = 13,
        RationalInequalities = 14,

        FactorQuads = 15,
        SolveQuadsFactor = 16,
        SolveQuadsCTS = 17,
        SolveQuadsQE = 18,

        FunctionDomain = 19,
        FunctionInverse = 20,
        PolyDiv = 21,
        PolyFactor = 22,
        BinomTheorem = 23,

        SOE_Sub_2Var = 24,
        SOE_Sub_3Var = 25,
        SOE_Elim_2Var = 26,
        SOE_Elim_3Var = 27,

        Limits = 28,
        DerivImp = 29,
        DerivPoly = 31,
        DerivLog = 32,
        DerivTrig = 33,
        DerivInvTrig = 34,
        DerivExp = 35,

        IntPoly = 36,
        IntBasicFunc = 37,
        IntParts = 39,
        IntUSub = 40,

        PartialDerivative,
        LeHopital,

        Invalid,
    };

    internal enum InputAddType
    {
        DerivCR,
        IntDef,
        Invalid,
    };

    internal static class InputTypeHelper
    {
        public static InputType ToInequalityType(InputType inputType)
        {
            if (inputType == (InputType.Linear))
                return InputType.LinearInequalities;
            else if (inputType == (InputType.Cubic))
                return InputType.PolyInequalities;
            else if (inputType == (InputType.AbsoluteValue))
                return InputType.AbsoluteValueInequalites;
            else if (inputType == (InputType.SolveQuadsCTS) ||
                inputType == (InputType.SolveQuadsFactor) ||
                inputType == (InputType.SolveQuadsQE))
                return InputType.QuadInequalities;
            else if (inputType == (InputType.RationalSolve))
                return InputType.RationalInequalities;

            return InputType.Invalid;
        }

        /// <summary>
        /// Can return null if it is an invalid input type.
        /// </summary>
        /// <param name="inputType"></param>
        /// <param name="inputAddType"></param>
        /// <returns></returns>
        public static string ToDescStr(InputType inputType, InputAddType inputAddType)
        {
            string retStr;

            if (inputType == (InputType.Linear))
                retStr = "linear equations";
            else if (inputType == (InputType.Cubic))
                retStr = "cubic equations";
            else if (inputType == (InputType.AbsoluteValue))
                retStr = "absolute value equations";
            else if (inputType == (InputType.ExponentSolve))
                retStr = "exponent equations";
            else if (inputType == (InputType.PowerSolve))
                retStr = "power equations";
            else if (inputType == (InputType.TrigSolve))
                retStr = "trig equations";
            else if (inputType == (InputType.RationalSolve))
                retStr = "rational equations";
            else if (inputType == (InputType.LogSolve))
                retStr = "log equations";
            else if (inputType == (InputType.LinearInequalities))
                retStr = "linear inequalities";
            else if (inputType == (InputType.QuadInequalities))
                retStr = "quadratic inequalities";
            else if (inputType == (InputType.PolyInequalities))
                retStr = "polynomial inequalities";
            else if (inputType == (InputType.CompoundInequalities))
                retStr = "compound inequalities";
            else if (inputType == (InputType.AbsoluteValueInequalites))
                retStr = "absolute value inequalities";
            else if (inputType == (InputType.RationalInequalities))
                retStr = "rational inequalities";
            else if (inputType == (InputType.FactorQuads))
                retStr = "factoring quadratics";
            else if (inputType == (InputType.SolveQuadsFactor))
                retStr = "quadratic equations with factoring";
            else if (inputType == (InputType.SolveQuadsCTS))
                retStr = "quadratic equations with completing the square";
            else if (inputType == (InputType.SolveQuadsQE))
                retStr = "quadratic equations with quadratic equation";
            else if (inputType == (InputType.FunctionDomain))
                retStr = "function domain";
            else if (inputType == (InputType.FunctionInverse))
                retStr = "function inverse";
            else if (inputType == (InputType.PolyDiv))
                retStr = "polynomial division";
            else if (inputType == (InputType.PolyFactor))
                retStr = "polynomial factoring";
            else if (inputType == (InputType.BinomTheorem))
                retStr = "binomial theorem";
            else if (inputType == (InputType.SOE_Sub_2Var))
                retStr = "system of equations using substitution with two variables";
            else if (inputType == (InputType.SOE_Sub_3Var))
                retStr = "system of equations using substitution with three variables";
            else if (inputType == (InputType.SOE_Elim_2Var))
                retStr = "system of equations using elimination with two variables";
            else if (inputType == (InputType.SOE_Elim_3Var))
                retStr = "system of equations using elimination with three variables";
            else if (inputType == (InputType.Limits))
                retStr = "limits";
            else if (inputType == (InputType.DerivExp))
                retStr = "the derivative of an exponent";
            else if (inputType == (InputType.DerivImp))
                retStr = "implicit differentiation";
            else if (inputType == (InputType.DerivInvTrig))
                retStr = "the derivative of an inverse trig function";
            else if (inputType == (InputType.DerivLog))
                retStr = "the derivative of a log function";
            else if (inputType == (InputType.DerivPoly))
                retStr = "the derivative of a polynomial";
            else if (inputType == (InputType.DerivTrig))
                retStr = "the derivative of a trig function";
            else if (inputType == (InputType.IntBasicFunc))
                retStr = "integration of basic functions";
            else if (inputType == (InputType.IntParts))
                retStr = "integration by parts";
            else if (inputType == (InputType.IntPoly))
                retStr = "integration of polynomials";
            else if (inputType == (InputType.IntUSub))
                retStr = "integration with u-substitution";
            else if (inputType == InputType.PartialDerivative)
                retStr = "partial derivatives";
            else if (inputType == InputType.LeHopital)
                retStr = "L'Hospital's rule";
            else
                return null;

            if (inputAddType == InputAddType.DerivCR)
                return "use the chain rule with " + retStr;
            else if (inputAddType == InputAddType.IntDef)
                return "indefinite integration with " + retStr;

            return retStr;
        }
    }
}