using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class SinusodalTermType : TermType
    {
        private ExComp _coeff;
        private ExComp _funcIden = null;
        private ExComp _period = null;
        private ExComp _phaseShift;
        private AlgebraVar _solveFor;
        private TrigFunction _trigFunc;
        private FunctionTermType tt_func = null;
        private SimplifyTermType tt_simp = null;
        private SolveTermType tt_solve = null;
        private string _graphStr;

        public SinusodalTermType()
        {
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (command == "Get amplitude")
            {
                // Get the amplitude.
                if (_coeff == null)
                {
                    pEvalData.AddFailureMsg("Function is not periodic.");
                    return SolveResult.Failure();
                }

                AbsValFunction coeffAbs = new AbsValFunction(_coeff);

                return SolveResult.Simplified(coeffAbs.Evaluate(false, ref pEvalData));
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
            else if (command == "Graph")
            {
                if (pEvalData.AttemptSetGraphData(_graphStr))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }
            else if (tt_func != null && tt_func.IsValidCommand(command))
                return tt_func.ExecuteCommand(command, ref pEvalData);
            else if (tt_solve != null && tt_solve.IsValidCommand(command))
                return tt_solve.ExecuteCommand(command, ref pEvalData);
            else if (tt_simp != null && tt_simp.IsValidCommand(command))
                return tt_simp.ExecuteCommand(command, ref pEvalData);

            return SolveResult.InvalidCmd(ref pEvalData);
        }

        public bool Init(EquationInformation eqInfo, ExComp left, ExComp right, List<TypePair<LexemeType, string>> lexemeTable,
            Dictionary<string, int> solveVars, string probSolveVar, ref EvalData pEvalData)
        {
            if (probSolveVar == null)
                return false;

            if (solveVars.Count > 1)
                return false;

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
                tt_func = new FunctionTermType();
                if (!tt_func.Init(new EquationSet(_funcIden, overall, LexemeType.EqualsOp), lexemeTable, solveVars,
                    _funcIden is AlgebraComp ? (_funcIden as AlgebraComp).Var.Var : ""))
                    tt_func = null;
            }
            else if (_funcIden is AlgebraComp)
            {
                solveVars.Remove((_funcIden as AlgebraComp).Var.Var);
            }

            string promptStr;
            if (left != null && right != null)
            {
                promptStr = "Solve for ";
            }
            else
            {
                promptStr = "Find zeros for ";
                tt_simp = new SimplifyTermType(left != null ? left : right);
            }

            tt_solve = new SolveTermType(new EquationSet(overall, Number.Zero, LexemeType.EqualsOp), lexemeTable, solveVars,
                probSolveVar, promptStr);

            int groupCount = overall.GroupCount;

            List<AlgebraGroup> variableGroups = overall.GetGroupsVariableTo(solveForComp);

            // Only one variable group is allowed.
            if (variableGroups.Count != 1)
                return false;

            ExComp[] variableGroup = variableGroups[0].Group;
            ExComp[] variableCoeffs = variableGroup.GetUnrelatableTermsOfGroup(solveForComp);

            _coeff = variableCoeffs.ToAlgTerm();

            List<ExComp> varGroupList = variableGroup.ToList();

            foreach (ExComp varCoeff in variableCoeffs)
            {
                if (!varGroupList.Remove(varCoeff) && !Number.One.IsEqualTo(varCoeff))
                    return false;
            }

            if (varGroupList.Count != 1)
                return false;

            ExComp singleGpCmp = varGroupList[0];
            if (singleGpCmp is AlgebraTerm)
                singleGpCmp = (singleGpCmp as AlgebraTerm).RemoveRedundancies();

            if (!(singleGpCmp is TrigFunction))
                return false;

            _trigFunc = singleGpCmp as TrigFunction;

            List<AlgebraGroup> innerConstantTerms = _trigFunc.InnerTerm.GetGroupsConstantTo(solveForComp);
            _phaseShift = Number.Zero;
            foreach (AlgebraGroup innerConstantTerm in innerConstantTerms)
                _phaseShift = AddOp.StaticCombine(_phaseShift, innerConstantTerm.ToTerm()).ToAlgTerm();

            _period = _trigFunc.GetPeriod(solveForComp, pEvalData.UseRad);
            if (_period == null)
                _coeff = null;

            List<string> tmpCmds = new List<string>();
            tmpCmds.Add("Get period");
            tmpCmds.Add("Get amplitude");
            tmpCmds.Add("Get phase shift");

            if (tt_func != null && tt_solve != null)
            {
                tmpCmds.AddRange(tt_solve.GetCommands());
                tmpCmds.AddRange(tt_func.GetCommands().ToList().GetRange(0, 2));
            }
            else if (tt_func != null)
            {
                tmpCmds.AddRange(tt_func.GetCommands());
            }
            else if (tt_solve != null)
            {
                tmpCmds.AddRange(tt_solve.GetCommands());
            }

            if (tt_simp != null)
            {
                tt_simp.SetToSimpOnly();
                tmpCmds.Add(SimplifyTermType.KEY_SIMPLIFY);
            }

            if (!tmpCmds.Contains("Graph"))
            {
                _graphStr = overall.ToJavaScriptString(pEvalData.UseRad);
                if (_graphStr != null)
                    tmpCmds.Add("Graph");
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }
    }
}