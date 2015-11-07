using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class ExNumber : ExComp
    {
        private const double EPSILON_ACCEPT = 1E-7;
        private const int ROUND_COUNT = 9;
        private const int DISP_ROUND = 5;
        private double d_imagComp;
        private double d_realComp;

        public const int FINAL_ROUND_COUNT = 4;

        public static ExNumber GetImagOne()
        {
            return new ExNumber(0.0, 1.0);
        }

        public static ExNumber GetNegInfinity()
        {
            return new ExNumber(double.NegativeInfinity);
        }

        public static ExNumber GetNegOne()
        {
            return new ExNumber(-1.0);
        }

        public static ExNumber GetOne()
        {
            return new ExNumber(1.0);
        }

        public static ExNumber GetPosInfinity()
        {
            return new ExNumber(double.PositiveInfinity);
        }

        public static ExNumber GetUndefined()
        {
            return new ExNumber(double.NaN, double.NaN);
        }

        public static ExNumber GetZero()
        {
            return new ExNumber(0.0);
        }

        public ExNumber GetImag()
        {
            return new ExNumber(d_imagComp);
        }

        public void SetImagComp(double value)
        {
            d_imagComp = value;
        }

        public double GetImagComp()
        {
            return d_imagComp;
        }

        public double GetImagCompRnd()
        {
            return Math.Round(GetImagComp(), ROUND_COUNT);
        }

        public ExNumber GetReal()
        {
            return new ExNumber(d_realComp);
        }

        public void SetRealComp(double value)
        {
            d_realComp = value;
        }

        public double GetRealComp()
        {
            return d_realComp;
        }

        public double GetRealCompRnd()
        {
            return Math.Round(GetRealComp(), ROUND_COUNT);
        }

        public ExNumber()
        {
            d_realComp = 0.0;
            d_imagComp = 0.0;
        }

        public ExNumber(double realComp)
        {
            d_realComp = realComp;
            d_imagComp = 0.0;
        }

        public ExNumber(int realComp)
            : this((double)realComp, 0.0)
        {
        }

        public ExNumber(long realComp)
            : this((double)realComp, 0.0)
        {
        }

        public ExNumber(double realComp, double imagComp)
        {
            d_realComp = realComp;
            d_imagComp = imagComp;
        }

        public static ExNumber Abs(ExNumber n1)
        {
            return new ExNumber(Math.Abs(n1.GetRealComp()), Math.Abs(n1.GetImagComp()));
        }

        public static bool CleanRootExists(ExNumber n1, ExNumber root)
        {
            if (n1.HasImaginaryComp() || root.HasImaginaryComp())
                throw new ArgumentException();

            double result = Math.Pow(n1.GetRealComp(), 1.0 / root.GetRealComp());
            return DoubleHelper.IsInteger(result);
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

        public static ExNumber GCF(List<ExNumber> numbers)
        {
            List<ExNumber> narrowedList = new List<ExNumber>();

            if (numbers.Count == 1)
                return numbers[0];
            if (numbers.Count == 2)
            {
                return GCF(numbers[0], numbers[1]);
            }

            for (int i = 1; i < numbers.Count; ++i)
            {
                ExNumber n1 = numbers[i];
                ExNumber n2 = numbers[i - 1];
                ExNumber gcf = GCF(n1, n2);
                narrowedList.Add(gcf);
            }

            return GCF(narrowedList);
        }

        public static ExNumber GCF(ExNumber n1, ExNumber n2)
        {
            double gcf;
            if (n1.GetRealComp() != 0.0 && n2.GetImagComp() != 0.0)
                gcf = MathHelper.GCFDouble(n1.GetRealComp(), n1.GetImagComp());
            else if (n1.GetRealComp() != 0.0)
                gcf = n1.GetRealComp();
            else
                gcf = n1.GetImagComp();

            if (n2.GetRealComp() != 0.0 && n2.GetImagComp() != 0.0)
            {
                gcf = MathHelper.GCFDouble(gcf, n2.GetRealComp());
                gcf = MathHelper.GCFDouble(gcf, n2.GetImagComp());
            }
            else if (n2.GetRealComp() != 0.0)
                gcf = MathHelper.GCFDouble(gcf, n2.GetRealComp());
            else
                gcf = MathHelper.GCFDouble(gcf, n2.GetImagComp());

            gcf = Math.Abs(gcf);

            if (n1.GetRealComp() == 0.0 && n2.GetRealComp() == 0.0)
                return new ExNumber(0.0, gcf);

            return new ExNumber(gcf);
        }

        public static void GCF_Base(ExNumber n1, ExNumber n2, out ExNumber n1Pow, out ExNumber n2Pow, out ExNumber nBase)
        {
            n1Pow = null;
            n2Pow = null;
            nBase = null;

            if (!n1.IsRealInteger() || !n2.IsRealInteger())
                return;

            int i1 = (int)n1.GetRealComp();
            int i2 = (int)n2.GetRealComp();

            // We have (g = h) we want to change this to (b^x = b^y).

            // 8 - 4 -> 2^3 - 2^2

            int[] divisors = MathHelper.GetCommonDivisors(i1, i2, true);
            List<int> sortedDivisors = ArrayFunc.ToList(divisors);
            sortedDivisors.Sort();

            int minVal = -1;
            foreach (int divisor in sortedDivisors)
            {
                // divisor ^ x = i1
                // log_(divisor)(i1)
                double log1 = Math.Log(i1, divisor);
                double log2 = Math.Log(i2, divisor);

                if (DoubleHelper.IsInteger(log1) && DoubleHelper.IsInteger(log2))
                {
                    minVal = divisor;
                    break;
                }
            }

            if (minVal == -1)
                return;

            nBase = new ExNumber(minVal);
            double dPow1 = Math.Log(i1, minVal);
            double dPow2 = Math.Log(i2, minVal);
            n1Pow = new ExNumber(dPow1);
            n2Pow = new ExNumber(dPow2);
        }

        public static bool IsUndef(ExComp ex)
        {
            if (ex is ExNumber)
            {
                ExNumber nEx = ex as ExNumber;
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
                    foreach (ExComp comp in term.GetSubComps())
                    {
                        if (IsUndef(comp))
                            return true;
                    }
                }
            }

            return false;
        }

        public static ExNumber LCF(ExNumber n1, ExNumber n2)
        {
            double lcfReal = MathHelper.LCFDouble(n1.GetRealComp(), n2.GetRealComp());
            double lcfImag = MathHelper.LCFDouble(n1.GetImagComp(), n2.GetImagComp());
            lcfReal = Math.Abs(lcfReal);
            lcfImag = Math.Abs(lcfImag);

            return new ExNumber(lcfReal, lcfImag);
        }

        public static ExNumber Maximum(ExNumber n1, ExNumber n2)
        {
            if (n1.d_realComp > n2.d_realComp)
                return n1;
            else
                return n2;
        }

        public static ExNumber Minimum(ExNumber n1, ExNumber n2)
        {
            if (n1.d_realComp < n2.d_realComp)
                return n1;
            else
                return n2;
        }

        public static ExNumber Parse(string parseStr)
        {
            if (parseStr.Contains("i"))
            {
                int imagIndex = parseStr.IndexOf('i');
                parseStr = StringFunc.Rm(parseStr, imagIndex, 1);
                if (parseStr == "")
                {
                    return new ExNumber(0, 1.0);
                }
                double imag;
                if (!double.TryParse(parseStr, out imag))
                    return null;
                return new ExNumber(0, imag);
            }

            double real;
            if (!double.TryParse(parseStr, out real))
                return null;
            return new ExNumber(real, 0);
        }

        public static ExNumber RaiseToPower(ExNumber n1, double d)
        {
            if (n1.HasImaginaryComp())
                return null;

            ExNumber resultant = new ExNumber(Math.Pow(n1.GetRealComp(), d), 0.0);
            return resultant;
        }

        public static ExNumber RaiseToPower(ExNumber n1, ExNumber n2)
        {
            if (n1.HasImaginaryComp() || n2.HasImaginaryComp())
                return null;

            return RaiseToPower(n1, n2.GetRealComp());
        }

        public static ExNumber TakeRootOf(ExNumber n1, double root)
        {
            if (n1.HasImaginaryComp())
                throw new ArgumentException();
            ExNumber resultant = new ExNumber(Math.Pow(n1.GetRealComp(), 1.0 / root), 0.0);
            return resultant;
        }

        public static ExNumber TakeRootOf(ExNumber n1, ExNumber root)
        {
            if (n1.HasImaginaryComp() || root.HasImaginaryComp())
                throw new ArgumentException();

            return TakeRootOf(n1, root.GetRealComp());
        }

        public void Add(double realComp)
        {
            Add(realComp, 0.0);
        }

        public void Add(ExNumber n)
        {
            Add(n.GetRealComp(), n.GetImagComp());
        }

        public void Add(double realComp, double imagComp)
        {
            d_realComp += realComp;
            d_imagComp += imagComp;

            EpsilonCorrect();
        }

        public void AssignTo(ExNumber n)
        {
            d_realComp = n.d_realComp;
            d_imagComp = n.d_imagComp;
        }

        public override ExComp CloneEx()
        {
            return new ExNumber(d_realComp, d_imagComp);
        }

        public void ConvertToLowestBase(out ExNumber nBase, out ExNumber nPow)
        {
            nBase = null;
            nPow = null;

            if (!this.IsRealInteger())
                return;

            int n = (int)d_realComp;

            int[] divisors = MathHelper.GetDivisors(n, false, false);
            List<int> sortedDivisors = ArrayFunc.ToList(divisors);
            sortedDivisors.Sort();

            int minVal = -1;
            foreach (int divisor in sortedDivisors)
            {
                // divisor ^ x = i1
                // log_(divisor)(i1)
                double logVal = Math.Log(n, divisor);

                if (DoubleHelper.IsInteger(logVal))
                {
                    minVal = divisor;
                    break;
                }
            }

            if (minVal == -1)
                return;

            nBase = new ExNumber(minVal);
            double dPow = Math.Log(n, minVal);
            nPow = new ExNumber(dPow);
        }

        public void EpsilonCorrect()
        {
            d_realComp = EpsilonCorrect(d_realComp);
            d_imagComp = EpsilonCorrect(d_imagComp);
        }

        public string FinalToDispString()
        {
            if (IsUndefined())
                return "\\text{Undefined}";
            if (IsNegInfinity())
                return "-oo";
            if (IsPosInfinity())
                return "oo";
            double realRounded = Math.Round(d_realComp, DISP_ROUND);
            double imagRounded = Math.Round(d_imagComp, DISP_ROUND);
            if (HasImaginaryComp())
            {
                string str = "";
                if (realRounded != 0.0)
                    str += realRounded.ToString() + "+";

                if (imagRounded == -1.0)
                    str += "-";
                else if (imagRounded != 1.0)
                    str += imagRounded.ToString();

                str += "i";

                return str;
            }

            return realRounded.ToString();
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

        public List<TypePair<ExNumber, ExNumber>> GetDivisors()
        {
            List<TypePair<ExNumber, ExNumber>> factors = new List<TypePair<ExNumber, ExNumber>>();
            if (!IsRealInteger() || HasImaginaryComp())
                return factors;         // Just an empty list.

            int realCompInt = (int)d_realComp;
            realCompInt = Math.Abs(realCompInt);

            for (int i = 1; i <= realCompInt; ++i)
            {
                if (realCompInt % i == 0)
                {
                    TypePair<ExNumber, ExNumber> pair = new TypePair<ExNumber, ExNumber>();
                    int num1 = i;
                    int num2 = realCompInt / i;
                    int highOrder = Math.Max(num1, num2);
                    int lowOrder = Math.Min(num1, num2);
                    pair.SetData1(new ExNumber((double)highOrder));
                    pair.SetData2(new ExNumber((double)lowOrder));
                    factors.Add(pair);
                }
            }

            if (d_realComp < 0)
            {
                List<TypePair<ExNumber, ExNumber>> finalPair = new List<TypePair<ExNumber, ExNumber>>();
                for (int i = 0; i < factors.Count; ++i)
                {
                    TypePair<ExNumber, ExNumber> pair = factors.ElementAt(i);
                    TypePair<ExNumber, ExNumber> newPair1 = new TypePair<ExNumber, ExNumber>();
                    newPair1.SetData1(pair.GetData1());
                    newPair1.SetData2(ExNumber.OpSub(pair.GetData2()));

                    finalPair.Add(newPair1);

                    TypePair<ExNumber, ExNumber> newPair2 = new TypePair<ExNumber, ExNumber>();
                    newPair2.SetData1(ExNumber.OpSub(newPair1.GetData1()));
                    newPair2.SetData2(ExNumber.OpSub(newPair1.GetData2()));

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
        public List<TypePair<ExNumber, ExNumber>> GetDivisorsSignInvariant()
        {
            if (!IsRealInteger() || HasImaginaryComp())
                return new List<TypePair<ExNumber, ExNumber>>(); ;         // Just an empty list.

            List<TypePair<ExNumber, ExNumber>> signInvariantDivisors = GetDivisors();

            if (d_realComp > 0)
            {
                // We have to account for the negative negative situation.
                for (int i = 0; i < signInvariantDivisors.Count; ++i)
                {
                    TypePair<ExNumber, ExNumber> ip = signInvariantDivisors.ElementAt(i);

                    // Make it into a negative negative combination.
                    // We can safely assume both of these numbers are positive as they are the divisors of a positive number.
                    TypePair<ExNumber, ExNumber> newPair = new TypePair<ExNumber, ExNumber>();
                    newPair.SetData1(ExNumber.OpSub(ip.GetData1()));
                    newPair.SetData2(ExNumber.OpSub(ip.GetData2()));

                    signInvariantDivisors.Insert(i, newPair);

                    // Skip over the added entry.
                    ++i;
                }
            }

            return signInvariantDivisors;
        }

        public void GetPolarData(out ExComp mag, out ExComp angle, ref TermType.EvalData pEvalData)
        {
            mag = null;
            angle = null;

            ExNumber real = new ExNumber(Math.Pow(d_realComp, 2.0));
            ExNumber imag = new ExNumber(Math.Pow(d_imagComp, 2.0));
            mag = Operators.PowOp.StaticCombine(ExNumber.OpAdd(real, imag), new ExNumber(0.5));

            if (ExNumber.OpEqual(GetReal(), 0.0) && ExNumber.OpEqual(GetImag(), 0.0))
            {
                angle = ExNumber.GetZero();
                return;
            }
            if (ExNumber.OpEqual(GetReal(), 0.0))
            {
                angle = ExNumber.OpGT(GetImag(), 0.0) ? AlgebraTerm.FromFraction(Constant.GetPi(), new ExNumber(2.0)) :
                    AlgebraTerm.FromFraction(new AlgebraTerm(new ExNumber(3.0), new Operators.MulOp(), Constant.GetPi()), new ExNumber(2.0));
                return;
            }
            if (ExNumber.OpEqual(GetImag(), 0.0))
            {
                angle = ExNumber.OpGT(GetReal(), 0.0) ? (ExComp)ExNumber.GetZero() : (ExComp)Constant.GetPi();
                return;
            }
            ExComp div = Operators.DivOp.StaticCombine(GetImag(), GetReal());
            Functions.ATanFunction tan = new Functions.ATanFunction(div);
            ExComp evaluated = tan.Evaluate(false, ref pEvalData);

            Term.SimpleFraction simpFrac = new Term.SimpleFraction();
            ExNumber num, den;
            if (simpFrac.LooseInit(evaluated.ToAlgTerm()) && simpFrac.IsSimpleUnitCircleAngle(out num, out den, true))
            {
                // Adjusting might have to be done to the angle as atan has a range of [-pi/2, pi/2].

                if (d_imagComp > 0.0 && ExNumber.OpGT(num, den))
                {
                    num = ExNumber.OpSub(num, den);
                }
                else if (d_imagComp < 0.0 && ExNumber.OpGT(den, num))
                {
                    num = ExNumber.OpAdd(num, den);
                }

                angle = AlgebraTerm.FromFraction(new AlgebraTerm(num, new Operators.MulOp(), Constant.GetPi()), den);
            }
            else
                angle = evaluated;
        }

        public ExNumber GetReciprocal()
        {
            double real = d_realComp == 0.0 ? 0.0 : 1.0 / d_realComp;
            double imag = d_imagComp == 0.0 ? 0.0 : 1.0 / d_imagComp;
            return new ExNumber(real, imag);
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
            if (d_realComp == 0.0 && d_imagComp == 0.0 && ex is AlgebraTerm && (ex as AlgebraTerm).GetTermCount() == 0)
                return true;
            if (ex is ExNumber)
            {
                ExNumber number = ex as ExNumber;
                EpsilonCorrect();
                number.EpsilonCorrect();
                return ((this.GetRealCompRnd() == number.GetRealCompRnd()) && (this.GetImagCompRnd() == number.GetImagCompRnd()));
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
            return DoubleFunc.IsInfinity(d_realComp);
        }

        public static ExNumber OpSub(ExNumber n1, ExNumber n2)
        {
            ExNumber resultant = new ExNumber(n1.GetRealComp() - n2.GetRealComp(), n1.GetImagComp() - n2.GetImagComp());

            resultant.EpsilonCorrect();

            return resultant;
        }

        public static ExNumber OpSub(ExNumber n1)
        {
            return new ExNumber(-n1.GetRealComp(), -n1.GetImagComp());
        }

        public static ExNumber OpSub(ExNumber n1, double d)
        {
            n1.SetRealComp(n1.GetRealComp() - d);
            if (Math.Abs(n1.GetRealComp()) < EPSILON_ACCEPT)
                n1.SetRealComp(0.0);
            return n1;
        }

        public static bool OpNotEquals(ExNumber n1, double d)
        {
            return !(ExNumber.OpEqual(n1, d));
        }

        public static bool OpNotEquals(ExNumber n1, ExNumber n2)
        {
            return !(ExNumber.OpEqual(n1, n2));
        }

        public static ExNumber OpMod(ExNumber n1, ExNumber n2)
        {
            if (!n1.IsRealInteger() || !n2.IsRealInteger())
                return null;

            int realInt1 = (int)n1.GetRealComp();
            int realInt2 = (int)n2.GetRealComp();

            int realResult = realInt1 % realInt2;

            return new ExNumber((double)realResult);
        }

        public static ExNumber OpMod(ExNumber n1, int n2)
        {
            if (!n1.IsRealInteger())
                return null;

            int realInt1 = (int)n1.GetRealComp();

            int realResult = realInt1 % n2;

            return new ExNumber((double)realResult);
        }

        public static ExNumber OpMul(ExNumber n1, ExNumber n2)
        {
            double real = (n1.GetRealComp() * n2.GetRealComp()) - (n1.GetImagComp() * n2.GetImagComp());
            double imag = (n1.GetRealComp() * n2.GetImagComp()) + (n1.GetImagComp() * n2.GetRealComp());
            if (n1.IsInfinity() || n2.IsInfinity())
                imag = 0.0;

            real = EpsilonCorrect(real);
            imag = EpsilonCorrect(imag);

            if (imag == 0.0 && n1.IsPosInfinity())
                return ExNumber.OpLT(n2, 0.0) ? GetNegInfinity() : GetPosInfinity();
            else if (imag == 0.0 && n2.IsPosInfinity())
                return ExNumber.OpLT(n1, 0.0) ? GetNegInfinity() : GetPosInfinity();

            return new ExNumber(real, imag);
        }

        public static ExNumber OpMul(ExNumber n1, double d)
        {
            return ExNumber.OpMul(n1, new ExNumber(d));
        }

        public static ExComp OpDiv(ExNumber n1, ExNumber n2)
        {
            if (!n1.HasImaginaryComp() && !n2.HasImaginaryComp())
            {
                ExNumber resultant = new ExNumber(n1.GetRealComp() / n2.GetRealComp());
                resultant.EpsilonCorrect();
                return resultant;
            }

            double realNum = ((n1.GetRealComp() * n2.GetRealComp()) + (n1.GetImagComp() * n2.GetImagComp()));
            double realDen = ((n2.GetRealComp() * n2.GetRealComp()) + (n2.GetImagComp() * n2.GetImagComp()));

            double imagNum = ((n1.GetImagComp() * n2.GetRealComp()) - (n1.GetRealComp() * n2.GetImagComp()));
            double imagDen = ((n2.GetRealComp() * n2.GetRealComp()) + (n2.GetImagComp() * n2.GetImagComp()));

            ExComp realEx = Operators.DivOp.StaticCombine(new ExNumber(realNum), new ExNumber(realDen));
            ExComp imagCoeffEx = Operators.DivOp.StaticCombine(new ExNumber(imagNum), new ExNumber(imagDen));

            ExComp imagEx = Operators.MulOp.StaticCombine(imagCoeffEx, new ExNumber(0.0, 1.0));

            ExComp finalCombined = Operators.AddOp.StaticCombine(realEx, imagEx);

            return finalCombined;
        }

        public static ExNumber OpPow(ExNumber n1, double d)
        {
            return RaiseToPower(n1, d);
        }

        public static ExNumber OpPow(ExNumber n1, ExNumber n2)
        {
            return RaiseToPower(n1, n2);
        }

        public static ExNumber OpAdd(ExNumber n1, ExNumber n2)
        {
            ExNumber resultant = new ExNumber(n1.GetRealComp() + n2.GetRealComp(), n1.GetImagComp() + n2.GetImagComp());

            resultant.EpsilonCorrect();

            return resultant;
        }

        public static ExNumber OpAdd(ExNumber n1, double d)
        {
            n1.SetRealComp(n1.GetRealComp() + d);
            if (Math.Abs(n1.GetRealComp()) < EPSILON_ACCEPT)
                n1.SetRealComp(0.0);
            return n1;
        }

        public static bool OpLT(ExNumber n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.GetRealComp() < d;
        }

        public static bool OpLT(ExNumber n1, ExNumber n2)
        {
            return (n1.GetRealComp() < n2.GetRealComp()) && (n1.GetImagComp() <= n2.GetImagComp());
        }

        public static bool OpLE(ExNumber n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.GetRealComp() <= d;
        }

        public static bool OpLE(ExNumber n1, ExNumber n2)
        {
            return (n1.GetRealComp() <= n2.GetRealComp()) && (n1.GetImagComp() <= n2.GetImagComp());
        }

        public static bool OpEqual(ExNumber n1, double d)
        {
            if (((object)n1) == null)
                return false;

            if (n1.HasImaginaryComp())
                return false;

            n1.SetRealComp(EpsilonCorrect(n1.GetRealComp()));
            d = EpsilonCorrect(d);

            return n1.GetRealCompRnd() == d;
        }

        public static bool OpEqual(ExNumber n1, ExNumber n2)
        {
            if (((object)n1) == null && ((object)n2) == null)
                return true;
            else if (((object)n1) == null || ((object)n2) == null)
                return false;

            return n1.IsEqualTo(n2);
        }

        public static bool OpGT(ExNumber n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.GetRealComp() > d;
        }

        public static bool OpGT(ExNumber n1, ExNumber n2)
        {
            return (n1.GetRealComp() > n2.GetRealComp()) && (n1.GetImagComp() >= n2.GetImagComp());
        }

        public static bool OpGE(ExNumber n1, ExNumber n2)
        {
            return (n1.GetRealComp() >= n2.GetRealComp()) && (n1.GetImagComp() >= n2.GetImagComp());
        }

        public static bool OpGE(ExNumber n1, double d)
        {
            if (n1.HasImaginaryComp())
                return false;

            return n1.GetRealComp() >= d;
        }

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

        public void Multiply(ExNumber n)
        {
            ExNumber result = ExNumber.OpMul(this, n);
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
            double realRounded = Math.Round(d_realComp, DISP_ROUND);
            double imagRounded = Math.Round(d_imagComp, DISP_ROUND);
            if (HasImaginaryComp())
            {
                string str = "";
                bool useParas = false;
                if (realRounded != 0.0)
                {
                    useParas = true;
                    str += realRounded.ToString() + "+";
                }

                if (imagRounded == -1.0)
                    str += "-";
                else if (imagRounded != 1.0)
                    str += imagRounded.ToString();

                str += "i";

                if (useParas)
                    str = StringHelper.SurroundWithParas(str);

                return str;
            }

            return realRounded.ToString();
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
            return ToTexString();
        }

        public override string ToTexString()
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
                    str = StringHelper.SurroundWithParas(str);

                return str;
            }

            return d_realComp.ToString();
        }

        public ExComp ToPolarForm(ref MathSolverLibrary.TermType.EvalData pEvalData)
        {
            ExComp mag, angle;
            GetPolarData(out mag, out angle, ref pEvalData);

            bool origRadVal = pEvalData.GetUseRad();
            pEvalData.TmpSetUseRad(true);

            ExComp result = MulOp.StaticWeakCombine(mag,
                SubOp.StaticCombine(new CosFunction(angle),
                MulOp.StaticCombine(ExNumber.GetImagOne(), new SinFunction(angle))));

            pEvalData.TmpSetUseRad(origRadVal);

            return result;
        }

        public ExComp ToExponentialForm(ref TermType.EvalData pEvalData)
        {
            ExComp mag, angle;
            GetPolarData(out mag, out angle, ref pEvalData);

            return MulOp.StaticCombine(mag, PowOp.Exp(MulOp.StaticCombine(angle, ExNumber.GetImagOne())));
        }
    }
}