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

        public static AlgebraVar GarbageVar
        {
            get { return new AlgebraVar(GARBAGE_VALUE); }
        }

        public string Var
        {
            get { return _varStr; }
            set
            {
                _varStr = value;
                _useEscape = _varStr == null ? false : Regex.IsMatch(_varStr, SPECIAL_MATCH);
            }
        }

        public AlgebraVar(string var)
        {
            _useEscape = false;
            _varStr = null;
            Var = var;
        }

        public static bool operator !=(AlgebraVar v1, AlgebraVar v2)
        {
            return !(v1 == v2);
        }

        public static bool operator ==(AlgebraVar v1, AlgebraVar v2)
        {
            if (((object)v1) == null && ((object)v1) == null)
                return true;
            else if (((object)v1) == null || ((object)v1) == null)
                return false;

            return (v1.Var == v2.Var);
        }

        public override int GetHashCode()
        {
            return Var.GetHashCode();
        }

        public bool IsGarbage()
        {
            return Var == GARBAGE_VALUE;
        }

        public AlgebraComp ToAlgebraComp()
        {
            return new AlgebraComp(Var);
        }

        public string ToMathAsciiString()
        {
            return (_useEscape ? "\\" : "") + Var.Replace("$", "");
        }

        public override string ToString()
        {
            return (_useEscape ? "\\" : "") + Var.Replace("$", "");
        }

        public string ToJavaScriptString()
        {
            return Var.Replace("$", "");
        }

        public string ToTexString()
        {
            return (_useEscape ? "\\" : "") + Var.Replace("$", "");
        }
    }

    internal class AlgebraComp : ExComp
    {
        protected AlgebraVar _var;

        public AlgebraVar Var
        {
            get { return _var; }
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

        public static bool operator !=(AlgebraComp ac1, AlgebraComp ac2)
        {
            return !(ac1 == ac2);
        }

        public static bool operator ==(AlgebraComp ac1, AlgebraComp ac2)
        {
            if (((object)ac1) == null && ((object)ac2) == null)
                return true;
            else if (((object)ac1) == null || ((object)ac2) == null)
                return false;

            return ac1.IsEqualTo(ac2);
        }

        public static AlgebraComp Parse(string parseStr)
        {
            return new AlgebraComp(parseStr);
        }

        public override ExComp Clone()
        {
            return new AlgebraComp(_var.Var);
        }

        public override double GetCompareVal()
        {
            return 1.0;
        }

        public override int GetHashCode()
        {
            return Var.GetHashCode();
        }

        public override bool IsEqualTo(ExComp comp)
        {
            if (comp is AlgebraComp)
            {
                AlgebraComp ac = comp as AlgebraComp;
                return _var == ac._var;
            }

            return false;
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return new AlgebraTerm(this);
        }

        public override string ToAsciiString()
        {
            return Var.ToMathAsciiString();
        }

        public Functions.PowerFunction ToPow(double realNum)
        {
            return new Functions.PowerFunction(this, new Number(realNum));
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return Var.ToJavaScriptString();
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "AC(" + _var.ToString() + ")";
        }

        public override string ToTexString()
        {
            return Var.ToTexString();
        }
    }
}