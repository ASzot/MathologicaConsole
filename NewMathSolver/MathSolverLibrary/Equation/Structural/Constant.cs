using System;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class Constant : AlgebraComp
    {
        private static Constant[] _definedConstants =
        {
            new Constant("e", Math.E),
            new Constant("pi", Math.PI),
        };

        private readonly double d_value;

        public static Constant E
        {
            get { return ParseConstant("e"); }
        }

        public static Constant Pi
        {
            get { return ParseConstant("pi"); }
        }

        public static AlgebraTerm TwoPi
        {
            get { return new AlgebraTerm(new Number(2.0), new Operators.MulOp(), Pi); }
        }

        public Number Value
        {
            get { return new Number(d_value); }
        }

        public Constant(string iden, double value)
            : base(iden)
        {
            d_value = value;
        }

        public static Constant ParseConstant(string parseStr)
        {
            foreach (Constant constant in _definedConstants.ToList())
            {
                if (constant.Var.Var == parseStr)
                    return constant;
            }

            return null;
        }

        public override ExComp Clone()
        {
            return new Constant(_var.Var, d_value);
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
            return "C(" + Var.ToString() + ")";
        }
    }
}