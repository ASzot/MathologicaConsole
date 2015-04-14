using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    class ExVector : ExMatrix
    {
        public int Length
        {
            get { return base.Cols; }
        }

        public ExComp[] Components
        {
            get
            {
                return base._exData[0];
            }
        }

        public ExVector(int length)
            : base(1, length)
        {

        }

        public ExVector(params ExComp[] exs)
            : base(exs)
        {

        }

        public ExComp Get(int index)
        {
            return Get(0, index);
        }

        public void Set(int index, ExComp val)
        {
            Set(0, index, val);
        }

        public static ExComp Dot(ExVector vec0, ExVector vec1)
        {
            if (vec0.Length != vec1.Length)
                return Number.Undefined;

            ExComp totalSum = Number.Zero;
            for (int i = 0; i < vec0.Length; ++i)
            {
                ExComp prod = MulOp.StaticCombine(vec0.Get(i), vec1.Get(i));
                totalSum = AddOp.StaticCombine(prod, totalSum);
            }
            return totalSum;
        }

        public ExComp GetVecLength()
        {
            ExComp sum = Number.Zero;
            for (int i = 0; i < this.Length; ++i)
            {
                sum = AddOp.StaticCombine(Get(i), sum);
            }

            return PowOp.StaticCombine(sum, AlgebraTerm.FromFraction(Number.One, new Number(2.0)));
        }

        public ExVector Normalize()
        {
            ExVector vec = new ExVector(this.Length);
            ExComp vecLength = GetVecLength();

            for (int i = 0; i < this.Length; ++i)
            {
                ExComp setVal = DivOp.StaticCombine(this.Get(i), vecLength);
                vec.Set(i, setVal);
            }

            return vec;
        }
    }
}
