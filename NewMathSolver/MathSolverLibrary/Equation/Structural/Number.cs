using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class Number : ExComp
    {
        private const double EPSILON_ACCEPT = 1E-7;
        private const int ROUND_COUNT = 9;
        private double d_imagComp;
        private double d_realComp;

        public const int FINAL_ROUND_COUNT = 4;

        public static Number ImagOne
        {
            get { return new Number(0.0, 1.0); }
        }

        public static Number NegInfinity
        {
            get { return new Number(double.NegativeInfinity); }
        }

        public static Number NegOne
        {
            get { return new Number(-1.0); }
        }

        public static Number One
        {
            get { return new Number(1.0); }
        }

        public static Number PosInfinity
        {
            get { return new Number(double.PositiveInfinity); }
        }

        public static Number Undefined
        {
            get { return new Number(double.NaN, double.NaN); }
        }

        public static Number Zero
        {
            get { return new Number(0.0); }
        }

        public Number Imag
        {
            get { return new Number(d_imagComp); }
        }

        public double ImagComp
        {
            get { return d_imagComp; }
            set { d_imagComp = value; }
        }

        public double ImagCompRnd
        {
            get { return Math.Round(ImagComp, ROUND_COUNT); }
        }

        public Number Real
        {
            get { return new Number(d_realComp); }
        }

        public double RealComp
        {
            get { return d_realComp; }
            set { d_realComp = value; }
        }

        public double RealCompRnd
        {
            get { return Math.Round(RealComp, ROUND_COUNT); }
        }

        public Number()
        {
            d_realComp = 0.0;
            d_imagComp = 0.0;
        }

        public Number(double realComp)
        {
            d_realComp = realComp;
            d_imagComp = 0.0;
        }

        public Number(int realComp)
            : this((double)realComp, 0.0)
        {
        }

        public Number(long realComp)
            : this((double)realComp, 0.0)
        {
        }

        public Number(double realComp, double imagComp)
        {
            d_realComp = realComp;
            d_imagComp = imagComp;
        }

        public static Number Abs(Number n1)
        {
            return new Number(Math.Abs(n1.RealComp), Math.Abs(n1.ImagComp));
        }

        public static bool CleanRootExists(Number n1, Number root)
        {
            if (n1.HasImaginaryComp() || root.HasImaginaryComp())
                throw new ArgumentException();

            double result = Math.Pow(n1.RealComp, 1.0 / root.RealComp);
            return result.IsInteger();
        }

        public static double EpsilonCorrect(double d)
        {
            double absD = Math.Abs(d);
            if (absD > (double)int.MaxValue)
                return d;
            double intD = (double)(int)absD;
            double nextIntD = (double)(((int)absD) + 1);
            double dDecimal = absD - intD;

            if (dDecimal < EPSILON_ACCEPT)
                return intD * (d < 0.0 ? -1.0 : 1.0);

            dDecimal = nextIntD - absD;
            if (dDecimal < EPSILON_ACCEPT)
                return nextIntD * (d < 0.0 ? -1.0 : 1.0);

            return Math.Round(d, ROUND_COUNT);
        }

        public static Number GCF(List<Number> numbers)
        {
            List<Number> narrowedList = new List<Number>();

            if (numbers.Count == 1)
                return numbers[0];
            if (numbers.Count == 2)
            {
                return GCF(numbers[0], numbers[1]);
            }

            for (int i = 1; i < numbers.Count; ++i)
            {
                Number n1 = numbers[i];
                Number n2 = numbers[i - 1];
                Number gcf = GCF(n1, n2);
                narrowedList.Add(gcf);
            }

            return GCF(narrowedList);
        }

        public static Number GCF(Number n1, Number n2)
        {
            double gcf;
            if (n1.RealComp != 0.0 && n2.ImagComp != 0.0)
                gcf = MathHelper.GCFDouble(n1.RealComp, n1.ImagComp);
            else if (n1.RealComp != 0.0)
                gcf = n1.RealComp;
            else
                gcf = n1.ImagComp;

            if (n2.RealComp != 0.0 && n2.ImagComp != 0.0)
            {
                gcf = MathHelper.GCFDouble(gcf, n2.RealComp);
                gcf = MathHelper.GCFDouble(gcf, n2.ImagComp);
            }
            else if (n2.RealComp != 0.0)
                gcf = MathHelper.GCFDouble(gcf, n2.RealComp);
            else
                gcf = MathHelper.GCFDouble(gcf, n2.ImagComp);

            gcf = Math.Abs(gcf);

            if (n1.RealComp == 0.0 && n2.RealComp == 0.0)
                return new Number(0.0, gcf);

            return new Number(gcf);
        }

        public static void GCF_Base(Number n1, Number n2, out Number n1Pow, out Number n2Pow, out Number nBase)
        {
            n1Pow = null;
            n2Pow = null;
            nBase = null;

            if (!n1.IsRealInteger() || !n2.IsRealInteger())
                return;

            int i1 = (int)n1.RealComp;
            int i2 = (int)n2.RealComp;

            // We have (g = h) we want to change this to (b^x = b^y).

            // 8 - 4 -> 2^3 - 2^2

            int[] divisors = MathHelper.GetCommonDivisors(i1, i2, true);
            List<int> sortedDivisors = divisors.ToList();
            sortedDivisors.Sort();

            int minVal = -1;
            foreach (int divisor in sortedDivisors)
            {
                // divisor ^ x = i1
                // log_(divisor)(i1)
                double log1 = Math.Log(i1, divisor);
                double log2 = Math.Log(i2, divisor);

                if (log1.IsInteger() && log2.IsInteger())
                {
                    minVal = divisor;
                    break;
                }
            }

            if (minVal == -1)
                return;

            nBase = new Number(minVal);
            double dPow1 = Math.Log(i1, minVal);
            double dPow2 = Math.Log(i2, minVal);
            n1Pow = new Number(dPow1);
            n2Pow = new Number(dPow2);
        }

        public static bool IsUndef(ExComp ex)
        {
            if (ex is Number)
            {
                Number nEx = ex as Number;
                if (double.IsNaN(nEx.d_realComp) || double.IsNaN(nEx.d_imagComp))
                    return true;
            }
            else if (ex is AlgebraTerm && !(ex is AlgebraFunction))
            {
                AlgebraTerm term = ex as AlgebraTerm;
                if (term is GeneralSolution)
                {
                    GeneralSolution genSol = term as GeneralSolution;
                    return genSol.IsResultUndef();
                }
                else
                {
                    foreach (ExComp comp in term.SubComps)
                    {
                        if (IsUndef(comp))
                            return true;
                    }
                }
            }

            return false;
        }

        public static Number LCF(Number n1, Number n2)
        {
            double lcfReal = MathHelper.LCFDouble(n1.RealComp, n2.RealComp);
            double lcfImag = MathHelper.LCFDouble(n1.ImagComp, n2.ImagComp);
            lcfReal = Math.Abs(lcfReal);
            lcfImag = Math.Abs(lcfImag);

            return new Number(lcfReal, lcfImag);
        }

        public static Number Maximum(Number n1, Number n2)
        {
            if (n1.d_realComp > n2.d_realComp)
                return n1;
            else
                return n2;
        }

        public static Number Minimum(Number n1, Number n2)
        {
            if (n1.d_realComp < n2.d_realComp)
                return n1;
            else
                return n2;
        }

        public static Number Parse(string parseStr)
        {
            if (parseStr.Contains("i"))
            {
                int imagIndex = parseStr.IndexOf('i');
                parseStr = parseStr.Remove(imagIndex, 1);
                if (parseStr == "")
                {
                    return new Number(0, 1.0);
                }
                double imag;
                if (!double.TryParse(parseStr, out imag))
                    return null;
                return new Number(0, imag);
            }

            double real;
            if (!double.TryParse(parseStr, out real))
                return null;
            return new Number(real, 0);
        }

        public static Number RaiseToPower(Number n1, double d)
        {
            if (n1.HasImaginaryComp())
                return null;

            Number resultant = new Number(Math.Pow(n1.RealComp, d), 0.0);
            return resultant;
        }

        public static Number RaiseToPower(Number n1, Number n2)
        {
            if (n1.HasImaginaryComp() || n2.HasImaginaryComp())
                return null;

            return RaiseToPower(n1, n2.RealComp);
        }

        public static Number TakeRootOf(Number n1, double root)
        {
            if (n1.HasImaginaryComp())
                throw new ArgumentException();
            Number resultant = new Number(Math.Pow(n1.RealComp, 1.0 / root), 0.0);
            return resultant;
        }

        public static Number TakeRootOf(Number n1, Number root)
        {
            if (n1.HasImaginaryComp() || root.HasImaginaryComp())
                throw new ArgumentException();

            return TakeRootOf(n1, root.RealComp);
        }

        public void Add(double realComp)
        {
            Add(realComp, 0.0);
        }

        public void Add(Number n)
        {
            Add(n.RealComp, n.ImagComp);
        }

        public void Add(double realComp, double imagComp)
        {
            d_realComp += realComp;
            d_imagComp += imagComp;

            EpsilonCorrect();
        }

        public void AssignTo(Number n)
        {
            d_realComp = n.d_realComp;
            d_imagComp = n.d_imagComp;
        }

        public override ExComp Clone()
        {
            return new Number(d_realComp, d_imagComp);
        }

        public void ConvertToLowestBase(out Number nBase, out Number nPow)
        {
            nBase = null;
            nPow = null;

            if (!this.IsRealInteger())
                return;

            int n = (int)d_realComp;

            int[] divisors = n.GetDivisors();
            List<int> sortedDivisors = divisors.ToList();
            sortedDivisors.Sort();

            int minVal = -1;
            foreach (int divisor in sortedDivisors)
            {
                // divisor ^ x = i1
                // log_(divisor)(i1)
                double logVal = Math.Log(n, divisor);

                if (logVal.IsInteger())
                {
                    minVal = divisor;
                    break;
                }
            }

            if (minVal == -1)
                return;

            nBase = new Number(minVal);
            double dPow = Math.Log(n, minVal);
            nPow = new Number(dPow);
        }

        public void EpsilonCorrect()
        {
            d_realComp = EpsilonCorrect(d_realComp);
            d_imagComp = EpsilonCorrect(d_imagComp);
        }

        public string FinalToDispString()
        {
            if (IsUndefined())
                return "Undefined";
            if (IsNegInfinity())
                return "-oo";
            if (IsPosInfinity())
                return "oo";
            if (HasImaginaryComp())
            {
                string str = "";
                if (d_realComp != 0.0)
                    str += d_realComp.ToString() + "+";

                if (d_imagComp == -1.0)
                    str += "-";
                else if (d_imagComp != 1.0)
                    str += d_imagComp.ToString();

                str += "i";

                return str;
            }

            return d_realComp.ToString();
        }

        public override double GetCompareVal()
        {
            return 0.0;
        }

        public string GetCountingPrefix()
        {
            if (!HasImaginaryComp())
            {
                string numStr = this.ToString();
                if (numStr.EndsWith("1"))
                    return "st";
                else if (numStr.EndsWith("2"))
                    return "nd";
                else if (numStr.EndsWith("3"))
                    return "rd";
            }

            return "th";
        }

        public List<TypePair<Number, Number>> GetDivisors()
        {
            List<TypePair<Number, Number>> factors = new List<TypePair<Number, Number>>();
            if (!IsRealInteger() || HasImaginaryComp())
                return factors;         // Just an empty list.

            int realCompInt = (int)d_realComp;
            realCompInt = Math.Abs(realCompInt);

            for (int i = 1; i <= realCompInt; ++i)
            {
                if (realCompInt % i == 0)
                {
                    TypePair<Number, Number> pair = new TypePair<Number, Number>();
                    int num1 = i;
                    int num2 = realCompInt / i;
                    int highOrder = Math.Max(num1, num2);
                    int lowOrder = Math.Min(num1, num2);
                    pair.Data1 = new Number((double)highOrder);
                    pair.Data2 = new Number((double)lowOrder);
                    factors.Add(pair);
                }
            }

            if (d_realComp < 0)
            {
                List<TypePair<Number, Number>> finalPair = new List<TypePair<Number, Number>>();
                for (int i = 0; i < factors.Count; ++i)
                {
                    TypePair<Number, Number> pair = factors.ElementAt(i);
                    TypePair<Number, Number> newPair1 = new TypePair<Number, Number>();
                    newPair1.Data1 = pair.Data1;
                    newPair1.Data2 = -pair.Data2;

                    finalPair.Add(newPair1);

                    TypePair<Number, Number> newPair2 = new TypePair<Number, Number>();
                    newPair2.Data1 = -newPair1.Data1;
                    newPair2.Data2 = -newPair1.Data2;

                    finalPair.Add(newPair2);
                }

                return finalPair;
            }

            return factors.Distinct().ToList();
        }

        /// <summary>
        /// Includes the negative divisors.
        /// </summary>
        /// <returns></returns>
        public List<TypePair<Number, Number>> GetDivisorsSignInvariant()
        {
            if (!IsRealInteger() || HasImaginaryComp())
                return new List<TypePair<Number, Number>>(); ;         // Just an empty list.

            var signInvariantDivisors = GetDivisors();

            if (d_realComp > 0)
            {
                // We have to account for the negative negative situation.
                for (int i = 0; i < signInvariantDivisors.Count; ++i)
                {
                    TypePair<Number, Number> ip = signInvariantDivisors.ElementAt(i);

                    // Make it into a negative negative combination.
                    // We can safely assume both of these numbers are positive as they are the divisors of a positive number.
                    TypePair<Number, Number> newPair = new TypePair<Number, Number>();
                    newPair.Data1 = -ip.Data1;
                    newPair.Data2 = -ip.Data2;

                    signInvariantDivisors.Insert(i, newPair);

                    // Skip over the added entry.
                    ++i;
                }
            }

            return signInvariantDivisors;
        }

        public override int GetHashCode()
        {
            //TODO:
            // Make this an actual hash value!
            int modifier = d_realComp < d_imagComp ? 0x45f : 0x5ab;
            return modifier;
        }

        public void GetPolarData(out ExComp mag, out ExComp angle, ref TermType.EvalData pEvalData)
        {
            mag = null;
            angle = null;

            Number real = new Number(Math.Pow(d_realComp, 2.0));
            Number imag = new Number(Math.Pow(d_imagComp, 2.0));
            mag = Operators.PowOp.StaticCombine(real + imag, new Number(0.5));

            if (Real == 0.0 && Imag == 0.0)
            {
                angle = Number.Zero;
                return;
            }
            if (Real == 0.0)
            {
                angle = Imag > 0.0 ? AlgebraTerm.FromFraction(Constant.Pi, new Number(2.0)) :
                    AlgebraTerm.FromFraction(new AlgebraTerm(new Number(3.0), new Operators.MulOp(), Constant.Pi), new Number(2.0));
                return;
            }
            if (Imag == 0.0)
            {
                angle = Real > 0.0 ? (ExComp)Number.Zero : (ExComp)Constant.Pi;
                return;
            }
            ExComp div = Operators.DivOp.StaticCombine(Imag, Real);
            Functions.TanFunction tan = new Functions.TanFunction(div);
            ExComp evaluated = tan.Evaluate(false, ref pEvalData);

            Term.SimpleFraction simpFrac = new Term.SimpleFraction();
            Number num, den;
            if (simpFrac.LooseInit(evaluated.ToAlgTerm()) && simpFrac.IsSimpleUnitCircleAngle(out num, out den))
            {
                // Adjusting might have to be done to the angle as atan has a range of [-pi/2, pi/2].

                if (d_imagComp > 0.0 && num > den)
                {
                    num = num - den;
                }
                else if (d_imagComp < 0.0 && den > num)
                {
                    num = num + den;
                }

                angle = AlgebraTerm.FromFraction(new AlgebraTerm(num, new Operators.MulOp(), Constant.Pi), den);
            }
            else
                angle = evaluated;
        }

        public Number GetReciprocal()
        {
            double real = d_realComp == 0.0 ? 0.0 : 1.0 / d_realComp;
            double imag = d_imagComp == 0.0 ? 0.0 : 1.0 / d_imagComp;
            return new Number(real, imag);
        }

        public bool HasImaginaryComp()
        {
            return (d_imagComp != 0.0 && !IsUndefined());
        }

        public bool HasImagRealComp()
        {
            return (d_realComp != 0.0 && d_imagComp != 0.0);
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (d_realComp == 0.0 && d_imagComp == 0.0 && ex is AlgebraTerm && (ex as AlgebraTerm).TermCount == 0)
                return true;
            if (ex is Number)
            {
                Number number = ex as Number;
                EpsilonCorrect();
                number.EpsilonCorrect();
                return ((this.RealCompRnd == number.RealCompRnd) && (this.ImagCompRnd == number.ImagCompRnd));
            }

            return false;
        }

        public bool IsEven()
        {
            if (!IsRealInteger())
                return false;
            return ((int)d_realComp) % 2 == 0;
        }

        public bool IsInfinity()
        {
            return Double.IsInfinity(d_realComp);
        }

        #region Operators

        public static Number operator -(Number n1, Number n2)
        {
            Number resultant = new Number(n1.RealComp - n2.RealComp, n1.ImagComp - n2.ImagComp);

            resultant.EpsilonCorrect();

            return resultant;
        }

        public static Number operator -(Number n1)
        {
            return new Number(-n1.RealComp, -n1.ImagComp);
        }

        public static Number operator -(Number n1, double d)
        {
            n1.RealComp -= d;
            if (Math.Abs(n1.RealComp) < EPSILON_ACCEPT)
                n1.RealComp = 0.0;
            return n1;
        }

        public static bool operator !=(Number n1, double d)
        {
            return !(n1 == d);
        }

        public static bool operator !=(Number n1, Number n2)
        {
            return !(n1 == n2);
        }

        public static Number operator %(Number n1, Number n2)
        {
            if (!n1.IsRealInteger() || !n2.IsRealInteger())
                return null;

            int realInt1 = (int)n1.RealComp;
            int realInt2 = (int)n2.RealComp;

            int realResult = realInt1 % realInt2;

            return new Number((double)realResult);
        }

        public static Number operator %(Number n1, int n2)
        {
            if (!n1.IsRealInteger())
                return null;

            int realInt1 = (int)n1.RealComp;

            int realResult = realInt1 % n2;

            return new Number((double)realResult);
        }

        public static Number operator *(Number n1, Number n2)
        {
            double real = (n1.RealComp * n2.RealComp) - (n1.ImagComp * n2.ImagComp);
            double imag = (n1.RealComp * n2.ImagComp) + (n1.ImagComp * n2.RealComp);

            real = EpsilonCorrect(real);
            imag = EpsilonCorrect(imag);

            return new Number(real, imag);
        }

        public static Number operator *(Number n1, double d)
        {
            return n1 * new Number(d);
        }

        public static ExComp operator /(Number n1, Number n2)
        {
            //double real = ((n1.RealComp * n2.RealComp) + (n1.ImagComp * n2.ImagComp)) /
            //    ((n2.RealComp * n2.RealComp) + (n2.ImagComp * n2.ImagComp));

            //double imag = ((n1.ImagComp * n2.RealComp) - (n1.RealComp * n2.ImagComp)) /
            //    ((n2.RealComp * n2.RealComp) + (n2.ImagComp * n2.ImagComp));

            //Number resultant = new Number(real, imag);
            //return resultant;

            if (!n1.HasImaginaryComp() && !n2.HasImaginaryComp())
            {
                Number resultant = new Number(n1.RealComp / n2.RealComp);
                resultant.EpsilonCorrect();
                return resultant;
            }

            double realNum = ((n1.RealComp * n2.RealComp) + (n1.ImagComp * n2.ImagComp));
            double realDen = ((n2.RealComp * n2.RealComp) + (n2.ImagComp * n2.ImagComp));

            double imagNum = ((n1.ImagComp * n2.RealComp) - (n1.RealComp * n2.ImagComp));
            double imagDen = ((n2.RealComp * n2.RealComp) + (n2.ImagComp * n2.ImagComp));

            ExComp realEx = Operators.DivOp.StaticCombine(new Number(realNum), new Number(realDen));
            ExComp imagCoeffEx = Operators.DivOp.StaticCombine(new Number(imagNum), new Number(imagDen));

            ExComp imagEx = Operators.MulOp.StaticCombine(imagCoeffEx, new Number(0.0, 1.0));

            ExComp final = Operators.AddOp.StaticCombine(realEx, imagEx);

            return final;
        }

        public static Number operator ^(Number n1, double d)
        {
            return RaiseToPower(n1, d);
        }

        public static Number operator ^(Number n1, Number n2)
        {
            return RaiseToPower(n1, n2);
        }

        public static Number operator +(Number n1, Number n2)
        {
            Number resultant = new Number(n1.RealComp + n2.RealComp, n1.ImagComp + n2.ImagComp);

            resultant.EpsilonCorrect();

            return resultant;
        }

        public static Number operator +(Number n1, double d)
        {
            n1.RealComp += d;
            if (Math.Abs(n1.RealComp) < EPSILON_ACCEPT)
                n1.RealComp = 0.0;
            return n1;
        }

        public static bool operator <(Number n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.RealComp < d;
        }

        public static bool operator <(Number n1, Number n2)
        {
            return (n1.RealComp < n2.RealComp) && (n1.ImagComp <= n2.ImagComp);
        }

        public static bool operator <=(Number n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.RealComp <= d;
        }

        public static bool operator <=(Number n1, Number n2)
        {
            return (n1.RealComp <= n2.RealComp) && (n1.ImagComp <= n2.ImagComp);
        }

        public static bool operator ==(Number n1, double d)
        {
            if (((object)n1) == null)
                return false;

            if (n1.HasImaginaryComp())
                return false;

            n1.RealComp = EpsilonCorrect(n1.RealComp);
            d = EpsilonCorrect(d);

            return n1.RealCompRnd == d;
        }

        public static bool operator ==(Number n1, Number n2)
        {
            if (((object)n1) == null && ((object)n2) == null)
                return true;
            else if (((object)n1) == null || ((object)n2) == null)
                return false;

            return n1.IsEqualTo(n2);
        }

        public static bool operator >(Number n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.RealComp > d;
        }

        public static bool operator >(Number n1, Number n2)
        {
            return (n1.RealComp > n2.RealComp) && (n1.ImagComp >= n2.ImagComp);
        }

        public static bool operator >=(Number n1, Number n2)
        {
            return (n1.RealComp >= n2.RealComp) && (n1.ImagComp >= n2.ImagComp);
        }

        public static bool operator >=(Number n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.RealComp >= d;
        }

        #endregion Operators

        public bool IsNegInfinity()
        {
            return Double.IsNegativeInfinity(d_realComp);
        }

        public bool IsPosInfinity()
        {
            return Double.IsPositiveInfinity(d_realComp);
        }

        public bool IsRealInteger()
        {
            string realStr = d_realComp.ToString();
            return !realStr.Contains(".") && !IsInfinity();
        }

        public bool IsUndefined()
        {
            if (double.IsNaN(d_imagComp) || double.IsNaN(d_realComp))
                return true;
            return false;
        }

        public void Multiply(Number n)
        {
            Number result = this * n;
            AssignTo(result);
        }

        public void Round(int digits)
        {
            d_realComp = Math.Round(d_realComp, digits);
            d_imagComp = Math.Round(d_imagComp, digits);
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return new AlgebraTerm(this);
        }

        public override string ToAsciiString()
        {
            if (IsUndefined())
                return "\\text{Undefined}";
            if (IsNegInfinity())
                return "-oo";
            if (IsPosInfinity())
                return "oo";
            if (HasImaginaryComp())
            {
                string str = "";
                bool useParas = false;
                if (d_realComp != 0.0)
                {
                    useParas = true;
                    str += d_realComp.ToString() + "+";
                }

                if (d_imagComp == -1.0)
                    str += "-";
                else if (d_imagComp != 1.0)
                    str += d_imagComp.ToString();

                str += "i";

                if (useParas)
                    str = str.SurroundWithParas();

                return str;
            }

            return d_realComp.ToString();
        }

        public override string ToJavaScriptString(bool useRad)
        {
            if (HasImaginaryComp())
            {
                return null;
            }
            return d_realComp.ToString();
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            if (d_imagComp != 0.0)
            {
                string str = "N({0} + i{1})";
                return String.Format(str, d_realComp, d_imagComp);
            }

            return "N(" + d_realComp.ToString() + ")";
        }

        public override string ToTexString()
        {
            if (IsUndefined())
                return "Undefined";
            if (IsNegInfinity())
                return "-oo";
            if (IsPosInfinity())
                return "oo";
            if (HasImaginaryComp())
            {
                string str = "";
                bool useParas = false;
                if (d_realComp != 0.0)
                {
                    useParas = true;
                    str += d_realComp.ToString() + "+";
                }

                if (d_imagComp == -1.0)
                    str += "-";
                else if (d_imagComp != 1.0)
                    str += d_imagComp.ToString();

                str += "i";

                if (useParas)
                    str = str.SurroundWithParas();

                return str;
            }

            return d_realComp.ToString();
        }

        public ExComp ToPolarForm(ref TermType.EvalData pEvalData)
        {
            ExComp mag, angle;
            GetPolarData(out mag, out angle, ref pEvalData);

            bool origRadVal = pEvalData.UseRad;
            pEvalData.TmpSetUseRad(true);

            ExComp result = MulOp.StaticCombine(mag,
                SubOp.StaticCombine(new CosFunction(angle), 
                MulOp.StaticCombine(Number.ImagOne, new SinFunction(angle))));

            pEvalData.TmpSetUseRad(origRadVal);

            return result;
        }

        public ExComp ToExponentialForm(ref TermType.EvalData pEvalData)
        {
            ExComp mag, angle;
            GetPolarData(out mag, out angle, ref pEvalData);

            return MulOp.StaticCombine(mag, PowOp.Exp(MulOp.StaticCombine(angle, Number.ImagOne)));
        }
    }
}