using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Solving.Diff_Eqs
{
    internal class DiffAgSolver
    {
        public static bool ContainsDerivative(ExComp ex)
        {
            if (ex is Derivative)
                return true;
            else if (ex is AlgebraTerm)
            {
                AlgebraTerm term = ex as AlgebraTerm;
                foreach (ExComp subEx in term.GetSubComps())
                {
                    if (ContainsDerivative(subEx))
                        return true;
                }
            }

            return false;
        }

        public static bool ContainsDerivative(ExComp[] gp)
        {
            foreach (ExComp ex in gp)
            {
                if (ContainsDerivative(ex))
                    return true;
            }

            return false;
        }

        private static ExComp[] SolveDiffEq(AlgebraTerm ex0Term, AlgebraTerm ex1Term, AlgebraComp solveForFunc,
            AlgebraComp withRespect, int order, ref TermType.EvalData pEvalData)
        {
            ExComp[] atmpt = null;
            int prevWorkStepCount;

            DiffSolve[] diffSolves = new DiffSolve[] { new SeperableSolve(), new HomogeneousSolve(), new IntegratingFactorSolve(), new ExactEqsSolve() };

            for (int i = 0; i < diffSolves.Length; ++i)
            {
                // Try separable differential equations.
                prevWorkStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
                atmpt = diffSolves[i].Solve((AlgebraTerm)ex0Term.CloneEx(), (AlgebraTerm)ex1Term.CloneEx(), solveForFunc, withRespect, ref pEvalData);
                if (atmpt != null)
                {
                    if (!(atmpt[0] is Integral || atmpt[1] is Integral))
                    {
                        // Add on a constant that will have the properties of a variable.
                        AlgebraComp varConstant = new AlgebraComp("$C");
                        atmpt[1] = Equation.Operators.AddOp.StaticCombine(atmpt[1], varConstant);

                        pEvalData.GetWorkMgr().FromSides(atmpt[0], atmpt[1], "Add the constant of integration.");
                    }

                    return atmpt;
                }
                else
                    pEvalData.GetWorkMgr().PopSteps(prevWorkStepCount);
            }

            return null;
        }

        public static SolveResult Solve(ExComp ex0, ExComp ex1, AlgebraComp solveForFunc, AlgebraComp withRespect, int order, ref TermType.EvalData pEvalData)
        {
            if (order > 1)
            {
                SolveResult failSolved = SolveResult.Failure("Cannot solve differential equations with an order greater than one", ref pEvalData);
                return failSolved;
            }

            ExComp[] leftRight = SolveDiffEq(ex0.ToAlgTerm(), ex1.ToAlgTerm(), solveForFunc, withRespect, order, ref pEvalData);
            if (leftRight == null)
                return SolveResult.Failure();

            Solution genSol = new Solution(leftRight[0], leftRight[1]);
            genSol.IsGeneral = true;

            if (leftRight[0] is Integral || leftRight[1] is Integral)
            {
                SolveResult resultSolved = SolveResult.Solved(leftRight[0], leftRight[1], ref pEvalData);
                return resultSolved;
            }

            AlgebraSolver agSolver = new AlgebraSolver();

            int startStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());

            pEvalData.GetWorkMgr().FromFormatted("", "Solve for " + WorkMgr.STM + solveForFunc.ToDispString() + WorkMgr.EDM);
            WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp solved = agSolver.SolveEq(solveForFunc.GetVar(), leftRight[0].CloneEx().ToAlgTerm(), leftRight[1].CloneEx().ToAlgTerm(), ref pEvalData);
            lastStep.GoUp(ref pEvalData);

            if (solved == null)
            {
                pEvalData.GetWorkMgr().PopSteps(startStepCount);
                SolveResult resultSolved = SolveResult.Solved(leftRight[0], leftRight[1], ref pEvalData);
                return resultSolved;
            }

            lastStep.SetWorkHtml(WorkMgr.STM + solveForFunc.ToDispString() + " = " + WorkMgr.ToDisp(solved) + WorkMgr.EDM);

            SolveResult solveResult = SolveResult.Solved(solveForFunc, solved, ref pEvalData);
            solveResult.Solutions.Insert(0, genSol);

            return solveResult;
        }
    }
}