using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Solving
{
    internal class ExponentSolve : SolveMethod
    {
        private AlgebraSolver p_agSolver;

        public ExponentSolve(AlgebraSolver pAgSolver)
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

            ExComp leftEx;
            ExComp rightEx;

            pEvalData.AttemptSetInputType(TermType.InputType.ExponentSolve);

            List<ExComp[]> leftGroups = left.GetGroups();
            List<ExComp[]> rightGroups = right.GetGroups();
            if (leftGroups.Count == 1 && rightGroups.Count == 1 && left.Contains(solveForComp) && right.Contains(solveForComp))
            {
                ExComp gcf = DivOp.GetCommonFactor(left, right);

                // Took out because logs are used to solve some of these equations so there is no base converting.
                //WorkMgr.FromSides(left, right, "Both sides must be converted to a like base.");

                if (gcf != null && !ExNumber.GetOne().IsEqualTo(gcf) && !ExNumber.GetZero().IsEqualTo(gcf))
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})/({1})=({2})/({1})" + WorkMgr.EDM, "Divide by the greatest common factor.", left, gcf, right);

                    leftEx = DivOp.StaticCombine(left, gcf).ToAlgTerm();
                    rightEx = DivOp.StaticCombine(right, gcf).ToAlgTerm();

                    pEvalData.GetWorkMgr().FromSides(left, right, "Simplify");
                }
                else
                {
                    leftEx = left;
                    rightEx = right;
                }

                if (leftEx is AlgebraTerm)
                    leftEx = (leftEx as AlgebraTerm).RemoveRedundancies(false);
                if (rightEx is AlgebraTerm)
                    rightEx = (rightEx as AlgebraTerm).RemoveRedundancies(false);

                if (leftEx is PowerFunction && rightEx is PowerFunction)
                {
                    PowerFunction pfLeft = leftEx as PowerFunction;
                    PowerFunction pfRight = rightEx as PowerFunction;
                    if (pfLeft.HasVariablePowers(solveForComp) && pfRight.HasVariablePowers(solveForComp))
                    {
                        ExComp dualPowResult = DualVarPowSolve(pfLeft, pfRight, solveFor, ref pEvalData);
                        return dualPowResult;
                    }
                }
            }

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
            leftGroups = left.GetGroupsNoOps();
            if (leftGroups.Count != 1)
            {
                if (leftGroups.Count == 2)
                {
                    ExComp[] gp0 = leftGroups[0];
                    ExComp[] gp1 = leftGroups[1];

                    ExComp[] gpConst0 = GroupHelper.GetUnrelatableTermsOfGroup(gp0, solveForComp);
                    ExComp[] gpConst1 = GroupHelper.GetUnrelatableTermsOfGroup(gp1, solveForComp);

                    ExComp exConst0 = GroupHelper.ToAlgTerm(gpConst0).RemoveRedundancies(false);
                    ExComp exConst1 = GroupHelper.ToAlgTerm(gpConst1).RemoveRedundancies(false);

                    ExComp negExConst0 = MulOp.Negate(exConst0);

                    if (negExConst0.IsEqualTo(exConst1))
                    {
                        ExComp exVar0 = GroupHelper.ToAlgTerm(GroupHelper.RemoveExTerms(gp0, gpConst0)).RemoveRedundancies(false);
                        ExComp exVar1 = GroupHelper.ToAlgTerm(GroupHelper.RemoveExTerms(gp1, gpConst1)).RemoveRedundancies(false);

                        if (exVar0 is PowerFunction && exVar1 is PowerFunction)
                        {
                            PowerFunction pfGp0 = exVar0 as PowerFunction;
                            PowerFunction pfGp1 = exVar1 as PowerFunction;

                            if (pfGp0.HasVariablePowers(solveForComp) && pfGp1.HasVariablePowers(solveForComp))
                            {
                                ExComp dualPowSolve = DualVarPowSolve(pfGp0, pfGp1, solveFor, ref pEvalData);
                                return dualPowSolve;
                            }
                        }
                    }
                }

                pEvalData.AddFailureMsg("Too many exponents are present");
                return null;
            }
            DivideByVariableCoeffs(ref left, ref right, solveForComp, ref pEvalData, false);

            // We should now have an isolated exponent term. (base)^(term containing our solve variable).
            leftEx = left.RemoveRedundancies(false);
            if (!(leftEx is PowerFunction))
                return null;

            PowerFunction powFunc = leftEx as PowerFunction;

            rightEx = right.RemoveRedundancies(false);
            if (powFunc.GetBase() is ExNumber && rightEx is ExNumber)
            {
                // We might be able to find a common base.
                ExNumber nLeftPow;
                ExNumber nRightPow;
                ExNumber nBase;
                ExNumber.GCF_Base(powFunc.GetBase() as ExNumber, rightEx as ExNumber, out nLeftPow, out nRightPow, out nBase);

                if (nLeftPow != null && nRightPow != null && nBase != null)
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})^({1}*({2}))=({0})^({3})" + WorkMgr.EDM, "Convert both sides to have equal bases.", nBase, nLeftPow, powFunc.GetPower(), nRightPow);
                    // We found a like base between our left and right sides.
                    ExComp leftPow = MulOp.StaticCombine(nLeftPow, powFunc.GetPower());

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})^({1})=({0})^({2})" + WorkMgr.EDM, "The powers can be set equal because they have equal bases. This is due to " + WorkMgr.STM +
                        "a^x=a^y" + WorkMgr.EDM + " then " + WorkMgr.STM + "x=y" + WorkMgr.EDM, nBase, leftPow, nRightPow);
                    pEvalData.GetWorkMgr().FromSides(leftPow, nRightPow);
                    ExComp solveResult = p_agSolver.SolveEq(solveFor, leftPow.ToAlgTerm(), nRightPow.ToAlgTerm(), ref pEvalData);
                    return solveResult;
                }
            }

            ExComp baseEx = powFunc.GetBase();
            ExComp pow = powFunc.GetPower();

            LogFunction logRight = LogFunction.Ln(right);
            LogFunction logLeft = LogFunction.Ln(baseEx);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "ln({0})=ln({1})" + WorkMgr.EDM, "Take the natural log of both sides.", left, right);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Using the logarithm power property convert the power to a coefficient. This comes from the definition " + WorkMgr.STM +
                "log(x^a)=alog(x)" + WorkMgr.EDM, MulOp.StaticWeakCombine(logLeft, pow), logRight);

            // If we expand the logs we have a chance of evaluating them.
            // Example: log(1/25) -> log(1) - log(25) -> 0 - log(25) -> -log(25)
            AlgebraTerm expandedRightLog = AdvAlgebraTerm.ExpandLogs(logRight);
            AlgebraTerm expandedLeftLog = AdvAlgebraTerm.ExpandLogs(logLeft);

            expandedRightLog.EvaluateFunctions(FunctionType.Logarithm, false, ref pEvalData);
            expandedLeftLog.EvaluateFunctions(FunctionType.Logarithm, false, ref pEvalData);

            expandedRightLog = expandedRightLog.ApplyOrderOfOperations();
            ExComp rightLogEx = expandedRightLog.MakeWorkable();

            expandedLeftLog = expandedLeftLog.ApplyOrderOfOperations();
            ExComp leftLogEx = expandedLeftLog.MakeWorkable();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Simplify", MulOp.StaticWeakCombine(leftLogEx, pow), rightLogEx);
            pEvalData.GetWorkMgr().FromDivision(leftLogEx.ToAlgTerm(), MulOp.StaticWeakCombine(leftLogEx, pow), rightLogEx);

            left = pow.ToAlgTerm();
            right = DivOp.StaticCombine(rightLogEx, leftLogEx).ToAlgTerm();

            pEvalData.GetWorkMgr().FromSides(left, right, "Simplify");

            ExComp agSolved = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            return agSolved;
        }

        private ExComp DualVarPowSolve(PowerFunction pfLeft, PowerFunction pfRight, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm left, right;

            ExComp leftBaseEx = pfLeft.GetBase();
            ExComp rightBaseEx = pfRight.GetBase();
            if (leftBaseEx.IsEqualTo(rightBaseEx))
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}={1}" + WorkMgr.EDM, "Since the bases are equal set the powers equal to each other. " + WorkMgr.STM + "{2}={3}" + WorkMgr.EDM,
                    pfLeft, pfRight, pfLeft.GetPower(), pfRight.GetPower());
                ExComp agSolved = p_agSolver.SolveEq(solveFor, pfLeft.GetPower().ToAlgTerm(), pfRight.ToAlgTerm(), ref pEvalData);
                return agSolved;
            }

            if (leftBaseEx is ExNumber && rightBaseEx is ExNumber && !(leftBaseEx as ExNumber).HasImaginaryComp() &&
                !(rightBaseEx as ExNumber).HasImaginaryComp())
            {
                ExNumber nLeftBase = leftBaseEx as ExNumber;
                ExNumber nRightBase = rightBaseEx as ExNumber;
                ExNumber nLeftPow, nRightPow, nBase;

                ExNumber.GCF_Base(nLeftBase, nRightBase, out nLeftPow, out nRightPow, out nBase);

                if (nLeftPow != null && nRightPow != null && nBase != null)
                {
                    string coeff0Str = ExNumber.OpNotEquals(nLeftPow, 1.0) ? "{1}*" : "";
                    string coeff1Str = ExNumber.OpNotEquals(nRightPow, 1.0) ? "{3}*" : "";

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})^(" + coeff0Str + "({2}))=({0})^(" + coeff1Str + "({4}))" + WorkMgr.EDM, "Convert both sides to have like bases.",
                        nBase, nLeftPow, pfLeft.GetPower(), nRightPow, pfRight.GetPower());
                    // nBase will be used in displaying work.
                    left = MulOp.StaticCombine(nLeftPow, pfLeft.GetPower()).ToAlgTerm();
                    right = MulOp.StaticCombine(nRightPow, pfRight.GetPower()).ToAlgTerm();

                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "({0})^({1})=({0})^({2})" + WorkMgr.EDM, "With like bases the powers can be set equal to each other. This is due to " +
                        WorkMgr.STM + "a^x=a^y" + WorkMgr.EDM + " then " + WorkMgr.STM + "x=y" + WorkMgr.EDM, nBase, left, right);

                    ExComp agSolved = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
                    return agSolved;
                }
            }

            LogFunction leftLog = new LogFunction(pfLeft.GetBase());
            LogFunction rightLog = new LogFunction(pfRight.GetBase());
            ExComp temp = Constant.ParseConstant("e");
            rightLog.SetBase(temp);
            leftLog.SetBase(temp);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "ln({0})=ln({1})" + WorkMgr.EDM, "Take the natural log of both sides.", pfLeft, pfRight);

            ExComp baseLeftEx = leftLog.Evaluate(false, ref pEvalData);
            ExComp baseRightEx = rightLog.Evaluate(false, ref pEvalData);

            if (baseLeftEx is LogFunction)
                baseLeftEx = AdvAlgebraTerm.ForceLogPowToCoeff((baseLeftEx as LogFunction));
            if (baseRightEx is LogFunction)
                baseRightEx = AdvAlgebraTerm.ForceLogPowToCoeff((baseRightEx as LogFunction));

            left = MulOp.StaticCombine(pfLeft.GetPower(), baseLeftEx).ToAlgTerm();
            right = MulOp.StaticCombine(pfRight.GetPower(), baseRightEx).ToAlgTerm();

            pEvalData.GetWorkMgr().FromSides(left, right, "Make any exponents in the log become coefficients.");

            ExComp finalAgSolved = p_agSolver.SolveEq(solveFor, left, right, ref pEvalData);
            return finalAgSolved;
        }
    }
}