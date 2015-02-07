namespace MathSolverWebsite.MathSolverLibrary.Equation.LinearAlg
{
    internal class ExVector : ExComp
    {
        private ExComp _x;
        private ExComp _y;

        public ExComp X
        {
            get { return _x; }
        }

        public ExComp Y
        {
            get { return _y; }
        }

        public ExVector(ExComp x, ExComp y)
        {
            _x = x;
            _y = y;
        }

        public override ExComp Clone()
        {
            return new ExVector(_x, _y);
        }

        public override double GetCompareVal()
        {
            return 1.0;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is ExVector)
            {
                ExVector vec = ex as ExVector;
                if (vec.X.IsEqualTo(_x) && vec.Y.IsEqualTo(_y))
                    return true;
            }

            return false;
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return new AlgebraTerm(this);
        }

        public override string ToMathAsciiString()
        {
            return "(" + _x.ToMathAsciiString() + "," + _y.ToMathAsciiString() + ")";
        }

        public override string ToSearchString()
        {
            return "(" + _x.ToSearchString() + "," + _y.ToSearchString() + ")";
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "(" + _x.ToString() + "," + _y.ToString() + ")";
        }

        public override string ToTexString()
        {
            return "(" + _x.ToTexString() + "," + _y.ToTexString() + ")";
        }
    }
}