﻿using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class TrigSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public TrigSolve(AlgebraSolver pAgSolver)
        {
            p_agSolver = pAgSolver;
        }

        public ExComp InverseTrigSolve(InverseTrigFunction inverseTrigFunc, ExComp right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            TrigFunction trigFunc;
            if (inverseTrigFunc is ASinFunction)
                trigFunc = new SinFunction(right);
            else if (inverseTrigFunc is ACosFunction)
                trigFunc = new CosFunction(right);
            else if (inverseTrigFunc is ATanFunction)
                trigFunc = new TanFunction(right);
            else if (inverseTrigFunc is ACscFunction)
                trigFunc = new CscFunction(right);
            else if (inverseTrigFunc is ASecFunction)
                trigFunc = new SecFunction(right);
            else if (inverseTrigFunc is ACotFunction)
                trigFunc = new CotFunction(right);
            else
            {
                pEvalData.AddFailureMsg("Solving error.");
                return null;
            }

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + trigFunc.GetFuncName() + "({0})={1}" + WorkMgr.EDM, "Take the " + trigFunc.GetFuncName() + " of both sides.", inverseTrigFunc, trigFunc);

            ExComp evaluated = trigFunc.Evaluate(false, ref pEvalData);
            ExComp innerEx = inverseTrigFunc.GetInnerEx();

            if (ExNumber.IsUndef(evaluated))
            {
                if (inverseTrigFunc is ATanFunction && AlgebraTerm.FromFraction(ExNumber.GetOne(), solveFor.ToAlgebraComp()).IsEqualTo(inverseTrigFunc.GetInnerEx()))
                    return ExNumber.GetZero();
                else
                {
                    pEvalData.GetWorkMgr().FromSides(innerEx, evaluated, "The " + trigFunc.GetFuncName() + " function is undefined at the angle " +
                        WorkMgr.STM + (right is AlgebraTerm ? (right as AlgebraTerm).FinalToDispStr() : right.ToAsciiString()) + WorkMgr.EDM);

                    return ExNumber.GetUndefined();
                }
            }

            pEvalData.GetWorkMgr().FromSides(innerEx, evaluated, "The trig function and its inverse cancel leaving just the inner term.");

            ExComp solveResult = p_agSolver.SolveEq(solveFor, inverseTrigFunc.GetInnerEx().ToAlgTerm(), evaluated.ToAlgTerm(), ref pEvalData);
            return solveResult;
        }

        public override ExComp SolveEquation(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
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

            pEvalData.AttemptSetInputType(TermType.InputType.TrigSolve);

            List<ExComp[]> groups = left.GetGroups();
            if (groups.Count != 1)
            {
                // We have multiple groups involving an undetermined amount of variable trig functions.
                ExComp multiGroupSolveResult = SolveEquationMultiGroup(left, right, solveFor, ref pEvalData);
                if (multiGroupSolveResult == null)
                {
                    pEvalData.AddFailureMsg("Couldn't solve trigonometric equation.");
                    return null;
                }

                return multiGroupSolveResult;
            }

            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData, false);

            ExComp leftEx = left.RemoveRedundancies(false);
            if (leftEx is AlgebraTerm)
                leftEx = (leftEx as AlgebraTerm).RemoveRedundancies(false);
            if (!(leftEx is TrigFunction))
            {
                if (leftEx is InverseTrigFunction)
                {
                    ExComp inverseTrigSolveResult = InverseTrigSolve(left as InverseTrigFunction, right, solveFor,
                        ref pEvalData);
                    return inverseTrigSolveResult;
                }

                // We have a singular group involving an undetermined amount of variable trig functions.
                return null;
            }

            TrigFunction appliedTrigFunc = (TrigFunction)leftEx;
            ExComp period = appliedTrigFunc.GetPeriod(solveForComp, pEvalData.GetUseRad());
            if (period == null)
            {
                pEvalData.AddFailureMsg("Function is not periodic.");
                return null;
            }

            ExComp[] rights = GetSolutionsForInverse(appliedTrigFunc, right, ref pEvalData);
            if (rights.Length == 1 && ExNumber.IsUndef(rights[0]))
                return rights[0];

            pEvalData.GetWorkMgr().FromFormatted("Period " + WorkMgr.STM + "={0}" + WorkMgr.EDM, "The period of " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " is " + WorkMgr.STM + "{0}" + WorkMgr.EDM, period, appliedTrigFunc);

            ExComp interval = MulOp.StaticCombine(period.CloneEx(), p_agSolver.IterationVar);

            //ExComp innerCoeff = appliedTrigFunc.InnerTerm.GetCoeffOfVar(solveForComp);

            //int[] subIntervalCount = new int[rights.Length];

            //for (int i = 0; i < rights.Length; ++i)
            //{
            //    ExComp ucAngleEx = rights[i];
            //    SimpleFraction ucAngle = new SimpleFraction();
            //    if (ucAngle.Init(ucAngleEx.ToAlgTerm()))
            //    {
            //        Number ucNum, ucDen;
            //        if (ucAngle.IsSimpleUnitCircleAngle(out ucNum, out ucDen))
            //        {
            //            ExComp finalDen = MulOp.StaticCombine(ucDen, innerCoeff);
            //            ExComp finalNum = new AlgebraTerm(ucNum, Constant.Pi);

            //            ExComp sub = MulOp.StaticCombine(finalDen, period);
            //            finalNum = SubOp.StaticCombine(finalNum, sub);
            //        }
            //    }

            //    subIntervalCount[i] = 0;
            //}

            left = appliedTrigFunc.GetInnerTerm();

            AlgebraTermArray rightArray = new AlgebraTermArray(ArrayFunc.ToList(rights));
            bool allSols;
            AlgebraTermArray solvedArray = rightArray.SimulSolve(left, solveFor, p_agSolver, ref pEvalData, out allSols, false);
            if (allSols)
                return new AllSolutions();
            if (solvedArray == null)
                return null;

            AlgebraTermArray finalSolved = new AlgebraTermArray();
            foreach (AlgebraTerm solvedTerm in solvedArray.GetTerms())
            {
                GeneralSolution generalSol = new GeneralSolution(solvedTerm, interval, p_agSolver.IterationVar);
                pEvalData.GetWorkMgr().FromSides(solveForComp, generalSol, "Add back the period to give the general solution for the trigonometric term.");
                finalSolved.Add(generalSol);
            }

            return finalSolved;
        }

        private static ExComp[] GetSolutionsForInverse(TrigFunction appliedTrigFunc, ExComp inverseOf, ref TermType.EvalData pEvalData)
        {
            BasicAppliedFunc inverseTrigFunc;
            ExComp evaluated;
            ExComp otherAngle;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\text{arc" + appliedTrigFunc.GetFuncName() + "}(" + appliedTrigFunc.FinalToDispStr() + ")=\\text{arc" + appliedTrigFunc.GetFuncName() + "}(" +
                WorkMgr.ToDisp(inverseOf) + ")" + WorkMgr.EDM, "Take the inverse " + WorkMgr.STM +
                appliedTrigFunc.GetFuncName() + WorkMgr.EDM + " of both sides.");

            if (appliedTrigFunc is SinFunction)
            {
                inverseTrigFunc = new ASinFunction(inverseOf);
                evaluated = inverseTrigFunc.Evaluate(false, ref pEvalData);
                if (ExNumber.IsUndef(evaluated))
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\text{Undefined}" + WorkMgr.EDM, WorkMgr.STM + "arcsin" + WorkMgr.EDM + " is undefined at " + WorkMgr.STM + "{0}" + WorkMgr.EDM, inverseOf);

                    ExComp[] singularSol = new ExComp[] { evaluated };
                    return singularSol;
                }

                otherAngle = SubOp.StaticCombine(TrigFunction.GetHalfRot(pEvalData.GetUseRad()), evaluated);
                if (otherAngle is AlgebraTerm)
                    otherAngle = (otherAngle as AlgebraTerm).CompoundFractions();
            }
            else if (appliedTrigFunc is CosFunction)
            {
                inverseTrigFunc = new ACosFunction(inverseOf);
                evaluated = inverseTrigFunc.Evaluate(false, ref pEvalData);

                if (ExNumber.IsUndef(evaluated))
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\text{Undefined}" + WorkMgr.EDM, WorkMgr.STM + "arccos" + WorkMgr.EDM + " is undefined at " + WorkMgr.STM + "{0}" + WorkMgr.EDM, inverseOf);

                    ExComp[] singularSol = new ExComp[] { evaluated };
                    return singularSol;
                }

                otherAngle = SubOp.StaticCombine(TrigFunction.GetFullRot(pEvalData.GetUseRad()), evaluated);
                if (otherAngle is AlgebraTerm)
                    otherAngle = (otherAngle as AlgebraTerm).CompoundFractions();
            }
            else if (appliedTrigFunc is TanFunction)
            {
                inverseTrigFunc = new ATanFunction(inverseOf);
                evaluated = inverseTrigFunc.Evaluate(false, ref pEvalData);

                ExComp[] singularSol = new ExComp[] { evaluated };
                return singularSol;
            }
            else if (appliedTrigFunc is CscFunction)
            {
                inverseTrigFunc = new ACscFunction(inverseOf);
                evaluated = inverseTrigFunc.Evaluate(false, ref pEvalData);

                if (ExNumber.IsUndef(evaluated))
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\text{Undefined}" + WorkMgr.EDM, WorkMgr.STM + "arccsc" + WorkMgr.EDM + " is undefined at " + WorkMgr.STM + "{0}" + WorkMgr.EDM, inverseOf);

                    ExComp[] singularSol = new ExComp[] { evaluated };
                    return singularSol;
                }

                otherAngle = SubOp.StaticCombine(TrigFunction.GetHalfRot(pEvalData.GetUseRad()), evaluated);
                if (otherAngle is AlgebraTerm)
                    otherAngle = (otherAngle as AlgebraTerm).CompoundFractions();
            }
            else if (appliedTrigFunc is SecFunction)
            {
                inverseTrigFunc = new ASecFunction(inverseOf);
                evaluated = inverseTrigFunc.Evaluate(false, ref pEvalData);

                if (ExNumber.IsUndef(evaluated))
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\text{Undefined}" + WorkMgr.EDM, WorkMgr.STM + "arcsec" + WorkMgr.EDM + " is undefined at " + WorkMgr.STM + "{0}" + WorkMgr.EDM, inverseOf);

                    ExComp[] singularSol = new ExComp[] { evaluated };
                    return singularSol;
                }

                otherAngle = SubOp.StaticCombine(TrigFunction.GetFullRot(pEvalData.GetUseRad()), evaluated);
                if (otherAngle is AlgebraTerm)
                    otherAngle = (otherAngle as AlgebraTerm).CompoundFractions();
            }
            else if (appliedTrigFunc is CotFunction)
            {
                inverseTrigFunc = new ACotFunction(inverseOf);
                evaluated = inverseTrigFunc.Evaluate(false, ref pEvalData);

                ExComp[] singularSol = new ExComp[] { evaluated };
                return singularSol;
            }
            else
            {
                throw new ArgumentException();
            }

            SimpleFraction simpFrac = new SimpleFraction();
            if (pEvalData.GetUseRad() && simpFrac.Init(otherAngle.ToAlgTerm()))
            {
                ExNumber nNum, nDen;
                if (simpFrac.IsSimpleUnitCircleAngle(out nNum, out nDen, true))
                {
                    ExComp numEx = MulOp.StaticCombine(nNum, Constant.GetPi());
                    otherAngle = AlgebraTerm.FromFraction(numEx, nDen);
                }
            }

            if (otherAngle is AlgebraTerm)
                otherAngle = (otherAngle as AlgebraTerm).RemoveRedundancies(false);
            if (evaluated is AlgebraTerm)
                evaluated = (evaluated as AlgebraTerm).RemoveRedundancies(false);

            if (otherAngle.IsEqualTo(evaluated) || TrigFunction.GetFullRot(pEvalData.GetUseRad()).IsEqualTo(otherAngle))
            {
                ExComp[] singularSol = new ExComp[] { evaluated };

                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "The " + appliedTrigFunc.GetFuncName() + " function is equal to the value " + WorkMgr.STM + "{2}" + WorkMgr.EDM +
                    " at the angles " + WorkMgr.STM + "{1}" + WorkMgr.EDM, appliedTrigFunc.GetInnerEx(),
                    evaluated, inverseOf);

                return singularSol;
            }

            ExComp sol0 = evaluated;
            ExComp sol1 = otherAngle;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "arc" + appliedTrigFunc.GetFuncName() + "({0})={1},{2}" + WorkMgr.EDM, "The " + appliedTrigFunc.GetFuncName() +
                " function is equal to the value " + WorkMgr.STM + "{3}" + WorkMgr.EDM + " at the angles " + WorkMgr.STM + "{1}" + WorkMgr.EDM + " and " + WorkMgr.STM + "{2}" + WorkMgr.EDM, appliedTrigFunc,
                evaluated, otherAngle, inverseOf);

            ExComp[] sols = new ExComp[] { sol0, sol1 };

            return sols;
        }

        private static AlgebraTerm PythagreomSubIn(AlgebraTerm term, TrigFunction simplifyTo, out bool success, ref TermType.EvalData pEvalData)
        {
            PowerFunction subOut = null;
            ExComp subIn = null;
            if (simplifyTo is SinFunction)
            {
                subOut = new PowerFunction(new CosFunction(simplifyTo.GetInnerEx()), new ExNumber(2.0));
                subIn = new AlgebraTerm(ExNumber.GetOne(), new AddOp(), ExNumber.GetNegOne(), new MulOp(), new PowerFunction(simplifyTo, new ExNumber(2.0)));
            }
            else if (simplifyTo is CosFunction)
            {
                subOut = new PowerFunction(new SinFunction(simplifyTo.GetInnerEx()), new ExNumber(2.0));
                subIn = new AlgebraTerm(ExNumber.GetOne(), new AddOp(), ExNumber.GetNegOne(), new MulOp(), new PowerFunction(simplifyTo, new ExNumber(2.0)));
            }

            if (subOut != null && subIn != null)
            {
                success = false;
                AlgebraTerm subbedTerm = term.Substitute(subOut, subIn, ref success);
                if (success && pEvalData.GetWorkMgr().GetAllowWork())
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + subbedTerm.FinalToDispStr() + WorkMgr.EDM,
                        "Make the substitution " + WorkMgr.STM + subOut.FinalToDispStr() + "=" + WorkMgr.ToDisp(subIn) + WorkMgr.EDM +
                        " that comes from the Pythagorean trig identities.");
                }

                return subbedTerm;
            }

            success = false;
            return term;
        }

        private ExComp SolveEquationMultiGroup(AlgebraTerm left, AlgebraTerm right, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            AlgebraTerm overallTerm = right.IsZero() ? left : SubOp.StaticCombine(left, right).ToAlgTerm();
            List<ExComp[]> groups = overallTerm.GetGroupsNoOps();

            //////////////////////////////////////////////////////////
            // Just try to turn the expression into a pair of factors.
            ExComp[] gcf = overallTerm.GetGroupGCF();

            if (gcf != null)
            {
                AlgebraTerm gcfTerm = GroupHelper.ToAlgNoRedunTerm(gcf);
                if (!ExNumber.GetZero().IsEqualTo(gcfTerm) && !ExNumber.GetOne().IsEqualTo(gcfTerm) && gcfTerm.Contains(solveForComp))
                {
                    AlgebraTerm otherFactor = DivOp.StaticCombine(overallTerm, gcfTerm).ToAlgTerm();
                    if (otherFactor.Contains(solveForComp) && gcfTerm.Contains(solveForComp))
                    {
                        pEvalData.GetWorkMgr().FromSides(MulOp.StaticWeakCombine(gcfTerm, otherFactor), ExNumber.GetZero(), "Factor " +
                            WorkMgr.STM + gcfTerm.FinalToDispStr() + WorkMgr.EDM + " from the expression. Solve for each factor as it equals.");
                        FactorSolve factorSolve = new FactorSolve(p_agSolver);
                        ExComp factorSolveResult = factorSolve.SolveEquationFactors(solveFor, ref pEvalData, gcfTerm, otherFactor);
                        return factorSolveResult;
                    }
                }
            }

            overallTerm = overallTerm.RemoveRedundancies(false).ToAlgTerm();
            List<ExComp> trigFuncs = overallTerm.GetTrigFunctions();

            ///////////////////////////////////////////////////////
            // Try to divide to remove one of the trig functions.
            if (groups.Count == 2)
            {
                TypePair<int, ExComp>[] complexitiesArr = new TypePair<int, ExComp>[trigFuncs.Count];
                for (int i = 0; i < trigFuncs.Count; ++i)
                    complexitiesArr[i] = new TypePair<int, ExComp>(TrigFunction.GetTrigFuncComplexity(trigFuncs[i]), trigFuncs[i]);

                TrigFunction minTrigFunc = null;
                int minVal = int.MaxValue;
                foreach (TypePair<int, ExComp> complexity in complexitiesArr)
                {
                    if (complexity.GetData1() > 2)
                    {
                        minTrigFunc = null;
                        break;
                    }

                    if (minVal > complexity.GetData1())
                    {
                        minVal = complexity.GetData1();
                        if (!(complexity.GetData2() is TrigFunction))
                        {
                            minTrigFunc = null;
                            break;
                        }
                        minTrigFunc = complexity.GetData2() as TrigFunction;
                    }
                }

                if (minTrigFunc != null)
                {
                    AlgebraTerm gpTerm0 = GroupHelper.ToAlgTerm(groups[0]);
                    AlgebraTerm gpTerm1 = GroupHelper.ToAlgTerm(groups[1]);

                    if (pEvalData.GetWorkMgr().GetAllowWork())
                    {
                        string divisor = minTrigFunc.FinalToDispStr();
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "(" + gpTerm0.FinalToDispStr() + "+" + gpTerm1.FinalToDispStr() + ")/(" +
                            divisor + ")=0" + WorkMgr.EDM, "Divide by " + WorkMgr.STM + divisor + WorkMgr.EDM +
                            " to remove one of the trig functions.");
                    }

                    ExComp gp0 = DivOp.StaticCombine(gpTerm0, minTrigFunc);
                    ExComp gp1 = DivOp.StaticCombine(gpTerm1, minTrigFunc);

                    overallTerm = new AlgebraTerm(gp0, new AddOp(), gp1);

                    overallTerm = AdvAlgebraTerm.TrigSimplify(overallTerm, ref pEvalData);
                    overallTerm = overallTerm.ApplyOrderOfOperations();
                    overallTerm = overallTerm.MakeWorkable().ToAlgTerm();

                    pEvalData.GetWorkMgr().FromSides(overallTerm, ExNumber.GetZero(), "Simplify.");

                    ExComp agSolveResult = p_agSolver.SolveEq(solveFor, overallTerm.ToAlgTerm(), ExNumber.GetZero().ToAlgTerm(), ref pEvalData);
                    return agSolveResult;
                }
            }

            //////////////////////////////////////////////////////////////////////////////////////////////
            // Is there a simple substitution we can make to have all the terms be the same trig function.
            TrigFunction simplifyTo = AlgebraTerm.TrigToSimplifyTo(trigFuncs);

            if (simplifyTo != null)
            {
                bool success;
                overallTerm = PythagreomSubIn(overallTerm, simplifyTo, out success, ref pEvalData);
                if (success)
                {
                    overallTerm = overallTerm.ApplyOrderOfOperations();
                    ExComp overallEx = overallTerm.MakeWorkable();
                    if (pEvalData.GetWorkMgr().GetAllowWork())
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + overallTerm.FinalToDispStr() + "=0" + WorkMgr.EDM, "Simplify.");
                    return p_agSolver.SolveEq(solveFor, overallTerm.ToAlgTerm(), ExNumber.GetZero().ToAlgTerm(), ref pEvalData);
                }
            }

            return null;
        }
    }
}