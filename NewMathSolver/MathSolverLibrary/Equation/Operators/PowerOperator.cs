using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class PowOp : AgOp
    {
        private const int MAX_BINOM_COMPLEXITY = 20;
        private const int MAX_COMPLEXITY = 1000;

        public static ExComp RaiseNumToNum(ExComp ex1, ExComp ex2)
        {
            Number n1 = ex1 as Number;
            Number n2 = ex2 as Number;

            if (!n1.HasImaginaryComp() && !n2.HasImaginaryComp())
            {
                double dBase = n1.RealComp;
                double dPow = n2.RealComp;

                bool imag = false;

                // Given odd is false, even is not necessarily true as there can be decimals.
                bool isOdd = false; 
                if (dBase < 0.0)
                {
                    double root = 1.0 / dPow;
                    if (root.IsInteger())
                    {
                        int rootInt = (int)root;
                        isOdd = rootInt % 2 != 0;

                        if (rootInt == 2)
                        {
                            // Even root. We got an imaginary number.
                            imag = true;
                            dBase *= -1.0;
                        }
                    }
                }

                bool negPow = false;
                if (dPow < 0.0)
                {
                    dPow *= -1.0;
                    negPow = true;
                }

                double result;
                if (isOdd)
                    result = Math.Pow(Math.Abs(dBase), dPow);
                else
                    result = Math.Pow(dBase, dPow);

                bool isResultInt = result.IsInteger();

                if (isResultInt && isOdd && dBase < 0.0)
                    result *= -1.0;
                else if (!isResultInt)
                    result = Math.Pow(dBase, dPow);

                ExComp resultEx = null;
                if (imag)
                {
                    if (isResultInt)
                    {
                        resultEx = new Number(0.0, result);
                    }
                    else
                    {
                        PowerFunction imagCoeff = new PowerFunction(new Number(dBase), ex2);
                        ExComp imagCoeffEx = imagCoeff.SimplifyRadical();

                        resultEx = MulOp.StaticCombine(new Number(0.0, 1.0), imagCoeffEx);
                    }
                }

                if (isResultInt && resultEx == null)
                    resultEx = new Number(result);

                if (resultEx != null && negPow)
                    resultEx = AlgebraTerm.FromFraction(Number.One, resultEx);

                if (resultEx != null)
                    return resultEx;
            }
            else if (!n2.HasImaginaryComp())
            {
                double dPow = n2.RealComp;
                if (dPow.IsInteger())
                {
                    int iPow = (int)dPow;

                    //IMPROVE:
                    // This could be improved by a lot.
                    Number final = n1.Clone() as Number;
                    for (int i = 1; i < iPow; ++i)
                    {
                        final = final * (Number)n1.Clone();
                    }

                    return final;
                }
            }

            return null;
        }

        public static ExComp RaiseToPower(ExComp term, Number power, ref TermType.EvalData pEvalData)
        {
            if (!power.IsRealInteger())
                return StaticWeakCombine(term, power).ToAlgTerm();

            int powerInt = (int)power.RealComp;

            if (term is AlgebraTerm)
            {
                var groups = (term as AlgebraTerm).GetGroups();
                int groupCount = groups.Count;

                if (groups.Count == 2)
                {
                    if (powerInt > MAX_BINOM_COMPLEXITY)
                        return StaticWeakCombine(term, power).ToAlgTerm();

                    // Use the binomial theorem.
                    AlgebraComp iterVar = new AlgebraComp("$k");
                    ChooseFunction chooseFunc = new ChooseFunction(power, iterVar);
                    var group0 = groups[0].ToAlgTerm();
                    var group1 = groups[1].ToAlgTerm();
                    ExComp overallEx = MulOp.StaticWeakCombine(chooseFunc, PowOp.StaticWeakCombine(group0, SubOp.StaticWeakCombine(power, iterVar)));
                    overallEx = MulOp.StaticWeakCombine(overallEx, PowOp.StaticWeakCombine(group1, iterVar));
                    SumFunction sumFunc = new SumFunction(overallEx, iterVar, Number.Zero, power);

                    return sumFunc.Evaluate(false, ref pEvalData);
                }

                int complexityRating = (int)Math.Pow(groupCount, powerInt);

                if (complexityRating > MAX_COMPLEXITY)
                    return StaticWeakCombine(term, power).ToAlgTerm();
            }

            ExComp acumTerm = term.Clone();
            for (int i = 1; i < powerInt; ++i)
            {
                acumTerm = MulOp.StaticCombine(acumTerm.Clone(), term.Clone());
            }

            if (acumTerm is AlgebraTerm)
            {
                AlgebraTerm agAcumTerm = acumTerm as AlgebraTerm;
                agAcumTerm = agAcumTerm.ApplyOrderOfOperations();
                acumTerm = agAcumTerm.MakeWorkable();
                acumTerm = PowerFunction.FixFraction(acumTerm);
            }

            return acumTerm;
        }

        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveRedundancies();
            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveRedundancies();

            if (ex1 is Functions.Calculus.CalcConstant)
                return ex1;
            else if (ex2 is Functions.Calculus.CalcConstant)
                return ex2;

            if (Number.IsUndef(ex1) || Number.IsUndef(ex2))
                return Number.Undefined;

            if (Number.One.IsEqualTo(ex2))
                return ex1;
            if (Number.Zero.IsEqualTo(ex2))
                return Number.One;

            if (ex1 is Number && ex2 is Number)
            {
                ExComp result = RaiseNumToNum(ex1, ex2);
                if (result != null)
                    return result;
            }
            if (ex2 is Number && !(ex2 as Number).HasImaginaryComp() && (ex2 as Number) < 0.0)
            {
                Number nEx2 = ex2 as Number;
                nEx2 *= -1.0;
                ExComp raised = StaticCombine(ex1, nEx2);
                return AlgebraTerm.FromFraction(Number.One, raised);
            }
            else if (ex1 is PowerFunction)
            {
                PowerFunction powFunc1 = ex1 as PowerFunction;
                ExComp ex1Power = powFunc1.Power;
                powFunc1.Power = MulOp.StaticCombine(ex1Power, ex2);

                ExComp resultant = powFunc1.RemoveRedundancies();

                return resultant;
            }
            else if (ex1 is AlgebraTerm)
            {
                Term.SimpleFraction simpFrac = new Term.SimpleFraction();
                if (simpFrac.HarshInit(ex1 as AlgebraTerm))
                {
                    ExComp num = StaticCombine(simpFrac.NumEx, ex2);
                    ExComp den = StaticCombine(simpFrac.DenEx, ex2);

                    if (!num.IsEqualTo(StaticWeakCombine(simpFrac.NumEx, ex2)) ||
                        !den.IsEqualTo(StaticWeakCombine(simpFrac.DenEx, ex2)))
                    {
                        return AlgebraTerm.FromFraction(num, den);
                    }
                }
            }
            else if (ex1 is Number && ex2 is AlgebraTerm)
            {
                Number n1 = ex1 as Number;
                AlgebraTerm term2 = ex2 as AlgebraTerm;

                AlgebraTerm[] numDen = term2.GetNumDenFrac();

                if (numDen != null)
                {
                    ExComp num = numDen[0].RemoveRedundancies();
                    ExComp den = numDen[1].RemoveRedundancies();

                    if (num is Number && den is Number)
                    {
                        Number numeratorNum = num as Number;
                        Number denominatorNum = den as Number;

                        Number recipDen = denominatorNum.GetReciprocal();

                        if (numeratorNum != 1.0)
                        {
                            ex1 = StaticCombine(n1, numeratorNum);
                            ex2 = AlgebraTerm.FromFraction(Number.One, denominatorNum);
                        }

                        ExComp result = RaiseNumToNum(ex1, recipDen);
                        if (result != null)
                        {
                            if (result is PowerFunction)
                                return (result as PowerFunction).SimplifyRadical();
                            return result;
                        }
                    }
                }
            }

            PowerFunction powFunc = new PowerFunction(ex1, ex2);
            return powFunc.SimplifyRadical();
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            if (ex2 is Number)
            {
                if ((ex2 as Number) == 0.0)
                    return Number.One;
                else if ((ex2 as Number) == 1.0)
                    return ex1;
            }
            return new PowerFunction(ex1, ex2);
        }

        public static ExComp TakeRoot(ExComp ex1, Number root, ref TermType.EvalData pEvalData, bool showWork = false)
        {
            if (Number.Zero.IsEqualTo(ex1))
            {
                // To get multiplicities correct two zeros must be returned.
                return new AlgebraTermArray(Number.Zero.ToAlgTerm(), Number.Zero.ToAlgTerm());
            }

            AlgebraTerm pow = AlgebraTerm.FromFraction(Number.One, root);

            // Using DeMoivre's theorem.
            if (root.IsRealInteger() && ex1 is Number && !root.IsEqualTo(new Number(2.0)))
            {
                Number n1 = ex1 as Number;

                // z^(1/n) = r^(1/n)(cos((θ+2πk)/n) + i*sin((θ+2πk))/n)
                ExComp r, theta;
                n1.GetPolarData(out r, out theta, ref pEvalData);

                AlgebraTerm[] roots = new AlgebraTerm[(int)root.RealComp];

                for (Number k = Number.Zero; k < root; k += 1.0)
                {
                    ExComp p0 = StaticCombine(r, pow);

                    ExComp num = MulOp.StaticCombine(new Number(2.0), k);
                    num = MulOp.StaticCombine(num, Constant.Pi);
                    num = AddOp.StaticCombine(num, theta);
                    ExComp frac = DivOp.StaticCombine(num, root);

                    CosFunction cos = new CosFunction(frac);
                    ExComp p1 = cos.Evaluate(false, ref pEvalData);

                    SinFunction sin = new SinFunction(frac);
                    ExComp p2 = MulOp.StaticCombine(Number.ImagOne, sin.Evaluate(false, ref pEvalData));
                    ExComp final;
                    ExComp add = AddOp.StaticCombine(p1, p2);

                    if (add is AlgebraTerm)
                        add = (add as AlgebraTerm).CompoundFractions();
                    if (Number.One.IsEqualTo(p0))
                    {
                        final = add;
                    }
                    else if (Number.One.IsEqualTo(add))
                    {
                        final = p0;
                    }
                    else
                    {
                        if (add is AlgebraTerm && (add as AlgebraTerm).GroupCount != 1)
                            final = MulOp.StaticWeakCombine(p0, add);
                        else
                            final = MulOp.StaticCombine(p0, add);
                    }

                    if (showWork)
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "root({0})({1})={2}" + WorkMgr.EDM, "Using De Moivre's theorem the root " + WorkMgr.STM + "{2}" + WorkMgr.EDM + " was found.", root, ex1, final);
                    }

                    roots[(int)k.RealComp] = final.ToAlgTerm();
                }

                return new AlgebraTermArray(roots);
            }

            ExComp result = StaticCombine(ex1, pow);

            AlgebraTerm pos = result.Clone().ToAlgTerm();
            if (root.IsEven())
            {
                AlgebraTerm neg = MulOp.Negate(result).ToAlgTerm();

                AlgebraTermArray agTermArray = new AlgebraTermArray(pos, neg);
                return agTermArray;
            }
            else
                return pos;
        }

        public static AlgebraTermArray TakeSqrt(ExComp ex1, ref TermType.EvalData pEvalData)
        {
            return TakeRoot(ex1, new Number(2.0), ref pEvalData) as AlgebraTermArray;
        }

        public static ExComp WeakTakeSqrt(ExComp ex1)
        {
            return StaticWeakCombine(ex1, AlgebraTerm.FromFraction(new Number(1.0), new Number(2.0)));
        }

        public override ExComp Clone()
        {
            return new PowOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override int GetHashCode()
        {
            return (int)((double)"Pow".GetHashCode() * Math.E);
        }

        public override string ToString()
        {
            return "^";
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return StaticWeakCombine(ex1, ex2);
        }
    }
}