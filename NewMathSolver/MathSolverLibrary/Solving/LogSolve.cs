using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class LogSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public LogSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public override Equation.ExComp SolveEquation(Equation.AlgebraTerm left, Equation.AlgebraTerm right, Equation.AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();
            if (right.Contains(solveForComp) && !left.Contains(solveForComp))
            {
                AlgebraTerm leftTmp = left;
                left = right;
                right = leftTmp;

                if (pEvalData.NegDivCount != -1)
                    pEvalData.NegDivCount++;
            }

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);

            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            left = left.CompoundLogs(solveForComp);
            right = right.CompoundLogs(solveForComp);

            var leftGroups = left.GetGroups();
            var rightGroups = right.GetGroups();
            if (leftGroups.Count == 1 && rightGroups.Count == 1 && left.Contains(solveForComp) && right.Contains(solveForComp))
            {
                left = left.ForceLogCoeffToPow();
                right = right.ForceLogCoeffToPow();
                ExComp log0Ex = left.RemoveRedundancies();
                ExComp log1Ex = right.RemoveRedundancies();

                if (log0Ex is LogFunction && log1Ex is LogFunction)
                {
                    LogFunction log0 = log0Ex as LogFunction;
                    LogFunction log1 = log1Ex as LogFunction;

                    if (log0.Base.IsEqualTo(log1.Base))
                    {
                        // Using the property log(x)=log(y) -> x = y
                        left = log0.InnerEx.ToAlgTerm();
                        right = log1.InnerEx.ToAlgTerm();
                        pEvalData.WorkMgr.FromSides(left, right, "By the rule if " + WorkMgr.STM + "log(x)=log(y)" + WorkMgr.EDM + " then " + WorkMgr.STM + "x=y" + WorkMgr.EDM);
                        return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                    }

                    if (log0.Base is Number && log1.Base is Number)
                    {
                        ExComp log0Inner = log0.InnerEx;
                        ExComp log1Inner = log1.InnerEx;

                        Number nLog0Base = log0.Base as Number;
                        Number nLog1Base = log1.Base as Number;

                        if (nLog0Base % nLog1Base == 0)
                        {
                            AlgebraComp tmpSolveFor = p_agSolver.NextSubVar();
                            AlgebraComp tmpSolveFor0 = new AlgebraComp(tmpSolveFor.Var.Var + "_0");
                            AlgebraComp tmpSolveFor1 = new AlgebraComp(tmpSolveFor.Var.Var + "_1");

                            string workStr = WorkStep.FormatStr("Given this it can be said " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor0), log0Inner);
                            workStr += WorkStep.FormatStr(" and " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor1), log1Inner);
                            workStr += " make the bases equal so the right hand sides can be set equal to each other.";

                            pEvalData.WorkMgr.FromSides(left, right, workStr);

                            ExponentSolve expSolve = new ExponentSolve(p_agSolver);

                            AlgebraTerm tmpLeft = PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor).ToAlgTerm();
                            AlgebraTerm tmpRight = nLog0Base.ToAlgTerm();

                            pEvalData.WorkMgr.AllowWork = false;
                            ExComp raiseTo = expSolve.SolveEquation(tmpLeft, tmpRight, tmpSolveFor.Var, ref pEvalData);
                            pEvalData.WorkMgr.AllowWork = true;

                            ExComp tmpRaiseTo = MulOp.StaticCombine(raiseTo, tmpSolveFor0);

                            log1Inner = PowOp.StaticCombine(log1Inner, raiseTo);

                            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Make the bases equal.", PowOp.StaticWeakCombine(nLog1Base, tmpRaiseTo), log1Inner);

                            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM + "<br />" + WorkMgr.STM + "{0}={2}" + WorkMgr.EDM, "The left hand sides are now equal. So " +
                                WorkMgr.STM + "{0}={1}={2}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor0), log0Inner, log1Inner);
                        }
                        else if (nLog1Base % nLog0Base == 0)
                        {
                            AlgebraComp tmpSolveFor = p_agSolver.NextSubVar();
                            AlgebraComp tmpSolveFor0 = new AlgebraComp(tmpSolveFor.Var.Var + "_0");
                            AlgebraComp tmpSolveFor1 = new AlgebraComp(tmpSolveFor.Var.Var + "_1");

                            string workStr = WorkStep.FormatStr("Given this it can be said " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor1), log1Inner);
                            workStr += WorkStep.FormatStr(" and " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor0), log0Inner);
                            workStr += " make the bases equal so the right hand sides can be set equal to each other.";

                            pEvalData.WorkMgr.FromSides(left, right, workStr);

                            ExponentSolve expSolve = new ExponentSolve(p_agSolver);

                            AlgebraTerm tmpLeft = PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor).ToAlgTerm();
                            AlgebraTerm tmpRight = nLog1Base.ToAlgTerm();

                            pEvalData.WorkMgr.AllowWork = false;
                            ExComp raiseTo = expSolve.SolveEquation(tmpLeft, tmpRight, tmpSolveFor.Var, ref pEvalData);
                            pEvalData.WorkMgr.AllowWork = true;

                            ExComp tmpRaiseTo = MulOp.StaticCombine(raiseTo, tmpSolveFor1);

                            log0Inner = PowOp.StaticCombine(log0Inner, raiseTo);

                            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Make the bases equal.", PowOp.StaticWeakCombine(nLog0Base, tmpRaiseTo), log0Inner);

                            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM + "<br />" + WorkMgr.STM + "{0}={2}" + WorkMgr.EDM,
                                "The left hand sides are now equal. So " + WorkMgr.STM + "{0}={1}={2}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor1), log1Inner, log0Inner);
                        }
                        else
                        {
                            pEvalData.AddFailureMsg("Can't convert to common base.");
                            return null;
                        }

                        pEvalData.WorkMgr.FromSides(log0Inner, log1Inner, "Solve this equation.");
                        return p_agSolver.SolveEq(solveFor, log0Inner.ToAlgTerm(), log1Inner.ToAlgTerm(), ref pEvalData);
                    }
                }
            }

            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                return new NoSolutions();
            }
            CombineFractions(ref left, ref right, ref pEvalData);

            left = left.CompoundLogs(solveForComp);
            right = right.CompoundLogs(solveForComp);

            leftGroups = left.GetGroups();

            if (leftGroups.Count != 1)
            {
                pEvalData.AddFailureMsg("Couldn't combine variable logarithms to singular term.");
                return null;
            }
            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData);

            ExComp leftEx = left.RemoveRedundancies();
            if (!(leftEx is LogFunction))
                return null;

            LogFunction log = leftEx as LogFunction;

            left = log.InnerTerm;
            pEvalData.WorkMgr.FromSides(left, PowOp.StaticWeakCombine(log.Base, right), "Convert from logarithm form to exponential form.");

            right = PowOp.StaticCombine(log.Base, right).ToAlgTerm();

            pEvalData.WorkMgr.FromSides(left, right);

            return p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
        }
    }
}