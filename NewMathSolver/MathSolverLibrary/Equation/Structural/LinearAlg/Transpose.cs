using MathSolverWebsite.MathSolverLibrary.Equation.Functions;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    internal class Transpose : AppliedFunction
    {
        public Transpose(ExComp exMat)
            : base(exMat, FunctionType.Transpose, typeof(Transpose))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            ExMatrix mat = GetInnerEx() as ExMatrix;
            if (mat == null)
                return ExNumber.GetUndefined();

            return mat.Transpose();
        }

        public override string FinalToAsciiString()
        {
            return GetInnerTerm().FinalToAsciiString() + "^{T}";
        }

        public override string FinalToTexString()
        {
            return GetInnerTerm().FinalToTexString() + "^{T}";
        }

        public override string ToAsciiString()
        {
            return GetInnerTerm().ToAsciiString() + "^{T}";
        }

        public override string ToTexString()
        {
            return ToTexString();
        }
    }
}