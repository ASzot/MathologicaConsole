using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class SolveTermType : TermType
    {
        private AlgebraSolver _agSolver;
        private EqSet _eqSet;
        private List<TypePair<LexemeType, string>> _lt;
        private string[] s_promptStrs;

        public SolveTermType(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, 
            string probSolveVar, string[] promptStrs, string noIncludeVar)
        {
            s_promptStrs = promptStrs;

            _eqSet = eqSet;
            _lt = lt;
            _agSolver = new AlgebraSolver();

            _agSolver.CreateUSubTable(solveVars);

            List<string> solveVarKeys = (from solveVar in solveVars
                                         select solveVar.Key).Distinct().ToList();

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == probSolveVar)
                {
                    solveVarKeys.RemoveAt(i);
                    break;
                }
            }

            solveVarKeys.Insert(0, probSolveVar);

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == noIncludeVar)
                {
                    solveVarKeys.RemoveAt(i);
                    break;
                }
            }

            List<string> tmpCmds = new List<string>();
            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (probSolveVar == solveVarKeys[i])
                {
                    foreach (string promptStr in s_promptStrs)
                        tmpCmds.Add(promptStr + solveVarKeys[i]);
                }
                else
                    tmpCmds.Add(promptStrs[0] + solveVarKeys[i]);
            }

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                tmpCmds.Add("Domain of " + solveVarKeys[i]);
            }

            _cmds = tmpCmds.ToArray();
        }

        public SolveTermType(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, string probSolveVar, string promptStr)
            : base()
        {
            s_promptStrs = new string[1];

            s_promptStrs[0] = promptStr;

            _eqSet = eqSet;
            _lt = lt;
            _agSolver = new AlgebraSolver();

            _agSolver.CreateUSubTable(solveVars);

            List<string> solveVarKeys = (from solveVar in solveVars
                                         select solveVar.Key).Distinct().ToList();

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == probSolveVar)
                {
                    solveVarKeys.RemoveAt(i);
                    break;
                }
            }

            solveVarKeys.Insert(0, probSolveVar);
            List<string> cmds = new List<string>();

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                cmds.Add(s_promptStrs[0] + solveVarKeys[i]);
            }

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                cmds.Add("Domain of " + solveVarKeys[i]);
            }

            if (solveVarKeys.Count > 1)
            {
                for (int i = 0; i < solveVarKeys.Count; ++i)
                {
                    for (int j = 0; j < solveVarKeys.Count; ++j)
                    {
                        if (i != j)
                            cmds.Add("Implicit differentiation d" + solveVarKeys[i] + "/d" + solveVarKeys[j]);
                    }
                }
            }

            _cmds = cmds.ToArray();
        }

        public SolveTermType(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, 
            string probSolveVar)
            : this(eqSet, lt, solveVars, probSolveVar, "Solve for ")
        {
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            _agSolver.ResetIterCount();

            foreach (string promptStr in s_promptStrs)
            {
                if (command.StartsWith(promptStr))
                {
                    string solveForKey = command.Substring(promptStr.Length, command.Length - promptStr.Length);
                    AlgebraVar solveFor = new AlgebraVar(solveForKey);

                    EqSet useSet = _eqSet.Clone();

                    pEvalData.IsWorkable = false;
                    SolveResult result;
                    if (_eqSet.ComparisonOp == LexemeType.EqualsOp)
                        result = _agSolver.SolveEquationEquality(solveFor, useSet.Left.ToAlgTerm(), useSet.Right.ToAlgTerm(), ref pEvalData);
                    else
                        result = _agSolver.SolveEquationInequality(useSet.Sides, useSet.ComparisonOps, solveFor, ref pEvalData);

                    result.RemoveUndefinedSolutions();
                    result.RemoveExtraneousSolutions(_eqSet, ref pEvalData);
                    if (!result.HasSolutions && !result.HasRestrictions && !pEvalData.HasPartialSolutions && result.Success)
                        return SolveResult.Solved(solveFor, new NoSolutions(), ref pEvalData);

                    return result;
                }
            }

            if (command.StartsWith("Domain of "))
            {
                string varForKey = command.Substring("Domain of ".Length, command.Length - "Domain of ".Length);
                AlgebraVar varFor = new AlgebraVar(varForKey);

                return _agSolver.CalculateDomain(_eqSet, varFor, ref pEvalData);
            }
            else if (command.StartsWith("Implicit differentiation "))
            {
                string differential = command.Remove(0, "Implicit differentiation ".Length);
                string[] split = differential.Split('/');

                split[0] = split[0].Remove(0, 1);
                split[1] = split[1].Remove(0, 1);

                SolveResult result = _eqSet.ImplicitDifferentiation(split[0], split[1], _agSolver, ref pEvalData);
                result.RemoveUndefinedSolutions();

                return result;
            }

            return SolveResult.InvalidCmd(ref pEvalData);
        }
    }
}