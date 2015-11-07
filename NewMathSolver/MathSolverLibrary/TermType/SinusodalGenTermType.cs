using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class SinusodalGenTermType : GenTermType
    {
        private ExComp _coeff;
        private ExComp _funcIden = null;
        private ExComp _period = null;
        private ExComp _phaseShift;
        private AlgebraVar _solveFor;
        private TrigFunction _trigFunc;
        private FunctionGenTermType tt_func = null;
        private SimplifyGenTermType tt_simp = null;
        private SolveGenTermType _ttSolveGen = null;
        private string _graphStr = null;

        public SinusodalGenTermType()
        {
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            if (command == "Get amplitude")
            {
                // Get the amplitude.
                if (_coeff == null)
                {
                    pEvalData.AddFailureMsg("Function is not periodic.");
                    return SolveResult.Failure();
                }

                AbsValFunction coeffAbs = new AbsValFunction(_coeff);

                SolveResult simpResult = SolveResult.Simplified(coeffAbs.Evaluate(false, ref pEvalData));
                return simpResult;
            }
            else if (command == "Get phase shift")
            {
                // Get the phase shift.

                return SolveResult.Simplified(MulOp.Negate(_phaseShift));
            }
            else if (command == "Get period")
            {
                if (_period == null)
                {
                    pEvalData.AddFailureMsg("Function is not periodic.");
                    return SolveResult.Failure();
                }
                return SolveResult.Simplified(_period);
            }
            else if (command == "Graph" && _graphStr != null)
            {
                if (pEvalData.AttemptSetGraphData(_graphStr, _solveFor.GetVar()))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }
            else if (tt_func != null && tt_func.IsValidCommand(command))
            {
                SolveResult funcSolveResult = tt_func.ExecuteCommand(command, ref pEvalData);
                return funcSolveResult;
            }
            else if (_ttSolveGen != null && _ttSolveGen.IsValidCommand(command))
            {
                SolveResult solveSolveResult = _ttSolveGen.ExecuteCommand(command, ref pEvalData);
                return solveSolveResult;
            }
            else if (tt_simp != null && tt_simp.IsValidCommand(command))
            {
                SolveResult simpResult = tt_simp.ExecuteCommand(command, ref pEvalData);
                return simpResult;
            }

            SolveResult invalidResult = SolveResult.InvalidCmd(ref pEvalData);
            return invalidResult;
        }

        public bool Init(EquationInformation eqInfo, ExComp left, ExComp right, List<TypePair<LexemeType, string>> lexemeTable,
            Dictionary<string, int> solveVars, string probSolveVar, ref EvalData pEvalData)
        {
            if (probSolveVar == null)
                return false;

            if (solveVars.Count > 1 && right != null)
            {
                if (!(left is AlgebraComp && !right.ToAlgTerm().Contains(left as AlgebraComp)) &&
                    !(right is AlgebraComp && !left.ToAlgTerm().Contains(right as AlgebraComp)))
                    return false;
            }

            if (!eqInfo.HasOnlyOrFunctions(FunctionType.Sinusodal))
                return false;

            AlgebraTerm overall;

            _solveFor = new AlgebraVar(probSolveVar);
            AlgebraComp solveForComp = _solveFor.ToAlgebraComp();

            if ((left is AlgebraComp && !solveForComp.IsEqualTo(left)) || left is FunctionDefinition)
            {
                overall = right.ToAlgTerm();
                _funcIden = left;
            }
            else if ((right is AlgebraComp && !solveForComp.IsEqualTo(right)) || right is FunctionDefinition)
            {
                overall = left.ToAlgTerm();
                _funcIden = right;
            }
            else if (right == null)
            {
                overall = left.ToAlgTerm();
            }
            else
            {
                return false;
            }

            if (_funcIden is FunctionDefinition)
            {
                tt_func = new FunctionGenTermType();
                if (!tt_func.Init(new EqSet(_funcIden, overall, LexemeType.EqualsOp), lexemeTable, solveVars,
                    _funcIden is AlgebraComp ? (_funcIden as AlgebraComp).GetVar().GetVar() : ""))
                    tt_func = null;
            }
            else if (_funcIden is AlgebraComp)
            {
                solveVars.Remove((_funcIden as AlgebraComp).GetVar().GetVar());
            }

            string promptStr;
            if (left != null && right != null)
            {
                promptStr = "Solve for ";
            }
            else
            {
                promptStr = "Find zeros for ";
                tt_simp = new SimplifyGenTermType(left != null ? left : right);
            }

            _ttSolveGen = new SolveGenTermType(new EqSet(overall, ExNumber.GetZero(), LexemeType.EqualsOp), lexemeTable, solveVars,
                probSolveVar, promptStr);

            int groupCount = overall.GetGroupCount();

            List<AlgebraGroup> variableGroups = overall.GetGroupsVariableTo(solveForComp);

            // Only one variable group is allowed.
            if (variableGroups.Count != 1)
                return false;

            ExComp[] variableGroup = variableGroups[0].GetGroup();
            ExComp[] variableCoeffs = GroupHelper.GetUnrelatableTermsOfGroup(variableGroup, solveForComp);

            _coeff = GroupHelper.ToAlgTerm(variableCoeffs);

            List<ExComp> varGroupList = ArrayFunc.ToList(variableGroup);

            foreach (ExComp varCoeff in variableCoeffs)
            {
                if (!varGroupList.Remove(varCoeff) && !ExNumber.GetOne().IsEqualTo(varCoeff))
                    return false;
            }

            if (varGroupList.Count != 1)
                return false;

            ExComp singleGpCmp = varGroupList[0];
            if (singleGpCmp is AlgebraTerm)
                singleGpCmp = (singleGpCmp as AlgebraTerm).RemoveRedundancies(false);

            if (!(singleGpCmp is TrigFunction))
                return false;

            _trigFunc = singleGpCmp as TrigFunction;

            List<AlgebraGroup> innerConstantTerms = _trigFunc.GetInnerTerm().GetGroupsConstantTo(solveForComp);
            _phaseShift = ExNumber.GetZero();
            foreach (AlgebraGroup innerConstantTerm in innerConstantTerms)
                _phaseShift = AddOp.StaticCombine(_phaseShift, innerConstantTerm.ToTerm()).ToAlgTerm();

            _period = _trigFunc.GetPeriod(solveForComp, pEvalData.GetUseRad());
            if (_period == null)
                _coeff = null;

            List<string> tmpCmds = new List<string>();
            tmpCmds.Add("Get period");
            tmpCmds.Add("Get amplitude");
            tmpCmds.Add("Get phase shift");

            if (tt_func != null && _ttSolveGen != null)
            {
                tmpCmds.AddRange(ArrayFunc.ToList(tt_func.GetCommands()).GetRange(0, 3));
                tmpCmds.AddRange(_ttSolveGen.GetCommands());
            }
            else if (tt_func != null)
            {
                tmpCmds.AddRange(tt_func.GetCommands());
            }
            else if (_ttSolveGen != null)
            {
                tmpCmds.AddRange(_ttSolveGen.GetCommands());
            }

            if (tt_simp != null)
            {
                tt_simp.SetToSimpOnly();
                tmpCmds.Add(SimplifyGenTermType.KEY_SIMPLIFY);
            }

            if (!tmpCmds.Contains("Graph"))
            {
                _graphStr = overall.ToJavaScriptString(pEvalData.GetUseRad());
                if (_graphStr != null && solveVars.Count == 1)
                    tmpCmds.Insert(0, "Graph");
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }
    }
}