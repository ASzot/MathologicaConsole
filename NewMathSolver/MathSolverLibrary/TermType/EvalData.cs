using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class EvalData
    {
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
        public string InputTypeStr
        {
            get
            {
                return InputTypeHelper.ToDescStr(_inputType, _inputAddType);
            }
        }

        public Solving.QuadraticSolveMethod QuadSolveMethod
        {
            get { return _quadSolveMethod; }
            set { _quadSolveMethod = value; }
        }

        public string GraphVar
        {
            get { return _graphVar; }
        }

        public string[] GraphEqStrs
        {
            get { return _graphEqStrs; }
            internal set
            {
                _graphEqStrs = new string[value.Length];
                for (int i = 0; i < value.Length; ++i)
                {
                    _graphEqStrs[i] = value[i].Replace("+-", "-");
                }
            }
        }

        public Information_Helpers.FuncDefHelper FuncDefs
        {
            get { return _funcDefs; }
        }

        public bool CheckSolutions
        {
            get { return _checkSolutions; }
            set { _checkSolutions = value; }
        }

        public bool IsWorkable
        {
            get { return _isWorkable; }
            set { _isWorkable = value; }
        }

        public int NegDivCount
        {
            get { return _negDivCount; }
            set { _negDivCount = value; }
        }

        public List<string> FailureMsgs
        {
            get { return _failureMsgs; }
        }

        public bool HasPartialSolutions
        {
            get { return _partialSols != null && _partialSols.Count != 0; }
        }

        public List<string> Msgs
        {
            get { return _msgs; }
        }

        public List<ExComp> PartialSolutions
        {
            get { return _partialSols; }
        }

        public virtual bool UseRad
        {
            get { return _useRad; }
        }

        /// <summary>
        /// Only use when the angle mode will be restored shortly after.
        /// </summary>
        /// <param name="useRad"></param>
        public void TmpSetUseRad(bool useRad)
        {
            _useRad = useRad;
        }

        public virtual WorkMgr WorkMgr
        {
            get { return _workMgr; }
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
                AlgebraTerm term = graphEx.ToAlgTerm();
                var vars = term.GetAllAlgebraCompsStr();
                if (vars.Count != 1)
                    return false;

                string graphStr = term.ToJavaScriptString(_useRad);
                if (graphStr != null)
                {
                    GraphEqStrs = new string[1] { graphStr };
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
                GraphEqStrs = new string[1] { str };
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
                GraphEqStrs = strs;
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
            if (!_msgs.Contains(msg))
                _msgs.Add(msg);
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
