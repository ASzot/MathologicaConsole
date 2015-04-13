using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class LogFunction : BasicAppliedFunc
    {
        private ExComp _baseEx = new Number(10.0);

        public ExComp Base
        {
            get { return _baseEx; }
            set { _baseEx = value; }
        }

        public override string FuncName
        {
            get
            {
                if (_baseEx is Constant && (_baseEx as Constant).IsEqualTo(Constant.ParseConstant("e")))
                    return "ln";
                return "log";
            }
        }

        public LogFunction(ExComp innerEx)
            : base(innerEx, "log", FunctionType.Logarithm, typeof(LogFunction))
        {
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            AlgebraTerm innerTerm = InnerTerm.ConvertImaginaryToVar();
            ExComp baseEx;
            if (Base is AlgebraTerm)
                baseEx = (Base as AlgebraTerm).ConvertImaginaryToVar();
            else
                baseEx = Base;

            LogFunction retVal = new LogFunction(innerTerm);
            retVal.Base = baseEx;

            return retVal;
        }

        public static LogFunction Ln(ExComp innerEx)
        {
            LogFunction log = new LogFunction(innerEx);
            log.Base = Constant.ParseConstant("e");

            return log;
        }

        public override ExComp Clone()
        {
            LogFunction log = new LogFunction(InnerTerm.Clone());
            log.Base = this.Base.Clone();
            return log;
        }

        public override AlgebraTerm CompoundFractions()
        {
            ExComp compoundedBase = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).CompoundFractions() : _baseEx;
            ExComp compoundedInner = InnerTerm.CompoundFractions();

            LogFunction lf = new LogFunction(compoundedInner);
            lf._baseEx = compoundedBase;

            return lf;
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            bool baseValid = false;
            ExComp compoundedBase = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).CompoundFractions(out baseValid) : _baseEx;
            bool innerValid;
            ExComp compoundedInner = InnerTerm.CompoundFractions(out innerValid);

            LogFunction lf = new LogFunction(compoundedInner);
            lf._baseEx = compoundedBase;

            valid = baseValid || innerValid;

            return lf;
        }

        public override bool Contains(AlgebraComp varFor)
        {
            return InnerTerm.Contains(varFor) || Base.ToAlgTerm().Contains(varFor);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            double dInnerVal = double.NaN;
            double dBaseVal = double.NaN;
            double dLogVal = double.NaN;
            ExComp innerEx = InnerEx;

            if (Number.IsUndef(innerEx))
                return Number.Undefined;

            if ((innerEx is Number && !(innerEx as Number).HasImaginaryComp()) ||
                (innerEx is Constant && !(innerEx as Constant).Value.HasImaginaryComp()))
            {
                dInnerVal = innerEx is Constant ? (innerEx as Constant).Value.RealComp : (innerEx as Number).RealComp;
                if ((_baseEx is Number && !(_baseEx as Number).HasImaginaryComp()) ||
                    (_baseEx is Constant && !(_baseEx as Constant).Value.HasImaginaryComp()))
                {
                    dBaseVal = _baseEx is Constant ? (_baseEx as Constant).Value.RealComp : (_baseEx as Number).RealComp;

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
                    return new Number(dLogVal);
                }

                if (dInnerVal <= 0.0)
                    return Number.Undefined;

                return this;
            }

            if (dLogVal.IsInteger())
                return new Number(dLogVal);

            if (InnerEx is PowerFunction)
            {
                PowerFunction powFunc = (InnerEx as PowerFunction);
                ExComp powFuncBase = powFunc.Base;

                if (powFuncBase.IsEqualTo(_baseEx))
                {
                    return powFunc.Power;
                }
            }

            return this;
        }

        public override string FinalToAsciiString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).Var.Var == "e")
            {
                return "ln(" + InnerTerm.FinalToDispStr() + ")";
            }

            string baseStr = "";
            if (!(new Number(10.0)).IsEqualTo(_baseEx))
                baseStr = "_(" + _baseEx.ToAlgTerm().FinalToDispStr() + ")";
            string finalStr = @"\log" + baseStr + "(" + InnerTerm.FinalToDispStr() + ")";

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
            if (_baseEx is Constant && (_baseEx as Constant).Var.Var == "e")
            {
                return "ln(" + InnerTerm.FinalToDispStr() + ")";
            }

            string baseStr = "";
            if (!(new Number(10.0)).IsEqualTo(_baseEx))
                baseStr = "_{" + _baseEx.ToAlgTerm().FinalToDispStr() + "}";
            string finalStr = @"\log" + baseStr + "(" + InnerTerm.FinalToDispStr() + ")";

            return finalStr;
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            SolveResult result = agSolver.SolveRegInequality(InnerTerm, Number.Zero.ToAlgTerm(), Parsing.LexemeType.Greater, varFor, ref pEvalData);
            if (!result.Success)
                return null;

            return result.Restrictions;
        }

        public override AlgebraTerm HarshEvaluation()
        {
            ExComp innerTermEval = InnerTerm.HarshEvaluation();
            ExComp baseTermEval = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).HarshEvaluation() : _baseEx;

            LogFunction log = new LogFunction(innerTermEval);
            log.Base = baseTermEval;

            return log;
        }

        public override bool HasLogFunctions()
        {
            return true;
        }

        public override bool HasTrigFunctions()
        {
            return _baseEx.ToAlgTerm().HasTrigFunctions() || InnerTerm.HasTrigFunctions();
        }

        public override bool IsEqualTo(MathSolverWebsite.MathSolverLibrary.Equation.ExComp ex)
        {
            if (!(ex is LogFunction))
                return false;
            LogFunction exLog = ex as LogFunction;
            if (!_baseEx.IsEqualTo(exLog._baseEx))
                return false;
            if (!InnerTerm.IsEqualTo(exLog.InnerTerm))
                return false;

            return true;
        }

        public bool IsLn()
        {
            return _baseEx is Constant && (_baseEx as Constant).Var.Var == "e";
        }

        public override bool IsUndefined()
        {
            if (_baseEx is Number && (_baseEx as Number).IsUndefined())
                return true;
            if (InnerTerm.IsUndefined())
                return true;
            if (_baseEx is AlgebraTerm && (_baseEx as AlgebraTerm).IsUndefined())
                return true;
            return false;
        }

        public override AlgebraTerm Order()
        {
            ExComp orderedInner = InnerTerm.Order();
            ExComp orderedBase = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).Order() : _baseEx;

            LogFunction log = new LogFunction(orderedInner);
            log.Base = orderedBase;
            return log;
        }

        public override ExComp RemoveRedundancies(bool postWorkable = false)
        {
            ExComp innerEx = InnerTerm.RemoveRedundancies(postWorkable);
            ExComp baseEx = _baseEx is AlgebraTerm ? (_baseEx as AlgebraTerm).RemoveRedundancies(postWorkable) : _baseEx;

            LogFunction log = new LogFunction(innerEx);
            log.Base = baseEx;

            return log;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            ExComp baseEx = _baseEx;
            if (baseEx.IsEqualTo(subOut))
                baseEx = subIn;
            else if (baseEx is AlgebraTerm)
                baseEx = (baseEx as AlgebraTerm).Substitute(subOut, subIn);

            ExComp innerEx = InnerEx;
            if (innerEx.IsEqualTo(subOut))
                innerEx = subIn;
            else if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).Substitute(subOut, subIn);

            LogFunction log = new LogFunction(innerEx);
            log.Base = baseEx;

            return log;
        }

        public override string ToAsciiString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).Var.Var == "e")
            {
                return "ln(" + InnerTerm.FinalToAsciiString() + ")";
            }

            string baseStr = "";
            if (!(new Number(10.0)).IsEqualTo(_baseEx))
                baseStr = "_(" + WorkMgr.ExFinalToAsciiStr(_baseEx) + ")";
            string finalStr = @"\log" + baseStr + "(" + InnerTerm.FinalToAsciiString() + ")";

            return finalStr;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = InnerTerm.ToJavaScriptString(useRad);
            if (innerStr == null)
                return null;

            if (_baseEx is Constant && (_baseEx as Constant).Var.Var == "e")
            {
                return "Math.log(" + innerStr + ")";
            }

            return "(Math.log(" + innerStr + ") / Math.LN10)";
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return "LOG(" + _baseEx.ToString() + ")(" + InnerTerm.ToString() + ")";
        }

        public override string ToTexString()
        {
            if (_baseEx is Constant && (_baseEx as Constant).Var.Var == "e")
            {
                return "ln(" + InnerTerm.ToTexString() + ")";
            }

            string baseStr = "";
            if (!(new Number(10.0)).IsEqualTo(_baseEx))
                baseStr = "_{" + _baseEx.ToTexString() + "}";
            string finalStr = @"\log" + baseStr + "(" + InnerTerm.ToTexString() + ")";

            return finalStr;
        }
    }
}