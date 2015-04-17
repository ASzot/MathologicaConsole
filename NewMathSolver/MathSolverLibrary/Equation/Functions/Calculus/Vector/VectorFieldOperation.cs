using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    abstract class FieldTransformation : BasicAppliedFunc
    {
        public FieldTransformation(ExComp innerEx, string name, FunctionType ft, Type type)
            : base(innerEx, name, ft, type)
        {

        }
    }
}
