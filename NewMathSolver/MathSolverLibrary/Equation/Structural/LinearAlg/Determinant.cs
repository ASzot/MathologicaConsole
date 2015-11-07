using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    internal class Determinant : AppliedFunction
    {
        private const int MAX_DET_DIMEN = 4;

        public Determinant(ExComp innerMat)
            : base(innerMat, FunctionType.Deteriment, typeof(Determinant))
        {
        }

        public static ExComp TakeDeteriment(ExMatrix mat)
        {
            if (mat.GetRows() == 2)
            {
                ExComp a = mat.Get(0, 0);
                ExComp b = mat.Get(0, 1);
                ExComp c = mat.Get(1, 0);
                ExComp d = mat.Get(1, 1);

                return SubOp.StaticCombine(MulOp.StaticCombine(a, d), MulOp.StaticCombine(b, c));
            }

            ExComp total = ExNumber.GetZero();
            for (int i = 0; i < mat.GetCols(); ++i)
            {
                ExComp factor = mat.Get(0, i);
                ExComp cofactor = mat.GetCofactor(0, i);
                ExComp comp = MulOp.StaticCombine(factor, cofactor);

                total = AddOp.StaticCombine(total, comp);
            }

            return total;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            ExComp innerEx = GetInnerEx();

            ExMatrix mat = innerEx as ExMatrix;
            if (mat == null || !mat.GetIsSquare())
            {
                pEvalData.AddMsg("Only the deteriment of square matrices can be taken");
                return ExNumber.GetUndefined();
            }

            if (mat.GetRows() > MAX_DET_DIMEN)
                return this;

            return TakeDeteriment(mat);
        }

        public override string FinalToAsciiString()
        {
            return "\\text{det}" + GetInnerTerm().FinalToAsciiString();
        }

        public override string FinalToTexString()
        {
            return "\\text{det}" + GetInnerTerm().FinalToTexString();
        }

        public override string ToTexString()
        {
            return "\\text{det}" + GetInnerEx().ToTexString();
        }

        public override string ToAsciiString()
        {
            return "\\text{det}" + GetInnerEx().ToAsciiString();
        }

        public override string ToString()
        {
            return ToTexString();
        }
    }
}