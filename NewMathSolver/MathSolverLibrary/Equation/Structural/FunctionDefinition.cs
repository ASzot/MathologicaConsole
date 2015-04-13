using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System.Text.RegularExpressions;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class FunctionDefinition : ExComp
    {
        private AlgebraComp[] _args;
        private ExComp[] _callArgs;
        private AlgebraComp _iden;

        public ExComp[] CallArgs
        {
            set { _callArgs = value; }
            get { return _callArgs; }
        }

        public bool HasCallArgs
        {
            get { return _callArgs != null; }
        }

        public AlgebraComp Iden
        {
            get { return _iden; }
        }

        public int InputArgCount
        {
            get { return _args.Length; }
        }

        public AlgebraComp[] InputArgs
        {
            get { return _args; }
        }

        public FunctionDefinition()
        {
        }

        public FunctionDefinition(AlgebraComp iden, AlgebraComp[] args, ExComp[] callArgs)
        {
            _iden = iden;
            _args = args;
            _callArgs = callArgs;
        }

        public ExComp CallFunc(ref TermType.EvalData pEvalData)
        {
            var def = pEvalData.FuncDefs.GetDefinition(this);
            if (def.Value == null)
            {
                return this;
            }

            ExComp thisDef = def.Value;
            if (_callArgs == null && !def.Key.IsEqualTo(this))
            {
                _callArgs = _args;
                _args = def.Key._args;
            }

            if (_callArgs == null)
                return thisDef;

            AlgebraTerm thisDefTerm = thisDef.ToAlgTerm();

            if (_callArgs.Length != _args.Length)
                return null;

            for (int i = 0; i < _args.Length; ++i)
            {
                AlgebraComp varParameter = _args[i];
                AlgebraTerm input = _callArgs[i].ToAlgTerm();
                // The input for some reason isn't weak workable most of the time.
                ExComp inputEx = input.WeakMakeWorkable();
                if (inputEx == null)
                    return null;
                if (inputEx is AlgebraTerm)
                {
                    if (!(inputEx as AlgebraTerm).CallFunctions(ref pEvalData))
                        return null;
                }
                else if (inputEx is FunctionDefinition)
                    inputEx = (inputEx as FunctionDefinition).CallFunc(ref pEvalData);

                thisDefTerm = thisDefTerm.Substitute(varParameter, inputEx);
            }

            thisDefTerm.EvaluateFunctions(false, ref pEvalData);
            thisDefTerm = thisDefTerm.EvaluatePowers(ref pEvalData);
            thisDefTerm = thisDefTerm.ApplyOrderOfOperations();
            ExComp workable = thisDefTerm.MakeWorkable();
            if (workable == null)
                return null;
            return workable;
        }

        public override ExComp Clone()
        {
            return new FunctionDefinition(_iden, _args, _callArgs);
        }

        public override double GetCompareVal()
        {
            return 1.0;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is FunctionDefinition)
            {
                FunctionDefinition funcDef = ex as FunctionDefinition;
                if (!funcDef._iden.IsEqualTo(_iden))
                    return false;

                if (funcDef._args.Length != _args.Length)
                    return false;

                for (int i = 0; i < funcDef._args.Length; ++i)
                {
                    if (!funcDef._args[i].IsEqualTo(_args[i]))
                        return false;
                }

                return true;
            }

            return false;
        }

        public bool Parse(string parseStr)
        {
            int startParaIndex = parseStr.IndexOf('(');

            string idenStr = parseStr.Substring(0, startParaIndex);

            _iden = AlgebraComp.Parse(idenStr);

            string argStr = parseStr.Substring(startParaIndex + 1, parseStr.Length - (startParaIndex + 2));

            string[] args = argStr.Split(',');

            _args = new AlgebraComp[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                if (!Regex.IsMatch(args[i], MathSolverLibrary.Parsing.LexicalParser.IDEN_MATCH))
                    return false;

                _args[i] = AlgebraComp.Parse(args[i]);
            }

            return true;
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return new AlgebraTerm(this);
        }

        public override string ToAsciiString()
        {
            string funcStr = _iden.ToAsciiString() + "(";
            for (int i = 0; i < _args.Length; ++i)
            {
                if (_callArgs != null && _callArgs.Length > i)
                    funcStr += _callArgs[i].ToAsciiString();
                else
                    funcStr += _args[i].ToAsciiString();
                if (i != _args.Length - 1)
                    funcStr += ",";
            }

            funcStr += ")";

            return funcStr;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            string funcStr = _iden.ToString() + "(";
            for (int i = 0; i < _args.Length; ++i)
            {
                if (_callArgs != null && _callArgs.Length > i)
                    funcStr += _callArgs[i].ToString();
                else
                    funcStr += _args[i].ToString();
                if (i != _args.Length - 1)
                    funcStr += ",";
            }

            funcStr += ")";

            return funcStr;
        }

        public override string ToTexString()
        {
            string funcStr = _iden.ToTexString() + "(";
            for (int i = 0; i < _args.Length; ++i)
            {
                if (_callArgs != null && _callArgs.Length > i)
                    funcStr += _callArgs[i].ToTexString();
                else
                    funcStr += _args[i].ToTexString();
                if (i != _args.Length - 1)
                    funcStr += ",";
            }

            funcStr += ")";

            return funcStr;
        }
    }
}