namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class PermutationOp : AgOp
    {
        public override ExComp CloneEx()
        {
            return new PermutationOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return new Functions.PermutationFunction(ex1, ex2);
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return new Functions.PermutationFunction(ex1, ex2);
        }

        public override string ToAsciiString()
        {
            return "_P_";
        }

        public override string ToTexString()
        {
            return "_P_";
        }

        public override string ToString()
        {
            return "_P_";
        }
    }
}