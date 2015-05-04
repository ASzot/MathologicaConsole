using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;

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
            foreach (ExComp ex in exs)
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

        public static ExComp PowOpCombine(ExMatrix mat, ExComp ex)
        {
            if (ex is ExMatrix)
                return null;

            if (ex is AlgebraComp && (ex as AlgebraComp).Var.Var == "T")
            {
                // This is the transpose operation.
                return mat.Transpose();
            }

            mat.ModifyEach((ExComp comp) =>
                {
                    return PowOp.StaticCombine(comp, ex);
                });

            return mat;
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
            if (m == 1)
                return finalMat.GetRowVec(0);
            return finalMat;
        }

        public static ExComp MulOpCombine(ExMatrix mat0, ExComp ex)
        {
            if (!(ex is ExMatrix))
            {

                if (ex is AlgebraTerm)
                {
                    AlgebraTerm term = ex as AlgebraTerm;
                    AlgebraTerm[] numDen = term.GetNumDenFrac();
                    if (numDen == null)
                        return null;

                    if (!(numDen[0].RemoveRedundancies() is Number) ||
                        !(numDen[1].RemoveRedundancies() is Number))
                        return null;
                }
                else if (!(ex is Number))
                    return null;
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

        public static bool TermContainsMatrices(ExComp ex)
        {
            if (ex is ExMatrix)
                return true;
            if (ex is PowerFunction)
            {
                PowerFunction pf = ex as PowerFunction;
                return TermContainsMatrices(pf.Base) || TermContainsMatrices(pf.Power);
            }
            else if (ex is LogFunction)
            {
                LogFunction log = ex as LogFunction;
                return TermContainsMatrices(log.Base) || TermContainsMatrices(log.InnerTerm);
            }
            else if (ex is ChooseFunction)
            {
                ChooseFunction choose = ex as ChooseFunction;
                return TermContainsMatrices(choose.Bottom) || TermContainsMatrices(choose.Top);
            }
            else if (ex is AlgebraTerm)
            {
                AlgebraTerm term = ex as AlgebraTerm;
                foreach (var subComp in term.SubComps)
                {
                    if (TermContainsMatrices(subComp))
                        return true;
                }
            }

            return false;
        }
    }
}
