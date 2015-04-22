using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    class LinearAlgebraSolve : TermType
    {
        private string GAUSSIAN_SOLVE = "Gaussian solve for ";

        /// <summary>
        /// Can either be a vector or a variable.
        /// </summary>
        private ExComp _b;
        private AlgebraComp _solveFor;
        private ExMatrix _A;

        public LinearAlgebraSolve(EqSet eqSet)
        {
            // In the form Ax=b

            ExComp other = null;
            if (eqSet.Right is AlgebraComp || eqSet.Right is ExVector)
            {
                _b = eqSet.Right;
                other = eqSet.Left;
            }
            else if (eqSet.Left is AlgebraComp || eqSet.Left is ExVector)
            {
                _b = eqSet.Left;
                other = eqSet.Right;
            }

            List<string> cmds = new List<string>();

            const string errorMsg = "Cannot solve";

            if (other == null || !(other is AlgebraTerm))
            {
                _cmds = new string[] { errorMsg};
                return;
            }

            AlgebraTerm otherTerm = other as AlgebraTerm;
            var groups = otherTerm.GetGroupsNoOps();
            if (groups.Count != 1 || groups[0].Length != 2)
            {
                _cmds = new string[] { errorMsg };
                return;
            }

            ExComp gp0 = groups[0][0];
            ExComp gp1 = groups[0][1];

            if (gp0 is AlgebraTerm)
                gp0 = (gp0 as AlgebraTerm).RemoveRedundancies();
            if (gp1 is AlgebraTerm)
                gp1 = (gp1 as AlgebraTerm).RemoveRedundancies();

            if (gp0 is ExMatrix && gp1 is AlgebraComp)
            {
                _A = gp0 as ExMatrix;
                _solveFor = gp1 as AlgebraComp;
            }
            else if (gp1 is ExMatrix && gp0 is AlgebraComp)
            {
                _solveFor = gp0 as AlgebraComp;
                _A = gp1 as ExMatrix;
            }
            else
            {
                _cmds = new string[] { errorMsg };
                return;
            }

            _cmds = new string[] { GAUSSIAN_SOLVE + _solveFor.ToString() };
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (command.StartsWith(GAUSSIAN_SOLVE))
            {
                return SolveResult.Failure();
            }

            return SolveResult.Failure();
        }
    }
}
