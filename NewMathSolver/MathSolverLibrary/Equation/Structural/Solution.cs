using MathSolverWebsite.MathSolverLibrary.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal struct SolveResult
    {
        public List<Restriction> Restrictions;
        public List<Solution> Solutions;
        public bool Success;

        public bool HasRestrictions
        {
            get { return Restrictions != null && Restrictions.Count != 0; }
        }

        public bool HasSolutions
        {
            get { return Solutions != null && Solutions.Count != 0; }
        }

        public bool UndefinedSolution
        {
            get
            {
                return (Solutions.Count == 1 && Number.IsUndef(Solutions[0].Result));
            }
        }

        public SolveResult(params Solution[] solutions)
        {
            Solutions = new List<Solution>(solutions);
            Success = true;
            Restrictions = new List<Restriction>();
            RemoveDuplicateSols();
        }

        public static SolveResult Failure()
        {
            SolveResult failure;
            failure.Success = false;
            failure.Solutions = null;
            failure.Restrictions = null;

            return failure;
        }

        public static SolveResult Failure(string msg, ref TermType.EvalData pEvalData)
        {
            pEvalData.AddMsg(msg);
            return Failure();
        }

        public static SolveResult InequalitySolved(params Restriction[] restrictions)
        {
            SolveResult valid;
            valid.Success = true;
            valid.Solutions = new List<Solution>();
            valid.Restrictions = new List<Restriction>(restrictions);

            return valid;
        }

        public static SolveResult InvalidCmd(ref TermType.EvalData pEvalData)
        {
            pEvalData.AddFailureMsg("Couldn't recognize command.");
            return Failure();
        }

        public static SolveResult NoSolutions()
        {
            return new SolveResult(new Solution(new NoSolutions()));
        }

        public static SolveResult Simplified(ExComp result)
        {
            SolveResult success;
            success.Success = true;
            success.Solutions = new List<Solution>();
            success.Restrictions = null;

            if (result is AlgebraTermArray)
            {
                AlgebraTermArray resultArray = result as AlgebraTermArray;

                foreach (AlgebraTerm resultTerm in resultArray.Terms)
                {
                    success.Solutions.Add(new Solution(resultTerm));
                }
            }
            else
                success.Solutions.Add(new Solution(result));

            return success;
        }

        public static SolveResult SimplifiedForceFormatting(ExComp result)
        {
            SolveResult success;
            success.Success = true;
            success.Solutions = new List<Solution>();
            success.Restrictions = null;

            if (result is AlgebraTermArray)
            {
                AlgebraTermArray resultArray = result as AlgebraTermArray;

                foreach (AlgebraTerm resultTerm in resultArray.Terms)
                {
                    Solution sol = new Solution(resultTerm);
                    sol.ForceFormatting = true;
                    success.Solutions.Add(sol);
                }
            }
            else
            {
                Solution sol = new Solution(result);
                sol.ForceFormatting = true;
                success.Solutions.Add(sol);
            }

            return success;
        }

        public static SolveResult Solved(AlgebraVar solveFor, ExComp result, ref TermType.EvalData pEvalData)
        {
            return Solved(solveFor.ToAlgebraComp(), result, ref pEvalData);
        }

        public static SolveResult Solved(ExComp solveFor, ExComp result, ref TermType.EvalData pEvalData)
        {
            SolveResult success;
            success.Success = true;
            success.Solutions = new List<Solution>();
            success.Restrictions = null;

            if (result is AlgebraTermArray)
            {
                AlgebraTermArray resultArray = result as AlgebraTermArray;

                foreach (AlgebraTerm resultTerm in resultArray.Terms)
                {
                    if (resultTerm is GeneralSolution)
                        success.Solutions.Add(Solution.FromGeneralSol(solveFor, resultTerm as GeneralSolution));
                    else
                        success.Solutions.Add(new Solution(solveFor, resultTerm));
                }
            }
            else
            {
                if (result is GeneralSolution)
                    success.Solutions.Add(Solution.FromGeneralSol(solveFor, result as GeneralSolution));
                else
                    success.Solutions.Add(new Solution(solveFor, result));
            }

            success.RemoveDuplicateSols();
            success.CalculateApproximates(ref pEvalData);

            return success;
        }

        public static SolveResult Solved()
        {
            SolveResult success;
            success.Success = true;
            success.Solutions = null;
            success.Restrictions = null;

            return success;
        }

        public static SolveResult SolvedForceFormatting(ExComp solveFor, ExComp result)
        {
            SolveResult success;
            success.Success = true;
            success.Solutions = new List<Solution>();
            success.Restrictions = null;

            if (result is AlgebraTermArray)
            {
                AlgebraTermArray resultArray = result as AlgebraTermArray;

                foreach (AlgebraTerm resultTerm in resultArray.Terms)
                {
                    Solution addSol;
                    if (resultTerm is GeneralSolution)
                        addSol = Solution.FromGeneralSol(solveFor, resultTerm as GeneralSolution);
                    else
                        addSol = new Solution(solveFor, resultTerm);
                    addSol.ForceFormatting = true;
                    success.Solutions.Add(addSol);
                }
            }
            else
            {
                Solution addSol;
                if (result is GeneralSolution)
                    addSol = Solution.FromGeneralSol(solveFor, result as GeneralSolution);
                else
                    addSol = new Solution(solveFor, result);

                addSol.ForceFormatting = true;
                success.Solutions.Add(addSol);
            }

            success.RemoveDuplicateSols();

            return success;
        }

        public void CalculateApproximates(ref TermType.EvalData pEvalData)
        {
            if (Solutions == null)
                return;
            foreach (Solution sol in Solutions)
                sol.CalculateApproximate(ref pEvalData);
        }

        public List<string> GetIterationVarStrs()
        {
            List<string> iterVarStrs = new List<string>();
            if (Solutions != null)
            {
                foreach (Solution sol in Solutions)
                {
                    if (sol.GeneralResult != null)
                    {
                        string iterVarStr = sol.GeneralResult.IterVarToDispString();
                        if (!iterVarStrs.Contains(iterVarStr))
                            iterVarStrs.Add(iterVarStr);
                    }
                }
            }

            if (Restrictions != null)
            {
                foreach (Restriction rest in Restrictions)
                {
                    ExComp lower = rest.GetLower();
                    ExComp upper = rest.GetUpper();

                    if (lower is GeneralSolution)
                    {
                        string addStr = (lower as GeneralSolution).IterVarToDispString();
                        if (!iterVarStrs.Contains(addStr))
                            iterVarStrs.Add(addStr);
                    }
                    if (upper is GeneralSolution)
                    {
                        string addStr = (upper as GeneralSolution).IterVarToDispString();
                        if (!iterVarStrs.Contains(addStr))
                            iterVarStrs.Add(addStr);
                    }
                }
            }

            return iterVarStrs;
        }

        public int GetTotalMultiplicity()
        {
            int mulplicitySum = 0;
            foreach (Solution sol in Solutions)
            {
                mulplicitySum += sol.Multiplicity;
            }

            return mulplicitySum;
        }

        public bool IsOnlyAllSolutions(ref TermType.EvalData pEvalData)
        {
            bool allAllSols = true;
            foreach (Solution sol in Solutions)
            {
                if (!(sol.Result is AllSolutions))
                {
                    allAllSols = false;
                    break;
                }
            }

            return allAllSols && !HasRestrictions && !pEvalData.HasPartialSolutions;
        }

        public bool IsOnlyNoSolutions(ref TermType.EvalData pEvalData)
        {
            bool allNoSols = true;
            foreach (Solution sol in Solutions)
            {
                if (!(sol.Result is NoSolutions))
                {
                    allNoSols = false;
                    break;
                }
            }

            return allNoSols && !HasRestrictions && !pEvalData.HasPartialSolutions;
        }

        public bool RemoveComplexSolutions()
        {
            bool complexSolsRemoved = false;
            for (int i = 0; i < Solutions.Count; ++i)
            {
                if (Solutions[i].Result == null)
                    continue;
                if (Solutions[i].Result.ToAlgTerm().IsComplex())
                {
                    Solutions.RemoveAt(i--);
                    complexSolsRemoved = true;
                }
            }

            return complexSolsRemoved;
        }

        public void RemoveDuplicateSols()
        {
            if (Solutions == null)
                return;
            for (int i = 0; i < Solutions.Count; ++i)
            {
                Solution sol = Solutions[i];

                // As the solution is equal to itself.
                int equalCount = 1;

                for (int j = i + 1; j < Solutions.Count; ++j)
                {
                    Solution compareSol = Solutions[j];

                    ExComp comp0 = sol.Result;
                    if (comp0 == null)
                        continue;
                    if (comp0 is AlgebraTerm)
                        comp0 = (comp0 as AlgebraTerm).RemoveRedundancies();
                    ExComp comp1 = compareSol.Result;
                    if (comp1 is AlgebraTerm)
                        comp1 = (comp1 as AlgebraTerm).RemoveRedundancies();

                    if (comp0.IsEqualTo(comp1))
                    {
                        Solutions.RemoveAt(j);
                        j--;
                        equalCount++;
                    }
                }

                Solutions[i].Multiplicity = equalCount;
            }
        }

        public void RemoveExtraneousSolutions(EqSet eqSet, ref TermType.EvalData pEvalData)
        {
            if (!Success || !pEvalData.CheckSolutions)
                return;

            for (int i = 0; i < Solutions.Count; ++i)
            {
                // Not all solutions will have results.
                // For instance, with solutions to periodic functions there be only generic solutions.
                if (Solutions[i].Result == null || Solutions[i].Result is SpecialSolution)
                    continue;
                ExComp solution = Simplifier.HarshSimplify(Solutions[i].Result.Clone().ToAlgTerm(), ref pEvalData, false);
                // This should never happen as AlgebraTermArray's cannot be added to an already existing array of solutions.
                // (There can't be an array of arrays with solutions, that's not how they are added.)

                if (!(solution is Number))
                    continue;

                for (int j = 0; j < eqSet.Sides.Count; j += 2)
                {
                    ExComp left = eqSet.Sides[j];
                    ExComp right = eqSet.Sides[j + 1];

                    var comparison = eqSet.ComparisonOps[j];

                    AlgebraTerm leftSubbed = left.Clone().ToAlgTerm().Substitute(Solutions[i].SolveFor, solution);
                    AlgebraTerm rightSubbed = right.Clone().ToAlgTerm().Substitute(Solutions[i].SolveFor, solution);

                    ExComp leftEx = TermType.SimplifyTermType.BasicSimplify(leftSubbed, ref pEvalData);
                    ExComp rightEx = TermType.SimplifyTermType.BasicSimplify(rightSubbed, ref pEvalData);

                    leftEx = Simplifier.HarshSimplify(leftEx.ToAlgTerm(), ref pEvalData, false);
                    rightEx = Simplifier.HarshSimplify(rightEx.ToAlgTerm(), ref pEvalData, false);

                    if (leftEx is Number)
                        (leftEx as Number).Round(Number.FINAL_ROUND_COUNT);
                    if (rightEx is Number)
                        (rightEx as Number).Round(Number.FINAL_ROUND_COUNT);

                    if (!TermType.EqualityCheckTermType.EvalComparison(leftEx, rightEx, comparison))
                    {
                        string varSolStr = Solutions[i].SolveFor.ToAsciiString() + "=" + WorkMgr.ExFinalToAsciiStr(Solutions[i].Result);

                        pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + "{0}\\ne{1}" + WorkMgr.EDM, "Plugging " + WorkMgr.STM + varSolStr + WorkMgr.EDM +
                            " back into the original equation gives the above. The equation doesn't hold true under this solution therefore " + WorkMgr.STM +
                            varSolStr + WorkMgr.EDM + " is an extraneous solution.", leftEx, rightEx);
                        Solutions.RemoveAt(i--);
                    }
                }
            }
        }

        public void RemoveUndefinedSolutions()
        {
            if (Solutions == null)
                return;

            for (int i = 0; i < Solutions.Count; ++i)
            {
                ExComp result = Solutions[i].Result;
                if (result == null)
                {
                    // We might have a general solution instead.
                    GeneralSolution genSol = Solutions[i].GeneralResult;
                    if (genSol != null)
                    {
                        if (Number.IsUndef(genSol))
                            Solutions.RemoveAt(i--);
                    }
                }
                else
                {
                    if (Number.IsUndef(result))
                        Solutions.RemoveAt(i--);
                }
            }

            if (Solutions.Count == 0 && (Restrictions == null || Restrictions.Count == 0))
                Solutions.Add(new Solution(new NoSolutions()));
        }

        private bool IsValidSolution(ExComp sol, ExComp solveFor, AlgebraTerm left, AlgebraTerm right, out ExComp leftEx, out ExComp rightEx, ref TermType.EvalData pEvalData)
        {
            left = left.Substitute(solveFor, sol);
            right = right.Substitute(solveFor, sol);

            leftEx = TermType.SimplifyTermType.BasicSimplify(left, ref pEvalData);
            rightEx = TermType.SimplifyTermType.BasicSimplify(right, ref pEvalData);

            return leftEx.IsEqualTo(rightEx);
        }
    }

    internal class AndRestriction : Restriction
    {
        public ExComp Compare0;
        public ExComp Compare1;
        public LexemeType Comparison0;
        public LexemeType Comparison1;
        private ExComp _harshSimpCompare0;
        private ExComp _harshSimpCompare1;

        /// <summary>
        /// Initializes the AndRestriction class. Will fix all incorrect usages of inequalities and infinity. (Like 2 \lt x \le oo) 
        /// </summary>
        /// <param name="compare0"></param>
        /// <param name="comparison0"></param>
        /// <param name="varFor"></param>
        /// <param name="comparison1"></param>
        /// <param name="compare1"></param>
        /// <param name="pEvalData"></param>
        public AndRestriction(ExComp compare0, LexemeType comparison0, AlgebraComp varFor, LexemeType comparison1, ExComp compare1, ref TermType.EvalData pEvalData)
            : base(varFor)
        {
            Comparison0 = comparison0;
            Comparison1 = comparison1;

            Compare0 = compare0;
            Compare1 = compare1;

            if (Compare0 is Number && (Compare0 as Number).IsInfinity() && Restriction.IsEqualTo(Comparison0))
                Comparison0 = Comparison0 == LexemeType.LessEqual ? LexemeType.Less : LexemeType.Greater;
            if (Compare1 is Number && (Compare1 as Number).IsInfinity() && Restriction.IsEqualTo(Comparison1))
                Comparison1 = Comparison1 == LexemeType.LessEqual ? LexemeType.Less : LexemeType.Greater;

            _harshSimpCompare0 = Simplifier.HarshSimplify(compare0.Clone().ToAlgTerm(), ref pEvalData, false);
            _harshSimpCompare1 = Simplifier.HarshSimplify(compare1.Clone().ToAlgTerm(), ref pEvalData, false);
        }

        public override ExComp GetLower()
        {
            if (Restriction.IsGreaterThan(Comparison1))
                return Compare1;

            return Compare0;
        }

        public override ExComp GetUpper()
        {
            if (Restriction.IsGreaterThan(Comparison0))
                return Compare0;

            return Compare1;
        }

        public override bool IsLowerInclusive()
        {
            if (Restriction.IsGreaterThan(Comparison1))
                return Restriction.IsEqualTo(Comparison1);
            else
                return Restriction.IsEqualTo(Comparison0);
        }

        public override bool IsUpperInclusive()
        {
            if (Restriction.IsGreaterThan(Comparison0))
                return Restriction.IsEqualTo(Comparison0);
            else
                return Restriction.IsEqualTo(Comparison1);
        }

        public override bool IsValidValue(ExComp value, ref TermType.EvalData pEvalData)
        {
            ExComp harshEvalVal = Simplifier.HarshSimplify(value.Clone().ToAlgTerm(), ref pEvalData);
            if (harshEvalVal is AlgebraTerm)
                harshEvalVal = (harshEvalVal as AlgebraTerm).RemoveRedundancies();

            if (harshEvalVal is Number && _harshSimpCompare0 is Number && _harshSimpCompare1 is Number)
            {
                Number nVal = harshEvalVal as Number;
                Number nComp0 = _harshSimpCompare0 as Number;
                Number nComp1 = _harshSimpCompare1 as Number;

                return TermType.EqualityCheckTermType.EvalComparison(nComp0, nVal, Comparison0) &&
                    TermType.EqualityCheckTermType.EvalComparison(nVal, nComp1, Comparison1);
            }

            return true;
        }

        public override string ToMathAsciiStr()
        {
            if (Number.NegInfinity.IsEqualTo(Compare0) && Number.PosInfinity.IsEqualTo(Compare1))
                return VarComp.ToTexString() + "\\in\\mathbb{R}";

            string compare0Str = Compare0 is AlgebraTerm ? (Compare0 as AlgebraTerm).FinalToTexString() : Compare0.ToTexString();
            string compare1Str = Compare1 is AlgebraTerm ? (Compare1 as AlgebraTerm).FinalToTexString() : Compare1.ToTexString();

            return compare0Str + " " + ComparisonOpToStr(Comparison0) + " " + VarComp.ToTexString() + " " + ComparisonOpToStr(Comparison1) + " " +
                compare1Str;
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToMathAsciiStr();
            return Compare0.ToString() + ComparisonOpToStr(Comparison0) + VarComp.ToString() + ComparisonOpToStr(Comparison1) + Compare1.ToString();
        }
    }

    internal class NotRestriction : Restriction
    {
        public ExComp NotVal;

        public NotRestriction(AlgebraComp varFor, ExComp notVal)
            : base(varFor)
        {
            NotVal = notVal;
        }

        public static NotRestriction[] FromSolution(ExComp result, AlgebraComp varFor)
        {
            if (result is AlgebraTermArray)
            {
                AlgebraTermArray resultArray = result as AlgebraTermArray;

                var rests = from term in resultArray.Terms
                            select new NotRestriction(varFor, term);

                return rests.ToArray();
            }

            NotRestriction[] singleRest = { new NotRestriction(varFor, result) };

            return singleRest;
        }

        public override ExComp GetLower()
        {
            return NotVal;
        }

        public override ExComp GetUpper()
        {
            return NotVal;
        }

        public override bool IsLowerInclusive()
        {
            return false;
        }

        public override bool IsUpperInclusive()
        {
            return false;
        }

        public override bool IsValidValue(ExComp value, ref TermType.EvalData pEvalData)
        {
            return !NotVal.IsEqualTo(value);
        }

        public override string ToMathAsciiStr()
        {
            return VarComp.ToAsciiString() + "\\ne" + WorkMgr.ExFinalToAsciiStr(NotVal);
        }

        public override string ToString()
        {
            return VarComp.ToString() + "\\ne" + NotVal.ToString();
        }
    }

    internal class OrRestriction : Restriction
    {
        public ExComp Compare;
        public LexemeType Comparison;
        private ExComp _harshSimpCompare;

        public bool IsNoRealNums
        {
            get
            {
                return Number.NegInfinity.IsEqualTo(Compare) && Comparison == LexemeType.LessEqual;
            }
        }

        public OrRestriction(AlgebraComp varFor, LexemeType comparison, ExComp compare, ref TermType.EvalData pEvalData)
            : base(varFor)
        {
            Comparison = comparison;
            Compare = compare;
            _harshSimpCompare = Simplifier.HarshSimplify(compare.Clone().ToAlgTerm(), ref pEvalData, false);
        }

        public static AndRestriction AttemptCombine(OrRestriction rest0, OrRestriction rest1, ref TermType.EvalData pEvalData)
        {
            if (rest0.VarComp != rest1.VarComp)
                return null;

            ExComp rest0Compare = rest0.Compare is GeneralSolution ? (rest0.Compare as GeneralSolution).Result : rest0.Compare;
            ExComp rest1Compare = rest1.Compare is GeneralSolution ? (rest1.Compare as GeneralSolution).Result : rest1.Compare;

            ExComp compareEx0 = rest0Compare is AlgebraTerm ? Simplifier.HarshSimplify(rest0Compare.Clone() as AlgebraTerm, ref pEvalData, false) : rest0Compare;
            ExComp compareEx1 = rest1Compare is AlgebraTerm ? Simplifier.HarshSimplify(rest1Compare.Clone() as AlgebraTerm, ref pEvalData, false) : rest1Compare;

            if (compareEx0 is Number && compareEx1 is Number)
            {
                Number rest0Num = compareEx0 as Number;
                Number rest1Num = compareEx1 as Number;

                OrRestriction min, max;
                if (rest0Num < rest1Num)
                {
                    min = rest0;
                    max = rest1;
                }
                else
                {
                    min = rest1;
                    max = rest0;
                }

                if ((min.Comparison == LexemeType.Greater || min.Comparison == LexemeType.GreaterEqual) &&
                    (max.Comparison == LexemeType.Less || max.Comparison == LexemeType.LessEqual))
                {
                    min.SwitchComparison();
                    return new AndRestriction(min.Compare, min.Comparison, min.VarComp, max.Comparison, max.Compare, ref pEvalData);
                }
            }

            return null;
        }

        public static OrRestriction GetNoRealNumsRestriction(AlgebraComp varFor, ref TermType.EvalData pEvalData)
        {
            return new OrRestriction(varFor, LexemeType.LessEqual, Number.NegInfinity, ref pEvalData);
        }

        public override ExComp GetLower()
        {
            if (Restriction.IsGreaterThan(Comparison))
                return Compare;
            return Number.NegInfinity;
        }

        public override ExComp GetUpper()
        {
            if (!Restriction.IsGreaterThan(Comparison))
                return Compare;
            return Number.PosInfinity;
        }

        public override bool IsLowerInclusive()
        {
            if (!Restriction.IsGreaterThan(Comparison))
                return false;
            return Restriction.IsEqualTo(Comparison);
        }

        public override bool IsUpperInclusive()
        {
            if (Restriction.IsGreaterThan(Comparison))
                return false;
            return Restriction.IsEqualTo(Comparison);
        }

        public override bool IsValidValue(ExComp value, ref TermType.EvalData pEvalData)
        {
            ExComp harshEvalVal = Simplifier.HarshSimplify(value.Clone().ToAlgTerm(), ref pEvalData);
            if (harshEvalVal is AlgebraTerm)
                harshEvalVal = (harshEvalVal as AlgebraTerm).RemoveRedundancies();

            if (harshEvalVal is Number && _harshSimpCompare is Number)
            {
                Number nVal = harshEvalVal as Number;
                Number nComp = _harshSimpCompare as Number;

                return TermType.EqualityCheckTermType.EvalComparison(nVal, nComp, Comparison);
            }

            return true;
        }

        public void SwitchComparison()
        {
            Comparison = SwitchComparisonType(Comparison);
        }

        public override string ToMathAsciiStr()
        {
            if (Number.NegInfinity.IsEqualTo(Compare) && Comparison == LexemeType.LessEqual)
                return VarComp.ToAsciiString() + "\\in\\text{No Real Numbers}";
            string compareStr;
            if (Compare is AlgebraTerm)
                compareStr = (Compare as AlgebraTerm).FinalToTexString();
            else
                compareStr = Compare.ToTexString();
            return VarComp.ToTexString() + " " + ComparisonOpToStr(Comparison) + " " + compareStr;
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToMathAsciiStr();
            return VarComp.ToString() + ComparisonOpToStr(Comparison) + Compare.ToString();
        }
    }

    internal abstract class Restriction
    {
        public AlgebraComp VarComp;

        public Restriction(AlgebraComp varFor)
        {
            VarComp = varFor;
        }

        public static AndRestriction AllNumbers(AlgebraVar varFor, ref TermType.EvalData pEvalData)
        {
            return AllNumbers(varFor.ToAlgebraComp(), ref pEvalData);
        }

        public static AndRestriction AllNumbers(AlgebraComp varFor, ref TermType.EvalData pEvalData)
        {
            return new AndRestriction(Number.NegInfinity, LexemeType.Less, varFor, LexemeType.Less, Number.PosInfinity, ref pEvalData);
        }

        public static bool AreEqualTo(Restriction rest0, Restriction rest1)
        {
            return (rest0.GetType() == rest1.GetType() && rest0.GetLower().IsEqualTo(rest1.GetLower()) &&
                rest0.GetUpper().IsEqualTo(rest1.GetUpper()) && rest0.IsLowerInclusive() == rest1.IsLowerInclusive() &&
                rest0.IsUpperInclusive() == rest1.IsUpperInclusive());
        }

        public static string ComparisonOpToStr(LexemeType comp)
        {
            switch (comp)
            {
                case Parsing.LexemeType.EqualsOp:
                    return "=";

                case Parsing.LexemeType.Greater:
                    return "\\gt";

                case Parsing.LexemeType.GreaterEqual:
                    return "\\ge";

                case Parsing.LexemeType.Less:
                    return "\\lt";

                case Parsing.LexemeType.LessEqual:
                    return "\\le";
            }

            throw new ArgumentException("Not valid comparison operator.");
        }

        public static Restriction CompoundDomains(List<Restriction> rests, AlgebraVar varFor, ref TermType.EvalData pEvalData)
        {
            var regRests = (from rest in rests
                            where !(rest is NotRestriction)
                            select rest).ToList();

            if (regRests.Count == 0)
                return OrRestriction.AllNumbers(varFor, ref pEvalData);

            if (regRests.Count == 1)
                return regRests[0];

            Restriction finalRestriction = regRests[0];
            for (int i = 1; i < regRests.Count; ++i)
            {
                Restriction compareRest = regRests[i];
                finalRestriction = IntersectRestrictions(compareRest, finalRestriction, ref pEvalData);
                if (finalRestriction == null)
                    return null;
            }

            return finalRestriction;
        }

        public static List<Restriction> CompoundRestrictions(List<Restriction> rests0, List<Restriction> rests1, ref TermType.EvalData pEvalData)
        {
            List<Restriction> combinedRests = new List<Restriction>();
            combinedRests.AddRange(rests0);
            combinedRests.AddRange(rests1);

            return CompoundRestrictions(combinedRests, ref pEvalData);
        }

        public static List<Restriction> CompoundRestrictions(List<Restriction> rests, ref TermType.EvalData pEvalData)
        {
            if (rests.Count == 1)
                return rests;
            List<Restriction> finalRestrictions = new List<Restriction>();

            for (int i = 0; i < rests.Count; ++i)
            {
                Restriction rest = rests[i];

                if (rest is NotRestriction)
                {
                    finalRestrictions.Add(rest);
                    continue;
                }

                //bool intersectionFound = false;
                bool wasCompared = false;
                for (int j = 0; j < rests.Count; ++j)
                {
                    if (i == j || rests[j] is NotRestriction)
                        continue;
                    wasCompared = true;
                    Restriction compareRest = rests[j];
                    Restriction intersection = IntersectRestrictions(rest, compareRest, ref pEvalData);
                    if (intersection != null)
                    {
                        // Ensure final restrictions doesn't already contain the same thing.
                        bool add = true;
                        foreach (var finalRest in finalRestrictions)
                        {
                            if (finalRest is OrRestriction && intersection is OrRestriction)
                            {
                                var finalRestOr = finalRest as OrRestriction;
                                var intersectionOr = intersection as OrRestriction;
                                if (finalRestOr.Compare.IsEqualTo(intersectionOr.Compare) && finalRestOr.Comparison == intersectionOr.Comparison
                                    && finalRestOr.VarComp.IsEqualTo(intersectionOr.VarComp))
                                    add = false;
                            }
                            else if (finalRest is AndRestriction && intersection is AndRestriction)
                            {
                                var finalRestAnd = finalRest as AndRestriction;
                                var intersectionAnd = intersection as AndRestriction;

                                if (finalRestAnd.Compare0.IsEqualTo(intersectionAnd.Compare0) && finalRestAnd.Compare1.IsEqualTo(intersectionAnd.Compare1) &&
                                    finalRestAnd.Comparison0 == intersectionAnd.Comparison0 && finalRestAnd.Comparison1 == intersectionAnd.Comparison1 &&
                                    finalRestAnd.VarComp.IsEqualTo(intersection.VarComp))
                                    add = false;
                            }
                        }
                        if (!add)
                            continue;
                        finalRestrictions.Add(intersection);
                        var rem0 = rests[i];
                        var rem1 = rests[j];

                        //rests.Remove(rem0);
                        //rests.Remove(rem1);
                        //intersectionFound = true;
                        break;
                    }
                }

                if (!wasCompared)
                    finalRestrictions.Add(rest);
            }

            return finalRestrictions;
        }

        public static double FindConvenientTestPoint(double lower, double upper)
        {
            if (lower >= upper)
                throw new ArgumentException("Invalid lower and upper bounds!");

            int iLower = (int)lower;
            int iUpper = (int)upper;

            // Are there any integers between the numbers?
            if (lower.IsInteger() && upper.IsInteger() && iUpper - iLower > 1.0)
                return (double)((iLower + iUpper) / 2);

            double avg = (lower + upper) / 2;

            if (iLower != iUpper)
            {
                return (double)avg;
            }

            return avg;
        }

        public static double FindLowerTestPoint(double point)
        {
            if (point.IsInteger())
                return point - 1.0;
            return Math.Round(point - 1.0);
        }

        public static double FindUpperTestPoint(double point)
        {
            if (point.IsInteger())
                return point + 1.0;
            return Math.Round(point + 1.0);
        }

        public static Restriction[] FromAllBut(ExComp value, AlgebraComp varFor)
        {
            return NotRestriction.FromSolution(value, varFor);
        }

        public static Restriction[] ConstructAllBut(ExComp value, AlgebraComp varFor, ref TermType.EvalData pEvalData)
        {
            OrRestriction lower = new OrRestriction(varFor, LexemeType.Less, value, ref pEvalData);
            OrRestriction upper = new OrRestriction(varFor, LexemeType.Greater, value, ref pEvalData);

            return new Restriction[] { lower, upper };
        }

        public static Restriction FromOnly(ExComp value, AlgebraComp varFor, ref TermType.EvalData pEvalData)
        {
            return new AndRestriction(value, LexemeType.Less, varFor, LexemeType.Less, value, ref pEvalData);
        }

        public static Restriction IntersectRestrictions(Restriction rest0, Restriction rest1, ref TermType.EvalData pEvalData)
        {
            if ((rest0 is OrRestriction && (rest0 as OrRestriction).IsNoRealNums) ||
                (rest1 is OrRestriction && (rest1 as OrRestriction).IsNoRealNums))
                return null;

            ExComp lower0 = rest0.GetLower();
            ExComp upper0 = rest0.GetUpper();
            ExComp lower1 = rest1.GetLower();
            ExComp upper1 = rest1.GetUpper();

            if (lower0 is AlgebraTerm)
                lower0 = (lower0 as AlgebraTerm).RemoveRedundancies();
            if (lower1 is AlgebraTerm)
                lower1 = (lower1 as AlgebraTerm).RemoveRedundancies();
            if (upper0 is AlgebraTerm)
                upper0 = (upper0 as AlgebraTerm).RemoveRedundancies();
            if (upper1 is AlgebraTerm)
                upper1 = (upper1 as AlgebraTerm).RemoveRedundancies();

            if (lower0 is Constant)
                lower0 = (lower0 as Constant).Value;
            if (upper0 is Constant)
                upper0 = (upper0 as Constant).Value;
            if (lower1 is Constant)
                lower1 = (lower1 as Constant).Value;
            if (upper1 is Constant)
                upper1 = (upper1 as Constant).Value;

            lower0 = Simplifier.HarshSimplify(lower0.Clone().ToAlgTerm(), ref pEvalData, false);
            lower1 = Simplifier.HarshSimplify(lower1.Clone().ToAlgTerm(), ref pEvalData, false);
            upper0 = Simplifier.HarshSimplify(upper0.Clone().ToAlgTerm(), ref pEvalData, false);
            upper1 = Simplifier.HarshSimplify(upper1.Clone().ToAlgTerm(), ref pEvalData, false);

            if (!(lower0 is Number) || !(lower1 is Number) || !(upper0 is Number) || !(upper1 is Number))
                return null;

            Number nLower0 = (Number)lower0;
            Number nUpper0 = (Number)upper0;
            Number nLower1 = (Number)lower1;
            Number nUpper1 = (Number)upper1;

            if (nLower1 > nUpper0 || nLower0 > nUpper1 || nUpper0 < nLower0 || nUpper1 < nLower1)
            {
                return null;
            }

            ExComp start, end;
            bool lowerInclusive, upperInclusive;

            if (nLower0 < nLower1)
            {
                start = rest1.GetLower();
                lowerInclusive = rest1.IsLowerInclusive();

                if (!lowerInclusive && nUpper0 == nLower1)
                    return null;
            }
            else
            {
                start = rest0.GetLower();
                lowerInclusive = rest0.IsLowerInclusive();

                if (!lowerInclusive && nLower0 == nUpper1)
                    return null;
            }

            if (nUpper0 < nUpper1)
            {
                end = rest0.GetUpper();
                upperInclusive = rest0.IsUpperInclusive();

                if (!upperInclusive && nLower0 == nUpper1)
                    return null;
            }
            else
            {
                end = rest1.GetUpper();
                upperInclusive = rest1.IsUpperInclusive();

                if (!upperInclusive && nUpper0 == nLower1)
                    return null;
            }

            return new AndRestriction(start, lowerInclusive ? LexemeType.LessEqual : LexemeType.Less,
                rest0.VarComp, upperInclusive ? LexemeType.LessEqual : LexemeType.Less, end, ref pEvalData);
        }

        public static LexemeType InvertComparison(LexemeType lt)
        {
            switch (lt)
            {
                case LexemeType.Greater:
                    return LexemeType.Less;
                    break;

                case LexemeType.GreaterEqual:
                    return LexemeType.LessEqual;
                    break;

                case LexemeType.Less:
                    return LexemeType.Greater;
                    break;

                case LexemeType.LessEqual:
                    return LexemeType.GreaterEqual;
                    break;

                default:
                    throw new ArgumentException("Inequality sign not input.");
            }
        }

        public static bool IsEqualTo(LexemeType lt)
        {
            if (lt == LexemeType.LessEqual || lt == LexemeType.GreaterEqual)
                return true;
            return false;
        }

        public static bool IsGreaterThan(LexemeType lt)
        {
            if (lt == LexemeType.Greater || lt == LexemeType.GreaterEqual)
                return true;
            return false;
        }

        public static LexemeType SwitchComparisonType(LexemeType lt)
        {
            switch (lt)
            {
                case LexemeType.Greater:
                    return LexemeType.Less;

                case LexemeType.GreaterEqual:
                    return LexemeType.LessEqual;

                case LexemeType.Less:
                    return LexemeType.Greater;

                case LexemeType.LessEqual:
                    return LexemeType.GreaterEqual;

                default:
                    throw new ArgumentException("Inequality sign not input.");
            }
        }

        public abstract ExComp GetLower();

        public abstract ExComp GetUpper();

        public abstract bool IsLowerInclusive();

        public abstract bool IsUpperInclusive();

        public abstract bool IsValidValue(ExComp value, ref TermType.EvalData pEvalData);

        public abstract string ToMathAsciiStr();
    }

    internal class Solution
    {
        public ExComp AlternateResult;
        public ExComp ApproximateResult;
        public Parsing.LexemeType ComparisonOp;
        public bool ForceFormatting = false;
        public GeneralSolution GeneralResult;
        public int Multiplicity;
        public ExComp Result;
        public ExComp SolveFor;

        public Solution(ExComp result)
        {
            Result = result;
            SolveFor = null;
            ApproximateResult = null;
            AlternateResult = null;
            ComparisonOp = Parsing.LexemeType.EqualsOp;
            Multiplicity = 1;
            GeneralResult = null;
        }

        public Solution(ExComp result, Parsing.LexemeType comparisonOp)
        {
            Result = result;
            SolveFor = null;
            ApproximateResult = null;
            AlternateResult = null;
            ComparisonOp = comparisonOp;
            Multiplicity = 1;
            GeneralResult = null;
        }

        public Solution(ExComp solveFor, ExComp result)
        {
            SolveFor = solveFor;
            Result = result;
            ApproximateResult = null;
            AlternateResult = null;
            ComparisonOp = Parsing.LexemeType.EqualsOp;
            Multiplicity = 1;
            GeneralResult = null;
        }

        public Solution(ExComp solveFor, ExComp result, ExComp approximateResult)
        {
            SolveFor = solveFor;
            Result = result;
            ApproximateResult = approximateResult;
            AlternateResult = null;
            ComparisonOp = Parsing.LexemeType.EqualsOp;
            Multiplicity = 1;
            GeneralResult = null;
        }

        public static Solution FromGeneralSol(ExComp solveFor, GeneralSolution generalSol)
        {
            Solution sol = new Solution(null);
            sol.SolveFor = solveFor;
            sol.GeneralResult = generalSol;

            return sol;
        }

        public string AlternateToMathAsciiStr()
        {
            AlgebraTerm toTerm = AlternateResult.ToAlgTerm();
            if (toTerm == null)
                return Result.ToAsciiString();
            if (ForceFormatting)
                return toTerm.FinalDispKeepFormatting();
            return toTerm.FinalToDispStr();
        }

        public string AlternateToTexStr()
        {
            AlgebraTerm toTerm = AlternateResult.ToAlgTerm();
            if (toTerm == null)
                return Result.ToTexString();
            return toTerm.FinalToTexString();
        }

        public string ApproximateToMathAsciiStr()
        {
            AlgebraTerm toTerm = ApproximateResult.ToAlgTerm();
            if (toTerm == null)
                return Result.ToAsciiString();
            if (ForceFormatting)
                return toTerm.FinalDispKeepFormatting();
            return toTerm.FinalToDispStr();
        }

        public string ApproximateToTexStr()
        {
            AlgebraTerm toTerm = ApproximateResult.ToAlgTerm();
            if (toTerm == null)
                return Result.ToTexString();
            return toTerm.FinalToTexString();
        }

        public void CalculateApproximate(ref TermType.EvalData pEvalData)
        {
            if (Result is AlgebraTerm)
                Result = (Result as AlgebraTerm).RemoveRedundancies();

            if (Result != null && !(Result is SpecialSolution) && ApproximateResult == null)
            {
                ExComp harshSimpResult = Simplifier.HarshSimplify(Result.Clone().ToAlgTerm(), ref pEvalData);
                if (harshSimpResult is AlgebraTerm)
                    harshSimpResult = (harshSimpResult as AlgebraTerm).RemoveRedundancies();
                if (!harshSimpResult.IsEqualTo(Result))
                    ApproximateResult = harshSimpResult;
            }
        }

        public string ComparisonOpToMathAsciiStr()
        {
            switch (ComparisonOp)
            {
                case Parsing.LexemeType.EqualsOp:
                    return "=";

                case Parsing.LexemeType.Greater:
                    return ">";

                case Parsing.LexemeType.GreaterEqual:
                    return ">=";

                case Parsing.LexemeType.Less:
                    return "<";

                case Parsing.LexemeType.LessEqual:
                    return "<=";
            }

            throw new ArgumentException("Not valid comparison operator.");
        }

        public string ComparisonOpToTexStr()
        {
            switch (ComparisonOp)
            {
                case Parsing.LexemeType.EqualsOp:
                    return "=";

                case Parsing.LexemeType.Greater:
                    return ">";

                case Parsing.LexemeType.GreaterEqual:
                    return ">=";

                case Parsing.LexemeType.Less:
                    return "<";

                case Parsing.LexemeType.LessEqual:
                    return "<=";
            }

            throw new ArgumentException("Not valid comparison operator.");
        }

        public string GeneralToMathAsciiStr()
        {
            if (ForceFormatting)
                return GeneralResult.FinalDispKeepFormatting();
            return GeneralResult.FinalToDispStr();
        }

        public string GeneralToTexStr()
        {
            return GeneralResult.FinalToTexString();
        }

        public string ResultToMathAsciiStr()
        {
            AlgebraTerm toTerm = Result.ToAlgTerm();
            if (toTerm == null)
                return Result.ToAsciiString();
            if (ForceFormatting)
                return toTerm.FinalDispKeepFormatting();
            return toTerm.FinalToDispStr();
        }

        public string ResultToTexStr()
        {
            AlgebraTerm toTerm = Result.ToAlgTerm();
            if (toTerm == null)
                return Result.ToTexString();
            return toTerm.FinalToTexString();
        }

        public string SolveForToMathAsciiStr()
        {
            return SolveFor.ToAsciiString();
        }

        public string SolveForToTexStr()
        {
            return SolveFor.ToTexString();
        }

        public SolveResult ToSolveResult()
        {
            return new SolveResult(this);
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return SolveForToTexStr() + ComparisonOpToTexStr() + ResultToTexStr();
            return SolveFor.ToString() + ComparisonOpToTexStr() + Result.ToString();
        }
    }
}