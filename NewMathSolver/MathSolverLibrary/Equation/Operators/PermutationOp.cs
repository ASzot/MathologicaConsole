using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    class PermutationOp : AgOp
    {
        public override ExComp Clone()
        {
            return new PermutationOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return new Functions.PermutationFunction(ex1, ex2);
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return new Functions.PermutationFunction(ex1, ex2);
        }

        public override string ToAsciiString()
        {
            return "_P_";
        }

        public override string ToTexString()
        {
            return "_P_";
        }

        public override string ToString()
        {
            return "_P_";
        }
    }
}
