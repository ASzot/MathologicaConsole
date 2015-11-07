using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class FunctionGenTermType : GenTermType
    {
        private AlgebraSolver _agSolver;
        private ExComp _assignTo;
        private FunctionDefinition _func;

        public FunctionGenTermType()
            : base()
        {
        }

        public override Equation.SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            _agSolver.ResetIterCount();

            if (command == "Find inverse")
            {
                pEvalData.AttemptSetInputType(InputType.FunctionInverse);

                if (pEvalData.GetWorkMgr().GetAllowWork() && _func.GetInputArgCount() > 0)
                {
                    string funcStr = WorkMgr.ToDisp(_func);
                    string callArgStr = WorkMgr.ToDisp(_func.GetInputArgs()[0]);
                    pEvalData.GetWorkMgr().FromSides(_func, _assignTo, "To find the inverse switch `" + WorkMgr.ToDisp(_func) + "` with `" + callArgStr +
                        "` and solve for `" + funcStr + "`");
                }

                // Find the inverse.
                AlgebraComp inverseFunc = new AlgebraComp(_func.GetIden().ToString() + "^(-1)" + "(" + _func.GetInputArgs()[0].ToString() + ")");
                AlgebraTerm left = _func.GetInputArgs()[0].ToAlgTerm();
                AlgebraTerm right = _assignTo.CloneEx().ToAlgTerm().Substitute(_func.GetInputArgs()[0], inverseFunc);

                if (pEvalData.GetWorkMgr().GetAllowWork())
                    pEvalData.GetWorkMgr().FromSides(left, right, "`" + WorkMgr.ToDisp(inverseFunc) + "` is the inverse function, solve for it.");

                SolveResult solveResult = _agSolver.SolveEquationEquality(inverseFunc.GetVar(), left, right, ref pEvalData);
                return solveResult;
            }
            else if (command.StartsWith("Assign"))
            {
                // Assign the function.
                if (_assignTo is AlgebraTerm)
                {
                    (_assignTo as AlgebraTerm).ApplyOrderOfOperations();
                    _assignTo = (_assignTo as AlgebraTerm).MakeWorkable();
                }

                pEvalData.GetFuncDefs().Define(_func, _assignTo, ref pEvalData);
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
                if (pEvalData.AttemptSetGraphData(_assignTo, _func.GetInputArgs()[0].GetVar().GetVar()))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            SolveResult invalidCmd = SolveResult.InvalidCmd(ref pEvalData);
            return invalidCmd;
        }

        private bool ContainsSpecialFuncs(AlgebraTerm term)
        {
            foreach (ExComp subTerm in term.GetSubComps())
            {
                if ((subTerm is Equation.Functions.Calculus.Derivative) ||
                    (subTerm is Equation.Functions.Calculus.Integral) ||
                    (subTerm is Equation.Functions.Calculus.Vector.FieldTransformation) ||
                    (subTerm is Equation.Functions.ChooseFunction) ||
                    (subTerm is Equation.Functions.PermutationFunction) ||
                    (subTerm is Equation.Functions.Calculus.Limit) ||
                    (subTerm is Equation.Structural.LinearAlg.ExMatrix) ||
                    (subTerm is Equation.Structural.LinearAlg.Determinant) ||
                    (subTerm is Equation.Structural.LinearAlg.MatrixInverse))
                {
                    return true;
                }

                if (subTerm is AlgebraTerm && ContainsSpecialFuncs(subTerm as AlgebraTerm))
                    return true;
            }

            return false;
        }

        public bool Init(EqSet eqSet, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, string probSolveVar)
        {
            // Also allow the single variable assigns like y=x^2
            AlgebraComp funcIden = null;

            if (eqSet.GetLeft() is FunctionDefinition)
            {
                _func = eqSet.GetLeft() as FunctionDefinition;
                _assignTo = eqSet.GetRight();
            }
            else if (eqSet.GetRight() is FunctionDefinition)
            {
                _func = eqSet.GetRight() as FunctionDefinition;
                _assignTo = eqSet.GetLeft();
            }
            else if (eqSet.GetLeft() is AlgebraComp && !eqSet.GetRightTerm().Contains(eqSet.GetLeft() as AlgebraComp))
            {
                funcIden = eqSet.GetLeft() as AlgebraComp;
                _assignTo = eqSet.GetRight();
            }
            else if (eqSet.GetRight() is AlgebraComp && !eqSet.GetLeftTerm().Contains(eqSet.GetRight() as AlgebraComp))
            {
                funcIden = eqSet.GetRight() as AlgebraComp;
                _assignTo = eqSet.GetLeft();
            }
            else
                return false;

            if (funcIden != null)
            {
                if (_assignTo.ToAlgTerm().Contains(funcIden))
                    return false;

                // The input variable for the function needs to be assumed.
                AlgebraComp[] useVars;
                if (_assignTo is Equation.Structural.LinearAlg.ExMatrix ||
                    _assignTo is ExNumber)
                {
                    useVars = new AlgebraComp[] { new AlgebraComp(AlgebraVar.GetGarbageVar()) };
                }
                else if (probSolveVar == funcIden.GetVar().GetVar())
                    return false;
                else
                {
                    useVars = new AlgebraComp[] { new AlgebraComp(probSolveVar) };
                    // For graphing later.
                    solveVars.Remove(funcIden.GetVar().GetVar());
                }

                _func = new FunctionDefinition(funcIden, useVars, null, false);
            }

            if (_assignTo == null || ExNumber.IsUndef(_assignTo))
                return false;

            //if (_func.HasCallArgs)
            //    return false;

            _agSolver = new AlgebraSolver();
            _agSolver.CreateUSubTable(solveVars);

            List<string> solveVarKeys = (from solveVar in solveVars
                                         select solveVar.Key).Distinct().ToList();

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == probSolveVar)
                {
                    ArrayFunc.RemoveIndex(solveVarKeys, i);
                    break;
                }
            }

            solveVarKeys.Insert(0, probSolveVar);

            List<string> tmpCmds = new List<string>();
            if (solveVars.Count == 1 && !_func.GetIsMultiValued() && _func.GetInputArgs() != null && _func.GetHasValidInputArgs())
            {
                AlgebraTerm term = _assignTo.ToAlgTerm();
                string graphStr = term.ToJavaScriptString(true);
                if (graphStr != null)
                    tmpCmds.Add("Graph");
            }

            if (!ContainsSpecialFuncs(new AlgebraTerm(_assignTo)) &&
                !_func.GetIsMultiValued() &&
                _assignTo.ToAlgTerm().Contains(_func.GetInputArgs()[0]))
            {
                tmpCmds.Add("Find inverse");
            }

            tmpCmds.Add(_func.GetHasValidInputArgs() ? "Assign function" : "Assign value");
            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                tmpCmds.Add("Domain of " + solveVarKeys[i]);
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }
    }
}