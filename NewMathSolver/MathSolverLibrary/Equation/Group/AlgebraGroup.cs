using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class AlgebraGroup
    {
        private ExComp[] _group;

        public ExComp[] Group
        {
            get { return _group; }
        }

        public int GroupCount
        {
            get { return _group.Count(); }
        }

        public ExComp this[int i]
        {
            get { return _group[i]; }
            set { _group[i] = value; }
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
            return _group.Length == 1 && Number.Zero.IsEqualTo(_group[0]);
        }

        public override string ToString()
        {
            string finalStr = "";
            for (int i = 0; i < _group.Count(); ++i)
                finalStr += _group[i].ToString();
            return finalStr;
        }

        public AlgebraTerm ToTerm()
        {
            AlgebraTerm term = new AlgebraTerm();
            term.AddGroup(_group);
            return term;
        }
    }
}