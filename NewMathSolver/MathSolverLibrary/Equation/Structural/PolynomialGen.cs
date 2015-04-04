using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.Polynomial
{
    class PolynomialGen
    {
        public static AlgebraTerm GenGenericOfDegree(int deg, AlgebraComp varFor, int startAlphaChar, List<AlgebraComp> usedVars)
        {
            if (deg > 26)
                return null;

            AlgebraTerm finalTerm = new AlgebraTerm();
            for (int i = 0; i < deg + 1; ++i)
            {
                char alphaChar = (char)(i + startAlphaChar);
                AlgebraComp generic = new AlgebraComp(alphaChar.ToString());
                usedVars.Add(generic);
                int reversedI = deg - i;

                ExComp raised = varFor.ToPow(reversedI);

                finalTerm.Add(generic, new Operators.MulOp(), raised);

                if (i != deg)
                    finalTerm.Add(new Operators.AddOp());
            }

            return finalTerm;
        }
    }
}