using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class SolveGenTermType : GenTermType
    {
        private AlgebraSolver _agSolver;
        private EqSet _eqSet;
        private List<TypePair<LexemeType, string>> _lt;
        private string[] s_promptStrs;

        public SolveGenTermType(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars,
            string probSolveVar, string[] promptStrs, string noIncludeVar)
        {
            s_promptStrs = promptStrs;

            _eqSet = eqSet;
            _lt = lt;
            _agSolver = new AlgebraSolver();

            _agSolver.CreateUSubTable(solveVars);

            List<string> solveVarKeys = ArrayFunc.Distinct(solveVars);

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == probSolveVar)
                {
                    ArrayFunc.RemoveIndex(solveVarKeys, i);
                    break;
                }
            }

            solveVarKeys.Insert(0, probSolveVar);

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == noIncludeVar)
                {
                    ArrayFunc.RemoveIndex(solveVarKeys, i);
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

        public SolveGenTermType(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, string probSolveVar, string promptStr)
            : base()
        {
            s_promptStrs = new string[1];

            s_promptStrs[0] = promptStr;

            _eqSet = eqSet;
            _lt = lt;
            _agSolver = new AlgebraSolver();

            _agSolver.CreateUSubTable(solveVars);

            List<string> solveVarKeys = ArrayFunc.Distinct(solveVars);

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == probSolveVar)
                {
                    ArrayFunc.RemoveIndex(solveVarKeys, i);
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

        public SolveGenTermType(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars,
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

                    pEvalData.SetIsWorkable(false);
                    SolveResult result;
                    if (_eqSet.GetComparisonOp() == LexemeType.EqualsOp)
                        result = _agSolver.SolveEquationEquality(solveFor, useSet.GetLeft().ToAlgTerm(), useSet.GetRight().ToAlgTerm(), ref pEvalData);
                    else
                        result = _agSolver.SolveEquationInequality(useSet.GetSides(), useSet.GetComparisonOps(), solveFor, ref pEvalData);

                    result.RemoveUndefinedSolutions();
                    result.RemoveExtraneousSolutions(_eqSet, ref pEvalData);
                    if (!result.GetHasSolutions() && !result.GetHasRestrictions() && !pEvalData.GetHasPartialSolutions() &&
                        result.Success)
                    {
                        SolveResult solved = SolveResult.Solved(solveFor, new NoSolutions(), ref pEvalData);
                        return solved;
                    }

                    return result;
                }
            }

            if (command.StartsWith("Domain of "))
            {
                string varForKey = command.Substring("Domain of ".Length, command.Length - "Domain of ".Length);
                AlgebraVar varFor = new AlgebraVar(varForKey);

                SolveResult domainResult = _agSolver.CalculateDomain(_eqSet, varFor, ref pEvalData);
                return domainResult;
            }
            else if (command.StartsWith("Implicit differentiation "))
            {
                string differential = StringFunc.Rm(command, 0, "Implicit differentiation ".Length);
                string[] split = differential.Split('/');

                split[0] = StringFunc.Rm(split[0], 0, 1);
                split[1] = StringFunc.Rm(split[1], 0, 1);

                SolveResult result = _eqSet.ImplicitDifferentiation(split[0], split[1], _agSolver, ref pEvalData);
                result.RemoveUndefinedSolutions();

                return result;
            }

            SolveResult invalidResult = SolveResult.InvalidCmd(ref pEvalData);
            return invalidResult;
        }
    }
}