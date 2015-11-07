using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    partial class AlgebraTerm
    {
        public static ExNumber GetCoeffTerm(ExComp[] group)
        {
            foreach (ExComp comp in group)
            {
                if (comp is ExNumber)
                    return comp as ExNumber;
            }

            return null;
        }

        public static ExNumber GetCoeffTerm(List<ExComp> comps)
        {
            foreach (ExComp comp in comps)
            {
                if (comp is ExNumber)
                    return comp as ExNumber;
            }
            return null;
        }

        public virtual bool Contains(AlgebraComp varFor)
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraTerm && (subComp as AlgebraTerm).Contains(varFor))
                    return true;
                if (subComp is AlgebraComp && (subComp as AlgebraComp).IsEqualTo(varFor))
                    return true;
            }

            return false;
        }

        public bool ContainsFractions()
        {
            List<ExComp[]> groups = GetGroups();

            foreach (ExComp[] group in groups)
            {
                if (GroupHelper.ContainsFrac(group))
                    return true;
            }

            return false;
        }

        public bool ContainsOnlyFractions()
        {
            List<ExComp[]> groups = GetGroups();

            foreach (ExComp[] group in groups)
            {
                if (!GroupHelper.ContainsFrac(group))
                    return false;
            }

            return true;
        }

        public int GetAppliedFuncCount(AlgebraComp varFor, FunctionType type)
        {
            List<FunctionType> appliedFuncs = GetAppliedFunctionsNoPow(varFor);
            int count = 0;
            for (int i = 0; i < appliedFuncs.Count; ++i)
            {
                if (appliedFuncs[i] == type)
                    count++;
            }

            return count;
        }

        public virtual List<FunctionType> GetAppliedFunctionsNoPow(AlgebraComp varFor)
        {
            List<FunctionType> appliedFuncs = new List<FunctionType>();
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraFunction)
                {
                    if (subComp is PowerFunction)
                    {
                        PowerFunction powFunc = subComp as PowerFunction;
                        if ((powFunc.GetPower() is AlgebraTerm && (powFunc.GetPower() as AlgebraTerm).Contains(varFor)) ||
                            (powFunc.GetPower() is AlgebraComp && powFunc.GetPower().IsEqualTo(varFor)))
                            appliedFuncs.Add(FunctionType.Exponential);
                        else if (powFunc.GetBase() is TrigFunction)
                        {
                            appliedFuncs.Add(FunctionType.Sinusodal);
                        }
                    }
                    else if (subComp is AbsValFunction)
                    {
                        AbsValFunction absValFunc = subComp as AbsValFunction;

                        if (varFor.IsEqualTo(absValFunc.GetInnerEx()) || absValFunc.Contains(varFor))
                            appliedFuncs.Add(FunctionType.AbsoluteValue);
                    }
                    else if (subComp is LogFunction)
                    {
                        LogFunction logFunc = subComp as LogFunction;

                        if (logFunc.GetInnerTerm().Contains(varFor))
                            appliedFuncs.Add(FunctionType.Logarithm);
                        if (logFunc.GetBase().IsEqualTo(varFor) || (logFunc.GetBase() is AlgebraTerm && (logFunc.GetBase() as AlgebraTerm).Contains(varFor)))
                            appliedFuncs.Add(FunctionType.LogarithmBase);
                    }
                    else if (subComp is TrigFunction)
                    {
                        TrigFunction trigFunc = subComp as TrigFunction;
                        if (trigFunc.Contains(varFor))
                            appliedFuncs.Add(FunctionType.Sinusodal);
                    }
                    else if (subComp is InverseTrigFunction)
                    {
                        InverseTrigFunction inverseTrigFunc = subComp as InverseTrigFunction;
                        if (inverseTrigFunc.Contains(varFor))
                            appliedFuncs.Add(FunctionType.InverseSinusodal);
                    }
                }
                else if (subComp is AlgebraTerm)
                {
                    List<FunctionType> subAppliedFuncs = (subComp as AlgebraTerm).GetAppliedFunctionsNoPow(varFor);
                    appliedFuncs.AddRange(subAppliedFuncs);
                }
            }

            return appliedFuncs;
        }

        /// <summary>
        /// Gets the coefficient of the variable in the expression.
        /// Will not return null.
        /// </summary>
        /// <param name="varFor"></param>
        /// <returns></returns>
        public ExComp GetCoeffOfVar(AlgebraComp varFor)
        {
            if (!Contains(varFor))
                return null;

            if (GetTermCount() == 1)
            {
                return ExNumber.GetOne();
            }

            List<ExComp[]> groups = GetGroupsNoOps();

            ExComp[][] unrelatedGroupsArr = new ExComp[groups.Count][];
            for (int i = 0; i < groups.Count; ++i)
            {
                if (GroupHelper.ToAlgTerm(groups[i]).Contains(varFor))
                    unrelatedGroupsArr[i] = GroupHelper.GetUnrelatableTermsOfGroup(groups[i], varFor);
            }

            // Combine all of the unrelated terms.
            AlgebraTerm unrelatedTerm = new AlgebraTerm(unrelatedGroupsArr);
            if (unrelatedTerm.GetGroupCount() > 1)
            {
                unrelatedTerm = unrelatedTerm.ApplyOrderOfOperations();
                unrelatedTerm = unrelatedTerm.MakeWorkable().ToAlgTerm();
            }

            return unrelatedTerm;
        }

        public virtual List<ExNumber> GetCoeffs()
        {
            List<ExComp[]> groups = GetGroups();

            List<ExNumber> coeffsList = new List<ExNumber>();
            for (int i = 0; i < groups.Count; ++i)
                coeffsList.Add(GetCoeffTerm(groups[i]));

            return coeffsList;
        }

        public int GetComplexityOfVar(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            int complexity = 0;
            foreach (ExComp[] group in groups)
            {
                if (group.Length == 1 && group[0].IsEqualTo(varFor))
                {
                    complexity += 1;
                    continue;
                }
                else if (group.Length == 2)
                {
                    ExComp gpc0 = group[0];
                    ExComp gpc1 = group[1];

                    if (varFor.IsEqualTo(gpc0) || varFor.IsEqualTo(gpc1))
                    {
                        ExComp varForEx = varFor.IsEqualTo(gpc0) ? gpc0 : gpc1;
                        ExComp otherEx = varFor.IsEqualTo(gpc0) ? gpc1 : gpc0;

                        if (otherEx is ExNumber && ExNumber.GetNegOne().IsEqualTo(otherEx))
                        {
                            complexity += 1;
                            continue;
                        }

                        complexity += 2;
                    }
                }
                else
                {
                    foreach (ExComp groupComp in group)
                    {
                        if (groupComp.IsEqualTo(varFor))
                        {
                            // We have coefficients surrounding the term.
                            complexity += 2;
                        }
                        else if (groupComp is AlgebraFunction && (groupComp as AlgebraFunction).Contains(varFor))
                        {
                            complexity += 4;
                        }
                        else if (groupComp is AlgebraTerm && (groupComp as AlgebraTerm).Contains(varFor))
                        {
                            complexity += 3;
                        }
                    }
                }
            }

            return complexity;
        }

        public double GetHighestDegree()
        {
            double highestDegree = 0;
            foreach (ExComp comp in _subComps)
            {
                if (comp is AgOp)
                    continue;
                highestDegree = Math.Max(highestDegree, comp.GetCompareVal());
            }

            return highestDegree;
        }

        public LoosePolyInfo GetLoosePolyInfo()
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            if (groups.Count == 0)
                return null;

            List<TypePair<ExComp, int>> polyInfo = new List<TypePair<ExComp, int>>();

            AlgebraComp polyVar = null;
            foreach (ExComp[] group in groups)
            {
                if (group.Length > 3)
                    return null;

                AlgebraComp variable = null;
                ExComp coeff = new ExNumber(1.0);
                int pow = -1;
                foreach (ExComp groupComp in group)
                {
                    if (groupComp is AlgebraComp)
                    {
                        pow = 1;
                        variable = groupComp as AlgebraComp;
                        if (polyVar == null)
                            polyVar = variable;
                    }
                    else if (groupComp is PowerFunction)
                    {
                        PowerFunction powFunc = groupComp as PowerFunction;
                        if (!(powFunc.GetPower() is ExNumber))
                            return null;
                        ExNumber powNum = powFunc.GetPower() as ExNumber;
                        if (!powNum.IsRealInteger())
                            return null;

                        if (-1 == (int)powNum.GetRealComp())
                        {
                            if (!(powFunc.GetBase() is ExNumber))
                                return null;
                            coeff = Operators.MulOp.StaticCombine(coeff, ExNumber.OpDiv(ExNumber.GetOne(), (powFunc.GetBase() as ExNumber)));
                        }
                        else
                        {
                            ExComp powFuncInner = powFunc.GetBase();

                            if (!(powFuncInner is AlgebraComp))
                                return null;

                            variable = powFuncInner as AlgebraComp;
                            if (polyVar == null)
                                polyVar = variable;

                            pow = (int)powNum.GetRealComp();
                        }
                    }
                    else if (groupComp is ExNumber)
                    {
                        coeff = Operators.MulOp.StaticCombine(coeff, (groupComp as ExNumber));
                    }
                    else
                        return null;
                }

                if (variable == null && pow == -1 && group.Length != 1 && coeff == null)
                    return null;

                if (variable != null && (variable == null || !variable.IsEqualTo(polyVar)))
                    return null;
                if (coeff == null)
                    coeff = new ExNumber(1.0);
                if (pow == -1)
                    pow = 0;

                polyInfo.Add(new TypePair<ExComp, int>(coeff, pow));
            }

            return new LoosePolyInfo(polyInfo, polyVar);
        }

        public PolyInfo GetPolynomialInfo()
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            if (groups.Count == 0)
                return null;

            List<TypePair<ExNumber, int>> polyInfo = new List<TypePair<ExNumber, int>>();

            AlgebraComp polyVar = null;
            foreach (ExComp[] group in groups)
            {
                if (group.Length > 2)
                    return null;

                AlgebraComp variable = null;
                ExNumber coeff = null;
                int pow = -1;
                foreach (ExComp groupComp in group)
                {
                    if (groupComp is AlgebraComp)
                    {
                        pow = 1;
                        if (polyVar == null)
                            polyVar = variable;
                    }
                    else if (groupComp is PowerFunction)
                    {
                        PowerFunction powFunc = groupComp as PowerFunction;
                        if (!(powFunc.GetPower() is ExNumber))
                            return null;
                        ExNumber powNum = powFunc.GetPower() as ExNumber;
                        if (!powNum.IsRealInteger())
                            return null;

                        pow = (int)powNum.GetRealComp();

                        ExComp powFuncInner = powFunc.GetBase();
                        if (!(powFuncInner is AlgebraComp))
                            return null;

                        variable = powFuncInner as AlgebraComp;
                        if (polyVar == null)
                            polyVar = variable;
                    }
                    else if (groupComp is ExNumber)
                    {
                        coeff = groupComp as ExNumber;
                    }
                }

                if (variable == null && pow == -1 && group.Length != 1 && coeff == null)
                    return null;

                if (variable != null && (variable == null || !variable.IsEqualTo(polyVar)))
                    return null;
                if (coeff == null)
                    coeff = new ExNumber(1.0);
                if (pow == -1)
                    pow = 0;

                polyInfo.Add(new TypePair<ExNumber, int>(coeff, pow));
            }

            return new PolyInfo(polyInfo, polyVar);
        }

        public virtual List<ExComp> GetPowersOfVar(AlgebraComp varFor)
        {
            List<ExComp> powersApplied = new List<ExComp>();
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraComp && (subComp as AlgebraComp).IsEqualTo(varFor))
                    powersApplied.Add(ExNumber.GetOne());
                else if (subComp is PowerFunction)
                {
                    PowerFunction subCompPowFunc = subComp as PowerFunction;
                    if ((subCompPowFunc.GetBase() is AlgebraTerm &&
                        (subCompPowFunc.GetBase() as AlgebraTerm).Contains(varFor)) ||
                        (subCompPowFunc.GetBase() is AlgebraComp &&
                        (subCompPowFunc.GetBase() as AlgebraComp).IsEqualTo(varFor)))
                    {
                        powersApplied.Add(subCompPowFunc.GetPower());
                    }
                }
                else if (subComp is AlgebraTerm)
                {
                    List<ExComp> subPowersApplied = (subComp as AlgebraTerm).GetPowersOfVar(varFor);
                    powersApplied.AddRange(subPowersApplied);
                }
            }

            return GroupHelper.RemoveDuplicates(powersApplied);
        }

        public List<PowerFunction> GetRadicals()
        {
            List<PowerFunction> radicals = new List<PowerFunction>();
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is PowerFunction && (subComp as PowerFunction).IsRadical())
                    radicals.Add(subComp as PowerFunction);
            }

            return radicals;
        }

        public List<ExComp> GetTrigFunctions()
        {
            List<ExComp> trigFuncs = new List<ExComp>();

            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is TrigFunction)
                {
                    trigFuncs.Add(_subComps[i] as TrigFunction);
                }
                else if (_subComps[i] is PowerFunction)
                {
                    PowerFunction pf = _subComps[i] as PowerFunction;
                    if (pf.GetBase() is TrigFunction)
                    {
                        trigFuncs.Add(pf);
                    }
                }
            }

            return trigFuncs;
        }

        public virtual bool HasLogFunctions()
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is LogFunction)
                    return true;
                else if (subComp is AlgebraTerm && (subComp as AlgebraTerm).HasLogFunctions())
                    return true;
            }

            return false;
        }

        public virtual bool HasTrigFunctions()
        {
            foreach (ExComp subComp in _subComps)
            {
                if (TrigFunction.GetTrigType(subComp) != null)
                    return true;
                else if (subComp is AlgebraTerm && (subComp as AlgebraTerm).HasTrigFunctions())
                    return true;
            }

            return false;
        }

        public bool HasVariableDens(AlgebraComp varFor)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            foreach (ExComp[] group in groups)
            {
                if (GroupHelper.ContainsFrac(group))
                {
                    AlgebraTerm denTerm = GroupHelper.ToAlgTerm(GroupHelper.GetDenominator(group, false));

                    if (denTerm.Contains(varFor))
                        return true;
                }
            }

            return false;
        }

        public virtual bool HasVariablePowers(AlgebraComp varFor)
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraTerm && (subComp as AlgebraTerm).HasVariablePowers(varFor))
                    return true;
            }

            return false;
        }

        public bool IsComplex()
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraTerm && (subComp as AlgebraTerm).IsComplex())
                    return true;
                else if (subComp is ExNumber && (subComp as ExNumber).HasImaginaryComp())
                    return true;
            }

            return false;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (GetTermCount() == 0)
                Add(ExNumber.GetZero());

            if (ex is AlgebraFunction)
                return false;
            else if (!(ex is AlgebraTerm))
                return false;

            AlgebraTerm term = ex as AlgebraTerm;

            if (this.GetTermCount() != term.GetTermCount())
                return false;

            List<ExComp[]> gps1 = this.GetGroups();
            List<ExComp[]> gps2 = term.GetGroups();

            if (gps1.Count != gps2.Count)
                return false;

            List<TypePair<ExComp[], bool>> matches = new List<TypePair<ExComp[], bool>>();
            for (int i = 0; i < gps1.Count; ++i)
                matches.Add(new TypePair<ExComp[], bool>(gps1[i], false));

            for (int i = 0; i < gps2.Count; ++i)
            {
                bool matchFound = false;
                for (int j = 0; j < matches.Count; ++j)
                {
                    if (matches[j].GetData2())
                        continue;

                    if (Equation.Group.GroupUtil.GpsEqual(gps2[i], matches[j].GetData1()))
                    {
                        matchFound = true;
                        matches[j].SetData2(true);
                        break;
                    }
                }

                if (!matchFound)
                    return false;
            }

            foreach (TypePair<ExComp[], bool> match in matches)
            {
                if (!match.GetData2())
                    return false;
            }

            return true;
        }

        public virtual bool IsOne()
        {
            if (GetTermCount() == 1 && _subComps[0] is ExNumber)
                return ExNumber.OpEqual((_subComps[0] as ExNumber), 1.0);
            return false;
        }

        public virtual bool IsUndefined()
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is ExNumber && (subComp as ExNumber).IsUndefined())
                    return true;
                if (subComp is AlgebraTerm && (subComp as AlgebraTerm).IsUndefined())
                    return true;
            }
            return false;
        }

        public virtual bool IsZero()
        {
            if (GetTermCount() == 1)
            {
                if (_subComps[0] is ExNumber)
                    return ExNumber.OpEqual((_subComps[0] as ExNumber), 0.0);
                else if (_subComps[0] is AlgebraTerm)
                    return (_subComps[0] as AlgebraTerm).IsZero();
            }
            else if (GetTermCount() == 0)
                return true;
            else
            {
                List<ExComp[]> groups = GetGroups();
                bool allZero = true;
                foreach (ExComp[] group in groups)
                {
                    ExNumber coeff = GetCoeffTerm(group);
                    if (coeff == null)
                    {
                        allZero = false;
                        break;
                    }
                    else if (ExNumber.OpNotEquals(coeff, 0.0))
                    {
                        allZero = false;
                        break;
                    }
                }

                return allZero;
            }

            return false;
        }

        public virtual bool TermsRelatable(ExComp comp)
        {
            if (IsEqualTo(comp))
                return true;
            if (comp is AlgebraTerm)
            {
                AlgebraTerm term = comp as AlgebraTerm;
                if (term.GetTermCount() == 1 && GetTermCount() == 1)
                {
                    ExComp first1 = term._subComps[0];
                    ExComp first2 = _subComps[0];

                    return GroupHelper.CompsRelatable(first1, first2);
                }
            }
            else if (GetTermCount() == 1)
            {
                ExComp first = _subComps[0];

                return GroupHelper.CompsRelatable(first, comp);
            }

            return false;
        }
    }
}