using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    class CalcConstant : AlgebraComp
    {
        public CalcConstant()
            : base("C")
        {

        }

        public override double GetCompareVal()
        {
            // Always goes at the end.

            return -1.0;
        }

        public override ExComp Clone()
        {
            return new CalcConstant();
        }
    }
}
