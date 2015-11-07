using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class AddOp : AgOp
    {
        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveRedundancies(false);
            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveRedundancies(false);

            if (ExNumber.IsUndef(ex1) || ExNumber.IsUndef(ex2))
                return ExNumber.GetUndefined();

            if (ExNumber.GetZero().IsEqualTo(ex1))
                return ex2;
            if (ExNumber.GetZero().IsEqualTo(ex2))
                return ex1;

            if ((ExNumber.GetPosInfinity().IsEqualTo(ex1) && ExNumber.GetNegInfinity().IsEqualTo(ex2)) ||
                (ExNumber.GetPosInfinity().IsEqualTo(ex2) && ExNumber.GetNegInfinity().IsEqualTo(ex1)))
                return ExNumber.GetZero();
            if (ExNumber.GetNegInfinity().IsEqualTo(ex1) || ExNumber.GetNegInfinity().IsEqualTo(ex2))
                return ExNumber.GetNegInfinity();
            if (ExNumber.GetPosInfinity().IsEqualTo(ex1) || ExNumber.GetPosInfinity().IsEqualTo(ex2))
                return ExNumber.GetPosInfinity();

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
                ExComp addedFuncs = AlgebraFunction.OpAdd(func1, func2);
                return addedFuncs;
            }
            else if (ex1 is AlgebraTerm && ex2 is AlgebraTerm)
            {
                AlgebraTerm term1 = ex1 as AlgebraTerm;
                AlgebraTerm term2 = ex2 as AlgebraTerm;

                if (term1.GetTermCount() == 0)
                    return term2;
                if (term2.GetTermCount() == 0)
                    return term1;

                ExComp intersect = AlgebraTerm.Intersect(term1, term2);

                if (intersect is AlgebraTerm && (intersect as AlgebraTerm).GetTermCount() == 0)
                    return ExNumber.GetZero();

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
            else if ((ex1 is AlgebraTerm && ex2 is ExNumber) ||
                (ex1 is ExNumber && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                ExNumber num = ex1 is ExNumber ? ex1 as ExNumber : ex2 as ExNumber;
                if (ExNumber.OpEqual(num, 0.0))
                    return term;

                ExComp intersect = AlgebraTerm.Intersect(term, num);
                return intersect;
            }
            else if (ex1 is ExNumber && ex2 is ExNumber)
            {
                ExNumber n1 = ex1 as ExNumber;
                ExNumber n2 = ex2 as ExNumber;
                ExNumber result = ExNumber.OpAdd(n1, n2);
                return result;
            }
            else if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp c1 = ex1 as AlgebraComp;
                AlgebraComp c2 = ex2 as AlgebraComp;

                if (c1.IsEqualTo(c2))
                {
                    AlgebraTerm term = new AlgebraTerm();
                    term.Add(new ExNumber(2.0), new MulOp(), c1);

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

        public override ExComp CloneEx()
        {
            return new AddOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
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