using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class DivOp : AgOp
    {
        private const int MAX_POLY_DIV_COUNT = 12;

        public static ExComp AttemptPolyDiv(PolynomialExt dividend, PolynomialExt divisor, ref TermType.EvalData pEvalData)
        {
            pEvalData.AttemptSetInputType(TermType.InputType.PolyDiv);

            if (dividend.GetInfo().GetTermCount() > MAX_POLY_DIV_COUNT || divisor.GetInfo().GetTermCount() > MAX_POLY_DIV_COUNT)
                return null;

            AlgebraTerm divided = new AlgebraTerm();
            ExComp remainder = null;

            AlgebraComp varFor = dividend.GetInfo().GetVar();

            // Change this maybe? This might only be valid with ascii math.
            string startingWork = "`{:(,),(" + divisor.ToMathAsciiStr() + ",bar(\")\"" + dividend.ToMathAsciiStr() + ")):}`";

            string previousWork = "),(" + divisor.ToMathAsciiStr() + ",bar(\")\"" + dividend.ToMathAsciiStr() + "))";

            for (; ; )
            {
                pEvalData.GetWorkMgr().FromFormatted("`{:(," + divided.FinalToDispStr() + previousWork + ":}`");

                if (dividend.GetMaxPow() < divisor.GetMaxPow())
                {
                    // There is a remainder.
                    remainder = dividend.ToAlgTerm().RemoveRedundancies(false);
                    break;
                }
                int divPow = dividend.GetMaxPow() - divisor.GetMaxPow();
                ExComp divCoeff = DivOp.StaticCombine(dividend.GetLeadingCoeff(), divisor.GetLeadingCoeff());

                ExComp[] singularGroup =
                    new ExComp[]
                {
                    MulOp.StaticCombine(divCoeff, PowOp.StaticCombine(varFor, new ExNumber(divPow)))
                };

                divided.AddGroup(singularGroup);

                // We are sure the first coefficients will cancel.
                LoosePolyInfo coeffs = dividend.GetInfo();
                LoosePolyInfo subCoeffs = divisor.GetInfo().Clone();

                for (int j = 0; j < subCoeffs.GetInfo().Count; ++j)
                {
                    subCoeffs.GetInfo()[j].SetData1(MulOp.StaticCombine(divCoeff, subCoeffs.GetInfo()[j].GetData1()));
                    subCoeffs.GetInfo()[j].SetData2(subCoeffs.GetInfo()[j].GetData2() + divPow);
                }

                previousWork += ",(," + subCoeffs.GetNeg().ToMathAsciiStr() + ")";
                pEvalData.GetWorkMgr().FromFormatted("`{:(," + divided.FinalToDispStr() + previousWork + ":}`");

                for (int j = 0; j < subCoeffs.GetInfo().Count; ++j)
                {
                    TypePair<ExComp, int> subCoeffInfo = subCoeffs.GetInfo()[j];
                    if (coeffs.HasPower(subCoeffInfo.GetData2()))
                    {
                        ExComp coeff = coeffs.GetCoeffForPow(subCoeffInfo.GetData2());
                        coeff = SubOp.StaticCombine(coeff, subCoeffInfo.GetData1());
                        if (ExNumber.GetZero().IsEqualTo(coeff))
                            coeffs.RemovePowCoeffPair(subCoeffInfo.GetData2());
                        else
                            coeffs.SetCoeffForPow(subCoeffInfo.GetData2(), coeff);
                    }
                    else
                    {
                        coeffs.GetInfo().Add(new TypePair<ExComp, int>(subCoeffInfo.GetData1(), subCoeffInfo.GetData2()));
                    }
                }

                previousWork += ",(,bar(" + coeffs.ToMathAsciiStr() + "))";
                pEvalData.GetWorkMgr().FromFormatted("`{:(," + divided.FinalToDispStr() + previousWork + ":}`");

                if (coeffs.GetTermCount() == 0)
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
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + (divided as AlgebraTerm).FinalToDispStr() + WorkMgr.EDM,
                    "Above is the divided result without the remainder. Since the division produced " +
                    "a remainder of " + WorkMgr.STM +
                    (remainder is AlgebraTerm ? (remainder as AlgebraTerm).FinalToDispStr() : remainder.ToAsciiString()) +
                    WorkMgr.EDM + " the remainder of the result = " + WorkMgr.STM + "\\frac{\\text{Division Remainder}}{\\text{Divisor}}" + WorkMgr.EDM +
                    ". This comes from the statement that " + WorkMgr.STM + "\\frac{f(x)}{g(x)}=q(x)+\\frac{r(x)}{d(x)}" + WorkMgr.EDM);
                remainder = DivOp.StaticWeakCombine(remainder, divisor.ToAlgTerm());
                ExComp[] singularGroup = new ExComp[] { remainder };
                divided.AddGroup(singularGroup);

                finalStepDesc = "Add in the remainder.";
            }
            else
            {
                finalStepDesc = "There was no remainder.";
            }

            pEvalData.GetWorkMgr().FromFormatted(
                    WorkMgr.STM + (divided is AlgebraTerm ? (divided as AlgebraTerm).FinalToDispStr() : divided.ToAsciiString()) + WorkMgr.EDM,
                    finalStepDesc);

            return divided;
        }

        public static ExComp FactorOutTerm(ExComp exComp, ExComp factorOutTerm)
        {
            if (exComp.IsEqualTo(factorOutTerm))
                return ExNumber.GetOne();

            if (exComp is PowerFunction && factorOutTerm is PowerFunction)
            {
                PowerFunction pfExCmp = exComp as PowerFunction;
                PowerFunction pfFactorOut = factorOutTerm as PowerFunction;

                if (pfExCmp.GetBase().IsEqualTo(pfFactorOut.GetBase()))
                    return AlgebraTerm.OpDiv(pfExCmp, pfFactorOut);
            }
            else if (exComp is PowerFunction && factorOutTerm is AlgebraTerm)
            {
                PowerFunction pfExCmp = exComp as PowerFunction;
                AlgebraTerm atFactorOut = factorOutTerm as AlgebraTerm;

                if (pfExCmp.GetBase().IsEqualTo(atFactorOut))
                    return AlgebraTerm.OpDiv(pfExCmp, new PowerFunction(atFactorOut, ExNumber.GetOne()));
            }
            else if (exComp is AlgebraTerm && factorOutTerm is PowerFunction)
            {
                AlgebraTerm atExCmp = exComp as AlgebraTerm;
                PowerFunction pfFactorOut = factorOutTerm as PowerFunction;

                if (pfFactorOut.GetBase().IsEqualTo(atExCmp))
                    return PowerFunction.OpDiv((new PowerFunction(atExCmp, ExNumber.GetOne())), pfFactorOut);
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
                            List<ExComp> removedGroup = ArrayFunc.ToList(singleGroup);
                            ArrayFunc.RemoveIndex(removedGroup, i);
                            return GroupHelper.ToAlgTerm(removedGroup.ToArray());
                        }
                        if (singleGroupComp is PowerFunction)
                        {
                            PowerFunction pfSingleGroupComp = singleGroupComp as PowerFunction;
                            if (pfSingleGroupComp.GetBase().IsEqualTo(factorOutTerm))
                            {
                                ExComp changedPow = SubOp.StaticCombine(pfSingleGroupComp.GetPower().CloneEx(), ExNumber.GetOne());
                                if (!(changedPow is ExNumber))
                                    continue;
                                if (!(changedPow as ExNumber).IsRealInteger())
                                    continue;
                                if (ExNumber.GetOne().IsEqualTo(changedPow))
                                {
                                    singleGroup[i] = pfSingleGroupComp.GetBase();
                                }
                                else
                                {
                                    (singleGroup[i] as PowerFunction).SetPower(changedPow);
                                }

                                return GroupHelper.ToAlgTerm(singleGroup);
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
                ExComp[] singularGroup = new ExComp[] { exComp };
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
                ExComp[] singularGp = new ExComp[] { ExNumber.GetOne() };
                return singularGp;
            }

            if (factorOutTerm is PowerFunction)
            {
                PowerFunction powFuncFactorOut = factorOutTerm as PowerFunction;
                for (int i = 0; i < group.Length; ++i)
                {
                    if (!(group[i] is PowerFunction) && !(group[i].IsEqualTo(powFuncFactorOut.GetBase())))
                    {
                        continue;
                    }
                    PowerFunction powFuncGroup = group[i] is PowerFunction ? group[i] as PowerFunction :
                        new PowerFunction(group[i], ExNumber.GetOne());
                    if (powFuncFactorOut.GetBase().IsEqualTo(powFuncGroup.GetBase()))
                    {
                        ExComp factoredOut = PowerFunction.OpDiv(powFuncGroup, powFuncFactorOut);
                        if (factoredOut is AlgebraTerm)
                        {
                            factoredOut = (factoredOut as AlgebraTerm).RemoveRedundancies(false);
                        }
                        if (!ExNumber.GetOne().IsEqualTo(factoredOut) || group.Length == 1)
                            group[i] = factoredOut;
                        else
                        {
                            // Don't bother including one.
                            List<ExComp> groupList = ArrayFunc.ToList(group);
                            ArrayFunc.RemoveIndex(groupList, i);
                            return groupList.ToArray();
                        }

                        if (factoredOut is PowerFunction)
                        {
                            PowerFunction finalPowFunc = factoredOut as PowerFunction;
                            if (finalPowFunc.GetPower() is AlgebraTerm)
                            {
                                AlgebraTerm finalPowTerm = finalPowFunc.GetPower() as AlgebraTerm;

                                if (finalPowTerm.IsOne())
                                    group[i] = (finalPowFunc.GetBase());
                                else if (finalPowTerm.IsZero())
                                {
                                    List<ExComp> removedGroup = new List<ExComp>();
                                    ArrayFunc.RemoveIndex(removedGroup, i);
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
                for (int i = 0; i < group.Length; ++i)
                {
                    if (GroupHelper.CompsRelatable(group[i], factorOutTerm))
                    {
                        ExComp groupRelatableComp = group[i];
                        if (groupRelatableComp is AlgebraComp)
                        {
                            List<ExComp> removedGroup = ArrayFunc.ToList(group);
                            ArrayFunc.RemoveIndex(removedGroup, i);
                            return removedGroup.ToArray();
                        }
                        else if (groupRelatableComp is PowerFunction)
                        {
                            PowerFunction groupRelatablePowFunc = groupRelatableComp as PowerFunction;
                            groupRelatablePowFunc.SetPower(SubOp.StaticCombine(groupRelatablePowFunc.GetPower(), ExNumber.GetOne()));
                            if (ExNumber.GetOne().IsEqualTo(groupRelatablePowFunc.GetPower()))
                            {
                                group[i] = groupRelatablePowFunc.GetBase();
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

                ExComp[] matchGp = GroupHelper.CloneGroup(group);

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

                        matchGp[matchIndices.GetData1()] = FactorOutTerm(matchPair.GetData1(), matchPair.GetData2());
                    }
                }
                if (allGroupMatchesFound)
                {
                    group = matchGp;
                }

                group = GroupHelper.RemoveOneCoeffs(group);

                return group;
            }
            else if (factorOutTerm is ExNumber)
            {
                for (int i = 0; i < group.Length; ++i)
                {
                    if (group[i] is ExNumber)
                    {
                        group[i] = ExNumber.OpDiv((group[i] as ExNumber), (factorOutTerm as ExNumber));
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

                    if (powFunc1.GetBase().IsEqualTo(powFunc2.GetBase()))
                    {
                        ExComp pow1 = powFunc1.GetPower() is AlgebraTerm ? (powFunc1.GetPower() as AlgebraTerm).RemoveRedundancies(false) : powFunc1.GetPower();
                        ExComp pow2 = powFunc2.GetPower() is AlgebraTerm ? (powFunc2.GetPower() as AlgebraTerm).RemoveRedundancies(false) : powFunc2.GetPower();

                        ExComp origPow1 = pow1.CloneEx();
                        ExComp origPow2 = pow2.CloneEx();

                        if (pow1 is AlgebraTerm)
                        {
                            AlgebraTerm[] pow1Frac = (pow1 as AlgebraTerm).GetNumDenFrac();
                            if (pow1Frac != null)
                            {
                                ExComp pow1Num = pow1Frac[0].RemoveRedundancies(false);
                                ExComp pow1Den = pow1Frac[1].RemoveRedundancies(false);
                                if (pow1Num is ExNumber && pow1Den is ExNumber)
                                {
                                    pow1 = (ExNumber.OpDiv((ExNumber)pow1Num, (ExNumber)pow1Den));
                                }
                            }
                        }

                        if (pow2 is AlgebraTerm)
                        {
                            AlgebraTerm[] pow2Frac = (pow2 as AlgebraTerm).GetNumDenFrac();
                            if (pow2Frac != null)
                            {
                                ExComp pow2Num = pow2Frac[0].RemoveRedundancies(false);
                                ExComp pow2Den = pow2Frac[1].RemoveRedundancies(false);

                                if (pow2Num is ExNumber && pow2Den is ExNumber)
                                    pow2 = ExNumber.OpDiv((ExNumber)pow2Num, (ExNumber)pow2Den);
                            }
                        }

                        if (pow1 is ExNumber && pow2 is ExNumber)
                        {
                            ExNumber pow1Num = pow1 as ExNumber;
                            ExNumber pow2Num = pow2 as ExNumber;
                            ExComp usePow;
                            if (ExNumber.OpLT(pow1Num, pow2Num))
                                usePow = origPow1;
                            else
                                usePow = origPow2;
                            ExComp powerGcf = ExNumber.Minimum(pow1Num, pow2Num);
                            PowerFunction powerFuncGcf = new PowerFunction(powFunc1.GetBase(), usePow);
                            return powerFuncGcf;
                        }

                        return new PowerFunction(powFunc1.GetBase(), pow2);//SubOp.StaticCombine(pow1, pow2));
                    }
                }
            }
            if (ex1 is PowerFunction || ex2 is PowerFunction)
            {
                PowerFunction powFunc = ex1 is PowerFunction ? ex1 as PowerFunction : ex2 as PowerFunction;
                ExComp nonPowFunc = ex1 is PowerFunction ? ex2 : ex1;
                if (powFunc.GetBase().IsEqualTo(nonPowFunc))
                {
                    // Return whichever one has less of a power.
                    if (powFunc.GetPower() is ExNumber)
                        return ExNumber.OpLT((powFunc.GetPower() as ExNumber), ExNumber.GetOne()) ? powFunc : nonPowFunc;
                    else if (powFunc.GetPower() is AlgebraTerm)
                    {
                        SimpleFraction simpFrac = new SimpleFraction();
                        if (simpFrac.Init(powFunc.GetPower() as AlgebraTerm))
                        {
                            if (simpFrac.GetNumEx() is ExNumber && simpFrac.GetDenEx() is ExNumber)
                            {
                                return ExNumber.OpGT((simpFrac.GetNumEx() as ExNumber), (simpFrac.GetDenEx() as ExNumber)) ? nonPowFunc : powFunc;
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
                    bool isTerm1SingleGroup = term1.GetGroupCount() == 1;
                    bool isTerm2SingleGroup = term2.GetGroupCount() == 1;

                    // Check if any of the terms cancel.
                    // This will cancel things like ((x+2)(x-1))/(x+2)

                    ExComp[] numGroupTerms;
                    if (isTerm1SingleGroup)
                    {
                        numGroupTerms = term1.GetGroupsNoOps()[0];
                    }
                    else
                    {
                        ExComp[] singleGroup = new ExComp[] { term1 };
                        numGroupTerms = singleGroup;
                    }

                    ExComp[] denGroupTerms;
                    if (isTerm2SingleGroup)
                    {
                        denGroupTerms = term2.GetGroupsNoOps()[0];
                    }
                    else
                    {
                        ExComp[] singleGroup = new ExComp[] { term2 };
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

                bool allHaveComp = GroupHelper.GetRelatableTermOfGroup(gcfGroup, comp) != null;

                if (allHaveComp)
                {
                    return comp;
                }
                else
                    return null;
            }
            else if ((ex1 is AlgebraTerm && ex2 is ExNumber) ||
                (ex2 is AlgebraTerm && ex1 is ExNumber))
            {
                AlgebraTerm term = ex1 is AlgebraTerm ? ex1 as AlgebraTerm : ex2 as AlgebraTerm;
                ExNumber num = ex2 is ExNumber ? ex2 as ExNumber : ex1 as ExNumber;

                List<ExNumber> coeffs = term.GetCoeffs();
                foreach (ExNumber coeff in coeffs)
                {
                    if (coeff == null)
                        return null;
                }

                ExNumber coeffsGCF = ExNumber.GCF(coeffs);
                ExNumber totalGCF = ExNumber.GCF(coeffsGCF, num);

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
            else if (ex1 is ExNumber && ex2 is ExNumber)
            {
                ExNumber num1 = ex1 as ExNumber;
                ExNumber num2 = ex2 as ExNumber;

                ExNumber resulant = ExNumber.GCF(num1, num2);
                if (num1.GetRealComp() < 0.0 && num2.GetRealComp() < 0.0)
                    resulant.SetRealComp(resulant.GetRealComp()*-1.0);
                if (num1.GetImagComp() < 0.0 && num2.GetImagComp() < 0.0)
                    resulant.SetImagComp(resulant.GetImagComp()*-1.0);

                return resulant;
            }
            return null;    // There was no common factor.
        }

        public static AlgebraTerm GroupDivide(AlgebraTerm term, ExComp div)
        {
            List<ExComp[]> gps = term.GetGroupsNoOps();

            AlgebraTerm dividedTerm = null;
            for (int i = 0; i < gps.Count; ++i)
            {
                ExComp[] gp = gps[i];

                ExComp divided = StaticCombine(GroupHelper.ToAlgTerm(gp), div);
                if (dividedTerm == null)
                    dividedTerm = divided.ToAlgTerm();
                else
                {
                    if (dividedTerm is AlgebraTerm)
                        dividedTerm.Add((divided as AlgebraTerm).GetSubComps().ToArray());
                    else
                        dividedTerm.Add(divided);
                }

                if (i != gps.Count - 1)
                    dividedTerm.Add(new AddOp());
            }

            return dividedTerm;
        }

        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveRedundancies(false);
            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveRedundancies(false);

            if (ex1 is Functions.Calculus.CalcConstant)
                return ex1;
            else if (ex2 is Functions.Calculus.CalcConstant)
                return ex2;

            if (ex2 is ExNumber && ExNumber.OpEqual((ex2 as ExNumber), 0.0))
                return ExNumber.GetUndefined();
            else if (ex1 is ExNumber && ExNumber.OpEqual((ex1 as ExNumber), 0.0))
                return ExNumber.GetZero();
            else if (ExNumber.GetPosInfinity().IsEqualTo(ex1) || ExNumber.GetPosInfinity().IsEqualTo(ex2))
                return ExNumber.GetPosInfinity();
            else if (ExNumber.GetNegInfinity().IsEqualTo(ex1) || ExNumber.GetNegInfinity().IsEqualTo(ex2))
                return ExNumber.GetNegInfinity();

            if (ex1.IsEqualTo(ex2))
                return ExNumber.GetOne();

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

            if (ex1 is AlgebraTerm && AdvAlgebraTerm.IsSimpleFraction((ex1 as AlgebraTerm)) && !(ex2 is AlgebraTerm && AdvAlgebraTerm.IsSimpleFraction((ex2 as AlgebraTerm))))
            {
                SimpleFraction frac = new SimpleFraction();
                if (frac.Init(ex1 as AlgebraTerm))
                {
                    ExComp den = MulOp.StaticCombine(frac.GetDenEx(), ex2);

                    return StaticCombine(frac.GetNumEx(), den);
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

            if (ex1 is ExNumber && ex2 is ExNumber)
            {
                ExNumber nEx1 = ex1 as ExNumber;
                ExNumber nEx2 = ex2 as ExNumber;

                if (nEx1.HasImagRealComp() || nEx2.HasImagRealComp())
                {
                    return ExNumber.OpDiv(nEx1, nEx2);
                }
            }

            ExComp commonFactor = GetCommonFactor(ex1, ex2);
            if (commonFactor is AlgebraTerm)
                commonFactor = (commonFactor as AlgebraTerm).RemoveRedundancies(false);
            if (ExNumber.GetZero().IsEqualTo(commonFactor) || ExNumber.GetOne().IsEqualTo(commonFactor))
                commonFactor = null;
            if (commonFactor != null)
            {
                if (commonFactor is ExNumber)
                {
                    ExNumber commonFactorNumber = commonFactor as ExNumber;
                    ex1 = FactorOutTerm(ex1, commonFactor);
                    ex2 = FactorOutTerm(ex2, commonFactor);

                    if (ex1 is AlgebraTerm)
                        ex1 = (ex1 as AlgebraTerm).RemoveRedundancies(false);
                    if (ex2 is AlgebraTerm)
                        ex2 = (ex2 as AlgebraTerm).RemoveRedundancies(false);
                }
                else
                {
                    ex1 = FactorOutTerm(ex1, commonFactor);
                    ex2 = FactorOutTerm(ex2, commonFactor);
                }
            }
            if (ex1 is AlgebraTerm && ex2 is AlgebraTerm &&
                (ex1 as AlgebraTerm).GetGroupCount() > 1 && (ex2 as AlgebraTerm).GetGroupCount() > 1)
            {
                ExComp[] ex1GcfGroup = ex1.ToAlgTerm().GetGroupGCF();
                if (ex1GcfGroup != null)
                {
                    ExComp ex1Gcf = GroupHelper.ToAlgTerm(ex1GcfGroup).RemoveRedundancies(false);
                    if (ex1Gcf is AlgebraTerm)
                    {
                        (ex1Gcf as AlgebraTerm).ApplyOrderOfOperations();
                        ex1Gcf = (ex1Gcf as AlgebraTerm).MakeWorkable();
                    }

                    if (ex1Gcf is AlgebraTerm)
                        ex1Gcf = (ex1Gcf as AlgebraTerm).RemoveRedundancies(false);

                    if (!ExNumber.GetZero().IsEqualTo(ex1Gcf))
                    {
                        ExComp remainder = FactorOutTerm(ex1.CloneEx(), ex1Gcf);

                        if (ex1Gcf.IsEqualTo(ex2))
                            return remainder;
                        if (remainder.IsEqualTo(ex2))
                            return ex1Gcf;
                    }
                }
            }

            if (ex1 is AlgebraTerm)
                ex1 = (ex1 as AlgebraTerm).RemoveZeros();

            if (ex2 is ExNumber && ExNumber.OpEqual((ex2 as ExNumber), 1.0))
            {
                return ex1;
            }

            if (ex2 is AlgebraTerm)
                ex2 = (ex2 as AlgebraTerm).RemoveZeros();

            if (ex1 is AlgebraTerm)
            {
                AlgebraTerm[] ex1NumDen = (ex1 as AlgebraTerm).GetNumDenFrac();
                if (ex1NumDen != null)
                {
                    ex1 = ex1NumDen[0];
                    ex2 = MulOp.StaticCombine(ex2, ex1NumDen[1]);
                }
            }
            if (ex2 is AlgebraTerm)
            {
                AlgebraTerm[] ex2NumDen = (ex2 as AlgebraTerm).GetNumDenFrac();
                if (ex2NumDen != null)
                {
                    ex2 = ex2NumDen[0];
                    ex1 = MulOp.StaticCombine(ex1, ex2NumDen[1]);
                }
            }

            AlgebraTerm agTerm = AlgebraTerm.FromFraction(ex1, ex2);

            return agTerm;
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            return AlgebraTerm.FromFraction(ex1, ex2);
        }

        public override ExComp CloneEx()
        {
            return new DivOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
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