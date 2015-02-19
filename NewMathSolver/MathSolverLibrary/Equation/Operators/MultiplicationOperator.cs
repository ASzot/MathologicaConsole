using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class MulOp : AgOp
    {
        private const int MAX_MUL_COMPLEX = 100;

        public static ExComp Negate(ExComp ex1)
        {
            if (Number.NegInfinity.IsEqualTo(ex1))
                return Number.PosInfinity;
            else if (Number.PosInfinity.IsEqualTo(ex1))
                return Number.NegInfinity;

            return StaticCombine(Number.NegOne, ex1);
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

            if (ex2 is PowerFunction && Number.Zero.IsEqualTo((ex2 as PowerFunction).Base) &&
                Number.NegOne.IsEqualTo((ex2 as PowerFunction).Power))
                return Number.Undefined;

            if (Number.Zero.IsEqualTo(ex1) || Number.Zero.IsEqualTo(ex2))
                return Number.Zero;

            if (ex1 is PowerFunction && (ex1 as PowerFunction).IsDenominator() && !Number.NegOne.IsEqualTo((ex1 as PowerFunction).Power))
            {
                (ex1 as PowerFunction).Power = DivOp.StaticCombine((ex1 as PowerFunction).Power, Number.NegOne);
                ex1 = new PowerFunction(ex1, Number.NegOne);
            }

            if (ex2 is PowerFunction && (ex2 as PowerFunction).IsDenominator() && !Number.NegOne.IsEqualTo((ex2 as PowerFunction).Power))
            {
                (ex2 as PowerFunction).Power = DivOp.StaticCombine((ex2 as PowerFunction).Power, Number.NegOne);
                ex2 = new PowerFunction(ex2, Number.NegOne);
            }

            if (ex1 is AlgebraFunction && ex2 is AlgebraFunction)
            {
                AlgebraFunction func1 = ex1 as AlgebraFunction;
                AlgebraFunction func2 = ex2 as AlgebraFunction;

                if (func1 is Functions.PowerFunction && func1 is Functions.PowerFunction &&
                    (func1 as Functions.PowerFunction).IsDenominator())
                {
                    ExComp divBy = (func1 as Functions.PowerFunction).FlipFrac();

                    return DivOp.StaticCombine(func2, divBy);
                }
                else if (func2 is Functions.PowerFunction && func2 is Functions.PowerFunction &&
                    (func2 as Functions.PowerFunction).IsDenominator())
                {
                    ExComp divBy = (func2 as Functions.PowerFunction).FlipFrac();

                    return DivOp.StaticCombine(func1, divBy);
                }

                ExComp resultant = func1 * func2;
                return resultant;
            }
            else if ((ex1 is AlgebraFunction && ex2 is AlgebraComp) ||
                (ex1 is AlgebraComp && ex2 is AlgebraFunction))
            {
                AlgebraFunction func = ex1 is AlgebraFunction ? ex1 as AlgebraFunction : ex2 as AlgebraFunction;
                AlgebraComp comp = ex2 is AlgebraComp ? ex2 as AlgebraComp : ex1 as AlgebraComp;

                ExComp resultant = func * comp;
                return resultant;
            }
            else if ((ex1 is AlgebraFunction && ex2 is AlgebraTerm) ||
                (ex1 is AlgebraTerm && ex2 is AlgebraFunction))
            {
                AlgebraFunction func = ex1 is AlgebraFunction ? ex1 as AlgebraFunction : ex2 as AlgebraFunction;
                AlgebraTerm term = (ex2 is AlgebraTerm && !(ex2 is AlgebraFunction)) ? ex2 as AlgebraTerm : ex1 as AlgebraTerm;

                if (func is Functions.PowerFunction && (func as Functions.PowerFunction).IsDenominator())
                {
                    ExComp flipped = (func as Functions.PowerFunction).FlipFrac();
                    return DivOp.StaticCombine(term, flipped);
                }

                ExComp resultant = func * term;
                return resultant;
            }
            else if (ex1 is AlgebraTerm && ex2 is AlgebraTerm)
            {
                AlgebraTerm term1 = ex1 as AlgebraTerm;
                AlgebraTerm term2 = ex2 as AlgebraTerm;

                var numDenTerm1 = term1.GetNumDenFrac();
                var numDenTerm2 = term2.GetNumDenFrac();

                if (numDenTerm1 != null || numDenTerm2 != null)
                {
                    ExComp numTerm1 = numDenTerm1 != null ? numDenTerm1[0] : term1;
                    ExComp numTerm2 = numDenTerm2 != null ? numDenTerm2[0] : term2;

                    ExComp denTerm1 = numDenTerm1 != null ? (numDenTerm1[1] as ExComp) : (Number.One as ExComp);
                    ExComp denTerm2 = numDenTerm2 != null ? (numDenTerm2[1] as ExComp) : (Number.One as ExComp);

                    if (numTerm1 is AlgebraTerm)
                        numTerm1 = (numTerm1 as AlgebraTerm).RemoveRedundancies();
                    if (numTerm2 is AlgebraTerm)
                        numTerm2 = (numTerm2 as AlgebraTerm).RemoveRedundancies();
                    if (denTerm1 is AlgebraTerm)
                        denTerm1 = (denTerm1 as AlgebraTerm).RemoveRedundancies();
                    if (denTerm2 is AlgebraTerm)
                        denTerm2 = (denTerm2 as AlgebraTerm).RemoveRedundancies();

                    if (numTerm2.IsEqualTo(denTerm1))
                        return DivOp.StaticCombine(numTerm1, denTerm2);

                    if (numTerm1.IsEqualTo(denTerm2))
                        return DivOp.StaticCombine(numTerm2, denTerm1);

                    ExComp combinedNum = StaticCombine(numTerm1, numTerm2);
                    ExComp combinedDen = StaticCombine(denTerm1, denTerm2);

                    ExComp finalFrac = DivOp.StaticCombine(combinedNum, combinedDen);

                    return finalFrac;
                }

                var groups1 = term1.GetGroupsNoOps();
                var groups2 = term2.GetGroupsNoOps();

                int groups1Count = groups1.Count;
                int groups2Count = groups2.Count;

                if (groups1Count > 1 && groups2Count > 1 && groups1Count * groups2Count > MAX_MUL_COMPLEX)
                {
                    return StaticWeakCombine(ex1, ex2);
                }

                List<ExComp[]> combinedGroups = new List<ExComp[]>();
                for (int i = 0; i < groups1.Count; ++i)
                {
                    var group1 = groups1[i];

                    for (int j = 0; j < groups2.Count; ++j)
                    {
                        var group2 = groups2[j];

                        ExComp[] combination = AlgebraTerm.MultiplyGroups(group1.CloneGroup(), group2.CloneGroup());
                        combinedGroups.Add(combination);
                    }
                }
                AlgebraTerm term = new AlgebraTerm();
                foreach (var group in combinedGroups)
                    term.AddGroup(group);

                term.CombineLikeTerms();

                return term;
            }
            else if ((ex1 is AlgebraTerm && ex2 is AlgebraComp) ||
                (ex1 is AlgebraComp && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                AlgebraComp comp = ex1 is AlgebraComp ? ex1 as AlgebraComp : ex2 as AlgebraComp;

                var numDen = term.GetNumDenFrac();
                if (numDen != null && Number.One.IsEqualTo(numDen[0].RemoveRedundancies()) && comp.IsEqualTo(numDen[1].RemoveRedundancies()))
                {
                    return Number.One;
                }

                var groups = term.PopGroups();
                List<ExComp[]> combinedGroups = new List<ExComp[]>();
                for (int i = 0; i < groups.Count; ++i)
                {
                    var group = groups[i];
                    bool matchingFound = false;
                    for (int j = 0; j < group.Count(); ++j)
                    {
                        var groupComp = group[j];

                        if (GroupHelper.CompsRelatable(groupComp, comp))
                        {
                            matchingFound = true;
                            group[j] = StaticCombine(groupComp, comp);
                            break;
                        }
                    }

                    if (!matchingFound)
                    {
                        AlgebraTerm.AddTermToGroup(ref group, comp);
                    }

                    combinedGroups.Add(group);
                }

                term = term.PushGroups(combinedGroups);

                return term;
            }
            else if ((ex1 is AlgebraTerm && ex2 is Number) ||
                (ex1 is Number && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;

                Number num = ex1 is Number ? ex1 as Number : ex2 as Number;
                if (term is Functions.PowerFunction && ex2 is AlgebraTerm)
                {
                    Functions.PowerFunction pfTmpTerm = term as Functions.PowerFunction;
                    if (Number.NegOne.IsEqualTo(pfTmpTerm.Power))
                    {
                        return DivOp.StaticCombine(num, pfTmpTerm.Base);
                    }
                }

                if (num == 1.0)
                    return term;

                List<ExComp[]> groups = term.GetGroupsNoOps();
                List<ExComp[]> combinedGroups = new List<ExComp[]>();
                for (int i = 0; i < groups.Count; ++i)
                {
                    var group = groups[i];
                    if (group.Length == 1 && group[0] is Functions.Calculus.CalcConstant)
                    {
                        combinedGroups.Add(groups[i]);
                        break;
                    }
                    var groupToAdd = AlgebraTerm.RemoveCoeffs(group);
                    Number coeff = AlgebraTerm.GetCoeffTerm(group);
                    if (coeff == null)
                        coeff = new Number(1.0);

                    Number newCoeff = coeff * num;
                    AlgebraTerm.AddTermToGroup(ref groupToAdd, newCoeff, false);

                    combinedGroups.Add(groupToAdd);
                }

                AlgebraTerm finalTerm = new AlgebraTerm(combinedGroups.ToArray());
                finalTerm = finalTerm.Order();

                return finalTerm.ReduceFracs();
            }
            else if ((ex1 is AlgebraComp && ex2 is Number) ||
                (ex1 is Number && ex2 is AlgebraComp))
            {
                AlgebraComp comp = ex1 is AlgebraComp ? ex1 as AlgebraComp : ex2 as AlgebraComp;
                Number number = ex1 is Number ? ex1 as Number : ex2 as Number;

                if (number == 1.0)
                    return comp;

                if (number == 1.0)
                    return comp;

                AlgebraTerm term = new AlgebraTerm();
                term.Add(number, new Operators.MulOp(), comp);
                return term;
            }
            else if (ex1 is Number && ex2 is Number)
            {
                Number n1 = ex1 as Number;
                Number n2 = ex2 as Number;

                Number result = n1 * n2;
                return result;
            }
            else if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp c1 = ex1 as AlgebraComp;
                AlgebraComp c2 = ex2 as AlgebraComp;

                if (c1 == c2)
                {
                    Functions.PowerFunction powFunc = new Functions.PowerFunction(c1, new Number(2.0));
                    return powFunc;
                }
                else
                {
                    AlgebraTerm term = new AlgebraTerm();
                    term.Add(c1, new MulOp(), c2);
                    return term;
                }
            }
            else if (ex1 is FunctionDefinition || ex2 is FunctionDefinition)
                return StaticWeakCombine(ex1, ex2);

            throw new ArgumentException();
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            return new AlgebraTerm(ex1, new MulOp(), ex2);
        }

        public override ExComp Clone()
        {
            return new MulOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override int GetHashCode()
        {
            return (int)((double)"Mul".GetHashCode() * Math.E);
        }

        public override string ToString()
        {
            return "*";
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return StaticWeakCombine(ex1, ex2);
        }
    }
}