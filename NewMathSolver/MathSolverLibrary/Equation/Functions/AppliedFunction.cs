using System;
using System.Collections.Generic;
using System.Linq;

using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal abstract class AppliedFunction : AlgebraFunction
    {
        protected FunctionType _functionType;
        protected Type _type;

        public FunctionType FunctionType
        {
            get { return _functionType; }
        }

        public ExComp InnerEx
        {
            get { return InnerTerm.RemoveRedundancies(); }
        }

        public AlgebraTerm InnerTerm
        {
            get { return new AlgebraTerm(_subComps.ToArray()); }
        }

        public AppliedFunction(ExComp ex, FunctionType functionType, Type type)
        {
            if (ex is AlgebraFunction)
                _subComps.Add(ex);
            else
                base.AssignTo(ex.ToAlgTerm());

            _functionType = functionType;
            _type = type;
        }

        public override AlgebraTerm ApplyOrderOfOperations()
        {
            AlgebraTerm innerTerm = InnerTerm;
            innerTerm = innerTerm.ApplyOrderOfOperations();

            _subComps = new List<ExComp>();
            _subComps = innerTerm.SubComps;

            return this;
        }

        public override void AssignTo(AlgebraTerm algebraTerm)
        {
            if (algebraTerm.GetType() == this.GetType())
            {
                base.AssignTo(algebraTerm);
            }
            else
                throw new ArgumentException();
        }

        public override ExComp MakeWorkable()
        {
            return base.MakeWorkable();
        }

        public override ExComp Clone()
        {
            return CreateInstance(InnerTerm.Clone());
        }

        public override AlgebraTerm CompoundFractions()
        {
            AlgebraTerm compoundedFracs = InnerTerm.CompoundFractions();

            return CreateInstance(compoundedFracs);
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            AlgebraTerm compoundedFractions = InnerTerm.CompoundFractions(out valid);

            return CreateInstance(compoundedFractions);
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            AlgebraTerm converted = InnerTerm.ConvertImaginaryToVar();
            base.AssignTo(converted);
            return this;
        }

        public override List<FunctionType> GetAppliedFunctionsNoPow(AlgebraComp varFor)
        {
            List<FunctionType> appliedFuncs = new List<FunctionType>();
            if (this.Contains(varFor))
                appliedFuncs.Add(_functionType);
            return appliedFuncs;
        }

        public override double GetCompareVal()
        {
            return 0.5;
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            // The domain is all real numbers.
            return new List<Restriction>();
        }

        public override List<ExComp[]> GetGroups()
        {
            List<ExComp[]> groups = new List<ExComp[]>();
            ExComp[] onlyGroup = { this.Clone() };
            groups.Add(onlyGroup);
            return groups;
        }

        public override AlgebraTerm HarshEvaluation()
        {
            AlgebraTerm harshEval = InnerTerm.HarshEvaluation();

            return CreateInstance(harshEval);
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex.GetType() == this.GetType())
            {
                return this.InnerTerm.IsEqualTo((ex as AppliedFunction).InnerTerm);
            }
            else
                return false;
        }

        public override AlgebraTerm Order()
        {
            return CreateInstance(InnerTerm.Order());
        }

        public override AlgebraTerm RemoveOneCoeffs()
        {
            ExComp innerEx = InnerEx;
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).RemoveOneCoeffs();
            return CreateInstance(innerEx);
        }

        public override ExComp RemoveRedundancies(bool postWorkable = false)
        {
            ExComp nonRedundantInner = InnerTerm.RemoveRedundancies(postWorkable);
            return CreateInstance(nonRedundantInner);
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            AlgebraTerm term = InnerTerm.Substitute(subOut, subIn);
            return CreateInstance(term);
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            AlgebraTerm term = InnerTerm.Substitute(subOut, subIn, ref success);
            return CreateInstance(term);
        }

        public override bool TermsRelatable(ExComp comp)
        {
            return this.IsEqualTo(comp);
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return this;
        }

        protected virtual AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return (AlgebraTerm)Activator.CreateInstance(_type, args[0]);
        }


    }

    internal abstract class AppliedFunction_NArgs : AppliedFunction
    {
        protected ExComp[] _args;

        public AppliedFunction_NArgs(FunctionType functionType, Type type, params ExComp[] args)
            : base(args[0], functionType, type)
        {
            _args = args;
        }

        public override ExComp Clone()
        {
            var cloned = from arg in _args
                         select arg.Clone();
            return CreateInstance(cloned.ToArray());
        }

        public override AlgebraTerm CompoundFractions()
        {
            return this;
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            valid = false;

            return this;
        }

        public override AlgebraTerm HarshEvaluation()
        {
            var harshEval = from arg in _args
                            select arg.ToAlgTerm().HarshEvaluation();
            return CreateInstance(harshEval.ToArray());
        }

        public override AlgebraTerm Order()
        {
            var ordered = from arg in _args
                          select arg.ToAlgTerm().Order();
            return CreateInstance(ordered.ToArray());
        }

        public override AlgebraTerm RemoveOneCoeffs()
        {
            var noOneCoeffs = from arg in _args
                              select (arg is AlgebraTerm ? (arg as AlgebraTerm).RemoveOneCoeffs() : arg);
            return CreateInstance(noOneCoeffs.ToArray());
        }

        public override ExComp RemoveRedundancies(bool postWorkable = false)
        {
            var noRedun = from arg in _args
                          select arg.ToAlgTerm().RemoveRedundancies(postWorkable);
            return CreateInstance(noRedun.ToArray());
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            var substituted = from arg in _args
                              select arg.ToAlgTerm().Substitute(subOut, subIn);
            return CreateInstance(substituted.ToArray());
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            ExComp[] substituted = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
            {
                substituted[i] = _args[i].ToAlgTerm().Substitute(subOut, subIn, ref success);
            }

            return CreateInstance(substituted);
        }

        protected override AlgebraTerm CreateInstance(params ExComp[] args)
        {
            return (AlgebraTerm)Activator.CreateInstance(_type, args);
        }
    }

    internal abstract class BasicAppliedFunc : AppliedFunction
    {
        protected string _useEnd = ")";
        protected string _useStart = "(";
        protected string s_name;

        public virtual string FuncName
        {
            get { return s_name; }
        }

        public BasicAppliedFunc(ExComp innerEx, string name, FunctionType ft, Type type)
            : base(innerEx, ft, type)
        {
            s_name = name;
        }

        public static ExComp Parse(string parseStr, ExComp innerEx, ref List<string> pParseErrors)
        {
            if (parseStr == "sin")
                return new SinFunction(innerEx);
            else if (parseStr == "cos")
                return new CosFunction(innerEx);
            else if (parseStr == "tan")
                return new TanFunction(innerEx);
            else if (parseStr == "log")
                return new LogFunction(innerEx);   // By default we are log base 10.
            else if (parseStr == "ln")
            {
                LogFunction log = new LogFunction(innerEx);
                log.Base = Constant.ParseConstant("e");
                return log;
            }
            else if (parseStr == "sec")
                return new SecFunction(innerEx);
            else if (parseStr == "csc")
                return new CscFunction(innerEx);
            else if (parseStr == "cot")
                return new CotFunction(innerEx);
            else if (parseStr == "asin" || parseStr == "arcsin")
                return new ASinFunction(innerEx);
            else if (parseStr == "acos" || parseStr == "arccos")
                return new ACosFunction(innerEx);
            else if (parseStr == "atan" || parseStr == "arctan")
                return new ATanFunction(innerEx);
            else if (parseStr == "acsc" || parseStr == "arccsc")
                return new ACscFunction(innerEx);
            else if (parseStr == "asec" || parseStr == "arcsec")
                return new ASecFunction(innerEx);
            else if (parseStr == "acot" || parseStr == "arccot")
                return new ACotFunction(innerEx);
            else if (parseStr == "sqrt")
                return new AlgebraTerm(innerEx, new Operators.PowOp(), new AlgebraTerm(Number.One, new Operators.DivOp(), new Number(2.0)));
            else if (parseStr == "det")
            {
                var exMat = innerEx as Structural.LinearAlg.ExMatrix;
                if (exMat == null)
                {
                    pParseErrors.Add("Can only take the determinant of matrices.");
                    return null;
                }

                return new Structural.LinearAlg.Determinant(exMat);
            }
            else if (parseStr == "curl")
            {
                if (innerEx is AlgebraTerm)
                    innerEx = (innerEx as AlgebraTerm).RemoveRedundancies();
                if (!CurlFunc.IsSuitableField(innerEx))
                    return null;
                
                return new CurlFunc(innerEx);
            }
            else if (parseStr == "div")
            {
                if (innerEx is AlgebraTerm)
                    innerEx = (innerEx as AlgebraTerm).RemoveRedundancies();
                if (!DivergenceFunc.IsSuitableField(innerEx))
                    return null;

                return new DivergenceFunc(innerEx);
            }
            else if (parseStr == "!")
            {
                return new FactorialFunction(innerEx);
            }

            return null;
        }

        public override string FinalToDispStr()
        {
            return s_name + _useStart + InnerTerm.FinalToDispStr() + _useEnd;
        }

        public override string ToAsciiString()
        {
            return s_name + _useStart + InnerTerm.ToAsciiString() + _useEnd;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string innerStr = InnerTerm.ToJavaScriptString(useRad);
            if (InnerTerm == null)
                return null;
            return "Math." + s_name + "(" + innerStr + ")";
        }

        public override string ToString()
        {
            if (MathSolver.USE_TEX_DEBUG)
                return ToTexString();
            return s_name + _useStart + InnerTerm.ToString() + _useEnd;
        }

        public override string ToTexString()
        {
            return s_name + _useStart + InnerTerm.ToTexString() + _useEnd;
        }
    }
}