using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class ChooseFunction : AppliedFunction_NArgs
    {
        private const string IDEN = "C";
        public ExComp Bottom
        {
            get { return _args[1]; }
            set { _args[1] = value; }
        }

        public ExComp Top
        {
            get { return _args[0]; }
            set { _args[0] = value; }
        }

        public AlgebraTerm TopTerm
        {
            get { return Top.ToAlgTerm(); }
        }

        public AlgebraTerm BottomTerm
        {
            get { return Bottom.ToAlgTerm(); }
        }

        public ChooseFunction(ExComp top, ExComp bottom)
            : base(FunctionType.ChooseFunction, typeof(ChooseFunction),
            top is AlgebraTerm ? (top as AlgebraTerm).RemoveRedundancies() : top,
            bottom is AlgebraTerm ? (bottom as AlgebraTerm).RemoveRedundancies() : bottom)
        {
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            ExComp bottom, top;
            if (Bottom is AlgebraTerm)
                bottom = (Bottom as AlgebraTerm).ConvertImaginaryToVar();
            else
                bottom = Bottom;
            if (Top is AlgebraTerm)
                top = (Top as AlgebraTerm).ConvertImaginaryToVar();
            else
                top = Top;

            return new ChooseFunction(top, bottom);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp n = Top;
            ExComp k = Bottom;

            if (n is Number && k is Number && (n as Number).IsRealInteger() && (k as Number).IsRealInteger())
            {
                FactorialFunction nFactorial = new FactorialFunction(n);

                FactorialFunction kFactorial = new FactorialFunction(k);

                FactorialFunction nMinusKFactorial = new FactorialFunction(Operators.SubOp.StaticCombine(n, k));

                ExComp nFactEval = nFactorial.Evaluate(harshEval, ref pEvalData);
                if (Number.IsUndef(nFactEval))
                    return Number.Undefined;

                ExComp kFactEval = kFactorial.Evaluate(harshEval, ref pEvalData);
                if (Number.IsUndef(kFactEval))
                    return Number.Undefined;

                ExComp nMinusKFactEval = nMinusKFactorial.Evaluate(harshEval, ref pEvalData);
                if (Number.IsUndef(nMinusKFactEval))
                    return Number.Undefined;

                ExComp divBy = Operators.MulOp.StaticCombine(kFactEval, nMinusKFactEval);
                return Operators.DivOp.StaticCombine(nFactEval, divBy);
            }

            return this;
        }

        public override string ToAsciiString()
        {
            return "(_{" + Top.ToAsciiString() + "} " + IDEN + " _{" + Bottom.ToAsciiString() + "})";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return ToTexString();
        }

        public override string ToTexString()
        {
            return "(_{" + Top.ToTexString() + "} " + IDEN + " _{" + Bottom.ToTexString() + "})";
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return "_{" + TopTerm.FinalToAsciiKeepFormatting() + "} " + IDEN + 
                " _{" + BottomTerm.FinalToAsciiKeepFormatting() + "}";
        }

        public override string FinalToAsciiString()
        {
            return "_{" + TopTerm.FinalToAsciiString() + "} " + IDEN +
                " _{" + BottomTerm.FinalToAsciiString() + "}";
        }

        public override string FinalToTexKeepFormatting()
        {
            return "_{" + TopTerm.FinalToTexKeepFormatting() + "} " + IDEN +
                " _{" + BottomTerm.FinalToTexKeepFormatting() + "}";
        }

        public override string FinalToTexString()
        {
            return "_{" + TopTerm.FinalToTexString() + "} " + IDEN +
                " _{" + BottomTerm.FinalToTexString() + "}";
        }
    }
}