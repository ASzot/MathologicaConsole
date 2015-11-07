using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
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
            if (ExNumber.GetNegInfinity().IsEqualTo(ex1))
                return ExNumber.GetPosInfinity();
            else if (ExNumber.GetPosInfinity().IsEqualTo(ex1))
                return ExNumber.GetNegInfinity();

            return StaticCombine(ExNumber.GetNegOne(), ex1);
        }

        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveRedundancies(false);
            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveRedundancies(false);

            if (ex1 is Functions.Calculus.CalcConstant)
                return ex1;
            else if (ex2 is Functions.Calculus.CalcConstant)
                return ex2;

            if (ex2 is PowerFunction && ExNumber.GetZero().IsEqualTo((ex2 as PowerFunction).GetBase()) &&
                ExNumber.GetNegOne().IsEqualTo((ex2 as PowerFunction).GetPower()))
                return ExNumber.GetUndefined();

            if (ex1 is PowerFunction && ExNumber.GetZero().IsEqualTo((ex1 as PowerFunction).GetBase()) &&
                ExNumber.GetNegOne().IsEqualTo((ex1 as PowerFunction).GetPower()))
                return ExNumber.GetUndefined();

            if (ExNumber.GetZero().IsEqualTo(ex1) || ExNumber.GetZero().IsEqualTo(ex2))
                return ExNumber.GetZero();

            if (ex1 is PowerFunction && (ex1 as PowerFunction).IsDenominator() && !ExNumber.GetNegOne().IsEqualTo((ex1 as PowerFunction).GetPower()))
            {
                (ex1 as PowerFunction).SetPower(DivOp.StaticCombine((ex1 as PowerFunction).GetPower(), ExNumber.GetNegOne()));
                ex1 = new PowerFunction(ex1, ExNumber.GetNegOne());
            }

            if (ex2 is PowerFunction && (ex2 as PowerFunction).IsDenominator() && !ExNumber.GetNegOne().IsEqualTo((ex2 as PowerFunction).GetPower()))
            {
                (ex2 as PowerFunction).SetPower(DivOp.StaticCombine((ex2 as PowerFunction).GetPower(), ExNumber.GetNegOne()));
                ex2 = new PowerFunction(ex2, ExNumber.GetNegOne());
            }

            if (ex1 is ExMatrix || ex2 is ExMatrix)
            {
                ExMatrix mat;
                ExComp other;
                if (ex1 is ExMatrix)
                {
                    mat = ex1 as ExMatrix;
                    other = ex2;
                }
                else
                {
                    mat = ex2 as ExMatrix;
                    other = ex1;
                }

                ExComp atmpt = MatrixHelper.MulOpCombine(mat, other);
                if (atmpt != null)
                    return atmpt;

                return MulOp.StaticWeakCombine(ex1, ex2);
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

                ExComp resultant = AlgebraFunction.OpMul(func1, func2);
                return resultant;
            }
            else if ((ex1 is AlgebraFunction && ex2 is AlgebraComp) ||
                (ex1 is AlgebraComp && ex2 is AlgebraFunction))
            {
                AlgebraFunction func = ex1 is AlgebraFunction ? ex1 as AlgebraFunction : ex2 as AlgebraFunction;
                AlgebraComp comp = ex2 is AlgebraComp ? ex2 as AlgebraComp : ex1 as AlgebraComp;

                ExComp resultant = AlgebraFunction.OpMul(func, comp);
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

                ExComp resultant = AlgebraFunction.OpMul(func, term);
                return resultant;
            }
            else if (ex1 is AlgebraTerm && ex2 is AlgebraTerm)
            {
                AlgebraTerm term1 = ex1 as AlgebraTerm;
                AlgebraTerm term2 = ex2 as AlgebraTerm;

                AlgebraTerm[] numDenTerm1 = term1.GetNumDenFrac();
                AlgebraTerm[] numDenTerm2 = term2.GetNumDenFrac();

                if (numDenTerm1 != null || numDenTerm2 != null)
                {
                    ExComp numTerm1 = numDenTerm1 != null ? numDenTerm1[0] : term1;
                    ExComp numTerm2 = numDenTerm2 != null ? numDenTerm2[0] : term2;

                    ExComp denTerm1 = numDenTerm1 != null ? (numDenTerm1[1] as ExComp) : (ExNumber.GetOne() as ExComp);
                    ExComp denTerm2 = numDenTerm2 != null ? (numDenTerm2[1] as ExComp) : (ExNumber.GetOne() as ExComp);

                    if (numTerm1 is AlgebraTerm)
                        numTerm1 = (numTerm1 as AlgebraTerm).RemoveRedundancies(false);
                    if (numTerm2 is AlgebraTerm)
                        numTerm2 = (numTerm2 as AlgebraTerm).RemoveRedundancies(false);
                    if (denTerm1 is AlgebraTerm)
                        denTerm1 = (denTerm1 as AlgebraTerm).RemoveRedundancies(false);
                    if (denTerm2 is AlgebraTerm)
                        denTerm2 = (denTerm2 as AlgebraTerm).RemoveRedundancies(false);

                    if (numTerm2.IsEqualTo(denTerm1))
                        return DivOp.StaticCombine(numTerm1, denTerm2);

                    if (numTerm1.IsEqualTo(denTerm2))
                        return DivOp.StaticCombine(numTerm2, denTerm1);

                    ExComp combinedNum = StaticCombine(numTerm1, numTerm2);
                    ExComp combinedDen = StaticCombine(denTerm1, denTerm2);

                    ExComp finalFrac = DivOp.StaticCombine(combinedNum, combinedDen);

                    return finalFrac;
                }

                List<ExComp[]> groups1 = term1.GetGroupsNoOps();
                List<ExComp[]> groups2 = term2.GetGroupsNoOps();

                int groups1Count = groups1.Count;
                int groups2Count = groups2.Count;

                if (groups1Count > 1 && groups2Count > 1 && groups1Count * groups2Count > MAX_MUL_COMPLEX)
                {
                    return StaticWeakCombine(ex1, ex2);
                }

                List<ExComp[]> combinedGroups = new List<ExComp[]>();
                for (int i = 0; i < groups1.Count; ++i)
                {
                    ExComp[] group1 = groups1[i];

                    for (int j = 0; j < groups2.Count; ++j)
                    {
                        ExComp[] group2 = groups2[j];

                        ExComp[] combination = AlgebraTerm.MultiplyGroups(GroupHelper.CloneGroup(group1), GroupHelper.CloneGroup(group2));
                        combinedGroups.Add(combination);
                    }
                }
                AlgebraTerm term = new AlgebraTerm();
                foreach (ExComp[] group in combinedGroups)
                    term.AddGroup(group);

                term.CombineLikeTerms();

                return term;
            }
            else if ((ex1 is AlgebraTerm && ex2 is AlgebraComp) ||
                (ex1 is AlgebraComp && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                AlgebraComp comp = ex1 is AlgebraComp ? ex1 as AlgebraComp : ex2 as AlgebraComp;

                AlgebraTerm[] numDen = term.GetNumDenFrac();
                if (numDen != null && ExNumber.GetOne().IsEqualTo(numDen[0].RemoveRedundancies(false)) && comp.IsEqualTo(numDen[1].RemoveRedundancies(false)))
                {
                    return ExNumber.GetOne();
                }
                else if (numDen != null)
                {
                    ExComp num = MulOp.StaticCombine(comp, numDen[0]);
                    return DivOp.StaticCombine(num, numDen[1]);
                }

                List<ExComp[]> groups = term.PopGroups();
                List<ExComp[]> combinedGroups = new List<ExComp[]>();
                for (int i = 0; i < groups.Count; ++i)
                {
                    ExComp[] group = groups[i];
                    bool matchingFound = false;
                    for (int j = 0; j < group.Length; ++j)
                    {
                        ExComp groupComp = group[j];

                        bool matchFound = GroupHelper.CompsRelatable(groupComp, comp);

                        if (matchFound)
                        {
                            matchingFound = true;
                            group[j] = StaticCombine(groupComp, comp);
                            break;
                        }
                    }

                    if (!matchingFound)
                    {
                        AlgebraTerm.AddTermToGroup(ref group, comp, true);
                    }

                    combinedGroups.Add(group);
                }

                term = term.PushGroups(combinedGroups);

                return term;
            }
            else if ((ex1 is AlgebraTerm && ex2 is ExNumber) ||
                (ex1 is ExNumber && ex2 is AlgebraTerm))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;

                ExNumber num = ex1 is ExNumber ? ex1 as ExNumber : ex2 as ExNumber;
                if (term is Functions.PowerFunction && ex2 is AlgebraTerm)
                {
                    Functions.PowerFunction pfTmpTerm = term as Functions.PowerFunction;
                    if (ExNumber.GetNegOne().IsEqualTo(pfTmpTerm.GetPower()))
                    {
                        return DivOp.StaticCombine(num, pfTmpTerm.GetBase());
                    }
                }

                if (ExNumber.OpEqual(num, 1.0))
                    return term;

                List<ExComp[]> groups = term.GetGroupsNoOps();
                List<ExComp[]> combinedGroups = new List<ExComp[]>();
                for (int i = 0; i < groups.Count; ++i)
                {
                    ExComp[] group = groups[i];
                    if (group.Length == 1 && group[0] is Functions.Calculus.CalcConstant)
                    {
                        combinedGroups.Add(groups[i]);
                        break;
                    }
                    ExComp[] groupToAdd = AlgebraTerm.RemoveCoeffs(group);
                    ExNumber coeff = AlgebraTerm.GetCoeffTerm(group);
                    if (coeff == null)
                        coeff = new ExNumber(1.0);

                    ExNumber newCoeff = ExNumber.OpMul(coeff, num);
                    AlgebraTerm.AddTermToGroup(ref groupToAdd, newCoeff, false);

                    combinedGroups.Add(groupToAdd);
                }

                AlgebraTerm finalTerm = new AlgebraTerm(combinedGroups.ToArray());
                finalTerm = finalTerm.Order();

                return finalTerm.ReduceFracs();
            }
            else if ((ex1 is AlgebraComp && ex2 is ExNumber) ||
                (ex1 is ExNumber && ex2 is AlgebraComp))
            {
                AlgebraComp comp = ex1 is AlgebraComp ? ex1 as AlgebraComp : ex2 as AlgebraComp;
                ExNumber number = ex1 is ExNumber ? ex1 as ExNumber : ex2 as ExNumber;

                if (ExNumber.OpEqual(number, 1.0))
                    return comp;

                if (ExNumber.OpEqual(number, 1.0))
                    return comp;

                AlgebraTerm term = new AlgebraTerm();
                term.Add(number, new Operators.MulOp(), comp);
                return term;
            }
            else if (ex1 is ExNumber && ex2 is ExNumber)
            {
                ExNumber n1 = ex1 as ExNumber;
                ExNumber n2 = ex2 as ExNumber;

                ExNumber result = ExNumber.OpMul(n1, n2);
                return result;
            }
            else if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp c1 = ex1 as AlgebraComp;
                AlgebraComp c2 = ex2 as AlgebraComp;

                if (c1.IsEqualTo(c2))
                {
                    Functions.PowerFunction powFunc = new Functions.PowerFunction(c1, new ExNumber(2.0));
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

        public override ExComp CloneEx()
        {
            return new MulOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
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