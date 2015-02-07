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
        private List<EquationSet> _eqSets;
        private List<List<TypePair<LexemeType, string>>> _lts;
        private int _singularIndex = -1;

        public EquationSystemTermType(List<EquationSet> eqSets, List<List<TypePair<LexemeType, string>>> lts, Dictionary<string, int> allIdens)
            : base()
        {
            _lts = lts;
            _eqSets = eqSets;
            _allIdens = allIdens;
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
            else if (command == SimplifyTermType.KEY_SIMPLIFY)
            {
                if (_singularIndex == -1 || _eqSets[_singularIndex].ContentStr == null)
                {
                    pEvalData.AddFailureMsg("Internal error.");
                    return SolveResult.Failure();
                }
                // Do all of the substitutions.
                for (int i = 0; i < _eqSets.Count; ++i)
                {
                    if (i == _singularIndex)
                        continue;

                    if (!(_eqSets[i].Left is FunctionDefinition || _eqSets[i].Right is FunctionDefinition))
                    {
                        pEvalData.AddFailureMsg("Internal error.");
                        return SolveResult.Failure();
                    }

                    FunctionDefinition funcDef;
                    ExComp assignTo;
                    if (_eqSets[i].Left is FunctionDefinition)
                    {
                        funcDef = _eqSets[i].Left as FunctionDefinition;
                        assignTo = _eqSets[i].Right;
                    }
                    else
                    {
                        funcDef = _eqSets[i].Right as FunctionDefinition;
                        assignTo = _eqSets[i].Left;
                    }

                    // Display the assignment as a message.
                    string funcDefStr;
                    if (assignTo is AlgebraTerm)
                        funcDefStr = (assignTo as AlgebraTerm).FinalToDispStr();
                    else
                        funcDefStr = assignTo.ToMathAsciiString();
                    funcDefStr = MathSolver.FinalizeOutput(funcDefStr);
                    pEvalData.AddMsg(WorkMgr.STM + funcDef.ToMathAsciiString() + WorkMgr.EDM + " defined as " + WorkMgr.STM + funcDefStr + WorkMgr.EDM);

                    pEvalData.FuncDefs.Define((FunctionDefinition)funcDef.Clone(), assignTo.Clone());
                }
                EquationSet tmpEqSet;
                if (!_eqSets[_singularIndex].ReparseInfo(out tmpEqSet, ref pEvalData))
                {
                    pEvalData.AddFailureMsg("Internal error.");
                    return SolveResult.Failure();
                }

                _eqSets[_singularIndex] = tmpEqSet;

                if (!_eqSets[_singularIndex].FixEqFuncDefs(ref pEvalData))
                {
                    pEvalData.AddFailureMsg("Internal error.");
                    return SolveResult.Failure();
                }

                return SimplifyTermType.SimplfyTerm(_eqSets[_singularIndex].Left, ref pEvalData);
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

            return SolveResult.InvalidCmd(ref pEvalData);
        }

        public bool Init()
        {
            if (_eqSets.Count > 3)
                return false;

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

            // Check if we have assigns and then one statement.
            _singularIndex = -1;
            for (int i = 0; i < _eqSets.Count; ++i)
            {
                bool leftIsFunc = _eqSets[i].Left is FunctionDefinition;
                bool rightIsFunc = _eqSets[i].Right is FunctionDefinition;
                if ((leftIsFunc || rightIsFunc) && !(leftIsFunc && rightIsFunc))
                {
                    if (_eqSets[i].Left != null && _eqSets[i].Right != null)
                        continue;
                }

                if (_singularIndex != -1)
                {
                    _singularIndex = -1;
                    break;
                }

                _singularIndex = i;
            }

            if (_singularIndex != -1 && _eqSets[_singularIndex].ContentStr != null)
            {
                var singularEqSet = _eqSets[_singularIndex];
                if (singularEqSet.Left == null || singularEqSet.Right == null)
                {
                    _cmds = new String[1];
                    _cmds[0] = SimplifyTermType.KEY_SIMPLIFY;
                    return true;
                }
            }

            if (options == null || options.Count == 0)
                return false;

            List<string> tmpCmds = new List<string>();

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