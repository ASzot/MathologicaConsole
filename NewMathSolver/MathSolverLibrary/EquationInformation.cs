using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal struct EquationInformation
    {
        public List<FunctionType> AppliedFunctions;
        public bool HasVariableDens;
        public bool HasVariablePowers;
        public ExNumber MaxPower;
        public int NumberOfAppliedFuncs;
        public bool OnlyFactors;
        public bool OnlyFractions;
        public List<ExComp> Powers;

        public bool GetIsLinear()
        {
            return HasOnlyPowers(ExNumber.GetOne());
        }

        public int GetNumberOfPowers()
        {
            return Powers.Count;
        }

        public bool GetOnlyIntPows()
        {
            if (Powers.Count == 0)
                return false;
            return GetIntegerPowCount() == Powers.Count;
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
                if (pow is ExNumber)
                {
                    ExNumber powNum = pow as ExNumber;
                    if (MaxPower == null || ExNumber.OpGT(powNum, MaxPower))
                        MaxPower = powNum;
                }
            }

            OnlyFactors = false;

            if (singular.GetGroupCount() == 1)
            {
                ExComp[] onlyGroup = singular.GetGroupsNoOps()[0];
                if (onlyGroup.Length > 1)
                {
                    OnlyFactors = true;
                    foreach (ExComp onlyGroupComp in onlyGroup)
                    {
                        // There should only be factors which are the terms.
                        if (!(onlyGroupComp is AlgebraTerm) && !onlyGroupComp.IsEqualTo(varFor) &&
                            !(onlyGroupComp is ExNumber))
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
            Powers = GroupHelper.RemoveDuplicates(Powers);

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
                if (pow is ExNumber)
                {
                    ExNumber powNum = pow as ExNumber;
                    if (MaxPower == null || ExNumber.OpGT(powNum, MaxPower))
                        MaxPower = powNum;
                }
            }

            OnlyFactors = false;
            if (left.IsZero() || right.IsZero())
            {
                AlgebraTerm nonZeroTerm = left.IsZero() ? right : left;

                if (nonZeroTerm.GetGroupCount() == 1)
                {
                    ExComp[] onlyGroup = nonZeroTerm.GetGroupsNoOps()[0];
                    if (onlyGroup.Length > 1)
                    {
                        OnlyFactors = true;
                        foreach (ExComp onlyGroupComp in onlyGroup)
                        {
                            // There should only be factors which are the terms.
                            if (!(onlyGroupComp is AlgebraTerm) && !onlyGroupComp.IsEqualTo(varFor) &&
                                !(onlyGroupComp is ExNumber))
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
                        if (simpFrac.GetNumEx() is ExNumber && simpFrac.GetDenEx() is ExNumber)
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
                if (power is ExNumber && (power as ExNumber).IsRealInteger())
                    integerPowCount++;
            }

            return integerPowCount;
        }

        public ExComp GetSubOutRecom(AlgebraTerm left, AlgebraTerm right, AlgebraComp varFor, out AlgebraTerm totalTerm, out bool factor)
        {
            factor = false;
            AlgebraTerm clonedLeft = (AlgebraTerm)left.CloneEx();
            AlgebraTerm clonedRight = (AlgebraTerm)right.CloneEx();

            totalTerm = Equation.Operators.SubOp.StaticCombine(clonedLeft, clonedRight).ToAlgTerm();

            List<ExComp[]> totalGroups = totalTerm.GetGroups();
            int totalGpCnt = totalGroups.Count;

            List<AlgebraGroup> groupsVarTo = totalTerm.GetGroupsVariableTo(varFor);

            // Check for a quadratic substitution.
            if (totalGpCnt == 3)
            {
                List<AlgebraGroup> groupsConstTo = totalTerm.GetGroupsConstantTo(varFor);
                if (groupsVarTo.Count == 2 && groupsConstTo.Count == 1)
                {
                    ExComp variableTerm0 = groupsVarTo[0].GetVariableGroupComps(varFor).ToTerm().RemoveRedundancies(false);
                    ExComp variableTerm1 = groupsVarTo[1].GetVariableGroupComps(varFor).ToTerm().RemoveRedundancies(false);

                    ExComp pow0;
                    ExComp pow1;
                    ExComp base0;
                    ExComp base1;

                    if (variableTerm0 is Equation.Functions.PowerFunction)
                    {
                        Equation.Functions.PowerFunction fnVariableTerm0 = variableTerm0 as Equation.Functions.PowerFunction;
                        pow0 = fnVariableTerm0.GetPower();
                        base0 = fnVariableTerm0.GetBase();
                    }
                    else
                    {
                        pow0 = ExNumber.GetOne();
                        base0 = variableTerm0;
                    }

                    if (variableTerm1 is Equation.Functions.PowerFunction)
                    {
                        Equation.Functions.PowerFunction fnVariableTerm1 = variableTerm1 as Equation.Functions.PowerFunction;
                        pow1 = fnVariableTerm1.GetPower();
                        base1 = fnVariableTerm1.GetBase();
                    }
                    else
                    {
                        pow1 = ExNumber.GetOne();
                        base1 = variableTerm1;
                    }

                    // Ensure that neither of these powers are actually roots.
                    if (base1.IsEqualTo(base0) && !(pow0 is AlgebraTerm && (pow0 as AlgebraTerm).GetNumDenFrac() != null) &&
                        !(pow1 is AlgebraTerm && (pow1 as AlgebraTerm).GetNumDenFrac() != null))
                    {
                        ExComp pow0Double = Equation.Operators.MulOp.StaticCombine(new ExNumber(2.0), pow0);
                        ExComp pow1Double = Equation.Operators.MulOp.StaticCombine(new ExNumber(2.0), pow1);

                        if (pow0Double.IsEqualTo(pow1) && !variableTerm0.IsEqualTo(varFor))
                            return variableTerm0;
                        else if (pow1Double.IsEqualTo(pow0) && !variableTerm1.IsEqualTo(varFor))
                            return variableTerm1;
                    }
                }
            }

            if (HasOnlyOrFunctionsBasicOnly(FunctionType.Logarithm, FunctionType.Sinusodal, FunctionType.AbsoluteValue) && GetNumberOfPowers() > 1)
            {
                ExComp[] variableTermsPowsArr = new ExComp[groupsVarTo.Count];
                for (int i = 0; i < groupsVarTo.Count; ++i)
                {
                    variableTermsPowsArr[i] =
                        groupsVarTo[i].GetVariableGroupComps(varFor).ToTerm().RemoveRedundancies(false);
                }

                List<AlgebraTerm> variableTerms = new List<AlgebraTerm>();
                foreach (ExComp varTermPow in variableTermsPowsArr)
                {
                    if (varTermPow.IsEqualTo(varFor))
                        return null;

                    if (varTermPow is Equation.Functions.PowerFunction)
                    {
                        AlgebraTerm baseTerm = (varTermPow as PowerFunction).GetBase().ToAlgTerm();
                        variableTerms.Add(baseTerm);
                        continue;
                    }
                    variableTerms.Add(varTermPow.ToAlgTerm());
                }

                // There may be the case where factoring is easier.
                // Factoring is easier in the case where there is no constant term.
                if (variableTerms.Count == totalGroups.Count)
                {
                    factor = true;

                    foreach (AlgebraTerm variableTerm in variableTerms)
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