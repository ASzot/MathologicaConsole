using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal static class GroupHelper
    {
        public static ExComp[] CloneGroup(ExComp[] group)
        {
            ExComp[] clonedGroup = new ExComp[group.Length];

            for (int i = 0; i < group.Length; ++i)
            {
                clonedGroup[i] = group[i].CloneEx();
            }

            return clonedGroup;
        }

        public static ExComp CompoundGroups(List<AlgebraGroup> groups)
        {
            AlgebraTerm term = new AlgebraTerm();
            foreach (AlgebraGroup group in groups)
            {
                term.AddGroup(group.GetGroup());
            }

            return term;
        }

        public static List<ExComp[]> CloneGpList(List<ExComp[]> gps)
        {
            List<ExComp[]> cloned = new List<ExComp[]>();
            for (int i = 0; i < gps.Count; ++i)
            {
                cloned.Add(CloneGroup(gps[i]));
            }

            return cloned;
        }

        public static bool CompsRelatable(ExComp ex1, ExComp ex2)
        {
            if (ex1 is ExNumber && ex2 is ExNumber)
                return true;
            if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp agComp1 = ex1 as AlgebraComp;
                AlgebraComp agComp2 = ex2 as AlgebraComp;

                if (agComp1.GetVar().GetVar() == agComp2.GetVar().GetVar())
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

        public static bool ContainsFrac(ExComp[] group)
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
                    List<ExComp[]> groups = (comp as AlgebraTerm).GetGroups();
                    foreach (ExComp[] compGroup in groups)
                    {
                        if (ContainsFrac(compGroup))
                            return true;
                    }
                }
            }

            return false;
        }

        public static string FinalToMathAsciiString(ExComp[] group)
        {
            if (group.Length == 1)
            {
                if (group[0] is ExNumber)
                    return (group[0] as ExNumber).FinalToDispString();
                if (group[0] is AlgebraTerm)
                    return (group[0] as AlgebraTerm).FinalToDispStr();
            }

            return ToAsciiString(group);
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

        public static List<Constant> GetConstantTerms(ExComp[] group)
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

        public static void GetConstVarTo(ExComp[] group, out ExComp[] varGp, out ExComp[] constGp, params AlgebraComp[] varFors)
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

        public static ExComp[] ForceDistributeExponent(ExComp[] group)
        {
            if (group.Length == 1 && group[0] is PowerFunction && !(group[0] as PowerFunction).GetPower().IsEqualTo(ExNumber.GetNegOne()))
            {
                PowerFunction pf = group[0] as PowerFunction;
                if (pf.GetBase() is AlgebraTerm)
                {
                    AlgebraTerm baseTerm = pf.GetBase() as AlgebraTerm;
                    List<ExComp[]> baseTermGps = baseTerm.GetGroupsNoOps();
                    if (baseTermGps.Count == 1)
                    {
                        // Raise each term to the power.
                        ExComp[] singularGp = baseTermGps[0];
                        for (int i = 0; i < singularGp.Length; ++i)
                        {
                            singularGp[i] = Operators.PowOp.StaticCombine(singularGp[i], pf.GetPower());
                        }
                        return singularGp;
                    }
                }
            }

            return group;
        }

        public static ExComp[] GetDenominator(ExComp[] group, bool force)
        {
            List<ExComp> denGroup = new List<ExComp>();
            for (int j = 0; j < group.Length; ++j)
            {
                ExComp groupComp = group[j];
                if (groupComp is Functions.PowerFunction && (groupComp as Functions.PowerFunction).IsDenominator())
                {
                    PowerFunction powFunc = groupComp as PowerFunction;

                    powFunc.SetPower(ExNumber.GetNegOne());
                    if (powFunc.GetPower() is AlgebraTerm)
                        powFunc.SetPower((powFunc.GetPower() as AlgebraTerm).MakeWorkable());
                    denGroup.Add(powFunc.GetBase());
                }
            }

            if (denGroup.Count == 0 && force)
            {
                denGroup.Add(new ExNumber(1.0));
            }

            ExComp[] denGroupArray = denGroup.ToArray();
            denGroupArray = RemoveRedundancies(denGroupArray);

            return denGroupArray;
        }

        public static ExComp[] GetFactorOf(ExComp[] group, ExComp[] compareGroup)
        {
            List<ExComp> factorOfList = new List<ExComp>();
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                bool found = false;
                for (int j = 0; j < compareGroup.Length; ++j)
                {
                    ExComp compareGroupComp = compareGroup[j];

                    if (groupComp is ExNumber && compareGroupComp is ExNumber)
                    {
                        ExNumber groupCompNum = groupComp as ExNumber;
                        ExNumber comapreGroupCompNum = compareGroupComp as ExNumber;
                        ExNumber gcf = ExNumber.GCF(groupCompNum, comapreGroupCompNum);
                        if (ExNumber.OpEqual(gcf, groupCompNum))
                        {
                            ExComp diff = ExNumber.OpDiv(comapreGroupCompNum, groupCompNum);
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
                        ExComp power1 = groupCompPow.GetPower();
                        ExComp power2 = compareGroupCompPow.GetPower();

                        if (power1 is AlgebraTerm)
                            power1 = (power1 as AlgebraTerm).RemoveRedundancies(false);
                        if (power2 is AlgebraTerm)
                            power2 = (power2 as AlgebraTerm).RemoveRedundancies(false);

                        if (power1 is ExNumber && power2 is ExNumber)
                        {
                            ExNumber powNum1 = power1 as ExNumber;
                            ExNumber powNum2 = power2 as ExNumber;

                            if (ExNumber.OpLT(powNum1, powNum2))
                            {
                                factorOfList.Add(ExNumber.OpSub(powNum2, powNum1));
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (groupComp is AlgebraComp && compareGroupComp is PowerFunction)
                    {
                        PowerFunction compareGroupPowFunc = compareGroupComp as PowerFunction;
                        if (compareGroupPowFunc.GetBase().IsEqualTo(groupComp) && compareGroupPowFunc.GetPower() is ExNumber)
                        {
                            ExNumber powNum = compareGroupPowFunc.GetPower() as ExNumber;
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

        public static double GetHighestPower(ExComp[] group)
        {
            double max = -1;
            foreach (ExComp groupComp in group)
            {
                double compareVal = groupComp.GetCompareVal();
                max = Math.Max(compareVal, max);
            }
            return max;
        }

        public static ExComp[] GetNumerator(ExComp[] group)
        {
            List<ExComp> numGroup = new List<ExComp>();
            for (int j = 0; j < group.Length; ++j)
            {
                ExComp groupComp = group[j];
                if (groupComp is Functions.PowerFunction && (groupComp as Functions.PowerFunction).IsDenominator())
                    continue;

                numGroup.Add(groupComp);
            }

            ExComp[] numGroupArray = numGroup.ToArray();
            numGroupArray = RemoveRedundancies(numGroupArray);

            if (numGroupArray.Length == 0)
            {
                ExComp[] oneGroup = new ExComp[] { ExNumber.GetOne() };
                return oneGroup;
            }

            return numGroupArray;
        }

        public static ExComp GetPowerOfComp(ExComp[] group, ExComp comp)
        {
            foreach (ExComp groupComp in group)
            {
                if (groupComp is Functions.PowerFunction)
                {
                    Functions.PowerFunction groupPowFunc = groupComp as Functions.PowerFunction;
                    if (groupPowFunc.GetBase().IsEqualTo(comp))
                        return groupPowFunc.GetPower();
                }
            }
            return null;
        }

        public static ExComp GetRelatableTermOfGroup(ExComp[] group, ExComp comp, out int index)
        {
            index = -1;
            for (int i = 0; i < group.Length; ++i)
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

        public static ExComp GetRelatableTermOfGroup(ExComp[] group, ExComp comp)
        {
            foreach (ExComp groupComp in group)
            {
                if (CompsRelatable(groupComp, comp))
                    return groupComp;
            }

            return null;
        }

        public static ExComp GetRelatableTermOfGroup(List<TypePair<ExComp, bool>> group, ExComp comp)
        {
            foreach (TypePair<ExComp, bool> groupComp in group)
            {
                if (CompsRelatable(groupComp.GetData1(), comp))
                {
                    groupComp.SetData2(true);
                    return groupComp.GetData1();
                }
            }

            return null;
        }

        public static ExComp[] GetUnrelatableTermsOfGroup(ExComp[] group, AlgebraComp comp)
        {
            group = RemoveRedundancies(group);
            List<ExComp> unrelatableTerms = new List<ExComp>();
            foreach (ExComp groupComp in group)
            {
                if (groupComp is AlgebraComp && (groupComp as AlgebraComp).IsEqualTo(comp))
                    continue;
                else if (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).Contains(comp))
                    continue;
                unrelatableTerms.Add(groupComp);
            }

            if (unrelatableTerms.Count == 0)
            {
                unrelatableTerms.Add(ExNumber.GetOne());
            }

            return unrelatableTerms.ToArray();
        }

        public static ExComp[] GetVariableTo(ExComp[] group, AlgebraComp varFor)
        {
            List<ExComp> varGp = new List<ExComp>();
            foreach (ExComp gpCmp in group)
            {
                if (varFor.IsEqualTo(gpCmp) || (gpCmp is AlgebraTerm && (gpCmp as AlgebraTerm).Contains(varFor)))
                    varGp.Add(gpCmp);
            }

            return varGp.ToArray();
        }

        public static bool GroupContains(ExComp[] group, AlgebraComp varFor)
        {
            foreach (ExComp groupComp in group)
            {
                if (groupComp is AlgebraComp && (groupComp as AlgebraComp).IsEqualTo(varFor))
                    return true;
                else if (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).Contains(varFor))
                    return true;
                else if (groupComp is LogFunction && (groupComp as LogFunction).GetBase().ToAlgTerm().Contains(varFor))
                    return true;
            }

            return false;
        }

        public static bool GroupContains(ExComp[] group, ExComp ex)
        {
            foreach (ExComp groupComp in group)
            {
                if (groupComp.IsEqualTo(ex))
                    return true;
            }
            return false;
        }

        public static bool IsNeg(ExComp[] group)
        {
            foreach (ExComp gpCmp in group)
            {
                if (gpCmp is ExNumber && ExNumber.OpLT((gpCmp as ExNumber), 0.0))
                    return true;
            }

            return false;
        }

        public static ExComp LCF(ExComp comp1, ExComp comp2)
        {
            if (comp1.IsEqualTo(comp2))
                return comp2;
            if (comp1 is ExNumber && comp2 is ExNumber)
            {
                ExNumber n1 = comp1 as ExNumber;
                ExNumber n2 = comp2 as ExNumber;

                ExNumber lcf = ExNumber.LCF(n1, n2);
                return lcf;
            }
            else if (comp1 is PowerFunction && comp2 is PowerFunction)
            {
                PowerFunction pow1 = comp1 as PowerFunction;
                PowerFunction pow2 = comp2 as PowerFunction;

                if (pow1.GetPower().IsEqualTo(pow2.GetPower()))
                {
                    return pow1;
                }

                if (pow1.GetPower() is ExNumber && pow2.GetPower() is ExNumber)
                {
                    ExNumber max = ExNumber.Maximum(pow1.GetPower() as ExNumber, pow2.GetPower() as ExNumber);
                    PowerFunction maxPow = new PowerFunction(pow1.GetBase(), max);
                    return maxPow;
                }
            }
            else if ((comp1 is PowerFunction && comp2 is AlgebraComp) ||
                (comp1 is AlgebraComp && comp2 is PowerFunction))
            {
                PowerFunction pow = comp1 is PowerFunction ? comp1 as PowerFunction : comp2 as PowerFunction;
                AlgebraComp agComp = comp1 is AlgebraComp ? comp1 as AlgebraComp : comp2 as AlgebraComp;

                if (pow.GetPower() is ExNumber && ExNumber.OpGT((pow.GetPower() as ExNumber), 1.0))
                    return pow;
            }

            if (ExNumber.GetOne().IsEqualTo(comp1))
                return comp2;
            if (ExNumber.GetOne().IsEqualTo(comp2))
                return comp1;

            Operators.MulOp mulOp = new Operators.MulOp();
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
            group1 = RemoveRedundancies(group1);
            group2 = RemoveRedundancies(group2);
            List<ExComp> lcmComps = new List<ExComp>();

            List<TypePair<ExComp, bool>> group2Checks = new List<TypePair<ExComp, bool>>();

            for (int i = 0; i < group2.Length; ++i)
                group2Checks.Add(new TypePair<ExComp, bool>(group2[i], false));

            foreach (ExComp group1Comp in group1)
            {
                ExComp relatableComp = GetRelatableTermOfGroup(group2Checks, group1Comp);
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

            foreach (TypePair<ExComp, bool> group2Check in group2Checks)
            {
                if (!group2Check.GetData2())
                    lcmComps.Add(group2Check.GetData1());
            }

            for (int i = 0; i < lcmComps.Count; ++i)
            {
                if (ExNumber.GetOne().IsEqualTo(lcmComps[i]))
                    ArrayFunc.RemoveIndex(lcmComps, i);
            }

            return lcmComps.ToArray();
        }

        public static List<TypePair<ExComp, ExComp>> MatchCorresponding(ExComp[] group1, ExComp[] group2, out List<TypePair<int, int>> matchIndices)
        {
            matchIndices = new List<TypePair<int, int>>();
            List<TypePair<ExComp, bool>> checkedGroup2 = (from group2Comp in group2
                                                          select new TypePair<ExComp, bool>(group2Comp, false)).ToList();

            List<TypePair<ExComp, ExComp>> corresponding = new List<TypePair<ExComp, ExComp>>();

            for (int i = 0; i < group1.Length; ++i)
            {
                ExComp group1Comp = group1[i];
                for (int j = 0; j < checkedGroup2.Count; ++j)
                {
                    TypePair<ExComp, bool> checkedComp2 = checkedGroup2.ElementAt(j);
                    if (checkedComp2.GetData2())
                        continue;
                    if (CompsRelatable(group1Comp, checkedComp2.GetData1()))
                    {
                        corresponding.Add(new TypePair<ExComp, ExComp>(group1Comp, checkedComp2.GetData1()));
                        matchIndices.Add(new TypePair<int, int>(i, j));
                        checkedComp2.SetData2(true);
                    }
                }
            }

            return corresponding;
        }

        public static ExNumber GetCoeff(ExComp[] group)
        {
            foreach (ExComp gpCmp in group)
            {
                if (gpCmp is ExNumber)
                    return gpCmp as ExNumber;
            }

            return null;
        }

        public static void AssignCoeff(ExComp[] group, ExNumber coeff)
        {
            for (int i = 0; i < group.Length; ++i)
            {
                if (group[i] is ExNumber)
                {
                    group[i] = coeff;
                    break;
                }
            }
        }

        public static ExComp[] OrderGroup(ExComp[] group)
        {
            List<ExNumber> coeffs = new List<ExNumber>();
            foreach (ExComp gpCmp in group)
            {
                if (gpCmp is ExNumber)
                    coeffs.Add(gpCmp as ExNumber);
            }
            if (coeffs.Count != 0)
                group = AlgebraTerm.RemoveCoeffs(group);

            List<Constant> constants = GetConstantTerms(group);
            group = RemoveExTerms(group, constants);

            group = ArrayFunc.OrderListReverse(ArrayFunc.ToList(group)).ToArray();
            if (coeffs.Count != 0 || constants.Count > 0)
            {
                List<ExComp> finalExList = new List<ExComp>();
                if (coeffs.Count != 0)
                    finalExList.AddRange(coeffs);
                finalExList.AddRange(constants);
                finalExList.AddRange(group);
                group = finalExList.ToArray();
            }

            if (ContainsVectors(group))
                return group;

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

        public static List<ExComp> RemoveDuplicates(List<ExComp> exCompList)
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
                        ArrayFunc.RemoveIndex(exCompList, i--);
                        break;
                    }
                }
            }

            return exCompList;
        }

        public static ExComp[] RemoveEx(ExComp[] group, ExComp exToRemove)
        {
            List<ExComp> groupList = ArrayFunc.ToList(group);

            groupList.Remove(exToRemove);

            return groupList.ToArray();
        }

        public static ExComp[] RemoveEx(ExComp[] group, int indexToRemove)
        {
            List<ExComp> groupList = ArrayFunc.ToList(group);

            ArrayFunc.RemoveIndex(groupList, indexToRemove);

            return groupList.ToArray();
        }

        public static ExComp[] RemoveExTerms(ExComp[] group, IEnumerable<ExComp> termsToRemove)
        {
            foreach (ExComp term in termsToRemove)
            {
                group = RemoveEx(group, term);
            }

            return group;
        }

        public static ExComp[] RemoveOneCoeffs(ExComp[] group)
        {
            bool hasDen = ContainsFrac(group);
            if (hasDen)
            {
                ExComp[] num = GetNumerator(group);

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
                else if (groupComp is ExNumber && ExNumber.OpEqual((groupComp as ExNumber), 1.0))
                    continue;
                removedList.Add(groupComp);
            }

            ExComp[] retVal = removedList.ToArray();

            return retVal;
        }

        public static ExComp[] RemoveOperators(ExComp[] group)
        {
            List<ExComp> groupList = ArrayFunc.ToList(group);

            for (int i = 0; i < groupList.Count; ++i)
            {
                if (groupList[i] is AgOp)
                    ArrayFunc.RemoveIndex(groupList, i--);
            }

            return groupList.ToArray();
        }

        public static ExComp[] RemoveRedundancies(ExComp[] group)
        {
            List<ExComp> workedGroup = new List<ExComp>();
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];

                if (groupComp is AlgebraTerm && !(groupComp is AlgebraFunction))
                {
                    AlgebraTerm groupCompTerm = groupComp as AlgebraTerm;

                    List<ExComp[]> subGroups = groupCompTerm.GetGroupsNoOps();
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

        public static AlgebraTerm ToAlgNoRedunTerm(ExComp[] group)
        {
            AlgebraTerm term = ToAlgTerm(group);
            ExComp ex = term.RemoveRedundancies(false);
            return ex.ToAlgTerm();
        }

        public static AlgebraTerm ToAlgTerm(ExComp[] group)
        {
            AlgebraTerm term = new AlgebraTerm(group);

            return term;
        }

        public static string ToAsciiString(ExComp[] group)
        {
            string finalStr = "";
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                finalStr += groupComp.ToAsciiString();
                if ((groupComp is ExNumber && ExNumber.OpEqual((groupComp as ExNumber), -1) && group.Length > 1) ||
                    (groupComp is ExNumber && i < group.Length - 1 && group[i + 1] is ExNumber))
                    finalStr += "*";
                else if (groupComp is AlgebraComp || groupComp is ExNumber)
                    finalStr += " ";
            }

            return finalStr;
        }

        public static string ToTexString(ExComp[] group)
        {
            string finalStr = "";
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp groupComp = group[i];
                finalStr += groupComp.ToTexString();
                if ((groupComp is ExNumber && ExNumber.OpEqual((groupComp as ExNumber), -1) && group.Length > 1) ||
                    (groupComp is ExNumber && i < group.Length - 1 && group[i + 1] is ExNumber))
                    finalStr += "*";
                else if (groupComp is AlgebraComp)
                    finalStr += " ";
            }

            return finalStr;
        }

        public static ExComp[] AccumulateTerms(ExComp[] group)
        {
            ExComp accum = AccumulateTermsRecur(ArrayFunc.ToList(group));
            if (accum is AlgebraTerm)
            {
                AlgebraTerm acumTerm = accum as AlgebraTerm;
                List<ExComp[]> groups = acumTerm.GetGroupsNoOps();
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

        private static bool ContainsVectors(ExComp[] group)
        {
            foreach (ExComp cmp in group)
            {
                if (cmp is Equation.Structural.LinearAlg.ExMatrix)
                    return true;
            }
            return false;
        }
    }
}