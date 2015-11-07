using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    internal abstract class FieldTransformation : BasicAppliedFunc
    {
        public FieldTransformation(ExComp innerEx, string name, FunctionType ft, Type type)
            : base(innerEx, name, ft, type)
        {
        }

        protected ExComp GetCorrectedInnerEx(ref TermType.EvalData pEvalData)
        {
            ExComp innerEx = GetInnerEx();

            if (innerEx is AlgebraComp)
            {
                // There is a chance this is actually refering to a function.
                AlgebraComp funcIden = innerEx as AlgebraComp;
                KeyValuePair<FunctionDefinition, ExComp> def = pEvalData.GetFuncDefs().GetDefinition(funcIden);
                if (def.Key != null && def.Key.GetInputArgCount() > 1)
                {
                    innerEx = def.Value;
                    if (innerEx is AlgebraTerm)
                        innerEx = (innerEx as AlgebraTerm).RemoveRedundancies(false);
                }
            }

            return innerEx;
        }
    }
}