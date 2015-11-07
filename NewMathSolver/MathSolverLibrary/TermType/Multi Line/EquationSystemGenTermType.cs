using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class EquationSystemGenTermType : GenTermType
    {
        private Dictionary<string, int> _allIdens;
        private List<EqSet> _eqSets;
        private List<List<TypePair<LexemeType, string>>> _lts;
        private string[] _graphStrs;
        private string _graphVarStr = null;

        public EquationSystemGenTermType(List<EqSet> eqSets, List<List<TypePair<LexemeType, string>>> lts, Dictionary<string, int> allIdens)
            : base()
        {
            _lts = lts;
            _eqSets = eqSets;
            _allIdens = allIdens;
        }

        /// <summary>
        /// Everything will be created automatically.
        /// </summary>
        /// <param name="eqSets"></param>
        public EquationSystemGenTermType(List<EqSet> eqSets)
        {
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (command.StartsWith("Solve by substitution for "))
            {
                AlgebraSolver agSolver = new AlgebraSolver();

                command = StringFunc.Rm(command, 0, "Solve by substitution for ".Length);
                string[] solveVars = command.Split(',');

                Solving.EquationSystemSolve solveMethod = new Solving.EquationSystemSolve(agSolver);

                solveMethod.SetSolveFors(ArrayFunc.ToList(solveVars));

                List<EqSet> clonedEqSet = new List<EqSet>();
                for (int i = 0; i < _eqSets.Count; ++i)
                {
                    clonedEqSet.Add(_eqSets[i].Clone());
                }

                solveMethod.SetSolvingMethod(Solving.EquationSystemSolveMethod.Substitution);
                SolveResult arraySolveResult = solveMethod.SolveEquationArray(clonedEqSet, _lts, _allIdens, ref pEvalData);
                return arraySolveResult;
            }
            else if (command.StartsWith("Solve by elimination for "))
            {
                AlgebraSolver agSolver = new AlgebraSolver();

                command = StringFunc.Rm(command, 0, "Solve by elimination for ".Length);
                string[] solveVars = command.Split(',');

                Solving.EquationSystemSolve solveMethod = new Solving.EquationSystemSolve(agSolver);

                solveMethod.SetSolveFors(ArrayFunc.ToList(solveVars));

                List<EqSet> clonedEqSet = new List<EqSet>();
                for (int i = 0; i < _eqSets.Count; ++i)
                    clonedEqSet.Add(_eqSets[i]);

                solveMethod.SetSolvingMethod(Solving.EquationSystemSolveMethod.Elimination);
                SolveResult arraySolveResult = solveMethod.SolveEquationArray(clonedEqSet, _lts, _allIdens, ref pEvalData);
                return arraySolveResult;
            }
            else if (command == "Graph")
            {
                if (_graphVarStr != null && pEvalData.AttemptSetGraphData(_graphStrs, _graphVarStr))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            SolveResult invalidCmdResult = SolveResult.InvalidCmd(ref pEvalData);
            return invalidCmdResult;
        }

        public bool InitGraphingOnly(ref EvalData pEvalData)
        {
            if (_allIdens.Count != 1)
                return false;

            string[] graphStrs = new string[_eqSets.Count];
            for (int i = 0; i < _eqSets.Count; ++i)
            {
                if (!_eqSets[i].GetIsSingular())
                    return false;

                string graphStr = _eqSets[i].GetLeftTerm().ToJavaScriptString(pEvalData.GetUseRad());
                if (graphStr == null)
                    return false;
                graphStrs[i] = graphStr;
            }

            _graphStrs = graphStrs;

            // There is only one in the dictionary.
            foreach (string iden in _allIdens.Keys)
                _graphVarStr = iden;

            _cmds = new string[1] { "Graph" };

            return true;
        }

        public bool Init(ref EvalData pEvalData)
        {
            if (_eqSets.Count > 3)
                return false;

            List<string> tmpCmds = new List<string>();

            bool isGraph = true;
            _graphStrs = new string[_eqSets.Count];
            _graphVarStr = null;
            for (int i = 0; i < _eqSets.Count; ++i)
            {
                EqSet eqSet = _eqSets[i];
                if (!eqSet.GetIsSingular())
                {
                    ExComp[] funcDef = eqSet.GetFuncDefComps();
                    if (funcDef != null)
                    {
                        AlgebraTerm term = funcDef[1].ToAlgTerm();
                        List<string> vars = term.GetAllAlgebraCompsStr();
                        if (vars.Count == 1 && (_graphVarStr == null || vars[0] == _graphVarStr))
                        {
                            _graphVarStr = vars[0];
                            string graphStr = term.ToJavaScriptString(true);
                            if (graphStr != null)
                            {
                                _graphStrs[i] = graphStr;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    AlgebraTerm term = eqSet.GetLeftTerm();
                    List<string> vars = term.GetAllAlgebraCompsStr();
                    if (vars.Count == 1 && (_graphVarStr == null || vars[0] == _graphVarStr))
                    {
                        _graphVarStr = vars[0];
                        string graphStr = term.ToJavaScriptString(true);
                        if (graphStr != null)
                        {
                            _graphStrs[i] = graphStr;
                            continue;
                        }
                    }
                }

                isGraph = false;
                break;
            }

            if (!isGraph)
                _graphStrs = null;
            else
                tmpCmds.Add("Graph");

            List<Dictionary<string, int>> idenOccurs = new List<Dictionary<string, int>>();
            for (int i = 0, j = 0; i < _eqSets.Count; i++, j++)
            {
                if (_eqSets[i].GetLeft() == null || _eqSets[i].GetRight() == null)
                {
                    idenOccurs = null;
                    break;
                }

                List<TypePair<LexemeType, string>> lt0 = _lts[j];
                if (_eqSets[i].GetRight() != null)
                {
                    List<TypePair<LexemeType, string>> lt1 = _lts[j + 1];
                    j++;

                    lt0.AddRange(lt1);
                }

                idenOccurs.Add(AlgebraSolver.GetIdenOccurances(lt0));
            }

            List<string> options = null;
            if (idenOccurs != null)
            {
                List<string> solveVars = new List<string>();
                foreach (Dictionary<string, int> idenOccur in idenOccurs)
                {
                    foreach (KeyValuePair<string, int> iden in idenOccur)
                    {
                        if (!solveVars.Contains(iden.Key))
                            solveVars.Add(iden.Key);
                    }
                }

                List<string> combinations = Combination(solveVars);

                options = new List<string>();
                for (int i = 0; i < combinations.Count; ++i)
                {
                    if (combinations[i].Split(',').Length == _eqSets.Count)
                        options.Add(combinations[i]);
                }
            }

            if (options == null || options.Count == 0)
            {
                if (tmpCmds.Count != 0)
                {
                    _cmds = tmpCmds.ToArray();
                    return true;
                }
                return false;
            }

            foreach (string option in options)
            {
                tmpCmds.Add("Solve by substitution for " + option);
                tmpCmds.Add("Solve by elimination for " + option);
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }

        private List<string> Combination(List<string> str)
        {
            if (str.Count == 1)
            {
                List<string> tmpList = new List<string>();
                tmpList.Add(str[0]);
                return tmpList;
            }

            // read the last character
            string c = str[str.Count - 1];

            // apart from the last character send remaining string for further processing
            List<string> returnArray = Combination(str.GetRange(0, str.Count - 1));

            // List to keep final string combinations
            List<string> finalArray = new List<string>();

            // add whatever is coming from the previous routine
            foreach (string s in returnArray)
                finalArray.Add(s);

            // take the last character
            finalArray.Add(c.ToString());

            // take the combination between the last char and the returning strings from the previous routine
            foreach (string s in returnArray)
                finalArray.Add(s + "," + c);

            return finalArray;
        }
    }
}