using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Solving;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal struct EquationInformation
    {
        public List<FunctionType> AppliedFunctions;
        public bool HasVariableDens;
        public bool HasVariablePowers;
        public Number MaxPower;
        public int NumberOfAppliedFuncs;
        public bool OnlyFactors;
        public bool OnlyFractions;
        public List<ExComp> Powers;

        public bool IsLinear
        {
            get
            {
                return HasOnlyPowers(Number.One);
            }
        }

        public int NumberOfPowers
        {
            get { return Powers.Count; }
        }

        public bool OnlyIntPows
        {
            get
            {
                if (Powers.Count == 0)
                    return false;
                return GetIntegerPowCount() == Powers.Count;
            }
        }

        public EquationInformation(AlgebraTerm singular, AlgebraComp varFor)
        {
            List<ExComp> powers = singular.GetPowersOfVar(varFor);

            List<FunctionType> funcs = singular.GetAppliedFunctionsNoPow(varFor);

            NumberOfAppliedFuncs = funcs.Count;

            Powers = powers;

            AppliedFunctions = funcs;

            OnlyFractions = singular.ContainsOnlyFractions();

            HasVariableDens = singular.HasVariableDens(varFor);

            HasVariablePowers = singular.HasVariablePowers(varFor);

            MaxPower = null;
            foreach (ExComp pow in Powers)
            {
                if (pow is Number)
                {
                    Number powNum = pow as Number;
                    if (MaxPower == null || powNum > MaxPower)
                        MaxPower = powNum;
                }
            }

            OnlyFactors = false;

            if (singular.GroupCount == 1)
            {
                ExComp[] onlyGroup = singular.GetGroupsNoOps()[0];
                if (onlyGroup.Length > 1)
                {
                    OnlyFactors = true;
                    foreach (ExComp onlyGroupComp in onlyGroup)
                    {
                        // There should only be factors which are the terms.
                        if (!(onlyGroupComp is AlgebraTerm) && !onlyGroupComp.IsEqualTo(varFor) &&
                            !(onlyGroupComp is Number))
                            OnlyFactors = false;
                    }
                }
            }
        }

        public EquationInformation(AlgebraTerm left, AlgebraTerm right, AlgebraComp varFor)
        {
            List<ExComp> powersLeft = (left is AppliedFunction) ? new List<ExComp>() : left.GetPowersOfVar(varFor);
            List<ExComp> powersRight = (right is AppliedFunction) ? new List<ExComp>() : right.GetPowersOfVar(varFor);

            List<FunctionType> funcsLeft = left.GetAppliedFunctionsNoPow(varFor);
            List<FunctionType> funcsRight = right.GetAppliedFunctionsNoPow(varFor);

            HasVariablePowers = left.HasVariablePowers(varFor) || right.HasVariablePowers(varFor);

            NumberOfAppliedFuncs = funcsLeft.Count + funcsRight.Count;

            Powers = new List<ExComp>();
            Powers.AddRange(powersLeft);
            Powers.AddRange(powersRight);
            Powers = Powers.RemoveDuplicates();

            AppliedFunctions = new List<FunctionType>();
            AppliedFunctions.AddRange(funcsLeft);
            AppliedFunctions.AddRange(funcsRight);
            AppliedFunctions = AppliedFunctions.Distinct().ToList();

            bool leftFracOnly = left.ContainsOnlyFractions();
            bool rightFracOnly = right.ContainsOnlyFractions();

            OnlyFractions = leftFracOnly && rightFracOnly;

            HasVariableDens = left.HasVariableDens(varFor) || right.HasVariableDens(varFor);

            MaxPower = null;
            foreach (ExComp pow in Powers)
            {
                if (pow is Number)
                {
                    Number powNum = pow as Number;
                    if (MaxPower == null || powNum > MaxPower)
                        MaxPower = powNum;
                }
            }

            OnlyFactors = false;
            if (left.IsZero() || right.IsZero())
            {
                AlgebraTerm nonZeroTerm = left.IsZero() ? right : left;

                if (nonZeroTerm.GroupCount == 1)
                {
                    ExComp[] onlyGroup = nonZeroTerm.GetGroupsNoOps()[0];
                    if (onlyGroup.Length > 1)
                    {
                        OnlyFactors = true;
                        foreach (ExComp onlyGroupComp in onlyGroup)
                        {
                            // There should only be factors which are the terms.
                            if (!(onlyGroupComp is AlgebraTerm) && !onlyGroupComp.IsEqualTo(varFor) &&
                                !(onlyGroupComp is Number))
                                OnlyFactors = false;
                        }
                    }
                }
            }
        }

        public int GetFractionalPowCount()
        {
            int fracPowCount = 0;
            foreach (ExComp power in Powers)
            {
                Equation.Term.SimpleFraction simpFrac = new Equation.Term.SimpleFraction();

                if (power is AlgebraTerm)
                {
                    if (simpFrac.Init(power as AlgebraTerm))
                    {
                        if (simpFrac.NumEx is Number && simpFrac.DenEx is Number)
                            fracPowCount++;
                    }
                }
            }

            return fracPowCount;
        }

        public int GetIntegerPowCount()
        {
            int integerPowCount = 0;
            foreach (ExComp power in Powers)
            {
                if (power is Number && (power as Number).IsRealInteger())
                    integerPowCount++;
            }

            return integerPowCount;
        }

        public ExComp GetSubOutRecom(AlgebraTerm left, AlgebraTerm right, AlgebraComp varFor, out AlgebraTerm totalTerm, out bool factor)
        {
            factor = false;
            AlgebraTerm clonedLeft = (AlgebraTerm)left.Clone();
            AlgebraTerm clonedRight = (AlgebraTerm)right.Clone();

            totalTerm = Equation.Operators.SubOp.StaticCombine(clonedLeft, clonedRight).ToAlgTerm();

            var totalGroups = totalTerm.GetGroups();
            int totalGpCnt = totalGroups.Count;

            var groupsVarTo = totalTerm.GetGroupsVariableTo(varFor);

            // Check for a quadratic substitution.
            if (totalGpCnt == 3)
            {
                var groupsConstTo = totalTerm.GetGroupsConstantTo(varFor);
                if (groupsVarTo.Count == 2 && groupsConstTo.Count == 1)
                {
                    ExComp variableTerm0 = groupsVarTo[0].GetVariableGroupComps(varFor).ToTerm().RemoveRedundancies();
                    ExComp variableTerm1 = groupsVarTo[1].GetVariableGroupComps(varFor).ToTerm().RemoveRedundancies();

                    ExComp pow0;
                    ExComp pow1;
                    ExComp base0;
                    ExComp base1;

                    if (variableTerm0 is Equation.Functions.PowerFunction)
                    {
                        Equation.Functions.PowerFunction fnVariableTerm0 = variableTerm0 as Equation.Functions.PowerFunction;
                        pow0 = fnVariableTerm0.Power;
                        base0 = fnVariableTerm0.Base;
                    }
                    else
                    {
                        pow0 = Number.One;
                        base0 = variableTerm0;
                    }

                    if (variableTerm1 is Equation.Functions.PowerFunction)
                    {
                        Equation.Functions.PowerFunction fnVariableTerm1 = variableTerm1 as Equation.Functions.PowerFunction;
                        pow1 = fnVariableTerm1.Power;
                        base1 = fnVariableTerm1.Base;
                    }
                    else
                    {
                        pow1 = Number.One;
                        base1 = variableTerm1;
                    }

                    // Ensure that neither of these powers are actually roots.
                    if (base1.IsEqualTo(base0) && !(pow0 is AlgebraTerm && (pow0 as AlgebraTerm).GetNumDenFrac() != null) &&
                        !(pow1 is AlgebraTerm && (pow1 as AlgebraTerm).GetNumDenFrac() != null))
                    {
                        ExComp pow0Double = Equation.Operators.MulOp.StaticCombine(new Number(2.0), pow0);
                        ExComp pow1Double = Equation.Operators.MulOp.StaticCombine(new Number(2.0), pow1);

                        if (pow0Double.IsEqualTo(pow1) && !variableTerm0.IsEqualTo(varFor))
                            return variableTerm0;
                        else if (pow1Double.IsEqualTo(pow0) && !variableTerm1.IsEqualTo(varFor))
                            return variableTerm1;
                    }
                }
            }

            if (HasOnlyOrFunctionsBasicOnly(FunctionType.Logarithm, FunctionType.Sinusodal, FunctionType.AbsoluteValue) && NumberOfPowers > 1)
            {
                var variableTermsPows = from varGp in groupsVarTo
                                        select varGp.GetVariableGroupComps(varFor).ToTerm().RemoveRedundancies();

                List<AlgebraTerm> variableTerms = new List<AlgebraTerm>();
                foreach (ExComp varTermPow in variableTermsPows)
                {
                    if (varTermPow.IsEqualTo(varFor))
                        return null;

                    if (varTermPow is Equation.Functions.PowerFunction)
                    {
                        var baseTerm = (varTermPow as PowerFunction).Base.ToAlgTerm();
                        variableTerms.Add(baseTerm);
                        continue;
                    }
                    //else if (varTermPow is AlgebraTerm)
                    //{
                    //    AlgebraTerm varTerm = varTermPow as AlgebraTerm;
                    //    bool allPows = true;
                    //    foreach (ExComp subComp in varTerm)
                    //    {
                    //        if (!(subComp is PowerFunction))
                    //    }
                    //}
                    variableTerms.Add(varTermPow.ToAlgTerm());
                }

                // There may be the case where factoring is easier.
                // Factoring is easier in the case where there is no constant term.
                if (variableTerms.Count == totalGroups.Count)
                {
                    factor = true;

                    foreach (var variableTerm in variableTerms)
                    {
                        if (variableTerm is AlgebraFunction)
                        {
                            factor = false;
                            break;
                        }
                    }

                    if (factor)
                        return null;
                }

                if (variableTerms.Count <= 1)
                    return null;

                //TODO:
                // This could defintely be made more effecient.

                bool allEqual = true;
                for (int i = 0; i < variableTerms.Count; ++i)
                {
                    AlgebraTerm varTerm = variableTerms[i];
                    for (int j = 0; j < variableTerms.Count; ++j)
                    {
                        if (j == i)
                            continue;

                        AlgebraTerm compareTerm = variableTerms[j];

                        if (!varTerm.IsEqualTo(compareTerm))
                        {
                            allEqual = false;
                            break;
                        }
                    }
                }

                if (allEqual)
                {
                    return variableTerms[0];
                }
            }

            return null;
        }

        public bool HasFunctionsApplied()
        {
            return AppliedFunctions.Count != 0;
        }

        public bool HasFunctionsNotApplied(params FunctionType[] functions)
        {
            foreach (FunctionType fn in functions)
            {
                if (AppliedFunctions.Contains(fn))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if only ONE of the functions is contained in the function list.
        /// </summary>
        /// <param name="functions"></param>
        /// <returns></returns>
        public bool HasOnlyOrFunctions(params FunctionType[] functions)
        {
            if (AppliedFunctions.Count != 1)
                return false;

            foreach (FunctionType functionType in functions)
            {
                if (AppliedFunctions.Contains(functionType))
                    return true;
            }

            return false;
        }

        public bool HasOnlyOrFunctionsBasicOnly(params FunctionType[] functions)
        {
            bool foundOne = false;
            foreach (FunctionType functionType in functions)
            {
                if (AppliedFunctions.Contains(functionType))
                {
                    if (foundOne)
                        return false;

                    foundOne = true;
                }
            }
            return foundOne;
        }

        public bool HasOnlyPowers(params ExComp[] powers)
        {
            if (Powers.Count != powers.Length)
                return false;

            foreach (ExComp power in powers)
            {
                if (!HasPower(power))
                    return false;
            }

            return true;
        }

        public bool HasPower(ExComp power)
        {
            foreach (ExComp subPower in Powers)
            {
                if (subPower.IsEqualTo(power))
                    return true;
            }

            return false;
        }
    }
}
