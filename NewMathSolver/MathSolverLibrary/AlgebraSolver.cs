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

    internal class AlgebraSolver
    {
        private int _iterationCount = 0;
        private AlgebraComp _iterationVar;
        private List<AlgebraComp> _subVars = new List<AlgebraComp>();
        private int i_subVarIndex = 0;
        private int _linearSolveRepeatCount = 0;

        public AlgebraComp IterationVar
        {
            get { return _iterationVar; }
        }

        public AlgebraSolver()
        {
        }

        public static Dictionary<string, int> GetIdenOccurances(List<TypePair<LexemeType, string>> lexemeTable)
        {
            Dictionary<string, int> occurances = new Dictionary<string, int>();
            foreach (var lexeme in lexemeTable)
            {
                if (lexeme.Data1 == LexemeType.Identifier)
                {
                    if (occurances.ContainsKey(lexeme.Data2))
                        occurances[lexeme.Data2] = occurances[lexeme.Data2] + 1;
                    else
                        occurances[lexeme.Data2] = 1;
                }
            }

            return occurances;
        }

        public static string GetProbableVar(Dictionary<string, int> idens)
        {
            KeyValuePair<string, int> maxIden = new KeyValuePair<string, int>("-", -1);
            foreach (var keyVal in idens)
            {
                if (keyVal.Key == "x")
                    return "x";
                if (keyVal.Value > maxIden.Value)
                    maxIden = keyVal;
            }

            if (maxIden.Key == "-")
                return null;

            return maxIden.Key;
        }

        /// <summary>
        /// The cloning process is already done in the EquationSet calculating the domain.
        /// </summary>
        /// <param name="term"></param>
        /// <param name="varFor"></param>
        /// <returns></returns>
        public SolveResult CalculateDomain(ExComp term, AlgebraVar varFor, ref TermType.EvalData evalData)
        {
            return CalculateDomain(new EquationSet(term), varFor, ref evalData);
        }

        /// <summary>
        /// The cloning process is already done in the EquationSet calculating the domain.
        /// </summary>
        /// <param name="useSet"></param>
        /// <param name="varFor"></param>
        /// <returns></returns>
        public SolveResult CalculateDomain(EquationSet useSet, AlgebraVar varFor, ref TermType.EvalData pEvalData)
        {
            if (pEvalData == null)
                throw new InvalidOperationException("Evaluate data not set.");

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Find the domain of " + WorkMgr.STM + varFor.ToMathAsciiString() + WorkMgr.EDM + " in the term.", useSet.FinalToDispStr());

            List<Restriction> domainRestrictions = useSet.GetDomain(varFor, this, ref pEvalData);
            if (domainRestrictions == null)
            {
                pEvalData.AddFailureMsg("Couldn't find the domain.");
                return SolveResult.Failure();
            }

            if (domainRestrictions.Count == 0)
            {
                // The domain is unrestricted.
                domainRestrictions.Add(Restriction.AllNumbers(varFor, ref pEvalData));
            }

            return SolveResult.InequalitySolved(domainRestrictions.ToArray());
        }

        public void CreateUSubTable(List<TypePair<LexemeType, string>> lexemeTable)
        {
            var idenOccurances = GetIdenOccurances(lexemeTable);
            CreateUSubTable(idenOccurances);
        }

        public void CreateUSubTable(Dictionary<string, int> idenOccurances)
        {
            _subVars = new List<AlgebraComp>();
            int a_code = (int)'a';
            int z_code = (int)'z';

            if (!idenOccurances.ContainsKey("u"))
                _subVars.Add(new AlgebraComp("u"));
            if (!idenOccurances.ContainsKey("w"))
                _subVars.Add(new AlgebraComp("w"));

            for (int i = a_code; i <= z_code; ++i)
            {
                string searchChar = ((char)i).ToString();
                if (!idenOccurances.ContainsKey(searchChar))
                    _subVars.Add(new AlgebraComp(searchChar));
            }

            // Also get the iteration variable.
            if (!idenOccurances.ContainsKey("n"))
                _iterationVar = new AlgebraComp("n");
            else if (!idenOccurances.ContainsKey("k"))
                _iterationVar = new AlgebraComp("k");
            else
            {
                // Be sure to prevent the user from entering this variable.
                _iterationVar = new AlgebraComp("n_{iter}");
            }

            ////TODO:
            //// This could be a potential problem spot.
            //if (_subVars.Count < 8)
            //    throw new ArgumentException("How are there so few sub vars?!");
        }

        public AlgebraVar GetSolveVar(List<TypePair<LexemeType, string>> lexemeTable)
        {
            Dictionary<string, int> idenOccurances = GetIdenOccurances(lexemeTable);

            CreateUSubTable(idenOccurances);

            string varKey = GetProbableVar(idenOccurances);

            return new AlgebraVar(varKey);
        }

        public AlgebraComp NextSubVar()
        {
            AlgebraComp comp = _subVars[i_subVarIndex++];
            if (i_subVarIndex >= _subVars.Count)
                i_subVarIndex = 0;
            return comp;
        }

        public void ResetIterCount()
        {
            _iterationCount = 0;
        }

        public ExComp Solve(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData, bool showFinalStep = false)
        {
            SolveMethod solveMethod;

            if (left is AlgebraFunction)
                left = (left as AlgebraFunction).Evaluate(false, ref pEvalData).ToAlgTerm();
            else
                left.EvaluateFunctions(false, ref pEvalData);
            if (right is AlgebraFunction)
                right = (right as AlgebraFunction).Evaluate(false, ref pEvalData).ToAlgTerm();
            else
                right.EvaluateFunctions(false, ref pEvalData);

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            if (left.IsUndefined() || right.IsUndefined())
                return Number.Undefined;

            if (left.IsEqualTo(right))
                return new AllSolutions();

            if (!left.Contains(solveForComp) && !right.Contains(solveForComp))
            {
                if (Simplifier.Simplify(left, ref pEvalData).IsEqualTo(Simplifier.Simplify(right, ref pEvalData)))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            if (left.RemoveRedundancies().IsEqualTo(solveForComp) && !right.Contains(solveForComp))
            {
                //WorkMgr.FromSides(left, right);
                right = right.ApplyOrderOfOperations();
                return right.MakeWorkable();
            }

            if (right.RemoveRedundancies().IsEqualTo(solveForComp) && !left.Contains(solveForComp))
            {
                if (pEvalData.NegDivCount != -1)
                    pEvalData.NegDivCount++;
                //WorkMgr.FromSides(right, left);
                left = left.ApplyOrderOfOperations();
                return left.MakeWorkable();
            }

            EquationInformation solveInfo = new EquationInformation(left, right, solveForComp);
            AlgebraTerm totalTerm;
            bool factor;
            ExComp subOutRecom = solveInfo.GetSubOutRecom(left, right, solveForComp, out totalTerm, out factor);

            // Don't bother spending an hour on what this does. It belongs here regardless of what future me may think.
            // This is used for when the term is something like sin(x)^2 = 1/2. Now why isn't this mechanism in place
            // for a problem like ln(x)^2=1/2? On a power function with a trig function the trig func type is added to the
            // functions for the term. This is to allow for trig simplifications (which often use the properties of powers).
            bool skipSinusodal = solveInfo.NumberOfAppliedFuncs == 1 && solveInfo.NumberOfPowers > 0 && !solveInfo.HasPower(Number.One);

            string solveDesc = null;

            // The order of these solving methods matters very much,
            // Keep this in mind when altering the following.
            if (solveInfo.OnlyFractions || solveInfo.HasVariableDens)
            {
                solveMethod = new FractionalSolve(this, solveInfo.OnlyFractions);
                solveDesc = "Solve by eliminating fractions.";
            }
            else if (solveInfo.OnlyFactors)
            {
                solveMethod = new FactorSolve(this);
                solveDesc = "There are only factors. Solve them each separately.";
            }
            else if (factor)
            {
                solveMethod = new FactorSolve(this, totalTerm);
                solveDesc = "There appears to be a constant factor for the whole term.";
            }
            else if (subOutRecom != null)
            {
                solveMethod = new SubstitutionSolve(this, subOutRecom);
            }
            else if (solveInfo.HasOnlyOrFunctions(FunctionType.AbsoluteValue))
            {
                solveMethod = new AbsoluteValueSolve(this);
                solveDesc = "Solve this absolute value problem.";
            }
            else if (solveInfo.HasOnlyOrFunctions(FunctionType.Logarithm))
            {
                solveMethod = new LogSolve(this);
                solveDesc = "Solve this logarithm problem.";
            }
            else if (solveInfo.HasOnlyOrFunctions(FunctionType.Sinusodal) && !skipSinusodal)
            {
                solveMethod = new TrigSolve(this);
                solveDesc = "Solve this sinusoidal problem.";
            }
            else if (solveInfo.HasOnlyOrFunctions(FunctionType.LogarithmBase))
            {
                solveMethod = new LogBaseSolve(this);

                solveDesc = "Solve for the unknown in the log base.";
            }
            else if (solveInfo.HasOnlyPowers(new Number(1.0)) && !solveInfo.HasVariablePowers)
            {
                solveMethod = new LinearSolve(this, _linearSolveRepeatCount);
                _linearSolveRepeatCount++;
                solveDesc = "Solve this linear problem.";
            }
            else if (solveInfo.NumberOfPowers == 1)
            {
                solveMethod = new PowerSolve(this, solveInfo.Powers[0]);
                solveDesc = "Solve this power problem";
            }
            else if (solveInfo.HasOnlyPowers(new Number(2.0), new Number(1.0)))
            {
                solveMethod = new QuadraticSolve(this);
                solveDesc = "Solve this quadratic.";
            }
            else if (solveInfo.MaxPower == 3.0 && solveInfo.OnlyIntPows)
            {
                solveMethod = new CubicSolve(this);
                solveDesc = "Solve this cubic.";
            }
            else if (solveInfo.OnlyIntPows)
            {
                solveMethod = new PolynomialSolve(this);
                solveDesc = "Solve this polynomial.";
            }
            else if (solveInfo.HasOnlyOrFunctions(FunctionType.Exponential))
            {
                solveMethod = new ExponentSolve(this);
                solveDesc = "Solve this exponent problem.";
            }
            else if (solveInfo.NumberOfPowers == 2 && solveInfo.GetFractionalPowCount() == 1
                && solveInfo.GetIntegerPowCount() == 1)
            {
                ExComp root = solveInfo.Powers[0] is AlgebraTerm ? solveInfo.Powers[0] : solveInfo.Powers[1];
                ExComp pow = solveInfo.Powers[0] is Number ? solveInfo.Powers[0] : solveInfo.Powers[1];
                solveMethod = new MixedTermsSolve(root, pow, this);
                solveDesc = "Solve this mixed powers problem.";
            }
            else
            {
                pEvalData.AddFailureMsg("Couldn't determine solve method.");
                return null;
            }

            if (solveDesc != null)
                pEvalData.WorkMgr.FromSides(left, right, solveDesc);

            ExComp origLeft = null, origRight = null;
            if (pEvalData.WorkMgr.AllowWork)
            {
                origLeft = left.Clone();
                origRight = right.Clone();
            }

            if (!(solveMethod is LinearSolve))
                _linearSolveRepeatCount = 0;

            ExComp result = solveMethod.SolveEquation(left, right, solveFor, ref pEvalData);
            if (result is AlgebraTermArray && (result as AlgebraTermArray).TermCount == 1)
                result = (result as AlgebraTermArray).Terms[0];

            if (showFinalStep && result != null && pEvalData.WorkMgr.AllowWork && !Number.IsUndef(result))
            {
                if (result is AlgebraTermArray)
                {
                    string workStr = "";
                    AlgebraTermArray ataResult = result as AlgebraTermArray;
                    for (int i = 0; i < ataResult.Terms.Count; ++i)
                    {
                        var indResult = ataResult.Terms[i];
                        if (indResult == null || Number.IsUndef(indResult))
                            continue;
                        string indResultDispStr = indResult.FinalToDispStr();
                        workStr += WorkMgr.STM + solveForComp.ToDispString() + "=" + indResultDispStr + WorkMgr.EDM;
                        if (i != ataResult.Terms.Count - 1)
                            workStr += "<br />";
                    }

                    if (workStr == "")
                        pEvalData.WorkMgr.FromSides(origLeft, origRight, "There are no solutions to the above equation.");
                    else
                    {
                        pEvalData.WorkMgr.FromFormatted(workStr, "Above are the solutions of " + WorkMgr.STM + solveForComp.ToDispString() + WorkMgr.EDM +
                        " for " + WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(origLeft) + "=" +
                        WorkMgr.ExFinalToAsciiStr(origRight) + WorkMgr.EDM + ".");
                    }
                }
                else if (result != null)
                {
                    if (Number.IsUndef(result))
                        pEvalData.WorkMgr.FromSides(origLeft, origRight, "There are no solutions to the above equation.");
                    else
                    {
                        pEvalData.WorkMgr.FromSides(solveForComp, result,
                        "The above is the solution of " + WorkMgr.STM + solveForComp.ToDispString() + WorkMgr.EDM +
                        " for " + WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(origLeft) + "=" +
                        WorkMgr.ExFinalToAsciiStr(origRight) + WorkMgr.EDM + ".");
                    }
                }
            }

            return result;
        }

        public ExComp SolveEq(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData, bool showFinalStep = false, bool stayIterLevel = false)
        {
            if (!stayIterLevel)
                _iterationCount++;

            ExComp solved = Solve(solveFor, left, right, ref pEvalData, showFinalStep);
            if (solved == null)
                return null;

            // Clean up the formatting.
            if (solved is AlgebraTerm && !(solved is GeneralSolution))
            {
                solved = (solved as AlgebraTerm).MakeFormattingCorrect(ref pEvalData);
            }

            return solved;
        }

        public SolveResult SolveEquationEquality(AlgebraTerm left, AlgebraTerm right, List<TypePair<LexemeType, string>> lexemeTable, ref TermType.EvalData evalData)
        {
            AlgebraVar solveVar = GetSolveVar(lexemeTable);

            return SolveEquationEquality(solveVar, left, right, ref evalData);
        }

        public SolveResult SolveEquationEquality(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData)
        {
            ResetIterCount();
            ExComp result = SolveEq(solveFor, left, right, ref pEvalData, true);

            if (result == null)
                return SolveResult.Failure();
            else
                return SolveResult.Solved(solveFor, result, ref pEvalData);
        }

        public SolveResult SolveEquationInequality(List<ExComp> sides, List<LexemeType> comparisonTypes, List<TypePair<LexemeType, string>> lexemeTable, ref TermType.EvalData evalData)
        {
            AlgebraVar solveFor = GetSolveVar(lexemeTable);

            return SolveEquationInequality(sides, comparisonTypes, solveFor, ref evalData);
        }

        public SolveResult SolveEquationInequality(List<ExComp> sides, List<LexemeType> comparisonTypes, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            ResetIterCount();
            if (sides.Count > 3)
            {
                pEvalData.AddFailureMsg("Can't solve an inequality with more than two comparison operators.");
                return SolveResult.Failure();
            }

            if (sides.Count == 3 && comparisonTypes.Count == 2)
            {
                // Right now if we have a compound inequality it can only be solved if the variable exists on one side.
                AlgebraComp solveForComp = solveFor.ToAlgebraComp();

                if (!sides[1].IsEqualTo(solveForComp) && !(sides[1] is AlgebraTerm && (sides[1] as AlgebraTerm).Contains(solveForComp)))
                {
                    pEvalData.AddFailureMsg("Can't solve compound inequality.");
                    return SolveResult.Failure();
                }

                pEvalData.WorkMgr.FromSidesAndComps(sides, comparisonTypes, "Solve this compound inequality");

                if (sides[0] is Number && sides[2] is Number)
                {
                    Number nSide0 = sides[0] as Number;
                    Number nSide2 = sides[2] as Number;
                    if (Restriction.IsGreaterThan(comparisonTypes[0]) && Restriction.IsGreaterThan(comparisonTypes[1]) && nSide0 < nSide2)
                    {
                        pEvalData.WorkMgr.FromSidesAndComps(sides, comparisonTypes, "This inequality will never be true.");
                        return SolveResult.NoSolutions();
                    }
                    if (Restriction.IsGreaterThan(comparisonTypes[0]) && !Restriction.IsGreaterThan(comparisonTypes[1]) && nSide2 < nSide0)
                    {
                        pEvalData.WorkMgr.FromSidesAndComps(sides, comparisonTypes, "This inequality will never be true..");
                        return SolveResult.NoSolutions();
                    }
                    if (Restriction.IsGreaterThan(comparisonTypes[0]) && nSide0 < nSide2)
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM +
                            WorkMgr.ExFinalToAsciiStr(nSide0) + Restriction.ComparisonOpToStr(comparisonTypes[0]) + WorkMgr.ExFinalToAsciiStr(sides[1]) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(nSide0.ToAlgTerm(), sides[1].ToAlgTerm(), comparisonTypes[0], solveFor, ref pEvalData);
                    }
                    else if (!Restriction.IsGreaterThan(comparisonTypes[1]) && nSide2 < nSide0)
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM +
                            WorkMgr.ExFinalToAsciiStr(sides[1]) + Restriction.ComparisonOpToStr(comparisonTypes[1]) + WorkMgr.ExFinalToAsciiStr(nSide2) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(sides[1].ToAlgTerm(), nSide2.ToAlgTerm(), comparisonTypes[1], solveFor, ref pEvalData);
                    }
                    else if (!Restriction.IsGreaterThan(comparisonTypes[0]) && nSide0 > nSide2)
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM +
                            WorkMgr.ExFinalToAsciiStr(nSide0) + Restriction.ComparisonOpToStr(comparisonTypes[0]) + WorkMgr.ExFinalToAsciiStr(sides[1]) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(nSide0.ToAlgTerm(), sides[1].ToAlgTerm(), comparisonTypes[0], solveFor, ref pEvalData);
                    }
                    else if (Restriction.IsGreaterThan(comparisonTypes[1]) && nSide2 > nSide0)
                    {
                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM +
                            WorkMgr.ExFinalToAsciiStr(sides[1]) + Restriction.ComparisonOpToStr(comparisonTypes[1]) + WorkMgr.ExFinalToAsciiStr(nSide2) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(sides[1].ToAlgTerm(), nSide2.ToAlgTerm(), comparisonTypes[1], solveFor, ref pEvalData);
                    }
                }

                return SolveCompoundInequality(sides[0].ToAlgTerm(), sides[2].ToAlgTerm(), sides[1].ToAlgTerm(), comparisonTypes[0], comparisonTypes[1], solveFor, ref pEvalData);
            }
            else if (sides.Count == 2 && comparisonTypes.Count == 1)
            {
                AlgebraTerm left = sides[0].ToAlgTerm();
                AlgebraTerm right = sides[1].ToAlgTerm();

                // Left [Comp] Right
                return SolveRegInequality(left, right, comparisonTypes[0], solveFor, ref pEvalData);
            }
            else
                throw new ArgumentException("There are less than two sides...");
        }

        /// <summary>
        /// In the form left [comparison] right
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="comparison"></param>
        /// <param name="solveFor"></param>
        /// <returns></returns>
        public SolveResult SolveRegInequality(AlgebraTerm left, AlgebraTerm right, LexemeType comparison, AlgebraVar solveFor, ref TermType.EvalData pEvalData)
        {
            ResetIterCount();
            pEvalData.NegDivCount = 0;

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            SolveMethod.PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);
            if (pEvalData.NegDivCount == 0 ? false : pEvalData.NegDivCount % 2 != 0)
            {
                comparison = Restriction.SwitchComparisonType(comparison);
                pEvalData.NegDivCount = 0;
            }

            pEvalData.WorkMgr.UseComparison = Restriction.ComparisonOpToStr(comparison);
            SolveMethod.ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);
            SolveMethod.VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            SolveMethod.CombineFractions(ref left, ref right, ref pEvalData);
            pEvalData.WorkMgr.UseComparison = "=";

            AlgebraTerm completeTerm = Equation.Operators.SubOp.StaticCombine(left.Clone(), right.Clone()).ToAlgTerm();
            completeTerm.EvaluateFunctions(false, ref pEvalData);
            completeTerm = completeTerm.CompoundFractions();

            if (completeTerm.RemoveRedundancies().IsEqualTo(Number.Zero))
            {
                if (Restriction.IsEqualTo(comparison))
                {
                    pEvalData.WorkMgr.FromFormatted("`{0}=0`", "As the right side is equal to the left and the inequality is inclusive there are infinite solutions.", completeTerm);
                    return SolveResult.Simplified(new AllSolutions());
                }
                else
                {
                    pEvalData.WorkMgr.FromFormatted("`{0}=0`", "As the right side is equal to the left and the inequality is non inclusive there are no solutions.", completeTerm);
                    return SolveResult.Simplified(new NoSolutions());
                }
            }

            SolveResult result;
            bool forceRootsCheck = false;
            AlgebraTerm[] numDen = left.GetNumDenFrac();
            if (numDen != null && numDen[1].Contains(solveForComp) && numDen[0].Contains(solveForComp))
            {
                pEvalData.WorkMgr.FromSides(left, right, "Find points of interest in the equation to find over what ranges this rational function is in the restriction. This will include solving for the denominator.");
                // This will be solved slightly differently.
                pEvalData.WorkMgr.FromSides(left, right, "Solve the term as a whole.");
                SolveResult result0 = SolveEquationEquality(solveFor, (AlgebraTerm)left.Clone(), (AlgebraTerm)right.Clone(), ref pEvalData);
                if (result0.UndefinedSolution)
                    return SolveResult.NoSolutions();
                if (!result0.Success)
                    return SolveResult.Failure();

                LexemeType noninclusiveForced;
                if (comparison == LexemeType.GreaterEqual)
                    noninclusiveForced = LexemeType.Greater;
                else if (comparison == LexemeType.LessEqual)
                    noninclusiveForced = LexemeType.Less;
                else
                    noninclusiveForced = comparison;

                pEvalData.WorkMgr.FromSides(numDen[1], right, "Solve the denominator.");
                SolveResult result1 = SolveEquationEquality(solveFor, (AlgebraTerm)numDen[1].Clone(), Number.Zero.ToAlgTerm(), ref pEvalData);
                if (result1.UndefinedSolution)
                    return SolveResult.NoSolutions();
                if (!result1.Success)
                    return SolveResult.Failure();

                List<Solution> combinedSolutions = new List<Solution>();
                combinedSolutions.AddRange(result0.Solutions);
                combinedSolutions.AddRange(result1.Solutions);
                result = new SolveResult(combinedSolutions.ToArray());

                forceRootsCheck = true;
            }
            else
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}{1}{2}" + WorkMgr.EDM, "Solve the equation just like an equality.", left, Restriction.ComparisonOpToStr(comparison), right);
                if (right.RemoveRedundancies().IsEqualTo(solveForComp))
                {
                    OrRestriction orRest = new OrRestriction(solveForComp, Restriction.InvertComparison(comparison), left, ref pEvalData);
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + orRest.ToMathAsciiStr() + WorkMgr.EDM, "The resulting equality.");
                    return SolveResult.InequalitySolved(orRest);
                }
                else
                {
                    result = SolveEquationEquality(solveFor, (AlgebraTerm)left.Clone(), (AlgebraTerm)right.Clone(), ref pEvalData);
                    if (result.UndefinedSolution)
                        return SolveResult.NoSolutions();
                    if (!result.Success)
                        return SolveResult.Failure();

                    //result.RemoveExtraneousSolutions(new EquationSet(left, right, LexemeType.EqualsOp));
                    result.RemoveUndefinedSolutions();

                    if (!result.HasSolutions && !result.HasRestrictions && !pEvalData.HasPartialSolutions && result.Success)
                    {
                        return SolveResult.Solved(solveFor, new NoSolutions(), ref pEvalData);
                    }
                    if (result.IsOnlyNoSolutions(ref pEvalData) || result.IsOnlyAllSolutions(ref pEvalData))
                    {
                        return result;
                    }
                }
            }

            if (!result.Success)
                return result;

            if (result.Solutions.Count > 1)
            {
                if (result.RemoveComplexSolutions())
                {
                    pEvalData.WorkMgr.FromSides(left, right, "Exclude all complex solutions.");
                }
            }

            bool switchSign = pEvalData.NegDivCount == 0 ? false : pEvalData.NegDivCount % 2 != 0;
            // So the sign switches are no longer counted.
            pEvalData.NegDivCount = -1;

            int totalMultiplicity = result.GetTotalMultiplicity();

            if (totalMultiplicity == 2 && result.Solutions.Count == 2 && !forceRootsCheck)
            {
                ExComp sol0 = result.Solutions[0].Result;
                if (sol0 == null)
                    sol0 = result.Solutions[0].GeneralResult;
                ExComp sol1 = result.Solutions[1].Result;
                if (sol1 == null)
                    sol1 = result.Solutions[1].GeneralResult;

                OrRestriction rest0 = new OrRestriction(solveForComp, comparison, sol0, ref pEvalData);
                OrRestriction rest1 = new OrRestriction(solveForComp, comparison, sol1, ref pEvalData);

                if (switchSign)
                    rest0.SwitchComparison();
                else
                    rest1.SwitchComparison();

                AndRestriction overallRest = OrRestriction.AttemptCombine(rest0, rest1, ref pEvalData);
                if (overallRest != null)
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Combine the ranges to get the final result.", overallRest);
                    return SolveResult.InequalitySolved(overallRest);
                }
                else
                {
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM + ", " + WorkMgr.STM + "{1}" + WorkMgr.EDM, "Get the final result from the solved equation.", rest0, rest1);
                    return SolveResult.InequalitySolved(rest0, rest1);
                }
            }
            else if (totalMultiplicity == 1 && result.Solutions.Count == 1 && !forceRootsCheck)
            {
                OrRestriction rest = new OrRestriction(solveForComp, comparison, result.Solutions[0].Result, ref pEvalData);
                if (switchSign)
                {
                    rest.SwitchComparison();
                    pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + rest.ToMathAsciiStr() + WorkMgr.EDM, "Switch the inequality sign because of the division by a negative number.");
                }

                return SolveResult.InequalitySolved(rest);
            }
            else if (totalMultiplicity == 2 && result.Solutions.Count == 1 && !forceRootsCheck)
            {
                // We have a quadratic function (or quadratic like function) which bounces off the x-axis.
                pEvalData.WorkMgr.FromSides(left, right,
                    "The above equation has only one root, " +
                    WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(result.Solutions[0].Result) + WorkMgr.EDM + ", with a multiplicity of two.");
                return (Restriction.IsGreaterThan(comparison) == !switchSign ?
                    SolveResult.InequalitySolved(Restriction.AllNumbers(solveForComp, ref pEvalData)) :
                    SolveResult.NoSolutions());
            }
            else if (result.Solutions.Count != 0)
            {
                List<TypePair<double, ExComp>> roots = new List<TypePair<double, ExComp>>();

                // We can only do this if all the solutions are numbers.
                foreach (Solution sol in result.Solutions)
                {
                    ExComp harshSimplified = Simplifier.HarshSimplify(sol.Result.Clone().ToAlgTerm(), ref pEvalData);
                    if (harshSimplified is AlgebraTerm)
                        harshSimplified = (harshSimplified as AlgebraTerm).RemoveRedundancies();
                    if (!(harshSimplified is Number) || (harshSimplified is Number && (harshSimplified as Number).HasImaginaryComp()))
                    {
                        pEvalData.AddFailureMsg("Couldn't solve inequality");
                        return SolveResult.Failure();
                    }

                    roots.Add(new TypePair<double, ExComp>((harshSimplified as Number).RealComp, sol.Result));
                }

                roots = roots.OrderBy(x => x.Data1).ToList();

                if (pEvalData.WorkMgr.AllowWork)
                {
                    string rootsStr = "";
                    for (int i = 0; i < roots.Count; ++i)
                    {
                        rootsStr += WorkMgr.STM + WorkMgr.ExFinalToAsciiStr(roots[i].Data2) + WorkMgr.EDM;
                        if (i != roots.Count - 1)
                            rootsStr += ", ";
                    }

                    pEvalData.WorkMgr.FromFormatted(rootsStr, "Using the roots determine the ranges where the statement " +
                        WorkMgr.STM + left.FinalToDispStr() + Restriction.ComparisonOpToStr(comparison) + right.FinalToDispStr() + WorkMgr.EDM + " is true.");
                }

                List<bool> testPointsPos = new List<bool>();

                for (int i = 0; i < roots.Count; ++i)
                {
                    double testPoint;
                    bool? isPos;
                    if (i == 0)
                    {
                        testPoint = Restriction.FindLowerTestPoint(roots[0].Data1);
                        isPos = IsTestPointPos(completeTerm, solveForComp, testPoint, ref pEvalData);
                        if (!isPos.HasValue)
                        {
                            pEvalData.AddFailureMsg("Couldn't solve inequality.");
                            return SolveResult.Failure();
                        }

                        testPointsPos.Add(isPos.Value);
                    }
                    // Important that it's not an else-if here. The first element can also be the last if there is only one solution.
                    if (i == roots.Count - 1)
                    {
                        testPoint = Restriction.FindUpperTestPoint(roots[i].Data1);
                        isPos = IsTestPointPos(completeTerm, solveForComp, testPoint, ref pEvalData);
                        if (!isPos.HasValue)
                        {
                            pEvalData.AddFailureMsg("Couldn't solve inequality.");
                            return SolveResult.Failure();
                        }

                        testPointsPos.Add(isPos.Value);

                        continue;
                    }

                    testPoint = Restriction.FindConvenientTestPoint(roots[i].Data1, roots[i + 1].Data1);
                    isPos = IsTestPointPos(completeTerm, solveForComp, testPoint, ref pEvalData);
                    if (!isPos.HasValue)
                    {
                        pEvalData.AddFailureMsg("Couldn't solve inequality.");
                        return SolveResult.Failure();
                    }

                    testPointsPos.Add(isPos.Value);
                }

                if (testPointsPos.Count != roots.Count + 1)
                {
                    pEvalData.AddFailureMsg("Couldn't solve inequality.");
                    return SolveResult.Failure();
                }

                List<Restriction> ranges = new List<Restriction>();

                bool isGreaterThan = Restriction.IsGreaterThan(comparison);
                bool isEqualTo = Restriction.IsEqualTo(comparison);
                LexemeType less = isEqualTo ? LexemeType.LessEqual : LexemeType.Less;

                for (int i = 0; i < testPointsPos.Count; ++i)
                {
                    bool isPos = testPointsPos[i];

                    // It's possible one of the roots alone is in the final restriction.
                    // Find if this is a bouncer which normally wouldn't be included in the final solution.
                    if (i > 0 && testPointsPos[i] == testPointsPos[i - 1] && testPointsPos[i] != isGreaterThan && isEqualTo)
                    {
                        // We have a root which is a 'bouncer' rather than a piercer.
                        ranges.Add(Restriction.FromOnly(roots[i - 1].Data2, solveForComp, ref pEvalData));
                    }

                    if (isPos != isGreaterThan)
                        continue;

                    // Find the first conflicting.
                    int j;
                    ExComp lower = null, upper = null;
                    for (j = i + 1; j < testPointsPos.Count; ++j)
                    {
                        if ((testPointsPos[j] != isPos) ||
                            (testPointsPos[j] == testPointsPos[j - 1] && !isEqualTo))
                        {
                            if (i == 0)
                            {
                                lower = Number.NegInfinity;
                            }
                            else
                                lower = roots[i - 1].Data2;

                            upper = roots[j - 1].Data2;

                            break;
                        }
                    }

                    if (lower == null || upper == null)
                    {
                        if (i == 0)
                            lower = Number.NegInfinity;
                        else
                            lower = roots[i - 1].Data2;
                        upper = Number.PosInfinity;
                    }

                    ranges.Add(new AndRestriction(lower, less, solveForComp, less, upper, ref pEvalData));

                    i = j - 1;
                }

                if (ranges.Count == 0)
                    return SolveResult.NoSolutions();

                return SolveResult.InequalitySolved(ranges.ToArray());
            }

            pEvalData.AddFailureMsg("Couldn't solve inequality.");
            return SolveResult.Failure();
        }

        private bool? IsTestPointPos(AlgebraTerm completeTerm, AlgebraComp solveForComp, double testPoint, ref TermType.EvalData pEvalData)
        {
            AlgebraTerm useTerm = (AlgebraTerm)completeTerm.Clone();
            AlgebraTerm subInTerm = useTerm.Substitute(solveForComp, new Number(testPoint));
            subInTerm = subInTerm.ApplyOrderOfOperations();
            AlgebraTerm final = subInTerm.MakeWorkable().ToAlgTerm();

            ExComp simpEx = Simplifier.HarshSimplify(final, ref pEvalData, false);

            if (simpEx is AlgebraTerm)
                simpEx = (simpEx as AlgebraTerm).HarshEvaluation().RemoveRedundancies();

            if (!(simpEx is Number) || (simpEx is Number && (simpEx as Number).HasImaginaryComp()))
                return false;

            return (simpEx as Number).RealComp > 0.0;
        }

        private SolveResult SolveCompoundInequality(AlgebraTerm left0, AlgebraTerm left1, AlgebraTerm right, LexemeType comparison0, LexemeType comparison1, AlgebraVar solveFor,
            ref TermType.EvalData pEvalData)
        {
            if (pEvalData == null)
                throw new InvalidOperationException("Evaluate data is not set.");

            ResetIterCount();
            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}{1}{2}{3}{4}" + WorkMgr.EDM, "To solve this compound inequality solve the middle expression for the outer ones just as regular inequalities.",
                left0, Restriction.ComparisonOpToStr(comparison0), right, Restriction.ComparisonOpToStr(comparison1), left1);

            SolveResult solve0 = SolveRegInequality(left0, right, comparison0, solveFor, ref pEvalData);
            SolveResult solve1 = SolveRegInequality(right, left1, comparison1, solveFor, ref pEvalData);

            if (!solve0.Success || !solve1.Success)
                return SolveResult.Failure();

            List<Restriction> finalRestrictions = Restriction.CompoundRestrictions(solve0.Restrictions, solve1.Restrictions, ref pEvalData);

            if (finalRestrictions.Count == 0)
            {
                pEvalData.AddFailureMsg("Couldn't solve inequality.");
                return SolveResult.Failure();
                //// There are no intersections.
                //finalRestrictions.AddRange(solve0.Restrictions);
                //finalRestrictions.AddRange(solve1.Restrictions);
            }

            return SolveResult.InequalitySolved(finalRestrictions.ToArray());
        }
    }
}