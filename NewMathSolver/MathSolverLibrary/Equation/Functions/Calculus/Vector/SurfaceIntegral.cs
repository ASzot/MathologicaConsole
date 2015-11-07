using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Information_Helpers;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    internal class SurfaceIntegral : Integral
    {
        private AlgebraComp _surface;

        public SurfaceIntegral(ExComp innerEx)
            : base(innerEx)
        {
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (!(ex is SurfaceIntegral))
                return false;
            SurfaceIntegral other = ex as SurfaceIntegral;

            return other._surface.IsEqualTo(this._surface) && base.IsEqualTo(ex);
        }

        public override ExComp CloneEx()
        {
            return ConstructSurfaceIntegral(GetInnerTerm().CloneEx(), _surface == null ? null : (AlgebraComp)_surface.CloneEx(), _dVar == null ? null : (AlgebraComp)_dVar.CloneEx());
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return ConstructSurfaceIntegral(GetInnerTerm(), _surface, _dVar);
        }

        public static SurfaceIntegral ConstructSurfaceIntegral(ExComp innerEx, AlgebraComp surface, AlgebraComp surfaceDiff)
        {
            SurfaceIntegral surfaceIntegral = new SurfaceIntegral(innerEx);
            surfaceIntegral._surface = surface;
            surfaceIntegral._dVar = surfaceDiff;

            return surfaceIntegral;
        }

        public override string ToTexString()
        {
            return "\\int\\int_{" + _surface.ToTexString() + "}(" + GetInnerTerm().ToTexString() + ")d" + _surface.ToTexString();
        }

        public override string FinalToTexString()
        {
            return "\\int\\int_{" + _surface.ToTexString() + "}(" + GetInnerTerm().FinalToTexString() + ")d" + _surface.ToTexString();
        }

        public override string ToAsciiString()
        {
            return "\\int\\int_{" + _surface.ToAsciiString() + "}(" + GetInnerTerm().ToAsciiString() + ")d" + _dVar.ToAsciiString();
        }

        public override string FinalToAsciiString()
        {
            return "\\int\\int_{" + _surface.ToAsciiString() + "}(" + GetInnerTerm().FinalToAsciiString() + ")d" + _dVar.ToAsciiString();
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            int startingWorkSteps = ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps());
            // Only the surface differential identifier was specified.
            List<FunctionDefinition> vectorFuncs = pEvalData.GetFuncDefs().GetAllVecEquations(2);
            FunctionDefinition vectorFunc = FuncDefHelper.GetMostCurrentDef(vectorFuncs, _dVar);

            // Search for a set of parametric equations.
            List<FunctionDefinition> paraFuncs = pEvalData.GetFuncDefs().GetProbableParametricEquations(2);

            int maxIndex = int.MinValue;
            if (paraFuncs != null)
                maxIndex = FuncDefHelper.GetMostCurrentIndex(paraFuncs);

            if (vectorFunc == null && (paraFuncs == null || paraFuncs.Count == 0))
                return this;

            ExVector vectorDef = null;
            AlgebraComp withRespect0 = null;
            AlgebraComp withRespect1 = null;
            if (vectorFunc != null && vectorFunc.GetFuncDefIndex() > maxIndex)
            {
                ExComp def = pEvalData.GetFuncDefs().GetDefinition(vectorFunc).Value;
                if (def == null)
                    return this;
                withRespect0 = vectorFunc.GetInputArgs()[0];
                withRespect1 = vectorFunc.GetInputArgs()[1];
                if (!(def is ExVector))
                    return this;
                vectorDef = def as ExVector;
            }
            else if (paraFuncs.Count < 2)
                return this;
            else
            {
                vectorDef = new ExVector(paraFuncs.Count);
                for (int i = 0; i < paraFuncs.Count; ++i)
                {
                    ExComp definition = pEvalData.GetFuncDefs().GetDefinition(paraFuncs[i]).Value;
                    vectorDef.Set(i, definition);
                }
                withRespect0 = paraFuncs[0].GetInputArgs()[0];
                withRespect1 = paraFuncs[0].GetInputArgs()[1];
            }

            if (vectorDef == null || vectorDef.GetLength() > 3)
                return this;

            AndRestriction respect0Rest = pEvalData.GetVariableRestriction(withRespect0);
            AndRestriction respect1Rest = pEvalData.GetVariableRestriction(withRespect1);

            if (respect0Rest == null || respect1Rest == null)
            {
                return this;
            }

            string vectorFuncIden = "\\vec{" + _dVar.ToDispString() + "}";
            string withRespect0Str = withRespect0.ToDispString();
            string withRespect1Str = withRespect1.ToDispString();
            string integralStr = "\\int\\int_{" + _surface.ToDispString() + "}";
            string innerStr = GetInnerTerm().FinalToDispStr();
            WorkStep lastStep = null;

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + innerStr +
                "|\\frac{\\partial " + vectorFuncIden + "}{\\partial " + withRespect0Str + "}" + CrossProductOp.IDEN +
                "\\frac{\\partial " + vectorFuncIden + "}{\\partial " + withRespect1Str + "}|d" + withRespect0Str + "d" + withRespect1Str + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "|\\frac{\\partial " + vectorFuncIden + "}{\\partial " + withRespect0Str + "}" + CrossProductOp.IDEN +
                "\\frac{\\partial " + vectorFuncIden + "}{\\partial " + withRespect1Str + "}|" + WorkMgr.EDM, "Calculate.");

            pEvalData.GetWorkMgr().FromFormatted("", "Calculate.");
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp surfacePartial0 = Derivative.TakeDeriv(vectorDef, withRespect0, ref pEvalData, true, false);
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "\\frac{\\partial " + vectorFuncIden + "}{\\partial " + withRespect0Str + "}=" + WorkMgr.ToDisp(surfacePartial0) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted("", "Calculate.");
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp surfacePartial1 = Derivative.TakeDeriv(vectorDef, withRespect1, ref pEvalData, true, false);
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + "\\frac{\\partial " + vectorFuncIden + "}{\\partial " + withRespect1Str + "}=" + WorkMgr.ToDisp(surfacePartial1) + WorkMgr.EDM);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + WorkMgr.ToDisp(surfacePartial0) + CrossProductOp.IDEN + WorkMgr.ToDisp(surfacePartial1) + WorkMgr.EDM, "Calculate the cross product");
            ExComp crossed = CrossProductOp.StaticCombine(surfacePartial0, surfacePartial1);
            pEvalData.GetWorkMgr().FromSides(crossed, null, "The calculated cross product.");

            if (!(crossed is ExVector))
            {
                pEvalData.GetWorkMgr().GetWorkSteps().RemoveRange(startingWorkSteps, ArrayFunc.GetCount(pEvalData.GetWorkMgr().GetWorkSteps()) - startingWorkSteps);
                return this;
            }

            ExVector crossedVec = crossed as ExVector;
            ExComp crossedVecLength = crossedVec.GetVecLength();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "|" + crossedVec.FinalToDispStr() + "|=" + WorkMgr.ToDisp(crossedVecLength) + WorkMgr.EDM,
                "Calculate the length of the vector.");

            string surfaceDiffIdenStr = "d" + _dVar.ToDispString();
            string surfaceDiffStr = WorkMgr.ToDisp(crossedVecLength);
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + surfaceDiffIdenStr + "=" + surfaceDiffStr + WorkMgr.EDM, "The calculated surface differential");

            // Sub the parameterization of the surface into the function.
            AlgebraTerm innerTerm = GetInnerTerm();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + innerStr + surfaceDiffIdenStr + WorkMgr.EDM,
                "Change to terms of " + WorkMgr.STM + withRespect0Str + WorkMgr.EDM + " and " + WorkMgr.STM + withRespect1Str + WorkMgr.EDM);

            // X, Y, and Z have to be assumed for this.
            for (int i = 0; i < vectorDef.GetLength(); ++i)
            {
                string dimenIden = FunctionDefinition.GetDimenStr(i);
                innerTerm = innerTerm.Substitute(new AlgebraComp(dimenIden), vectorDef.Get(i));
            }

            innerStr = innerTerm.FinalToDispStr();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + integralStr + "(" + innerStr + ")(" + surfaceDiffStr + ")d" + withRespect0Str + "d" + withRespect1Str + WorkMgr.EDM);
            ExComp innerEx = MulOp.StaticCombine(innerTerm, crossedVecLength);

            Integral firstIntegral = Integral.ConstructIntegral(innerEx, withRespect0, respect0Rest.GetLower(), respect0Rest.GetUpper(), false, true);
            Integral secondIntegral = Integral.ConstructIntegral(firstIntegral, withRespect1, respect1Rest.GetLower(), respect1Rest.GetUpper(), false, true);

            pEvalData.GetWorkMgr().FromFormatted("", "Use the surface domain to convert to a definite integral.");
            lastStep = pEvalData.GetWorkMgr().GetLast();

            lastStep.GoDown(ref pEvalData);
            ExComp evaluated = ((Integral)secondIntegral.CloneEx()).Evaluate(false, ref pEvalData);
            lastStep.GoUp(ref pEvalData);

            lastStep.SetWorkHtml(WorkMgr.STM + secondIntegral.FinalToDispStr() + (evaluated is Integral ? "" : "=" + WorkMgr.ToDisp(evaluated)) + WorkMgr.EDM);

            return evaluated;
        }
    }
}