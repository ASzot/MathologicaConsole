using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Information_Helpers
{
    internal class FuncDefHelper
    {
        private int _defineIndex = 0;
        private Dictionary<FunctionDefinition, ExComp> _defs = new Dictionary<FunctionDefinition, ExComp>();

        public IEnumerable<KeyValuePair<FunctionDefinition, ExComp>> GetAllDefinitions()
        {
            return _defs;
        }

        public FuncDefHelper()
        {
        }

        private List<FunctionDefinition> GetAllParaFuncs(int dimen)
        {
            List<FunctionDefinition> funcDefs = new List<FunctionDefinition>();
            foreach (KeyValuePair<FunctionDefinition, ExComp> func in _defs)
            {
                if (func.Key.GetInputArgCount() == dimen && !(func.Value is ExMatrix))
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
                string varStr = func.GetInputArgs()[0].GetVar().GetVar();
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
                if (funcDef.GetIden().GetVar().GetVar() == iden)
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
                if (funcDef.GetFuncDefIndex() > maxIndex)
                {
                    maxIndex = funcDef.GetFuncDefIndex();
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Get the most current definition from a list favoring the bias identifier.
        /// </summary>
        /// <param name="funcDefs"></param>
        /// <param name="biasIden"></param>
        /// <returns></returns>
        public static FunctionDefinition GetMostCurrentDef(List<FunctionDefinition> funcDefs, AlgebraComp biasIden)
        {
            int maxIndex = int.MinValue;
            FunctionDefinition func = null;
            foreach (FunctionDefinition funcDef in funcDefs)
            {
                if (biasIden != null && funcDef.GetIden().IsEqualTo(biasIden))
                    return funcDef;
                if (funcDef.GetFuncDefIndex() > maxIndex)
                {
                    maxIndex = funcDef.GetFuncDefIndex();
                    func = funcDef;
                }
            }

            return func;
        }

        public void Remove(string iden)
        {
            FunctionDefinition removeKey = null;
            foreach (FunctionDefinition funcDef in _defs.Keys)
            {
                if (funcDef.ToDispString() == iden)
                    removeKey = funcDef;
            }

            if (removeKey != null)
                _defs.Remove(removeKey);
        }

        public void SetFunctionState(Dictionary<FunctionDefinition, ExComp> defs)
        {
            _defs = defs;
        }

        public List<FunctionDefinition> GetAllVecEquations(int dimen)
        {
            List<FunctionDefinition> funcs = new List<FunctionDefinition>();
            foreach (KeyValuePair<FunctionDefinition, ExComp> keyVal in _defs)
            {
                if (keyVal.Key.GetInputArgCount() == dimen && (keyVal.Value is ExVector))
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
                if (keyVal.Key.GetIden().IsEqualTo(iden) && !keyVal.Key.GetHasValidInputArgs())
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
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().IsEqualTo(func.GetIden()))
                {
                    // The user has redefined this function.
                    removeKey = def.Key;
                    break;
                }
            }

            if (removeKey != null)
                _defs.Remove(removeKey);
            func.SetFuncDefIndex(_defineIndex++);

            if (funcDef is ExVector)
                func.SetIsVectorFunc(true);

            pEvalData.AddMsg(WorkMgr.STM + func.ToDispString() + WorkMgr.EDM + " defined as " +
                WorkMgr.STM + WorkMgr.ToDisp(funcDef) + WorkMgr.EDM);

            _defs.Add(func, funcDef);
        }

        public KeyValuePair<FunctionDefinition, ExComp> GetDefinition(FunctionDefinition func)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().IsEqualTo(func.GetIden()))
                    return def;
            }

            return ArrayFunc.CreateKeyValuePair<FunctionDefinition, ExComp>(null, null);
        }

        public KeyValuePair<FunctionDefinition, ExComp> GetDefinition(AlgebraComp searchFuncIden)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().IsEqualTo(searchFuncIden))
                    return def;
            }

            return ArrayFunc.CreateKeyValuePair<FunctionDefinition, ExComp>(null, null);
        }

        public TypePair<string, ExComp>[] GetDefinitionToPara(FunctionDefinition func)
        {
            ExVector useVector = null;
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().IsEqualTo(func.GetIden()))
                {
                    useVector = def.Value as ExVector;
                    break;
                }
            }

            TypePair<string, ExComp>[] retVec = new TypePair<string, ExComp>[useVector.GetLength()];
            for (int i = 0; i < retVec.Length; ++i)
            {
                retVec[i] = new TypePair<string, ExComp>(FunctionDefinition.GetDimenStr(i), useVector.Get(i));
            }

            return retVec;
        }

        public int GetFuncArgCount(string iden)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (iden == def.Key.GetIden().GetVar().GetVar())
                    return def.Key.GetInputArgCount();
            }

            return -1;
        }

        public FunctionDefinition GetFuncDef(string idenStr)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().GetVar().GetVar() == idenStr)
                    return def.Key;
            }

            return null;
        }

        public bool IsFuncDefined(string idenStr)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().GetVar().GetVar() == idenStr)
                    return true;
            }

            return false;
        }

        public bool IsValidFuncCall(string idenStr, int argCount)
        {
            foreach (KeyValuePair<FunctionDefinition, ExComp> def in _defs)
            {
                if (def.Key.GetIden().GetVar().GetVar() == idenStr && argCount == def.Key.GetInputArgCount())
                    return true;
            }

            return false;
        }
    }
}