using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Term
{
    internal class PolynomialExt : TermExtension
    {
        private LoosePolyInfo _info;
        private int i_maxPow;

        public ExComp GetConstantCoeff()
        {
            return _info.GetCoeffForPow(0);
        }

        public LoosePolyInfo GetInfo()
        {
            return _info;
        }

        public ExComp GetLeadingCoeff()
        {
            return _info.GetCoeffForPow(i_maxPow);
        }

        public int GetMaxPow()
        {
            return i_maxPow;
        }

        public PolynomialExt()
        {
        }

        public PolynomialExt(LoosePolyInfo polyInfo, int maxPow)
        {
            _info = polyInfo;
            i_maxPow = maxPow;
        }

        public PolynomialExt AttemptSynthDiv(ExComp root, out List<ExComp> muls, out List<ExComp> results)
        {
            muls = new List<ExComp>();
            results = new List<ExComp>();

            ExComp prev = GetLeadingCoeff();

            List<ExComp> resultCoeffs = new List<ExComp>();

            for (int i = i_maxPow - 1; i >= 0; --i)
            {
                resultCoeffs.Add(prev);
                ExComp coeff = _info.GetCoeffForPow(i);

                ExComp rootMul = MulOp.StaticCombine(root, prev);
                muls.Add(rootMul.CloneEx());
                ExComp result = AddOp.StaticCombine(coeff, rootMul);
                results.Add(result.CloneEx());
                prev = result;
            }
            resultCoeffs.Add(prev);

            List<TypePair<ExComp, int>> coeffPowPairs = new List<TypePair<ExComp, int>>();
            for (int i = resultCoeffs.Count - 1; i >= 0; --i)
            {
                // The index of the coefficient must be 'switched' to make this work. Like coming from the other direction.
                TypePair<ExComp, int> pair = new TypePair<ExComp, int>(resultCoeffs[resultCoeffs.Count - i - 1], i);
                coeffPowPairs.Add(pair);
            }

            LoosePolyInfo polyInfo = new LoosePolyInfo(coeffPowPairs, _info.GetVar());

            ExComp resultingPow = polyInfo.GetCoeffForPow(0);
            if (ExNumber.GetZero().IsEqualTo(resultingPow))
            {
                polyInfo.RemovePowCoeffPair(0);
                polyInfo.ShiftPowers(-1);
                return new PolynomialExt(polyInfo, i_maxPow - 1);
            }

            return new PolynomialExt(polyInfo, i_maxPow);
        }

        public PolynomialExt Clone()
        {
            return new PolynomialExt(_info.Clone(), i_maxPow);
        }

        public IEnumerable<ExComp> GetCoeffs()
        {
            List<ExComp> coeffs = new List<ExComp>();
            for (int i = 0; i <= i_maxPow; ++i)
            {
                ExComp coeff = _info.GetCoeffForPow(i);
                if (coeff == null)
                    coeffs.Add(ExNumber.GetZero());
                else
                    coeffs.Add(coeff);
            }

            return coeffs;
        }

        public List<ExComp> GetRationalPossibleRoots()
        {
            if (!(GetLeadingCoeff() is ExNumber) || !(GetConstantCoeff() is ExNumber))
                return null;

            ExNumber a = GetLeadingCoeff() as ExNumber;
            ExNumber nConst = GetConstantCoeff() as ExNumber;

            if (a.IsRealInteger() && nConst.IsRealInteger())
            {
                int iA = (int)a.GetRealComp();
                int iConst = (int)nConst.GetRealComp();

                iA = Math.Abs(iA);
                iConst = Math.Abs(iConst);

                int[] aDivs = MathHelper.GetDivisors(iA, true, true);
                int[] constDivs = MathHelper.GetDivisors(iConst, true, true);

                List<ExComp> posRoots = new List<ExComp>();

                foreach (int constDiv in constDivs)
                {
                    foreach (int aDiv in aDivs)
                    {
                        ExComp posRoot = Operators.DivOp.StaticCombine(new ExNumber(constDiv), new ExNumber(aDiv));
                        posRoots.Add(posRoot);
                    }
                }

                // Also inlcude the negative versions.
                int posRootsCount = posRoots.Count;
                for (int i = 0; i < posRootsCount; ++i)
                {
                    ExComp posRoot = posRoots[i];
                    ExComp negPosRoot = Operators.MulOp.Negate(posRoot);

                    posRoots.Add(negPosRoot);
                }

                return GroupHelper.RemoveDuplicates(posRoots);
            }

            return null;
        }

        public override bool Init(AlgebraTerm term)
        {
            _info = term.GetLoosePolyInfo();

            if (!ComputeMaxPow())
                return false;

            return _info != null;
        }

        public bool InitLPI(LoosePolyInfo lpi)
        {
            _info = lpi;

            return ComputeMaxPow();
        }

        public AlgebraTerm ToAlgTerm()
        {
            AlgebraTerm finalTerm = new AlgebraTerm();

            foreach (TypePair<ExComp, int> coeffPow in _info.GetInfo())
            {
                if (ExNumber.GetZero().IsEqualTo(coeffPow.GetData1()))
                    continue;

                if (coeffPow.GetData2() == 0.0)
                {
                    finalTerm.AddGroup(coeffPow.GetData1());
                    continue;
                }

                if (coeffPow.GetData2() == 1.0)
                {
                    finalTerm.AddGroup(MulOp.StaticCombine(coeffPow.GetData1(), _info.GetVar()));
                    continue;
                }

                ExComp varPow = new Functions.PowerFunction(_info.GetVar(), new ExNumber(coeffPow.GetData2()));
                finalTerm.AddGroup(MulOp.StaticCombine(coeffPow.GetData1(), varPow));
            }

            return finalTerm;
        }

        public string ToMathAsciiStr()
        {
            return _info.ToMathAsciiStr();
        }

        private bool ComputeMaxPow()
        {
            if (_info != null)
            {
                i_maxPow = int.MinValue;
                for (int i = 0; i < _info.GetInfo().Count; ++i)
                {
                    int pow = _info.GetInfo()[i].GetData2();
                    if (pow < 0.0)
                        return false;
                    i_maxPow = Math.Max(i_maxPow, pow);
                }

                _info.FillInPowRanges();

                return true;
            }

            return false;
        }
    }
}