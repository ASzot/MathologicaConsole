using System.Text.RegularExpressions;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal struct AlgebraVar
    {
        public const string GARBAGE_VALUE = "GARBAGE!";

        private const string SPECIAL_MATCH = "alpha|beta|gamma|delta|epsilon|varepsilon|zeta|eta|theta|vartheta|iota|kappa|lambda|mu|nu|xi|rho|sigma|tau|usilon|phi|varphi|" +
            "chi|psi|omega|Gamma|Theta|Lambda|Xi|Phsi|Psi|Omega";

        private bool _useEscape;

        private string _varStr;

        public static AlgebraVar GetGarbageVar()
        {
            return new AlgebraVar(GARBAGE_VALUE);
        }

        public void SetVar(string value)
        {
            _varStr = value;
            _useEscape = _varStr == null ? false : Regex.IsMatch(_varStr, SPECIAL_MATCH);
        }

        public string GetVar()
        {
            return _varStr;
        }

        public AlgebraVar(string var)
        {
            _useEscape = false;
            _varStr = null;
            SetVar(var);
        }

        public bool IsGarbage()
        {
            return GetVar() == GARBAGE_VALUE;
        }

        public AlgebraComp ToAlgebraComp()
        {
            return new AlgebraComp(GetVar());
        }

        public string ToMathAsciiString()
        {
            return (_useEscape ? "\\" : "") + GetVar().Replace("$", "");
        }

        public override string ToString()
        {
            return (_useEscape ? "\\" : "") + GetVar().Replace("$", "");
        }

        public string ToJavaScriptString()
        {
            return GetVar().Replace("$", "");
        }

        public string ToTexString()
        {
            return (_useEscape ? "\\" : "") + GetVar().Replace("$", "");
        }
    }

    internal class AlgebraComp : ExComp
    {
        protected AlgebraVar _var;

        public AlgebraVar GetVar()
        {
            return _var;
        }

        public bool GetIsTrash()
        {
            return _var.GetVar() == AlgebraVar.GARBAGE_VALUE;
        }

        public AlgebraComp()
        {
            _var = new AlgebraVar(AlgebraVar.GARBAGE_VALUE);
        }

        public AlgebraComp(string var)
        {
            _var = new AlgebraVar(var);
        }

        public AlgebraComp(AlgebraVar var)
        {
            _var = var;
        }

        public static AlgebraComp Parse(string parseStr)
        {
            return new AlgebraComp(parseStr);
        }

        public override ExComp CloneEx()
        {
            return new AlgebraComp(_var.GetVar());
        }

        public override double GetCompareVal()
        {
            return 1.0;
        }

        public override bool IsEqualTo(ExComp comp)
        {
            if (comp is AlgebraComp)
            {
                AlgebraComp ac = comp as AlgebraComp;
                return _var.GetVar() == ac._var.GetVar();
            }

            return false;
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return new AlgebraTerm(this);
        }

        public override string ToAsciiString()
        {
            return GetVar().ToMathAsciiString();
        }

        public Functions.PowerFunction ToPow(double realNum)
        {
            return new Functions.PowerFunction(this, new ExNumber(realNum));
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return GetVar().ToJavaScriptString();
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "AC(" + _var.ToString() + ")";
        }

        public override string ToTexString()
        {
            return GetVar().ToTexString();
        }
    }
}