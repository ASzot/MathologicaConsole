using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    internal class DivergenceFunc : FieldTransformation
    {
        public DivergenceFunc(ExComp innerEx)
            : base(innerEx, "\\text{div}", FunctionType.Divergence, typeof(DivergenceFunc))
        {
        }

        public static bool IsSuitableField(ExComp innerEx)
        {
            if (innerEx is ExVector)
            {
                ExVector exVec = innerEx as ExVector;
                return exVec.GetLength() > 1 && exVec.GetLength() < 4;
            }
            else if (innerEx is FunctionDefinition)
            {
                FunctionDefinition funcDef = innerEx as FunctionDefinition;

                return funcDef.GetInputArgCount() > 1 && funcDef.GetInputArgCount() < 4;
            }
            else if (innerEx is AlgebraComp)
            {
                // The user is reasonably referring to a function not an individual variable.
                return true;
            }
            else
                return false;
        }

        public override ExComp CancelWith(ExComp innerEx, ref TermType.EvalData evalData)
        {
            if (innerEx is CurlFunc)
                return ExNumber.GetZero();
            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            if (!IsSuitableField(GetInnerEx()))
                return ExNumber.GetUndefined();

            ExComp p, q, r;
            ExComp innerEx = GetCorrectedInnerEx(ref pEvalData);

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
                    }, null, true);
            }

            if (innerEx is ExVector)
            {
                ExVector innerVec = innerEx as ExVector;
                p = innerVec.GetX();
                q = innerVec.GetY();
                r = innerVec.GetZ();
                isFuncDeriv = false;
            }
            else if (innerEx is FunctionDefinition)
            {
                FunctionDefinition funcDef = innerEx as FunctionDefinition;
                p = new AlgebraComp("P");
                q = new AlgebraComp("Q");
                r = funcDef.GetInputArgCount() == 3 ? new AlgebraComp("R") : null;

                x = funcDef.GetInputArgs()[0];
                y = funcDef.GetInputArgs()[1];
                z = funcDef.GetInputArgCount() == 3 ? funcDef.GetInputArgs()[2] : null;

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
                r_z = ExNumber.GetZero();

            return AddOp.StaticCombine(AddOp.StaticCombine(p_x, q_y), r_z);
        }
    }
}