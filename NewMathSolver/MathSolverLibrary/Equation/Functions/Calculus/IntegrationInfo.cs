using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    class IntegrationInfo
    {
        public const int MAX_U_SUB_COUNT = 3;
        public const int MAX_BY_PARTS_COUNT = 3;

        public int USubCount = 0;
        public int ByPartsCount = 0;

        public void IncPartsCount()
        {
            ByPartsCount++;
        }

        public void IncSubCount()
        {
            USubCount++;
        }
    }
}
