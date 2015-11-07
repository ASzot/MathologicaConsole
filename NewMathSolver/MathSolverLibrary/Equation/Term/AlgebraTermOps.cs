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

        public static AlgebraTerm OpSub(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GetGroupCount() == 0)
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

        public static AlgebraTerm OpSub(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.GetTermCount() == 0)
                return term1;
            if (term1.GetTermCount() == 0)
                return term2;

            ExComp combined = Operators.SubOp.StaticCombine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpSub(AlgebraTerm term, List<AlgebraGroup> groups)
        {
            AlgebraTerm resultant = term;
            foreach (AlgebraGroup group in groups)
            {
                resultant = AlgebraTerm.OpSub(resultant, group);
            }

            return resultant;
        }

        public static AlgebraTerm OpSub(AlgebraTerm term)
        {
            ExComp negTerm = Operators.MulOp.StaticCombine(ExNumber.GetNegOne(), term);
            return negTerm.ToAlgTerm();
        }

        public static bool OpNotEqual(AlgebraTerm a1, AlgebraTerm a2)
        {
            return !(AlgebraTerm.OpEqual(a1, a2));
        }

        public static AlgebraTerm OpMul(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GetGroupCount() == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            ExComp combined = Operators.MulOp.StaticCombine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpMul(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.GetTermCount() == 0)
                return term1;
            if (term1.GetTermCount() == 0)
                return term2;

            ExComp combined = Operators.MulOp.StaticCombine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpDiv(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GetGroupCount() == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            ExComp combined = Operators.DivOp.StaticCombine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpDiv(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.GetTermCount() == 0)
                return ExNumber.GetUndefined().ToAlgTerm();
            if (term1.GetTermCount() == 0)
                return ExNumber.GetZero().ToAlgTerm();

            ExComp combined = Operators.DivOp.StaticCombine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpAdd(AlgebraTerm term, AlgebraGroup group)
        {
            if (group.GetGroupCount() == 0)
                return term;

            AlgebraTerm groupTerm = group.ToTerm();
            ExComp combined = Operators.AddOp.StaticCombine(term, groupTerm);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpAdd(AlgebraTerm term1, AlgebraTerm term2)
        {
            if (term2.GetTermCount() == 0)
                return term1;
            if (term1.GetTermCount() == 0)
                return term2;

            ExComp combined = Operators.AddOp.StaticCombine(term1, term2);
            if (combined is AlgebraTerm)
                return combined as AlgebraTerm;
            else
            {
                AlgebraTerm combinedTerm = new AlgebraTerm();
                combinedTerm.Add(combined);
                return combinedTerm;
            }
        }

        public static AlgebraTerm OpAdd(AlgebraTerm term, List<AlgebraGroup> groups)
        {
            AlgebraTerm resultant = term;
            foreach (AlgebraGroup group in groups)
            {
                resultant = AlgebraTerm.OpAdd(resultant, group);
            }

            return resultant;
        }

        public static bool OpEqual(AlgebraTerm a1, AlgebraTerm a2)
        {
            if (((object)a1) == null && ((object)a2) == null)
                return true;
            if (((object)a1) == null || ((object)a2) == null)
                return false;

            return a1.IsEqualTo(a2);
        }
    }
}