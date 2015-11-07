using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class AbsoluteValueSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public AbsoluteValueSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            pEvalData.SetCheckSolutions(true);

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);
            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }
            CombineFractions(ref left, ref right, ref pEvalData);
            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData, false);

            pEvalData.AttemptSetInputType(TermType.InputType.AbsoluteValue);

            if (left.GetGroupCount() == 1)
            {
                AbsValFunction absValFunc = left.RemoveRedundancies(false) as AbsValFunction;
                if (absValFunc == null)
                {
                    pEvalData.AddFailureMsg("Couldn't solve absolute value equation.");
                    return null;
                }

                ExComp rightEx = right.RemoveRedundancies(false);
                if (rightEx is ExNumber && !(rightEx as ExNumber).HasImaginaryComp() && ExNumber.OpLT((rightEx as ExNumber), -1.0))
                {
                    pEvalData.GetWorkMgr().FromSides(left, right, "An absolute value will never equal a negative number. So there are no solutions to this equation.");
                    return new NoSolutions();
                }

                AlgebraTerm innerTerm = absValFunc.GetInnerTerm();
                if (right.IsZero())
                {
                    pEvalData.GetWorkMgr().FromSides(left, right, "The absolute value has no effect as it equals zero which is neither positive or negative.");
                    ExComp agSolved = p_agSolver.Solve(solveFor, innerTerm, right, ref pEvalData);
                    return agSolved;
                }
                AlgebraTerm solve1 = right;
                AlgebraTerm solve2 = MulOp.StaticCombine(ExNumber.GetNegOne(), right).ToAlgTerm();

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}=\\pm({1})" + WorkMgr.EDM, "The absolute value function allows the right hand side to be both positive and negative.", absValFunc, right);

                AlgebraTermArray termArray = new AlgebraTermArray(solve1, solve2);
                string[] solveDescs = new string[] { "Solve for the positive case.", "Solve for the negative case" };
                termArray.SetSolveDescs(solveDescs);
                bool allSols;
                AlgebraTermArray solvedTermArray = termArray.SimulSolve(innerTerm, solveFor, p_agSolver, ref pEvalData, out allSols, false);
                if (allSols)
                    return new AllSolutions();
                if (solvedTermArray == null)
                    return null;

                return solvedTermArray;
            }
            else if (left.GetGroupCount() == 2 && right.IsZero())
            {
                List<ExComp[]> groups = left.GetGroups();
                AlgebraTerm groupTerm0 = GroupHelper.ToAlgTerm(groups[0]);
                AlgebraTerm groupTerm1 = GroupHelper.ToAlgTerm(groups[1]);
                groupTerm1 = MulOp.Negate(groupTerm1).ToAlgTerm();

                groupTerm0 = AdvAlgebraTerm.AbsValToParas(groupTerm0);
                groupTerm1 = AdvAlgebraTerm.AbsValToParas(groupTerm1);

                // Solve when one is negative and when they are both positive.

                pEvalData.GetWorkMgr().FromSides(left, right, "Solve when one of the absolute values is negative and when they are both positive.");

                AlgebraTerm solve1 = groupTerm1;
                AlgebraTerm solve2 = MulOp.Negate(groupTerm1).ToAlgTerm();

                AlgebraTermArray termArray = new AlgebraTermArray(solve1, solve2);
                bool allSols;
                AlgebraTermArray solvedArray = termArray.SimulSolve(groupTerm0, solveFor, p_agSolver, ref pEvalData, out allSols, false);
                if (allSols)
                    return new AllSolutions();

                return solvedArray;
            }

            pEvalData.AddFailureMsg("There are too many absolute values for this equation to be solved!");
            return null;
        }
    }
}