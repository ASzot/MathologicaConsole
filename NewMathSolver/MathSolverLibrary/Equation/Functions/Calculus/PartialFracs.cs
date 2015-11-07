using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.Polynomial;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class PartialFracs
    {
        public static ExComp Split(AlgebraTerm num, AlgebraTerm den, PolynomialExt numPoly, AlgebraComp dVar, ref TermType.EvalData pEvalData)
        {
            den = den.RemoveRedundancies(false).ToAlgTerm();
            AlgebraTerm[] factors = AdvAlgebraTerm.GetFactors(den, ref pEvalData);
            if (factors == null || factors.Length < 2)
                return null;

            List<ExComp> bottomFactors = new List<ExComp>();
            for (int i = 0; i < factors.Length; ++i)
            {
                ExComp factorEx = factors[i].RemoveRedundancies(false);
                if (factorEx is PowerFunction)
                {
                    ExComp pow = (factorEx as PowerFunction).GetPower();
                    if (!(pow is ExNumber) || !(pow as ExNumber).IsRealInteger())
                        return null;

                    int iPow = (int)(pow as ExNumber).GetRealComp();

                    if (iPow < 0)
                        return null;

                    ExComp baseEx = (factorEx as PowerFunction).GetBase();
                    for (int j = 1; j <= iPow; ++j)
                    {
                        bottomFactors.Add(PowOp.StaticCombine(baseEx, new ExNumber(j)));
                    }
                }
                else
                    bottomFactors.Add(factorEx);
            }

            // Then convert over to the expanded fractions with the unknowns in the numerator.
            int alphaNumericChar = (int)'A';

            ExComp[] nums = new ExComp[bottomFactors.Count];
            string[][] usedVars = new string[nums.Length][];

            for (int i = 0; i < bottomFactors.Count; ++i)
            {
                int denGpCount = 1;
                if (bottomFactors[i] is AlgebraTerm)
                    denGpCount = (bottomFactors[i] as AlgebraTerm).GetGroupCount();

                int startChar = alphaNumericChar;
                AlgebraTerm decomNum = PolynomialGen.GenGenericOfDegree(Math.Max(denGpCount - 2, 0), dVar, ref alphaNumericChar);
                if (decomNum == null)
                    return null;

                usedVars[i] = new string[alphaNumericChar - startChar];
                for (int k = startChar, j = 0; k < alphaNumericChar; ++k, ++j)
                    usedVars[i][j] = ((char)k).ToString();

                nums[i] = decomNum;
            }

            string numsDisp = "";
            for (int i = 0; i < nums.Length; ++i)
            {
                numsDisp += "\\frac{" + WorkMgr.ToDisp(nums[i]) + "}{" + WorkMgr.ToDisp(bottomFactors[i]) + "}";
                if (i != nums.Length - 1)
                    numsDisp += "+";
            }

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + "\\frac{" + num.FinalToDispStr() + "}{" + den.FinalToDispStr() + "}=" + numsDisp + WorkMgr.EDM,
                "Split the fraction up.");

            // Now multiply the numerator by each of the other denominators.
            ExComp left = null;
            for (int i = 0; i < nums.Length; ++i)
            {
                ExComp multipliedNumEx = nums[i];
                for (int j = 0; j < bottomFactors.Count; ++j)
                {
                    if (i == j)
                        continue;

                    multipliedNumEx = MulOp.StaticCombine(multipliedNumEx, bottomFactors[j].CloneEx());
                }

                if (left == null)
                    left = multipliedNumEx;
                else
                    left = AddOp.StaticCombine(left, multipliedNumEx);
            }

            AlgebraTerm leftTerm = left.ToAlgTerm();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + leftTerm.FinalToDispStr() + "=" + num.FinalToDispStr() + WorkMgr.EDM,
                "Cross multiply the fractions.");

            List<ExComp> pows = leftTerm.GetPowersOfVar(dVar);
            List<ExComp> decomCoeffs = new List<ExComp>();

            List<int> iPows = new List<int>();
            for (int i = 0; i < pows.Count; ++i)
            {
                ExComp pow = pows[i];
                if (!(pow is ExNumber) || !(pow as ExNumber).IsRealInteger())
                    return null;
                int powInt = (int)(pow as ExNumber).GetRealComp();
                iPows.Add(powInt);
            }

            iPows.Sort();

            for (int i = 0; i < iPows.Count; ++i)
            {
                int pow = iPows[i];

                List<ExComp[]> decomVarGroups = leftTerm.GetGroupContainingTerm(PowOp.StaticCombine(dVar, new ExNumber(pow)));
                AlgebraTerm[] decomVarTermsArr = new AlgebraTerm[decomVarGroups.Count];
                for (int j = 0; j < decomVarGroups.Count; ++j)
                    decomVarTermsArr[j] =
                        GroupHelper.ToAlgTerm(GroupHelper.GetUnrelatableTermsOfGroup(decomVarGroups[i], dVar)); 

                AlgebraTerm decomCoeff = new AlgebraTerm();
                foreach (AlgebraTerm aTerm in decomVarTermsArr)
                {
                    decomCoeff = AlgebraTerm.OpAdd(decomCoeff, aTerm);
                }

                decomCoeffs.Add(decomCoeff);
            }

            // Also add in the constant term.
            List<AlgebraGroup> constantGps = leftTerm.GetGroupsConstantTo(dVar);
            if (constantGps.Count != 0)
            {
                AlgebraTerm constantCoeff = new AlgebraTerm();
                foreach (AlgebraGroup constantGp in constantGps)
                {
                    constantCoeff = AlgebraTerm.OpAdd(constantCoeff, constantGp);
                }

                decomCoeffs.Add(constantCoeff);
                iPows.Add(0);
            }

            // Solve the system of equations for the decomposition coefficients.
            List<EqSet> equations = new List<EqSet>();
            for (int i = 0; i < decomCoeffs.Count; ++i)
            {
                int pow = iPows[i];
                ExComp coeffForPow = numPoly.GetInfo().GetCoeffForPow(pow);
                ExComp right = coeffForPow ?? ExNumber.GetZero();

                equations.Add(new EqSet(decomCoeffs[i], right, Parsing.LexemeType.EqualsOp));
            }
            AlgebraSolver agSolver = new AlgebraSolver();

            Solving.EquationSystemSolve soe = new Solving.EquationSystemSolve(agSolver);

            List<List<TypePair<Parsing.LexemeType, string>>> lts = new List<List<TypePair<Parsing.LexemeType, string>>>();
            Dictionary<string, int> allIdens = new Dictionary<string, int>();

            for (int i = 0; i < usedVars.Length; ++i)
            {
                List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>> lt = new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>();
                for (int j = 0; j < usedVars[i].Length; ++j)
                {
                    lt.Add(new TypePair<Parsing.LexemeType, string>(Parsing.LexemeType.Identifier, usedVars[i][j]));
                    allIdens.Add(usedVars[i][j], 1);
                }

                lts.Add(lt);
                // Add nothing for the other side of the equation.
                // It doesn't matter as they will be compounded upon solving anyways.
                lts.Add(new List<TypePair<MathSolverLibrary.Parsing.LexemeType, string>>());
            }

            SolveResult solveResult = soe.SolveEquationArray(equations, lts, allIdens, ref pEvalData);
            if (!solveResult.Success)
                return null;

            ExComp overall = null;
            for (int i = 0; i < nums.Length; ++i)
            {
                foreach (Solution sol in solveResult.Solutions)
                {
                    if (!(sol.SolveFor is AlgebraComp))
                        return null;
                    AlgebraComp solvedFor = sol.SolveFor as AlgebraComp;
                    nums[i] = nums[i].ToAlgTerm().Substitute(solvedFor, sol.Result);
                }

                AlgebraTerm addFrac = AlgebraTerm.FromFraction(nums[i], bottomFactors[i]);

                if (overall == null)
                    overall = addFrac;
                else
                    overall = AddOp.StaticCombine(overall, addFrac);
            }

            return overall;
        }
    }
}