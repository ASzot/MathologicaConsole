using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class FunctionDefinition : ExComp
    {
        private AlgebraComp[] _args;
        private ExComp[] _callArgs;
        private AlgebraComp _iden;
        private bool _funcNotation = true;
        private int _funcDefIndex = -1;
        private bool _isVectorFunc = false;

        /// <summary>
        /// The supplied arguments to the function.
        /// They are not defined they are given.
        /// Example is f(2) but not f(x)
        /// </summary>
        public void SetCallArgs(ExComp[] value)
        {
            _callArgs = value;
        }

        /// <summary>
        /// The supplied arguments to the function.
        /// They are not defined they are given.
        /// Example is f(2) but not f(x)
        /// </summary>
        public ExComp[] GetCallArgs()
        {
            return _callArgs;
        }

        public void SetFuncDefIndex(int value)
        {
            _funcDefIndex = value;
        }

        public int GetFuncDefIndex()
        {
            return _funcDefIndex;
        }

        public bool GetHasValidInputArgs()
        {
            if (_args == null)
                return false;
            foreach (AlgebraComp arg in _args)
            {
                if (arg.GetIsTrash())
                    return false;
            }
            return true;
        }

        public bool GetHasCallArgs()
        {
            return _callArgs != null;
        }

        public bool GetFuncNotation()
        {
            return _funcNotation;
        }

        public AlgebraComp GetIden()
        {
            return _iden;
        }

        public int GetInputArgCount()
        {
            return _args == null ? 0 : _args.Length;
        }

        public bool GetIsMultiValued()
        {
            return _args.Length != 1;
        }

        public void SetIsVectorFunc(bool value)
        {
            _isVectorFunc = value;
        }

        /// <summary>
        /// The defined arguments to the function.
        /// These are not supplied.
        /// Example is f(x) but not f(2)
        /// </summary>
        public AlgebraComp[] GetInputArgs()
        {
            return _args;
        }

        public FunctionDefinition()
        {
        }

        public static string GetDimenStr(int dimen)
        {
            if (dimen == 0)
                return "x";
            else if (dimen == 1)
                return "y";
            else if (dimen == 2)
                return "z";
            else if (dimen == 3)
                return "w";
            return "w_" + (dimen + 1).ToString();
        }

        ///  <summary>
        /// 
        ///  </summary>
        /// <param name="iden">The identifier of the function.</param>
        /// <param name="args">The definition of the arguments. Null is not acceptable.</param>
        /// <param name="callArgs">What the user is inputting. These are not defined. Null is acceptable.</param>
        /// <param name="funcNotation">True means the function will output like y(x). False means the function will output just y.</param>
        public FunctionDefinition(AlgebraComp iden, AlgebraComp[] args, ExComp[] callArgs, bool funcNotation)
        {
            _iden = iden;
            _args = args;
            _callArgs = callArgs;
            _funcNotation = funcNotation;
        }

        public ExComp CallFunc(KeyValuePair<FunctionDefinition, ExComp> def, ref EvalData pEvalData, bool callSubFuncs)
        {
            if (def.Value == null)
            {
                if (GetCallArgs() != null && GetInputArgs() == null)
                    return null;
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

            if (_callArgs.Length != def.Key._args.Length)
                return null;

            for (int i = 0; i < _args.Length; ++i)
            {
                AlgebraComp varParameter = def.Key._args[i];
                AlgebraTerm input = _callArgs[i].ToAlgTerm();
                // The input for some reason isn't weak workable most of the time.
                ExComp inputEx = input.WeakMakeWorkable(ref pEvalData);
                if (inputEx == null)
                    return null;
                if (callSubFuncs)
                {
                    if (inputEx is AlgebraTerm)
                    {
                        if (!(inputEx as AlgebraTerm).CallFunctions(ref pEvalData))
                            return null;
                    }
                    else if (inputEx is FunctionDefinition)
                        inputEx = (inputEx as FunctionDefinition).CallFunc(ref pEvalData);
                }

                thisDefTerm = thisDefTerm.Substitute(varParameter, inputEx);
            }

            thisDefTerm.EvaluateFunctions(false, ref pEvalData);
            thisDefTerm = AdvAlgebraTerm.EvaluatePowers(thisDefTerm, ref pEvalData);
            thisDefTerm = thisDefTerm.ApplyOrderOfOperations();
            ExComp workable = thisDefTerm.MakeWorkable();
            if (workable == null)
                return null;
            return workable;
        }

        public ExComp CallFunc(ref TermType.EvalData pEvalData)
        {
            KeyValuePair<FunctionDefinition, ExComp> def = pEvalData.GetFuncDefs().GetDefinition(this);
            return CallFunc(def, ref pEvalData, true);
        }

        public override ExComp CloneEx()
        {
            return new FunctionDefinition(_iden, _args, _callArgs, _funcNotation);
        }

        public bool IsArg(string argIden)
        {
            foreach (AlgebraComp arg in _args)
            {
                if (arg.GetVar().GetVar() == argIden)
                    return true;
            }

            return false;
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

                if (funcDef._args == null || _args == null)
                {
                    return funcDef._args == null && _args == null;
                }

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
            if (!_funcNotation || (_args == null || _args.Length == 0))
                return _iden.ToAsciiString();

            string funcStr = "";
            if (_isVectorFunc)
                funcStr += "\\vec{";
            funcStr += _iden.ToAsciiString();
            if (_isVectorFunc)
                funcStr += "}";
            funcStr += "(";

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
            if (!_funcNotation || (_args == null || _args.Length == 0))
                return _iden.ToString();
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
            if (!_funcNotation || (_args == null || _args.Length == 0))
                return _iden.ToTexString();

            string funcStr = "";
            if (_isVectorFunc)
                funcStr += "\\vec{";
            funcStr += _iden.ToAsciiString();
            if (_isVectorFunc)
                funcStr += "}";
            funcStr += "(";

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