using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    class LinearAlgebraSolve : TermType
    {
        private string GAUSSIAN_SOLVE = "Gaussian solve for ";
        private string INVERSE_SOLVE = "Solve using inverses";

        /// <summary>
        /// Can either be a vector or a variable.
        /// </summary>
        private ExMatrix _B = null;
        private bool _aFirst = true;
        private AlgebraComp _x;
        private ExMatrix _X;
        private ExMatrix _A;
        private EquationSystemTermType _systemType = null;

        public LinearAlgebraSolve(EqSet eqSet, string probSolveVar)
        {
            // In the form Ax=b
            ExComp left = eqSet.Left;
            ExComp right = eqSet.Right;
            const string errorMsg = "Cannot solve";

            if (AttemptInit(left, right, probSolveVar))
            {
                if (_cmds != null && _cmds.Length > 0)
                    return;
                if (_A.IsSquare && AreMultiplicationsValid())
                    _cmds = new string[] { INVERSE_SOLVE };
            }

            if (_cmds == null || _cmds.Length == 0)
                _cmds = new string[] { errorMsg };
        }

        private bool AreMultiplicationsValid()
        {
            if (_aFirst)
            {
                return (_A.Cols == _B.Rows && (_X == null || (_X.Rows == _A.Cols)));
            }
            else
            {
                return (_A.Rows == _B.Cols && (_X == null || (_X.Cols == _A.Rows)));
            }
        }

        private bool AttemptInit(ExComp left, ExComp right, string probSolveVar)
        {
            AlgebraTerm leftTerm = left.ToAlgTerm();
            AlgebraTerm rightTerm = right.ToAlgTerm();
            if (AttemptInitLinearCombination(leftTerm, rightTerm))
                return true;
            if (AttemptInitVectorEq(left, right, probSolveVar))
                return true;

            return false;
        }

        private bool AttemptInitVectorEq(ExComp left, ExComp right, string probSolveVar)
        {
            ExMatrix mat = null;
            AlgebraTerm term = null;
            if (left is AlgebraTerm && right is ExMatrix)
            {
                mat = right as ExMatrix;
                term = left as AlgebraTerm;
            }
            else if (right is AlgebraTerm && left is ExMatrix)
            {
                mat = left as ExMatrix;
                term = right as AlgebraTerm;
            }

            _B = mat;

            // Both in the forms...
            // Ax=B
            // AX=B

            List<ExComp[]> groups = term.GetGroupsNoOps();
            if (groups.Count != 1 && groups[0].Length == 2)
                return false;

            ExComp ex0 = groups[0][0];
            ExComp ex1 = groups[0][1];

            if (ex0 is ExMatrix && ex1 is ExMatrix)
            {
                ExMatrix mat0 = ex0 as ExMatrix;
                ExMatrix mat1 = ex1 as ExMatrix;
                if (mat0.Contains(new AlgebraComp(probSolveVar)))
                {
                    _A = mat1;
                    _X = mat0;
                    _aFirst = false;
                }
                else if (mat1.Contains(new AlgebraComp(probSolveVar)))
                {
                    _A = mat0;
                    _X = mat1;
                    _aFirst = true;
                }
                else
                {
                    // Neither of them contain the probable solve var.
                    List<string> mat0AllVars = mat0.GetAllAlgebraCompsStr();
                    List<string> mat1AllVars = mat1.GetAllAlgebraCompsStr();

                    // Which one has more solve vars?
                    _X = mat0AllVars.Count > mat1AllVars.Count ? mat0 : mat1;
                    _A = _X == mat0 ? mat1 : mat0;
                    _aFirst = _X == mat0 ? false : true;
                }
            }
            else if (ex0 is ExMatrix && ex1 is AlgebraComp)
            {
                _A = ex0 as ExMatrix;
                _x = ex1 as AlgebraComp;
                _aFirst = true;
            }
            else if (ex0 is AlgebraComp && ex1 is ExMatrix)
            {
                _A = ex1 as ExMatrix;
                _x = ex0 as AlgebraComp;
                _aFirst = false;
            }
            else
                return false;

            return true;
        }

        private bool AttemptInitLinearCombination(AlgebraTerm leftTerm, AlgebraTerm rightTerm)
        {
            List<ExComp[]> leftGroups = leftTerm.GetGroupsNoOps();
            List<ExComp[]> rightGroups = rightTerm.GetGroupsNoOps();

            ExVector leftCompacted = CompactToSingleVec(leftGroups);
            if (leftCompacted == null)
                return false;
            ExVector rightCompacted = CompactToSingleVec(rightGroups);
            if (rightCompacted == null)
                return false;

            if (leftCompacted.Length != rightCompacted.Length)
                return false;

            List<EqSet> eqSets = new List<EqSet>();

            for (int i = 0; i < leftCompacted.Length; ++i)
            {
                eqSets.Add(new EqSet(leftCompacted[i], rightCompacted[i], LexemeType.EqualsOp));
            }

            _systemType = new EquationSystemTermType(eqSets);
            _cmds = _systemType.GetCommands();

            return true;
        }

        private ExVector CompactToSingleVec(List<ExComp[]> groups)
        {
            ExVector leftTotalVec = null;
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] leftGroup = groups[i];
                if (leftGroup.Length != 1)
                    return null;
                ExComp singleMem = leftGroup[0];
                if (!(singleMem is ExVector))
                    return null;
                ExVector exVec = singleMem as ExVector;
                if (leftTotalVec == null)
                    leftTotalVec = exVec;
                else
                {
                    if (leftTotalVec.Length != exVec.Length)
                        return null;
                    leftTotalVec = AddOp.StaticCombine(leftTotalVec, exVec) as ExVector;
                    if (leftTotalVec == null)
                        return null;
                }
            }

            return leftTotalVec;
        }

        private SolveResult SolveInverse(ref EvalData pEvalData)
        {
            ExMatrix bInverse = _B.GetInverse();
            if (bInverse == null)
            {
                return SolveResult.Failure("No inverse exists.", ref pEvalData);
            }

            ExComp result = _aFirst ? MulOp.StaticCombine(bInverse, _B) : MulOp.StaticCombine(_B, bInverse);

            return SolveResult.Solved(_x == null ? (ExComp)_X : _x, result, ref pEvalData);
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            if (_systemType != null)
                return _systemType.ExecuteCommand(command, ref pEvalData);
            else if (command.StartsWith(GAUSSIAN_SOLVE))
            {
                return SolveResult.Failure();
            }
            else if (command.StartsWith(INVERSE_SOLVE))
            {
                if (_B == null || _A == null || (_x == null && _X == null))
                    return SolveResult.Failure();

                return SolveInverse(ref pEvalData);
            }

            return SolveResult.Failure();
        }
    }
}
