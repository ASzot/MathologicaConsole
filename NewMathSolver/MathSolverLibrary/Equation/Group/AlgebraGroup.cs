using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class AlgebraGroup
    {
        private ExComp[] _group;

        public ExComp[] GetGroup()
        {
            return _group;
        }

        public int GetGroupCount()
        {
            return _group.Length;
        }

        public void SetItem(int i, ExComp value)
        {
            _group[i] = value;
        }

        public ExComp GetItem(int i)
        {
            return _group[i];
        }

        public AlgebraGroup(ExComp[] group)
        {
            _group = group;
        }

        public AlgebraGroup GetVariableGroupComps(AlgebraComp varFor)
        {
            List<ExComp> addExComps = new List<ExComp>();

            foreach (ExComp groupComp in _group)
            {
                if (groupComp.IsEqualTo(varFor) || (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).Contains(varFor)))
                    addExComps.Add(groupComp);
            }

            return new AlgebraGroup(addExComps.ToArray());
        }

        public bool IsZero()
        {
            return _group.Length == 1 && ExNumber.GetZero().IsEqualTo(_group[0]);
        }

        public override string ToString()
        {
            string finalStr = "";
            for (int i = 0; i < _group.Length; ++i)
                finalStr += _group[i].ToString();
            return finalStr;
        }

        public static AlgebraTerm ToTerm(List<AlgebraGroup> gps)
        {
            AlgebraTerm term = new AlgebraTerm();
            foreach (AlgebraGroup gp in gps)
                term = AlgebraTerm.OpAdd(term, gp);

            return term;
        }

        public static AlgebraTerm GetConstantTo(List<AlgebraGroup> gps, AlgebraComp cmp)
        {
            AlgebraTerm[] termsArr = new AlgebraTerm[gps.Count];
            for (int i = 0; i < gps.Count; ++i)
                termsArr[i] = GroupHelper.ToAlgTerm(GroupHelper.GetUnrelatableTermsOfGroup(gps[i].GetGroup(), cmp));

            AlgebraTerm totalTerm = new AlgebraTerm();
            foreach (AlgebraTerm term in termsArr)
            {
                totalTerm = AlgebraTerm.OpAdd(totalTerm, term);
            }

            if (totalTerm.GetSubComps().Count == 0)
                return ExNumber.GetOne().ToAlgTerm();

            return totalTerm;
        }

        public bool IsEqualTo(AlgebraGroup ag)
        {
            if (this.GetGroupCount() != ag.GetGroupCount())
                return false;
            for (int i = 0; i < ag.GetGroupCount(); ++i)
            {
                if (!GetItem(i).IsEqualTo(ag.GetItem(i)))
                    return false;
            }

            return true;
        }

        public AlgebraTerm ToTerm()
        {
            AlgebraTerm term = new AlgebraTerm();
            term.AddGroup(_group);
            return term;
        }
    }
}