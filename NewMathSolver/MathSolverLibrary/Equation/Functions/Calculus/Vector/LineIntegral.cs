using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathSolverWebsite.MathSolverLibrary.Information_Helpers;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    class LineIntegral : Integral
    {
        private AlgebraComp _surfaceIden;

        public LineIntegral(ExComp innerEx)
            : base(innerEx)
        {

        }

        public static LineIntegral ConstructLineIntegral(ExComp innerEx, AlgebraComp surfaceIden, AlgebraComp withRespectTo)
        {
            LineIntegral lineIntegral = new LineIntegral(innerEx);
            lineIntegral._surfaceIden = surfaceIden;
            lineIntegral._dVar = withRespectTo;

            return lineIntegral;
        }

        private ExComp EvaluateScalarField(ref TermType.EvalData pEvalData, AlgebraComp pathVar, AndRestriction pathRestriction, TypePair<string, ExComp>[] useDefs)
        {
            ExComp[] derived = new ExComp[useDefs.Length];
            for (int i = 0; i < derived.Length; ++i)
            {
                derived[i] = Derivative.TakeDeriv(useDefs[i].Data2, pathVar, ref pEvalData);
            }

            ExComp[] squared = new ExComp[derived.Length];
            for (int i = 0; i < squared.Length; ++i)
            {
                squared[i] = PowOp.StaticCombine(derived[i], new Number(2.0));
            }

            ExComp combined = null;
            for (int i = 0; i < squared.Length; ++i)
            {
                if (combined == null)
                    combined = squared[i];
                else
                    combined = AddOp.StaticCombine(combined, squared[i]);
            }

            ExComp surfaceDifferential = PowOp.StaticCombine(combined, AlgebraTerm.FromFraction(Number.One, new Number(2.0)));

            AlgebraTerm innerTerm = InnerTerm;
            for (int i = 0; i < useDefs.Length; ++i)
            {
                innerTerm = innerTerm.Substitute(new AlgebraComp(useDefs[i].Data1), useDefs[i].Data2);
            }

            ExComp totalInner = MulOp.StaticCombine(innerTerm, surfaceDifferential);

            Integral integral = Integral.ConstructIntegral(totalInner, pathVar, pathRestriction.GetLower(), pathRestriction.GetUpper());

            return integral.Evaluate(false, ref pEvalData);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {

            // Get the line.
            List<FunctionDefinition> vectorFuncs = pEvalData.FuncDefs.GetAllVecEquations(1);
            FunctionDefinition vectorFunc = FuncDefHelper.GetMostCurrentDef(vectorFuncs);

            List<FunctionDefinition> paraFuncs = pEvalData.FuncDefs.GetProbableParametricEquations(1);
            int maxIndex = FuncDefHelper.GetMostCurrentIndex(paraFuncs);

            if (vectorFunc == null && (paraFuncs == null || paraFuncs.Count == 0))
                return this;


            TypePair<string, ExComp>[] useDefs;
            AlgebraComp pathVar = null;

            if (vectorFunc != null && vectorFunc.FuncDefIndex > maxIndex)
            {
                useDefs = pEvalData.FuncDefs.GetDefinitionToPara(vectorFunc);
                pathVar = vectorFunc.InputArgs[0];
            }
            else if (paraFuncs.Count < 2)
                return this;
            else
            {
                useDefs = new TypePair<string, ExComp>[paraFuncs.Count];
                pathVar = paraFuncs[0].InputArgs[0];
                for (int i = 0; i < useDefs.Length; ++i)
                {
                    ExComp definition = pEvalData.FuncDefs.GetDefinition(paraFuncs[i]).Value;
                    useDefs[i] = new TypePair<string, ExComp>(paraFuncs[i].Iden.Var.Var, definition);
                }
            }

            AndRestriction pathRestriction = pEvalData.GetVariableRestriction(pathVar);

            ExComp innerEx = InnerEx;
            if (innerEx is ExVector)
            {
                return this;
            }
            else if (innerEx is ExMatrix)
                return this;
            else
                return EvaluateScalarField(ref pEvalData, pathVar, pathRestriction, useDefs);
        }
    }
}
