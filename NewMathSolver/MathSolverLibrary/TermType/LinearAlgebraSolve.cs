using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class LinearAlgebraSolve : GenTermType
    {
        private string GAUSSIAN_SOLVE = "Gaussian solve for ";
        private string INVERSE_SOLVE = "Solve using inverses";
        private const string ERROR_MSG = "Cannot solve";

        /// <summary>
        /// Can either be a vector or a variable.
        /// </summary>
        private ExMatrix _B = null;

        private bool _aFirst = true;
        private AlgebraComp _x;
        private ExMatrix _X;
        private ExMatrix _A;
        private List<EqSet> _eqSets = null;

        public LinearAlgebraSolve(EqSet eqSet, string probSolveVar, ref EvalData pEvalData)
        {
            // In the form Ax=b
            ExComp left = eqSet.GetLeft();
            ExComp right = eqSet.GetRight();

            if (AttemptInit(left, right, probSolveVar, ref pEvalData))
            {
                if (_cmds != null && _cmds.Length > 0)
                    return;
                if (_A != null && _A.GetIsSquare() && AreMultiplicationsValid())
                    _cmds = new string[] { INVERSE_SOLVE };
            }

            if (_cmds == null || _cmds.Length == 0)
                _cmds = new string[] { ERROR_MSG };
        }

        private bool AreMultiplicationsValid()
        {
            if (_aFirst)
            {
                return (_A.GetCols() == _B.GetRows() && (_X == null || (_X.GetRows() == _A.GetCols())));
            }
            else
            {
                return (_A.GetRows() == _B.GetCols() && (_X == null || (_X.GetCols() == _A.GetRows())));
            }
        }

        private bool AttemptInit(ExComp left, ExComp right, string probSolveVar, ref EvalData pEvalData)
        {
            AlgebraTerm leftTerm = left.ToAlgTerm();
            AlgebraTerm rightTerm = right.ToAlgTerm();
            if (AttemptInitLinearCombination(leftTerm, rightTerm, ref pEvalData))
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

            if (term == null || mat == null)
                return false;

            _B = mat;

            // Both in the forms...
            // Ax=B
            // AX=B

            List<ExComp[]> groups = term.GetGroupsNoOps();
            if (groups.Count != 1 || groups[0].Length != 2)
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

        /// <summary>
        /// These are the vector equations where the variable is a scalar quantity.
        /// </summary>
        /// <param name="leftTerm"></param>
        /// <param name="rightTerm"></param>
        /// <returns></returns>
        private bool AttemptInitLinearCombination(AlgebraTerm leftTerm, AlgebraTerm rightTerm, ref EvalData pEvalData)
        {
            List<ExComp[]> leftGroups = leftTerm.GetGroupsNoOps();
            List<ExComp[]> rightGroups = rightTerm.GetGroupsNoOps();

            ExVector leftCompacted = CompactToSingleVec(leftGroups);
            if (leftCompacted == null)
                return false;
            ExVector rightCompacted = CompactToSingleVec(rightGroups);
            if (rightCompacted == null)
                return false;

            if (leftCompacted.GetLength() != rightCompacted.GetLength() || leftCompacted.GetLength() < 2)
                return false;

            _eqSets = new List<EqSet>();

            List<string> solveVarsLeft = new List<string>();
            List<string> solveVarsRight = new List<string>();
            for (int i = 0; i < leftCompacted.GetLength(); ++i)
            {
                ExComp leftEx = leftCompacted.Get(i);
                ExComp rightEx = rightCompacted.Get(i);

                List<string> leftVars = leftEx.ToAlgTerm().GetAllAlgebraCompsStr();
                List<string> rightVars = rightEx.ToAlgTerm().GetAllAlgebraCompsStr();

                for (int j = 0; j < solveVarsLeft.Count; ++j)
                {
                    if (!leftVars.Contains(solveVarsLeft[j]))
                        ArrayFunc.RemoveIndex(solveVarsLeft, j--);
                }

                for (int j = 0; j < solveVarsRight.Count; ++j)
                {
                    if (!rightVars.Contains(solveVarsRight[j]))
                        ArrayFunc.RemoveIndex(solveVarsRight, j--);
                }

                foreach (string leftVar in leftVars)
                {
                    if (!solveVarsLeft.Contains(leftVar))
                        solveVarsLeft.Add(leftVar);
                }

                foreach (string rightVar in rightVars)
                {
                    if (!solveVarsRight.Contains(rightVar))
                        solveVarsRight.Add(rightVar);
                }

                _eqSets.Add(new EqSet(leftEx, rightEx, LexemeType.EqualsOp));
            }

            List<string> cmdsList = new List<string>();
            // Remove any intersection.
            for (int i = 0; i < solveVarsLeft.Count; ++i)
            {
                if (solveVarsRight.Contains(solveVarsLeft[i]))
                {
                    solveVarsRight.Remove(solveVarsLeft[i]);
                    ArrayFunc.RemoveIndex(solveVarsLeft, i--);
                }
            }

            List<string> cmds = new List<string>();
            if (solveVarsLeft.Count != 0 && solveVarsRight.Count != 0)
            {
                for (int i = 0; i < solveVarsLeft.Count; ++i)
                {
                    for (int j = 0; j < solveVarsRight.Count; ++j)
                    {
                        cmds.Add("Solve for " + solveVarsLeft[i] + "," + solveVarsRight[i]);
                    }
                }
            }

            // Compound as if there is only one variable term and the rest are constants.
            solveVarsLeft.AddRange(solveVarsRight);

            for (int i = 0; i < solveVarsLeft.Count; ++i)
            {
                cmds.Add("Solve for " + solveVarsLeft[i]);
            }

            _cmds = cmds.ToArray();

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
                    if (leftTotalVec.GetLength() != exVec.GetLength())
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
            ExMatrix aInverse = _A.GetInverse();
            if (aInverse == null)
            {
                SolveResult noExistResult = SolveResult.Failure("No inverse exists.", ref pEvalData);
                return noExistResult;
            }

            ExComp result = _aFirst ? MulOp.StaticCombine(aInverse, _B) : MulOp.StaticCombine(_B, aInverse);
            result = Simplifier.Simplify(result.ToAlgTerm(), ref pEvalData);

            SolveResult solveResult = SolveResult.Solved(_x == null ? (ExComp)_X : _x, result, ref pEvalData);
            return solveResult;
        }

        private SolveResult SolveVectorEquation(string solveForStr, ref EvalData pEvalData)
        {
            AlgebraSolver agSolver = new AlgebraSolver();

            if (solveForStr.Contains(","))
            {
                string[] solveVars = solveForStr.Split(',');

                // First do a system of equations.
                EqSet eq0 = _eqSets[0];
                EqSet eq1 = _eqSets[1];

                List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>> lts = new List<List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>>();

                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt0 = new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>();
                lt0.Add(new TypePair<LexemeType, string>(LexemeType.Identifier, solveVars[0]));
                lt0.Add(new TypePair<LexemeType, string>(LexemeType.Identifier, solveVars[1]));

                lts.Add(lt0);
                lts.Add(new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>());

                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt1 = new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>();
                lt1.Add(new TypePair<LexemeType, string>(LexemeType.Identifier, solveVars[0]));
                lt1.Add(new TypePair<LexemeType, string>(LexemeType.Identifier, solveVars[1]));

                lts.Add(lt1);
                lts.Add(new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>());

                Dictionary<string, int> allVars = new Dictionary<string, int>();
                allVars.Add(solveVars[0], 1);
                allVars.Add(solveVars[1], 1);

                Solving.EquationSystemSolve solveMethod = new Solving.EquationSystemSolve(agSolver);
                SolveResult sysSolveResult = solveMethod.SolveEquationArray(_eqSets.GetRange(0, 2), lts, allVars, ref pEvalData);

                // Check that the results hold in the other equations.
                for (int i = 2; i < _eqSets.Count; ++i)
                {
                    AlgebraTerm leftSubbed = _eqSets[i].GetLeftTerm();
                    AlgebraTerm rightSubbed = _eqSets[i].GetRightTerm();
                    foreach (Solution sol in sysSolveResult.Solutions)
                    {
                        if (sol.Result is NoSolutions)
                            return SolveResult.Failure();
                        if (sol.Result is AllSolutions)
                            continue;
                        leftSubbed = leftSubbed.Substitute(sol.SolveFor, sol.Result);
                        rightSubbed = rightSubbed.Substitute(sol.SolveFor, sol.Result);
                    }

                    ExComp leftEx = leftSubbed.MakeWorkable();
                    ExComp rightEx = rightSubbed.MakeWorkable();

                    if (leftEx is AlgebraTerm)
                        leftEx = (leftEx as AlgebraTerm).RemoveRedundancies(false);
                    if (rightEx is AlgebraTerm)
                        rightEx = (rightEx as AlgebraTerm).RemoveRedundancies(false);

                    if (!leftEx.IsEqualTo(rightEx))
                    {
                        return SolveResult.Simplified(ExNumber.GetUndefined());
                    }
                }

                return sysSolveResult;
            }

            AlgebraVar solveForVar = new AlgebraVar(solveForStr);
            ExComp solveResult = null;
            int end = -1;

            for (int i = 0; i < _eqSets.Count; ++i)
            {
                ExComp tmpResult = agSolver.SolveEq(solveForVar, _eqSets[i].GetLeftTerm(), _eqSets[i].GetRightTerm(), ref pEvalData);
                if (i == 0)
                    end = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                if (tmpResult is AlgebraTerm)
                    tmpResult = (tmpResult as AlgebraTerm).RemoveRedundancies(false);

                if (tmpResult is AllSolutions)
                    continue;

                if (solveResult == null)
                    solveResult = tmpResult;
                else if (!solveResult.IsEqualTo(tmpResult))
                {
                    SolveResult solved = SolveResult.Solved(solveForVar, ExNumber.GetUndefined(), ref pEvalData);
                    return solved;
                }
            }

            pEvalData.GetWorkMgr().PopSteps(end);

            SolveResult solvedResult = SolveResult.Solved(solveForVar, solveResult, ref pEvalData);
            return solvedResult;
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            if (command.StartsWith("Solve for ") && _eqSets != null)
            {
                string solveForStr = StringFunc.Rm(command, 0, "Solve for ".Length);
                SolveResult solveVector = SolveVectorEquation(solveForStr, ref pEvalData);
                return solveVector;
            }
            else if (command.StartsWith(GAUSSIAN_SOLVE))
            {
                return SolveResult.Failure();
            }
            else if (command.StartsWith(INVERSE_SOLVE))
            {
                if (_B == null || _A == null || (_x == null && _X == null))
                    return SolveResult.Failure();

                SolveResult solveInverse = SolveInverse(ref pEvalData);
                return solveInverse;
            }

            return SolveResult.Failure();
        }
    }
}