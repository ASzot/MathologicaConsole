using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class MultiLineHelper
    {
        private List<TypePair<FunctionDefinition, ExComp>> _funcDefs = new List<TypePair<FunctionDefinition, ExComp>>();
        private List<AndRestriction> _intervalDefs = new List<AndRestriction>();
        private List<List<WorkStep>> _intervalWorkSteps = new List<List<WorkStep>>();
        private string[] _graphStrs = null;
        private string _graphVar = null;

        public bool GetShouldGraph()
        {
            return _graphVar != null && _graphStrs != null;
        }

        public string[] GetGraphStrs()
        {
            return _graphStrs;
        }

        public string GetGraphVar()
        {
            return _graphVar;
        }

        public MultiLineHelper()
        {
        }

        public bool IsPreDefined(AlgebraComp funcDefVar)
        {
            foreach (TypePair<FunctionDefinition, ExComp> funcDef in _funcDefs)
            {
                if (funcDef.GetData1().GetIden().IsEqualTo(funcDefVar))
                    return true;
            }

            return false;
        }

        public void DoAssigns(ref EvalData pEvalData)
        {
            foreach (TypePair<FunctionDefinition, ExComp> funcDef in _funcDefs)
            {
                pEvalData.GetFuncDefs().Define(funcDef.GetData1(), funcDef.GetData2(), ref pEvalData);
            }
            foreach (List<WorkStep> intervalWorkStep in _intervalWorkSteps)
            {
                if (intervalWorkStep != null)
                    pEvalData.GetWorkMgr().GetWorkSteps().AddRange(intervalWorkStep);
            }
            foreach (AndRestriction rest in _intervalDefs)
            {
                pEvalData.AddVariableRestriction(rest);
            }
        }

        public List<EqSet> AssignLines(List<EqSet> eqs, ref List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>> lts, ref Dictionary<string, int> solveVars, out List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> totalLt, ref EvalData pEvalData)
        {
            totalLt = new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>();
            int ltsCount = 0;
            for (int i = 0; i < eqs.Count; ++i)
            {
                // Compound the lts for the number of sides there are.
                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> eqLt = new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>();
                for (int j = ltsCount; j < ltsCount + eqs[i].GetValidComparisonOps().Count + 1; ++j)
                {
                    eqLt.AddRange(lts[j]);
                }

                if (AttemptAddFuncDef(eqs, i, ref pEvalData) || AttemptAddIntervalDef(eqs[i], eqLt, ref pEvalData))
                {
                    lts.RemoveRange(ltsCount, eqs[i].GetValidComparisonOps().Count + 1);
                    ArrayFunc.RemoveIndex(eqs, i--);
                }
                else
                    ltsCount += eqs[i].GetValidComparisonOps().Count + 1;
            }

            foreach (List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt in lts)
            {
                totalLt.AddRange(lt);
            }

            FunctionDefinition funcDef = null;
            bool allEqual = true;
            foreach (TypePair<FunctionDefinition, ExComp> searchFuncDef in _funcDefs)
            {
                if (funcDef == null)
                    funcDef = searchFuncDef.GetData1();
                else
                {
                    if (!funcDef.IsEqualTo(searchFuncDef.GetData1()))
                    {
                        allEqual = false;
                        break;
                    }
                }
            }

            if (!allEqual && funcDef != null)
            {
                solveVars.Remove(funcDef.GetIden().GetVar().GetVar());

                if (solveVars.Count == 1)
                {
                    // There should only be one.
                    foreach (string solveVar in solveVars.Keys)
                        _graphVar = solveVar;

                    _graphStrs = new string[_funcDefs.Count];
                    bool allValid = true;
                    for (int i = 0; i < _funcDefs.Count; ++i)
                    {
                        _graphStrs[i] = _funcDefs[i].GetData2().ToJavaScriptString(pEvalData.GetUseRad());
                        if (_graphStrs[i] == null)
                        {
                            allValid = false;
                            break;
                        }
                    }

                    if (!allValid)
                    {
                        _graphVar = null;
                        _graphStrs = null;
                    }
                }
            }

            return eqs;
        }

        private bool AttemptAddIntervalDef(EqSet eqSet, List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt, ref EvalData pEvalData)
        {
            if (eqSet.GetSides().Count != 3)
                return false;

            if (eqSet.GetComparisonOps().Count != 2)
                return false;

            AlgebraSolver algebraSolver = new AlgebraSolver();
            Dictionary<string, int> solveVars = AlgebraSolver.GetIdenOccurances(lt);
            string solveVar = AlgebraSolver.GetProbableVar(solveVars);

            int workStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

            ExComp centerEx = eqSet.GetSides()[1];
            if (centerEx is AlgebraTerm)
                centerEx = (centerEx as AlgebraTerm).RemoveRedundancies(false);

            bool addWork = !centerEx.IsEqualTo(new AlgebraComp(solveVar));

            SolveResult result = algebraSolver.SolveEquationInequality(eqSet.GetSides(), eqSet.GetComparisonOps(), new AlgebraVar(solveVar), ref pEvalData);

            if (addWork)
                _intervalWorkSteps.Add(pEvalData.GetWorkMgr().GetWorkSteps().GetRange(workStepCount, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - workStepCount));
            else
                _intervalWorkSteps.Add(null);
            pEvalData.GetWorkMgr().GetWorkSteps().RemoveRange(workStepCount, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - workStepCount);

            if ((result.Solutions != null && result.Solutions.Count != 0) ||
                (result.Restrictions == null || result.Restrictions.Count != 1) ||
                (!(result.Restrictions[0] is AndRestriction)))
                return false;

            _intervalDefs.Add(result.Restrictions[0] as AndRestriction);

            return true;
        }

        private bool AttemptAddFuncDef(List<EqSet> eqs, int i, ref EvalData pEvalData)
        {
            EqSet eqSet = eqs[i];
            if (eqSet.GetSides().Count != 2 || eqSet.GetSides()[1] == null)
                return false;
            FunctionDefinition funcDef = null;
            ExComp assignTo = null;

            ExComp left = eqSet.GetLeft();
            if (left is AlgebraTerm)
                left = (left as AlgebraTerm).RemoveRedundancies(false);

            ExComp right = eqSet.GetRight();
            if (right is AlgebraTerm)
                right = (right as AlgebraTerm).RemoveRedundancies(false);

            if (left is FunctionDefinition)
            {
                funcDef = left as FunctionDefinition;
                assignTo = right;
            }
            else if (right is FunctionDefinition)
            {
                funcDef = right as FunctionDefinition;
                assignTo = left;
            }
            else if (left is AlgebraComp)
            {
                funcDef = new FunctionDefinition(left as AlgebraComp, null, null, false);
                assignTo = right;
            }
            else if (right is AlgebraComp)
            {
                funcDef = new FunctionDefinition(right as AlgebraComp, null, null, false);
                assignTo = left;
            }
            else
                return false;

            bool allEqual = eqs.Count != 1;
            // Check that the rest of the input is not using the same function.
            for (int j = 0; j < eqs.Count; ++j)
            {
                if (j == i)
                    continue;
                ExComp leftEx = eqs[j].GetLeft();
                ExComp rightEx = eqs[j].GetRight();

                if ((leftEx != null && leftEx.IsEqualTo(funcDef.GetIden())) || (rightEx != null && rightEx.IsEqualTo(funcDef.GetIden())))
                    continue;
                allEqual = false;
                break;
            }

            if (allEqual)
                return false;

            if (assignTo is AlgebraTerm)
            {
                (assignTo as AlgebraTerm).ApplyOrderOfOperations();
                assignTo = (assignTo as AlgebraTerm).MakeWorkable();
            }

            _funcDefs.Add(new TypePair<FunctionDefinition, ExComp>(funcDef, assignTo));
            // Also call this function on every single other side.
            for (int j = 0; j < eqs.Count; ++j)
            {
                if (j == i)
                    continue;
                eqs[j].CallFunction(funcDef, assignTo, ref pEvalData);
            }

            //pEvalData.FuncDefs.Define(funcDef, assignTo, ref pEvalData);
            return true;
        }
    }
}