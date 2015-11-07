using System;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class Constant : AlgebraComp
    {
        private static Constant[] _definedConstants =
            new Constant[]
        {
            new Constant("e", Math.E),
            new Constant("pi", Math.PI),
        };

        private readonly double d_value;

        public static Constant GetE()
        {
            return ParseConstant("e");
        }

        public static Constant GetPi()
        {
            return ParseConstant("pi");
        }

        public static AlgebraTerm GetTwoPi()
        {
            return new AlgebraTerm(new ExNumber(2.0), new Operators.MulOp(), GetPi());
        }

        public ExNumber GetValue()
        {
            return new ExNumber(d_value);
        }

        public Constant(string iden, double value)
            : base(iden)
        {
            d_value = value;
        }

        public static Constant ParseConstant(string parseStr)
        {
            foreach (Constant constant in _definedConstants)
            {
                if (constant.GetVar().GetVar() == parseStr)
                    return constant;
            }

            return null;
        }

        public override ExComp CloneEx()
        {
            return new Constant(_var.GetVar(), d_value);
        }

        public override double GetCompareVal()
        {
            // We want the compare value to behave like a number not a variable.
            return 0.0;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string baseStr = base.ToString();
            if (baseStr == null)
                return null;
            else if (baseStr == "e")
                return "Math.E";
            else if (baseStr == "pi")
                return "Math.PI";
            return d_value.ToString();
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "C(" + GetVar().ToString() + ")";
        }
    }
}