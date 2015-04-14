using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    class Determinant : AppliedFunction
    {
        private const int MAX_DET_DIMEN = 4;

        public Determinant(ExMatrix innerMat)
            : base(innerMat, FunctionType.Deteriment, typeof(Determinant))
        {

        }

        private ExComp TakeDeteriment(ExMatrix mat)
        {
            if (mat.Rows == 2)
            {
                ExComp a = mat.Get(0, 0);
                ExComp b = mat.Get(0, 1);
                ExComp c = mat.Get(1, 0);
                ExComp d = mat.Get(1, 1);

                return SubOp.StaticCombine(MulOp.StaticCombine(a, c), MulOp.StaticCombine(b, d));
            }

            ExComp total = Number.Zero;
            for (int i = 0; i < mat.Cols; ++i)
            {
                ExComp cofactor = mat.Get(0, i);
                // Multiply by the matrix minor not including the current row or column.
                // Cancel the 0th row and the ith col.
                ExMatrix minor = mat.GetMatrixMinor(0, i);
                ExComp minorDet = TakeDeteriment(minor);

                ExComp comp = MulOp.StaticCombine(cofactor, minorDet);
                if (i != 0 && i % 2 != 0)
                    comp = MulOp.Negate(comp);

                total = AddOp.StaticCombine(total, comp);
            }

            return total;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = InnerEx;
            if (!(innerEx is ExMatrix))
                return Number.Undefined;

            ExMatrix mat = innerEx as ExMatrix;
            if (!mat.IsSquare)
            {
                pEvalData.AddMsg("Only the deteriment of square matrices can be taken");
                return Number.Undefined;
            }

            if (mat.Rows > MAX_DET_DIMEN)
                return this;

            return TakeDeteriment(mat);
        }

        public override string FinalToAsciiKeepFormatting()
        {
            return "\\text{det}" + InnerTerm.FinalToAsciiKeepFormatting();
        }

        public override string FinalToAsciiString()
        {
            return "\\text{det}" + InnerTerm.FinalToAsciiString();
        }

        public override string FinalToTexKeepFormatting()
        {
            return "\\text{det}" + InnerTerm.FinalToTexKeepFormatting();
        }

        public override string FinalToTexString()
        {
            return "\\text{det}" + InnerTerm.FinalToTexString();
        }

        public override string ToTexString()
        {
            return "\\text{det}" + InnerEx.ToTexString();
        }

        public override string ToAsciiString()
        {
            return "\\text{det}" + InnerEx.ToAsciiString();
        }

        public override string ToString()
        {
            return ToTexString();
        }
    }
}
