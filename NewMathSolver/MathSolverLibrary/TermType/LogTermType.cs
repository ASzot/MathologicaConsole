using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class LogTermType : TermType
    {
        private ExComp _coeff;
        private ExComp _funcIden;
        private ExComp _horizontalShift;
        private LogFunction _log;
        private AlgebraVar _solveFor;
        private ExComp _verticalShift = null;
        private FunctionTermType tt_func = null;
        private SolveTermType tt_solve = null;

        public LogTermType()
        {
        }

        public override Equation.SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (command == "Find vertical asymptote")
            {
                return SolveResult.Simplified(MulOp.Negate(_horizontalShift));
            }

            if (tt_func != null && tt_func.IsValidCommand(command))
                return tt_func.ExecuteCommand(command, ref pEvalData);
            else if (tt_solve != null && tt_solve.IsValidCommand(command))
                return tt_solve.ExecuteCommand(command, ref pEvalData);

            return SolveResult.InvalidCmd(ref pEvalData);
        }

        public bool Init(EquationInformation eqInfo, ExComp left, ExComp right, List<TypePair<LexemeType, string>> lexemeTable,
            Dictionary<string, int> solveVars, string probSolveVar)
        {
            if (probSolveVar == null)
                return false;

            if (!eqInfo.HasOnlyOrFunctions(FunctionType.Logarithm))
                return false;

            AlgebraTerm overall;

            if (left is AlgebraComp || left is FunctionDefinition)
            {
                overall = right.ToAlgTerm();
                _funcIden = left;
            }
            else if (right is AlgebraComp || right is FunctionDefinition)
            {
                overall = left.ToAlgTerm();
                _funcIden = right;
            }
            else
            {
                overall = SubOp.StaticCombine(left, right).ToAlgTerm();
            }

            if (_funcIden is FunctionDefinition)
            {
                tt_func = new FunctionTermType();
                if (!tt_func.Init(new EquationSet(_funcIden, left == null ? right : left, LexemeType.EqualsOp), lexemeTable, solveVars, probSolveVar))
                    tt_func = null;
            }

            _solveFor = new AlgebraVar(probSolveVar);
            AlgebraComp solveForComp = _solveFor.ToAlgebraComp();

            string promptStr = null;
            if (_funcIden != null)
                promptStr = "Find zeros for ";
            else if (left != null && right != null)
                promptStr = "Solve for ";

            if (promptStr == null)
                return false;

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

            if (!(singleGpCmp is LogFunction))
                return false;

            _log = singleGpCmp as LogFunction;

            List<AlgebraGroup> innerConstantTerms = _log.InnerTerm.GetGroupsConstantTo(solveForComp);
            _horizontalShift = Number.Zero;
            foreach (AlgebraGroup innerConstantTerm in innerConstantTerms)
                _horizontalShift = AddOp.StaticCombine(_horizontalShift, innerConstantTerm.ToTerm()).ToAlgTerm();

            List<string> tmpCmds = new List<string>();
            tmpCmds.Add("Find vertical asymptote");

            if (tt_func != null && tt_solve != null)
            {
                tmpCmds.AddRange(tt_func.GetCommands());
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

            _cmds = tmpCmds.ToArray();

            return true;
        }
    }
}