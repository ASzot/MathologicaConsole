using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    partial class AlgebraTerm
    {
        private const int MAX_GCF_TERM_COUNT = 50;

        public List<string> GetAllAlgebraCompsStr()
        {
            List<string> varStrs = new List<string>();

            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraComp && !(subComp is Constant))
                {
                    varStrs.Add((subComp as AlgebraComp).GetVar().GetVar());
                }
                else if (subComp is AlgebraTerm)
                {
                    varStrs.AddRange((subComp as AlgebraTerm).GetAllAlgebraCompsStr());
                }
            }

            return varStrs.Distinct().ToList();
        }

        public override double GetCompareVal()
        {
            return GetHighestDegree();
        }

        public List<ExComp[]> GetGroupContainingTerm(ExComp term)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            List<ExComp[]> matchingGroups = new List<ExComp[]>();

            foreach (ExComp[] group in groups)
            {
                if (GroupHelper.GroupContains(group, term))
                    matchingGroups.Add(group);
            }

            return matchingGroups;
        }

        public ExComp[] GetGroupGCF()
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            if (groups.Count == 0)
                return null;
            else if (groups.Count == 1)
                return groups[0];
            else if (groups.Count > MAX_GCF_TERM_COUNT)
                return null;

            List<ExComp> gcf = RecursiveGroupGCFSolve(groups);
            return gcf.ToArray();
        }

        public List<ExComp> GetGroupPow(ExComp power)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            List<ExComp> matchingTerms = new List<ExComp>();

            foreach (ExComp[] gp in groups)
            {
                foreach (ExComp gpCmp in gp)
                {
                    if (gpCmp is Functions.PowerFunction && (gpCmp as Functions.PowerFunction).GetPower().IsEqualTo(power))
                    {
                        matchingTerms.Add(GroupHelper.ToAlgTerm(gp));
                        break;
                    }
                }
            }

            return matchingTerms;
        }

        public virtual List<ExComp[]> GetGroups()
        {
            if (_subComps.Count == 1 && _subComps[0] is AlgebraTerm && !(_subComps[0] is AlgebraFunction))
                return (_subComps[0] as AlgebraTerm).GetGroups();

            // Split the term by addition and subtraction.
            List<ExComp[]> groups = new List<ExComp[]>();
            int startIndex = 0;

            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp exComp = _subComps[i];
                if (exComp is Operators.AddOp || exComp is Operators.SubOp ||
                    i == _subComps.Count - 1)
                {
                    int endIndex = (i == _subComps.Count - 1) ? i + 1 : i;
                    List<ExComp> group = _subComps.GetRange(startIndex, (endIndex - startIndex));
                    startIndex = i + 1;

                    if (exComp is Operators.SubOp)
                    {
                        ExNumber coeff = GetCoeffTerm(group);
                        if (coeff == null)
                        {
                            coeff = new ExNumber(1.0);
                            group.Add(new Operators.MulOp());
                            group.Add(coeff);
                        }
                        coeff = ExNumber.OpMul(coeff, new ExNumber(-1.0f));
                    }

                    groups.Add(group.ToArray());
                }
            }

            return groups;
        }

        public List<AlgebraGroup> GetGroupsConstantTo(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroups();

            List<AlgebraGroup> constantGroupsList = new List<AlgebraGroup>();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (!GroupHelper.GroupContains(groups[i], varFor))
                    constantGroupsList.Add(new AlgebraGroup(groups[i]));
            }

            return constantGroupsList;
        }

        public List<ExComp[]> GetGroupsNoOps()
        {
            List<ExComp[]> groups = GetGroups();

            for (int i = 0; i < groups.Count; ++i)
            {
                groups[i] = GroupHelper.RemoveRedundancies(GroupHelper.RemoveOperators(groups[i]));
            }

            return groups;
        }

        public List<AlgebraGroup> GetGroupsVariableTo(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroups();

            List<AlgebraGroup> variableGroupsList = new List<AlgebraGroup>();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (GroupHelper.GroupContains(groups[i], varFor))
                    variableGroupsList.Add(new AlgebraGroup(groups[i]));
            }

            return variableGroupsList;
        }

        public List<AlgebraGroup> GetGroupsVariableToNoOps(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            List<AlgebraGroup> variableGroupsList = new List<AlgebraGroup>();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (GroupHelper.GroupContains(groups[i], varFor))
                    variableGroupsList.Add(new AlgebraGroup(groups[i]));
            }

            return variableGroupsList;
        }

        public ExComp[] GetMatchingGroup(ExComp[] group)
        {
            List<ExComp[]> groups = GetGroups();

            foreach (ExComp[] compareGroup in groups)
            {
                if (GroupsCombinable(compareGroup, group))
                    return compareGroup;
            }

            return null;
        }

        public AlgebraTerm[] GetNumDenFrac()
        {
            List<ExComp[]> groups = GetGroupsNoOps();
            if (groups.Count != 1)
                return null;

            ExComp[] group = groups[0];
            if (!GroupHelper.ContainsFrac(group))
                return null;
            ExComp[] num = GroupHelper.GetNumerator(group);
            ExComp[] den = GroupHelper.GetDenominator(group, false);

            if (den.Length == 0)
                return null;

            AlgebraTerm[] numDenTerm = new AlgebraTerm[] { GroupHelper.ToAlgTerm(num), GroupHelper.ToAlgTerm(den) };
            return numDenTerm;
        }

        public List<AlgebraGroup> GetVariableFractionGroups(AlgebraComp varFor)
        {
            List<AlgebraGroup> varFracGroups = new List<AlgebraGroup>();

            List<ExComp[]> groups = GetGroupsNoOps();

            foreach (ExComp[] group in groups)
            {
                if (GroupHelper.ContainsFrac(group))
                {
                    AlgebraTerm denTerm = GroupHelper.ToAlgTerm(GroupHelper.GetDenominator(group, false));

                    if (denTerm.Contains(varFor))
                    {
                        varFracGroups.Add(new AlgebraGroup(group));
                    }
                }
            }

            return varFracGroups;
        }

        private static List<ExComp> GetCombinableGroup(ExComp[] groupToSort, bool includeNumbers)
        {
            List<ExComp> compareGroupList = new List<ExComp>();
            for (int i = 0; i < groupToSort.Length; ++i)
            {
                if (!(groupToSort[i] is AgOp) && (!(groupToSort[i] is ExNumber) || includeNumbers))
                    compareGroupList.Add(groupToSort[i]);
            }

            return compareGroupList;
        }
    }
}