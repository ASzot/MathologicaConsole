using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    class CrossProductOp : AgOp
    {
        public const string IDEN = "cross";

        public override ExComp Clone()
        {
            return new CrossProductOp();
        }

        public override string ToAsciiString()
        {
            return "\\" + IDEN;
        }

        public override string ToTexString()
        {
            return "\\" + IDEN;
        }

        public override string ToString()
        {
            return "\\" + IDEN;
        }

        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (!(ex1 is ExVector) || !(ex2 is ExVector))
                return Number.Undefined;

            ExVector vec0 = ex1 as ExVector;
            ExVector vec1 = ex2 as ExVector;

            if (vec0.Length != vec1.Length)
                return Number.Undefined;

            if (vec0.Length == 1)
                return Number.Undefined;

            if (vec0.Length != 2 || vec1.Length != 3)
                return StaticWeakCombine(vec0, vec1);

            // The formula is a2b3-a3b2,a3b1-a1b3,a1b2-a2b1

            ExComp a1 = vec0.Get(0);
            ExComp a2 = vec0.Get(1);
            ExComp a3 = vec0.Length > 2 ? vec0.Get(2) : Number.Zero;

            ExComp b1 = vec1.Get(0);
            ExComp b2 = vec1.Get(1);
            ExComp b3 = vec1.Length > 2 ? vec1.Get(2) : Number.Zero;

            ExComp x = SubOp.StaticCombine(MulOp.StaticCombine(a2, b3), MulOp.StaticCombine(a3, b2));
            ExComp y = SubOp.StaticCombine(MulOp.StaticCombine(a3, b1), MulOp.StaticCombine(a1, b3));
            ExComp z = SubOp.StaticCombine(MulOp.StaticCombine(a1, b2), MulOp.StaticCombine(a2, b1));

            return new ExVector(x, y, z);
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            return new AlgebraTerm(ex1, new CrossProductOp(), ex2);
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            if (!(ex1 is ExVector) || !(ex2 is ExVector))
                return null;

            ExVector vec0 = ex1 as ExVector;
            ExVector vec1 = ex2 as ExVector;
            if (vec0.Length != vec1.Length)
                return null;
            
            return StaticWeakCombine(ex1, ex2);
        }
    }
}
