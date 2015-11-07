using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{

    internal abstract class AppliedFunction_NArgs : AppliedFunction
    {
        protected ExComp[] _args;

        public AppliedFunction_NArgs(FunctionType functionType, Type type, params ExComp[] args)
            : base(args[0], functionType, type)
        {
            _args = args;
        }

        public override ExComp CloneEx()
        {
            ExComp[] cloned = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
                cloned[i] = _args[i].CloneEx();
            return CreateInstance(cloned);
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
            ExComp[] harshEval = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
                harshEval[i] = _args[i].ToAlgTerm().HarshEvaluation();
            AlgebraTerm created = CreateInstance(harshEval);
            return created;
        }

        public override AlgebraTerm Order()
        {
            ExComp[] orderedArr = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
                orderedArr[i] = _args[i].ToAlgTerm().Order();
            return CreateInstance(orderedArr);
        }

        public override AlgebraTerm RemoveOneCoeffs()
        {
            ExComp[] noOneCoeffsArr = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
                noOneCoeffsArr[i] = (_args[i] is AlgebraTerm ? (_args[i] as AlgebraTerm).RemoveOneCoeffs() : _args[i]);
            return CreateInstance(noOneCoeffsArr);
        }

        public override ExComp RemoveRedundancies(bool postWorkable)
        {
            ExComp[] noRedunArr = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
                noRedunArr[i] = _args[i].ToAlgTerm().RemoveRedundancies(postWorkable);
            return CreateInstance(noRedunArr);
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            ExComp[] substitutedArr = new ExComp[_args.Length];
            for (int i = 0; i < _args.Length; ++i)
                substitutedArr[i] = _args[i].ToAlgTerm().Substitute(subOut, subIn);
            return CreateInstance(substitutedArr);
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
            return (AlgebraTerm)TypeHelper.CreateInstance(_type, args);
        }
    }
}