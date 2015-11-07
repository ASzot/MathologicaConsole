using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Solving;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal class AlgebraSolver
    {
        private int _iterationCount = 0;
        private AlgebraComp _iterationVar;
        private List<AlgebraComp> _subVars = new List<AlgebraComp>();
        private int _subVarIndex = 0;
        private int _linearSolveRepeatCount = 0;

        public AlgebraComp IterationVar
        {
            get
            {
                if (_iterationVar == null)
                    CreateUSubTable(new List<TypePair<LexemeType, string>>());
                return _iterationVar;
            }
        }

        public AlgebraSolver()
        {
        }

        public void ClearLinearSolveRepeatCount()
        {
            _linearSolveRepeatCount = 0;
        }

        public static Dictionary<string, int> GetIdenOccurances(List<TypePair<LexemeType, string>> lexemeTable)
        {
            Dictionary<string, int> occurances = new Dictionary<string, int>();
            foreach (TypePair<LexemeType, string> lexeme in lexemeTable)
            {
                if (lexeme.GetData1() == LexemeType.Identifier)
                {
                    if (occurances.ContainsKey(lexeme.GetData2()))
                        occurances[lexeme.GetData2()] = occurances[lexeme.GetData2()] + 1;
                    else
                        occurances[lexeme.GetData2()] = 1;
                }
            }

            return occurances;
        }

        public static string GetProbableVar(Dictionary<string, int> idens)
        {
            KeyValuePair<string, int> maxIden = ArrayFunc.CreateKeyValuePair("-", -1);
            foreach (KeyValuePair<string, int> keyVal in idens)
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
            return CalculateDomain(new EqSet(term), varFor, ref evalData);
        }

        /// <summary>
        /// The cloning process is already done in the EquationSet calculating the domain.
        /// </summary>
        /// <param name="useSet"></param>
        /// <param name="varFor"></param>
        /// <returns></returns>
        public SolveResult CalculateDomain(EqSet useSet, AlgebraVar varFor, ref TermType.EvalData pEvalData)
        {
            if (pEvalData == null)
                throw new InvalidOperationException("Evaluate data not set.");

            pEvalData.AttemptSetInputType(TermType.InputType.FunctionDomain);

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Find the domain of " + WorkMgr.STM + varFor.ToMathAsciiString() + WorkMgr.EDM + " in the term.", useSet.FinalToDispStr());

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
            Dictionary<string, int> idenOccurances = GetIdenOccurances(lexemeTable);
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
            AlgebraComp comp = _subVars.ElementAt(_subVarIndex++);
            if (_subVarIndex >= _subVars.Count)
                _subVarIndex = 0;
            return comp;
        }

        public void ResetIterCount()
        {
            _iterationCount = 0;
        }

        public ExComp Solve(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData)
        {
            return Solve(solveFor, left, right, ref pEvalData, false);
        }

        public ExComp Solve(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData, bool showFinalStep)
        {
            SolveMethod solveMethod;

            left = new AlgebraTerm(left);
            right = new AlgebraTerm(right);

            // All functions must be called.
            if (!left.CallFunctions(ref pEvalData))
            {
                pEvalData.AddMsg("Invalid function call.");
                return ExNumber.GetUndefined();
            }
            if (!right.CallFunctions(ref pEvalData))
            {
                pEvalData.AddMsg("Invalid function call.");
                return ExNumber.GetUndefined();
            }

            ExComp leftEx = left.RemoveRedundancies(false);
            ExComp rightEx = right.RemoveRedundancies(false);
            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            if (left is AlgebraFunction)
                left = (left as AlgebraFunction).Evaluate(false, ref pEvalData).ToAlgTerm();
            else
                left.EvaluateFunctions(false, ref pEvalData);
            if (right is AlgebraFunction)
                right = (right as AlgebraFunction).Evaluate(false, ref pEvalData).ToAlgTerm();
            else
                right.EvaluateFunctions(false, ref pEvalData);

            if (left.IsUndefined() || right.IsUndefined())
                return ExNumber.GetUndefined();

            if (left.IsEqualTo(right))
                return new AllSolutions();

            if (!left.Contains(solveForComp) && !right.Contains(solveForComp))
            {
                if (Simplifier.Simplify(left, ref pEvalData).IsEqualTo(Simplifier.Simplify(right, ref pEvalData)))
                    return new AllSolutions();
                else
                    return new NoSolutions();
            }

            if (leftEx.IsEqualTo(solveForComp) && !right.Contains(solveForComp))
            {
                //WorkMgr.FromSides(left, right);
                right = right.ApplyOrderOfOperations();
                return right.MakeWorkable();
            }

            if (rightEx.IsEqualTo(solveForComp) && !left.Contains(solveForComp))
            {
                if (pEvalData.GetNegDivCount() != -1)
                    pEvalData.SetNegDivCount(pEvalData.GetNegDivCount() + 1);
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
            bool skipSinusodal = solveInfo.NumberOfAppliedFuncs == 1 && solveInfo.GetNumberOfPowers() > 0 && !solveInfo.HasPower(ExNumber.GetOne());

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
            else if (solveInfo.HasOnlyPowers(new ExNumber(1.0)) && !solveInfo.HasVariablePowers)
            {
                solveMethod = new LinearSolve(this, _linearSolveRepeatCount);
                _linearSolveRepeatCount++;
                solveDesc = "Solve this linear problem.";
            }
            else if (solveInfo.GetNumberOfPowers() == 1)
            {
                solveMethod = new PowerSolve(this, solveInfo.Powers[0]);
                solveDesc = "Solve this power problem";
            }
            else if (solveInfo.HasOnlyPowers(new ExNumber(2.0), new ExNumber(1.0)))
            {
                solveMethod = new QuadraticSolve(this);
                solveDesc = "Solve this quadratic.";
            }
            else if (ExNumber.OpEqual(solveInfo.MaxPower, 3.0) && solveInfo.GetOnlyIntPows())
            {
                solveMethod = new CubicSolve(this);
                solveDesc = "Solve this cubic.";
            }
            else if (solveInfo.GetOnlyIntPows())
            {
                solveMethod = new PolynomialSolve(this);
                solveDesc = "Solve this polynomial.";
            }
            else if (solveInfo.HasOnlyOrFunctions(FunctionType.Exponential))
            {
                solveMethod = new ExponentSolve(this);
                solveDesc = "Solve this exponent problem.";
            }
            else if (solveInfo.GetNumberOfPowers() == 2 && solveInfo.GetFractionalPowCount() == 1
                && solveInfo.GetIntegerPowCount() == 1)
            {
                ExComp root = solveInfo.Powers[0] is AlgebraTerm ? solveInfo.Powers[0] : solveInfo.Powers[1];
                ExComp pow = solveInfo.Powers[0] is ExNumber ? solveInfo.Powers[0] : solveInfo.Powers[1];
                solveMethod = new MixedTermsSolve(root, pow, this);
                solveDesc = "Solve this mixed powers problem.";
            }
            else
            {
                pEvalData.AddFailureMsg("Couldn't determine solve method.");
                return null;
            }

            if (solveDesc != null)
                pEvalData.GetWorkMgr().FromSides(left, right, solveDesc);

            ExComp origLeft = null, origRight = null;
            if (pEvalData.GetWorkMgr().GetAllowWork())
            {
                origLeft = left.CloneEx();
                origRight = right.CloneEx();
            }

            if (!(solveMethod is LinearSolve))
                _linearSolveRepeatCount = 0;

            ExComp result = solveMethod.SolveEquation(left, right, solveFor, ref pEvalData);
            if (result is AlgebraTermArray && (result as AlgebraTermArray).GetTermCount() == 1)
                result = (result as AlgebraTermArray).GetTerms()[0];

            if (showFinalStep && result != null && pEvalData.GetWorkMgr().GetAllowWork() && !ExNumber.IsUndef(result))
            {
                if (result is AlgebraTermArray)
                {
                    string workStr = "";
                    AlgebraTermArray ataResult = result as AlgebraTermArray;
                    for (int i = 0; i < ataResult.GetTerms().Count; ++i)
                    {
                        AlgebraTerm indResult = ataResult.GetTerms()[i];
                        if (indResult == null || ExNumber.IsUndef(indResult))
                            continue;
                        string indResultDispStr = indResult.FinalToDispStr();
                        workStr += WorkMgr.STM + solveForComp.ToDispString() + "=" + indResultDispStr + WorkMgr.EDM;
                        if (i != ataResult.GetTerms().Count - 1)
                            workStr += "<br />";
                    }

                    if (workStr == "")
                        pEvalData.GetWorkMgr().FromSides(origLeft, origRight, "There are no solutions to the above equation.");
                    else
                    {
                        pEvalData.GetWorkMgr().FromFormatted(workStr, "Above are the solutions of " + WorkMgr.STM + solveForComp.ToDispString() + WorkMgr.EDM +
                        " for " + WorkMgr.STM + WorkMgr.ToDisp(origLeft) + "=" +
                        WorkMgr.ToDisp(origRight) + WorkMgr.EDM + ".");
                    }
                }
                else if (result != null)
                {
                    if (ExNumber.IsUndef(result))
                        pEvalData.GetWorkMgr().FromSides(origLeft, origRight, "There are no solutions to the above equation.");
                    else
                    {
                        pEvalData.GetWorkMgr().FromSides(solveForComp, result,
                        "The above is the solution of " + WorkMgr.STM + solveForComp.ToDispString() + WorkMgr.EDM +
                        " for " + WorkMgr.STM + WorkMgr.ToDisp(origLeft) + "=" +
                        WorkMgr.ToDisp(origRight) + WorkMgr.EDM + ".");
                    }
                }
            }

            return result;
        }

        public ExComp SolveEq(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData)
        {
            return SolveEq(solveFor, left, right, ref pEvalData, false, false);
        }

        public ExComp SolveEq(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData, bool stayIterLevel)
        {
            return SolveEq(solveFor, left, right, ref pEvalData, false, stayIterLevel);
        }

        /// <summary>
        /// Solves the equation in addition to cleaning up the formatting.
        /// </summary>
        /// <param name="solveFor"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="pEvalData"></param>
        /// <param name="showFinalStep"></param>
        /// <param name="stayIterLevel"></param>
        /// <returns></returns>
        public ExComp SolveEq(AlgebraVar solveFor, AlgebraTerm left, AlgebraTerm right, ref TermType.EvalData pEvalData, bool showFinalStep, bool stayIterLevel)
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
            {
                SolveResult solved = SolveResult.Solved(solveFor, result, ref pEvalData);
                return solved;
            }
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

                pEvalData.GetWorkMgr().FromSidesAndComps(sides, comparisonTypes, "Solve this compound inequality");

                if (sides[0] is ExNumber && sides[2] is ExNumber)
                {
                    ExNumber nSide0 = sides[0] as ExNumber;
                    ExNumber nSide2 = sides[2] as ExNumber;
                    if (Restriction.IsGreaterThan(comparisonTypes[0]) && Restriction.IsGreaterThan(comparisonTypes[1]) && ExNumber.OpLT(nSide0, nSide2))
                    {
                        pEvalData.GetWorkMgr().FromSidesAndComps(sides, comparisonTypes, "This inequality will never be true.");
                        return SolveResult.NoSolutions();
                    }
                    if (Restriction.IsGreaterThan(comparisonTypes[0]) && !Restriction.IsGreaterThan(comparisonTypes[1]) && ExNumber.OpLT(nSide2, nSide0))
                    {
                        pEvalData.GetWorkMgr().FromSidesAndComps(sides, comparisonTypes, "This inequality will never be true..");
                        return SolveResult.NoSolutions();
                    }
                    if (Restriction.IsGreaterThan(comparisonTypes[0]) && ExNumber.OpLT(nSide0, nSide2))
                    {
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM +
                            WorkMgr.ToDisp(nSide0) + Restriction.ComparisonOpToStr(comparisonTypes[0]) + WorkMgr.ToDisp(sides[1]) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(nSide0.ToAlgTerm(), sides[1].ToAlgTerm(), comparisonTypes[0], solveFor, ref pEvalData);
                    }
                    else if (!Restriction.IsGreaterThan(comparisonTypes[1]) && ExNumber.OpLT(nSide2, nSide0))
                    {
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM +
                            WorkMgr.ToDisp(sides[1]) + Restriction.ComparisonOpToStr(comparisonTypes[1]) + WorkMgr.ToDisp(nSide2) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(sides[1].ToAlgTerm(), nSide2.ToAlgTerm(), comparisonTypes[1], solveFor, ref pEvalData);
                    }
                    else if (!Restriction.IsGreaterThan(comparisonTypes[0]) && ExNumber.OpGT(nSide0, nSide2))
                    {
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM +
                            WorkMgr.ToDisp(nSide0) + Restriction.ComparisonOpToStr(comparisonTypes[0]) + WorkMgr.ToDisp(sides[1]) + WorkMgr.EDM, "Remove the redundant side.");
                        return SolveRegInequality(nSide0.ToAlgTerm(), sides[1].ToAlgTerm(), comparisonTypes[0], solveFor, ref pEvalData);
                    }
                    else if (Restriction.IsGreaterThan(comparisonTypes[1]) && ExNumber.OpGT(nSide2, nSide0))
                    {
                        pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM +
                            WorkMgr.ToDisp(sides[1]) + Restriction.ComparisonOpToStr(comparisonTypes[1]) + WorkMgr.ToDisp(nSide2) + WorkMgr.EDM, "Remove the redundant side.");
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
            pEvalData.SetNegDivCount(0);

            AlgebraComp solveForComp = solveFor.ToAlgebraComp();

            SolveMethod.PrepareForSolving(ref left, ref right, solveForComp, ref pEvalData);
            if (pEvalData.GetNegDivCount() == 0 ? false : pEvalData.GetNegDivCount() % 2 != 0)
            {
                comparison = Restriction.SwitchComparisonType(comparison);
                pEvalData.SetNegDivCount(0);
            }

            pEvalData.GetWorkMgr().SetUseComparison(Restriction.ComparisonOpToStr(comparison));
            SolveMethod.ConstantsToRight(ref left, ref right, solveForComp, ref pEvalData);
            SolveMethod.VariablesToLeft(ref left, ref right, solveForComp, ref pEvalData);
            SolveMethod.CombineFractions(ref left, ref right, ref pEvalData);
            pEvalData.GetWorkMgr().SetUseComparison("=");

            AlgebraTerm completeTerm = Equation.Operators.SubOp.StaticCombine(left.CloneEx(), right.CloneEx()).ToAlgTerm();
            completeTerm.EvaluateFunctions(false, ref pEvalData);
            completeTerm = completeTerm.CompoundFractions();

            if (completeTerm.RemoveRedundancies(false).IsEqualTo(ExNumber.GetZero()))
            {
                if (Restriction.IsEqualTo(comparison))
                {
                    pEvalData.GetWorkMgr().FromFormatted("`{0}=0`", "As the right side is equal to the left and the inequality is inclusive there are infinite solutions.", completeTerm);
                    return SolveResult.Simplified(new AllSolutions());
                }
                else
                {
                    pEvalData.GetWorkMgr().FromFormatted("`{0}=0`", "As the right side is equal to the left and the inequality is non inclusive there are no solutions.", completeTerm);
                    return SolveResult.Simplified(new NoSolutions());
                }
            }

            SolveResult result;
            bool forceRootsCheck = false;
            AlgebraTerm[] numDen = left.GetNumDenFrac();
            if (numDen != null && numDen[1].Contains(solveForComp) && numDen[0].Contains(solveForComp))
            {
                pEvalData.GetWorkMgr().FromSides(left, right, "Find points of interest in the equation to find over what ranges this rational function is in the restriction. This will include solving for the denominator.");
                // This will be solved slightly differently.
                pEvalData.GetWorkMgr().FromSides(left, right, "Solve the term as a whole.");
                SolveResult result0 = SolveEquationEquality(solveFor, (AlgebraTerm)left.CloneEx(), (AlgebraTerm)right.CloneEx(), ref pEvalData);
                if (result0.GetUndefinedSolution())
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

                pEvalData.GetWorkMgr().FromSides(numDen[1], right, "Solve the denominator.");
                SolveResult result1 = SolveEquationEquality(solveFor, (AlgebraTerm)numDen[1].CloneEx(), ExNumber.GetZero().ToAlgTerm(), ref pEvalData);
                if (result1.GetUndefinedSolution())
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
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}{1}{2}" + WorkMgr.EDM, "Solve the equation just like an equality.", left, Restriction.ComparisonOpToStr(comparison), right);
                if (right.RemoveRedundancies(false).IsEqualTo(solveForComp))
                {
                    OrRestriction orRest = new OrRestriction(solveForComp, Restriction.InvertComparison(comparison), left, ref pEvalData);
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + orRest.ToMathAsciiStr() + WorkMgr.EDM, "The resulting equality.");
                    return SolveResult.InequalitySolved(orRest);
                }
                else
                {
                    result = SolveEquationEquality(solveFor, (AlgebraTerm)left.CloneEx(), (AlgebraTerm)right.CloneEx(), ref pEvalData);
                    if (result.GetUndefinedSolution())
                        return SolveResult.NoSolutions();
                    if (!result.Success)
                        return SolveResult.Failure();

                    //result.RemoveExtraneousSolutions(new EquationSet(left, right, LexemeType.EqualsOp));
                    result.RemoveUndefinedSolutions();

                    if (!result.GetHasSolutions() && !result.GetHasRestrictions() && !pEvalData.GetHasPartialSolutions() && result.Success)
                    {
                        SolveResult solved = SolveResult.Solved(solveFor, new NoSolutions(), ref pEvalData);
                        return solved;
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
                    pEvalData.GetWorkMgr().FromSides(left, right, "Exclude all complex solutions.");
                }
            }

            bool switchSign = pEvalData.GetNegDivCount() == 0 ? false : pEvalData.GetNegDivCount() % 2 != 0;
            // So the sign switches are no longer counted.
            pEvalData.SetNegDivCount(-1);

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
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM, "Combine the ranges to get the final result.", overallRest);
                    return SolveResult.InequalitySolved(overallRest);
                }
                else
                {
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}" + WorkMgr.EDM + ", " + WorkMgr.STM + "{1}" + WorkMgr.EDM, "Get the final result from the solved equation.", rest0, rest1);
                    return SolveResult.InequalitySolved(rest0, rest1);
                }
            }
            else if (totalMultiplicity == 1 && result.Solutions.Count == 1 && !forceRootsCheck)
            {
                OrRestriction rest = new OrRestriction(solveForComp, comparison, result.Solutions[0].Result, ref pEvalData);
                if (switchSign)
                {
                    rest.SwitchComparison();
                    pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + rest.ToMathAsciiStr() + WorkMgr.EDM, "Switch the inequality sign because of the division by a negative number.");
                }

                return SolveResult.InequalitySolved(rest);
            }
            else if (totalMultiplicity == 2 && result.Solutions.Count == 1 && !forceRootsCheck)
            {
                // We have a quadratic function (or quadratic like function) which bounces off the x-axis.
                pEvalData.GetWorkMgr().FromSides(left, right,
                    "The above equation has only one root, " +
                    WorkMgr.STM + WorkMgr.ToDisp(result.Solutions[0].Result) + WorkMgr.EDM + ", with a multiplicity of two.");
                if (Restriction.IsGreaterThan(comparison) == !switchSign)
                {
                    if (!Restriction.IsEqualTo(comparison))
                    {
                        SolveResult inequalitySolved = 
                            SolveResult.InequalitySolved(Restriction.ConstructAllBut(result.Solutions[0].Result,
                                solveForComp, ref pEvalData));
                        return inequalitySolved;
                    }
                    else
                    {
                        SolveResult inequalitySolved = SolveResult.InequalitySolved(Restriction.AllNumbers(solveForComp, ref pEvalData));
                        return inequalitySolved;
                    }
                }
                else
                {
                    if (Restriction.IsEqualTo(comparison))
                    {
                        SolveResult inequalitySolved = SolveResult.InequalitySolved(Restriction.FromOnly(result.Solutions[0].Result, solveForComp, ref pEvalData));
                        return inequalitySolved;
                    }
                    else
                        return SolveResult.NoSolutions();
                }
            }
            else if (result.Solutions.Count != 0)
            {
                List<TypePair<double, ExComp>> roots = new List<TypePair<double, ExComp>>();

                // We can only do this if all the solutions are numbers.
                foreach (Solution sol in result.Solutions)
                {
                    ExComp harshSimplified = Simplifier.HarshSimplify(sol.Result.CloneEx().ToAlgTerm(), ref pEvalData, true);
                    if (harshSimplified is AlgebraTerm)
                        harshSimplified = (harshSimplified as AlgebraTerm).RemoveRedundancies(false);
                    if (!(harshSimplified is ExNumber) || (harshSimplified is ExNumber && (harshSimplified as ExNumber).HasImaginaryComp()))
                    {
                        pEvalData.AddFailureMsg("Couldn't solve inequality");
                        return SolveResult.Failure();
                    }

                    roots.Add(new TypePair<double, ExComp>((harshSimplified as ExNumber).GetRealComp(), sol.Result));
                }

                roots = ArrayFunc.OrderList(roots);

                if (pEvalData.GetWorkMgr().GetAllowWork())
                {
                    string rootsStr = "";
                    for (int i = 0; i < roots.Count; ++i)
                    {
                        rootsStr += WorkMgr.STM + WorkMgr.ToDisp(roots[i].GetData2()) + WorkMgr.EDM;
                        if (i != roots.Count - 1)
                            rootsStr += ", ";
                    }

                    pEvalData.GetWorkMgr().FromFormatted(rootsStr, "Using the roots determine the ranges where the statement " +
                        WorkMgr.STM + left.FinalToDispStr() + Restriction.ComparisonOpToStr(comparison) + right.FinalToDispStr() + WorkMgr.EDM + " is true.");
                }

                List<bool> testPointsPos = new List<bool>();

                for (int i = 0; i < roots.Count; ++i)
                {
                    double testPoint;
                    bool? isPos;
                    if (i == 0)
                    {
                        testPoint = Restriction.FindLowerTestPoint(roots[0].GetData1());
                        isPos = IsTestPointPos(completeTerm, solveForComp, testPoint, ref pEvalData);
                        // The redundant parentheses is necessary for lang compat.
                        if (!(isPos.HasValue))
                        {
                            pEvalData.AddFailureMsg("Couldn't solve inequality.");
                            return SolveResult.Failure();
                        }

                        testPointsPos.Add(isPos.Value);
                    }
                    // Important that it's not an else-if here. The first element can also be the last if there is only one solution.
                    if (i == roots.Count - 1)
                    {
                        testPoint = Restriction.FindUpperTestPoint(roots[i].GetData1());
                        isPos = IsTestPointPos(completeTerm, solveForComp, testPoint, ref pEvalData);
                        // The redundant parentheses is necessary for lang compat.
                        if (!(isPos.HasValue))
                        {
                            pEvalData.AddFailureMsg("Couldn't solve inequality.");
                            return SolveResult.Failure();
                        }

                        testPointsPos.Add(isPos.Value);

                        continue;
                    }

                    testPoint = Restriction.FindConvenientTestPoint(roots[i].GetData1(), roots[i + 1].GetData1());
                    isPos = IsTestPointPos(completeTerm, solveForComp, testPoint, ref pEvalData);
                    // The redundant parentheses is necessary for lang compat.
                    if (!(isPos.HasValue))
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
                        ranges.Add(Restriction.FromOnly(roots[i - 1].GetData2(), solveForComp, ref pEvalData));
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
                                lower = ExNumber.GetNegInfinity();
                            }
                            else
                                lower = roots[i - 1].GetData2();

                            upper = roots[j - 1].GetData2();

                            break;
                        }
                    }

                    if (lower == null || upper == null)
                    {
                        if (i == 0)
                            lower = ExNumber.GetNegInfinity();
                        else
                            lower = roots[i - 1].GetData2();

                        upper = ExNumber.GetPosInfinity();
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
            AlgebraTerm useTerm = (AlgebraTerm)completeTerm.CloneEx();
            AlgebraTerm subInTerm = useTerm.Substitute(solveForComp, new ExNumber(testPoint));
            subInTerm = subInTerm.ApplyOrderOfOperations();
            AlgebraTerm finalTerm = subInTerm.MakeWorkable().ToAlgTerm();

            ExComp simpEx = Simplifier.HarshSimplify(finalTerm, ref pEvalData, false);

            if (simpEx is AlgebraTerm)
                simpEx = (simpEx as AlgebraTerm).HarshEvaluation().RemoveRedundancies(false);

            if (!(simpEx is ExNumber) || (simpEx is ExNumber && (simpEx as ExNumber).HasImaginaryComp()))
                return null;

            return (simpEx as ExNumber).GetRealComp() > 0.0;
        }

        private SolveResult SolveCompoundInequality(AlgebraTerm left0, AlgebraTerm left1, AlgebraTerm right, LexemeType comparison0, LexemeType comparison1, AlgebraVar solveFor,
            ref TermType.EvalData pEvalData)
        {
            if (pEvalData == null)
                throw new InvalidOperationException("Evaluate data is not set.");

            ResetIterCount();
            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "{0}{1}{2}{3}{4}" + WorkMgr.EDM, "To solve this compound inequality solve the middle expression for the outer ones just as regular inequalities.",
                left0, Restriction.ComparisonOpToStr(comparison0), right, Restriction.ComparisonOpToStr(comparison1), left1);

            SolveResult solve0 = SolveRegInequality(left0, right.CloneEx().ToAlgTerm(), comparison0, solveFor, ref pEvalData);
            SolveResult solve1 = SolveRegInequality(right, left1, comparison1, solveFor, ref pEvalData);

            pEvalData.AttemptSetInputType(TermType.InputType.CompoundInequalities);

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