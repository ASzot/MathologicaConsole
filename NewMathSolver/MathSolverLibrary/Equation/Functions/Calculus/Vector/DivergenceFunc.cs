using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    class DivergenceFunc : FieldTransformation
    {
        public DivergenceFunc(ExComp innerEx)
            : base(innerEx, "div", FunctionType.Divergence, typeof(DivergenceFunc))
        {

        }

        public static bool IsSuitableField(ExComp innerEx)
        {
            if (innerEx is ExVector)
            {
                ExVector exVec = innerEx as ExVector;
                return exVec.Length > 1 && exVec.Length < 4;
            }
            else if (innerEx is FunctionDefinition)
            {
                FunctionDefinition funcDef = innerEx as FunctionDefinition;

                return funcDef.InputArgCount > 1 && funcDef.InputArgCount < 4;
            }
            else if (innerEx is AlgebraComp)
            {
                // The user is reasonably referring to a function not an individual variable.
                return true;
            }
            else
                return false;
        }

        protected override ExComp CancelWith(ExComp innerEx, ref TermType.EvalData evalData)
        {
            if (innerEx is CurlFunc)
                return Number.Zero;
            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            if (!IsSuitableField(InnerEx))
                return Number.Undefined;

            ExComp p, q, r;
            ExComp innerEx = InnerEx;
            ExComp cancelWithResult = CancelWith(innerEx, ref pEvalData);
            if (cancelWithResult != null)
                return cancelWithResult;

            AlgebraComp x = null;
            AlgebraComp y = null;
            AlgebraComp z = null;

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

            if (innerEx is ExVector)
            {
                ExVector innerVec = innerEx as ExVector;
                p = innerVec.X;
                q = innerVec.Y;
                r = innerVec.Z;
                isFuncDeriv = false;
            }
            else if (innerEx is FunctionDefinition)
            {
                FunctionDefinition funcDef = innerEx as FunctionDefinition;
                p = new AlgebraComp("P");
                q = new AlgebraComp("Q");
                r = funcDef.InputArgCount == 3 ? new AlgebraComp("R") : null;

                x = funcDef.InputArgs[0];
                y = funcDef.InputArgs[1];
                z = funcDef.InputArgCount == 3 ? funcDef.InputArgs[2] : null;

                isFuncDeriv = true;
            }
            else
                return this;

            if (x == null)
            {
                x = new AlgebraComp("x");
                y = new AlgebraComp("y");
                z = new AlgebraComp("z");
            }

            ExComp p_x = Derivative.TakeDeriv(p, x, ref pEvalData, true, isFuncDeriv);
            ExComp q_y = Derivative.TakeDeriv(q, y, ref pEvalData, true, isFuncDeriv);

            ExComp r_z;
            if (z != null)
                r_z = Derivative.TakeDeriv(r, z, ref pEvalData, true, isFuncDeriv);
            else
                r_z = Number.Zero;

            return AddOp.StaticCombine(AddOp.StaticCombine(p_x, q_y), r_z);
        }
    }
}
