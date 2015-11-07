using System.Collections;
using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    ///////////////////////////////
    // This class is never used.
    /// //////////////////////////

    internal class LogGenTermType : GenTermType
    {
        private ExComp _coeff;
        private ExComp _funcIden;
        private ExComp _horizontalShift;
        private LogFunction _log;
        private AlgebraVar _solveFor;
        private ExComp _verticalShift = null;
        private FunctionGenTermType tt_func = null;
        private SolveGenTermType _ttSolveGen = null;

        public LogGenTermType()
        {
        }

        public override Equation.SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            if (command == "Find vertical asymptote")
            {
                return SolveResult.Simplified(MulOp.Negate(_horizontalShift));
            }

            if (tt_func != null && tt_func.IsValidCommand(command))
            {
                SolveResult solveFuncResult = tt_func.ExecuteCommand(command, ref pEvalData);
                return solveFuncResult;
            }
            else if (_ttSolveGen != null && _ttSolveGen.IsValidCommand(command))
            {
                SolveResult solveSolveResult = _ttSolveGen.ExecuteCommand(command, ref pEvalData);
                return solveSolveResult;
            }

            SolveResult solveResult = SolveResult.InvalidCmd(ref pEvalData);
            return solveResult;
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
                tt_func = new FunctionGenTermType();
                if (!tt_func.Init(new EqSet(_funcIden, left == null ? right : left, LexemeType.EqualsOp), lexemeTable, solveVars, probSolveVar))
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

            if (!(singleGpCmp is LogFunction))
                return false;

            _log = singleGpCmp as LogFunction;

            List<AlgebraGroup> innerConstantTerms = _log.GetInnerTerm().GetGroupsConstantTo(solveForComp);
            _horizontalShift = ExNumber.GetZero();
            foreach (AlgebraGroup innerConstantTerm in innerConstantTerms)
                _horizontalShift = AddOp.StaticCombine(_horizontalShift, innerConstantTerm.ToTerm()).ToAlgTerm();

            List<string> tmpCmds = new List<string>();
            tmpCmds.Add("Find vertical asymptote");

            if (tt_func != null && _ttSolveGen != null)
            {
                tmpCmds.AddRange(tt_func.GetCommands());
                tmpCmds.AddRange(ArrayFunc.ToList(tt_func.GetCommands()).GetRange(0, 2));
            }
            else if (tt_func != null)
            {
                tmpCmds.AddRange(tt_func.GetCommands());
            }
            else if (_ttSolveGen != null)
            {
                tmpCmds.AddRange(_ttSolveGen.GetCommands());
            }

            _cmds = tmpCmds.ToArray();

            return true;
        }
    }
}