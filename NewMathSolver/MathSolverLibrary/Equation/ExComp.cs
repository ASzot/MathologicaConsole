namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal abstract class ExComp
    {
        protected const bool USE_TEX = false;

        public abstract ExComp CloneEx();

        public abstract double GetCompareVal();

        public abstract bool IsEqualTo(ExComp ex);

        public abstract AlgebraTerm ToAlgTerm();

        public virtual string ToDispString()
        {
            if (USE_TEX)
                return ToTexString();
            return ToAsciiString();
        }

        public abstract string ToAsciiString();

        public abstract string ToJavaScriptString(bool useRad);

        public abstract string ToTexString();
    }
}