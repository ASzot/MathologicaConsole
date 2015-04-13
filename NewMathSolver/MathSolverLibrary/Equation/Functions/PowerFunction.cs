using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class PowerFunction : AlgebraFunction
    {
        private ExComp _power;
        private bool b_keepRedunFlag = false;

        public ExComp Base
        {
            get
            {
                AlgebraTerm term = new AlgebraTerm();
                term.AssignTo(this);
                if (b_keepRedunFlag)
                    return term;
                ExComp finalEx = term.RemoveRedundancies();

                return finalEx;
            }
        }

        public ExComp Power
        {
            get { return _power; }
            set { _power = value; }
        }

        public PowerFunction(ExComp baseTerm, ExComp power)
        {
            if (baseTerm is AlgebraTerm && !(baseTerm is AlgebraFunction))
                _subComps.AddRange((baseTerm as AlgebraTerm).SubComps);
            else
                _subComps.Add(baseTerm);
            _power = power;
        }

        #region Operators

        public static ExComp operator *(PowerFunction pf1, PowerFunction pf2)
        {
            ExComp base1 = pf1.Base;
            ExComp base2 = pf2.Base;
            // We only combine non radicals.
            if (base1.IsEqualTo(base2))
            {
                ExComp combinedPow = AddOp.StaticCombine(pf1.Power, pf2.Power);
                if (combinedPow is AlgebraTerm)
                {
                    combinedPow = (combinedPow as AlgebraTerm).CompoundFractions();
                    combinedPow = (combinedPow as AlgebraTerm).ReduceFracs();
                }
                AlgebraTerm combinedPowTerm = combinedPow.ToAlgTerm();
                if (combinedPowTerm.IsZero())
                {
                    AlgebraTerm term = new AlgebraTerm();
                    term.Add(new Number(1.0));
                    return term;
                }
                if (combinedPowTerm.IsOne())
                    return base1;
                PowerFunction resultant = new PowerFunction(base1, combinedPow);
                return resultant;
            }
            else if (pf1.Power.IsEqualTo(pf2.Power))
            {
                ExComp combinedBase = MulOp.StaticCombine(base1, base2);
                return new PowerFunction(combinedBase, pf1.Power);
            }
            else
            {
                AlgebraTerm term = new AlgebraTerm();
                term.Add(pf1, new MulOp(), pf2);

                return term;
            }
        }

        public static ExComp operator *(PowerFunction pf, AlgebraComp comp)
        {
            if (!pf.Base.IsEqualTo(comp))
            {
                AlgebraTerm term = new AlgebraTerm();
                term.Add(pf, new MulOp(), comp);

                return term;
            }
            PowerFunction compFunc = new PowerFunction(comp, new Number(1.0));
            ExComp resultant = pf * compFunc;
            return resultant;
        }

        public static ExComp operator *(PowerFunction pf, AlgebraTerm term)
        {
            if (!pf.Base.IsEqualTo(term))
            {
                bool multiplyOut = true;

                var groups = term.GetGroupsNoOps();

                int modGroupCount = 0;

                for (int i = 0; i < groups.Count; ++i)
                {
                    var group = groups[i];
                    bool equalTerm = false;
                    for (int j = 0; j < group.Length; ++j)
                    {
                        var groupComp = group[j];
                        if (groupComp.IsEqualTo(pf))
                        {
                            equalTerm = true;
                            break;
                        }
                        else if (groupComp.IsEqualTo(pf.Base) || (groupComp is PowerFunction && (groupComp as PowerFunction).Base.IsEqualTo(pf.Base)))
                        {
                            equalTerm = true;
                            break;
                        }
                        else if (groupComp is PowerFunction && (groupComp as PowerFunction).Power.IsEqualTo(pf.Power) &&
                            (groupComp as PowerFunction).Base is Number && pf.Base is Number)
                        {
                            if (modGroupCount != i)
                                continue;

                            PowerFunction pfGc = groupComp as PowerFunction;
                            ExComp resultBase = MulOp.StaticCombine(pfGc.Base, pf.Base);
                            PowerFunction pfAdd = new PowerFunction(resultBase, pf.Power);
                            group[j] = pfAdd;

                            groups[i] = group;

                            modGroupCount++;

                            break;
                        }
                    }

                    if (equalTerm)
                        break;

                    // This used to be an || but I changed it. I really don't know what the consequences of doing this are.
                    if (!equalTerm && modGroupCount != i + 1)
                    {
                        multiplyOut = false;
                        break;
                    }
                }

                if (modGroupCount != 0)
                {
                    return (new AlgebraTerm(groups.ToArray())).WeakMakeWorkable();
                }

                if (multiplyOut && (groups.Count != 1 || groups[0].Length != 1))
                {
                    for (int i = 0; i < groups.Count; ++i)
                    {
                        groups[i] = AlgebraTerm.MultiplyGroup(groups[i], pf);
                    }

                    AlgebraTerm multiplied = new AlgebraTerm(groups.ToArray());
                    ExComp final = multiplied.MakeWorkable();

                    return final;
                }

                return MulOp.StaticWeakCombine(pf, term);
            }
            PowerFunction termFunc = new PowerFunction(term, new Number(1.0));
            ExComp resultant = pf * termFunc;
            return resultant;
        }

        public static ExComp operator /(PowerFunction pf1, PowerFunction pf2)
        {
            ExComp base1 = pf1.Base;
            ExComp base2 = pf2.Base;
            if (base1.IsEqualTo(base2))
            {
                AlgebraTerm power = new AlgebraTerm();
                power.Add(pf1.Power, new SubOp(), pf2.Power);
                ExComp workablePow = power.MakeWorkable();
                if ((workablePow is AlgebraTerm && (workablePow as AlgebraTerm).IsZero()) ||
                    (workablePow is Number && (workablePow as Number) == 0.0))
                {
                    AlgebraTerm term = new AlgebraTerm();
                    term.Add(new Number(1.0));
                    return term;
                }

                if (power is AlgebraTerm)
                    power = (power as AlgebraTerm).CompoundFractions();

                PowerFunction resultant = new PowerFunction(base1, power);
                return resultant;
            }
            else
            {
                AlgebraTerm term = new AlgebraTerm();
                term.Add(pf1, new DivOp(), pf2);

                return term;
            }
        }

        public static ExComp operator +(PowerFunction pf1, PowerFunction pf2)
        {
            AlgebraTerm term = new AlgebraTerm();
            term.Add(pf1, new AddOp(), pf2);
            return term;
        }

        #endregion Operators

        public static ExComp FixFraction(ExComp power)
        {
            if (power is AlgebraTerm)
                power = (power as AlgebraTerm).RemoveRedundancies();
            if (power is PowerFunction && (power as PowerFunction).Power.IsEqualTo(Number.NegOne))
            {
                power = MulOp.StaticWeakCombine(Number.One, power);
            }

            return power;
        }

        public override AlgebraTerm ApplyOrderOfOperations()
        {
            AlgebraTerm innerTerm = Base.ToAlgTerm();
            innerTerm = innerTerm.ApplyOrderOfOperations();

            if (_power is AlgebraTerm)
                _power = (_power as AlgebraTerm).ApplyOrderOfOperations();

            return new PowerFunction(innerTerm, _power);
        }

        public override void AssignTo(AlgebraTerm algebraTerm)
        {
            if (algebraTerm is PowerFunction)
            {
                PowerFunction powerFunc = algebraTerm as PowerFunction;
                base.AssignTo(powerFunc);
                _power = new AlgebraTerm();
                _power = powerFunc.Power;
            }
            else
                throw new ArgumentException();
        }

        public override ExComp Clone()
        {
            return new PowerFunction(Base.Clone(), _power.Clone());
        }

        public override AlgebraTerm CompoundFractions()
        {
            ExComp innerCompounded = Base is AlgebraTerm ? (Base as AlgebraTerm).CompoundFractions() : Base;
            ExComp powerCompounded = _power is AlgebraTerm ? (_power as AlgebraTerm).CompoundFractions() : _power;

            return new PowerFunction(innerCompounded, powerCompounded);
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            bool innerValid = false;
            ExComp innerCompounded = Base is AlgebraTerm ? (Base as AlgebraTerm).CompoundFractions(out innerValid) : Base;

            bool powerValid = false;
            ExComp powerCompounded = _power is AlgebraTerm ? (_power as AlgebraTerm).CompoundFractions(out powerValid) : _power;

            valid = innerValid || powerValid;

            return new PowerFunction(innerCompounded, powerCompounded);
        }

        public override bool Contains(AlgebraComp varFor)
        {
            if (Base is AlgebraTerm && (Base as AlgebraTerm).Contains(varFor))
                return true;
            else if (Base is AlgebraComp && (Base as AlgebraComp) == varFor)
                return true;

            if (Power is AlgebraTerm && (Power as AlgebraTerm).Contains(varFor))
                return true;
            else if (Power is AlgebraComp && (Power as AlgebraComp) == varFor)
                return true;

            return false;
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            ExComp baseEx = Base is AlgebraTerm ? (Base as AlgebraTerm).ConvertImaginaryToVar() : Base;
            ExComp powEx = Power is AlgebraTerm ? (Power as AlgebraTerm).ConvertImaginaryToVar() : Power;

            return new PowerFunction(baseEx, powEx);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp baseEx = Base;
            if (Number.IsUndef(baseEx) || Number.IsUndef(_power))
                return Number.Undefined;

            if (Number.Zero.IsEqualTo(baseEx))
                return Number.Zero;

            if (baseEx is AlgebraTerm)
            {
                AlgebraTerm surroundingBase = new AlgebraTerm(baseEx);
                surroundingBase.EvaluateFunctions(harshEval, ref pEvalData);
                baseEx = surroundingBase;
            }

            if (_power is AlgebraTerm)
            {
                AlgebraTerm surroundingPow = new AlgebraTerm(_power);
                surroundingPow.EvaluateFunctions(harshEval, ref pEvalData);
                _power = surroundingPow;
            }

            if (harshEval)
            {
                if (baseEx is Number && _power is Number)
                {
                    Number nBase = baseEx as Number;
                    Number nPow = _power as Number;

                    Number raised = Number.RaiseToPower(nBase, nPow);
                    if (raised != null)
                        return raised;
                }
            }

            //return PowOp.StaticCombine(baseEx, _power);
            return new PowerFunction(baseEx, _power);
        }

        public override string FinalToAsciiString()
        {
            string powerAsciiStr = _power.ToAsciiString();
            if (powerAsciiStr == ("-1"))
                powerAsciiStr = powerAsciiStr.Remove(0, 2);

            ExComp baseNoRedun = Base is AlgebraTerm ? (Base as AlgebraTerm).RemoveRedundancies() : Base;
            if (baseNoRedun is TrigFunction || baseNoRedun is InverseTrigFunction)
            {
                BasicAppliedFunc funcBase = baseNoRedun as BasicAppliedFunc;

                return funcBase.FuncName + "^{" + powerAsciiStr + "}(" + funcBase.InnerTerm.ToAsciiString() + ")";
            }

            string baseAsciiStr = Base is AlgebraTerm ? (Base as AlgebraTerm).FinalToDispStr() : Base.ToAsciiString();

            if (powerAsciiStr == "")
                return baseAsciiStr;

            if (powerAsciiStr == "0.5")
                return @"sqrt(" + baseAsciiStr + ")";
            else if (_power is AlgebraTerm)
            {
                AlgebraTerm powTerm = _power as AlgebraTerm;

                AlgebraTerm[] numDen = powTerm.GetNumDenFrac();
                if (numDen != null)
                {
                    ExComp num = numDen[0].RemoveRedundancies();
                    ExComp den = numDen[1].RemoveRedundancies();

                    if (Number.One.IsEqualTo(num))
                    {
                        if ((new Number(2.0)).IsEqualTo(den))
                            return "sqrt(" + baseAsciiStr + ")";
                        else
                            return "root(" + den.ToAsciiString() + ")(" + baseAsciiStr + ")";
                    }
                }
            }

            if (!(Base is Constant || Base is AlgebraComp))
                baseAsciiStr = baseAsciiStr.SurroundWithParas();

            return baseAsciiStr + "^(" + powerAsciiStr + ")";
        }

        public override string FinalToTexString()
        {
            string powerTexStr = _power.ToTexString();
            if (powerTexStr == ("-1"))
                powerTexStr = powerTexStr.Remove(0, 2);

            ExComp baseNoRedun = Base is AlgebraTerm ? (Base as AlgebraTerm).RemoveRedundancies() : Base;
            if (baseNoRedun is TrigFunction || baseNoRedun is InverseTrigFunction)
            {
                BasicAppliedFunc funcBase = baseNoRedun as BasicAppliedFunc;

                return funcBase.FuncName + "^{" + powerTexStr + "}(" + funcBase.InnerTerm.ToTexString() + ")";
            }

            string baseTexStr = Base is AlgebraTerm ? (Base as AlgebraTerm).FinalToDispStr() : Base.ToTexString();

            if (powerTexStr == "")
                return baseTexStr;

            if (powerTexStr == "0.5")
                return @"sqrt(" + baseTexStr + ")";
            else if (_power is AlgebraTerm)
            {
                AlgebraTerm powTerm = _power as AlgebraTerm;

                AlgebraTerm[] numDen = powTerm.GetNumDenFrac();
                if (numDen != null)
                {
                    ExComp num = numDen[0].RemoveRedundancies();
                    ExComp den = numDen[1].RemoveRedundancies();

                    if (Number.One.IsEqualTo(num))
                    {
                        if ((new Number(2.0)).IsEqualTo(den))
                            return "sqrt(" + baseTexStr + ")";
                        else
                            return "root(" + den.ToTexString() + ")(" + baseTexStr + ")";
                    }
                }
            }
            else
            {
                if (!Regex.IsMatch(baseTexStr, @"^\d$") && baseTexStr.Length > 1 && !(baseTexStr.StartsWith("(") && baseTexStr.EndsWith(")")))
                    baseTexStr = baseTexStr.SurroundWithParas();
            }

            return baseTexStr + "^{" + powerTexStr + "}";
        }

        public ExComp FlipFrac()
        {
            AlgebraTerm powTerm = new AlgebraTerm(Number.NegOne, new MulOp(), _power);
            _power = powTerm.MakeWorkable();

            if (_power is Number && (_power as Number) == 1.0)
                return Base;

            return this;
        }

        public override AlgebraTerm ForceCombineExponents()
        {
            ExComp baseEx = Base;
            if (Base is AlgebraTerm)
                baseEx = (Base as AlgebraTerm).ForceCombineExponents();

            if (Base is PowerFunction)
            {
                ExComp pow = (Base as PowerFunction).Power;
                ExComp mulPow = MulOp.StaticCombine(pow, _power);

                return new PowerFunction((Base as PowerFunction).Base, mulPow);
            }

            return new PowerFunction(baseEx, _power);
        }

        public override List<FunctionType> GetAppliedFunctionsNoPow(AlgebraComp varFor)
        {
            AlgebraTerm surrounded = new AlgebraTerm(this);
            return surrounded.GetAppliedFunctionsNoPow(varFor);
        }

        public override double GetCompareVal()
        {
            Number powerNum = null;
            if (_power is AlgebraTerm)
            {
                AlgebraTerm powerTerm = _power as AlgebraTerm;
                if (powerTerm.TermCount == 1 && powerTerm.SubComps[0] is Number)
                    powerNum = powerTerm.SubComps[0] as Number;
                AlgebraTerm[] numDen = powerTerm.GetNumDenFrac();
                if (numDen != null)
                {
                    ExComp num = numDen[0].RemoveRedundancies();
                    ExComp den = numDen[1].RemoveRedundancies();
                    if (Number.One.IsEqualTo(num) && den is Number && (den as Number).IsRealInteger())
                        return (den as Number).GetReciprocal().RealComp;
                }
            }
            else if (_power is Number)
            {
                powerNum = _power as Number;
            }

            if (powerNum == null)
                return 1.0;
            return powerNum.RealComp;
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            int root;
            if (HasIntRoot(out root))
            {
                if (root % 2 != 0 || Base is Number)
                    return null;

                pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + WorkMgr.EDM, "The above radical cannot be anything less than 0. The domain will be restricted by " + WorkMgr.STM + "{0}" +
                    Restriction.ComparisonOpToStr(Parsing.LexemeType.GreaterEqual) + "0" + WorkMgr.EDM, Base);

                SolveResult result = agSolver.SolveRegInequality(Base.ToAlgTerm(), Number.Zero.ToAlgTerm(), Parsing.LexemeType.GreaterEqual, varFor, ref pEvalData);
                if (!result.Success)
                    return null;

                return result.Restrictions;
            }

            return new List<Restriction>();
        }

        public override List<ExComp[]> GetGroups()
        {
            List<ExComp[]> groups = new List<ExComp[]>();

            AlgebraTerm baseTerm = new AlgebraTerm();
            foreach (var comp in _subComps)
            {
                baseTerm.Add(comp);
            }

            ExComp baseExComp = baseTerm.RemoveRedundancies();

            PowerFunction powFunc = new PowerFunction(baseExComp, _power);
            ExComp[] group = { powFunc };

            groups.Add(group);

            return groups;
        }

        public override List<ExComp> GetPowersOfVar(AlgebraComp varFor)
        {
            List<ExComp> powers = new List<ExComp>();
            if (Base is AlgebraTerm && (Base as AlgebraTerm).Contains(varFor))
                powers.Add(_power);
            else if (Base is AlgebraComp && (Base as AlgebraComp) == varFor)
                powers.Add(_power);
            else
                powers = base.GetPowersOfVar(varFor);

            return powers.Distinct().ToList();
        }

        public override AlgebraTerm HarshEvaluation()
        {
            ExComp evalBase = Base.ToAlgTerm().HarshEvaluation();
            ExComp evalPow = _power;
            if (_power is AlgebraTerm)
                evalPow = (_power as AlgebraTerm).HarshEvaluation();

            return new PowerFunction(evalBase, evalPow);
        }

        public bool HasIntPow()
        {
            if (_power is AlgebraTerm)
                _power = (_power as AlgebraTerm).RemoveRedundancies();
            if (_power is Number && (_power as Number).IsRealInteger())
            {
                return true;
            }

            return false;
        }

        public bool HasIntPow(out int pow)
        {
            pow = int.MaxValue;
            if (_power is AlgebraTerm)
                _power = (_power as AlgebraTerm).RemoveRedundancies();
            if (_power is Number && (_power as Number).IsRealInteger())
            {
                pow = (int)(_power as Number).RealComp;
                return true;
            }

            return false;
        }

        public bool HasIntRoot(out int root)
        {
            root = int.MaxValue;
            if (_power is AlgebraTerm)
                _power = (_power as AlgebraTerm).RemoveRedundancies();
            if (_power is Number)
            {
                Number powRecip = (_power as Number).GetReciprocal();
                if (!powRecip.IsRealInteger())
                    return false;
                root = (int)powRecip.RealComp;
            }
            else if (_power is AlgebraTerm)
            {
                AlgebraTerm powTerm = _power as AlgebraTerm;
                Term.SimpleFraction frac = new Term.SimpleFraction();
                if (!frac.Init(powTerm))
                    return false;

                ExComp recip = frac.GetReciprocal();
                if (recip is Number && (recip as Number).IsRealInteger())
                {
                    root = (int)(recip as Number).RealComp;
                }
            }
            else
                return false;

            return true;
        }

        public override bool HasLogFunctions()
        {
            bool baseHas = false;
            if (Base is AlgebraTerm)
                baseHas = (Base as AlgebraTerm).HasLogFunctions();

            if (baseHas)
                return true;

            bool powHas = false;
            if (_power is AlgebraTerm)
                powHas = (_power as AlgebraTerm).HasLogFunctions();

            return powHas;
        }

        public override bool HasTrigFunctions()
        {
            return _power.ToAlgTerm().HasTrigFunctions() || Base.ToAlgTerm().HasTrigFunctions();
        }

        public override bool HasVariablePowers(AlgebraComp varFor)
        {
            if (base.HasVariablePowers(varFor))
                return true;

            if (_power is AlgebraTerm)
            {
                AlgebraTerm powerTerm = _power as AlgebraTerm;
                if (powerTerm.Contains(varFor))
                    return true;
            }
            else if (_power is AlgebraComp && _power.IsEqualTo(varFor))
                return true;

            return false;
        }

        public bool IsDenominator()
        {
            if (_power is AlgebraTerm)
                _power = (_power as AlgebraTerm).RemoveRedundancies();
            if (_power is Number)
            {
                Number powNum = _power as Number;
                return (powNum < 0.0);
            }

            return false;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is PowerFunction)
            {
                PowerFunction powerFunc = ex as PowerFunction;

                if (Base.IsEqualTo(powerFunc.Base))
                {
                    // There are different ways to represent powers.
                    // 0.5 or 1/2.
                    if (powerFunc.Power is AlgebraTerm && _power is Number)
                    {
                        AlgebraTerm powerFuncPowTerm = powerFunc.Power as AlgebraTerm;
                        AlgebraTerm[] numDen = powerFuncPowTerm.GetNumDenFrac();
                        if (numDen != null)
                        {
                            ExComp num = numDen[0].RemoveRedundancies();
                            ExComp den = numDen[1].RemoveRedundancies();

                            if (num is Number && den is Number)
                            {
                                ExComp divNum = (num as Number) / (den as Number);
                                if (divNum.IsEqualTo(_power))
                                    return true;
                            }
                        }
                    }
                    else if (_power is AlgebraTerm && powerFunc.Power is Number)
                    {
                        AlgebraTerm powerTerm = _power as AlgebraTerm;
                        AlgebraTerm[] numDen = powerTerm.GetNumDenFrac();
                        if (numDen != null)
                        {
                            ExComp num = numDen[0].RemoveRedundancies();
                            ExComp den = numDen[1].RemoveRedundancies();

                            if (num is Number && den is Number)
                            {
                                ExComp divNum = (num as Number) / (den as Number);
                                if (divNum.IsEqualTo(powerFunc.Power))
                                    return true;
                            }
                        }
                    }
                    else
                        return _power.IsEqualTo(powerFunc.Power);
                }
            }

            return false;
        }

        public bool IsRadical()
        {
            if (_power is Number && !(_power as Number).IsRealInteger())
                return true;

            if (_power is AlgebraTerm && (_power as AlgebraTerm).ContainsOnlyFractions())
                return true;

            return false;
        }

        public override bool IsUndefined()
        {
            if (Base is Number && (Base as Number).IsUndefined())
                return true;
            if (_power is Number && (_power as Number).IsUndefined())
                return true;
            if (Base is AlgebraTerm && (Base as AlgebraTerm).IsUndefined())
                return true;
            if (_power is AlgebraTerm && (_power as AlgebraTerm).IsUndefined())
                return true;
            return false;
        }

        public override ExComp MakeWorkable()
        {
            // Here redundancies are important to keeping the order of operations.
            b_keepRedunFlag = true;
            ExComp baseTerm = Base is AlgebraTerm ? (Base as AlgebraTerm).MakeWorkable() : Base;
            b_keepRedunFlag = false;
            ExComp powerTerm = _power;
            if (_power is AlgebraTerm)
            {
                powerTerm = (_power as AlgebraTerm).MakeWorkable();
            }

            powerTerm = FixFraction(powerTerm);

            return new PowerFunction(baseTerm, powerTerm);
        }

        public override AlgebraTerm Order()
        {
            ExComp baseTerm = Base;
            ExComp powerTerm = _power;

            if (Base is AlgebraTerm)
                baseTerm = (Base as AlgebraTerm).Order();
            if (Power is AlgebraTerm)
                powerTerm = (_power as AlgebraTerm).Order();

            PowerFunction powFunc = new PowerFunction(baseTerm, powerTerm);
            return powFunc;
        }

        public override List<ExComp[]> PopGroups()
        {
            var groups = GetGroups();

            _subComps.Clear();

            return groups;
        }

        public override AlgebraTerm PushGroups(List<ExComp[]> groups)
        {
            if (groups.Count == 1 && groups[0].Count() == 1 && groups[0][0] is PowerFunction)
            {
                PowerFunction powerFunc = groups[0][0] as PowerFunction;
                return powerFunc;
            }
            else
            {
                AlgebraTerm term = new AlgebraTerm();
                foreach (ExComp[] group in groups)
                    term.AddGroup(group);

                return term;
            }
        }

        public override AlgebraTerm RemoveOneCoeffs()
        {
            ExComp baseEx = Base;
            if (baseEx is AlgebraTerm)
                baseEx = (baseEx as AlgebraTerm).RemoveOneCoeffs();

            return new PowerFunction(baseEx, _power);
        }

        public override ExComp RemoveRedundancies(bool postWorkable = false)
        {
            ExComp baseTerm = Base is AlgebraTerm ? (Base as AlgebraTerm).RemoveRedundancies(postWorkable) : Base;
            ExComp powerTerm = Power is AlgebraTerm ? (Power as AlgebraTerm).RemoveRedundancies(postWorkable) : Power;

            if (powerTerm is Number)
            {
                Number powerNum = powerTerm as Number;
                if (powerNum == 0.0)
                    return new Number(1.0);
                else if (powerNum == 1.0)
                    return baseTerm;
            }

            return new PowerFunction(baseTerm, powerTerm);
        }

        public override AlgebraTerm RemoveZeros()
        {
            AlgebraTerm baseTerm = Base is AlgebraTerm ? (Base as AlgebraTerm).RemoveZeros() : Base.ToAlgTerm();

            AlgebraTerm oneTerm = new AlgebraTerm();
            oneTerm.Add(new Number(1.0));

            ExComp power = null;
            if (_power is AlgebraTerm)
            {
                AlgebraTerm powerTerm = _power as AlgebraTerm;
                powerTerm = powerTerm.RemoveZeros();
                if (powerTerm.TermCount == 0)
                    return oneTerm;

                power = powerTerm;
            }
            else if (_power is Number)
            {
                Number number = _power as Number;
                if (number == 0.0)
                    return oneTerm;
                if (number == 1.0)
                    return baseTerm;

                power = number;
            }
            else
                power = _power;

            PowerFunction powerFunc = new PowerFunction(baseTerm, power);
            return powerFunc;
        }

        public ExComp SimplifyRadical()
        {
            int root;
            if (!HasIntRoot(out root))
                return this;

            // See if any of the radical terms can be moved out.
            AlgebraTerm baseTerm = Base.ToAlgTerm();
            baseTerm = baseTerm.SimpleFactor();
            if (baseTerm.GroupCount != 1)
                return this;
            List<ExComp> mulTerms = new List<ExComp>();
            for (int i = 0; i < baseTerm.TermCount; ++i)
            {
                ExComp subComp = baseTerm[i];


                if (subComp is Number && (subComp as Number).IsRealInteger())
                {
                    Number nSubComp = subComp as Number;
                    int n = (int)nSubComp.RealComp;


                    if (root % 2 == 0)
                    {
                        // This is an even root. Take out any negative terms in the radical.
                        if (n < 0)
                        {
                            mulTerms.Add(new Number(0.0, 1.0));
                            baseTerm[i] = -nSubComp;
                            n = -n;
                        }
                    }

                    int[] divisors;
                    if (root % 2 != 0)
                        divisors = Math.Abs(n).GetDivisors(true);
                    else
                        divisors = n.GetDivisors(true);

                    for (int j = divisors.Length - 1; j >= 0; --j)
                    {
                        int divisor = divisors[j];
                        int rootResult;
                        if (divisor.IsPerfectRoot(root, out rootResult))
                        {
                            n = n / divisor;
                            if (n < 0 && root % 2 != 0)
                            {
                                rootResult = -rootResult;
                                n = -n;
                            }
                            baseTerm[i] = new Number(n);
                            Number outside = new Number(rootResult);
                            mulTerms.Add(outside);
                            break;
                        }
                    }
                }
                else if (subComp is PowerFunction)
                {
                    PowerFunction pfSubComp = subComp as PowerFunction;
                    ExComp powBase = pfSubComp.Base;

                    int comparePow;
                    if (pfSubComp.HasIntPow(out comparePow))
                    {
                        if (comparePow == 1 || comparePow == 0)
                            continue;
                        int outside = comparePow / root;
                        int inside = comparePow % root;

                        if (outside != 0)
                        {
                            ExComp outsideEx = outside == 1 ? powBase : PowOp.StaticCombine(powBase, new Number(outside));
                            ExComp insideEx = inside == 1 ? powBase : PowOp.StaticCombine(powBase, new Number(inside));

                            baseTerm[i] = insideEx;
                            mulTerms.Add(outsideEx);
                        }
                    }
                }
            }

            if (mulTerms.Count == 0)
                return this;

            AlgebraTerm outsideTerm = mulTerms.ToArray().ToAlgTerm();
            if (baseTerm.IsOne())
                return outsideTerm;

            ExComp baseEx = baseTerm.MakeWorkable();
            if (baseEx is AlgebraTerm)
                baseEx = (baseEx as AlgebraTerm).RemoveRedundancies();
            if (Number.One.IsEqualTo(baseEx))
                return outsideTerm.WeakMakeWorkable();

            ExComp finalPowFunc = PowOp.StaticWeakCombine(baseEx, _power);
            ExComp finalTerm = MulOp.StaticWeakCombine(outsideTerm.WeakMakeWorkable(), finalPowFunc);

            return finalTerm;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            ExComp baseEx = Base;

            if (subOut is PowerFunction)
            {
                PowerFunction pfSubOut = subOut as PowerFunction;
                if (pfSubOut.Base.IsEqualTo(baseEx))
                {
                    ExComp gcf = DivOp.GetCommonFactor(_power, pfSubOut._power);

                    if (gcf != null && !Number.Zero.IsEqualTo(gcf) && !Number.One.IsEqualTo(gcf))
                    {
                        ExComp divPow = DivOp.StaticCombine(_power, gcf);
                        return PowOp.StaticCombine(subIn, divPow).ToAlgTerm();
                    }
                }
            }

            if (baseEx.IsEqualTo(subOut))
            {
                baseEx = subIn;
            }
            else if (baseEx is AlgebraTerm)
            {
                baseEx = (baseEx as AlgebraTerm).Substitute(subOut, subIn);
            }

            ExComp powerEx = _power;
            if (powerEx.IsEqualTo(subOut))
                powerEx = subIn;
            else if (powerEx is AlgebraTerm)
            {
                powerEx = (powerEx as AlgebraTerm).Substitute(subOut, subIn);
            }

            return new PowerFunction(baseEx, powerEx);
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            ExComp baseEx = Base;

            if (subOut is PowerFunction)
            {
                PowerFunction pfSubOut = subOut as PowerFunction;
                if (pfSubOut.Base.IsEqualTo(baseEx))
                {
                    ExComp gcf = DivOp.GetCommonFactor(_power, pfSubOut._power);

                    if (gcf != null && !Number.Zero.IsEqualTo(gcf) && !Number.One.IsEqualTo(gcf))
                    {
                        ExComp divPow = DivOp.StaticCombine(_power, gcf);
                        success = true;
                        return PowOp.StaticCombine(subIn, divPow).ToAlgTerm();
                    }
                }
            }

            if (baseEx.IsEqualTo(subOut))
            {
                success = true;
                baseEx = subIn;
            }
            else if (baseEx is AlgebraTerm)
            {
                baseEx = (baseEx as AlgebraTerm).Substitute(subOut, subIn, ref success);
            }

            ExComp powerEx = _power;
            if (powerEx.IsEqualTo(subOut))
            {
                success = true;
                powerEx = subIn;
            }
            else if (powerEx is AlgebraTerm)
            {
                powerEx = (powerEx as AlgebraTerm).Substitute(subOut, subIn, ref success);
            }

            return new PowerFunction(baseEx, powerEx);
        }

        public override bool TermsRelatable(ExComp comp)
        {
            if (comp is AlgebraTerm)
                comp = (comp as AlgebraTerm).RemoveRedundancies();

            // This is to prevent stuff like 2 and sqrt(2) from combining.
            // While technically these could combine to keep things looking nice
            // they are not combined in this program.
            if (comp is Number)
                return false;

            if (comp is PowerFunction)
            {
                PowerFunction powFunc = comp as PowerFunction;
                if (Base.IsEqualTo(powFunc.Base))
                    return true;
                return false;
            }

            return Base.IsEqualTo(comp);
        }

        public override string ToAsciiString()
        {
            string powerAsciiStr = _power.ToAsciiString();
            if (powerAsciiStr.StartsWith("-1") && !powerAsciiStr.StartsWith("-1*"))
                powerAsciiStr = powerAsciiStr.Remove(0, 2);

            ExComp baseNoRedun = Base is AlgebraTerm ? (Base as AlgebraTerm).RemoveRedundancies() : Base;
            if (baseNoRedun is TrigFunction || baseNoRedun is InverseTrigFunction)
            {
                BasicAppliedFunc funcBase = baseNoRedun as BasicAppliedFunc;

                return funcBase.FuncName + "^{" + powerAsciiStr + "}(" + funcBase.InnerTerm.ToAsciiString() + ")";
            }

            string baseAsciiStr = Base.ToAsciiString();

            if (powerAsciiStr == "")
                return baseAsciiStr;

            bool surrounded = false;

            if (powerAsciiStr == "0.5")
            {
                string finalBaseAsciiStr = WorkMgr.ExFinalToAsciiStr(Base);
                return @"sqrt(" + finalBaseAsciiStr + ")";
            }
            else if (_power is AlgebraTerm)
            {
                string finalBaseAsciiStr = WorkMgr.ExFinalToAsciiStr(Base);
                AlgebraTerm powTerm = _power as AlgebraTerm;

                AlgebraTerm[] numDen = powTerm.GetNumDenFrac();
                if (numDen != null)
                {
                    ExComp num = numDen[0].RemoveRedundancies();
                    ExComp den = numDen[1].RemoveRedundancies();

                    if (Number.One.IsEqualTo(num))
                    {
                        if ((new Number(2.0)).IsEqualTo(den))
                            return "sqrt(" + finalBaseAsciiStr + ")";
                        else
                            return "root(" + den.ToAsciiString() + ")(" + finalBaseAsciiStr + ")";
                    }
                }
            }
            else
            {
                surrounded = true;
                if ((!Regex.IsMatch(baseAsciiStr, @"^\d$") && baseAsciiStr.Length > 1 && !(baseAsciiStr.StartsWith("(") && baseAsciiStr.EndsWith(")"))))
                    baseAsciiStr = baseAsciiStr.SurroundWithParas();
            }

            if (!surrounded && Base is Number)
            {
                baseAsciiStr = baseAsciiStr.SurroundWithParas();
            }

            return baseAsciiStr + "^(" + powerAsciiStr + ")";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string baseStr = base.ToJavaScriptString(useRad);
            if (baseStr == null)
                return null;
            string powerStr = _power.ToJavaScriptString(useRad);
            if (powerStr == null)
                return null;

            return "Math.pow(" + baseStr + "," + powerStr + ")";
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "PF(" + base.ToString() + ", " + _power.ToString() + ")";
        }

        public override string ToTexString()
        {
            string powerTexStr = _power.ToTexString();
            if (powerTexStr.StartsWith("-1") && !powerTexStr.StartsWith("-1*"))
                powerTexStr = powerTexStr.Remove(0, 2);

            ExComp baseNoRedun = Base is AlgebraTerm ? (Base as AlgebraTerm).RemoveRedundancies() : Base;
            if (baseNoRedun is TrigFunction || baseNoRedun is InverseTrigFunction)
            {
                BasicAppliedFunc funcBase = baseNoRedun as BasicAppliedFunc;

                return funcBase.FuncName + "^{" + powerTexStr + "}(" + funcBase.InnerTerm.ToTexString() + ")";
            }

            string baseTexStr = Base.ToTexString();

            if (powerTexStr == "")
                return baseTexStr;

            if (powerTexStr == "0.5" || powerTexStr == @"\frac{1}{2}")
                return @"\sqrt{" + baseTexStr + "}";
            else if (powerTexStr.StartsWith(@"\frac"))
            {
                var matches = Regex.Matches(powerTexStr, MathSolverLibrary.Parsing.LexicalParser.REAL_NUM_PATTERN);
                if (matches.Count != 2)
                    throw new ArgumentException();
                string numStr = matches[0].Value;
                string denStr = matches[1].Value;

                string radicalRootStr;
                if (numStr == "1")
                {
                    radicalRootStr = denStr;
                }
                else
                {
                    radicalRootStr = @"\frac{" + denStr + "}{" + numStr + "}";
                }

                return @"\sqrt[" + radicalRootStr + "]{" + baseTexStr + "}";
            }
            else
            {
                if (!Regex.IsMatch(baseTexStr, @"^\d$") && baseTexStr.Length > 1 && !(baseTexStr.StartsWith("(") && baseTexStr.EndsWith(")")))
                    baseTexStr = baseTexStr.SurroundWithParas();
            }

            return baseTexStr + "^{" + powerTexStr + "}";
        }
    }
}