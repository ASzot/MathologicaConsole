using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class FunctionTermType : TermType
    {
        private AlgebraSolver _agSolver;
        private ExComp _assignTo;
        private FunctionDefinition _func;

        public FunctionTermType()
            : base()
        {
        }

        public override Equation.SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            _agSolver.ResetIterCount();

            if (command == "Find inverse")
            {
                pEvalData.AttemptSetInputType(InputType.FunctionInverse);

                if (pEvalData.WorkMgr.AllowWork && _func.InputArgCount > 0)
                {
                    string funcStr = WorkMgr.ExFinalToAsciiStr(_func);
                    string callArgStr = WorkMgr.ExFinalToAsciiStr(_func.InputArgs[0]);
                    pEvalData.WorkMgr.FromSides(_func, _assignTo, "To find the inverse switch `" + WorkMgr.ExFinalToAsciiStr(_func) + "` with `" + callArgStr +
                        "` and solve for `" + funcStr + "`");
                }

                // Find the inverse.
                AlgebraComp inverseFunc = new AlgebraComp(_func.Iden.ToString() + "^(-1)" + "(" + _func.InputArgs[0].ToString() + ")");
                AlgebraTerm left = _func.InputArgs[0].ToAlgTerm();
                AlgebraTerm right = _assignTo.Clone().ToAlgTerm().Substitute(_func.InputArgs[0], inverseFunc);

                if (pEvalData.WorkMgr.AllowWork)
                    pEvalData.WorkMgr.FromSides(left, right, "`" + WorkMgr.ExFinalToAsciiStr(inverseFunc) + "` is the inverse function, solve for it.");

                return _agSolver.SolveEquationEquality(inverseFunc.Var, left, right, ref pEvalData);
            }
            else if (command == "Assign function")
            {
                // Display the assignment as a message.
                string funcDefStr;
                if (_assignTo is AlgebraTerm)
                    funcDefStr = (_assignTo as AlgebraTerm).FinalToDispStr();
                else
                    funcDefStr = _assignTo.ToAsciiString();
                funcDefStr = MathSolver.FinalizeOutput(funcDefStr);
                pEvalData.AddMsg(WorkMgr.STM + _func.ToAsciiString() + WorkMgr.EDM + " defined as " + WorkMgr.STM + funcDefStr + WorkMgr.EDM);

                // Assign the function.
                pEvalData.FuncDefs.Define(_func, _assignTo);
                return SolveResult.Solved();
            }
            else if (command.StartsWith("Domain of "))
            {
                string varForKey = command.Substring("Domain of ".Length, command.Length - "Domain of ".Length);
                AlgebraVar varFor = new AlgebraVar(varForKey);

                return _agSolver.CalculateDomain(_assignTo, varFor, ref pEvalData);
            }
            else if (command == "Graph")
            {
                if (pEvalData.AttemptSetGraphData(_assignTo))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            return SolveResult.InvalidCmd(ref pEvalData);
        }

        public bool Init(EquationSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, string probSolveVar)
        {
            if (eqSet.Left is FunctionDefinition)
            {
                _func = eqSet.Left as FunctionDefinition;
                _assignTo = eqSet.Right;
            }
            else if (eqSet.Right is FunctionDefinition)
            {
                _func = eqSet.Right as FunctionDefinition;
                _assignTo = eqSet.Left;
            }
            else
                return false;

            if (_assignTo == null || Number.IsUndef(_assignTo))
                return false;

            if (_func.HasCallArgs)
                return false;

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

            List<string> tmpCmds = new List<string>();
            if (!_func.IsMultiValued && _assignTo.ToAlgTerm().Contains(_func.InputArgs[0]))
                tmpCmds.Add("Find inverse");
            tmpCmds.Add("Assign function");
            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                tmpCmds.Add("Domain of " + solveVarKeys[i]);
            }

            if (solveVars.Count == 1 && !_func.IsMultiValued)
            {
                AlgebraTerm term = _assignTo.ToAlgTerm();
                string graphStr = term.ToJavaScriptString(true);
                if (graphStr != null)
                    tmpCmds.Add("Graph");
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }
    }
}