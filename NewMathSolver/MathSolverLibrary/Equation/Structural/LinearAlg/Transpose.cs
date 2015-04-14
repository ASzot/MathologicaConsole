using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    class Transpose : AppliedFunction
    {
        public Transpose(ExMatrix exMat)
            : base(exMat, FunctionType.Transpose, typeof(Transpose))
        {

        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExMatrix mat = InnerEx as ExMatrix;
            if (mat == null)
                return Number.Undefined;

            return mat.Transpose();
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return InnerTerm.FinalToAsciiKeepFormatting() + "^{T}";
        }

        public override string FinalToAsciiString()
        {
            return InnerTerm.FinalToAsciiString() + "^{T}";
        }

        public override string FinalToTexKeepFormatting()
        {
            return InnerTerm.FinalToTexKeepFormatting() + "^{T}";
        }

        public override string FinalToTexString()
        {
            return InnerTerm.FinalToTexString() + "^{T}";
        }

        public override string ToAsciiString()
        {
            return InnerTerm.ToAsciiString() + "^{T}";
        }

        public override string ToTexString()
        {
            return ToTexString();
        }
    }
}
