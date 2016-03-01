using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.Information_Helpers;
using MathSolverWebsite.MathSolverLibrary.Solving;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class EvalData
    {
        private const int MAX_TRIG_SUB_REPEAT_COUNT = 3;
        private int _trigSubRepeatCount = 0;
        private bool _checkSolutions = false;
        private List<string> _failureMsgs = new List<string>();
        private List<string> _msgs = null;
        private List<ExComp> _partialSols = null;
        private bool _useRad;
        private WorkMgr _workMgr;
        private int _negDivCount = -1;
        private bool _isWorkable = false;
        private Solving.QuadraticSolveMethod _quadSolveMethod;
        private Information_Helpers.FuncDefHelper _funcDefs;
        private string _graphVar = null;
        private string[] _graphEqStrs = null;
        private InputType _inputType = InputType.Invalid;
        private InputAddType _inputAddType = InputAddType.Invalid;
        private List<AndRestriction> _variableRestrictions = null;

        /// <summary>
        /// Can return null if the input type is invalid.
        /// </summary>
        public string GetInputTypeStr()
        {
            return InputTypeHelper.ToDescStr(_inputType, _inputAddType);
        }

        public int GetTrigSubRepeatCount()
        {
            return _trigSubRepeatCount;
        }

        public void IncrementTrigSubRepeatCount()
        {
            _trigSubRepeatCount++;
        }

        public void ResetTrigSubRepeatCount()
        {
            _trigSubRepeatCount = 0;
        }

        public bool TrigSubRepeatCountExceededMax()
        {
            return _trigSubRepeatCount >= MAX_TRIG_SUB_REPEAT_COUNT;
        }

        public void SetQuadSolveMethod(QuadraticSolveMethod value)
        {
            _quadSolveMethod = value;
        }

        public QuadraticSolveMethod GetQuadSolveMethod()
        {
            return _quadSolveMethod;
        }

        public string GetGraphVar()
        {
            return _graphVar;
        }

        internal void SetGraphEqStrs(string[] value)
        {
            _graphEqStrs = new string[value.Length];
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] == null)
                {
                    _graphEqStrs = null;
                    break;
                }
                _graphEqStrs[i] = value[i].Replace("+-", "-");
            }
        }

        public string[] GetGraphEqStrs()
        {
            return _graphEqStrs;
        }

        public FuncDefHelper GetFuncDefs()
        {
            return _funcDefs;
        }

        public void SetCheckSolutions(bool value)
        {
            _checkSolutions = value;
        }

        public bool GetCheckSolutions()
        {
            return _checkSolutions;
        }

        public void SetIsWorkable(bool value)
        {
            _isWorkable = value;
        }

        public bool GetIsWorkable()
        {
            return _isWorkable;
        }

        public void SetNegDivCount(int value)
        {
            _negDivCount = value;
        }

        public int GetNegDivCount()
        {
            return _negDivCount;
        }

        public List<string> GetFailureMsgs()
        {
            return _failureMsgs;
        }

        public bool GetHasPartialSolutions()
        {
            return _partialSols != null && _partialSols.Count != 0;
        }

        public List<string> GetMsgs()
        {
            return _msgs;
        }

        public List<ExComp> GetPartialSolutions()
        {
            return _partialSols;
        }

        public virtual bool GetUseRad()
        {
            return _useRad;
        }

        /// <summary>
        /// Only use when the angle mode will be restored shortly after.
        /// </summary>
        /// <param name="useRad"></param>
        public void TmpSetUseRad(bool useRad)
        {
            _useRad = useRad;
        }

        public virtual void SetWorkMgr(WorkMgr value)
        {
            _workMgr = value;
        }

        public virtual WorkMgr GetWorkMgr()
        {
            return _workMgr;
        }

        public EvalData(bool useRad, WorkMgr workMgr, Information_Helpers.FuncDefHelper pFuncDefHelper)
        {
            _useRad = useRad;
            _workMgr = workMgr;
            _funcDefs = pFuncDefHelper;
        }

        public void AddVariableRestriction(AndRestriction andRestriction)
        {
            if (_variableRestrictions == null)
                _variableRestrictions = new List<AndRestriction>();
            _variableRestrictions.Add(andRestriction);
        }

        public AndRestriction GetVariableRestriction(AlgebraComp varFor)
        {
            if (_variableRestrictions == null)
                return null;
            foreach (AndRestriction andRest in _variableRestrictions)
            {
                if (andRest.VarComp.IsEqualTo(varFor))
                    return andRest;
            }

            return null;
        }

        public void AttemptSetInputType(InputType inputType)
        {
            if (_inputType == (MathSolverLibrary.TermType.InputType.Invalid))
                _inputType = inputType;
        }

        public void AddInputType(InputAddType inputType)
        {
            if (_inputAddType == (MathSolverLibrary.TermType.InputAddType.Invalid))
                _inputAddType = inputType;
        }

        public void SwitchInputTypeForInequality()
        {
            _inputType = InputTypeHelper.ToInequalityType(_inputType);
        }

        public bool AttemptSetGraphData(ExComp graphEx, string graphVar)
        {
            if (_graphEqStrs == null)
            {
                _graphVar = graphVar;
                AlgebraTerm term = new AlgebraTerm(graphEx);
                List<string> vars = term.GetAllAlgebraCompsStr();
                if (vars.Count != 1)
                    return false;

                string graphStr = term.ToJavaScriptString(_useRad);
                if (graphStr != null)
                {
                    SetGraphEqStrs(new string[1] { graphStr });
                    return true;
                }
            }

            return false;
        }

        public bool AttemptSetGraphData(string str, string graphVar)
        {
            if (_graphEqStrs == null)
            {
                _graphVar = graphVar;
                SetGraphEqStrs(new string[1] { str });
                return true;
            }

            return false;
        }

        public bool AttemptSetGraphData(string[] strs, string graphVar)
        {
            if (strs == null)
                return false;
            if (_graphEqStrs == null)
            {
                _graphVar = graphVar;
                SetGraphEqStrs(strs);
                return true;
            }
            return false;
        }

        public void AddFailureMsg(string msg)
        {
            if (!_failureMsgs.Contains(msg))
                _failureMsgs.Add(msg);
        }

        public void AddMsg(string msg)
        {
            if (_msgs == null)
                _msgs = new List<string>();
            msg = MathSolver.FinalizeOutput(msg);
            if (!_msgs.Contains(msg))
            {
                _msgs.Add(msg);
            }
        }

        public void AddPartialSol(ExComp ex)
        {
            if (_partialSols == null)
                _partialSols = new List<ExComp>();
            _partialSols.Add(ex);
        }

        public string PartialSolToMathAsciiStr(int index)
        {
            ExComp partialSol = _partialSols[index];
            AlgebraTerm term = partialSol.ToAlgTerm();
            string finalStr;
            if (term != null)
                finalStr = term.FinalToDispStr();
            else
                finalStr = partialSol.ToAsciiString();

            return MathSolver.FinalizeOutput(finalStr);
        }

        public string PartialSolToTexStr(int index)
        {
            ExComp partialSol = _partialSols[index];
            AlgebraTerm term = partialSol.ToAlgTerm();
            string finalStr;
            if (term != null)
                finalStr = term.FinalToTexString();
            else
                finalStr = partialSol.ToTexString();

            return finalStr;
        }
    }
}