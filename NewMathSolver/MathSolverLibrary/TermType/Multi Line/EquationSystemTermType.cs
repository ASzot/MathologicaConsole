using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class EquationSystemTermType : TermType
    {
        private Dictionary<string, int> _allIdens;
        private List<EqSet> _eqSets;
        private List<List<TypePair<LexemeType, string>>> _lts;
        private string[] _graphStrs;
        private string _graphVarStr = null;

        public EquationSystemTermType(List<EqSet> eqSets, List<List<TypePair<LexemeType, string>>> lts, Dictionary<string, int> allIdens)
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
        public EquationSystemTermType(List<EqSet> eqSets)
        {
            
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (command.StartsWith("Solve by substitution for "))
            {
                AlgebraSolver agSolver = new AlgebraSolver();

                command = command.Remove(0, "Solve by substitution for ".Length);
                string[] solveVars = command.Split(',');

                Solving.EquationSystemSolve solveMethod = new Solving.EquationSystemSolve(agSolver);

                solveMethod.SolveFors = solveVars.ToList();

                var clonedEqSet = from eqSet in _eqSets
                                  select eqSet.Clone();
                solveMethod.SolvingMethod = Solving.EquationSystemSolveMethod.Substitution;
                return solveMethod.SolveEquationArray(clonedEqSet.ToList(), _lts, _allIdens, ref pEvalData);
            }
            else if (command.StartsWith("Solve by elimination for "))
            {
                AlgebraSolver agSolver = new AlgebraSolver();

                command = command.Remove(0, "Solve by elimination for ".Length);
                string[] solveVars = command.Split(',');

                Solving.EquationSystemSolve solveMethod = new Solving.EquationSystemSolve(agSolver);

                solveMethod.SolveFors = solveVars.ToList();

                var clonedEqSet = from eqSet in _eqSets
                                  select eqSet.Clone();
                solveMethod.SolvingMethod = Solving.EquationSystemSolveMethod.Elimination;
                return solveMethod.SolveEquationArray(clonedEqSet.ToList(), _lts, _allIdens, ref pEvalData);
            }
            else if (command == "Graph")
            {
                if (_graphVarStr != null && pEvalData.AttemptSetGraphData(_graphStrs, _graphVarStr))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            return SolveResult.InvalidCmd(ref pEvalData);
        }

        public bool InitGraphingOnly(ref EvalData pEvalData)
        {
            if (_allIdens.Count != 1)
                return false;

            string[] graphStrs = new string[_eqSets.Count];
            for (int i = 0; i < _eqSets.Count; ++i)
            {
                if (!_eqSets[i].IsSingular)
                    return false;

                string graphStr = _eqSets[i].LeftTerm.ToJavaScriptString(pEvalData.UseRad);
                if (graphStr == null)
                    return false;
                graphStrs[i] = graphStr;
            }

            _graphStrs = graphStrs;

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
                if (!eqSet.IsSingular)
                {
                    ExComp[] funcDef = eqSet.GetFuncDefComps();
                    if (funcDef != null)
                    {
                        AlgebraTerm term = funcDef[1].ToAlgTerm();
                        var vars = term.GetAllAlgebraCompsStr();
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
                    AlgebraTerm term = eqSet.LeftTerm;
                    var vars = term.GetAllAlgebraCompsStr();
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
                if (_eqSets[i].Left == null || _eqSets[i].Right == null)
                {
                    idenOccurs = null;
                    break;
                }

                var lt0 = _lts[j];
                if (_eqSets[i].Right != null)
                {
                    var lt1 = _lts[j + 1];
                    j++;

                    lt0.AddRange(lt1);
                }

                idenOccurs.Add(AlgebraSolver.GetIdenOccurances(lt0));
            }

            List<string> options = null;
            if (idenOccurs != null)
            {
                var solveVars = new List<string>();
                foreach (var idenOccur in idenOccurs)
                {
                    foreach (var iden in idenOccur)
                    {
                        if (!solveVars.Contains(iden.Key))
                            solveVars.Add(iden.Key);
                    }
                }

                var combinations = Combination(solveVars);

                options = (from comb in combinations
                           where (comb.Split(',').Length == _eqSets.Count)
                           select comb).ToList();
            }

            if (options == null || options.Count == 0)
                return false;


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