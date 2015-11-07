using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Term
{
    internal class SimpleFraction : TermExtension
    {
        private AlgebraTerm _den;
        private AlgebraTerm _num;

        public AlgebraTerm GetDen()
        {
            return _den;
        }

        public ExComp GetDenEx()
        {
            return _den.RemoveRedundancies(false);
        }

        public AlgebraTerm GetNum()
        {
            return _num;
        }

        public ExComp GetNumEx()
        {
            return _num.RemoveRedundancies(false);
        }

        public ExComp GetReciprocal()
        {
            return Operators.DivOp.StaticCombine(GetDen(), GetNum());
        }

        /// <summary>
        /// Doesn't allow zero. Stricly single grouped terms with a numerator and denominator.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public bool HarshInit(AlgebraTerm term)
        {
            if (!term.ContainsOnlyFractions() || term.GetGroupCount() != 1)
                return false;
            return Init(term);
        }

        public override bool Init(AlgebraTerm term)
        {
            if (term == null)
                throw new ArgumentException();
            if (term.IsZero())
            {
                _num = ExNumber.GetZero().ToAlgTerm();
                _den = ExNumber.GetZero().ToAlgTerm();

                return true;
            }
            term = term.RemoveRedundancies(false).ToAlgTerm();

            if (term.GetTermCount() == 1)
            {
                if (term is PowerFunction && (term as PowerFunction).GetPower().IsEqualTo(ExNumber.GetNegOne()))
                {
                    _num = ExNumber.GetOne().ToAlgTerm();
                    _den = (term as PowerFunction).GetBase().ToAlgTerm();
                }
                else
                {
                    _num = term.ToAlgTerm();
                    _den = ExNumber.GetOne().ToAlgTerm();
                }

                return true;
            }

            if (!term.ContainsOnlyFractions())
                return false;

            if (term.GetGroupCount() != 1)
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
            return ExNumber.GetOne().IsEqualTo(GetDenEx());
        }

        public bool IsSimpleUnitCircleAngle(out ExNumber num, out ExNumber den, bool handleNegs)
        {
            num = null;
            den = null;

            if (GetNumEx() is ExNumber && ExNumber.OpEqual((GetNumEx() as ExNumber), 0.0))
            {
                num = ExNumber.GetZero();
                den = ExNumber.GetZero();
                return true;
            }

            if (!(GetDenEx() is ExNumber))
                return false;

            if (!GetNum().Contains(Constant.ParseConstant(@"pi")))
                return false;

            System.Collections.Generic.List<ExComp[]> numGroups = GetNum().GetGroupsNoOps();
            if (numGroups.Count != 1)
                return false;
            ExComp[] numGroup = numGroups[0];

            if (numGroup.Length == 2)
            {
                ExComp first = numGroup[0];
                ExComp second = numGroup[1];

                ExComp piConstant = first is Constant ? first : second;
                ExComp otherEx = first is Constant ? second : first;

                if (!(otherEx is ExNumber))
                    return false;

                num = otherEx as ExNumber;
            }
            else if (numGroup.Length == 1)
                num = ExNumber.GetOne();
            else
                return false;

            den = GetDenEx() as ExNumber;

            if (!num.IsRealInteger() || !den.IsRealInteger())
                return false;

            ExNumber doubleDen = ExNumber.OpMul(den, 2.0);
            bool isNeg = false;
            if (ExNumber.OpLT(num, 0.0))
            {
                num = ExNumber.OpMul(num, -1.0);
                isNeg = true;
            }
            if (ExNumber.OpLE(doubleDen, num))
            {
                num = ExNumber.OpMod(num, doubleDen);
                if (ExNumber.OpEqual(num, 0.0))
                    den = new ExNumber(0.0);
            }

            if (isNeg && handleNegs)
            {
                ExNumber numSub = ExNumber.OpMul(den, 2.0);
                num = ExNumber.OpSub(numSub, num);
            }

            return true;
        }

        public bool LooseInit(AlgebraTerm term)
        {
            if (term == null)
                return false;
            if (term.IsZero())
            {
                _num = ExNumber.GetZero().ToAlgTerm();
                _den = ExNumber.GetZero().ToAlgTerm();

                return true;
            }

            if (term.ContainsOnlyFractions())
            {
                if (term.GetGroupCount() == 1)
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

            if (term.GetGroupCount() == 1)
            {
                _num = term.ToAlgTerm();
                _den = ExNumber.GetOne().ToAlgTerm();

                return true;
            }

            return false;
        }
    }
}