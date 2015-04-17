using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    class GradientFunc : FieldTransformation
    {
        public GradientFunc(ExComp ex)
            : base(ex, "\\nabla", FunctionType.Gradient, typeof(GradientFunc))
        {

        }

        public static bool IsSuitableField(ExComp ex)
        {
            if (ex is ExMatrix)
                return false;
            return true;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = InnerEx;

            AlgebraComp x, y, z;
            
            bool isFuncDeriv;

            if (innerEx is AlgebraComp)
            {
                innerEx = new FunctionDefinition(innerEx as AlgebraComp,
                    new AlgebraComp[] 
                    { 
                        new AlgebraComp("x"), 
                        new AlgebraComp("y"), 
                        new AlgebraComp("z") 
                    }, null);
            }

            if (innerEx is FunctionDefinition)
            {
                x = new AlgebraComp("x");
                y = new AlgebraComp("z");
                z = (innerEx as FunctionDefinition).InputArgCount == 3 ? new AlgebraComp("z") : null;
                isFuncDeriv = true;
            }
            else
            {
                isFuncDeriv = false;
                x = new AlgebraComp("x");
                y = new AlgebraComp("z");
                z = new AlgebraComp("z");
            }

            ExComp derivX = Derivative.TakeDeriv(innerEx, x, ref pEvalData, true, isFuncDeriv);
            ExComp derivY = Derivative.TakeDeriv(innerEx, y, ref pEvalData, true, isFuncDeriv);
            ExComp derivZ = Derivative.TakeDeriv(innerEx, z, ref pEvalData, true, isFuncDeriv);

            return new ExVector(derivX, derivY, derivZ);
        }
    }
}
