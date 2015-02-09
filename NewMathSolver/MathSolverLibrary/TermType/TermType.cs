using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Collections.Generic;
using System.Linq;

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

        public Solving.QuadraticSolveMethod QuadSolveMethod
        {
            get { return _quadSolveMethod; }
            set { _quadSolveMethod = value; } 
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
                finalStr = partialSol.ToMathAsciiString();

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

    internal abstract class TermType
    {
        protected string[] _cmds = null;

        public int CmdCount
        {
            get { return _cmds.Length; }
        }

        public TermType(params string[] cmds)
        {
            _cmds = cmds;
        }

        public TermType()
        {
        }

        public abstract SolveResult ExecuteCommand(string command, ref EvalData pEvalData);

        public virtual SolveResult ExecuteCommandIndex(int cmdIndex, ref EvalData pEvalData)
        {
            if (_cmds == null || cmdIndex < 0 || cmdIndex >= _cmds.Length)
                return SolveResult.Failure();

            return ExecuteCommand(_cmds[cmdIndex], ref pEvalData);
        }

        public virtual string[] GetCommands()
        {
            return _cmds;
        }

        public virtual bool IsValidCommand(string cmd)
        {
            if (_cmds.Contains(cmd))
                return true;
            return false;
        }
    }
}