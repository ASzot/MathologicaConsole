using System;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class AddOp : AgOp
    {
        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveRedundancies();
            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveRedundancies();

            if (Number.IsUndef(ex1) || Number.IsUndef(ex2))
                return Number.Undefined;

            if (Number.Zero.IsEqualTo(ex1))
                return ex2;
            if (Number.Zero.IsEqualTo(ex2))
                return ex1;

            if (ex1 is ExMatrix || ex2 is ExMatrix)
            {
                ExMatrix mat;
                ExComp other;
                if (ex1 is ExMatrix)
                {
                    mat = ex1 as ExMatrix;
                    other = ex2;
                }
                else
                {
                    mat = ex2 as ExMatrix;
                    other = ex1;
                }
                ExComp combineAtmpt = MatrixHelper.AdOpCombine(mat, other);
                if (combineAtmpt != null)
                    return combineAtmpt;

                // Order has to be preserved with vectors.
                return AddOp.StaticWeakCombine(ex1, ex2);
            }
            if (ex1 is AlgebraFunction && ex2 is AlgebraFunction)
            {
                AlgebraFunction func1 = ex1 as AlgebraFunction;
                AlgebraFunction func2 = ex2 as AlgebraFunction;
                ExComp addedFuncs = func1 + func2;
                return addedFuncs;
            }
            else if (ex1 is AlgebraTerm && ex2 is AlgebraTerm)
            {
                AlgebraTerm term1 = ex1 as AlgebraTerm;
                AlgebraTerm term2 = ex2 as AlgebraTerm;

                if (term1.TermCount == 0)
                    return term2;
                if (term2.TermCount == 0)
                    return term1;

                ExComp intersect = AlgebraTerm.Intersect(term1, term2);

                if (intersect is AlgebraTerm && (intersect as AlgebraTerm).TermCount == 0)
                    return Number.Zero;

                return intersect;
            }
            else if ((ex1 is AlgebraTerm && !(ex1 is AlgebraFunction) && ex2 is AlgebraComp) ||
                (ex1 is AlgebraComp && !(ex2 is AlgebraFunction) && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                AlgebraComp comp = ex1 is AlgebraComp ? ex1 as AlgebraComp : ex2 as AlgebraComp;

                ExComp intersect = AlgebraTerm.Intersect(term, comp);
                return intersect;
            }
            else if ((ex1 is AlgebraTerm && ex2 is Number) ||
                (ex1 is Number && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                Number num = ex1 is Number ? ex1 as Number : ex2 as Number;
                if (num == 0.0)
                    return term;

                ExComp intersect = AlgebraTerm.Intersect(term, num);
                return intersect;
            }
            else if (ex1 is Number && ex2 is Number)
            {
                Number n1 = ex1 as Number;
                Number n2 = ex2 as Number;
                Number result = n1 + n2;
                return result;
            }
            else if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp c1 = ex1 as AlgebraComp;
                AlgebraComp c2 = ex2 as AlgebraComp;

                if (c1 == c2)
                {
                    AlgebraTerm term = new AlgebraTerm();
                    term.Add(new Number(2.0), new MulOp(), c1);

                    return term;
                }
            }

            AlgebraTerm algebraTerm = new AlgebraTerm();
            algebraTerm.Add(ex1, new AddOp(), ex2);

            return algebraTerm;
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            AlgebraTerm term = new AlgebraTerm(ex1, new AddOp(), ex2);
            return term;
        }

        public override ExComp Clone()
        {
            return new AddOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override int GetHashCode()
        {
            return (int)((double)"Add".GetHashCode() * Math.E);
        }

        public override string ToString()
        {
            return "+";
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return StaticWeakCombine(ex1, ex2);
        }
    }
}