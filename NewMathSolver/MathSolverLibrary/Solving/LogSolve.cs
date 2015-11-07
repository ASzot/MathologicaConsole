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

                if (pEvalData.GetNegDivCount() != -1)
                    pEvalData.SetNegDivCount(pEvalData.GetNegDivCount() + 1);
            }

            ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);

            if (!left.Contains(solveForComp))
            {
                if (Simplifier.AreEqual(left, right, ref pEvalData))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            pEvalData.AttemptSetInputType(TermType.InputType.LogSolve);

            bool leftLogsCombined = false;
            left = AdvAlgebraTerm.CompoundLogs(left, out leftLogsCombined, solveForComp);
            bool rightLogsCombined = false;
            right = AdvAlgebraTerm.CompoundLogs(right, out rightLogsCombined, solveForComp);

            if (leftLogsCombined || rightLogsCombined)
                pEvalData.GetWorkMgr().FromSides(left, right, "Combine logarithms.");

            System.Collections.Generic.List<ExComp[]> leftGroups = left.GetGroups();
            System.Collections.Generic.List<ExComp[]> rightGroups = right.GetGroups();
            if (leftGroups.Count == 1 && rightGroups.Count == 1 && left.Contains(solveForComp) && right.Contains(solveForComp))
            {
                left = AdvAlgebraTerm.ForceLogCoeffToPow(left, null);
                right = AdvAlgebraTerm.ForceLogCoeffToPow(right, null);
                ExComp log0Ex = left.RemoveRedundancies(false);
                ExComp log1Ex = right.RemoveRedundancies(false);

                if (log0Ex is LogFunction && log1Ex is LogFunction)
                {
                    LogFunction log0 = log0Ex as LogFunction;
                    LogFunction log1 = log1Ex as LogFunction;

                    if (log0.GetBase().IsEqualTo(log1.GetBase()))
                    {
                        // Using the property log(x)=log(y) -> x = y
                        left = log0.GetInnerEx().ToAlgTerm();
                        right = log1.GetInnerEx().ToAlgTerm();
                        pEvalData.GetWorkMgr().FromSides(left, right, "By the rule if " + WorkMgr.STM + "log(x)=log(y)" + WorkMgr.EDM + " then " + WorkMgr.STM + "x=y" + WorkMgr.EDM);
                        ExComp agSolveResult = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                        return agSolveResult;
                    }

                    if (log0.GetBase() is ExNumber && log1.GetBase() is ExNumber)
                    {
                        ExComp log0Inner = log0.GetInnerEx();
                        ExComp log1Inner = log1.GetInnerEx();

                        ExNumber nLog0Base = log0.GetBase() as ExNumber;
                        ExNumber nLog1Base = log1.GetBase() as ExNumber;

                        if (ExNumber.OpEqual(ExNumber.OpMod(nLog0Base, nLog1Base), 0))
                        {
                            AlgebraComp tmpSolveFor = p_agSolver.NextSubVar();
                            AlgebraComp tmpSolveFor0 = new AlgebraComp(tmpSolveFor.GetVar().GetVar() + "_0");
                            AlgebraComp tmpSolveFor1 = new AlgebraComp(tmpSolveFor.GetVar().GetVar() + "_1");

                            string workStr = WorkStep.FormatStr("Given this it can be said " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor0), log0Inner);
                            workStr += WorkStep.FormatStr(" and " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor1), log1Inner);
                            workStr += " make the bases equal so the right hand sides can be set equal to each other.";

                            pEvalData.GetWorkMgr().FromSides(left, right, workStr);

                            ExponentSolve expSolve = new ExponentSolve(p_agSolver);

                            AlgebraTerm tmpLeft = PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor).ToAlgTerm();
                            AlgebraTerm tmpRight = nLog0Base.ToAlgTerm();

                            pEvalData.GetWorkMgr().SetAllowWork(false);
                            ExComp raiseTo = expSolve.SolveEquation(tmpLeft, tmpRight, tmpSolveFor.GetVar(), ref pEvalData);
                            pEvalData.GetWorkMgr().SetAllowWork(true);

                            ExComp tmpRaiseTo = MulOp.StaticCombine(raiseTo, tmpSolveFor0);

                            log1Inner = PowOp.StaticCombine(log1Inner, raiseTo);

                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Make the bases equal.", PowOp.StaticWeakCombine(nLog1Base, tmpRaiseTo), log1Inner);

                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM + "<br />" + WorkMgr.STM + "{0}={2}" + WorkMgr.EDM, "The left hand sides are now equal. So " +
                                WorkMgr.STM + "{0}={1}={2}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor0), log0Inner, log1Inner);
                        }
                        else if (ExNumber.OpEqual(ExNumber.OpMod(nLog1Base, nLog0Base), 0))
                        {
                            AlgebraComp tmpSolveFor = p_agSolver.NextSubVar();
                            AlgebraComp tmpSolveFor0 = new AlgebraComp(tmpSolveFor.GetVar().GetVar() + "_0");
                            AlgebraComp tmpSolveFor1 = new AlgebraComp(tmpSolveFor.GetVar().GetVar() + "_1");

                            string workStr = WorkStep.FormatStr("Given this it can be said " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor1), log1Inner);
                            workStr += WorkStep.FormatStr(" and " + WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor0), log0Inner);
                            workStr += " make the bases equal so the right hand sides can be set equal to each other.";

                            pEvalData.GetWorkMgr().FromSides(left, right, workStr);

                            ExponentSolve expSolve = new ExponentSolve(p_agSolver);

                            AlgebraTerm tmpLeft = PowOp.StaticWeakCombine(nLog0Base, tmpSolveFor).ToAlgTerm();
                            AlgebraTerm tmpRight = nLog1Base.ToAlgTerm();

                            pEvalData.GetWorkMgr().SetAllowWork(false);
                            ExComp raiseTo = expSolve.SolveEquation(tmpLeft, tmpRight, tmpSolveFor.GetVar(), ref pEvalData);
                            pEvalData.GetWorkMgr().SetAllowWork(true);

                            ExComp tmpRaiseTo = MulOp.StaticCombine(raiseTo, tmpSolveFor1);

                            log0Inner = PowOp.StaticCombine(log0Inner, raiseTo);

                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Make the bases equal.", PowOp.StaticWeakCombine(nLog0Base, tmpRaiseTo), log0Inner);

                            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM + "<br />" + WorkMgr.STM + "{0}={2}" + WorkMgr.EDM,
                                "The left hand sides are now equal. So " + WorkMgr.STM + "{0}={1}={2}" + WorkMgr.EDM, PowOp.StaticWeakCombine(nLog1Base, tmpSolveFor1), log1Inner, log0Inner);
                        }
                        else
                        {
                            pEvalData.AddFailureMsg("Can't convert to common base.");
                            return null;
                        }

                        pEvalData.GetWorkMgr().FromSides(log0Inner, log1Inner, "Solve this equation.");
                        ExComp agSolveResult = p_agSolver.SolveEq(solveFor, log0Inner.ToAlgTerm(), log1Inner.ToAlgTerm(), ref pEvalData);
                        return agSolveResult;
                    }
                }
            }

            VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            if (!left.Contains(solveForComp))
            {
                return new NoSolutions();
            }
            CombineFractions(ref left, ref right, ref pEvalData);

            left = AdvAlgebraTerm.CompoundLogs(left, solveForComp);
            right = AdvAlgebraTerm.CompoundLogs(right, solveForComp);

            leftGroups = left.GetGroups();

            if (leftGroups.Count != 1)
            {
                pEvalData.AddFailureMsg("Couldn't combine variable logarithms to singular term.");
                return null;
            }
            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData, false);

            ExComp leftEx = left.RemoveRedundancies(false);
            if (!(leftEx is LogFunction))
                return null;

            LogFunction log = leftEx as LogFunction;

            left = log.GetInnerTerm();
            pEvalData.GetWorkMgr().FromSides(left, PowOp.StaticWeakCombine(log.GetBase(), right), "Convert from logarithm form to exponential form.");

            right = PowOp.StaticCombine(log.GetBase(), right).ToAlgTerm();

            pEvalData.GetWorkMgr().FromSides(left, right);

            ExComp finalAgSolveResult = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            return finalAgSolveResult;
        }
    }
}