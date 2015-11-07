using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MathSolverWebsite.MathSolverLibrary.LangCompat
{
    public class DoubleFunc
    {
        public static bool IsInfinity(double d)
        {
            return double.IsInfinity(d);
        }
    }
}