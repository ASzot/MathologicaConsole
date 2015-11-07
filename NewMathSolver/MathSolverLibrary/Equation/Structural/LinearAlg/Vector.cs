using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    internal class ExVector : ExMatrix
    {
        public const string I = "\\vec{i}";
        public const string J = "\\vec{j}";
        public const string K = "\\vec{k}";

        public virtual int GetLength()
        {
            return base.GetCols();
        }

        public ExComp GetX()
        {
            return 0 < GetLength() ? Get(0) : ExNumber.GetZero();
        }

        public ExComp GetY()
        {
            return 1 < GetLength() ? Get(1) : ExNumber.GetZero();
        }

        public ExComp GetZ()
        {
            // Not all vectors will have Z component whereas
            // they are garunteed to have a least an x and y component.
            return 2 < GetLength() ? Get(2) : ExNumber.GetZero();
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

        public virtual ExComp Get(int index)
        {
            return Get(0, index);
        }

        public virtual void Set(int index, ExComp val)
        {
            Set(0, index, val);
        }

        public static ExComp Dot(ExVector vec0, ExVector vec1)
        {
            if (vec0.GetLength() != vec1.GetLength())
                return ExNumber.GetUndefined();

            ExComp totalSum = ExNumber.GetZero();
            for (int i = 0; i < vec0.GetLength(); ++i)
            {
                ExComp prod = MulOp.StaticCombine(vec0.Get(i), vec1.Get(i));
                totalSum = AddOp.StaticCombine(prod, totalSum);
            }
            return totalSum;
        }

        public virtual ExVector CreateEmptyBody()
        {
            return new ExVector(GetLength());
        }

        public virtual ExVector CreateVec(params ExComp[] exs)
        {
            return new ExVector(exs);
        }

        public ExComp GetVecLength()
        {
            ExComp sum = ExNumber.GetZero();
            for (int i = 0; i < this.GetLength(); ++i)
            {
                sum = AddOp.StaticCombine(PowOp.StaticCombine(Get(i), new ExNumber(2.0)), sum);
            }

            return PowOp.StaticCombine(sum, AlgebraTerm.FromFraction(ExNumber.GetOne(), new ExNumber(2.0)));
        }

        public override ExComp CloneEx()
        {
            ExVector vec = new ExVector(this.GetLength());
            for (int i = 0; i < this.GetLength(); ++i)
            {
                vec.Set(i, this.Get(i).CloneEx());
            }

            return vec;
        }

        public ExVector Normalize()
        {
            ExVector vec = this.CreateEmptyBody();
            ExComp vecLength = GetVecLength();

            for (int i = 0; i < this.GetLength(); ++i)
            {
                ExComp setVal = DivOp.StaticCombine(this.Get(i), vecLength.CloneEx());
                vec.Set(i, setVal);
            }

            return vec;
        }
    }
}