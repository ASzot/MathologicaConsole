using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal partial class AlgebraTerm : ExComp
    {
        protected List<ExComp> _subComps = new List<ExComp>();

        public int GetGroupCount()
        {
            return GetGroups().Count;
        }

        public List<ExComp> GetSubComps()
        {
            return _subComps;
        }

        public int GetTermCount()
        {
            return _subComps.Count;
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
                AddGroup(group.GetGroup());
            }
        }

        public AlgebraTerm(params ExComp[] addComps)
        {
            _subComps.AddRange(addComps);
        }

        public static void AddTermToGroup(ref ExComp[] group, ExComp comp, bool mulOp)
        {
            int groupCount = group.Length;
            if (groupCount == 0)
            {
                group = new ExComp[1];
                group[0] = comp;
                return;
            }

            int resizeCount = mulOp ? 2 : 1;

            ExComp[] tmpGp = new ExComp[groupCount + resizeCount];
            for (int i = 0; i < group.Length; ++i)
            {
                tmpGp[i] = group[i];
            }

            if (mulOp)
                tmpGp[groupCount] = new Operators.MulOp();
            tmpGp[groupCount + (resizeCount - 1)] = comp;

            group = tmpGp;
        }

        public static AlgebraTerm FromFactor(AlgebraComp varFor, ExComp factor)
        {
            // We have the factor (varFor - factor).
            ExComp bTerm;
            if (factor is AlgebraTerm)
            {
                AlgebraTerm[] numDen = (factor as AlgebraTerm).GetNumDenFrac();
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
            return FromFactors(ArrayFunc.ToList(factors));
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
                num = (num as AlgebraTerm).RemoveRedundancies(false);
            if (den is AlgebraTerm)
                den = (den as AlgebraTerm).RemoveRedundancies(false);

            if (den is ExNumber && ExNumber.OpLT((den as ExNumber), 0.0))
            {
                num = Operators.MulOp.Negate(num);
                den = ExNumber.OpMul(ExNumber.GetNegOne(), (den as ExNumber));
            }

            if (((den is AlgebraTerm) && (den as AlgebraTerm).IsOne()) ||
                (den is ExNumber) && ExNumber.OpEqual((den as ExNumber), 1.0))
                return num.ToAlgTerm();

            AlgebraTerm term = new AlgebraTerm(num, new Operators.MulOp(), new Functions.PowerFunction(den, ExNumber.GetNegOne()));

            return term;
        }

        public static ExComp[] RemoveCoeffs(ExComp[] group)
        {
            List<ExComp> removedGroups = new List<ExComp>();
            foreach (ExComp groupComp in group)
            {
                if (!(groupComp is ExNumber))
                    removedGroups.Add(groupComp);
            }

            return removedGroups.ToArray();
        }

        public static ExNumber ToNumber(AlgebraTerm term)
        {
            ExComp ex = term.RemoveRedundancies(false);
            if (ex is ExNumber)
                return ex as ExNumber;
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
            for (int i = 0; i < group.Length; ++i)
            {
                ExComp comp = group[i];
                ExComp next = (i == group.Length - 1) ? null : group[i + 1];
                finalGroup.Add(comp);
                if (!(comp is AgOp) && next != null && !(next is AgOp))
                    finalGroup.Add(new Operators.MulOp());
            }

            Add(finalGroup.ToArray());
        }

        public void AddGroup(ExComp singularGroupTerm)
        {
            ExComp[] singularGp = new ExComp[] { singularGroupTerm };
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
                        ExComp pow = (subComp as PowerFunction).GetPower();
                        ExComp baseEx = (subComp as PowerFunction).GetBase();

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

            if (GetGroupCount() == 1)
                return this;

            List<ExComp[]> groups = PopGroups();

            List<AlgebraTerm> groupTerms = new List<AlgebraTerm>();
            for (int i = 0; i < groups.Count; ++i)
                groupTerms.Add(GroupHelper.ToAlgTerm(groups[i]));

            groups.Clear();
            foreach (AlgebraTerm groupTerm in groupTerms)
            {
                ExComp[] groupToAdd = new ExComp[] { groupTerm };
                groups.Add(groupToAdd);
            }

            return PushGroups(groups);
        }

        public void SetSubCompsGps(List<ExComp[]> gps)
        {
            _subComps.Clear();
            foreach (ExComp[] gp in gps)
                AddGroup(gp);
        }

        public void SetSubComps(List<ExComp> subComps)
        {
            _subComps = subComps;
        }

        public virtual void AssignTo(AlgebraTerm algebraTerm)
        {
            _subComps = algebraTerm._subComps;
        }

        /// <summary>
        /// Call given function.
        /// </summary>
        /// <param name="funcDef"></param>
        /// <param name="def"></param>
        /// <param name="pEvalData"></param>
        /// <param name="callSubTerms"></param>
        public virtual void CallFunction(FunctionDefinition funcDef, ExComp def, ref EvalData pEvalData, bool callSubTerms)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraComp && (_subComps[i] as AlgebraComp).IsEqualTo(funcDef.GetIden()) && !funcDef.GetHasValidInputArgs())
                {
                    _subComps[i] = def;
                }

                if (_subComps[i] is Derivative)
                {
                    Derivative deriv = _subComps[i] as Derivative;
                    ExComp derivSubbed = deriv.GetDerivOfFunc(funcDef, def);
                    if (derivSubbed != null)
                    {
                        _subComps[i] = derivSubbed;
                    }
                }

                if (_subComps[i] is AlgebraTerm)
                {
                    (_subComps[i] as AlgebraTerm).CallFunction(funcDef, def, ref pEvalData, callSubTerms);
                }

                if (_subComps[i] is FunctionDefinition && (_subComps[i] as FunctionDefinition).IsEqualTo(funcDef))
                {
                    KeyValuePair<FunctionDefinition, ExComp> keyValDef = ArrayFunc.CreateKeyValuePair(funcDef, def);
                    ExComp tmpVal = (_subComps[i] as FunctionDefinition).CallFunc(keyValDef, ref pEvalData, callSubTerms);
                    if (tmpVal != null)
                        _subComps[i] = tmpVal;
                }
            }
        }

        /// <summary>
        /// Call the set of all defined functions.
        /// </summary>
        /// <param name="pEvalData"></param>
        /// <returns></returns>
        public virtual bool CallFunctions(ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraComp)
                {
                    AlgebraComp iden = _subComps[i] as AlgebraComp;
                    ExComp definition = pEvalData.GetFuncDefs().GetSingleVarDefinition(iden);
                    if (definition == null)
                        continue;
                    _subComps[i] = definition;
                }

                if (_subComps[i] is Derivative)
                {
                    Derivative deriv = _subComps[i] as Derivative;
                    if (deriv.GetDerivOf() != null && pEvalData.GetFuncDefs().IsFuncDefined(deriv.GetDerivOf().GetVar().GetVar()))
                    {
                        KeyValuePair<FunctionDefinition, ExComp> def = pEvalData.GetFuncDefs().GetDefinition(deriv.GetDerivOf());
                        ExComp derivSubbed = deriv.GetDerivOfFunc(def.Key, def.Value);
                        if (derivSubbed != null)
                        {
                            _subComps[i] = derivSubbed;
                        }
                    }
                }

                if (_subComps[i] is AlgebraTerm)
                {
                    bool result = (_subComps[i] as AlgebraTerm).CallFunctions(ref pEvalData);
                    if (!result)
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

        public override ExComp CloneEx()
        {
            List<ExComp> clonedSubComps = new List<ExComp>();
            foreach (ExComp subComp in _subComps)
            {
                clonedSubComps.Add(subComp.CloneEx());
            }

            return new AlgebraTerm(clonedSubComps.ToArray());
        }

        public void CombineLikeTerms()
        {
            // Combine like terms.
            List<ExComp[]> groups = PopGroups();

            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];

                for (int j = 0; j < groups.Count; ++j)
                {
                    if (i == j)
                        continue;

                    ExComp[] compareGroup = groups[j];

                    if (GroupsCombinable(group, compareGroup))
                    {
                        if (j > i)
                        {
                            ArrayFunc.RemoveIndex(groups, j);
                            ArrayFunc.RemoveIndex(groups, i);
                        }
                        else
                        {
                            ArrayFunc.RemoveIndex(groups, i);
                            ArrayFunc.RemoveIndex(groups, j);
                        }

                        ExNumber groupCoeff = GetCoeffTerm(group);
                        ExNumber compareGroupCoeff = GetCoeffTerm(compareGroup);

                        if (groupCoeff == null)
                            groupCoeff = new ExNumber(1.0);
                        if (compareGroupCoeff == null)
                            compareGroupCoeff = new ExNumber(1.0);

                        List<ExComp> tmpList = ArrayFunc.ToList(group);
                        ExComp[] groupCopy = tmpList.ToArray();

                        tmpList = ArrayFunc.ToList(RemoveCoeffs(groupCopy));
                        RemoveExtraOperators(ref tmpList);
                        groupCopy = tmpList.ToArray();

                        ExNumber combinedNumbers = ExNumber.OpAdd(groupCoeff, compareGroupCoeff);
                        AddTermToGroup(ref groupCopy, combinedNumbers, true);

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

            for (int i = 0; i < _subComps.Count; ++i)
            {
                if (_subComps[i] is AlgebraTerm)
                {
                    bool tmpValid;
                    _subComps[i] = (_subComps[i] as AlgebraTerm).CompoundFractions(out tmpValid);
                    if (tmpValid)
                        valid = tmpValid;
                }
            }

            List<ExComp[]> groups = GetGroupsNoOps();

            List<ExComp[]> fracGroups = new List<ExComp[]>();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (GroupHelper.ContainsFrac(groups[i]))
                    fracGroups.Add(groups[i]);
            }

            if (fracGroups.Count == 0)
                return this;

            List<ExComp[]> nonFracList = new List<ExComp[]>();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (!GroupHelper.ContainsFrac(groups[i]))
                    nonFracList.Add(groups[i]);
            }

            if (fracGroups.Count + nonFracList.Count == 1)
                return this;

            PowerFunction denominator = new PowerFunction(ExNumber.GetOne(), ExNumber.GetNegOne());
            foreach (ExComp[] nonFracGroup in nonFracList)
            {
                // Make this a fraction so we can combine it.
                List<ExComp> fracGroup = new List<ExComp>();
                fracGroup.AddRange(nonFracGroup);
                fracGroup.Add(denominator);

                fracGroups.Add(fracGroup.ToArray());
            }

            List<ExComp[]> denFracGroups = new List<ExComp[]>();

            for (int i = 0; i < fracGroups.Count; ++i)
                denFracGroups.Add(GroupHelper.GetDenominator(fracGroups[i], false));

            List<ExComp[]> numFracGroups = new List<ExComp[]>();

            for (int i = 0; i < fracGroups.Count; ++i)
                numFracGroups.Add(GroupHelper.GetNumerator(fracGroups[i]));

            for (int i = 0; i < denFracGroups.Count; ++i)
            {
                ExComp[] denFracGroup = denFracGroups[i];
                if (denFracGroup.Length <= 1)
                    continue;
                ExComp[] tmpGp = GroupHelper.AccumulateTerms(denFracGroup);
                if (tmpGp == null)
                    continue;
                denFracGroups[i] = tmpGp;
            }
            ExComp[] lcfDen = GroupHelper.LCF(denFracGroups);
            AlgebraTerm lcfTerm = GroupHelper.ToAlgTerm(lcfDen);

            List<ExComp> numMulTerms = new List<ExComp>();

            for (int i = 0; i < denFracGroups.Count; ++i)
            {
                ExComp[] denGroup = denFracGroups[i];
                AlgebraTerm denGroupTerm = GroupHelper.ToAlgTerm(denGroup);

                ExComp mulTerm = Operators.DivOp.StaticCombine(lcfTerm.CloneEx(), denGroupTerm);
                numMulTerms.Add(mulTerm);
            }

            List<ExComp> modifiedNumTerms = new List<ExComp>();

            for (int i = 0; i < numFracGroups.Count; ++i)
            {
                ExComp mulTerm = numMulTerms[i];
                ExComp[] numTerm = numFracGroups[i];

                AlgebraTerm term = GroupHelper.ToAlgTerm(numTerm);

                if (!(mulTerm is ExNumber && ExNumber.OpEqual((mulTerm as ExNumber), 1.0)))
                {
                    ExComp combined = MulOp.StaticCombine(term, mulTerm);
                    modifiedNumTerms.Add(combined);
                }
                else
                    modifiedNumTerms.Add(term);
            }

            AlgebraTerm finalNumTerm = new AlgebraTerm();
            foreach (ExComp modifiedNumTerm in modifiedNumTerms)
            {
                ExComp[] modifiedGroup = new ExComp[] { modifiedNumTerm };
                finalNumTerm.AddGroup(modifiedGroup);
            }

            finalNumTerm = finalNumTerm.ApplyOrderOfOperations();
            ExComp finalNum = finalNumTerm.MakeWorkable();
            ExComp finalFrac = Operators.DivOp.StaticCombine(finalNum, lcfTerm);

            AlgebraTerm finalTerm = new AlgebraTerm(finalFrac);

            ExComp finalComp = finalTerm.RemoveRedundancies(false);

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

            List<ExComp[]> groups = GetGroupsNoOps();

            List<ExComp[]> fracGroups = new List<ExComp[]>();

            for (int i = 0; i < groups.Count; ++i)
            {
                if (GroupHelper.ContainsFrac(groups[i]))
                    fracGroups.Add(groups[i]);
            }

            if (fracGroups.Count== 0)
                return this;

            List<ExComp[]> nonFracGroupsList = new List<ExComp[]>();
            for (int i = 0; i < groups.Count; ++i)
            {
                if (!GroupHelper.ContainsFrac(groups[i]))
                    nonFracGroupsList.Add(groups[i]);
            }

            if (fracGroups.Count + nonFracGroupsList.Count == 1)
                return this;

            PowerFunction denominator = new PowerFunction(ExNumber.GetOne(), ExNumber.GetNegOne());
            foreach (ExComp[] nonFracGroup in nonFracGroupsList)
            {
                // Make this a fraction so we can combine it.
                List<ExComp> fracGroup = new List<ExComp>();
                fracGroup.AddRange(nonFracGroup);
                fracGroup.Add(denominator);

                fracGroups.Add(fracGroup.ToArray());
            }

            List<ExComp[]> denFracGroups = new List<ExComp[]>();

            for (int i = 0; i < fracGroups.Count; ++i)
                denFracGroups.Add(GroupHelper.GetDenominator(fracGroups[i], false));

            List<ExComp[]> numFracGroups = new List<ExComp[]>();

            for (int i = 0; i < fracGroups.Count; ++i)
                numFracGroups.Add(GroupHelper.GetNumerator(fracGroups[i]));

            ExComp[] lcfDen = GroupHelper.LCF(denFracGroups);
            AlgebraTerm lcfTerm = GroupHelper.ToAlgTerm(lcfDen);
            lcfTerm.ApplyOrderOfOperations();
            lcfTerm = lcfTerm.MakeWorkable().ToAlgTerm();

            List<ExComp> numMulTerms = new List<ExComp>();

            for (int i = 0; i < denFracGroups.Count; ++i)
            {
                ExComp[] denGroup = denFracGroups[i];
                AlgebraTerm denGroupTerm = GroupHelper.ToAlgTerm(denGroup);

                ExComp mulTerm = DivOp.StaticCombine(lcfTerm.CloneEx(), denGroupTerm);
                numMulTerms.Add(mulTerm);
            }

            List<ExComp> modifiedNumTerms = new List<ExComp>();

            for (int i = 0; i < numFracGroups.Count; ++i)
            {
                ExComp mulTerm = numMulTerms[i];
                ExComp[] numTerm = numFracGroups[i];

                AlgebraTerm term = GroupHelper.ToAlgTerm(numTerm);

                if (!(mulTerm is ExNumber && ExNumber.OpEqual((mulTerm as ExNumber), 1.0)))
                {
                    MulOp mulOp = new MulOp();
                    ExComp combined = mulOp.Combine(term, mulTerm);
                    modifiedNumTerms.Add(combined);
                }
                else
                    modifiedNumTerms.Add(term);
            }

            AlgebraTerm finalNumTerm = new AlgebraTerm();
            foreach (ExComp modifiedNumTerm in modifiedNumTerms)
            {
                ExComp[] modifiedGroup = new ExComp[] { modifiedNumTerm };
                finalNumTerm.AddGroup(modifiedGroup);
            }

            finalNumTerm = finalNumTerm.ApplyOrderOfOperations();
            ExComp finalNum = finalNumTerm.MakeWorkable();
            // This might end in a stack overflow exception.
            //if (finalNum is AlgebraTerm)
            //{
            //    //finalNum = (finalNum as AlgebraTerm).CompoundFractions();
            //}

            ExComp finalFrac = DivOp.StaticCombine(finalNum, lcfTerm);

            AlgebraTerm finalTerm = new AlgebraTerm(finalFrac);

            ExComp finalComp = finalTerm.RemoveRedundancies(false);

            if (finalComp is AlgebraTerm)
                return (finalComp as AlgebraTerm);

            finalTerm = new AlgebraTerm(finalComp);
            return finalTerm;
        }

        public virtual AlgebraTerm ConvertImaginaryToVar()
        {
            List<ExComp[]> groups = GetGroups();

            for (int i = 0; i < groups.Count; ++i)
            {
                for (int j = 0; j < groups[i].Length; ++j)
                {
                    if (groups[i][j] is AlgebraTerm)
                    {
                        groups[i][j] = (groups[i][j] as AlgebraTerm).ConvertImaginaryToVar();
                    }
                    if (groups[i][j] is ExNumber)
                    {
                        ExNumber num = (groups[i][j] as ExNumber);
                        if (num.HasImaginaryComp())
                        {
                            double imag = num.GetImagComp();

                            ExComp varTerm = MulOp.StaticCombine(num.GetImag(), new AlgebraComp("i"));

                            groups[i][j] = AddOp.StaticCombine(num.GetReal(), varTerm);
                        }
                    }
                }
            }

            return new AlgebraTerm(groups.ToArray());
        }

        public void ConvertPowFracsToDecimal()
        {
            for (int i = 0; i < GetTermCount(); ++i)
            {
                if (_subComps[i] is PowerFunction)
                {
                    PowerFunction powFunc = _subComps[i] as PowerFunction;
                    AlgebraTerm pow = powFunc.GetPower().ToAlgTerm();
                    AlgebraTerm[] numDen = pow.GetNumDenFrac();
                    if (numDen != null)
                    {
                        ExComp num = numDen[0].RemoveRedundancies(false);
                        ExComp den = numDen[1].RemoveRedundancies(false);

                        if (num is ExNumber && den is ExNumber)
                        {
                            ExComp result = ExNumber.OpDiv((num as ExNumber), (den as ExNumber));
                            (_subComps[i] as PowerFunction).SetPower(result);
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
                if (_subComps[i] is AlgebraFunction)
                {
                    // Before evaluating anything check if a cancellation is possible.
                    ExComp innerEx = (new AlgebraTerm((_subComps[i] as AlgebraFunction)._subComps.ToArray())).RemoveRedundancies(false);
                    ExComp cancelAtmpt = (_subComps[i] as AlgebraFunction).CancelWith(innerEx, ref pEvalData);
                    if (cancelAtmpt != null)
                    {
                        ArrayFunc.RemoveIndex(_subComps, i);
                        _subComps.Insert(i, cancelAtmpt);
                    }
                }

                if (_subComps[i] is AlgebraFunction)
                {
                    AlgebraFunction func = _subComps[i] as AlgebraFunction;
                    ArrayFunc.RemoveIndex(_subComps, i);
                    ExComp evaluated = func.Evaluate(harshEval, ref pEvalData);
                    _subComps.Insert(i, evaluated);
                }
                if (_subComps[i] is AlgebraTerm && !(_subComps[i] is AppliedFunction))       // AppliedFunction is in charge of calling all children functions.
                {
                    (_subComps[i] as AlgebraTerm).EvaluateFunctions(harshEval, ref pEvalData);
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
                    if (func.GetFunctionType() == funcType)
                    {
                        ArrayFunc.RemoveIndex(_subComps, i);
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

        public virtual string FinalToAsciiString()
        {
            if (GetTermCount() == 0)
                return "0";

            List<ExComp[]> groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];
                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] num = GroupHelper.GetNumerator(group);
                    ExComp[] den = GroupHelper.GetDenominator(group, false);
                    if (den.Length != 0)
                    {
                        string numTexStr = GroupHelper.FinalToMathAsciiString(num);
                        string denTexStr = GroupHelper.FinalToMathAsciiString(den);

                        finalStr += "(" + numTexStr + ")/(" + denTexStr + ")";
                    }
                    else
                        finalStr += GroupHelper.ToAsciiString(group);
                }
                else
                    finalStr += GroupHelper.ToAsciiString(group);
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

        public virtual string FinalToTexString()
        {
            if (GetTermCount() == 0)
                return "0";

            List<ExComp[]> groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];
                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] num = GroupHelper.GetNumerator(group);
                    ExComp[] den = GroupHelper.GetDenominator(group, false);
                    string numTexStr = GroupHelper.ToTexString(num);
                    string denTexStr = GroupHelper.ToTexString(den);

                    numTexStr = StringHelper.RemoveSurroundingParas(numTexStr);
                    denTexStr = StringHelper.RemoveSurroundingParas(denTexStr);

                    finalStr += @"\frac{" + numTexStr + "}{" + denTexStr + "}";
                }
                else
                    finalStr += GroupHelper.ToTexString(group);
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
                    if (funcDef.GetCallArgs() == null)
                        _subComps[i] = new AlgebraComp(funcDef.ToString());
                }
            }
        }

        /// <summary>
        /// Makes the messy numbers where there previously was the reduced term.
        /// Like (1/4) will turn into 0.25.
        /// This is not intended for evaluating functions.
        /// For that functionality see Simplifier.HarshSimplify
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
                    ArrayFunc.RemoveIndex(_subComps, i);
                    _subComps.Insert(i, constant.GetValue());
                }
                else if (subComp is AlgebraTerm)
                {
                    _subComps[i] = (subComp as AlgebraTerm).HarshEvaluation();
                }
            }

            List<ExComp[]> groups = PopGroupsNoOps();

            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];
                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] num = GroupHelper.GetNumerator(group);
                    ExComp[] den = GroupHelper.GetDenominator(group, false);

                    if (num.Length == 1 && num[0] is ExNumber && den.Length == 1 && den[0] is ExNumber)
                    {
                        ExNumber numNumber = num[0] as ExNumber;
                        ExNumber denNumber = den[0] as ExNumber;

                        ExComp resultant = ExNumber.OpDiv(numNumber, denNumber);
                        ExComp[] revisedGroup = new ExComp[] { resultant };
                        groups[i] = revisedGroup;
                    }
                }
            }

            PushGroups(groups);

            return this;
        }

        public AlgebraTerm MakeFormattingCorrect(ref TermType.EvalData pEvalData)
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "Remove all radicals from the denominator by multiplying the numerator and denominator by the radical over itself.");

            bool found = false;

            AlgebraTerm overallTerm = new AlgebraTerm();

            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];

                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] den = GroupHelper.GetDenominator(group, false);

                    AlgebraTerm denTerm = GroupHelper.ToAlgTerm(den);

                    List<PowerFunction> radicals = denTerm.GetRadicals();

                    AlgebraTerm numTerm = GroupHelper.ToAlgTerm(GroupHelper.GetNumerator(group));

                    if (radicals.Count != 0)
                        found = true;

                    foreach (PowerFunction radical in radicals)
                    {
                        numTerm = Operators.MulOp.StaticCombine(numTerm, radical.CloneEx()).ToAlgTerm();
                        denTerm = Operators.MulOp.StaticCombine(denTerm, radical.CloneEx()).ToAlgTerm();
                    }

                    AlgebraTerm groupFrac = AlgebraTerm.FromFraction(numTerm, denTerm);
                    overallTerm = AlgebraTerm.OpAdd(overallTerm, groupFrac);
                }
                else
                    overallTerm = AlgebraTerm.OpAdd(overallTerm, GroupHelper.ToAlgTerm(group));
            }

            List<ExComp[]> overallGroups = overallTerm.GetGroups();

            for (int i = 0; i < overallGroups.Count; ++i)
            {
                overallGroups[i] = GroupHelper.RemoveOneCoeffs(overallGroups[i]);
            }

            overallTerm = overallTerm.Order();
            if (overallTerm.GetTermCount() == 0)
                overallTerm = ExNumber.GetZero().ToAlgTerm();

            if (found)
                pEvalData.GetWorkMgr().FromFormatted(WorkMgr.STM + overallTerm.FinalToDispStr() + WorkMgr.EDM, "The result of removing all radicals from the denominator.");
            else
                pEvalData.GetWorkMgr().PopStep();

            return overallTerm;
        }

        public AlgebraTerm MakeFormattingCorrect()
        {
            List<ExComp[]> groups = GetGroupsNoOps();

            AlgebraTerm overallTerm = new AlgebraTerm();

            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];

                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] den = GroupHelper.GetDenominator(group, false);

                    AlgebraTerm denTerm = GroupHelper.ToAlgTerm(den);

                    List<PowerFunction> radicals = denTerm.GetRadicals();

                    AlgebraTerm numTerm = GroupHelper.ToAlgTerm(GroupHelper.GetNumerator(group));

                    foreach (PowerFunction radical in radicals)
                    {
                        numTerm = Operators.MulOp.StaticCombine(numTerm, radical.CloneEx()).ToAlgTerm();
                        denTerm = Operators.MulOp.StaticCombine(denTerm, radical.CloneEx()).ToAlgTerm();
                    }

                    AlgebraTerm groupFrac = AlgebraTerm.FromFraction(numTerm, denTerm);
                    overallTerm = AlgebraTerm.OpAdd(overallTerm, groupFrac);
                }
                else
                    overallTerm = AlgebraTerm.OpAdd(overallTerm, GroupHelper.ToAlgTerm(group));
            }

            List<ExComp[]> overallGroups = overallTerm.GetGroups();

            for (int i = 0; i < overallGroups.Count; ++i)
            {
                overallGroups[i] = GroupHelper.RemoveOneCoeffs(overallGroups[i]);
            }

            overallTerm = overallTerm.Order();
            if (overallTerm.GetTermCount() == 0)
                overallTerm = ExNumber.GetZero().ToAlgTerm();

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

            ExComp finalEx = RemoveRedundancies(false);
            return finalEx;
        }

        public virtual AlgebraTerm Order()
        {
            List<ExComp[]> groups = PopGroupsNoOps();
            List<ExComp[]> orderedGroups = ArrayFunc.OrderListReverse(groups);

            for (int i = 0; i < orderedGroups.Count; ++i)
            {
                orderedGroups[i] = GroupHelper.OrderGroup(orderedGroups[i]);
            }

            AlgebraTerm term = PushGroups(orderedGroups);
            return term;
        }

        public void Pop()
        {
            ArrayFunc.RemoveIndex(_subComps, _subComps.Count - 1);
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
                AlgebraTerm[] numDen = GetNumDenFrac();
                if (numDen != null)
                {
                    return Operators.DivOp.StaticCombine(numDen[0], numDen[1]);
                }
            }

            return this;
        }

        public void RemoveGroup(ExComp[] group)
        {
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

            AlgebraTerm term = GroupHelper.ToAlgTerm(group);
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] compareGroup = groups[i];

                AlgebraTerm compareTerm = GroupHelper.ToAlgTerm(compareGroup);

                if (term.IsEqualTo(compareTerm))
                {
                    ArrayFunc.RemoveIndex(groups, i);
                    break;
                }
            }

            foreach (ExComp[] addGroup in groups)
                AddGroup(addGroup);
        }

        public virtual AlgebraTerm RemoveOneCoeffs()
        {
            List<ExComp[]> groups = GetGroupsNoOps();
            for (int i = 0; i < groups.Count; ++i)
            {
                groups[i] = GroupHelper.RemoveOneCoeffs(groups[i]);
            }

            return PushGroups(groups);
        }

        public virtual ExComp RemoveRedundancies(bool postWorkable)
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
            List<ExComp[]> groups = PopGroupsNoOps();
            for (int i = 0; i < groups.Count; ++i)
            {
                groups[i] = GroupHelper.RemoveOneCoeffs(groups[i]);

                if (groups[i].Length == 1 && groups[i][0] is AlgebraTerm && (groups[i][0] as AlgebraTerm).GetGroupCount() > 1)
                {
                    AlgebraTerm popTerm = groups[i][0] as AlgebraTerm;
                    ArrayFunc.RemoveIndex(groups, i);
                    groups.InsertRange(i, popTerm.GetGroupsNoOps());
                }
            }

            AlgebraTerm term = PushGroups(groups);

            if (term.GetTermCount() == 1)
            {
                return term[0];
            }

            return term;
        }

        public virtual AlgebraTerm RemoveZeros()
        {
            if (_subComps.Count == 1)
                return this;

            List<ExComp[]> groups = GetGroups();
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];

                ExNumber coeff = GetCoeffTerm(group);
                if (coeff != null && ExNumber.OpEqual(coeff, 0.0))
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

            ExComp gcfTerm = GroupHelper.ToAlgTerm(groupGcf).RemoveRedundancies(false);

            if (ExNumber.GetOne().IsEqualTo(gcfTerm))
                return this;

            ExComp thisCompare = this.RemoveRedundancies(false);

            if (thisCompare.IsEqualTo(gcfTerm))
                return this;

            ExComp factoredOut = DivOp.StaticCombine(this.CloneEx(), gcfTerm);

            return new AlgebraTerm(gcfTerm, new MulOp(), factoredOut);
        }

        public virtual AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            if (this.IsEqualTo(subOut))
                return subIn.ToAlgTerm();

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
            if (this.IsEqualTo(subOut))
            {
                success = true;
                return subIn.ToAlgTerm();
            }

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

        public override string ToAsciiString()
        {
            if (GetTermCount() == 0)
                return "0";
            List<ExComp[]> groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];
                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] num = GroupHelper.GetNumerator(group);
                    ExComp[] den = GroupHelper.GetDenominator(group, false);
                    string numTexStr = GroupHelper.ToAsciiString(num);
                    string denTexStr = GroupHelper.ToAsciiString(den);

                    finalStr += "(" + numTexStr + ")/(" + denTexStr + ")";
                }
                else
                    finalStr += GroupHelper.ToAsciiString(group);
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            if (GetGroupCount() > 1)
                finalStr = StringHelper.SurroundWithParas(finalStr);

            return finalStr;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string finalStr = "(";

            foreach (ExComp comp in _subComps)
            {
                string addStr = comp.ToJavaScriptString(useRad);
                if (addStr == null)
                    return null;

                finalStr += addStr;
            }

            finalStr += ")";

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
            List<ExComp[]> groups = GetGroupsNoOps();

            string finalStr = "";
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];
                if (GroupHelper.ContainsFrac(group))
                {
                    ExComp[] num = GroupHelper.GetNumerator(group);
                    ExComp[] den = GroupHelper.GetDenominator(group, false);
                    string numTexStr = GroupHelper.ToTexString(num);
                    string denTexStr = GroupHelper.ToTexString(den);

                    finalStr += @"\frac{" + numTexStr + "}{" + denTexStr + "}";
                }
                else
                    finalStr += GroupHelper.ToTexString(group);
                if (i != groups.Count - 1)
                    finalStr += "+";
            }

            if (GetGroupCount() > 1)
                finalStr = StringHelper.SurroundWithParas(finalStr);

            return finalStr;
        }

        public virtual ExComp WeakMakeWorkable(ref List<string> pParseErrors, ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                if (comp is AlgebraTerm)
                {
                    ExComp weakWorkable = (comp as AlgebraTerm).WeakMakeWorkable(ref pParseErrors, ref pEvalData);
                    if (weakWorkable == null)
                        return null;
                    if (ExNumber.IsUndef(weakWorkable))
                        return ExNumber.GetUndefined();
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
                    ExComp resultant = null;

                    bool isMultiGroup = false;
                    if (beforeComp is AlgebraTerm)
                        isMultiGroup = (beforeComp as AlgebraTerm).GetGroupCount() > 1 || afterComp is Structural.LinearAlg.ExMatrix;
                    if (!isMultiGroup && afterComp is AlgebraTerm)
                        isMultiGroup = (afterComp as AlgebraTerm).GetGroupCount() > 1 || afterComp is Structural.LinearAlg.ExMatrix;

                    AgOp algebraOp = comp as AgOp;

                    // Ensure that a potential division by 0 is not being ignored.
                    if (algebraOp is DivOp && afterComp is AlgebraFunction)
                    {
                        ExComp tmpEval = (afterComp.CloneEx() as AlgebraFunction).Evaluate(true, ref pEvalData);
                        if (tmpEval is AlgebraTerm)
                            tmpEval = (tmpEval as AlgebraTerm).RemoveRedundancies(false);
                        if (ExNumber.GetZero().IsEqualTo(tmpEval))
                            afterComp = tmpEval;
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
                        if (comp is Operators.PowOp)
                            resultant = algebraOp.WeakCombine(beforeComp, afterComp);

                        if (!(resultant is Structural.LinearAlg.Transpose) && !(resultant is Structural.LinearAlg.MatrixInverse))
                            resultant = algebraOp.Combine(beforeComp, afterComp);
                    }

                    if (resultant == null)
                        return null;

                    if (ExNumber.IsUndef(resultant))
                        return ExNumber.GetUndefined();

                    int startIndex = i - 1;
                    _subComps.RemoveRange(startIndex, 3);
                    _subComps.Insert(startIndex, resultant);
                    i--;
                }
            }

            ExComp finalEx = RemoveRedundancies(true);
            return finalEx;
        }

        public ExComp WeakMakeWorkable(ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _subComps.Count; ++i)
            {
                ExComp comp = _subComps[i];
                if (comp is AlgebraTerm)
                {
                    ExComp weakWorkable = (comp as AlgebraTerm).WeakMakeWorkable(ref pEvalData);
                    if (weakWorkable == null)
                        return null;
                    if (ExNumber.IsUndef(weakWorkable))
                        return ExNumber.GetUndefined();
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
                        isMultiGroup = (beforeComp as AlgebraTerm).GetGroupCount() > 1;
                    if (!isMultiGroup && afterComp is AlgebraTerm)
                        isMultiGroup = (afterComp as AlgebraTerm).GetGroupCount() > 1;

                    AgOp algebraOp = comp as AgOp;
                    // Ensure that a potential division by 0 is not being ignored.
                    if ((algebraOp is MulOp || algebraOp is DivOp) && afterComp is AlgebraFunction)
                    {
                        ExComp tmpEval = (afterComp.CloneEx() as AlgebraFunction).Evaluate(true, ref pEvalData);
                        if (tmpEval is AlgebraTerm)
                            tmpEval = (tmpEval as AlgebraTerm).RemoveRedundancies(false);
                        if (ExNumber.GetZero().IsEqualTo(tmpEval) || (tmpEval is PowerFunction && (tmpEval as PowerFunction).GetBase().IsEqualTo(ExNumber.GetZero())))
                            afterComp = tmpEval;
                    }

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

                    if (ExNumber.IsUndef(resultant))
                        return ExNumber.GetUndefined();

                    int startIndex = i - 1;
                    _subComps.RemoveRange(startIndex, 3);
                    _subComps.Insert(startIndex, resultant);
                    i--;
                }
            }

            ExComp finalEx = RemoveRedundancies(false);
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
                    ArrayFunc.RemoveIndex(comps, i--);
                    continue;
                }

                if (comps[i] is AgOp)
                    operatorPreceeding = true;
                else
                    operatorPreceeding = false;
            }

            if (comps.Count != 0 && comps.First() is AgOp)
                ArrayFunc.RemoveIndex(comps, 0);
            if (comps.Count != 0 && comps.Last() is AgOp)
                ArrayFunc.RemoveIndex(comps, comps.Count - 1);
        }

        private List<ExComp> RecursiveGroupGCFSolve(List<ExComp[]> groups)
        {
            List<ExComp[]> narrowedList = new List<ExComp[]>();

            if (groups.Count == 2)
            {
                return ArrayFunc.ToList(GroupHelper.GCF(groups[0], groups[1]));
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