namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal abstract class ExComp
    {
        protected const bool USE_TEX = false;

        public static bool operator !=(ExComp ex1, ExComp ex2)
        {
            return !(ex1 == ex2);
        }

        public static bool operator ==(ExComp ex1, ExComp ex2)
        {
            if (((object)ex1) == null && ((object)ex2) == null)
                return true;
            else if (((object)ex1) == null || ((object)ex2) == null)
                return false;

            return ex1.IsEqualTo(ex2);
        }

        public abstract ExComp Clone();

        public override bool Equals(object obj)
        {
            if (!(obj is ExComp))
                return false;

            ExComp ex = obj as ExComp;

            return this.IsEqualTo(ex);
        }

        public abstract double GetCompareVal();

        public abstract bool IsEqualTo(ExComp ex);

        public abstract AlgebraTerm ToAlgTerm();

        public virtual string ToDispString()
        {
            if (USE_TEX)
                return ToTexString();
            return ToMathAsciiString();
        }

        public abstract string ToMathAsciiString();

        public abstract string ToSearchString();

        public abstract string ToTexString();
    }
}