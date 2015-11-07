using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class LoosePolyInfo
    {
        private List<TypePair<ExComp, int>> _info;
        private AlgebraComp _var;

        public List<TypePair<ExComp, int>> GetInfo()
        {
            return _info;
        }

        public int GetTermCount()
        {
            return _info.Count;
        }

        public AlgebraComp GetVar()
        {
            return _var;
        }

        public LoosePolyInfo(List<TypePair<ExComp, int>> polyInfo, AlgebraComp var)
        {
            _info = polyInfo;
            _var = var;
        }

        public LoosePolyInfo Clone()
        {
            List<TypePair<ExComp, int>> info = new List<TypePair<ExComp, int>>();

            foreach (TypePair<ExComp, int> tp in GetInfo())
            {
                info.Add(new TypePair<ExComp, int>(tp.GetData1().CloneEx(), tp.GetData2()));
            }

            return new LoosePolyInfo(info, (AlgebraComp)_var.CloneEx());
        }

        public void FillInPowRanges()
        {
            if (_info.Count > 1)
            {
                int min = int.MaxValue, max = int.MinValue;
                foreach (TypePair<ExComp, int> info in _info)
                {
                    min = Math.Min(min, info.GetData2());
                    max = Math.Max(max, info.GetData2());
                }

                // Add all the powers between min and max.
                for (int i = min + 1; i < max; ++i)
                {
                    if (HasPower(i))
                        continue;

                    TypePair<ExComp, int> newInfo = new TypePair<ExComp, int>(ExNumber.GetZero(), i);
                    _info.Add(newInfo);
                }
            }
        }

        public ExComp GetCoeffForPow(int pow)
        {
            foreach (TypePair<ExComp, int> info in _info)
            {
                if (info.GetData2() == pow)
                    return info.GetData1();
            }

            return null;
        }

        public LoosePolyInfo GetNeg()
        {
            List<TypePair<ExComp, int>> info = new List<TypePair<ExComp, int>>();

            foreach (TypePair<ExComp, int> tp in GetInfo())
            {
                info.Add(new TypePair<ExComp, int>(MulOp.Negate(tp.GetData1().CloneEx()), tp.GetData2()));
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
                    if (info.GetData2() == pow)
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
                if (info.GetData2() == pow)
                    return true;
            }

            return false;
        }

        public void RemovePowCoeffPair(int pow)
        {
            for (int i = 0; i < _info.Count; ++i)
            {
                int comparePow = _info[i].GetData2();
                if (comparePow == pow)
                {
                    ArrayFunc.RemoveIndex(_info, i);
                    return;
                }
            }
        }

        public void SetCoeffForPow(int pow, ExComp setVal)
        {
            for (int i = 0; i < _info.Count; ++i)
            {
                if (_info[i].GetData2() == pow)
                {
                    _info[i].SetData1(setVal);
                    break;
                }
            }
        }

        public void ShiftPowers(int shift)
        {
            for (int i = 0; i < _info.Count; ++i)
            {
                _info[i].SetData2(_info[i].GetData2() + shift);
            }
        }

        public string ToMathAsciiStr()
        {
            if (GetTermCount() == 0)
                return "0";

            List<TypePair<ExComp, int>> orderedInfo = ArrayFunc.OrderList(_info);

            ArrayFunc.Reverse(orderedInfo);

            string finalStr = "";
            for (int i = 0; i < orderedInfo.Count; ++i)
            {
                TypePair<ExComp, int> termInfo = orderedInfo[i];
                string addStr = (MulOp.StaticCombine(termInfo.GetData1(), PowOp.StaticCombine(_var, new ExNumber(termInfo.GetData2())))).ToAsciiString();
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
        private List<TypePair<ExNumber, int>> _info;
        private AlgebraComp _var;

        public List<TypePair<ExNumber, int>> GetInfo()
        {
            return _info;
        }

        public int GetTermCount()
        {
            return _info.Count;
        }

        public AlgebraComp GetVar()
        {
            return _var;
        }

        public PolyInfo(List<TypePair<ExNumber, int>> polyInfo, AlgebraComp var)
        {
            _info = polyInfo;
            _var = var;
        }

        public void FillInPowRanges()
        {
            if (_info.Count > 1)
            {
                int min = int.MaxValue, max = int.MinValue;
                foreach (TypePair<ExNumber, int> info in _info)
                {
                    min = Math.Min(min, info.GetData2());
                    max = Math.Max(max, info.GetData2());
                }

                // Add all the powers between min and max.
                for (int i = min + 1; i < max; ++i)
                {
                    if (HasPower(i))
                        continue;

                    TypePair<ExNumber, int> newInfo = new TypePair<ExNumber, int>(ExNumber.GetZero(), i);
                    _info.Add(newInfo);
                }
            }
        }

        public ExNumber GetCoeffForPow(int pow)
        {
            foreach (TypePair<ExNumber, int> info in _info)
            {
                if (info.GetData2() == pow)
                    return info.GetData1();
            }

            return null;
        }

        public int GetMaxPow()
        {
            int max = -1;
            foreach (TypePair<ExNumber, int> infoPair in GetInfo())
            {
                max = Math.Max(infoPair.GetData2(), max);
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
                foreach (TypePair<ExNumber, int> info in _info)
                {
                    if (info.GetData2() == pow)
                        found = true;
                }
                if (!found)
                    return false;
            }

            return true;
        }

        public bool HasPower(int pow)
        {
            foreach (TypePair<ExNumber, int> info in _info)
            {
                if (info.GetData2() == pow)
                    return true;
            }

            return false;
        }
    }
}