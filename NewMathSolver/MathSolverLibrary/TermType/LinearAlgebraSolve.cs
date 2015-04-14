using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    class LinearAlgebraSolve : TermType
    {
        private ExComp _left;
        private ExComp _right;

        public LinearAlgebraSolve(EquationSet eqSet)
        {
            if (_left is ExVector && _right is ExVector)
            {
                // Do a combination of all the variables of each component.
                ExVector leftVec = _left as ExVector;
                ExVector rightVec = _right as ExVector;
            }

            List<string> cmds = new List<string>();
            cmds.Add("Not supported yet.");

            _cmds = cmds.ToArray();
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            return SolveResult.Failure();
        }
    }
}
