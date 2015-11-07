namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.Polynomial
{
    internal class PolynomialGen
    {
        public static AlgebraTerm GenGenericOfDegree(int deg, AlgebraComp varFor, ref int alphaNumChar)
        {
            if (deg > 26)
                return null;

            AlgebraTerm finalTerm = new AlgebraTerm();
            for (int i = 0; i < deg + 1; ++i, ++alphaNumChar)
            {
                char alphaChar = (char)(alphaNumChar);
                AlgebraComp generic = new AlgebraComp(alphaChar.ToString());
                int reversedI = deg - i;

                ExComp raised = Operators.PowOp.StaticCombine(varFor, new ExNumber(reversedI));

                finalTerm.Add(Operators.MulOp.StaticCombine(generic, raised));

                if (i != deg)
                    finalTerm.Add(new Operators.AddOp());
            }

            return finalTerm;
        }
    }
}