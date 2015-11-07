namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class CalcConstant : AlgebraComp
    {
        public CalcConstant()
            : base("C")
        {
        }

        public override double GetCompareVal()
        {
            // Always goes at the end.

            return -1.0;
        }

        public override ExComp CloneEx()
        {
            return new CalcConstant();
        }
    }
}