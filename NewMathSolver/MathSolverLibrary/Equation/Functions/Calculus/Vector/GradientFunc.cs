using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    internal class GradientFunc : FieldTransformation
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
            CallChildren(harshEval, ref pEvalData);

            ExComp innerEx = GetCorrectedInnerEx(ref pEvalData);

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
                    }, null, true);
            }

            if (innerEx is FunctionDefinition)
            {
                x = new AlgebraComp("x");
                y = new AlgebraComp("y");
                z = (innerEx as FunctionDefinition).GetInputArgCount() == 3 ? new AlgebraComp("z") : null;
                isFuncDeriv = true;
            }
            else
            {
                isFuncDeriv = false;
                x = new AlgebraComp("x");
                y = new AlgebraComp("y");
                z = new AlgebraComp("z");
            }

            ExComp derivX = Derivative.TakeDeriv(innerEx.CloneEx(), x, ref pEvalData, true, isFuncDeriv);
            ExComp derivY = Derivative.TakeDeriv(innerEx.CloneEx(), y, ref pEvalData, true, isFuncDeriv);
            ExComp derivZ = Derivative.TakeDeriv(innerEx.CloneEx(), z, ref pEvalData, true, isFuncDeriv);

            return new ExVector(derivX, derivY, derivZ);
        }
    }
}