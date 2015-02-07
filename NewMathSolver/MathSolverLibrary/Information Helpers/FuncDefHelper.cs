using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Information_Helpers
{
    internal class FuncDefHelper
    {
        private Dictionary<FunctionDefinition, ExComp> _defs = new Dictionary<FunctionDefinition, ExComp>();

        public FuncDefHelper()
        {
        }

        public void Define(FunctionDefinition func, ExComp funcDef)
        {
            FunctionDefinition removeKey = null;
            foreach (var def in _defs)
            {
                if (def.Key.Iden.IsEqualTo(func.Iden))
                {
                    // The user has redefined this function.
                    removeKey = def.Key;
                    break;
                }
            }

            if (removeKey != null)
                _defs.Remove(removeKey);

            _defs.Add(func, funcDef);
        }

        public KeyValuePair<FunctionDefinition, ExComp> GetDefinition(FunctionDefinition func)
        {
            foreach (var def in _defs)
            {
                if (def.Key.Iden.IsEqualTo(func.Iden))
                    return def;
            }

            return new KeyValuePair<FunctionDefinition, ExComp>(null, null);
        }

        public FunctionDefinition GetFuncDef(string idenStr)
        {
            foreach (var def in _defs)
            {
                if (def.Key.Iden.Var.Var == idenStr)
                    return def.Key;
            }

            return null;
        }

        public bool IsFuncDefined(string idenStr)
        {
            foreach (var def in _defs)
            {
                if (def.Key.Iden.Var.Var == idenStr)
                    return true;
            }

            return false;
        }

        public bool IsValidFuncCall(string idenStr, int argCount)
        {
            foreach (var def in _defs)
            {
                if (def.Key.Iden.Var.Var == idenStr && argCount == def.Key.InputArgCount)
                    return true;
            }

            return false;
        }
    }
}