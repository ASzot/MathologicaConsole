using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal abstract class AppliedFunction : AlgebraFunction
    {
        protected FunctionType _functionType;
        protected Type _type;

        public FunctionType GetFunctionType()
        {
            return _functionType;
        }

        public ExComp GetInnerEx()
        {
            return GetInnerTerm().RemoveRedundancies(false);
        }

        public AlgebraTerm GetInnerTerm()
        {
            return new AlgebraTerm(_subComps.ToArray());
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
            AlgebraTerm innerTerm = GetInnerTerm();
            innerTerm = innerTerm.ApplyOrderOfOperations();

            _subComps = new List<ExComp>();
            _subComps = innerTerm.GetSubComps();

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

        public override ExComp CloneEx()
        {
            return CreateInstance(GetInnerTerm().CloneEx());
        }

        protected void CallChildren(bool harshEval, ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < ArrayFunc.GetCount(_subComps); ++i)
            {
                if (ArrayFunc.GetAt(_subComps, i) is AlgebraFunction)
                {
                    ExComp evaluated = ((AlgebraFunction) ArrayFunc.GetAt(_subComps, i)).Evaluate(harshEval,
                        ref pEvalData);
                    ArrayFunc.SetAt(_subComps, i, evaluated);
                }
            }
        }

        public override AlgebraTerm CompoundFractions()
        {
            AlgebraTerm compoundedFracs = GetInnerTerm().CompoundFractions();

            return CreateInstance(compoundedFracs);
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            AlgebraTerm compoundedFractions = GetInnerTerm().CompoundFractions(out valid);

            return CreateInstance(compoundedFractions);
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            AlgebraTerm converted = GetInnerTerm().ConvertImaginaryToVar();
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
            ExComp[] onlyGroup = new ExComp[] { this.CloneEx() };
            groups.Add(onlyGroup);
            return groups;
        }

        public override AlgebraTerm HarshEvaluation()
        {
            AlgebraTerm harshEval = GetInnerTerm().HarshEvaluation();

            AlgebraTerm created = CreateInstance(harshEval);
            return created;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex.GetType() == this.GetType())
            {
                return this.GetInnerTerm().IsEqualTo((ex as AppliedFunction).GetInnerTerm());
            }
            else
                return false;
        }

        public override AlgebraTerm Order()
        {
            return CreateInstance(GetInnerTerm().Order());
        }

        public override AlgebraTerm RemoveOneCoeffs()
        {
            ExComp innerEx = GetInnerEx();
            if (innerEx is AlgebraTerm)
                innerEx = (innerEx as AlgebraTerm).RemoveOneCoeffs();
            return CreateInstance(innerEx);
        }

        public override ExComp RemoveRedundancies(bool postWorkable)
        {
            ExComp nonRedundantInner = GetInnerTerm().RemoveRedundancies(postWorkable);
            return CreateInstance(nonRedundantInner);
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            AlgebraTerm term = GetInnerTerm().Substitute(subOut, subIn);
            return CreateInstance(term);
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            AlgebraTerm term = GetInnerTerm().Substitute(subOut, subIn, ref success);
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
            return (AlgebraTerm)TypeHelper.CreateInstance(_type, args[0]);
        }
    }

}