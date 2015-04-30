using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Term
{
    internal class SimpleFraction : TermExtension
    {
        private AlgebraTerm _den;
        private AlgebraTerm _num;

        public AlgebraTerm Den
        {
            get { return _den; }
        }

        public ExComp DenEx
        {
            get { return _den.RemoveRedundancies(); }
        }

        public AlgebraTerm Num
        {
            get { return _num; }
        }

        public ExComp NumEx
        {
            get { return _num.RemoveRedundancies(); }
        }

        public ExComp GetReciprocal()
        {
            return Operators.DivOp.StaticCombine(Den, Num);
        }

        /// <summary>
        /// Doesn't allow zero. Stricly single grouped terms with a numerator and denominator.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public bool HarshInit(AlgebraTerm term)
        {
            if (!term.ContainsOnlyFractions() || term.GroupCount != 1)
                return false;
            return Init(term);
        }

        public override bool Init(AlgebraTerm term)
        {
            if (term == null)
                throw new ArgumentException();
            if (term.IsZero())
            {
                _num = Number.Zero.ToAlgTerm();
                _den = Number.Zero.ToAlgTerm();

                return true;
            }
            term = term.RemoveRedundancies().ToAlgTerm();

            if (term.TermCount == 1)
            {
                _num = term.ToAlgTerm();
                _den = Number.One.ToAlgTerm();

                return true;
            }

            if (!term.ContainsOnlyFractions())
                return false;

            if (term.GroupCount != 1)
                return false;

            AlgebraTerm[] numDen = term.GetNumDenFrac();
            if (numDen == null)
                return false;

            _num = numDen[0];
            _den = numDen[1];

            return true;
        }

        public bool IsDenOne()
        {
            return Number.One.IsEqualTo(DenEx);
        }

        public bool IsSimpleUnitCircleAngle(out Number num, out Number den, bool handleNegs = true)
        {
            num = null;
            den = null;

            if (NumEx is Number && (NumEx as Number) == 0.0)
            {
                num = Number.Zero;
                den = Number.Zero;
                return true;
            }

            if (!(DenEx is Number))
                return false;

            if (!Num.Contains(Constant.ParseConstant(@"pi")))
                return false;

            List<ExComp[]> numGroups = Num.GetGroupsNoOps();
            if (numGroups.Count != 1)
                return false;
            ExComp[] numGroup = numGroups[0];

            if (numGroup.Length == 2)
            {
                ExComp first = numGroup[0];
                ExComp second = numGroup[1];

                ExComp piConstant = first is Constant ? first : second;
                ExComp otherEx = first is Constant ? second : first;

                if (!(otherEx is Number))
                    return false;

                num = otherEx as Number;
            }
            else if (numGroup.Length == 1)
                num = Number.One;
            else
                return false;

            den = DenEx as Number;

            if (!num.IsRealInteger() || !den.IsRealInteger())
                return false;

            Number doubleDen = den * 2.0;
            bool isNeg = false;
            if (num < 0.0)
            {
                num *= -1;
                isNeg = true;
            }
            if (doubleDen <= num)
            {
                num = num % doubleDen;
                if (num == 0.0)
                    den = new Number(0.0);
            }

            if (isNeg && handleNegs)
            {
                Number numSub = den * 2.0;
                num = numSub - num;
            }

            return true;
        }

        public bool LooseInit(AlgebraTerm term)
        {
            if (term == null)
                return false;
            if (term.IsZero())
            {
                _num = Number.Zero.ToAlgTerm();
                _den = Number.Zero.ToAlgTerm();

                return true;
            }

            if (term.ContainsOnlyFractions())
            {
                if (term.GroupCount == 1)
                {
                    AlgebraTerm[] numDen = term.GetNumDenFrac();
                    if (numDen != null)
                    {
                        _num = numDen[0];
                        _den = numDen[1];
                        return true;
                    }
                }
            }

            if (term.GroupCount == 1)
            {
                _num = term.ToAlgTerm();
                _den = Number.One.ToAlgTerm();

                return true;
            }

            return false;
        }
    }
}