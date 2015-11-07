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
            _solveForPower = powerFor.CloneEx().ToAlgTerm();
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            pEvalData.SetCheckSolutions(true);

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            if (right.Contains(solveForComp) && !left.Contains(solveForComp))
            {
                AlgebraTerm leftTmp = left;
                left = right;
                right = leftTmp;

                if (pEvalData.GetNegDivCount() != -1)
                    pEvalData.SetNegDivCount(pEvalData.GetNegDivCount() + 1);
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
            if (left.GetGroupCount() != 1)
            {
                if (AdvAlgebraTerm.IsSimpleFraction(_solveForPower))
                {
                    // We are dealing with simple radicals.
                    ExComp radicalSolve = SolveMultipleRadicalEq(left, right, solveFor, ref pEvalData);
                    return radicalSolve;
                }
                else
                {
                    // We have powers.
                    // Expand out and try to simplify then solve as a polynomial.
                    return null;
                }
            }

            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData, false);

            // Get the reciprocal of the power.
            ExComp tmp = left.RemoveRedundancies(false);
            if (!(tmp is PowerFunction))
            {
                return null;
            }
            PowerFunction powFunc = tmp as PowerFunction;

            ExComp power = powFunc.GetPower();

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
                AlgebraTerm[] numDen = powerTerm.GetNumDenFrac();
                if (numDen == null)
                    return null;
                reciprocalPow = AlgebraTerm.FromFraction(numDen[1], numDen[0]);
            }
            else
                reciprocalPow = AlgebraTerm.FromFraction(ExNumber.GetOne(), power);

            if (power is ExNumber)
            {
                ExNumber nPower = power as ExNumber;
                string rootStr;
                string rootName;
                if (ExNumber.OpEqual(nPower, 2.0))
                {
                    rootName = "square";
                    rootStr = "sqrt";
                }
                else if (ExNumber.OpEqual(nPower, 3.0))
                {
                    rootName = "cube";
                    rootStr = "root(" + (power as ExNumber).FinalToDispString() + ")";
                }
                else
                {
                    rootName = WorkMgr.STM + nPower.FinalToDispString() + WorkMgr.EDM + nPower.GetCountingPrefix();
                    rootStr = "root(" + (power as ExNumber).FinalToDispString() + ")";
                }

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + rootStr + "({0})=" + rootStr + "({1})" + WorkMgr.EDM, "Take the " +
                    rootName +
                    " root of both sides. " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " results must exist", left, right, power);

                // The powers should cancel.
                left = PowOp.StaticCombine(left, reciprocalPow).ToAlgTerm();

                ExComp tmpRight = PowOp.TakeRoot(right.RemoveRedundancies(false), power as ExNumber, ref pEvalData, true);
                if (tmpRight is AlgebraTermArray)
                {
                    AlgebraTermArray rights = tmpRight as AlgebraTermArray;
                    string[] descs = new string[rights.GetTermCount()];
                    for (int i = 0; i < rights.GetTermCount(); ++i)
                    {
                        if (left.GetSubComps().Count == 1 && left.GetSubComps()[0] is AlgebraComp)
                            descs[i] = "Above is a solution for " + WorkMgr.STM + solveForComp.ToDispString() + WorkMgr.EDM + ".";
                        else
                            descs[i] = "Solve with the found " + (i + 1).ToString() + MathHelper.GetCountingPrefix((i + 1)) + " root.";
                    }
                    rights.SetSolveDescs(descs);
                    bool allSols;
                    AlgebraTermArray solutions = rights.SimulSolve(left, solveFor, p_agSolver, ref pEvalData, out allSols, false);
                    if (allSols)
                        return new AllSolutions();
                    if (solutions == null)
                        return null;

                    return solutions;
                }
                else
                {
                    pEvalData.GetWorkMgr().FromSides(left, tmpRight, "Simplify.");
                    right = tmpRight.ToAlgTerm();
                }
            }
            else
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})^({2})=({1})^({2})" + WorkMgr.EDM, "Raise both sides to the " + WorkMgr.STM + WorkMgr.ToDisp(reciprocalPow) + WorkMgr.EDM + " power.", left, right, reciprocalPow);

                // The powers should cancel.
                left = PowOp.StaticCombine(left, reciprocalPow).ToAlgTerm();

                right = PowOp.StaticCombine(right, reciprocalPow).ToAlgTerm();

                pEvalData.GetWorkMgr().FromSides(left, right, "Simplify.");
            }

            ExComp finalSolveResult = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            return finalSolveResult;
        }

        private ExComp Solve_N_Group_2_Root_Eq(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor,
            SimpleFraction solveForPowFrac, ref TermType.EvalData pEvalData)
        {
            System.Collections.Generic.List<ExComp[]> groups = left.GetGroups();

            AlgebraTerm subGroup = GroupHelper.ToAlgTerm(groups[1]);

            pEvalData.GetWorkMgr().FromSubtraction(subGroup, left, right);

            left = AlgebraTerm.OpSub(left, subGroup);
            right = AlgebraTerm.OpSub(right, subGroup);

            pEvalData.GetWorkMgr().FromSides(left, right);

            if (!solveForPowFrac.GetNum().IsOne())
            {
                pEvalData.AddFailureMsg("Cannot deal with radical index.");
                return null;
            }

            if (solveForPowFrac.GetDenEx() is ExNumber)
            {
                ExNumber root = solveForPowFrac.GetDenEx() as ExNumber;

                pEvalData.GetWorkMgr().FromFormatted("`({1})^({0})=({2})^({0})`", "Raise both sides to the `{0}` power.", root, left, right);

                left = AdvAlgebraTerm.RaiseToPow(left, root, ref pEvalData).ToAlgTerm();
                right = AdvAlgebraTerm.RaiseToPow(right, root, ref pEvalData).ToAlgTerm();

                pEvalData.GetWorkMgr().FromSides(left, right);

                pEvalData.GetWorkMgr().FromSubtraction(right, left, right);

                left = SubOp.StaticCombine(left, right.CloneEx()).ToAlgTerm();

                left = left.RemoveZeros();
                right = ExNumber.GetZero().ToAlgTerm();

                pEvalData.GetWorkMgr().FromSides(left, right);

                ExComp agSolveResult = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                return agSolveResult;
            }

            pEvalData.AddFailureMsg("There is a variable power which can't be dealt with.");
            return null;
        }

        private ExComp SolveMultipleRadicalEq(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            int groupCount = left.GetGroupCount();

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