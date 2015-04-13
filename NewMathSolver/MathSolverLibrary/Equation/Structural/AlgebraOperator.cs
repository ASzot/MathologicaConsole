using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal abstract class AgOp : ExComp
    {
        public static AgOp ParseOperator(string parseStr)
        {
            if (parseStr == "+")
                return new AddOp();
            else if (parseStr == "-")
                return new SubOp();
            else if (parseStr == "*")
                return new MulOp();
            else if (parseStr == "/")
                return new DivOp();
            else if (parseStr == "^")
                return new PowOp();
            else if (parseStr == "circ")
                return new DotOperator();
            else
                return null;
        }

        public abstract ExComp Combine(ExComp ex1, ExComp ex2);

        public override double GetCompareVal()
        {
            throw new NotImplementedException();
        }

        public override bool IsEqualTo(ExComp ex)
        {
            return this.GetType() == ex.GetType();
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return null;
        }

        public override string ToAsciiString()
        {
            return ToString();
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string str = ToString();
            if (!(str == "*" || str == "/" || str == "+" || str == "-"))
                return null;
            return str;
        }

        public override string ToTexString()
        {
            return ToString();
        }

        public abstract ExComp WeakCombine(ExComp ex1, ExComp ex2);
    }
}