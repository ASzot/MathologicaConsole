using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    partial class AlgebraTerm
    {
        public static bool GroupsCombinable(ExComp[] group1, ExComp[] group2)
        {
            List<ExComp> compareGroup1 = GetCombinableGroup(group1, false);
            List<ExComp> compareGroup2 = GetCombinableGroup(group2, false);

            // For this the coefficients don't matter.
            List<TypePair<bool, ExComp>> checkGroups2 = new List<TypePair<bool, ExComp>>();

            for (int i = 0; i < compareGroup2.Count; ++i)
                checkGroups2.Add(new TypePair<bool, ExComp>(false, compareGroup2[i]));

            if (compareGroup1.Count!= compareGroup2.Count)
                return false;

            foreach (ExComp comp in compareGroup1)
            {
                for (int j = 0; j < checkGroups2.Count; ++j)
                {
                    TypePair<bool, ExComp> checkGroup = checkGroups2.ElementAt(j);
                    if (checkGroup.GetData1())
                        continue;
                    if (checkGroup.GetData2().GetType() == comp.GetType())
                    {
                        if (checkGroup.GetData2().IsEqualTo(comp))
                            checkGroups2[j].SetData1(true);
                    }
                }
            }

            foreach (TypePair<bool, ExComp> checkGroup in checkGroups2)
            {
                if (!checkGroup.GetData1())
                    return false;
            }

            return true;
        }

        public static ExComp Intersect(AlgebraTerm term, ExComp comp)
        {
            if (comp is ExNumber)
                return Intersect(term, comp as ExNumber);
            else if (comp is AlgebraComp)
                return Intersect(term, comp as AlgebraComp);
            else if (comp is AlgebraTerm)
                return Intersect(term, comp as AlgebraTerm);
            else
                throw new ArgumentException("Couldn't cast ExComp!");
        }

        public static ExComp Intersect(AlgebraTerm term, ExNumber number)
        {
            ExComp[] group = new ExComp[] { number };

            ExComp[] matchingGroup = term.GetMatchingGroup(group);
            if (matchingGroup == null)
            {
                return AddOp.StaticWeakCombine(term, number);
            }
            term.RemoveGroup(matchingGroup);

            // Combine the groups.
            ExNumber coeff = GetCoeffTerm(matchingGroup);
            if (coeff == null)
            {
                coeff = new ExNumber(1.0);
                int matchingGroupCount = matchingGroup.Length;
                ExComp[] tmpGp = new ExComp[matchingGroupCount + 2];
                for (int i = 0; i < matchingGroupCount + 2; ++i)
                {
                    tmpGp[i] = matchingGroup[i];
                }
                tmpGp[matchingGroupCount] = new Operators.MulOp();
                tmpGp[matchingGroupCount + 1] = coeff;

                matchingGroup = tmpGp;
            }

            coeff.Add(number);

            if (ExNumber.OpNotEquals(coeff, 0.0))
                term.AddGroup(matchingGroup);
            return term;
        }

        public static ExComp Intersect(AlgebraTerm term, AlgebraComp comp)
        {
            ExComp[] group = new ExComp[] { comp };

            ExComp[] matchingGroup = term.GetMatchingGroup(group);
            if (matchingGroup == null)
            {
                term.Add(new Operators.AddOp());
                term.Add(comp);
                return term;
            }

            if (comp is Functions.Calculus.CalcConstant && matchingGroup.Length == 1 && matchingGroup[0] is Functions.Calculus.CalcConstant)
                return term;

            term.RemoveGroup(matchingGroup);

            ExNumber coeff1 = GetCoeffTerm(matchingGroup);
            if (coeff1 == null)
            {
                coeff1 = new ExNumber(1.0);
                int matchingGroupCount = matchingGroup.Length;
                ExComp[] tmpGp = new ExComp[matchingGroupCount + 2];
                for (int i = 0; i < matchingGroupCount; ++i)
                {
                    tmpGp[i] = matchingGroup[i];
                }
                tmpGp[matchingGroupCount] = new Operators.MulOp();
                tmpGp[matchingGroupCount + 1] = coeff1;

                matchingGroup = tmpGp;
            }

            coeff1.Add(1.0);
            term.AddGroup(matchingGroup);

            // There are very limited cases where the need for this call actually arises but for time to time there is a zero coefficient
            // left and this messes everything up with factoring. (i.e. infinity is factored for some reason.)
            return term.RemoveZeros();
        }

        public static ExComp Intersect(AlgebraTerm term1, AlgebraTerm term2)
        {
            List<TypePair<ExComp[], bool>> group2Checks = new List<TypePair<ExComp[], bool>>();

            List<ExComp[]> term2GroupsNoOps = term2.GetGroupsNoOps();
            for (int i = 0; i < term2GroupsNoOps.Count; ++i)
                group2Checks.Add(new TypePair<ExComp[], bool>(term2GroupsNoOps[i], false));

            List<ExComp[]> groups1 = term1.GetGroupsNoOps();

            List<ExComp[]> intersectedGroups = new List<ExComp[]>();
            for (int i = 0; i < groups1.Count; ++i)
            {
                ExComp[] group1 = groups1[i];

                ExComp[] match = null;
                for (int j = 0; j < group2Checks.Count; ++j)
                {
                    TypePair<ExComp[], bool> group2Check = group2Checks[j];
                    if (group2Check.GetData2())
                        continue;
                    if (GroupsCombinable(group1, group2Check.GetData1()))
                    {
                        match = group2Check.GetData1();
                        group2Checks[j].SetData2(true);
                        break;
                    }
                }
                if (match == null)
                {
                    intersectedGroups.Add(group1);
                }
                else
                {
                    ExComp[] baseGroup = RemoveCoeffs(match);

                    ExNumber coeff1 = GetCoeffTerm(group1) ?? new ExNumber(1.0);
                    ExNumber coeff2 = GetCoeffTerm(match) ?? new ExNumber(1.0);
                    ExNumber combinedCoeff = ExNumber.OpAdd(coeff1, coeff2);

                    if (ExNumber.OpNotEquals(combinedCoeff, 0.0))
                    {
                        if (ExNumber.OpNotEquals(combinedCoeff, 1.0))
                            AddTermToGroup(ref baseGroup, combinedCoeff, true);

                        intersectedGroups.Add(baseGroup);
                    }
                }
            }

            foreach (TypePair<ExComp[], bool> group2Check in group2Checks)
            {
                if (!group2Check.GetData2())
                    intersectedGroups.Add(group2Check.GetData1());
            }

            AlgebraTerm finalTerm = new AlgebraTerm(intersectedGroups.ToArray());
            return finalTerm;
        }

        public static ExComp[] MultiplyGroup(ExComp[] group, ExComp comp)
        {
            List<ExComp> combinedGroup = new List<ExComp>();
            bool matchingFound = false;
            for (int j = 0; j < group.Length; ++j)
            {
                ExComp groupComp = group[j];

                bool combinable = false;
                if (groupComp is PowerFunction || comp is PowerFunction)
                {
                    ExComp base1 = groupComp is PowerFunction ? (groupComp as PowerFunction).GetBase() : groupComp;
                    ExComp base2 = comp is PowerFunction ? (comp as PowerFunction).GetBase() : comp;

                    combinable = base1.IsEqualTo(base2);
                }
                else if (groupComp is ExNumber && comp is ExNumber)
                    combinable = true;
                else
                    combinable = groupComp.IsEqualTo(comp);

                if (combinable)
                {
                    matchingFound = true;
                    Operators.MulOp multOp = new Operators.MulOp();
                    group[j] = multOp.Combine(groupComp, comp);
                    break;
                }
            }

            if (!matchingFound)
            {
                combinedGroup.Add(comp);
            }

            combinedGroup.AddRange(group);

            return combinedGroup.ToArray();
        }

        public static ExComp[] MultiplyGroups(ExComp[] group1, ExComp[] group2)
        {
            ExComp[] acumGroup = group1;

            foreach (ExComp group2Comp in group2)
            {
                acumGroup = MultiplyGroup(acumGroup, group2Comp);
            }

            return acumGroup;
        }

        public static TrigFunction TrigToSimplifyTo(List<ExComp> trigFuncs)
        {
            TrigFunction trigEx = null;
            for (int i = 0; i < trigFuncs.Count; ++i)
            {
                if (trigFuncs[i] is TrigFunction)
                {
                    if (trigEx != null)
                        return null;
                    else
                        trigEx = trigFuncs[i] as TrigFunction;
                }
            }

            if (trigEx == null)
            {
                // Go for the highest power which is divisible by 2.
                List<TypePair<int, TrigFunction>> trigFuncIntPows = new List<TypePair<int, TrigFunction>>();
                foreach (ExComp trigFunc in trigFuncs)
                {
                    if (trigFunc is PowerFunction)
                    {
                        PowerFunction pfTf = trigFunc as PowerFunction;
                        if (!(pfTf.GetPower() is ExNumber))
                            return null;
                        ExNumber pfTfPow = pfTf.GetPower() as ExNumber;
                        if (!pfTfPow.IsRealInteger())
                            return null;
                        int pow = (int)pfTfPow.GetRealComp();
                        if (pow % 2 != 0)
                            return pfTf.GetBase() as TrigFunction;
                        trigFuncIntPows.Add(new TypePair<int, TrigFunction>(pow, pfTf.GetBase() as TrigFunction));
                    }
                    else
                        throw new InvalidOperationException();
                }

                trigFuncIntPows = ArrayFunc.OrderList(trigFuncIntPows);

                if (trigFuncIntPows.Count > 0)
                {
                    return trigFuncIntPows[0].GetData2();
                }
            }

            return trigEx;
        }
    }
}