using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Information_Helpers
{
    internal class FuncDefHelper
    {
        private int _defineIndex = 0;
        private Dictionary<FunctionDefinition, ExComp> _defs = new Dictionary<FunctionDefinition, ExComp>();

        public IEnumerable<KeyValuePair<FunctionDefinition, ExComp>> AllDefinitions
        {
            get { return _defs; }
        }

        public FuncDefHelper()
        {
        }

        private List<FunctionDefinition> GetAllParaFuncs(int dimen)
        {
            List<FunctionDefinition> funcDefs = new List<FunctionDefinition>();
            foreach (KeyValuePair<FunctionDefinition, ExComp> func in _defs)
            {
                if (func.Key.InputArgCount != dimen && !(func.Value is ExMatrix))
                    funcDefs.Add(func.Key);
            }

            return funcDefs;
        }

        private Dictionary<string, List<FunctionDefinition>> GetAllParasOfSingleVar(int dimen)
        {
            Dictionary<string, List<FunctionDefinition>> funcDicts = new Dictionary<string, List<FunctionDefinition>>();
            List<FunctionDefinition> funcs = GetAllParaFuncs(dimen);
            foreach (FunctionDefinition func in funcs)
            {
                string varStr = func.InputArgs[0].Var.Var;
                if (!funcDicts.ContainsKey(varStr))
                    funcDicts[varStr] = new List<FunctionDefinition>();
                funcDicts[varStr].Add(func);
            }

            return funcDicts;
        }

        private static FunctionDefinition GetFuncDef(string iden, List<FunctionDefinition> funcs)
        {
            for (int i = 0; i < funcs.Count; ++i)
            {
                FunctionDefinition funcDef = funcs[i];
                if (funcDef.Iden.Var.Var == iden)
                {
                    return funcDef;
                }
            }
            return null;
        }

        public static int GetMostCurrentIndex(List<FunctionDefinition> funcDefs)
        {
            int maxIndex = int.MinValue;
            foreach (FunctionDefinition funcDef in funcDefs)
            {
                if (funcDef.FuncDefIndex > maxIndex)
                {
                    maxIndex = funcDef.FuncDefIndex;
                }
            }

            return maxIndex;
        }

        public static FunctionDefinition GetMostCurrentDef(List<FunctionDefinition> funcDefs)
        {
            int maxIndex = int.MinValue;
            FunctionDefinition func = null;
            foreach (FunctionDefinition funcDef in funcDefs)
            {
                if (funcDef.FuncDefIndex > maxIndex)
                {
                    maxIndex = funcDef.FuncDefIndex;
                    func = funcDef;
                }
            }

            return func;
        }

        public List<FunctionDefinition> GetAllVecEquations(int dimen)
        {
            List<FunctionDefinition> funcs = new List<FunctionDefinition>();
            foreach (KeyValuePair<FunctionDefinition, ExComp> keyVal in _defs)
            {
                if (keyVal.Key.InputArgCount == dimen && (keyVal.Value is ExVector))
                {
                    funcs.Add(keyVal.Key);
                }
            }

            return funcs;
        }

        public ExComp GetSingleVarDefinition(AlgebraComp iden)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> keyVal in _defs)
            {
                if (keyVal.Key.Iden.IsEqualTo(iden) && !keyVal.Key.HasValidInputArgs)
                {
                    return keyVal.Value;
                }
            }

            return null;
        }

        public List<FunctionDefinition> GetProbableParametricEquations(int dimen)
        {
            Dictionary<string, List<FunctionDefinition>> singleFuncs = GetAllParasOfSingleVar(dimen);
            List<FunctionDefinition> mostOccuring = null;
            int max = int.MinValue;

            foreach (KeyValuePair<string, List<FunctionDefinition>> singleFunc in singleFuncs)
            {
                if (GetFuncDef("x", singleFunc.Value) != null && GetFuncDef("y", singleFunc.Value) != null)
                    return singleFunc.Value;

                if (singleFunc.Value.Count > max)
                {
                    max = singleFunc.Value.Count;
                    mostOccuring = singleFunc.Value;
                }
            }

            return mostOccuring;
        }

        public void Define(FunctionDefinition func, ExComp funcDef, ref TermType.EvalData pEvalData)
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

            func.FuncDefIndex = _defineIndex++;

            pEvalData.AddMsg(WorkMgr.STM + func.ToDispString() + WorkMgr.EDM + " defined as " + 
                WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(funcDef) + WorkMgr.EDM);

            _defs.Add(func, funcDef);
        }

        public KeyValuePair<FunctionDefinition, ExComp> GetDefinition(FunctionDefinition func)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.Iden.IsEqualTo(func.Iden))
                    return def;
            }

            return new KeyValuePair<FunctionDefinition, ExComp>(null, null);
        }

        public TypePair<string, ExComp>[] GetDefinitionToPara(FunctionDefinition func)
        {
            ExVector useVector = null;
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.Iden.IsEqualTo(func.Iden))
                {
                    useVector = def.Value as ExVector;
                    break;
                }
            }

            TypePair<string, ExComp>[] retVec = new TypePair<string, ExComp>[useVector.Length];
            for (int i = 0; i < retVec.Length; ++i)
            {
                retVec[i] = new TypePair<string, ExComp>(null, useVector.Get(i));
            }

            return retVec;
        }

        public int GetFuncArgCount(string iden)
        {
            foreach (var def in _defs)
            {
                if (iden == def.Key.Iden.Var.Var)
                    return def.Key.InputArgCount;           
            }

            return -1;
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