using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal abstract class BasicAppliedFunc : AppliedFunction
    {
        protected string _useEnd = ")";
        protected string _useStart = "(";
        protected string s_name;

        public virtual string GetFuncName()
        {
            return s_name;
        }

        public BasicAppliedFunc(ExComp innerEx, string name, FunctionType ft, Type type)
            : base(innerEx, ft, type)
        {
            s_name = name;
        }

        public static ExComp Parse(string parseStr, ExComp innerEx, ref List<string> pParseErrors)
        {
            if (parseStr == "sin")
                return new SinFunction(innerEx);
            else if (parseStr == "cos")
                return new CosFunction(innerEx);
            else if (parseStr == "tan")
                return new TanFunction(innerEx);
            else if (parseStr == "log")
                return new LogFunction(innerEx);   // By default we are log base 10.
            else if (parseStr == "ln")
            {
                LogFunction log = new LogFunction(innerEx);
                log.SetBase(Constant.ParseConstant("e"));
                return log;
            }
            else if (parseStr == "sec")
                return new SecFunction(innerEx);
            else if (parseStr == "csc")
                return new CscFunction(innerEx);
            else if (parseStr == "cot")
                return new CotFunction(innerEx);
            else if (parseStr == "asin" || parseStr == "arcsin")
                return new ASinFunction(innerEx);
            else if (parseStr == "acos" || parseStr == "arccos")
                return new ACosFunction(innerEx);
            else if (parseStr == "atan" || parseStr == "arctan")
                return new ATanFunction(innerEx);
            else if (parseStr == "acsc" || parseStr == "arccsc")
                return new ACscFunction(innerEx);
            else if (parseStr == "asec" || parseStr == "arcsec")
                return new ASecFunction(innerEx);
            else if (parseStr == "acot" || parseStr == "arccot")
                return new ACotFunction(innerEx);
            else if (parseStr == "sqrt")
                return new AlgebraTerm(innerEx, new Operators.PowOp(), new AlgebraTerm(ExNumber.GetOne(), new Operators.DivOp(), new ExNumber(2.0)));
            else if (parseStr == "det")
                return new Structural.LinearAlg.Determinant(innerEx);
            else if (parseStr == "curl")
                return new CurlFunc(innerEx);
            else if (parseStr == "div")
                return new DivergenceFunc(innerEx);
            else if (parseStr == "!")
                return new FactorialFunction(innerEx);

            return null;
        }

        public override string FinalToDispStr()
        {
            return s_name + _useStart + GetInnerTerm().FinalToDispStr() + _useEnd;
        }

        public override string ToAsciiString()
        {
            return s_name + _useStart + GetInnerTerm().ToAsciiString() + _useEnd;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (GetInnerTerm() == null)
                return null;
            return "Math." + s_name + "(" + innerStr + ")";
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return s_name + _useStart + GetInnerTerm().ToString() + _useEnd;
        }

        public override string ToTexString()
        {
            return s_name + _useStart + GetInnerTerm().ToTexString() + _useEnd;
        }
    }
}