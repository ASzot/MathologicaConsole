using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Solving.Diff_Eqs
{
    internal class ExactEqsSolve : DiffSolve
    {
        public ExactEqsSolve()
        {
        }

        public override ExComp[] Solve(AlgebraTerm left, AlgebraTerm right, AlgebraComp funcVar, AlgebraComp dVar,
            ref TermType.EvalData pEvalData)
        {
            // In the form N(x,y)(dy/dx)+M(x,y)=0

            // As the order is one convert to a variable representation of the derivatives.
            AlgebraComp derivVar = null;
            left = ConvertDerivsToAlgebraComps(left, funcVar, dVar, ref derivVar);
            right = ConvertDerivsToAlgebraComps(right, funcVar, dVar, ref derivVar);

            // Move everything to the left.
            if (left.IsZero())
            {
                left = SubOp.StaticCombine(left, right).ToAlgTerm();
                right = ExNumber.GetZero().ToAlgTerm();
                pEvalData.GetWorkMgr().FromSides(left, right, "Move everything to the left hand side.");
            }

            // Find N(x,y)
            List<AlgebraGroup> varGps = left.GetGroupsVariableToNoOps(derivVar);
            if (varGps.Count == 0)
                return null;
            AlgebraTerm funcN = AlgebraGroup.GetConstantTo(varGps, derivVar);
            if (!funcN.Contains(dVar) || !funcN.Contains(funcVar))
                return null;

            // Find M(x,y)
            List<AlgebraGroup> constGps = left.GetGroupsConstantTo(derivVar);
            if (constGps.Count == 0)
                return null;
            AlgebraTerm funcM = AlgebraGroup.ToTerm(constGps);
            if (!funcN.Contains(dVar) || !funcM.Contains(funcVar))
                return null;

            pEvalData.GetWorkMgr().FromSides(left, right, "The equation is in the form " + WorkMgr.STM +
                "N(x,y)\\frac{dy}{dx}+M(x,y)" + WorkMgr.EDM + " where " + WorkMgr.STM + "N(" + dVar.ToDispString() + "," + funcVar.ToDispString() + ")=" + WorkMgr.ToDisp(funcN) + WorkMgr.EDM + " and " +
                WorkMgr.STM + "M(" + dVar.ToDispString() + "," + funcVar.ToDispString() + ")=" + WorkMgr.ToDisp(funcM) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromSides(left, right, "Use exact equations only if " + WorkMgr.STM + "\\frac{\\partial M}{\\partial " + funcVar.ToDispString() + "}=\\frac{\\partial N}{\\partial " +
                dVar.ToDispString() + "}" + WorkMgr.EDM);

            // Does M_y = N_x ?
            pEvalData.GetWorkMgr().FromFormatted("");
            WorkStep last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp my = Derivative.TakeDeriv(funcM.CloneEx(), funcVar, ref pEvalData, true, false);
            last.GoUp(ref pEvalData);

            last.SetWorkHtml(WorkMgr.STM + "\\frac{\\partial M}{\\partial " + funcVar.ToDispString() + "}=" + WorkMgr.ToDisp(my) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp nx = Derivative.TakeDeriv(funcN.CloneEx(), dVar, ref pEvalData, true, false);
            last.GoUp(ref pEvalData);

            last.SetWorkHtml(WorkMgr.STM + "\\frac{\\partial N}{\\partial " + dVar.ToDispString() + "}=" + WorkMgr.ToDisp(nx) + WorkMgr.EDM);

            if (my is AlgebraTerm)
                my = (my as AlgebraTerm).RemoveRedundancies(false);
            if (nx is AlgebraTerm)
                nx = (nx as AlgebraTerm).RemoveRedundancies(false);

            if (!my.IsEqualTo(nx))
                return null;

            pEvalData.GetWorkMgr().FromFormatted("");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);

            int startWorkStepCount = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            string startWorkHtml = "\\psi = \\int (" + WorkMgr.ToDisp(funcM) + " )d" + dVar.ToDispString();
            ExComp psi = Integral.TakeAntiDeriv(funcM, dVar, ref pEvalData);
            last.SetWorkDesc("If " + WorkMgr.STM + "\\psi_" + dVar.ToDispString() + "=M" + WorkMgr.EDM + " then " + WorkMgr.STM + "\\int M d" + dVar.ToDispString() + " = \\psi" + WorkMgr.EDM);
            bool constX = false;
            if (psi is Integral)
            {
                pEvalData.GetWorkMgr().PopSteps(startWorkStepCount);
                // The integration failed.
                // Maybe integrating the other function will work.
                startWorkHtml = "\\psi = \\int (" + WorkMgr.ToDisp(funcN) + " )d" + funcVar.ToDispString();
                psi = Integral.TakeAntiDeriv(funcN, funcVar, ref pEvalData);
                last.SetWorkDesc("If " + WorkMgr.STM + "\\psi_" + funcVar.ToDispString() + "=N" + WorkMgr.EDM + " then " + WorkMgr.STM + "\\int N d" + funcVar.ToDispString() + " = \\psi" + WorkMgr.EDM);
                constX = true;
            }

            last.GoUp(ref pEvalData);

            if (psi is Integral)
                return null;

            startWorkHtml += " = " + WorkMgr.ToDisp(psi) + "+" + (constX ? " h(x)" : "h(y)");

            last.SetWorkHtml(WorkMgr.STM + startWorkHtml + WorkMgr.EDM);

            // Now solve for the constant function.
            AlgebraComp solveForConstVar = constX ? dVar : funcVar;

            pEvalData.GetWorkMgr().FromFormatted("");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp tmpPsi = Derivative.TakeDeriv(psi, solveForConstVar, ref pEvalData, true, false);
            last.GoUp(ref pEvalData);

            last.SetWorkHtml(WorkMgr.STM + "\\psi_" + funcVar.ToDispString() + " = " + WorkMgr.ToDisp(tmpPsi) + "+" + (constX ? "h'(x)" : "h'(y)") + WorkMgr.EDM);

            // Find the difference.
            ExComp derivConstFunc = SubOp.StaticCombine(constX ? funcM : funcN, tmpPsi);
            // Negate because we subtracted.
            derivConstFunc = MulOp.Negate(derivConstFunc);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + (constX ? "h'(x)" : "h'(y)") + " = " + WorkMgr.ToDisp(derivConstFunc) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("");
            last = pEvalData.GetWorkMgr().GetLast();

            last.GoDown(ref pEvalData);
            ExComp constFunc = Integral.TakeAntiDeriv(derivConstFunc, solveForConstVar, ref pEvalData);
            last.GoUp(ref pEvalData);
            if (constFunc is Integral)
                return null;

            last.SetWorkHtml(WorkMgr.STM + "\\int " + (constX ? "h'(x)" : "h'(y)") + " d" + (constX ? "x" : "y") + " = " + WorkMgr.ToDisp(constFunc) + WorkMgr.EDM);

            psi = AddOp.StaticCombine(psi, constFunc);

            pEvalData.GetWorkMgr().FromSides(new AlgebraComp("psi"), psi);

            ExComp[] sols = new ExComp[] { psi, ExNumber.GetZero() };
            pEvalData.GetWorkMgr().FromSides(sols[0], sols[1]);

            return sols;
        }
    }
}