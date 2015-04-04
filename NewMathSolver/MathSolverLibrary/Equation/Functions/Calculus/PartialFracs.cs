using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.Polynomial;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    class PartialFracs
    {
        public static ExComp Evaluate(AlgebraTerm num, AlgebraTerm den, PolynomialExt numPoly, AlgebraComp dVar, 
            ref IntegrationInfo pIntInfo, ref TermType.EvalData pEvalData)
        {
            // Be sure that something like (x-2)(x^2-1) already in factored form works.
            AlgebraTerm[] factors = den.GetFactors(ref pEvalData);

            if (factors == null || factors.Length == 0)
                return null;

            List<AlgebraTerm[]> decomNumDens = new List<AlgebraTerm[]>();

            int startAlphaChar = (int)'A';
            List<AlgebraComp> usedVars = new List<AlgebraComp>();
            for (int i = 0; i < factors.Length; ++i)
            {
                ExComp factor = factors[i].RemoveRedundancies();
                ExComp useEx;
                if (factor is PowerFunction)
                    useEx = (factor as PowerFunction).Base;
                else
                    useEx = factor;

                int maxPow = 1;
                if (factor is AlgebraTerm)
                {
                    PolynomialExt polyFactor = new PolynomialExt();
                    if (polyFactor.Init(factor as AlgebraTerm))
                        maxPow = polyFactor.MaxPow;
                }

                for (int j = 0; j < maxPow; ++j)
                {
                    AlgebraTerm decomNum = PolynomialGen.GenGenericOfDegree(maxPow - 1, dVar, startAlphaChar, usedVars);
                    if (decomNum == null)
                        return null;
                    startAlphaChar += maxPow;

                    decomNumDens.Add(new AlgebraTerm[] 
                    { 
                        decomNum, 
                        j == 1 ? useEx.ToAlgTerm() : Operators.PowOp.StaticWeakCombine(useEx, new Number(j + 1)).ToAlgTerm()
                    });
                }
            }

            // Multiply the numerators by every single other denominator that isn't that numerator.
            ExComp finalEx = null;
            for (int i = 0; i < decomNumDens.Count; ++i)
            {
                AlgebraTerm[] decomNumDen = decomNumDens[i];

                ExComp combined = decomNumDen[0];
                for (int j = 0; j < decomNumDens.Count; ++j)
                {
                    if (j == i)
                        continue;
                    combined = Operators.MulOp.StaticCombine(combined, decomNumDens[j][1]);
                }

                if (finalEx == null)
                    finalEx = combined;
                else
                    finalEx = Operators.AddOp.StaticCombine(finalEx, combined);
            }
            if (finalEx is AlgebraTerm)
                finalEx = (finalEx as AlgebraTerm).RemoveRedundancies();

            AlgebraTerm finalTerm = finalEx.ToAlgTerm();

            Number nMaxPow = GetMaxPow(finalTerm, dVar);
            if (nMaxPow.IsRealInteger())
                return null;

            int max = (int)nMaxPow.RealComp;

            List<ExComp> decomCoeffs = new List<ExComp>();
            for (int i = max; i >= 0; --i)
            {
                var decomVarGroups = finalTerm.GetGroupContainingTerm(dVar.ToPow(i));
                var decomVarTerms = from decomVarGroup in decomVarGroups
                                    select decomVarGroup.GetUnrelatableTermsOfGroup(dVar).ToAlgTerm();

                AlgebraTerm decomCoeff = new AlgebraTerm();
                foreach (AlgebraTerm aTerm in decomVarTerms)
                {
                    decomCoeff = decomCoeff + aTerm;
                }

                decomCoeffs.Add(decomCoeff);
            }

            // Solve the system of equations for the decomposition coefficients.
            List<EquationSet> equations = new List<EquationSet>();
            for (int i = 0; i < decomCoeffs.Count; ++i)
            {
                ExComp coeffForPow = numPoly.Info.GetCoeffForPow(i);
                ExComp right = coeffForPow ?? Number.Zero;

                equations.Add(new EquationSet(decomCoeffs[i], right, Parsing.LexemeType.EqualsOp));
            }

            AlgebraSolver agSolver = new AlgebraSolver();

            Solving.EquationSystemSolve soe = new Solving.EquationSystemSolve(agSolver);

            List<List<TypePair<Parsing.LexemeType, string>>> lts = new List<List<TypePair<Parsing.LexemeType,string>>>();
            List<TypePair<Parsing.LexemeType, string>> lt = new List<TypePair<Parsing.LexemeType, string>>();
            Dictionary<string, int> allIdens = new Dictionary<string,int>();

            foreach (AlgebraComp usedVar in usedVars)
            {
                allIdens.Add(usedVar.Var.Var, 1);
                lt.Add(new TypePair<Parsing.LexemeType,string>(Parsing.LexemeType.Identifier, usedVar.Var.Var));
            }

            lts.Add(lt);

            SolveResult solveResult = soe.SolveEquationArray(equations, lts, allIdens, ref pEvalData);
            if (!solveResult.Success)
                return null;

            AlgebraTerm overall = new AlgebraTerm();

            // Each of the fraction is integrated seperately.
            for (int i = 0; i < decomNumDens.Count; ++i)
            {
                AlgebraTerm[] numDen = decomNumDens[i];
                foreach (Solution sol in solveResult.Solutions)
                {
                    if (!(sol.SolveFor is AlgebraComp))
                        return null;
                    AlgebraComp solved = (AlgebraComp)sol.SolveFor;
                    numDen[0] = numDen[0].Substitute(solved, sol.Result);
                }

                overall.Add(AlgebraTerm.FromFraction(numDen[0], numDen[1]));
                if (i != decomNumDens.Count - 1)
                    overall.Add(new Operators.AddOp());
            }

            Integral integral = Integral.ConstructIntegral(overall, dVar);
            integral.AddConstant = false;

            finalEx = integral.Evaluate(false, ref pEvalData);
            if (finalEx is Integral)
                return null;

            return finalEx;
        }

        private static Number GetMaxPow(AlgebraTerm term, AlgebraComp varFor)
        {
            Number maxPow = new Number(double.MinValue);
            foreach (ExComp subComp in term.SubComps)
            {
                if (subComp is PowerFunction)
                {
                    PowerFunction pf = subComp as PowerFunction;
                    if (pf.Base.IsEqualTo(varFor) && pf.Power is Number)
                        maxPow = Number.Maximum(maxPow, pf.Power as Number);
                }
            }

            return maxPow;
        }
    }
}