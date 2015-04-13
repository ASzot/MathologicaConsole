namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class AllSolutions : SpecialSolution
    {
        public override string ToAsciiString()
        {
            return @"\text{All solutions}";
        }

        public override string ToTexString()
        {
            return "All real solutions";
        }
    }

    internal class NoSolutions : SpecialSolution
    {
        public override string ToAsciiString()
        {
            return @"\text{No solution}";
        }

        public override string ToTexString()
        {
            return "No solution";
        }
    }

    internal abstract class SpecialSolution : ExComp
    {
        public override ExComp Clone()
        {
            throw null;
        }

        public override double GetCompareVal()
        {
            return 0.0;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            return false;
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return null;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }
    }
}