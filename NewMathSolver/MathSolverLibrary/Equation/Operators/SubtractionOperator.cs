using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class SubOp : AgOp
    {
        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            MulOp mo = new MulOp();
            ExComp negativeEx2 = mo.Combine(new ExNumber(-1.0), ex2);

            AddOp additionOperator = new AddOp();
            ExComp resultant = additionOperator.Combine(ex1, negativeEx2);

            return resultant;
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            ExComp negativeEx2 = MulOp.StaticCombine(new ExNumber(-1.0), ex2);
            ExComp resultant = AddOp.StaticWeakCombine(ex1, negativeEx2);

            return resultant;
        }

        public override ExComp CloneEx()
        {
            return new SubOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override string ToString()
        {
            return "-";
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return StaticWeakCombine(ex1, ex2);
        }
    }
}