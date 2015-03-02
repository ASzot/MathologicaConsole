using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class PowerSolve : SolveMethod
    {
        private AlgebraTerm _solveForPower;
        private AlgebraSolver p_agSolver;

        public PowerSolve(AlgebraSolver pAgSolver, ExComp powerFor)
        {
            p_agSolver = pAgSolver;
            _solveForPower = powerFor.Clone().ToAlgTerm();
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            pEvalData.CheckSolutions = true;

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            if (right.Contains(solveForComp) && !left.Contains(solveForComp))
            {
                AlgebraTerm leftTmp = left;
                left = right;
                right = leftTmp;

                if (pEvalData.NegDivCount != -1)
                    pEvalData.NegDivCount++;
            }

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);
            if (!(left is PowerFunction) || !(right is PowerFunction))
                VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }
            CombineFractions(ref left, ref right, ref pEvalData);
            if (left.GroupCount != 1)
            {
                if (_solveForPower.IsSimpleFraction())
                {
                    // We are dealing with simple radicals.
                    return SolveMultipleRadicalEq(left, right, solveFor, ref pEvalData);
                }
                else
                {
                    // We have powers.
                    // Expand out and try to simplify then solve as a polynomial.
                    return null;
                }
            }

            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData);

            // Get the reciprocal of the power.
            ExComp tmp = left.RemoveRedundancies();
            if (!(tmp is PowerFunction))
            {
                return null;
            }
            PowerFunction powFunc = tmp as PowerFunction;

            ExComp power = powFunc.Power;

            // We can't be taking the root of the variable we are trying to solve for.
            if (power.ToAlgTerm().Contains(solveForComp))
            {
                pEvalData.AddFailureMsg("Can't solve this type of equation where the variable is present in the power as well as the base.");
                return null;
            }

            ExComp reciprocalPow;
            if (power is AlgebraTerm && (power as AlgebraTerm).ContainsOnlyFractions())
            {
                AlgebraTerm powerTerm = power as AlgebraTerm;
                var numDen = powerTerm.GetNumDenFrac();
                if (numDen == null)
                    return null;
                reciprocalPow = AlgebraTerm.FromFraction(numDen[1], numDen[0]);
            }
            else
                reciprocalPow = AlgebraTerm.FromFraction(Number.One, power);

            if (power is Number)
            {
                Number nPower = power as Number;
                string rootStr;
                string rootName;
                if (nPower == 2.0)
                {
                    rootName = "square";
                    rootStr = "sqrt";
                }
                else if (nPower == 3.0)
                {
                    rootName = "cube";
                    rootStr = "root(" + (power as Number).FinalToDispString() + ")";
                }
                else
                {
                    rootName = WorkMgr.STM + nPower.FinalToDispString() + WorkMgr.EDM + nPower.GetCountingPrefix();
                    rootStr = "root(" + (power as Number).FinalToDispString() + ")";
                }

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + rootStr + "({0})=" + rootStr + "({1})" + WorkMgr.EDM, "Take the " +
                    rootName +
                    " root of both sides. " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " results must exist", left, right, power);

                // The powers should cancel.
                left = PowOp.StaticCombine(left, reciprocalPow).ToAlgTerm();

                ExComp tmpRight = PowOp.TakeRoot(right.RemoveRedundancies(), power as Number, ref pEvalData, true);
                if (tmpRight is AlgebraTermArray)
                {
                    AlgebraTermArray rights = tmpRight as AlgebraTermArray;
                    string[] descs = new string[rights.TermCount];
                    for (int i = 0; i < rights.TermCount; ++i)
                    {
                        if (left.SubComps.Count == 1 && left.SubComps[0] is AlgebraComp)
                            descs[i] = "Above is a solution for " + WorkMgr.STM + solveForComp.ToDispString() + WorkMgr.EDM + ".";
                        else
                            descs[i] = "Solve with the found " + (i + 1).ToString() + (i + 1).GetCountingPrefix() + " root.";
                    }
                    rights.SolveDescs = descs;
                    bool allSols;
                    AlgebraTermArray solutions = rights.SimulSolve(left, solveFor, p_agSolver, ref pEvalData, out allSols);
                    if (allSols)
                        return new AllSolutions();
                    if (solutions == null)
                        return null;

                    return solutions;
                }
                else
                {
                    pEvalData.WorkMgr.FromSides(left, tmpRight, "Simplify.");
                    right = tmpRight.ToAlgTerm();
                }
            }
            else
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "({0})^({2})=({1})^({2})" + WorkMgr.EDM, "Raise both sides to the " + WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(reciprocalPow) + WorkMgr.EDM + " power.", left, right, reciprocalPow);

                // The powers should cancel.
                left = PowOp.StaticCombine(left, reciprocalPow).ToAlgTerm();

                right = PowOp.StaticCombine(right, reciprocalPow).ToAlgTerm();

                pEvalData.WorkMgr.FromSides(left, right, "Simplify.");
            }

            return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
        }

        private ExComp Solve_N_Group_2_Root_Eq(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor,
            SimpleFraction solveForPowFrac, ref TermType.EvalData pEvalData)
        {
            var groups = left.GetGroups();

            var subGroup = groups[1].ToAlgTerm();

            pEvalData.WorkMgr.FromSubtraction(subGroup, left, right);

            left = left - subGroup;
            right = right - subGroup;

            pEvalData.WorkMgr.FromSides(left, right);

            if (!solveForPowFrac.Num.IsOne())
            {
                pEvalData.AddFailureMsg("Cannot deal with radical index.");
                return null;
            }

            if (solveForPowFrac.DenEx is Number)
            {
                Number root = solveForPowFrac.DenEx as Number;

                pEvalData.WorkMgr.FromFormatted("`({1})^({0})=({2})^({0})`", "Raise both sides to the `{0}` power.", root, left, right);

                left = left.RaiseToPow(root, ref pEvalData).ToAlgTerm();
                right = right.RaiseToPow(root, ref pEvalData).ToAlgTerm();

                pEvalData.WorkMgr.FromSides(left, right);

                pEvalData.WorkMgr.FromSubtraction(right, left, right);

                left = SubOp.StaticCombine(left, right.Clone()).ToAlgTerm();

                left = left.RemoveZeros();
                right = Number.Zero.ToAlgTerm();

                pEvalData.WorkMgr.FromSides(left, right);

                return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            }

            pEvalData.AddFailureMsg("There is a variable power which can't be dealt with.");
            return null;
        }

        private ExComp SolveMultipleRadicalEq(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            int groupCount = left.GroupCount;

            SimpleFraction frac = new SimpleFraction();
            if (!frac.Init(_solveForPower))
                return null;

            if (groupCount == 2)
            {
                return Solve_N_Group_2_Root_Eq(left, right, solveFor, frac, ref pEvalData);
            }

            return null;
        }
    }
}