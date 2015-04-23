using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class DivOp : AgOp
    {
        private const int MAX_POLY_DIV_COUNT = 12;

        public static ExComp AttemptPolyDiv(PolynomialExt dividend, PolynomialExt divisor, ref TermType.EvalData pEvalData)
        {
            pEvalData.AttemptSetInputType(TermType.InputType.PolyDiv);

            if (dividend.Info.TermCount > MAX_POLY_DIV_COUNT || divisor.Info.TermCount > MAX_POLY_DIV_COUNT)
                return null;

            AlgebraTerm divided = new AlgebraTerm();
            ExComp remainder = null;

            AlgebraComp varFor = dividend.Info.Var;

            // Change this maybe? This might only be valid with ascii math.
            string startingWork = "`{:(,),(" + divisor.ToMathAsciiStr() + ",bar(\")\"" + dividend.ToMathAsciiStr() + ")):}`";

            string previousWork = "),(" + divisor.ToMathAsciiStr() + ",bar(\")\"" + dividend.ToMathAsciiStr() + "))";

            for (; ; )
            {
                pEvalData.WorkMgr.FromFormatted("`{:(," + divided.FinalToDispStr() + previousWork + ":}`");

                if (dividend.MaxPow < divisor.MaxPow)
                {
                    // There is a remainder.
                    remainder = dividend.ToAlgTerm().RemoveRedundancies();
                    break;
                }
                int divPow = dividend.MaxPow - divisor.MaxPow;
                ExComp divCoeff = DivOp.StaticCombine(dividend.LeadingCoeff, divisor.LeadingCoeff);

                ExComp[] singularGroup =
                {
                    MulOp.StaticCombine(divCoeff, PowOp.StaticCombine(varFor, new Number(divPow)))
                };

                divided.AddGroup(singularGroup);

                // We are sure the first coefficients will cancel.
                LoosePolyInfo coeffs = dividend.Info;
                LoosePolyInfo subCoeffs = divisor.Info.Clone();

                for (int j = 0; j < subCoeffs.Info.Count; ++j)
                {
                    subCoeffs.Info[j].Data1 = MulOp.StaticCombine(divCoeff, subCoeffs.Info[j].Data1);
                    subCoeffs.Info[j].Data2 = subCoeffs.Info[j].Data2 + divPow;
                }

                previousWork += ",(," + subCoeffs.GetNeg().ToMathAsciiStr() + ")";
                pEvalData.WorkMgr.FromFormatted("`{:(," + divided.FinalToDispStr() + previousWork + ":}`");

                for (int j = 0; j < subCoeffs.Info.Count; ++j)
                {
                    TypePair<ExComp, int> subCoeffInfo = subCoeffs.Info[j];
                    if (coeffs.HasPower(subCoeffInfo.Data2))
                    {
                        ExComp coeff = coeffs.GetCoeffForPow(subCoeffInfo.Data2);
                        coeff = SubOp.StaticCombine(coeff, subCoeffInfo.Data1);
                        if (Number.Zero.IsEqualTo(coeff))
                            coeffs.RemovePowCoeffPair(subCoeffInfo.Data2);
                        else
                            coeffs.SetCoeffForPow(subCoeffInfo.Data2, coeff);
                    }
                    else
                    {
                        coeffs.Info.Add(new TypePair<ExComp, int>(subCoeffInfo.Data1, subCoeffInfo.Data2));
                    }
                }

                previousWork += ",(,bar(" + coeffs.ToMathAsciiStr() + "))";
                pEvalData.WorkMgr.FromFormatted("`{:(," + divided.FinalToDispStr() + previousWork + ":}`");

                if (coeffs.TermCount == 0)
                {
                    // There was no remainded the term was divided evenly.
                    break;
                }

                dividend = new PolynomialExt();
                if (!dividend.InitLPI(coeffs))
                    return null;
            }

            string finalStepDesc;

            if (remainder != null)
            {
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + (divided as AlgebraTerm).FinalToDispStr() + WorkMgr.EDM,
                    "Above is the divided result without the remainder. Since the division produced " +
                    "a remainder of " + WorkMgr.STM +
                    (remainder is AlgebraTerm ? (remainder as AlgebraTerm).FinalToDispStr() : remainder.ToAsciiString()) +
                    WorkMgr.EDM + " the remainder of the result = " + WorkMgr.STM + "\\frac{\\text{Division Remainder}}{\\text{Divisor}}" + WorkMgr.EDM +
                    ". This comes from the statement that " + WorkMgr.STM + "\\frac{f(x)}{g(x)}=q(x)+\\frac{r(x)}{d(x)}" + WorkMgr.EDM);
                remainder = DivOp.StaticWeakCombine(remainder, divisor.ToAlgTerm());
                ExComp[] singularGroup = { remainder };
                divided.AddGroup(singularGroup);

                finalStepDesc = "Add in the remainder.";
            }
            else
            {
                finalStepDesc = "There was no remainder.";
            }

            pEvalData.WorkMgr.FromFormatted(
                    WorkMgr.STM + (divided is AlgebraTerm ? (divided as AlgebraTerm).FinalToDispStr() : divided.ToAsciiString()) + WorkMgr.EDM,
                    finalStepDesc);

            return divided;
        }

        public static ExComp FactorOutTerm(ExComp exComp, ExComp factorOutTerm)
        {
            if (exComp.IsEqualTo(factorOutTerm))
                return Number.One;

            if (exComp is PowerFunction && factorOutTerm is PowerFunction)
            {
                PowerFunction pfExCmp = exComp as PowerFunction;
                PowerFunction pfFactorOut = factorOutTerm as PowerFunction;

                if (pfExCmp.Base.IsEqualTo(pfFactorOut.Base))
                    return pfExCmp / pfFactorOut;
            }
            else if (exComp is PowerFunction && factorOutTerm is AlgebraTerm)
            {
                PowerFunction pfExCmp = exComp as PowerFunction;
                AlgebraTerm atFactorOut = factorOutTerm as AlgebraTerm;

                if (pfExCmp.Base.IsEqualTo(atFactorOut))
                    return pfExCmp / new PowerFunction(atFactorOut, Number.One);
            }
            else if (exComp is AlgebraTerm && factorOutTerm is PowerFunction)
            {
                AlgebraTerm atExCmp = exComp as AlgebraTerm;
                PowerFunction pfFactorOut = factorOutTerm as PowerFunction;

                if (pfFactorOut.Base.IsEqualTo(atExCmp))
                    return (new PowerFunction(atExCmp, Number.One)) / pfFactorOut;
            }

            if (exComp is AlgebraTerm)
            {
                AlgebraTerm term = exComp as AlgebraTerm;
                List<ExComp[]> groups = term.PopGroupsNoOps();

                // First try and just cancel one of the terms.
                if (groups.Count == 1)
                {
                    ExComp[] singleGroup = groups[0];
                    for (int i = 0; i < singleGroup.Length; ++i)
                    {
                        ExComp singleGroupComp = singleGroup[i];
                        if (singleGroupComp.IsEqualTo(factorOutTerm))
                        {
                            // The factor out term cancels.
                            List<ExComp> removedGroup = singleGroup.ToList();
                            removedGroup.RemoveAt(i);
                            return removedGroup.ToArray().ToAlgTerm();
                        }
                        if (singleGroupComp is PowerFunction)
                        {
                            PowerFunction pfSingleGroupComp = singleGroupComp as PowerFunction;
                            if (pfSingleGroupComp.Base.IsEqualTo(factorOutTerm))
                            {
                                ExComp changedPow = SubOp.StaticCombine(pfSingleGroupComp.Power.Clone(), Number.One);
                                if (!(changedPow is Number))
                                    continue;
                                if (!(changedPow as Number).IsRealInteger())
                                    continue;
                                if (Number.One.IsEqualTo(changedPow))
                                {
                                    singleGroup[i] = pfSingleGroupComp.Base;
                                }
                                else
                                {
                                    (singleGroup[i] as PowerFunction).Power = changedPow;
                                }

                                return singleGroup.ToArray().ToAlgTerm();
                            }
                        }
                    }
                }

                for (int i = 0; i < groups.Count; ++i)
                {
                    ExComp[] factoredOut = FactorOutTermGroup(groups[i], factorOutTerm);
                    if (factoredOut == null)
                    {
                        return AlgebraTerm.FromFraction(term.PushGroups(groups), factorOutTerm);
                    }
                    else
                        groups[i] = factoredOut;
                }

                term = term.PushGroups(groups);
                return term;
            }
            else
            {
                ExComp[] singularGroup = { exComp };
                ExComp[] factoredOut = FactorOutTermGroup(singularGroup, factorOutTerm);
                if (factoredOut == null)
                {
                    AlgebraTerm numerator = new AlgebraTerm();
                    numerator.AddGroup(factoredOut);

                    AlgebraTerm fraction = new AlgebraTerm();
                    fraction.Add(numerator, new DivOp(), factorOutTerm);
                    return fraction;
                }
                else
                {
                    AlgebraTerm term = new AlgebraTerm();
                    term.AddGroup(factoredOut);
                    return term;
                }
            }
        }

        public static ExComp[] FactorOutTermGroup(ExComp[] group, ExComp factorOutTerm)
        {
            if (group.Length == 1 && group[0].IsEqualTo(factorOutTerm))
            {
                ExComp[] singularGp = { Number.One };
                return singularGp;
            }

            if (factorOutTerm is PowerFunction)
            {
                PowerFunction powFuncFactorOut = factorOutTerm as PowerFunction;
                for (int i = 0; i < group.Count(); ++i)
                {
                    if (!(group[i] is PowerFunction) && !(group[i].IsEqualTo(powFuncFactorOut.Base)))
                    {
                        continue;
                    }
                    PowerFunction powFuncGroup = group[i] is PowerFunction ? group[i] as PowerFunction :
                        new PowerFunction(group[i], Number.One);
                    if (powFuncFactorOut.Base.IsEqualTo(powFuncGroup.Base))
                    {
                        ExComp factoredOut = powFuncGroup / powFuncFactorOut;
                        if (factoredOut is AlgebraTerm)
                        {
                            factoredOut = (factoredOut as AlgebraTerm).RemoveRedundancies();
                        }
                        if (!Number.One.IsEqualTo(factoredOut) || group.Length == 1)
                            group[i] = factoredOut;
                        else
                        {
                            // Don't bother including one.
                            List<ExComp> groupList = group.ToList();
                            groupList.RemoveAt(i);
                            return groupList.ToArray();
                        }

                        if (factoredOut is PowerFunction)
                        {
                            PowerFunction finalPowFunc = factoredOut as PowerFunction;
                            if (finalPowFunc.Power is AlgebraTerm)
                            {
                                AlgebraTerm finalPowTerm = finalPowFunc.Power as AlgebraTerm;

                                if (finalPowTerm.IsOne())
                                    group[i] = (finalPowFunc.Base);
                                else if (finalPowTerm.IsZero())
                                {
                                    List<ExComp> removedGroup = new List<ExComp>();
                                    removedGroup.RemoveAt(i);
                                    return removedGroup.ToArray();
                                }
                            }
                        }
                        return group;
                    }
                }
            }
            else if (factorOutTerm is AlgebraComp)
            {
                for (int i = 0; i < group.Count(); ++i)
                {
                    if (GroupHelper.CompsRelatable(group[i], factorOutTerm))
                    {
                        ExComp groupRelatableComp = group[i];
                        if (groupRelatableComp is AlgebraComp)
                        {
                            List<ExComp> removedGroup = group.ToList();
                            removedGroup.RemoveAt(i);
                            return removedGroup.ToArray();
                        }
                        else if (groupRelatableComp is PowerFunction)
                        {
                            PowerFunction groupRelatablePowFunc = groupRelatableComp as PowerFunction;
                            groupRelatablePowFunc.Power = SubOp.StaticCombine(groupRelatablePowFunc.Power, Number.One);
                            if (Number.One.IsEqualTo(groupRelatablePowFunc.Power))
                            {
                                group[i] = groupRelatablePowFunc.Base;
                            }
                            else
                                group[i] = groupRelatablePowFunc;

                            return group;
                        }
                    }
                }
            }
            else if (factorOutTerm is AlgebraTerm)
            {
                AlgebraTerm factorOutAgTerm = factorOutTerm as AlgebraTerm;
                List<ExComp[]> groups = factorOutAgTerm.GetGroupsNoOps();

                ExComp[] matchGp = group.CloneGroup();

                bool allGroupMatchesFound = true;
                for (int i = 0; i < groups.Count; ++i)
                {
                    ExComp[] groupToFactorOut = groups[i];
                    List<TypePair<int, int>> matchingIndices;
                    List<TypePair<ExComp, ExComp>> matching = GroupHelper.MatchCorresponding(matchGp, groupToFactorOut, out matchingIndices);
                    if (matching == null || matching.Count == 0)
                    {
                        allGroupMatchesFound = false;
                        break;
                    }

                    for (int j = 0; j < matching.Count; ++j)
                    {
                        TypePair<ExComp, ExComp> matchPair = matching[j];
                        TypePair<int, int> matchIndices = matchingIndices[j];

                        matchGp[matchIndices.Data1] = FactorOutTerm(matchPair.Data1, matchPair.Data2);
                    }
                }
                if (allGroupMatchesFound)
                {
                    group = matchGp;
                }

                group = group.RemoveOneCoeffs();

                return group;
            }
            else if (factorOutTerm is Number)
            {
                for (int i = 0; i < group.Count(); ++i)
                {
                    if (group[i] is Number)
                    {
                        group[i] = (group[i] as Number) / (factorOutTerm as Number);
                        return group;
                    }
                }
            }

            return null;
        }

        public static ExComp GetCommonFactor(ExComp ex1, ExComp ex2)
        {
            if (ex1.IsEqualTo(ex2))
                return ex1;

            if (ex1 is AlgebraFunction && ex2 is AlgebraFunction)
            {
                AlgebraFunction func1 = ex1 as AlgebraFunction;
                AlgebraFunction func2 = ex2 as AlgebraFunction;

                if (func1 is PowerFunction && func2 is PowerFunction)
                {
                    PowerFunction powFunc1 = func1 as PowerFunction;
                    PowerFunction powFunc2 = func2 as PowerFunction;

                    if (powFunc1.Base.IsEqualTo(powFunc2.Base))
                    {
                        ExComp pow1 = powFunc1.Power is AlgebraTerm ? (powFunc1.Power as AlgebraTerm).RemoveRedundancies() : powFunc1.Power;
                        ExComp pow2 = powFunc2.Power is AlgebraTerm ? (powFunc2.Power as AlgebraTerm).RemoveRedundancies() : powFunc2.Power;

                        ExComp origPow1 = pow1.Clone();
                        ExComp origPow2 = pow2.Clone();

                        if (pow1 is AlgebraTerm)
                        {
                            AlgebraTerm[] pow1Frac = (pow1 as AlgebraTerm).GetNumDenFrac();
                            if (pow1Frac != null)
                            {
                                ExComp pow1Num = pow1Frac[0].RemoveRedundancies();
                                ExComp pow1Den = pow1Frac[1].RemoveRedundancies();
                                if (pow1Num is Number && pow1Den is Number)
                                {
                                    pow1 = ((Number)pow1Num / (Number)pow1Den);
                                }
                            }
                        }

                        if (pow2 is AlgebraTerm)
                        {
                            AlgebraTerm[] pow2Frac = (pow2 as AlgebraTerm).GetNumDenFrac();
                            if (pow2Frac != null)
                            {
                                ExComp pow2Num = pow2Frac[0].RemoveRedundancies();
                                ExComp pow2Den = pow2Frac[1].RemoveRedundancies();

                                if (pow2Num is Number && pow2Den is Number)
                                    pow2 = ((Number)pow2Num / (Number)pow2Den);
                            }
                        }

                        if (pow1 is Number && pow2 is Number)
                        {
                            Number pow1Num = pow1 as Number;
                            Number pow2Num = pow2 as Number;
                            ExComp usePow;
                            if (pow1Num < pow2Num)
                                usePow = origPow1;
                            else
                                usePow = origPow2;
                            ExComp powerGcf = Number.Minimum(pow1Num, pow2Num);
                            PowerFunction powerFuncGcf = new PowerFunction(powFunc1.Base, usePow);
                            return powerFuncGcf;
                        }

                        return new PowerFunction(powFunc1.Base, SubOp.StaticCombine(pow1, pow2));
                    }
                }
            }
            if (ex1 is PowerFunction || ex2 is PowerFunction)
            {
                PowerFunction powFunc = ex1 is PowerFunction ? ex1 as PowerFunction : ex2 as PowerFunction;
                ExComp nonPowFunc = ex1 is PowerFunction ? ex2 : ex1;
                if (powFunc.Base.IsEqualTo(nonPowFunc))
                {
                    // Return whichever one has less of a power.
                    if (powFunc.Power is Number)
                        return (powFunc.Power as Number) < Number.One ? powFunc : nonPowFunc;
                    else if (powFunc.Power is AlgebraTerm)
                    {
                        SimpleFraction simpFrac = new SimpleFraction();
                        if (simpFrac.Init(powFunc.Power as AlgebraTerm))
                        {
                            if (simpFrac.NumEx is Number && simpFrac.DenEx is Number)
                            {
                                return (simpFrac.NumEx as Number) > (simpFrac.DenEx as Number) ? nonPowFunc : powFunc;
                            }
                        }
                    }
                    return nonPowFunc;
                }
            }
            if (ex1 is AlgebraTerm && ex2 is AlgebraTerm)
            {
                AlgebraTerm term1 = ex1 as AlgebraTerm;
                AlgebraTerm term2 = ex2 as AlgebraTerm;

                ExComp[] gcfGroup1 = term1.GetGroupGCF();
                if (gcfGroup1 != null && gcfGroup1.Length == 0)
                {
                    gcfGroup1 = new ExComp[1];
                    gcfGroup1[0] = term1;
                }
                ExComp[] gcfGroup2 = term2.GetGroupGCF();
                if (gcfGroup2 != null && gcfGroup2.Length == 0)
                {
                    gcfGroup2 = new ExComp[1];
                    gcfGroup2[0] = term2;
                }

                if (gcfGroup1 == null || gcfGroup2 == null)
                    return null;

                ExComp[] groupsGCF = GroupHelper.GCF(gcfGroup1, gcfGroup2);

                if (groupsGCF.Length == 0)
                {
                    bool isTerm1SingleGroup = term1.GroupCount == 1;
                    bool isTerm2SingleGroup = term2.GroupCount == 1;

                    // Check if any of the terms cancel.
                    // This will cancel things like ((x+2)(x-1))/(x+2)

                    ExComp[] numGroupTerms;
                    if (isTerm1SingleGroup)
                    {
                        numGroupTerms = term1.GetGroupsNoOps()[0];
                    }
                    else
                    {
                        ExComp[] singleGroup = { term1 };
                        numGroupTerms = singleGroup;
                    }

                    ExComp[] denGroupTerms;
                    if (isTerm2SingleGroup)
                    {
                        denGroupTerms = term2.GetGroupsNoOps()[0];
                    }
                    else
                    {
                        ExComp[] singleGroup = { term2 };
                        denGroupTerms = singleGroup;
                    }

                    List<ExComp> commonFactors = new List<ExComp>();
                    foreach (ExComp numGroupTerm in numGroupTerms)
                    {
                        foreach (ExComp denGroupTerm in denGroupTerms)
                        {
                            if (numGroupTerm.IsEqualTo(denGroupTerm))
                                commonFactors.Add(denGroupTerm);
                        }
                    }

                    if (commonFactors.Count == 0)
                    {
                        return null;
                    }

                    return AlgebraTerm.FromFactors(commonFactors);
                }

                AlgebraTerm term = new AlgebraTerm();
                term.AddGroup(groupsGCF);

                return term;
            }
            else if ((ex1 is AlgebraTerm && ex2 is AlgebraComp) ||
                (ex2 is AlgebraTerm && ex1 is AlgebraComp))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                AlgebraComp comp = ex2 is AlgebraComp ? ex2 as AlgebraComp : ex1 as AlgebraComp;

                ExComp[] gcfGroup = term.GetGroupGCF();

                bool allHaveComp = gcfGroup.GetRelatableTermOfGroup(comp) != null;

                if (allHaveComp)
                {
                    return comp;
                }
                else
                    return null;
            }
            else if ((ex1 is AlgebraTerm && ex2 is Number) ||
                (ex2 is AlgebraTerm && ex1 is Number))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                Number num = ex2 is Number ? ex2 as Number : ex1 as Number;

                List<Number> coeffs = term.GetCoeffs();
                foreach (Number coeff in coeffs)
                {
                    if (coeff == null)
                        return null;
                }

                Number coeffsGCF = Number.GCF(coeffs);
                Number totalGCF = Number.GCF(coeffsGCF, num);

                return totalGCF;
            }
            else if (ex1 is AlgebraComp && ex2 is AlgebraComp)
            {
                AlgebraComp agComp1 = ex1 as AlgebraComp;
                AlgebraComp agComp2 = ex2 as AlgebraComp;

                if (agComp1.IsEqualTo(agComp2))
                    return agComp1;
                else
                    return null;
            }
            else if (ex1 is Number && ex2 is Number)
            {
                Number num1 = ex1 as Number;
                Number num2 = ex2 as Number;

                Number resulant = Number.GCF(num1, num2);
                if (num1.RealComp < 0.0 && num2.RealComp < 0.0)
                    resulant.RealComp *= -1.0;
                if (num1.ImagComp < 0.0 && num2.ImagComp < 0.0)
                    resulant.ImagComp *= -1.0;

                return resulant;
            }
            return null;    // There was no common factor.
        }

        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveRedundancies();
            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveRedundancies();

            if (ex1 is Functions.Calculus.CalcConstant)
                return ex1;
            else if (ex2 is Functions.Calculus.CalcConstant)
                return ex2;

            if (ex2 is Number && (ex2 as Number) == 0.0)
                return Number.Undefined;
            else if (ex1 is Number && (ex1 as Number) == 0.0)
                return Number.Zero;
            else if (Number.PosInfinity.IsEqualTo(ex1) || Number.PosInfinity.IsEqualTo(ex2))
                return Number.PosInfinity;
            else if (Number.NegInfinity.IsEqualTo(ex1) || Number.NegInfinity.IsEqualTo(ex2))
                return Number.NegInfinity;

            if (ex1.IsEqualTo(ex2))
                return Number.One;

            if (ex1 is ExMatrix || ex2 is ExMatrix)
            {
                ExMatrix mat;
                ExComp other;
                if (ex1 is ExMatrix)
                {
                    mat = ex1 as ExMatrix;
                    other = ex2;
                }
                else
                {
                    mat = ex2 as ExMatrix;
                    other = ex1;
                }

                ExComp atmpt = MatrixHelper.DivOpCombine(mat, other);
                if (atmpt != null)
                    return atmpt;
            }

            if (ex1 is AlgebraTerm && (ex1 as AlgebraTerm).IsSimpleFraction() && !(ex2 is AlgebraTerm && (ex2 as AlgebraTerm).IsSimpleFraction()))
            {
                SimpleFraction frac = new SimpleFraction();
                if (frac.Init(ex1 as AlgebraTerm))
                {
                    ExComp den = MulOp.StaticCombine(frac.DenEx, ex2);

                    return StaticCombine(frac.NumEx, den);
                }
            }

            if ((ex2 is PowerFunction && (ex2 as PowerFunction).IsDenominator()) &&
                (ex1 is PowerFunction && (ex2 as PowerFunction).IsDenominator()))
            {
                ExComp flipped1 = (ex1 as PowerFunction).FlipFrac();
                ExComp flipped2 = (ex2 as PowerFunction).FlipFrac();

                return MulOp.StaticCombine(flipped1, flipped2);
            }
            else if ((ex2 is PowerFunction && (ex2 as PowerFunction).IsDenominator()) ||
                (ex1 is PowerFunction && (ex1 as PowerFunction).IsDenominator()))
            {
                ExComp regComp = ex1 is PowerFunction ? ex2 : ex1;
                ExComp flippedComp = ex1 is PowerFunction ? (ex1 as PowerFunction).FlipFrac() : (ex2 as PowerFunction).FlipFrac();

                return MulOp.StaticCombine(regComp, flippedComp);
            }

            if (ex2 is AlgebraTerm)
            {
                AlgebraTerm ex2Term = ex2 as AlgebraTerm;

                AlgebraTerm[] numDen = ex2Term.GetNumDenFrac();
                if (numDen != null)
                {
                    AlgebraTerm num = numDen[0];
					AlgebraTerm den = numDen[1];

                    AlgebraTerm reversedFrac = AlgebraTerm.FromFraction(den, num);
                    ExComp reverseFracMulResult = MulOp.StaticCombine(ex1, reversedFrac);
                    return reverseFracMulResult;
                }
            }

            if (ex1 is Number && ex2 is Number)
            {
                Number nEx1 = ex1 as Number;
                Number nEx2 = ex2 as Number;

                if (nEx1.HasImagRealComp() || nEx2.HasImagRealComp())
                {
                    return nEx1 / nEx2;
                }
            }

            ExComp commonFactor = GetCommonFactor(ex1, ex2);
            if (commonFactor is AlgebraTerm)
                commonFactor = (commonFactor as AlgebraTerm).RemoveRedundancies();
            if (Number.Zero.IsEqualTo(commonFactor) || Number.One.IsEqualTo(commonFactor))
                commonFactor = null;
            if (commonFactor != null)
            {
                if (commonFactor is Number)
                {
                    Number commonFactorNumber = commonFactor as Number;
                    ex1 = FactorOutTerm(ex1, commonFactor);
                    ex2 = FactorOutTerm(ex2, commonFactor);

                    if (ex1 is AlgebraTerm)
                        ex1 = (ex1 as AlgebraTerm).RemoveRedundancies();
                    if (ex2 is AlgebraTerm)
                        ex2 = (ex2 as AlgebraTerm).RemoveRedundancies();
                }
                else
                {
                    ex1 = FactorOutTerm(ex1, commonFactor);
                    ex2 = FactorOutTerm(ex2, commonFactor);
                }
            }
            if (ex1 is AlgebraTerm && ex2 is AlgebraTerm &&
                (ex1 as AlgebraTerm).GroupCount > 1 && (ex2 as AlgebraTerm).GroupCount > 1)
            {
                ExComp[] ex1GcfGroup = ex1.ToAlgTerm().GetGroupGCF();
                if (ex1GcfGroup != null)
                {
                    ExComp ex1Gcf = ex1GcfGroup.ToAlgTerm().RemoveRedundancies();
                    ExComp remainder = FactorOutTerm(ex1.Clone(), ex1Gcf);

                    if (ex1Gcf.IsEqualTo(ex2))
                        return remainder;
                    if (remainder.IsEqualTo(ex2))
                        return ex1Gcf;
                }
            }

            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveZeros();

            if (ex2 is Number && (ex2 as Number) == 1.0)
            {
                return ex1;
            }

            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveZeros();

            AlgebraTerm agTerm = AlgebraTerm.FromFraction(ex1, ex2);

            return agTerm;
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            return AlgebraTerm.FromFraction(ex1, ex2);
        }

        public override ExComp Clone()
        {
            return new DivOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override int GetHashCode()
        {
            return (int)((double)"Div".GetHashCode() * Math.E);
        }

        public override string ToString()
        {
            return "/";
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return StaticWeakCombine(ex1, ex2);
        }
    }
}