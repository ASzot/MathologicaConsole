using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal partial class AlgebraTerm : ExComp
    {
        protected List<ExComp> _subComps = new List<ExComp>();

        public int GroupCount
        {
            get { return GetGroups().Count; }
        }

        public List<ExComp> SubComps
        {
            get { return _subComps; }
        }

        public int TermCount
        {
            get { return _subComps.Count; }
        }

        public AlgebraTerm()
        {
        }

        public AlgebraTerm(params ExComp[][] groups)
        {
            foreach (ExComp[] group in groups)
            {
                AddGroup(group);
            }
        }

        public AlgebraTerm(params AlgebraGroup[] groups)
        {
            foreach (AlgebraGroup group in groups)
            {
                AddGroup(group.Group);
            }
        }

        public AlgebraTerm(params ExComp[] addComps)
        {
            _subComps.AddRange(addComps);
        }

        public static void AddTermToGroup(ref ExComp[] group, ExComp comp, bool mulOp = true)
        {
            int groupCount = group.Count();
            if (groupCount == 0)
            {
                Array.Resize<ExComp>(ref group, 1);
                group[0] = comp;
                return;
            }

            int resizeCount = mulOp ? 2 : 1;

            Array.Resize<ExComp>(ref group, groupCount + resizeCount);

            if (mulOp)
                group[groupCount] = new Operators.MulOp();
            group[groupCount + (resizeCount - 1)] = comp;
        }

        public static AlgebraTerm FromFactor(AlgebraComp varFor, ExComp factor)
        {
            // We have the factor (varFor - factor).
            ExComp bTerm;
            if (factor is AlgebraTerm)
            {
                var numDen = (factor as AlgebraTerm).GetNumDenFrac();
                if (numDen != null)
                {
                    bTerm = MulOp.Negate(numDen[0]);
                    return new AlgebraTerm(numDen[1], new MulOp(), varFor, new AddOp(), bTerm);
                }
            }

            bTerm = MulOp.Negate(factor);
            return new AlgebraTerm(varFor, new AddOp(), bTerm);
        }

        public static AlgebraTerm FromFactors(params ExComp[] factors)
        {
            return FromFactors(factors.ToList());
        }

        public static AlgebraTerm FromFactors(List<ExComp> factors)
        {
            AlgebraTerm finalTerm = new AlgebraTerm();
            for (int i = 0; i < factors.Count; ++i)
            {
                finalTerm.Add(factors[i]);
                if (i != factors.Count - 1)
                    finalTerm.Add(new MulOp());
            }

            return finalTerm;
        }

        public static AlgebraTerm FromFraction(ExComp num, ExComp den)
        {
            if (num is AlgebraTerm)
                num = (num as AlgebraTerm).RemoveRedundancies();
            if (den is AlgebraTerm)
                den = (den as AlgebraTerm).RemoveRedundancies();

            if (den is Number && (den as Number) < 0.0)
            {
                num = Operators.MulOp.Negate(num);
                den = Number.NegOne * (den as Number);
            }

            if (((den is AlgebraTerm) && (den as AlgebraTerm).IsOne()) ||
                (den is Number) && (den as Number) == 1.0)
                return num.ToAlgTerm();

            AlgebraTerm term = new AlgebraTerm(num, new Operators.MulOp(), new Functions.PowerFunction(den, Number.NegOne));

            return term;
        }

        public static ExComp[] RemoveCoeffs(ExComp[] group)
        {
            List<ExComp> removedGroups = new List<ExComp>();
            foreach (ExComp groupComp in group)
            {
                if (!(groupComp is Number))
                    removedGroups.Add(groupComp);
            }

            return removedGroups.ToArray();
        }

        public static Number ToNumber(AlgebraTerm term)
        {
            ExComp ex = term.RemoveRedundancies();
            if (ex is Number)
                return ex as Number;
            return null;
        }

        public void Add(params ExComp[] exComps)
        {
            _subComps.AddRange(exComps);
        }

        public void AddGroup(ExComp[] group)
        {
            if (_subComps.Count > 0)
                Add(new Operators.AddOp());

            List<ExComp> finalGroup = new List<ExComp>();
            for (int i = 0; i < group.Count(); ++i)
            {
                ExComp comp = group[i];
                ExComp next = (i == group.Count() - 1) ? null : group[i + 1];
                finalGroup.Add(comp);
                if (!(comp is AgOp) && next != null && !(next is AgOp))
                    finalGroup.Add(new Operators.MulOp());
            }

            Add(finalGroup.ToArray());
        }

        public void AddGroup(ExComp singularGroupTerm)
        {
            ExComp[] singularGp = { singularGroupTerm };
            AddGroup(singularGp);
        }

        public void AddGroups(params ExComp[][] groups)
        {
            foreach (ExComp[] group in groups)
                AddGroup(group);
        }

        public virtual AlgebraTerm ApplyOrderOfOperations()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp subComp = _subComps[i];
                if (subComp is AlgebraTerm)
                {
                    if (subComp is PowerFunction)
                    {
                        ExComp pow = (subComp as PowerFunction).Power;
                        ExComp baseEx = (subComp as PowerFunction).Base;

                        //ExComp tmpPow = null;
                        //if (pow is AlgebraTerm)
                        //    tmpPow = pow.Clone().ToAlgTerm().ApplyOrderOfOperations();
                        if (baseEx is AlgebraTerm)
                            baseEx = (baseEx as AlgebraTerm).ApplyOrderOfOperations();
                        if (pow is AlgebraTerm)
                            pow = (pow as AlgebraTerm).ApplyOrderOfOperations();

                        _subComps[i] = new PowerFunction(baseEx, pow);
                    }
                    else
                        _subComps[i] = (subComp as AlgebraTerm).ApplyOrderOfOperations();
                }
            }

            if (GroupCount == 1)
                return this;

            List<ExComp[]> groups = PopGroups();

            List<AlgebraTerm> groupTerms = (from gp in groups
                                            select gp.ToAlgTerm()).ToList();

            groups.Clear();
            foreach (AlgebraTerm groupTerm in groupTerms)
            {
                ExComp[] groupToAdd = { groupTerm };
                groups.Add(groupToAdd);
            }

            return PushGroups(groups);
        }

        public virtual void AssignTo(AlgebraTerm algebraTerm)
        {
            _subComps = algebraTerm._subComps;
        }

        public bool CallFunctions(ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraTerm)
                {
                    if (!(_subComps[i] as AlgebraTerm).CallFunctions(ref pEvalData))
                        return false;
                }
                if (_subComps[i] is FunctionDefinition)
                {
                    _subComps[i] = (_subComps[i] as FunctionDefinition).CallFunc(ref pEvalData);
                    if (_subComps[i] == null)
                        return false;
                }
            }

            return true;
        }

        public override ExComp Clone()
        {
            List<ExComp> clonedSubComps = new List<ExComp>();
            foreach (ExComp subComp in _subComps)
            {
                clonedSubComps.Add(subComp.Clone());
            }

            return new AlgebraTerm(clonedSubComps.ToArray());
        }

        public void CombineLikeTerms()
        {
            // Combine like terms.
            var groups = PopGroups();

            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];

                for (int j = 0; j < groups.Count; ++j)
                {
                    if (i == j)
                        continue;

                    var compareGroup = groups[j];

                    if (GroupsCombinable(group, compareGroup))
                    {
                        if (j > i)
                        {
                            groups.RemoveAt(j);
                            groups.RemoveAt(i);
                        }
                        else
                        {
                            groups.RemoveAt(i);
                            groups.RemoveAt(j);
                        }

                        Number groupCoeff = GetCoeffTerm(group);
                        Number compareGroupCoeff = GetCoeffTerm(compareGroup);

                        if (groupCoeff == null)
                            groupCoeff = new Number(1.0);
                        if (compareGroupCoeff == null)
                            compareGroupCoeff = new Number(1.0);

                        List<ExComp> tmpList = group.ToList();
                        var groupCopy = tmpList.ToArray();

                        tmpList = RemoveCoeffs(groupCopy).ToList();
                        RemoveExtraOperators(ref tmpList);
                        groupCopy = tmpList.ToArray();

                        Number combinedNumbers = groupCoeff + compareGroupCoeff;
                        AddTermToGroup(ref groupCopy, combinedNumbers);

                        groups.Insert(i, groupCopy);
                    }
                }
            }

            AlgebraTerm term = PushGroups(groups);
            AssignTo(term);
        }

        public virtual AlgebraTerm CompoundFractions(out bool valid)
        {
            valid = false;

            var groups = GetGroupsNoOps();

            var fracGroups = (from gp in groups
                              where gp.ContainsFrac()
                              select gp).ToList();

            if (fracGroups.Count() == 0)
                return this;

            var nonFracGroups = from gp in groups
                                where !gp.ContainsFrac()
                                select gp;

            if (fracGroups.Count + nonFracGroups.Count() == 1)
                return this;

            PowerFunction denominator = new PowerFunction(Number.One, Number.NegOne);
            foreach (var nonFracGroup in nonFracGroups)
            {
                // Make this a fraction so we can combine it.
                var fracGroup = new List<ExComp>();
                fracGroup.AddRange(nonFracGroup);
                fracGroup.Add(denominator);

                fracGroups.Add(fracGroup.ToArray());
            }

            var denFracGroups = (from fracGp in fracGroups
                                 select fracGp.GetDenominator()).ToList();

            var numFracGroups = (from fracGp in fracGroups
                                 select fracGp.GetNumerator()).ToList();

            var divOp = new Operators.DivOp();

            var lcfDen = GroupHelper.LCF(denFracGroups);
            AlgebraTerm lcfTerm = lcfDen.ToAlgTerm();

            List<ExComp> numMulTerms = new List<ExComp>();

            for (int i = 0; i < denFracGroups.Count; ++i)
            {
                ExComp[] denGroup = denFracGroups[i];
                AlgebraTerm denGroupTerm = denGroup.ToAlgTerm();

                ExComp mulTerm = divOp.Combine(lcfTerm.Clone(), denGroupTerm);
                numMulTerms.Add(mulTerm);
            }

            List<ExComp> modifiedNumTerms = new List<ExComp>();

            for (int i = 0; i < numFracGroups.Count; ++i)
            {
                ExComp mulTerm = numMulTerms[i];
                ExComp[] numTerm = numFracGroups[i];

                AlgebraTerm term = numTerm.ToAlgTerm();

                if (!(mulTerm is Number && (mulTerm as Number) == 1.0))
                {
                    var mulOp = new Operators.MulOp();
                    ExComp combined = mulOp.Combine(term, mulTerm);
                    modifiedNumTerms.Add(combined);
                }
                else
                    modifiedNumTerms.Add(term);
            }

            AlgebraTerm finalNumTerm = new AlgebraTerm();
            foreach (ExComp modifiedNumTerm in modifiedNumTerms)
            {
                ExComp[] modifiedGroup = { modifiedNumTerm };
                finalNumTerm.AddGroup(modifiedGroup);
            }

            finalNumTerm = finalNumTerm.ApplyOrderOfOperations();
            ExComp finalNum = finalNumTerm.MakeWorkable();
            ExComp finalFrac = divOp.Combine(finalNum, lcfTerm);

            AlgebraTerm finalTerm = new AlgebraTerm(finalFrac);

            ExComp finalComp = finalTerm.RemoveRedundancies();

            valid = true;

            if (finalComp is AlgebraTerm)
                return (finalComp as AlgebraTerm);

            finalTerm = new AlgebraTerm(finalComp);
            return finalTerm;
        }

        public virtual AlgebraTerm CompoundFractions()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraTerm)
                {
                    _subComps[i] = (_subComps[i] as AlgebraTerm).CompoundFractions();
                }
            }

            var groups = GetGroupsNoOps();

            var fracGroups = (from gp in groups
                              where gp.ContainsFrac()
                              select gp).ToList();

            if (fracGroups.Count() == 0)
                return this;

            var nonFracGroups = from gp in groups
                                where !gp.ContainsFrac()
                                select gp;

            if (fracGroups.Count + nonFracGroups.Count() == 1)
                return this;

            PowerFunction denominator = new PowerFunction(Number.One, Number.NegOne);
            foreach (var nonFracGroup in nonFracGroups)
            {
                // Make this a fraction so we can combine it.
                var fracGroup = new List<ExComp>();
                fracGroup.AddRange(nonFracGroup);
                fracGroup.Add(denominator);

                fracGroups.Add(fracGroup.ToArray());
            }

            var denFracGroups = (from fracGp in fracGroups
                                 select fracGp.GetDenominator()).ToList();

            var numFracGroups = (from fracGp in fracGroups
                                 select fracGp.GetNumerator()).ToList();

            var divOp = new Operators.DivOp();

            var lcfDen = GroupHelper.LCF(denFracGroups);
            AlgebraTerm lcfTerm = lcfDen.ToAlgTerm();

            List<ExComp> numMulTerms = new List<ExComp>();

            for (int i = 0; i < denFracGroups.Count; ++i)
            {
                ExComp[] denGroup = denFracGroups[i];
                AlgebraTerm denGroupTerm = denGroup.ToAlgTerm();

                ExComp mulTerm = divOp.Combine(lcfTerm.Clone(), denGroupTerm);
                numMulTerms.Add(mulTerm);
            }

            List<ExComp> modifiedNumTerms = new List<ExComp>();

            for (int i = 0; i < numFracGroups.Count; ++i)
            {
                ExComp mulTerm = numMulTerms[i];
                ExComp[] numTerm = numFracGroups[i];

                AlgebraTerm term = numTerm.ToAlgTerm();

                if (!(mulTerm is Number && (mulTerm as Number) == 1.0))
                {
                    var mulOp = new Operators.MulOp();
                    ExComp combined = mulOp.Combine(term, mulTerm);
                    modifiedNumTerms.Add(combined);
                }
                else
                    modifiedNumTerms.Add(term);
            }

            AlgebraTerm finalNumTerm = new AlgebraTerm();
            foreach (ExComp modifiedNumTerm in modifiedNumTerms)
            {
                ExComp[] modifiedGroup = { modifiedNumTerm };
                finalNumTerm.AddGroup(modifiedGroup);
            }

            finalNumTerm = finalNumTerm.ApplyOrderOfOperations();
            ExComp finalNum = finalNumTerm.MakeWorkable();
            // This might end in a stack overflow exception.
            if (finalNum is AlgebraTerm)
                finalNum = (finalNum as AlgebraTerm).CompoundFractions();

            ExComp finalFrac = divOp.Combine(finalNum, lcfTerm);

            AlgebraTerm finalTerm = new AlgebraTerm(finalFrac);

            ExComp finalComp = finalTerm.RemoveRedundancies();

            if (finalComp is AlgebraTerm)
                return (finalComp as AlgebraTerm);

            finalTerm = new AlgebraTerm(finalComp);
            return finalTerm;
        }

        public virtual AlgebraTerm ConvertImaginaryToVar()
        {
            var groups = GetGroups();

            for (int i = 0; i < groups.Count; ++i)
            {
                for (int j = 0; j < groups[i].Length; ++j)
                {
                    if (groups[i][j] is Number)
                    {
                        Number num = (groups[i][j] as Number);
                        if (num.HasImaginaryComp())
                        {
                            double imag = num.ImagComp;

                            ExComp varTerm = MulOp.StaticCombine(num.Imag, new AlgebraComp("i"));

                            groups[i][j] = AddOp.StaticCombine(num.Real, varTerm);
                        }
                    }
                }
            }

            return new AlgebraTerm(groups.ToArray());
        }

        public void ConvertPowFracsToDecimal()
        {
            for (int i = 0; i < TermCount; ++i)
            {
                if (_subComps[i] is PowerFunction)
                {
                    PowerFunction powFunc = _subComps[i] as PowerFunction;
                    AlgebraTerm pow = powFunc.Power.ToAlgTerm();
                    AlgebraTerm[] numDen = pow.GetNumDenFrac();
                    if (numDen != null)
                    {
                        ExComp num = numDen[0].RemoveRedundancies();
                        ExComp den = numDen[1].RemoveRedundancies();

                        if (num is Number && den is Number)
                        {
                            ExComp result = (num as Number) / (den as Number);
                            (_subComps[i] as PowerFunction).Power = result;
                        }
                    }
                }
                else if (_subComps[i] is AlgebraTerm)
                    (_subComps[i] as AlgebraTerm).ConvertPowFracsToDecimal();
            }
        }

        public void EvaluateFunctions(bool harshEval, ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp subComp = _subComps[i];
                if (subComp is AlgebraTerm)
                {
                    (_subComps[i] as AlgebraTerm).EvaluateFunctions(harshEval, ref pEvalData);
                }
                if (subComp is AlgebraFunction)
                {
                    AlgebraFunction func = subComp as AlgebraFunction;
                    _subComps.RemoveAt(i);
                    ExComp evaluated = func.Evaluate(harshEval, ref pEvalData);
                    _subComps.Insert(i, evaluated);
                }
            }
        }

        public void EvaluateFunctions(FunctionType funcType, bool harshEval, ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp subComp = _subComps[i];
                if (subComp is AppliedFunction)
                {
                    AppliedFunction func = subComp as AppliedFunction;
                    if (func.FunctionType == funcType)
                    {
                        _subComps.RemoveAt(i);
                        ExComp evaluated = func.Evaluate(harshEval, ref pEvalData);
                        _subComps.Insert(i, evaluated);
                    }
                }
                else if (subComp is AlgebraTerm)
                {
                    (_subComps[i] as AlgebraTerm).EvaluateFunctions(funcType, harshEval, ref pEvalData);
                }
            }
        }

        public virtual string FinalDispKeepFormatting()
        {
            if (USE_TEX)
                return FinalToTexKeepFormatting();
            return FinalToAsciiKeepFormatting();
        }

        public virtual string FinalToAsciiKeepFormatting()
        {
            if (TermCount == 0)
                return "0";

            var groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();
                    string numTexStr = num.ToMathAsciiString();
                    string denTexStr = den.ToMathAsciiString();

                    numTexStr = numTexStr.RemoveSurroundingParas();
                    denTexStr = denTexStr.RemoveSurroundingParas();

                    finalStr += @"\frac{" + numTexStr + "}{" + denTexStr + "}";
                }
                else
                    finalStr += group.ToMathAsciiString();
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            return finalStr;
        }

        public virtual string FinalToAsciiString()
        {
            if (TermCount == 0)
                return "0";

            var groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();
                    if (den.Length != 0)
                    {
                        string numTexStr = num.FinalToMathAsciiString();
                        string denTexStr = den.FinalToMathAsciiString();

                        finalStr += "(" + numTexStr + ")/(" + denTexStr + ")";
                    }
                    else
                        finalStr += group.ToMathAsciiString();
                }
                else
                    finalStr += group.ToMathAsciiString();
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            return finalStr;
        }

        public virtual string FinalToDispStr()
        {
            if (USE_TEX)
                return FinalToTexString();
            return FinalToAsciiString();
        }

        public virtual string FinalToTexKeepFormatting()
        {
            if (TermCount == 0)
                return "0";

            var groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();
                    string numTexStr = num.ToTexString();
                    string denTexStr = den.ToTexString();

                    numTexStr = numTexStr.RemoveSurroundingParas();
                    denTexStr = denTexStr.RemoveSurroundingParas();

                    finalStr += @"\frac{" + numTexStr + "}{" + denTexStr + "}";
                }
                else
                    finalStr += group.ToTexString();
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            return finalStr;
        }

        public virtual string FinalToTexString()
        {
            if (TermCount == 0)
                return "0";

            var groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();
                    string numTexStr = num.ToTexString();
                    string denTexStr = den.ToTexString();

                    numTexStr = numTexStr.RemoveSurroundingParas();
                    denTexStr = denTexStr.RemoveSurroundingParas();

                    finalStr += @"\frac{" + numTexStr + "}{" + denTexStr + "}";
                }
                else
                    finalStr += group.ToTexString();
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            return finalStr;
        }

        /// <summary>
        /// Force the exponents to multiply together if they are being raised to powers.
        /// Is used for denominator situations where there is the -1 exponent or in evaluating
        /// weak raises.
        /// </summary>
        /// <returns></returns>
        public virtual AlgebraTerm ForceCombineExponents()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraTerm)
                {
                    _subComps[i] = (_subComps[i] as AlgebraTerm).ForceCombineExponents();
                }
            }

            return this;
        }

        public void FuncDefsToAlgVars()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraTerm)
                    (_subComps[i] as AlgebraTerm).FuncDefsToAlgVars();
                if (_subComps[i] is FunctionDefinition)
                {
                    FunctionDefinition funcDef = (_subComps[i] as FunctionDefinition);
                    _subComps[i] = new AlgebraComp(funcDef.ToString());
                }
            }
        }

        /// <summary>
        /// Makes the messy numbers where there previously was the reduced term.
        /// Like (1/4) will turn into 0.25.
        /// </summary>
        /// <returns></returns>
        public virtual AlgebraTerm HarshEvaluation()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp subComp = _subComps[i];
                if (subComp is Constant)
                {
                    Constant constant = subComp as Constant;
                    _subComps.RemoveAt(i);
                    _subComps.Insert(i, constant.Value);
                }
                else if (subComp is AlgebraTerm)
                {
                    _subComps[i] = (subComp as AlgebraTerm).HarshEvaluation();
                }
            }

            var groups = PopGroupsNoOps();

            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();

                    if (num.Length == 1 && num[0] is Number && den.Length == 1 && den[0] is Number)
                    {
                        Number numNumber = num[0] as Number;
                        Number denNumber = den[0] as Number;

                        ExComp resultant = numNumber / denNumber;
                        ExComp[] revisedGroup = { resultant };
                        groups[i] = revisedGroup;
                    }
                }
            }

            PushGroups(groups);

            return this;
        }

        public AlgebraTerm MakeFormattingCorrect(ref TermType.EvalData pEvalData)
        {
            var groups = GetGroupsNoOps();

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "Remove all radicals from the denominator by multiplying the numerator and denominator by the radical over itself.");

            bool found = false;

            AlgebraTerm overallTerm = new AlgebraTerm();

            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];

                if (group.ContainsFrac())
                {
                    var den = group.GetDenominator();

                    AlgebraTerm denTerm = den.ToAlgTerm();

                    List<PowerFunction> radicals = denTerm.GetRadicals();

                    AlgebraTerm numTerm = group.GetNumerator().ToAlgTerm();

                    if (radicals.Count != 0)
                        found = true;

                    foreach (PowerFunction radical in radicals)
                    {
                        numTerm = Operators.MulOp.StaticCombine(numTerm, radical.Clone()).ToAlgTerm();
                        denTerm = Operators.MulOp.StaticCombine(denTerm, radical.Clone()).ToAlgTerm();
                    }

                    AlgebraTerm groupFrac = AlgebraTerm.FromFraction(numTerm, denTerm);
                    overallTerm = overallTerm + groupFrac;
                }
                else
                    overallTerm = overallTerm + group.ToAlgTerm();
            }

            var overallGroups = overallTerm.GetGroups();

            for (int i = 0; i < overallGroups.Count; ++i)
            {
                overallGroups[i] = overallGroups[i].RemoveOneCoeffs();
            }

            overallTerm = overallTerm.Order();
            if (overallTerm.TermCount == 0)
                overallTerm = Number.Zero.ToAlgTerm();

            if (found)
                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + overallTerm.FinalToDispStr() + WorkMgr.EDM, "The result of removing all radicals from the denominator.");
            else
                pEvalData.WorkMgr.PopStep();

            return overallTerm;
        }

        public AlgebraTerm MakeFormattingCorrect()
        {
            var groups = GetGroupsNoOps();

            AlgebraTerm overallTerm = new AlgebraTerm();

            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];

                if (group.ContainsFrac())
                {
                    var den = group.GetDenominator();

                    AlgebraTerm denTerm = den.ToAlgTerm();

                    List<PowerFunction> radicals = denTerm.GetRadicals();

                    AlgebraTerm numTerm = group.GetNumerator().ToAlgTerm();

                    foreach (PowerFunction radical in radicals)
                    {
                        numTerm = Operators.MulOp.StaticCombine(numTerm, radical.Clone()).ToAlgTerm();
                        denTerm = Operators.MulOp.StaticCombine(denTerm, radical.Clone()).ToAlgTerm();
                    }

                    AlgebraTerm groupFrac = AlgebraTerm.FromFraction(numTerm, denTerm);
                    overallTerm = overallTerm + groupFrac;
                }
                else
                    overallTerm = overallTerm + group.ToAlgTerm();
            }

            var overallGroups = overallTerm.GetGroups();

            for (int i = 0; i < overallGroups.Count; ++i)
            {
                overallGroups[i] = overallGroups[i].RemoveOneCoeffs();
            }

            overallTerm = overallTerm.Order();
            if (overallTerm.TermCount == 0)
                overallTerm = Number.Zero.ToAlgTerm();

            return overallTerm;
        }

        public virtual ExComp MakeWorkable()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                if (comp is AlgebraTerm)
                {
                    _subComps[i] = (comp as AlgebraTerm).MakeWorkable();
                }
            }

            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                if (comp is AgOp)
                {
                    ExComp beforeComp = _subComps[i - 1];
                    ExComp afterComp = _subComps[i + 1];

                    //if (comp is Operators.PowerOperator)
                    //{
                    //    _subComps.RemoveRange(i, 3);
                    //    Functions.PowerFunction powerFunc = new Functions.PowerFunction(beforeComp, afterComp);
                    //    _subComps.Insert(i, powerFunc);
                    //    i -= 2;
                    //    continue;
                    //}

                    AgOp algebraOp = comp as AgOp;

                    ExComp resultant = algebraOp.Combine(beforeComp, afterComp);
                    int startIndex = i - 1;
                    _subComps.RemoveRange(startIndex, 3);
                    _subComps.Insert(startIndex, resultant);
                    i--;
                }
            }

            ExComp finalEx = RemoveRedundancies();
            return finalEx;
        }

        public virtual AlgebraTerm Order()
        {
            var groups = PopGroupsNoOps();
            var orderedGroups = groups.OrderBy(g => g.GetHighestPower()).Reverse().ToList();

            for (int i = 0; i < orderedGroups.Count; ++i)
            {
                orderedGroups[i] = orderedGroups[i].OrderGroup();
            }

            AlgebraTerm term = PushGroups(orderedGroups);
            return term;
        }

        public void Pop()
        {
            _subComps.RemoveAt(_subComps.Count - 1);
        }

        public virtual List<ExComp[]> PopGroups()
        {
            List<ExComp[]> groups = GetGroups();

            _subComps.Clear();

            return groups;
        }

        public List<ExComp[]> PopGroupsNoOps()
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            _subComps.Clear();

            return groups;
        }

        public void Push(params ExComp[] exComps)
        {
            _subComps.InsertRange(0, exComps);
        }

        public virtual AlgebraTerm PushGroups(List<ExComp[]> groups)
        {
            foreach (ExComp[] group in groups)
                AddGroup(group);
            return this;
        }

        public ExComp ReduceFracs()
        {
            if (ContainsOnlyFractions())
            {
                var numDen = GetNumDenFrac();
                if (numDen != null)
                {
                    return Operators.DivOp.StaticCombine(numDen[0], numDen[1]);
                }
            }

            return this;
        }

        public void RemoveGroup(ExComp[] group)
        {
            //int groupCount = group.Count();
            //int to = _subComps.Count - groupCount;
            //for (int i = 0; i <= to; ++i)
            //{
            //    bool isGroup = true;
            //    int j;
            //    for (j = i; j < (groupCount + i); ++j)
            //    {
            //        ExComp ex1 = _subComps[j];
            //        ExComp ex2 = group[j - i];

            //        if (!ex1.IsEqualTo(ex2))
            //        {
            //            isGroup = false;
            //            break;
            //        }
            //    }

            //    if (isGroup)
            //    {
            //        _subComps.RemoveRange(i, j - i);
            //        if (_subComps.Count > 0 && _subComps[0] is AgOp)
            //            _subComps.RemoveAt(0);
            //        if (_subComps.Count > 0 && _subComps[_subComps.Count - 1] is AgOp)
            //            _subComps.RemoveAt(_subComps.Count - 1);

            //        bool operatorPreceeding = false;
            //        for (int k = 0; k < _subComps.Count; ++k)
            //        {
            //            ExComp tmpEx = _subComps[k];
            //            if (tmpEx is AgOp && operatorPreceeding)
            //            {
            //                _subComps.RemoveAt(k--);
            //            }
            //            if (tmpEx is AgOp)
            //                operatorPreceeding = true;
            //            else
            //                operatorPreceeding = false;
            //        }

            //        break;
            //    }
            //}

            bool hasOps = false;
            foreach (ExComp comp in group)
            {
                if (comp is AgOp)
                {
                    hasOps = true;
                    break;
                }
            }

            List<ExComp[]> groups;
            if (hasOps)
                groups = PopGroups();
            else
                groups = PopGroupsNoOps();

            AlgebraTerm term = group.ToAlgTerm();
            for (int i = 0; i < groups.Count; ++i)
            {
                var compareGroup = groups[i];

                AlgebraTerm compareTerm = compareGroup.ToAlgTerm();

                if (term.IsEqualTo(compareTerm))
                {
                    groups.RemoveAt(i);
                    break;
                }
            }

            foreach (var addGroup in groups)
                AddGroup(addGroup);
        }

        public virtual AlgebraTerm RemoveOneCoeffs()
        {
            var groups = GetGroupsNoOps();
            for (int i = 0; i < groups.Count; ++i)
            {
                groups[i] = groups[i].RemoveOneCoeffs();
            }

            return PushGroups(groups);
        }

        public virtual ExComp RemoveRedundancies(bool postWorkable = false)
        {
            if (_subComps.Count == 1)
            {
                if (_subComps[0] is AlgebraTerm)
                    return (_subComps[0] as AlgebraTerm).RemoveRedundancies(postWorkable);
                return _subComps[0];
            }

            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp exComp = _subComps[i];
                if (exComp is AlgebraTerm)
                {
                    AlgebraTerm algebraTerm = exComp as AlgebraTerm;
                    _subComps[i] = algebraTerm.RemoveRedundancies(postWorkable);
                }
            }

            if (postWorkable)
                return this;

            // The purpose of this is to remove the one coefficients.
            // This will mess up any division operators.
            var groups = PopGroupsNoOps();
            for (int i = 0; i < groups.Count; ++i)
            {
                groups[i] = groups[i].RemoveOneCoeffs();

                if (groups[i].Length == 1 && groups[i][0] is AlgebraTerm && (groups[i][0] as AlgebraTerm).GroupCount > 1)
                {
                    AlgebraTerm popTerm = groups[i][0] as AlgebraTerm;
                    groups.RemoveAt(i);
                    groups.InsertRange(i, popTerm.GetGroupsNoOps());
                }
            }

            AlgebraTerm term = PushGroups(groups);

            if (term.TermCount == 1)
            {
                return term[0];
            }

            return term;
        }

        public virtual AlgebraTerm RemoveZeros()
        {
            if (_subComps.Count == 1)
                return this;

            var groups = GetGroups();
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];

                Number coeff = GetCoeffTerm(group);
                if (coeff != null && coeff == 0.0)
                {
                    this.RemoveGroup(group);
                }
            }

            return this;
        }

        /// <summary>
        /// Factors the group GCF out of the equation.
        /// </summary>
        /// <returns></returns>
        public AlgebraTerm SimpleFactor()
        {
            ExComp[] groupGcf = this.GetGroupGCF();
            if (groupGcf == null || groupGcf.Length == 0)
                return this;

            ExComp gcfTerm = groupGcf.ToAlgTerm().RemoveRedundancies();

            if (Number.One.IsEqualTo(gcfTerm))
                return this;

            ExComp thisCompare = this.RemoveRedundancies();

            if (thisCompare.IsEqualTo(gcfTerm))
                return this;

            ExComp factoredOut = DivOp.StaticCombine(this.Clone(), gcfTerm);

            return new AlgebraTerm(gcfTerm, new MulOp(), factoredOut);
        }

        public virtual AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            List<ExComp> finalSubComps = new List<ExComp>();

            foreach (ExComp subComp in _subComps)
            {
                if (subComp.IsEqualTo(subOut))
                {
                    finalSubComps.Add(subIn);
                }
                else if (subComp is AlgebraTerm)
                {
                    AlgebraTerm subCompTerm = subComp as AlgebraTerm;
                    AlgebraTerm substituted = subCompTerm.Substitute(subOut, subIn);
                    finalSubComps.Add(substituted);
                }
                else
                    finalSubComps.Add(subComp);
            }

            return new AlgebraTerm(finalSubComps.ToArray());
        }

        public virtual AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            List<ExComp> finalSubComps = new List<ExComp>();

            foreach (ExComp subComp in _subComps)
            {
                if (subComp.IsEqualTo(subOut))
                {
                    finalSubComps.Add(subIn);
                    success = true;
                }
                else if (subComp is AlgebraTerm)
                {
                    AlgebraTerm subCompTerm = subComp as AlgebraTerm;
                    AlgebraTerm substituted = subCompTerm.Substitute(subOut, subIn, ref success);
                    finalSubComps.Add(substituted);
                }
                else
                    finalSubComps.Add(subComp);
            }

            return new AlgebraTerm(finalSubComps.ToArray());
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return this;
        }

        public override string ToMathAsciiString()
        {
            if (TermCount == 0)
                return "0";
            var groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();
                    string numTexStr = num.ToMathAsciiString();
                    string denTexStr = den.ToMathAsciiString();

                    finalStr += "(" + numTexStr + ")/(" + denTexStr + ")";
                }
                else
                    finalStr += group.ToMathAsciiString();
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            if (GroupCount > 1)
                finalStr = finalStr.SurroundWithParas();

            return finalStr;
        }

        public override string ToSearchString()
        {
            string finalStr = "";
            foreach (ExComp comp in _subComps)
            {
                finalStr += comp.ToSearchString();
            }
            return finalStr;
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            string finalStr = "";
            foreach (ExComp exComp in _subComps)
            {
                finalStr += exComp.ToString();
            }

            if (finalStr == "")
                finalStr = "0";

            return "AT(" + finalStr + ")";
        }

        public override string ToTexString()
        {
            var groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                var group = groups[i];
                if (group.ContainsFrac())
                {
                    ExComp[] num = group.GetNumerator();
                    ExComp[] den = group.GetDenominator();
                    string numTexStr = num.ToTexString();
                    string denTexStr = den.ToTexString();

                    finalStr += @"\frac{" + numTexStr + "}{" + denTexStr + "}";
                }
                else
                    finalStr += group.ToTexString();
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            if (GroupCount > 1)
                finalStr = finalStr.SurroundWithParas();

            return finalStr;
        }

        public ExComp WeakMakeWorkable(ref List<string> pParseErrors)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                if (comp is AlgebraTerm)
                {
                    ExComp weakWorkable = (comp as AlgebraTerm).WeakMakeWorkable();
                    if (weakWorkable == null)
                        return null;
                    if (Number.IsUndef(weakWorkable))
                        return Number.Undefined;
                    _subComps[i] = weakWorkable;
                }
            }

            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                // Pretty much a dirty shortcut.
                if (i != _subComps.Count - 1 && _subComps[i + 1] is AbsValFunction && !(comp is AgOp))
                {
                    _subComps.Insert(i + 1, new MulOp());
                    comp = _subComps[++i];
                }
                if (comp is AgOp)
                {
                    if (i == 0)
                        return null;
                    ExComp beforeComp = _subComps[i - 1];
                    if (beforeComp is AgOp)
                    {
                        pParseErrors.Add("Mismatched operators.");
                        return null;
                    }
                    if (i == _subComps.Count - 1)
                    {
                        pParseErrors.Add("Mismatched operators.");
                        return null;
                    }
                    ExComp afterComp = _subComps[i + 1];
                    if (afterComp is AgOp)
                    {
                        pParseErrors.Add("Mismatched operators.");
                        return null;
                    }
                    ExComp resultant;

                    bool isMultiGroup = false;
                    if (beforeComp is AlgebraTerm)
                        isMultiGroup = (beforeComp as AlgebraTerm).GroupCount > 1;
                    if (!isMultiGroup && afterComp is AlgebraTerm)
                        isMultiGroup = (afterComp as AlgebraTerm).GroupCount > 1;

                    AgOp algebraOp = comp as AgOp;

                    if (comp is Operators.PowOp)
                    {
                        resultant = algebraOp.WeakCombine(beforeComp, afterComp);
                    }
                    if (beforeComp is FunctionDefinition || afterComp is FunctionDefinition)
                    {
                        resultant = algebraOp.WeakCombine(beforeComp, afterComp);
                    }
                    else if ((comp is Operators.DivOp || comp is Operators.MulOp) && isMultiGroup)
                    {
                        resultant = algebraOp.WeakCombine(beforeComp, afterComp);
                    }
                    else
                    {
                        resultant = algebraOp.Combine(beforeComp, afterComp);
                    }

                    if (resultant == null)
                        return null;

                    if (Number.IsUndef(resultant))
                        return Number.Undefined;

                    int startIndex = i - 1;
                    _subComps.RemoveRange(startIndex, 3);
                    _subComps.Insert(startIndex, resultant);
                    i--;
                }
            }

            ExComp finalEx = RemoveRedundancies();
            return finalEx;
        }

        public ExComp WeakMakeWorkable()
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                if (comp is AlgebraTerm)
                {
                    ExComp weakWorkable = (comp as AlgebraTerm).WeakMakeWorkable();
                    if (weakWorkable == null)
                        return null;
                    if (Number.IsUndef(weakWorkable))
                        return Number.Undefined;
                    _subComps[i] = weakWorkable;
                }
            }

            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                // Pretty much a dirty shortcut.
                if (i != _subComps.Count - 1 && _subComps[i + 1] is AbsValFunction && !(comp is AgOp))
                {
                    _subComps.Insert(i + 1, new MulOp());
                    comp = _subComps[++i];
                }
                if (comp is AgOp)
                {
                    if (i == 0)
                        return null;
                    ExComp beforeComp = _subComps[i - 1];
                    if (beforeComp is AgOp)
                    {
                        return null;
                    }
                    if (i == _subComps.Count - 1)
                    {
                        return null;
                    }
                    ExComp afterComp = _subComps[i + 1];
                    if (afterComp is AgOp)
                    {
                        return null;
                    }
                    ExComp resultant;

                    bool isMultiGroup = false;
                    if (beforeComp is AlgebraTerm)
                        isMultiGroup = (beforeComp as AlgebraTerm).GroupCount > 1;
                    if (!isMultiGroup && afterComp is AlgebraTerm)
                        isMultiGroup = (afterComp as AlgebraTerm).GroupCount > 1;

                    AgOp algebraOp = comp as AgOp;

                    if (comp is Operators.PowOp)
                    {
                        resultant = algebraOp.WeakCombine(beforeComp, afterComp);
                    }
                    if (beforeComp is FunctionDefinition || afterComp is FunctionDefinition)
                    {
                        resultant = algebraOp.WeakCombine(beforeComp, afterComp);
                    }
                    else if ((comp is Operators.DivOp || comp is Operators.MulOp) && isMultiGroup)
                    {
                        resultant = algebraOp.WeakCombine(beforeComp, afterComp);
                    }
                    else
                    {
                        resultant = algebraOp.Combine(beforeComp, afterComp);
                    }

                    if (resultant == null)
                        return null;

                    if (Number.IsUndef(resultant))
                        return Number.Undefined;

                    int startIndex = i - 1;
                    _subComps.RemoveRange(startIndex, 3);
                    _subComps.Insert(startIndex, resultant);
                    i--;
                }
            }

            ExComp finalEx = RemoveRedundancies();
            return finalEx;
        }

        private static void RemoveExtraOperators(ref List<ExComp> comps)
        {
            if (comps.Count == 0)
                return;
            bool operatorPreceeding = false;
            for (int i = 0; i < comps.Count; ++i)
            {
                if (comps[i] is AgOp && operatorPreceeding)
                {
                    comps.RemoveAt(i--);
                    continue;
                }

                if (comps[i] is AgOp)
                    operatorPreceeding = true;
                else
                    operatorPreceeding = false;
            }

            if (comps.Count != 0 && comps.First() is AgOp)
                comps.RemoveAt(0);
            if (comps.Count != 0 && comps.Last() is AgOp)
                comps.RemoveAt(comps.Count - 1);
        }

        private List<ExComp> RecursiveGroupGCFSolve(List<ExComp[]> groups)
        {
            List<ExComp[]> narrowedList = new List<ExComp[]>();

            if (groups.Count == 2)
            {
                return GroupHelper.GCF(groups[0], groups[1]).ToList();
            }

            for (int i = 1; i < groups.Count; ++i)
            {
                ExComp[] g1 = groups[i];
                ExComp[] g2 = groups[i - 1];
                ExComp[] gcf = GroupHelper.GCF(g1, g2);
                narrowedList.Add(gcf);
            }

            return RecursiveGroupGCFSolve(narrowedList);
        }
    }
}