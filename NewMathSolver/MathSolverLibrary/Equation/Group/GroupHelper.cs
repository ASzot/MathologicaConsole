using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal static class GroupHelper
    {
        public static ExComp[] CloneGroup(this ExComp[] group)
        {
            ExComp[] clonedGroup = new ExComp[group.Length];

            for (int i = 0; i < group.Length; ++i)
            {
                clonedGroup[i] = group[i].Clone();
            }

            return clonedGroup;
        }

        public static ExComp CompoundGroups(this List<AlgebraGroup> groups)
        {
            AlgebraTerm term = new AlgebraTerm();
            foreach (AlgebraGroup group in groups)
            {
                term.AddGroup(group.Group);
            }

            return term;
        }

        public static bool CompsRelatable(ExComp ex1, ExComp ex2)
        {
            if (ex1 is Number && ex2 is Number)
                return true;
            if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp agComp1 = ex1 as AlgebraComp;
                AlgebraComp agComp2 = ex2 as AlgebraComp;

                if (agComp1.Var == agComp2.Var)
                    return true;
            }
            if (ex1 is AlgebraTerm || ex2 is AlgebraTerm)
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                ExComp other = ex1 is AlgebraTerm ? ex2 : ex1;
                return term.TermsRelatable(other);
            }

            return false;
        }

        public static bool ContainsFrac(this ExComp[] group)
        {
            foreach (ExComp comp in group)
            {
                if (comp is Functions.PowerFunction)
                {
                    Functions.PowerFunction powComp = comp as Functions.PowerFunction;
                    if (powComp.IsDenominator())
                        return true;
                }
                else if (comp is AlgebraTerm && !(comp is AlgebraFunction))
                {
                    var groups = (comp as AlgebraTerm).GetGroups();
                    foreach (var compGroup in groups)
                    {
                        if (compGroup.ContainsFrac())
                            return true;
                    }
                }
            }

            return false;
        }

        public static string FinalToMathAsciiString(this ExComp[] group)
        {
            if (group.Length == 1)
            {
                if (group[0] is Number)
                    return (group[0] as Number).FinalToDispString();
                if (group[0] is AlgebraTerm)
                    return (group[0] as AlgebraTerm).FinalToDispStr();
            }

            return group.ToMathAsciiString();
        }

        public static ExComp[] GCF(ExComp[] group1, ExComp[] group2)
        {
            List<ExComp> gcfGroup = new List<ExComp>();

            foreach (ExComp group1Comp in group1)
            {
                foreach (ExComp group2Comp in group2)
                {
                    if (CompsRelatable(group1Comp, group2Comp))
                    {
                        ExComp gcf = Operators.DivOp.GetCommonFactor(group1Comp, group2Comp);

                        gcfGroup.Add(gcf);
                        break;
                    }
                }
            }

            return gcfGroup.ToArray();
        }

        public static List<Constant> GetConstantTerms(this ExComp[] group)
        {
            List<Constant> constants = new List<Constant>();
            foreach (ExComp groupComp in group)
            {
                if (groupComp is Constant)
                {
                    constants.Add(groupComp as Constant);
                }
            }

            return constants;
        }

        public static void GetConstVarTo(this ExComp[] group, out ExComp[] varGp, out ExComp[] constGp, params AlgebraComp[] varFors)
        {
            List<ExComp> varGpList = new List<ExComp>();
            List<ExComp> constGpList = new List<ExComp>();

            foreach (ExComp gpCmp in group)
            {
                // Check if it equals one of the vars.
                bool match = false;
                foreach (AlgebraComp varFor in varFors)
                {
                    match = varFor.IsEqualTo(gpCmp) || (gpCmp is AlgebraTerm && (gpCmp as AlgebraTerm).Contains(varFor));
                    if (match)
                        break;
                }

                if (match)
                    varGpList.Add(gpCmp);
                else
                    constGpList.Add(gpCmp);
            }

            varGp = varGpList.ToArray();
            constGp = constGpList.ToArray();
        }

        public static ExComp[] GetDenominator(this ExComp[] group, bool force = false)
        {
            List<ExComp> denGroup = new List<ExComp>();
            for (int j = 0; j < group.Length; ++j)
            {
                var groupComp = group[j];
                if (groupComp is Functions.PowerFunction && (groupComp as Functions.PowerFunction).IsDenominator())
                {
                    PowerFunction powFunc = groupComp as PowerFunction;

                    powFunc.Power = Number.NegOne;
                    if (powFunc.Power is AlgebraTerm)
                        powFunc.Power = (powFunc.Power as AlgebraTerm).MakeWorkable();
                    denGroup.Add(powFunc.Base);
                }
            }

            if (denGroup.Count == 0 && force)
            {
                denGroup.Add(new Number(1.0));
            }

            ExComp[] denGroupArray = denGroup.ToArray();
            denGroupArray = denGroupArray.RemoveRedundancies();

            return denGroupArray;
        }

        public static ExComp[] GetFactorOf(this ExComp[] group, ExComp[] compareGroup)
        {
            List<ExComp> factorOfList = new List<ExComp>();
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                bool found = false;
                for (int j = 0; j < compareGroup.Length; ++j)
                {
                    ExComp compareGroupComp = compareGroup[j];

                    if (groupComp is Number && compareGroupComp is Number)
                    {
                        Number groupCompNum = groupComp as Number;
                        Number comapreGroupCompNum = compareGroupComp as Number;
                        Number gcf = Number.GCF(groupCompNum, comapreGroupCompNum);
                        if (gcf == groupCompNum)
                        {
                            ExComp diff = comapreGroupCompNum / groupCompNum;
                            factorOfList.Add(diff);
                            found = true;
                            break;
                        }
                    }
                    else if (groupComp is AlgebraComp && compareGroupComp is AlgebraComp)
                    {
                        if (groupComp.IsEqualTo(compareGroupComp))
                        {
                            // Nothing needs to be added as the thing added would just be a 1.0 number.
                            found = true;
                            break;
                        }
                    }
                    else if (groupComp is PowerFunction && compareGroupComp is PowerFunction)
                    {
                        PowerFunction groupCompPow = groupComp as PowerFunction;
                        PowerFunction compareGroupCompPow = compareGroupComp as PowerFunction;
                        ExComp power1 = groupCompPow.Power;
                        ExComp power2 = compareGroupCompPow.Power;

                        if (power1 is AlgebraTerm)
                            power1 = (power1 as AlgebraTerm).RemoveRedundancies();
                        if (power2 is AlgebraTerm)
                            power2 = (power2 as AlgebraTerm).RemoveRedundancies();

                        if (power1 is Number && power2 is Number)
                        {
                            Number powNum1 = power1 as Number;
                            Number powNum2 = power2 as Number;

                            if (powNum1 < powNum2)
                            {
                                factorOfList.Add(powNum2 - powNum1);
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (groupComp is AlgebraComp && compareGroupComp is PowerFunction)
                    {
                        PowerFunction compareGroupPowFunc = compareGroupComp as PowerFunction;
                        if (compareGroupPowFunc.Base.IsEqualTo(groupComp) && compareGroupPowFunc.Power is Number)
                        {
                            Number powNum = compareGroupPowFunc.Power as Number;
                            factorOfList.Add(powNum);
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    return null;
            }

            return factorOfList.ToArray();
        }

        public static double GetHighestPower(this ExComp[] group)
        {
            double max = -1;
            foreach (ExComp groupComp in group)
            {
                double compareVal = groupComp.GetCompareVal();
                max = Math.Max(compareVal, max);
            }
            return max;
        }

        public static ExComp[] GetNumerator(this ExComp[] group)
        {
            List<ExComp> numGroup = new List<ExComp>();
            for (int j = 0; j < group.Length; ++j)
            {
                var groupComp = group[j];
                if (groupComp is Functions.PowerFunction && (groupComp as Functions.PowerFunction).IsDenominator())
                    continue;

                numGroup.Add(groupComp);
            }

            ExComp[] numGroupArray = numGroup.ToArray();
            numGroupArray = numGroupArray.RemoveRedundancies();

            if (numGroupArray.Length == 0)
            {
                ExComp[] oneGroup = { Number.One };
                return oneGroup;
            }

            return numGroupArray;
        }

        public static ExComp GetPowerOfComp(this ExComp[] group, ExComp comp)
        {
            foreach (ExComp groupComp in group)
            {
                if (groupComp is Functions.PowerFunction)
                {
                    Functions.PowerFunction groupPowFunc = groupComp as Functions.PowerFunction;
                    if (groupPowFunc.Base.IsEqualTo(comp))
                        return groupPowFunc.Power;
                }
            }
            return null;
        }

        public static ExComp GetRelatableTermOfGroup(this ExComp[] group, ExComp comp, out int index)
        {
            index = -1;
            for (int i = 0; i < group.Count(); ++i)
            {
                ExComp groupComp = group[i];
                if (CompsRelatable(groupComp, comp))
                {
                    index = i;
                    return groupComp;
                }
            }

            return null;
        }

        public static ExComp GetRelatableTermOfGroup(this ExComp[] group, ExComp comp)
        {
            foreach (ExComp groupComp in group)
            {
                if (CompsRelatable(groupComp, comp))
                    return groupComp;
            }

            return null;
        }

        public static ExComp GetRelatableTermOfGroup(this List<TypePair<ExComp, bool>> group, ExComp comp)
        {
            foreach (var groupComp in group)
            {
                if (CompsRelatable(groupComp.Data1, comp))
                {
                    groupComp.Data2 = true;
                    return groupComp.Data1;
                }
            }

            return null;
        }

        public static ExComp[] GetUnrelatableTermsOfGroup(this ExComp[] group, AlgebraComp comp)
        {
            group = group.RemoveRedundancies();
            List<ExComp> unrelatableTerms = new List<ExComp>();
            foreach (ExComp groupComp in group)
            {
                if (groupComp is AlgebraComp && (groupComp as AlgebraComp) == comp)
                    continue;
                else if (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).Contains(comp))
                    continue;
                unrelatableTerms.Add(groupComp);
            }

            if (unrelatableTerms.Count == 0)
            {
                unrelatableTerms.Add(Number.One);
            }

            return unrelatableTerms.ToArray();
        }

        public static ExComp[] GetVariableTo(this ExComp[] group, AlgebraComp varFor)
        {
            List<ExComp> varGp = new List<ExComp>();
            foreach (ExComp gpCmp in group)
            {
                if (varFor.IsEqualTo(gpCmp) || (gpCmp is AlgebraTerm && (gpCmp as AlgebraTerm).Contains(varFor)))
                    varGp.Add(gpCmp);
            }

            return varGp.ToArray();
        }

        public static bool GroupContains(this ExComp[] group, AlgebraComp varFor)
        {
            foreach (ExComp groupComp in group)
            {
                if (groupComp is AlgebraComp && (groupComp as AlgebraComp) == varFor)
                    return true;
                else if (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).Contains(varFor))
                    return true;
                else if (groupComp is LogFunction && (groupComp as LogFunction).Base.ToAlgTerm().Contains(varFor))
                    return true;
            }

            return false;
        }

        public static bool GroupContains(this ExComp[] group, ExComp ex)
        {
            foreach (ExComp groupComp in group)
            {
                if (groupComp.IsEqualTo(ex))
                    return true;
            }
            return false;
        }

        public static bool IsNeg(this ExComp[] group)
        {
            foreach (ExComp gpCmp in group)
            {
                if (gpCmp is Number && (gpCmp as Number) < 0.0)
                    return true;
            }

            return false;
        }

        public static ExComp LCF(ExComp comp1, ExComp comp2)
        {
            if (comp1.IsEqualTo(comp2))
                return comp2;
            if (comp1 is Number && comp2 is Number)
            {
                Number n1 = comp1 as Number;
                Number n2 = comp2 as Number;

                Number lcf = Number.LCF(n1, n2);
                return lcf;
            }
            else if (comp1 is PowerFunction && comp2 is PowerFunction)
            {
                PowerFunction pow1 = comp1 as PowerFunction;
                PowerFunction pow2 = comp2 as PowerFunction;

                if (pow1.Power.IsEqualTo(pow2.Power))
                {
                    return pow1;
                }

                if (pow1.Power is Number && pow2.Power is Number)
                {
                    Number max = Number.Maximum(pow1.Power as Number, pow2.Power as Number);
                    PowerFunction maxPow = new PowerFunction(pow1.Base, max);
                    return maxPow;
                }
            }
            else if ((comp1 is PowerFunction && comp2 is AlgebraComp) ||
                (comp1 is AlgebraComp && comp2 is PowerFunction))
            {
                PowerFunction pow = comp1 is PowerFunction ? comp1 as PowerFunction : comp2 as PowerFunction;
                AlgebraComp agComp = comp1 is AlgebraComp ? comp1 as AlgebraComp : comp2 as AlgebraComp;

                if (pow.Power is Number && (pow.Power as Number) > 1.0)
                    return pow;
            }

            if (Number.One.IsEqualTo(comp1))
                return comp2;
            if (Number.One.IsEqualTo(comp2))
                return comp1;

            var mulOp = new Operators.MulOp();
            return mulOp.Combine(comp1, comp2);
        }

        public static ExComp[] LCF(List<ExComp[]> groups)
        {
            List<ExComp[]> narrowedList = new List<ExComp[]>();

            if (groups.Count == 0)
                return new ExComp[0];

            if (groups.Count == 1)
                return groups[0];
            if (groups.Count == 2)
            {
                return LCF(groups[0], groups[1]);
            }

            for (int i = 1; i < groups.Count; ++i)
            {
                ExComp[] n1 = groups[i];
                ExComp[] n2 = groups[i - 1];
                ExComp[] gcf = LCF(n1, n2);
                narrowedList.Add(gcf);
            }

            return LCF(narrowedList);
        }

        public static ExComp[] LCF(ExComp[] group1, ExComp[] group2)
        {
            group1 = group1.RemoveRedundancies();
            group2 = group2.RemoveRedundancies();
            List<ExComp> lcmComps = new List<ExComp>();

            var group2Checks = (from gpCmp in group2
                                select new TypePair<ExComp, bool>(gpCmp, false)).ToList();

            foreach (ExComp group1Comp in group1)
            {
                ExComp relatableComp = group2Checks.GetRelatableTermOfGroup(group1Comp);
                if (relatableComp != null)
                {
                    ExComp lcf = LCF(relatableComp, group1Comp);
                    lcmComps.Add(lcf);
                }
                else
                {
                    lcmComps.Add(group1Comp);
                }
            }

            foreach (var group2Check in group2Checks)
            {
                if (!group2Check.Data2)
                    lcmComps.Add(group2Check.Data1);
            }

            for (int i = 0; i < lcmComps.Count; ++i)
            {
                if (Number.One.IsEqualTo(lcmComps[i]))
                    lcmComps.RemoveAt(i);
            }

            return lcmComps.ToArray();
        }

        public static List<TypePair<ExComp, ExComp>> MatchCorresponding(ExComp[] group1, ExComp[] group2, out List<TypePair<int, int>> matchIndices)
        {
            matchIndices = new List<TypePair<int, int>>();
            var checkedGroup2 = (from group2Comp in group2
                                 select new TypePair<ExComp, bool>(group2Comp, false)).ToList();

            List<TypePair<ExComp, ExComp>> corresponding = new List<TypePair<ExComp, ExComp>>();

            for (int i = 0; i < group1.Count(); ++i)
            {
                ExComp group1Comp = group1[i];
                for (int j = 0; j < checkedGroup2.Count(); ++j)
                {
                    var checkedComp2 = checkedGroup2.ElementAt(j);
                    if (checkedComp2.Data2)
                        continue;
                    if (CompsRelatable(group1Comp, checkedComp2.Data1))
                    {
                        corresponding.Add(new TypePair<ExComp, ExComp>(group1Comp, checkedComp2.Data1));
                        matchIndices.Add(new TypePair<int, int>(i, j));
                        checkedComp2.Data2 = true;
                    }
                }
            }

            return corresponding;
        }

        public static ExComp[] OrderGroup(this ExComp[] group)
        {
            List<Number> coeffs = new List<Number>();
            foreach (ExComp gpCmp in group)
            {
                if (gpCmp is Number)
                    coeffs.Add(gpCmp as Number);
            }
            if (coeffs.Count != 0)
                group = AlgebraTerm.RemoveCoeffs(group);

            List<Constant> constants = group.GetConstantTerms();
            group = group.RemoveExTerms(constants);

            group = group.OrderBy(g => g.GetCompareVal()).Reverse().ToArray();
            if (coeffs.Count != 0 || constants.Count > 0)
            {
                List<ExComp> final = new List<ExComp>();
                if (coeffs.Count != 0)
                    final.AddRange(coeffs);
                final.AddRange(constants);
                final.AddRange(group);
                group = final.ToArray();
            }

            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                if (groupComp is AlgebraTerm)
                {
                    AlgebraTerm groupCompTerm = groupComp as AlgebraTerm;
                    group[i] = groupCompTerm.Order();
                }
            }

            return group;
        }

        public static List<ExComp> RemoveDuplicates(this List<ExComp> exCompList)
        {
            for (int i = 0; i < exCompList.Count; ++i)
            {
                ExComp exComp = exCompList[i];
                for (int j = 0; j < exCompList.Count; ++j)
                {
                    if (j == i)
                        continue;
                    ExComp compareExComp = exCompList[j];
                    if (exComp.IsEqualTo(compareExComp))
                    {
                        exCompList.RemoveAt(i--);
                        break;
                    }
                }
            }

            return exCompList;
        }

        public static ExComp[] RemoveEx(this ExComp[] group, ExComp exToRemove)
        {
            List<ExComp> groupList = group.ToList();

            groupList.Remove(exToRemove);

            return groupList.ToArray();
        }

        public static ExComp[] RemoveExTerms(this ExComp[] group, IEnumerable<ExComp> termsToRemove)
        {
            foreach (ExComp term in termsToRemove)
            {
                group = RemoveEx(group, term);
            }

            return group;
        }

        public static ExComp[] RemoveOneCoeffs(this ExComp[] group)
        {
            if (group.ContainsFrac())
            {
                var num = group.GetNumerator();
                var den = group.GetDenominator();

                if (num.Length <= 1)
                {
                    return group;
                }
            }

            if (group.Length == 1)
                return group;

            List<ExComp> removedList = new List<ExComp>();

            foreach (ExComp groupComp in group)
            {
                if (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).IsOne())
                    continue;
                else if (groupComp is Number && (groupComp as Number) == 1.0)
                    continue;
                removedList.Add(groupComp);
            }

            return removedList.ToArray();
        }

        public static ExComp[] RemoveOperators(this ExComp[] group)
        {
            List<ExComp> groupList = group.ToList();

            for (int i = 0; i < groupList.Count; ++i)
            {
                if (groupList[i] is AgOp)
                    groupList.RemoveAt(i--);
            }

            return groupList.ToArray();
        }

        public static ExComp[] RemoveRedundancies(this ExComp[] group)
        {
            List<ExComp> workedGroup = new List<ExComp>();
            for (int i = 0; i < group.Count(); ++i)
            {
                ExComp groupComp = group[i];

                if (groupComp is AlgebraTerm && !(groupComp is AlgebraFunction))
                {
                    AlgebraTerm groupCompTerm = groupComp as AlgebraTerm;

                    var subGroups = groupCompTerm.GetGroupsNoOps();
                    if (subGroups.Count == 1)
                    {
                        workedGroup.AddRange(subGroups[0]);
                        continue;
                    }
                }

                workedGroup.Add(groupComp);
            }

            return workedGroup.ToArray();
        }

        public static AlgebraTerm ToAlgNoRedunTerm(this ExComp[] group)
        {
            AlgebraTerm term = group.ToAlgTerm();
            ExComp ex = term.RemoveRedundancies();
            return ex.ToAlgTerm();
        }

        public static AlgebraTerm ToAlgTerm(this ExComp[] group)
        {
            AlgebraTerm term = new AlgebraTerm(group);

            return term;
        }

        public static string ToMathAsciiString(this ExComp[] group)
        {
            string finalStr = "";
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                finalStr += groupComp.ToMathAsciiString();
                if ((groupComp is Number && (groupComp as Number) == -1 && group.Length > 1) ||
                    (groupComp is Number && i < group.Length - 1 && group[i + 1] is Number))
                    finalStr += "*";
            }

            return finalStr;
        }

        public static string ToTexString(this ExComp[] group)
        {
            string finalStr = "";
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                finalStr += groupComp.ToTexString();
                if ((groupComp is Number && (groupComp as Number) == -1 && group.Length > 1) ||
                    (groupComp is Number && i < group.Length - 1 && group[i + 1] is Number))
                    finalStr += "*";
            }

            return finalStr;
        }

        public static ExComp[] AccumulateTerms(this ExComp[] group)
        {
            ExComp accum = AccumulateTermsRecur(group.ToList());
            if (accum is AlgebraTerm)
            {
                AlgebraTerm acumTerm = accum as AlgebraTerm;
                var groups = acumTerm.GetGroupsNoOps();
                if (groups.Count == 1)
                    return groups[0];
                else
                    return null;
            }

            return new ExComp[] { accum };
        }

        private static ExComp AccumulateTermsRecur(List<ExComp> exs)
        {
            if (exs.Count == 1)
                return exs[0];
            else if (exs.Count == 2)
            {
                return Operators.MulOp.StaticCombine(exs[0], exs[1]);
            }

            ExComp combined = Operators.MulOp.StaticCombine(exs[0], exs[1]);
            exs.RemoveRange(0, 2);
            exs.Insert(0, combined);

            return AccumulateTermsRecur(exs);
        }
    }
}