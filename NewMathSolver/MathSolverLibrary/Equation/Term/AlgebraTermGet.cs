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
                if (subComp is AlgebraComp)
                {
                    varStrs.Add((subComp as AlgebraComp).Var.Var);
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
                if (group.GroupContains(term))
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
                    if (gpCmp is Functions.PowerFunction && (gpCmp as Functions.PowerFunction).Power.IsEqualTo(power))
                    {
                        matchingTerms.Add(gp.ToAlgTerm());
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
                        Number coeff = GetCoeffTerm(group);
                        if (coeff == null)
                        {
                            coeff = new Number(1.0);
                            group.Add(new Operators.MulOp());
                            group.Add(coeff);
                        }
                        coeff = coeff * new Number(-1.0f);
                    }

                    groups.Add(group.ToArray());
                }
            }

            return groups;
        }

        public List<AlgebraGroup> GetGroupsConstantTo(AlgebraComp varFor)
        {
			List<ExComp[]> groups = GetGroups();

            IEnumerable<AlgebraGroup> constantGroups = from gp in groups
													   where !gp.GroupContains(varFor)
													   select new AlgebraGroup(gp);

            return constantGroups.ToList();
        }

        public List<ExComp[]> GetGroupsNoOps()
        {
            List<ExComp[]> groups = GetGroups();

            for (int i = 0; i < groups.Count; ++i)
            {
                groups[i] = groups[i].RemoveOperators().RemoveRedundancies();
            }

            return groups;
        }

        public List<AlgebraGroup> GetGroupsVariableTo(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroups();

            IEnumerable<AlgebraGroup> variableGroups = from gp in groups
                                 where gp.GroupContains(varFor)
                                 select new AlgebraGroup(gp);

            return variableGroups.ToList();
        }

        public List<AlgebraGroup> GetGroupsVariableToNoOps(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            IEnumerable<AlgebraGroup> variableGroups = from gp in groups
                                 where gp.GroupContains(varFor)
                                 select new AlgebraGroup(gp);

            return variableGroups.ToList();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = GroupCount ^ TermCount;
                foreach (ExComp subComp in _subComps)
                {
                    hash *= subComp.GetHashCode();
                }

                return hash;
            }
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
            var groups = GetGroupsNoOps();
            if (groups.Count != 1)
                return null;

            var group = groups[0];
            if (!group.ContainsFrac())
                return null;
            ExComp[] num = group.GetNumerator();
            ExComp[] den = group.GetDenominator();

            if (den.Length == 0)
                return null;

            AlgebraTerm[] numDenTerm = { num.ToAlgTerm(), den.ToAlgTerm() };
            return numDenTerm;
        }

        public List<AlgebraGroup> GetVariableFractionGroups(AlgebraComp varFor)
        {
            List<AlgebraGroup> varFracGroups = new List<AlgebraGroup>();

            var groups = GetGroupsNoOps();

            foreach (var group in groups)
            {
                if (group.ContainsFrac())
                {
                    var denTerm = group.GetDenominator().ToAlgTerm();

                    if (denTerm.Contains(varFor))
                    {
                        varFracGroups.Add(new AlgebraGroup(group));
                    }
                }
            }

            return varFracGroups;
        }

        private static IEnumerable<ExComp> GetCombinableGroup(IEnumerable<ExComp> groupToSort,
            bool includeNumbers = false)
        {
            //groupToSort = groupToSort.ToArray().RemoveRedundancies();
            var compareGroup = from comp in groupToSort
                               where !(comp is AgOp)
                               where !(comp is Number) || includeNumbers
                               select comp;
            return compareGroup;
        }
    }
}