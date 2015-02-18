using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    partial class AlgebraTerm
    {
        public ExComp this[int i]
        {
            get
            {
                return _subComps[i];
            }
            set
            {
                _subComps[i] = value;
            }
        }

        public static AlgebraTerm operator -(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GroupCount == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            ExComp combined = Operators.SubOp.StaticCombine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator -(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.TermCount == 0)
                return term1;
            if (term1.TermCount == 0)
                return term2;

            var op = new Operators.SubOp();
            ExComp combined = op.Combine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator -(AlgebraTerm term, List<AlgebraGroup> groups)
        {
            AlgebraTerm resultant = term;
            foreach (AlgebraGroup group in groups)
            {
                resultant = resultant - group;
            }

            return resultant;
        }

        public static AlgebraTerm operator -(AlgebraTerm term)
        {
            ExComp negTerm = Operators.MulOp.StaticCombine(Number.NegOne, term);
            return negTerm.ToAlgTerm();
        }

        public static bool operator !=(AlgebraTerm a1, AlgebraTerm a2)
        {
            return !(a1 == a2);
        }

        public static AlgebraTerm operator *(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GroupCount == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            var op = new Operators.MulOp();
            ExComp combined = op.Combine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator *(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.TermCount == 0)
                return term1;
            if (term1.TermCount == 0)
                return term2;

            var op = new Operators.MulOp();
            ExComp combined = op.Combine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator /(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GroupCount == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            var op = new Operators.DivOp();
            ExComp combined = op.Combine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator /(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.TermCount == 0)
                return Number.Undefined.ToAlgTerm();
            if (term1.TermCount == 0)
                return Number.Zero.ToAlgTerm();

            var op = new Operators.DivOp();
            ExComp combined = op.Combine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator +(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GroupCount == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            var op = new Operators.AddOp();
            ExComp combined = op.Combine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator +(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.TermCount == 0)
                return term1;
            if (term1.TermCount == 0)
                return term2;

            var op = new Operators.AddOp();
            ExComp combined = op.Combine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm operator +(AlgebraTerm term, List<AlgebraGroup> groups)
        {
            AlgebraTerm resultant = term;
            foreach (AlgebraGroup group in groups)
            {
                resultant = resultant + group;
            }

            return resultant;
        }

        public static bool operator ==(AlgebraTerm a1, AlgebraTerm a2)
        {
            if (((object)a1) == null && ((object)a2) == null)
                return true;
            if (((object)a1) == null || ((object)a2) == null)
                return false;

            return a1.IsEqualTo(a2);
        }
    }
}