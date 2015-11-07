using MathSolverWebsite.MathSolverLibrary.Equation;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal abstract class GenTermType
    {
        protected string[] _cmds = null;
        protected MultiLineHelper _multiLineHelper = null;

        public int GetCmdCount()
        {
            return _cmds.Length;
        }

        public GenTermType(params string[] cmds)
        {
            _cmds = cmds;
        }

        public GenTermType()
        {
        }

        public virtual void AttachMultiLineHelper(MultiLineHelper mlh)
        {
            _multiLineHelper = mlh;
        }

        public virtual SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (_multiLineHelper != null)
                _multiLineHelper.DoAssigns(ref pEvalData);

            return SolveResult.Failure();
        }

        public virtual SolveResult ExecuteCommandIndex(int cmdIndex, ref EvalData pEvalData)
        {
            if (_cmds == null || cmdIndex < 0 || cmdIndex >= _cmds.Length)
                return SolveResult.Failure();

            SolveResult executed = ExecuteCommand(_cmds[cmdIndex], ref pEvalData);
            return executed;
        }

        public virtual string[] GetCommands()
        {
            return _cmds;
        }

        public virtual bool IsValidCommand(string cmd)
        {
            if (ArrayFunc.Contains(_cmds, cmd))
                return true;
            return false;
        }
    }
}