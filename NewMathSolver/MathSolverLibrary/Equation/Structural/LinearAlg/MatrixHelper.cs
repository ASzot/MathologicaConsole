using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    static class MatrixHelper
    {
        public static ExMatrix CreateMatrix(List<ExComp> exs)
        {
            if (exs.Count == 0)
                return null;

            bool allMatrices = true;
            bool containsMatrix = false;
            foreach (var ex in exs)
            {
                if (ex is ExMatrix)
                    containsMatrix = true;
                else
                    allMatrices = false;
            }

            if (containsMatrix && !allMatrices)
                return null;

            if (allMatrices)
            {
                // Create a matrix.
                ExComp[][] vectors = new ExComp[exs.Count][];
                for (int i = 0; i < exs.Count(); ++i)
                {
                    // Too many dimensions...
                    if (!(exs[i] is ExVector))
                        return null;
                    ExVector vec = exs[i] as ExVector;
                    if (i != 0 && vectors[i - 1].Length != vec.Length)
                        return null;
                    vectors[i] = vec.Components;
                }

                return new ExMatrix(vectors);
            }

            return new ExVector(exs.ToArray());
        }

        public static ExComp AdOpCombine(ExMatrix mat0, ExComp ex)
        {
            if (!(ex is ExMatrix))
                return null;

            ExMatrix mat1 = ex as ExMatrix;

            if (mat0.Cols != mat1.Cols || mat0.Rows != mat1.Rows)
                return null;

            int m = mat0.Rows;
            int n = mat0.Cols;
            ExMatrix finalMat = new ExMatrix(m, n);
            for (int i = 0; i < m; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    finalMat.Set(i, j, AddOp.StaticCombine(mat0.Get(i, j), mat1.Get(i, j)));
                }
            }

            return finalMat;
        }

        public static ExComp MulOpCombine(ExMatrix mat0, ExComp ex)
        {
            if (!(ex is ExMatrix))
            {
                mat0.ModifyEach((ExComp ele) =>
                {
                    return MulOp.StaticCombine(ele, ex);
                });
                return mat0;
            }

            if (mat0 is ExVector && ex is ExVector)
            {
                return ExVector.Dot(mat0 as ExVector, ex as ExVector);
            }

            ExMatrix mat1 = ex as ExMatrix;
            
            // Matrix multiplication.
            if (mat0.Cols != mat1.Rows)
                return Number.Undefined;

            ExMatrix resultant = new ExMatrix(mat0.Rows, mat1.Cols);

            for (int i = 0; i < mat0.Rows; ++i)
            {
                ExVector rowVecMat0 = mat0.GetRowVec(i);
                for (int j = 0; j < mat1.Cols; ++j)
                {
                    ExVector colVecMat1 = mat1.GetColVec(j);
                    ExComp matEntry = ExVector.Dot(rowVecMat0, colVecMat1);
                    resultant.Set(i, j, matEntry);
                }
            }

            return resultant;
        }

        public static ExComp DivOpCombine(ExMatrix mat, ExComp ex)
        {
            if (ex is Number)
            {
                Number n = ex as Number;
                // Divide each component by n.
                mat.ModifyEach((ExComp ele) =>
                    {
                        return DivOp.StaticCombine(ele, n);
                    });
                return mat;
            }
            return null;
        }
    }
}
