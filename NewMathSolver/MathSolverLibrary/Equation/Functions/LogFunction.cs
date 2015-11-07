using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class LogFunction : BasicAppliedFunc
    {
        private ExComp _baseEx = new ExNumber(10.0);

        public void SetBase(ExComp value)
        {
            _baseEx = value;
        }

        public ExComp GetBase()
        {
            return _baseEx;
        }

        public override string GetFuncName()
        {
            if (_baseEx is Constant && (_baseEx as Constant).IsEqualTo(Constant.ParseConstant("e")))
                return "ln";
            return "log";
        }

        public LogFunction(ExComp innerEx)
            : base(innerEx, "log", FunctionType.Logarithm, typeof(LogFunction))
        {
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            AlgebraTerm innerTerm = GetInnerTerm().ConvertImaginaryToVar();
            ExComp baseEx;
            if (GetBase() is AlgebraTerm)
                baseEx = (GetBase() as AlgebraTerm).ConvertImaginaryToVar();
            else
                baseEx = GetBase();

            LogFunction retVal = new LogFunction(innerTerm);
            retVal.SetBase(baseEx);

            return retVal;
        }

        public static LogFunction Ln(ExComp innerEx)
        {
            LogFunction log = new LogFunction(innerEx);
            log.SetBase(Constant.ParseConstant("e"));

            return log;
        }

        public static LogFunction Create(ExComp innerEx, ExComp baseEx)
        {
            LogFunction logFunc = new LogFunction(innerEx);
            logFunc.SetBase(baseEx);

            return logFunc;
        }

        public override ExComp CloneEx()
        {
            LogFunction log = new LogFunction(GetInnerTerm().CloneEx());
            log.SetBase(this.GetBase().CloneEx());
            return log;
        }

        public override AlgebraTerm CompoundFractions()
        {
            ExComp compoundedBase = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).CompoundFractions() : _baseEx;
            ExComp compoundedInner = GetInnerTerm().CompoundFractions();

            LogFunction lf = new LogFunction(compoundedInner);
            lf._baseEx = compoundedBase;

            return lf;
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            bool baseValid = false;
            ExComp compoundedBase = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).CompoundFractions(out baseValid) : _baseEx;
            bool innerValid;
            ExComp compoundedInner = GetInnerTerm().CompoundFractions(out innerValid);

            LogFunction lf = new LogFunction(compoundedInner);
            lf._baseEx = compoundedBase;

            valid = baseValid || innerValid;

            return lf;
        }

        public override bool Contains(AlgebraComp varFor)
        {
            return GetInnerTerm().Contains(varFor) || GetBase().ToAlgTerm().Contains(varFor);
        }

        public override ExComp CancelWith(ExComp innerEx, ref TermType.EvalData evalData)
        {
            PowerFunction powFunc = innerEx as PowerFunction;
            if (powFunc != null && powFunc.GetBase().IsEqualTo(this.GetBase()))
                return powFunc.GetPower();
            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            double dInnerVal = double.NaN;
            double dBaseVal = double.NaN;
            double dLogVal = double.NaN;
            ExComp innerEx = GetInnerEx();

            if (ExNumber.IsUndef(innerEx))
                return ExNumber.GetUndefined();

            if ((innerEx is ExNumber && !(innerEx as ExNumber).HasImaginaryComp()) ||
                (innerEx is Constant && !(innerEx as Constant).GetValue().HasImaginaryComp()))
            {
                dInnerVal = innerEx is Constant ? (innerEx as Constant).GetValue().GetRealComp() : (innerEx as ExNumber).GetRealComp();
                if ((_baseEx is ExNumber && !(_baseEx as ExNumber).HasImaginaryComp()) ||
                    (_baseEx is Constant && !(_baseEx as Constant).GetValue().HasImaginaryComp()))
                {
                    dBaseVal = _baseEx is Constant ? (_baseEx as Constant).GetValue().GetRealComp() : (_baseEx as ExNumber).GetRealComp();

                    if (dInnerVal > 0.0)
                    {
                        dLogVal = Math.Log(dInnerVal, dBaseVal);
                    }
                }
            }

            if (harshEval)
            {
                if (!double.IsNaN(dInnerVal) && !double.IsNaN(dBaseVal) && !double.IsNaN(dLogVal))
                {
                    return new ExNumber(dLogVal);
                }

                if (dInnerVal <= 0.0)
                    return ExNumber.GetUndefined();

                return this;
            }

            if (DoubleHelper.IsInteger(dLogVal))
                return new ExNumber(dLogVal);

            if (GetInnerEx() is PowerFunction)
            {
                PowerFunction powFunc = (GetInnerEx() as PowerFunction);
                ExComp powFuncBase = powFunc.GetBase();

                if (powFuncBase.IsEqualTo(_baseEx))
                {
                    return powFunc.GetPower();
                }
            }

            return this;
        }

        public override string FinalToAsciiString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).GetVar().GetVar() == "e")
            {
                return "ln(" + GetInnerTerm().FinalToDispStr() + ")";
            }

            string baseStr = "";
            if (!(new ExNumber(10.0)).IsEqualTo(_baseEx))
                baseStr = "_(" + _baseEx.ToAlgTerm().FinalToDispStr() + ")";
            string finalStr = @"\log" + baseStr + "(" + GetInnerTerm().FinalToDispStr() + ")";

            return finalStr;
        }

        public override string FinalToDispStr()
        {
            if (USE_TEX)
                return FinalToTexString();
            return FinalToAsciiString();
        }

        public override string FinalToTexString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).GetVar().GetVar() == "e")
            {
                return "ln(" + GetInnerTerm().FinalToDispStr() + ")";
            }

            string baseStr = "";
            if (!(new ExNumber(10.0)).IsEqualTo(_baseEx))
                baseStr = "_{" + _baseEx.ToAlgTerm().FinalToDispStr() + "}";
            string finalStr = @"\log" + baseStr + "(" + GetInnerTerm().FinalToDispStr() + ")";

            return finalStr;
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            SolveResult result = agSolver.SolveRegInequality(GetInnerTerm(), ExNumber.GetZero().ToAlgTerm(), Parsing.LexemeType.Greater, varFor, ref pEvalData);
            if (!result.Success)
                return null;

            return result.Restrictions;
        }

        public override AlgebraTerm HarshEvaluation()
        {
            ExComp innerTermEval = GetInnerTerm().HarshEvaluation();
            ExComp baseTermEval = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).HarshEvaluation() : _baseEx;

            LogFunction log = new LogFunction(innerTermEval);
            log.SetBase(baseTermEval);

            return log;
        }

        public override bool HasLogFunctions()
        {
            return true;
        }

        public override bool HasTrigFunctions()
        {
            return _baseEx.ToAlgTerm().HasTrigFunctions() || GetInnerTerm().HasTrigFunctions();
        }

        public override bool IsEqualTo(MathSolverWebsite.MathSolverLibrary.Equation.ExComp ex)
        {
            if (!(ex is LogFunction))
                return false;
            LogFunction exLog = ex as LogFunction;
            if (!_baseEx.IsEqualTo(exLog._baseEx))
                return false;
            if (!GetInnerTerm().IsEqualTo(exLog.GetInnerTerm()))
                return false;

            return true;
        }

        public bool IsLn()
        {
            return _baseEx is Constant && (_baseEx as Constant).GetVar().GetVar() == "e";
        }

        public override bool IsUndefined()
        {
            if (_baseEx is ExNumber && (_baseEx as ExNumber).IsUndefined())
                return true;
            if (GetInnerTerm().IsUndefined())
                return true;
            if (_baseEx is AlgebraTerm && (_baseEx as AlgebraTerm).IsUndefined())
                return true;
            return false;
        }

        public override AlgebraTerm Order()
        {
            ExComp orderedInner = GetInnerTerm().Order();
            ExComp orderedBase = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).Order() : _baseEx;

            LogFunction log = new LogFunction(orderedInner);
            log.SetBase(orderedBase);
            return log;
        }

        public override ExComp RemoveRedundancies(bool postWorkable)
        {
            ExComp innerEx = GetInnerTerm().RemoveRedundancies(postWorkable);
            ExComp baseEx = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).RemoveRedundancies(postWorkable) : _baseEx;

            LogFunction log = new LogFunction(innerEx);
            log.SetBase(baseEx);

            return log;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            ExComp baseEx = _baseEx;
            if (baseEx.IsEqualTo(subOut))
                baseEx = subIn;
            else if (baseEx is AlgebraTerm)
                baseEx = (baseEx as AlgebraTerm).Substitute(subOut, subIn);

            ExComp innerEx = GetInnerEx();
            if (innerEx.IsEqualTo(subOut))
                innerEx = subIn;
            else if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).Substitute(subOut, subIn);

            LogFunction log = new LogFunction(innerEx);
            log.SetBase(baseEx);

            return log;
        }

        public override string ToAsciiString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).GetVar().GetVar() == "e")
            {
                return "ln(" + GetInnerTerm().FinalToAsciiString() + ")";
            }

            string baseStr = "";
            if (!(new ExNumber(10.0)).IsEqualTo(_baseEx))
                baseStr = "_(" + WorkMgr.ToDisp(_baseEx) + ")";
            string finalStr = @"\log" + baseStr + "(" + GetInnerTerm().FinalToAsciiString() + ")";

            return finalStr;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = GetInnerTerm().ToJavaScriptString(useRad);
            if (innerStr == null)
                return null;

            if (_baseEx is Constant && (_baseEx as Constant).GetVar().GetVar() == "e")
            {
                return "Math.log(" + innerStr + ")";
            }

            return "(Math.log(" + innerStr + ") / Math.LN10)";
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "LOG(" + _baseEx.ToString() + ")(" + GetInnerTerm().ToString() + ")";
        }

        public override string ToTexString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).GetVar().GetVar() == "e")
            {
                return "ln(" + GetInnerTerm().ToTexString() + ")";
            }

            string baseStr = "";
            if (!(new ExNumber(10.0)).IsEqualTo(_baseEx))
                baseStr = "_{" + _baseEx.ToTexString() + "}";
            string finalStr = @"\log" + baseStr + "(" + GetInnerTerm().ToTexString() + ")";

            return finalStr;
        }
    }
}