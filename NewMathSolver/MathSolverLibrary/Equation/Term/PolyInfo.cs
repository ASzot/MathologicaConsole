using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class LoosePolyInfo
    {
        private List<TypePair<ExComp, int>> _info;
        private AlgebraComp _var;

        public List<TypePair<ExComp, int>> Info
        {
            get { return _info; }
        }

        public int TermCount
        {
            get { return _info.Count; }
        }

        public AlgebraComp Var
        {
            get { return _var; }
        }

        public LoosePolyInfo(List<TypePair<ExComp, int>> polyInfo, AlgebraComp var)
        {
            _info = polyInfo;
            _var = var;
        }

        public LoosePolyInfo Clone()
        {
            List<TypePair<ExComp, int>> info = new List<TypePair<ExComp, int>>();

            foreach (TypePair<ExComp, int> tp in Info)
            {
                info.Add(new TypePair<ExComp, int>(tp.Data1.Clone(), tp.Data2));
            }

            return new LoosePolyInfo(info, (AlgebraComp)_var.Clone());
        }

        public void FillInPowRanges()
        {
            if (_info.Count > 1)
            {
                int min = int.MaxValue, max = int.MinValue;
                foreach (TypePair<ExComp, int> info in _info)
                {
                    min = Math.Min(min, info.Data2);
                    max = Math.Max(max, info.Data2);
                }

                // Add all the powers between min and max.
                for (int i = min + 1; i < max; ++i)
                {
                    if (HasPower(i))
                        continue;

                    TypePair<ExComp, int> newInfo = new TypePair<ExComp, int>(Number.Zero, i);
                    _info.Add(newInfo);
                }
            }
        }

        public ExComp GetCoeffForPow(int pow)
        {
            foreach (TypePair<ExComp, int> info in _info)
            {
                if (info.Data2 == pow)
                    return info.Data1;
            }

            return null;
        }

        public LoosePolyInfo GetNeg()
        {
            List<TypePair<ExComp, int>> info = new List<TypePair<ExComp, int>>();

            foreach (TypePair<ExComp, int> tp in Info)
            {
                info.Add(new TypePair<ExComp, int>(MulOp.Negate(tp.Data1.Clone()), tp.Data2));
            }

            return new LoosePolyInfo(info, (AlgebraComp)_var);
        }

        public bool HasOnlyPowers(params double[] powers)
        {
            if (powers.Length != _info.Count)
                return false;

            foreach (int pow in powers)
            {
                bool found = false;
                foreach (TypePair<ExComp, int> info in _info)
                {
                    if (info.Data2 == pow)
                        found = true;
                }
                if (!found)
                    return false;
            }

            return true;
        }

        public bool HasPower(int pow)
        {
            foreach (TypePair<ExComp, int> info in _info)
            {
                if (info.Data2 == pow)
                    return true;
            }

            return false;
        }

        public void RemovePowCoeffPair(int pow)
        {
            for (int i = 0; i < _info.Count; ++i)
            {
                int comparePow = _info[i].Data2;
                if (comparePow == pow)
                {
                    _info.RemoveAt(i);
                    return;
                }
            }
        }

        public void SetCoeffForPow(int pow, ExComp setVal)
        {
            for (int i = 0; i < _info.Count; ++i)
            {
                if (_info[i].Data2 == pow)
                {
                    _info[i].Data1 = setVal;
                    break;
                }
            }
        }

        public void ShiftPowers(int shift)
        {
            for (int i = 0; i < _info.Count; ++i)
            {
                _info[i].Data2 += shift;
            }
        }

        public string ToMathAsciiStr()
        {
            if (TermCount == 0)
                return "0";

            var orderedInfo = (from termInfo in _info
                               orderby termInfo.Data2
                               select termInfo).ToList();

            orderedInfo.Reverse();

            string finalStr = "";
            for (int i = 0; i < orderedInfo.Count; ++i)
            {
                var termInfo = orderedInfo[i];
                string addStr = (MulOp.StaticCombine(termInfo.Data1, PowOp.StaticCombine(_var, new Number(termInfo.Data2)))).ToAsciiString();
                if (addStr == "0")
                    addStr = "0" + _var.ToAsciiString();
                finalStr += addStr;
                if (i != orderedInfo.Count - 1)
                    finalStr += "+";
            }

            return finalStr;
        }
    }

    internal class PolyInfo
    {
        private List<TypePair<Number, int>> _info;
        private AlgebraComp _var;

        public List<TypePair<Number, int>> Info
        {
            get { return _info; }
        }

        public int TermCount
        {
            get { return _info.Count; }
        }

        public AlgebraComp Var
        {
            get { return _var; }
        }

        public PolyInfo(List<TypePair<Number, int>> polyInfo, AlgebraComp var)
        {
            _info = polyInfo;
            _var = var;
        }

        public void FillInPowRanges()
        {
            if (_info.Count > 1)
            {
                int min = int.MaxValue, max = int.MinValue;
                foreach (var info in _info)
                {
                    min = Math.Min(min, info.Data2);
                    max = Math.Max(max, info.Data2);
                }

                // Add all the powers between min and max.
                for (int i = min + 1; i < max; ++i)
                {
                    if (HasPower(i))
                        continue;

                    var newInfo = new TypePair<Number, int>(Number.Zero, i);
                    _info.Add(newInfo);
                }
            }
        }

        public Number GetCoeffForPow(int pow)
        {
            foreach (var info in _info)
            {
                if (info.Data2 == pow)
                    return info.Data1;
            }

            return null;
        }

        public int GetMaxPow()
        {
            int max = -1;
            foreach (var infoPair in Info)
            {
                max = Math.Max(infoPair.Data2, max);
            }

            return max;
        }

        public bool HasOnlyPowers(params int[] powers)
        {
            if (powers.Length != _info.Count)
                return false;

            foreach (int pow in powers)
            {
                bool found = false;
                foreach (var info in _info)
                {
                    if (info.Data2 == pow)
                        found = true;
                }
                if (!found)
                    return false;
            }

            return true;
        }

        public bool HasPower(int pow)
        {
            foreach (var info in _info)
            {
                if (info.Data2 == pow)
                    return true;
            }

            return false;
        }
    }
}