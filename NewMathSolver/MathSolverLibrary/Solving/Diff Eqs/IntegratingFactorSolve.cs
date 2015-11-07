using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Solving.Diff_Eqs
{
    internal class IntegratingFactorSolve : DiffSolve
    {
        public IntegratingFactorSolve()
        {
        }

        public override Equation.ExComp[] Solve(Equation.AlgebraTerm left, Equation.AlgebraTerm right, Equation.AlgebraComp funcVar, Equation.AlgebraComp dVar, ref TermType.EvalData pEvalData)
        {
            // In the form dy/dx + N(x)y = M(x)

            // As the order is one convert to a variable representation of the derivatives.
            AlgebraComp derivVar = null;
            left = SeperableSolve.ConvertDerivsToAlgebraComps(left, funcVar, dVar, ref derivVar);
            right = SeperableSolve.ConvertDerivsToAlgebraComps(right, funcVar, dVar, ref derivVar);

            // Move the dy/dx to the left.
            SolveMethod.VariablesToLeft(ref left, ref right, derivVar, ref pEvalData);

            // Move N(x)y to the left.
            SolveMethod.VariablesToLeft(ref left, ref right, funcVar, ref pEvalData);

            // Move M(x) to the right.
            SolveMethod.ConstantsToRight(ref left, ref right, new AlgebraComp[] { funcVar, derivVar }, ref pEvalData);

            List<AlgebraGroup> gps = left.GetGroupsVariableToNoOps(derivVar);
            AlgebraTerm constantTo = AlgebraGroup.GetConstantTo(gps, derivVar);

            if (!constantTo.IsOne())
            {
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\frac{" + WorkMgr.ToDisp(left) + "}{" + WorkMgr.ToDisp(constantTo) +
                    "}=\\frac{" + WorkMgr.ToDisp(right) + "}{" + WorkMgr.ToDisp(constantTo) + "}" + WorkMgr.EDM);

                // Divide each group by the constant.
                left = DivOp.GroupDivide(left, constantTo);
                right = DivOp.GroupDivide(right, constantTo);

                pEvalData.GetWorkMgr().FromSides(left, right);
            }

            pEvalData.GetWorkMgr().FromSides(left, right, "The equation is in the form " + WorkMgr.STM +
                "\\frac{d" + funcVar.ToDispString() + "}{d" + dVar.ToDispString() + "}+p(" + dVar.ToDispString() + ")" + funcVar.ToDispString() + "=g(" + dVar.ToDispString() + ")" + WorkMgr.EDM);

            List<AlgebraGroup> ags = left.GetGroupsVariableToNoOps(funcVar);
            AlgebraTerm fullNFunc = AlgebraGroup.ToTerm(ags);
            AlgebraTerm nx = AlgebraGroup.GetConstantTo(ags, funcVar);

            ExComp divCheck = DivOp.StaticCombine(fullNFunc, nx);
            if (divCheck is AlgebraTerm)
                divCheck = (divCheck as AlgebraTerm).RemoveRedundancies(false);

            if (!divCheck.IsEqualTo(funcVar))
                return null;

            string nxStr = WorkMgr.ToDisp(nx);

            if (nx.Contains(funcVar))
                return null;

            pEvalData.GetWorkMgr().FromFormatted("",
                "Find the integrating factor.");
            WorkStep lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp ix = Integral.TakeAntiDeriv(nx, dVar, ref pEvalData);

            ix = new PowerFunction(Constant.GetE(), ix);
            ix = new AlgebraTerm(ix);
            (ix as AlgebraTerm).EvaluateFunctions(false, ref pEvalData);
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "I(" + dVar.ToDispString() + ")=e^{\\int " + WorkMgr.ToDisp(nx) + "d" + dVar.ToDispString() + "}=" + WorkMgr.ToDisp(ix) + WorkMgr.EDM);

            string ixStr = WorkMgr.ToDisp(ix);
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "(" + ixStr + ")" + "\\frac{d" + funcVar.ToDispString() + "}{d" + dVar.ToDispString() + "}+(" + ixStr + ")(" + nxStr + ")" +
                funcVar.ToDispString() + "=(" + ixStr + ")" + WorkMgr.ToDisp(right) + WorkMgr.EDM, "Multiply everything by the integrating factor.");

            left = MulOp.StaticCombine(ix, funcVar).ToAlgTerm();
            right = MulOp.StaticCombine(ix, right).ToAlgTerm();

            string rightDispStr = WorkMgr.ToDisp(right);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\frac{d}{d" + dVar.ToDispString() + "}[" + WorkMgr.ToDisp(left) +
                "]= " + rightDispStr + WorkMgr.EDM,
                "Use the backwards product rule.");

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int \\frac{d}{d" + dVar.ToDispString() + "}[" + WorkMgr.ToDisp(left) + "]d" + dVar.ToDispString() +
                "= \\int (" + rightDispStr + ") d" + dVar.ToDispString() + WorkMgr.EDM,
                "Take the antiderivative of both sides.");

            pEvalData.GetWorkMgr().FromFormatted("");
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\int \\frac{d}{d" + dVar.ToDispString() + "}[" + WorkMgr.ToDisp(left) + "] d" + dVar.ToDispString() + WorkMgr.EDM,
                "The integral and the derivative cancel on the left hand side.");

            right = Integral.TakeAntiDeriv(right, dVar, ref pEvalData).ToAlgTerm();
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + WorkMgr.ToDisp(left) + " = " + WorkMgr.ToDisp(right) + WorkMgr.EDM);

            return new ExComp[] { left, right };
        }
    }
}