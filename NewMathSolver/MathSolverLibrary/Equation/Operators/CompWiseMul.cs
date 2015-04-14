using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    class CompWiseMul : AgOp
    {
        public const string IDEN = "CompWiseMul";

        public override ExComp Clone()
        {
            return new CompWiseMul();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            if (!(ex1 is ExVector) || !(ex2 is ExVector))
                return MulOp.StaticCombine(ex1, ex2);

            ExVector vec0 = ex1 as ExVector;
            ExVector vec1 = ex2 as ExVector;

            if (vec0.Length != vec1.Length)
                return Number.Undefined;

            ExVector vec = new ExVector(vec0.Length);

            for (int i = 0; i < vec0.Length; ++i)
            {
                vec.Set(i, MulOp.StaticCombine(vec0.Get(i), vec1.Get(i)));
            }

            return vec;
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            if (!(ex1 is ExVector) || !(ex2 is ExVector))
                return MulOp.StaticCombine(ex1, ex2);
            return new AlgebraTerm(ex1, new CompWiseMul(), ex2);
        }

        public override string ToAsciiString()
        {
            return "";
        }

        public override string ToTexString()
        {
            return "";
        }
    }
}
