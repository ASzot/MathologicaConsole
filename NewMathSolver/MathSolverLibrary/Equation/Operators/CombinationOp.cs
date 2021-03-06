﻿namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class CombinationOp : AgOp
    {
        public override ExComp CloneEx()
        {
            return new PermutationOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return new Functions.ChooseFunction(ex1, ex2);
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return new Functions.ChooseFunction(ex1, ex2);
        }

        public override string ToAsciiString()
        {
            return "_C_";
        }

        public override string ToTexString()
        {
            return "_C_";
        }

        public override string ToString()
        {
            return "_C_";
        }
    }
}