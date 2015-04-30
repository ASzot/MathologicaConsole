using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    partial class AlgebraTerm
    {
        public static Number GetCoeffTerm(ExComp[] group)
        {
            foreach (ExComp comp in group)
            {
                if (comp is Number)
                    return comp as Number;
            }

            return null;
        }

        public static Number GetCoeffTerm(List<ExComp> comps)
        {
            foreach (ExComp comp in comps)
            {
                if (comp is Number)
                    return comp as Number;
            }
            return null;
        }

        public virtual bool Contains(AlgebraComp varFor)
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraTerm && (subComp as AlgebraTerm).Contains(varFor))
                    return true;
                if (subComp is AlgebraComp && (subComp as AlgebraComp) == varFor)
                    return true;
            }

            return false;
        }

        public bool ContainsFractions()
        {
            List<ExComp[]> groups = GetGroups();

            foreach (ExComp[] group in groups)
            {
                if (group.ContainsFrac())
                    return true;
            }

            return false;
        }

        public bool ContainsOnlyFractions()
        {
            List<ExComp[]> groups = GetGroups();

            foreach (ExComp[] group in groups)
            {
                if (!group.ContainsFrac())
                    return false;
            }

            return true;
        }

        public int GetAppliedFuncCount(AlgebraComp varFor, FunctionType type)
        {
            List<FunctionType> appliedFuncs = GetAppliedFunctionsNoPow(varFor);
            IEnumerable<FunctionType> appliedFunc = from af in appliedFuncs
                              where af == type
                              select af;
            return appliedFunc.Count();
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
                        if ((powFunc.Power is AlgebraTerm && (powFunc.Power as AlgebraTerm).Contains(varFor)) ||
                            (powFunc.Power is AlgebraComp && powFunc.Power.IsEqualTo(varFor)))
                            appliedFuncs.Add(FunctionType.Exponential);
                        else if (powFunc.Base is TrigFunction)
                        {
                            appliedFuncs.Add(FunctionType.Sinusodal);
                        }
                    }
                    else if (subComp is AbsValFunction)
                    {
                        AbsValFunction absValFunc = subComp as AbsValFunction;

                        if (varFor.IsEqualTo(absValFunc.InnerEx) || absValFunc.Contains(varFor))
                            appliedFuncs.Add(FunctionType.AbsoluteValue);
                    }
                    else if (subComp is LogFunction)
                    {
                        LogFunction logFunc = subComp as LogFunction;

                        if (logFunc.InnerTerm.Contains(varFor))
                            appliedFuncs.Add(FunctionType.Logarithm);
                        if (logFunc.Base.IsEqualTo(varFor) || (logFunc.Base is AlgebraTerm && (logFunc.Base as AlgebraTerm).Contains(varFor)))
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

        public ExComp GetCoeffOfVar(AlgebraComp varFor)
        {
            if (!Contains(varFor))
                return null;

            if (TermCount == 1)
            {
                return Number.One;
            }

            List<ExComp[]> groups = GetGroupsNoOps();
            IEnumerable<ExComp[]> unrelatedGroups = from gp in groups
                                  where gp.ToAlgTerm().Contains(varFor)
                                  select gp.GetUnrelatableTermsOfGroup(varFor);

            // Combine all of the unrelated terms.
            AlgebraTerm unrelatedTerm = new AlgebraTerm(unrelatedGroups.ToArray());
            if (unrelatedTerm.GroupCount > 1)
            {
                unrelatedTerm = unrelatedTerm.ApplyOrderOfOperations();
                unrelatedTerm = unrelatedTerm.MakeWorkable().ToAlgTerm();
            }

            return unrelatedTerm;
        }

        public virtual List<Number> GetCoeffs()
        {
            List<ExComp[]> groups = GetGroups();
            IEnumerable<Number> coeffs = from gp in groups
                         select GetCoeffTerm(gp);
            return coeffs.ToList();
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

                        if (otherEx is Number && Number.NegOne.IsEqualTo(otherEx))
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
                ExComp coeff = new Number(1.0);
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
                        if (!(powFunc.Power is Number))
                            return null;
                        Number powNum = powFunc.Power as Number;
                        if (!powNum.IsRealInteger())
                            return null;

                        if (-1 == (int)powNum.RealComp)
                        {
                            if (!(powFunc.Base is Number))
                                return null;
                            coeff = Operators.MulOp.StaticCombine(coeff, Number.One / (powFunc.Base as Number));
                        }
                        else
                        {
                            ExComp powFuncInner = powFunc.Base;

                            if (!(powFuncInner is AlgebraComp))
                                return null;

                            variable = powFuncInner as AlgebraComp;
                            if (polyVar == null)
                                polyVar = variable;

                            pow = (int)powNum.RealComp;
                        }
                    }
                    else if (groupComp is Number)
                    {
                        coeff = Operators.MulOp.StaticCombine(coeff, (groupComp as Number));
                    }
                    else
                        return null;
                }

                if (variable == null && pow == -1 && group.Length != 1 && coeff == null)
                    return null;

                if (variable != null && (variable == null || !variable.IsEqualTo(polyVar)))
                    return null;
                if (coeff == null)
                    coeff = new Number(1.0);
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

            List<TypePair<Number, int>> polyInfo = new List<TypePair<Number, int>>();

            AlgebraComp polyVar = null;
            foreach (ExComp[] group in groups)
            {
                if (group.Length > 2)
                    return null;

                AlgebraComp variable = null;
                Number coeff = null;
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
                        if (!(powFunc.Power is Number))
                            return null;
                        Number powNum = powFunc.Power as Number;
                        if (!powNum.IsRealInteger())
                            return null;

                        pow = (int)powNum.RealComp;

                        ExComp powFuncInner = powFunc.Base;
                        if (!(powFuncInner is AlgebraComp))
                            return null;

                        variable = powFuncInner as AlgebraComp;
                        if (polyVar == null)
                            polyVar = variable;
                    }
                    else if (groupComp is Number)
                    {
                        coeff = groupComp as Number;
                    }
                }

                if (variable == null && pow == -1 && group.Length != 1 && coeff == null)
                    return null;

                if (variable != null && (variable == null || !variable.IsEqualTo(polyVar)))
                    return null;
                if (coeff == null)
                    coeff = new Number(1.0);
                if (pow == -1)
                    pow = 0;

                polyInfo.Add(new TypePair<Number, int>(coeff, pow));
            }

            return new PolyInfo(polyInfo, polyVar);
        }

        public virtual List<ExComp> GetPowersOfVar(AlgebraComp varFor)
        {
            List<ExComp> powersApplied = new List<ExComp>();
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is AlgebraComp && (subComp as AlgebraComp) == varFor)
                    powersApplied.Add(Number.One);
                else if (subComp is PowerFunction)
                {
                    PowerFunction subCompPowFunc = subComp as PowerFunction;
                    if ((subCompPowFunc.Base is AlgebraTerm &&
                        (subCompPowFunc.Base as AlgebraTerm).Contains(varFor)) ||
                        (subCompPowFunc.Base is AlgebraComp &&
                        (subCompPowFunc.Base as AlgebraComp) == varFor))
                    {
                        powersApplied.Add(subCompPowFunc.Power);
                    }
                }
                else if (subComp is AlgebraTerm)
                {
                    List<ExComp> subPowersApplied = (subComp as AlgebraTerm).GetPowersOfVar(varFor);
                    powersApplied.AddRange(subPowersApplied);
                }
            }

            return powersApplied.RemoveDuplicates();
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
                    if (pf.Base is TrigFunction)
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
                if (group.ContainsFrac())
                {
                    AlgebraTerm denTerm = group.GetDenominator().ToAlgTerm();

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
                else if (subComp is Number && (subComp as Number).HasImaginaryComp())
                    return true;
            }

            return false;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (TermCount == 0)
                Add(Number.Zero);

            if (ex is AlgebraFunction)
                return false;
            else if (!(ex is AlgebraTerm))
                return false;

            AlgebraTerm term = ex as AlgebraTerm;

            if (this.TermCount != term.TermCount)
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
                    if (matches[j].Data2)
                        continue;

                    if (Equation.Group.GroupUtil.GpsEqual(gps2[i], matches[j].Data1))
                    {
                        matchFound = true;
                        matches[j].Data2 = true;
                        break;
                    }
                }

                if (!matchFound)
                    return false;
            }

            foreach (TypePair<ExComp[], bool> match in matches)
            {
                if (!match.Data2)
                    return false;
            }

            return true;
        }

        public virtual bool IsOne()
        {
            if (TermCount == 1 && _subComps[0] is Number)
                return (_subComps[0] as Number) == 1.0;
            return false;
        }

        public virtual bool IsUndefined()
        {
            foreach (ExComp subComp in _subComps)
            {
                if (subComp is Number && (subComp as Number).IsUndefined())
                    return true;
                if (subComp is AlgebraTerm && (subComp as AlgebraTerm).IsUndefined())
                    return true;
            }
            return false;
        }

        public virtual bool IsZero()
        {
            if (TermCount == 1)
            {
                if (_subComps[0] is Number)
                    return (_subComps[0] as Number) == 0.0;
                else if (_subComps[0] is AlgebraTerm)
                    return (_subComps[0] as AlgebraTerm).IsZero();
            }
            else if (TermCount == 0)
                return true;
            else
            {
                List<ExComp[]> groups = GetGroups();
                bool allZero = true;
                foreach (ExComp[] group in groups)
                {
                    Number coeff = GetCoeffTerm(group);
                    if (coeff == null)
                    {
                        allZero = false;
                        break;
                    }
                    else if (coeff != 0.0)
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
                if (term.TermCount == 1 && TermCount == 1)
                {
                    ExComp first1 = term._subComps[0];
                    ExComp first2 = _subComps[0];

                    return GroupHelper.CompsRelatable(first1, first2);
                }
            }
            else if (TermCount == 1)
            {
                ExComp first = _subComps[0];

                return GroupHelper.CompsRelatable(first, comp);
            }

            return false;
        }
    }
}