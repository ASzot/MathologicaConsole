using MathSolverWebsite.MathSolverLibrary.Equation.Functions;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    internal class MatrixInverse : AppliedFunction
    {
        public MatrixInverse(ExComp exMat)
            : base(exMat, FunctionType.MatInverse, typeof(MatrixInverse))
        {
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            ExMatrix mat = GetInnerEx() as ExMatrix;
            if (mat == null)
                return ExNumber.GetUndefined();

            return mat.GetInverse();
        }

        public override string FinalToAsciiString()
        {
            return GetInnerTerm().FinalToAsciiString() + "^{-1}";
        }

        public override string FinalToTexString()
        {
            return GetInnerTerm().FinalToTexString() + "^{-1}";
        }

        public override string ToAsciiString()
        {
            return GetInnerTerm().ToAsciiString() + "^{-1}";
        }

        public override string ToTexString()
        {
            return ToTexString();
        }
    }
}