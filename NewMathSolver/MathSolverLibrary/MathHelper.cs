using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal static class MathHelper
    {
        // The max decimal place where the GCF will be calculated in terms of a whole number.
        private const int MAX_GCF_ZERO_DIST = 5;

        public static double GCFDouble(double n1, double n2)
        {
            n1 = Equation.ExNumber.EpsilonCorrect(n1);
            n2 = Equation.ExNumber.EpsilonCorrect(n2);
            if (n1 == 0.0 || n2 == 0.0)
                return 0.0;

            if (double.IsNaN(n1) || double.IsNaN(n2))
                return double.NaN;

            if (DoubleFunc.IsInfinity(n1) || DoubleFunc.IsInfinity(n2))
                return double.PositiveInfinity;

            double an1 = Math.Abs(n1);
            double an2 = Math.Abs(n2);

            double multiple = 1;

            // Check if either of the numbers are not whole numbers.
            if (an1 % 1 != 0 || an2 % 1 != 0)
            {
                // Either the top or the bottom is not whole.
                int distanceFromZero1 = 0, distanceFromZero2 = 0;
                int index;
                string afterPointStr;
                if (an1 % 1 != 0)
                {
                    string an1Str = an1.ToString();
                    // Check how far the string goes out after the decimal point.
                    index = an1Str.IndexOf('.');
                    afterPointStr = an1Str.Substring(index);
                    distanceFromZero1 = afterPointStr.Length - 1;
                }

                if (an2 % 1 != 0)
                {
                    string an2Str = an2.ToString();
                    // Check how far the string goes out after the decimal point.
                    index = an2Str.IndexOf('.');
                    afterPointStr = an2Str.Substring(index);
                    distanceFromZero2 = afterPointStr.Length - 1;
                }

                int maxDistanceFromZero = Math.Max(distanceFromZero1, distanceFromZero2);
                //if (maxDistanceFromZero > MAX_GCF_ZERO_DIST)
                //    return n1 * n2;

                multiple *= Math.Pow(10, maxDistanceFromZero);
                an1 *= multiple;
                an2 *= multiple;
            }

            while (an1 != 0)
            {
                double tmp = an1;
                an1 = an2 % an1;
                an2 = tmp;
            }

            if (n1 < 0)
                return -an2 / multiple;

            return an2 / multiple;
        }

        public static int GCFInt(int n1, int n2)
        {
            int an1 = Math.Abs(n1);
            int an2 = Math.Abs(n2);

            while (an1 != 0)
            {
                int tmp = an1;
                an1 = an2 % an1;
                an2 = tmp;
            }

            if (n1 < 0)
                return -an2;

            return an2;
        }

        public static int[] GetCommonDivisors(int n1, int n2, bool includeSelf)
        {
            List<int> divisors = new List<int>();
            int[] divisorsN1 = GetDivisors(n1, includeSelf, false);
            int[] divisorsN2 = GetDivisors(n2, includeSelf, false);

            divisors.AddRange(divisorsN1);
            divisors.AddRange(divisorsN2);

            return divisors.Distinct().ToArray();
        }

        public static string GetCountingPrefix(int num)
        {
            string numStr = num.ToString();

            if (numStr.EndsWith("1"))
                return "st";
            else if (numStr.EndsWith("2"))
                return "nd";
            else if (numStr.EndsWith("3"))
                return "rd";

            return "th";
        }

        public static int[] GetDivisors(int n, bool includeSelf, bool includeOne)
        {
            List<int> divisors = new List<int>();
            if (includeSelf)
            {
                for (int i = 2; i <= n; ++i)
                {
                    if (n % i == 0)
                        divisors.Add(i);
                }
            }
            else
            {
                for (int i = 2; i < n; ++i)
                {
                    if (n % i == 0)
                        divisors.Add(i);
                }
            }

            if (includeOne)
                divisors.Add(1);

            return divisors.ToArray();
        }

        public static bool IsPerfectRoot(int n, int root, out int resultInt)
        {
            double result = Math.Pow(n, 1.0 / root);

            resultInt = (int)result;

            return result == (double)resultInt;
        }

        public static double LCFDouble(double d1, double d2)
        {
            d1 = Equation.ExNumber.EpsilonCorrect(d1);
            d2 = Equation.ExNumber.EpsilonCorrect(d2);

            if (d1 == 0.0 || d2 == 0.0)
                return 0.0;

            return (d1 / GCFDouble(d1, d2)) * d2;
        }

        public static int LCFInt(int n1, int n2)
        {
            return (n1 / GCFInt(n1, n2)) * n2;
        }
    }
}