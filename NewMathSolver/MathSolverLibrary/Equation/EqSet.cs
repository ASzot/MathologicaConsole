using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal struct EqSet
    {
        private Type _startingType;
        private List<LexemeType> _comparisonOps;
        private List<ExComp> _sides;
        private string _strContent;

        public void SetComparisonOp(LexemeType value)
        {
            if (_comparisonOps.Count < 1)
                _comparisonOps.Add(value);
            else
                _comparisonOps[0] = value;
        }

        public LexemeType GetComparisonOp()
        {
            return _comparisonOps[0];
        }

        public void SetStartingType(Type value)
        {
            _startingType = value;
        }

        public Type GetStartingType()
        {
            return _startingType;
        }

        public List<LexemeType> GetComparisonOps()
        {
            return _comparisonOps;
        }

        /// <summary>
        /// All of the comparison ops not including the invalid 'ErrorType'.
        /// </summary>
        public List<LexemeType> GetValidComparisonOps()
        {
            List<LexemeType> lexs = new List<LexemeType>();
            for (int i = 0; i < _comparisonOps.Count; ++i)
            {
                if (_comparisonOps[i] != LexemeType.ErrorType)
                    lexs.Add(_comparisonOps[i]);
            }

            return lexs;
        }

        public string GetContentStr()
        {
            return _strContent;
        }

        public bool GetIsSingular()
        {
            return GetRight() == null;
        }

        public void SetLeft(ExComp value)
        {
            if (_sides.Count < 1)
                _sides.Add(value);
            else
                _sides[0] = value;
        }

        public ExComp GetLeft()
        {
            if (_sides.Count < 1)
                return null;
            else
                return _sides[0];
        }

        public AlgebraTerm GetLeftTerm()
        {
            return GetLeft().ToAlgTerm();
        }

        public void SetRight(ExComp value)
        {
            if (_sides.Count < 2)
            {
                if (_sides.Count == 1)
                    _sides.Add(value);
                else
                {
                    _sides.Add(null);
                    _sides.Add(value);
                }
            }
            else
                _sides[1] = value;
        }

        public ExComp GetRight()
        {
            if (_sides.Count < 2)
                return null;
            else
                return _sides[1];
        }

        public AlgebraTerm GetRightTerm()
        {
            return GetRight().ToAlgTerm();
        }

        public List<ExComp> GetSides()
        {
            return _sides;
        }

        public EqSet(ExComp singleEx, string strContent)
        {
            _sides = new List<ExComp>();
            _comparisonOps = new List<LexemeType>();
            _strContent = strContent;
            _startingType = null;
            SetComparisonOp(LexemeType.ErrorType);
            SetLeft(singleEx);
            SetRight(null);
        }

        public EqSet(List<ExComp> sides, List<LexemeType> comparionOps)
        {
            _sides = sides;
            _comparisonOps = comparionOps;
            _strContent = null;
            _startingType = null;
        }

        public EqSet(ExComp left, ExComp right, LexemeType comparisonOp)
        {
            _sides = new List<ExComp>();
            _comparisonOps = new List<LexemeType>();
            _strContent = null;
            _startingType = null;
            SetComparisonOp(comparisonOp);
            SetLeft(left);
            SetRight(right);
        }

        public EqSet(ExComp singleEx)
        {
            _sides = new List<ExComp>();
            _comparisonOps = new List<LexemeType>();
            _strContent = null;
            _startingType = null;
            SetComparisonOp(LexemeType.ErrorType);
            SetLeft(singleEx);
            SetRight(null);
        }

        public static IEnumerable<AlgebraTerm> GetSides(List<EqSet> eqSets)
        {
            List<AlgebraTerm> sides = new List<AlgebraTerm>();
            foreach (EqSet eqSet in eqSets)
            {
                sides.Add(eqSet.GetLeftTerm());
                sides.Add(eqSet.GetRightTerm());
            }

            return sides;
        }

        public EqSet Clone()
        {
            List<ExComp> clonedSides = new List<ExComp>();
            foreach (ExComp side in _sides)
            {
                clonedSides.Add(side.CloneEx());
            }

            return new EqSet(clonedSides, _comparisonOps);
        }

        public ExComp[] GetFuncDefComps()
        {
            if ((GetLeft() is FunctionDefinition && !(GetRight() is FunctionDefinition)) ||
                GetLeft() is AlgebraComp)
                return new ExComp[] { GetLeft(), GetRight() };

            return null;
        }

        public bool ContainsVar(AlgebraComp varFor)
        {
            foreach (ExComp side in _sides)
            {
                if (side == null)
                    continue;
                if (side.ToAlgTerm().Contains(varFor))
                    return true;
            }

            return false;
        }

        public string FinalToDispStr()
        {
            if (_sides.Count - 1 != _comparisonOps.Count)
                return null;

            string finalStr = "";
            for (int i = 0; i < _sides.Count; ++i)
            {
                if (_sides[i] == null)
                    continue;

                if (i != 0 && _comparisonOps[i - 1] != LexemeType.ErrorType)
                {
                    finalStr += Restriction.ComparisonOpToStr(_comparisonOps[i - 1]);
                }

                finalStr += WorkMgr.ToDisp(_sides[i]);
            }

            return finalStr;
        }

        /// <summary>
        /// Calls all functions.
        /// False will be returned if the function is in the
        /// form f(2) where f is not defined.
        /// </summary>
        /// <param name="pEvalData"></param>
        /// <returns></returns>
        public bool FixEqFuncDefs(ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _sides.Count; ++i)
            {
                if (_sides[i] is AlgebraTerm)
                {
                    if (!(_sides[i] as AlgebraTerm).CallFunctions(ref pEvalData))
                        return false;
                    (_sides[i] as AlgebraTerm).FuncDefsToAlgVars();
                }
                else if (_sides[i] is FunctionDefinition)
                {
                    //continue;
                    // To allow for assignments this needs to be commented out.
                    FunctionDefinition funcDef = _sides[i] as FunctionDefinition;
                    bool allEqual = true;
                    if ((funcDef.GetCallArgs() != null && funcDef.GetInputArgs() != null &&
                        funcDef.GetCallArgs().Length == funcDef.GetInputArgs().Length))
                    {
                        for (int j = 0; j < funcDef.GetCallArgs().Length; ++j)
                        {
                            if (!funcDef.GetCallArgs()[j].IsEqualTo(funcDef.GetInputArgs()[j]))
                            {
                                allEqual = false;
                                break;
                            }
                        }
                    }

                    if (allEqual && !(_sides.Count == 1 || (_sides.Count == 2 && _sides[1] == null)))
                    {
                        continue;
                    }

                    _sides[i] = (_sides[i] as FunctionDefinition).CallFunc(ref pEvalData);
                    if (_sides[i] == null)
                        return false;
                }
            }

            return true;
        }

        public void CallFunction(FunctionDefinition funcDef, ExComp def, ref TermType.EvalData pEvalData)
        {
            for (int i = 0; i < _sides.Count; ++i)
            {
                if (_sides[i] == null)
                    continue;

                AlgebraTerm term = new AlgebraTerm(_sides[i]);
                term.CallFunction(funcDef, def, ref pEvalData, false);
                _sides[i] = term;
            }
        }

        public void GetAdditionVarFors(ref Dictionary<string, int> varFors)
        {
            foreach (ExComp side in _sides)
            {
                if (side == null)
                    continue;
                AlgebraTerm sideTerm = side.ToAlgTerm();
                AdvAlgebraTerm.GetAdditionalVarFors(sideTerm, ref varFors);
            }
        }

        public List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> domainRests = new List<Restriction>();
            foreach (ExComp side in _sides)
            {
                if (side == null)
                    continue;
                AdvAlgebraTerm.GetDomain(side.CloneEx().ToAlgTerm(), agSolver, varFor, ref domainRests, ref pEvalData);
                if (domainRests == null)
                    return null;
            }

            List<Restriction> compoundedRests = Restriction.CompoundRestrictions(domainRests, ref pEvalData);
            return compoundedRests;
        }

        public SolveResult ImplicitDifferentiation(string derivativeOfStr, string withRespectToStr, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            if (GetLeft() == null || GetRight() == null)
            {
                pEvalData.AddFailureMsg("Internal error.");
                return SolveResult.Failure();
            }

            pEvalData.AttemptSetInputType(TermType.InputType.DerivImp);

            AlgebraComp withRespectTo = new AlgebraComp(withRespectToStr);
            AlgebraComp derivOf = new AlgebraComp(derivativeOfStr);

            Derivative derivLeft = Derivative.ConstructDeriv(GetLeft(), withRespectTo, derivOf);
            Derivative derivRight = Derivative.ConstructDeriv(GetRight(), withRespectTo, derivOf);

            pEvalData.GetWorkMgr().FromSides(derivLeft, derivRight, "Take the implicit derivative of each side.");
            pEvalData.GetWorkMgr().FromFormatted("`{0}`", "First take the derivative of the left side.", GetLeft());
            ExComp left = derivLeft.Evaluate(false, ref pEvalData);
            pEvalData.GetWorkMgr().FromFormatted("`{0}`", "Now take the derivative of the right side.", GetRight());
            ExComp right = derivRight.Evaluate(false, ref pEvalData);

            AlgebraComp solveFor = derivLeft.ConstructImplicitDerivAgCmp();

            return agSolver.SolveEquationEquality(solveFor.GetVar(), left.ToAlgTerm(), right.ToAlgTerm(), ref pEvalData);
        }

        public List<TypePair<LexemeType, string>> CreateLexemeTable()
        {
            List<TypePair<LexemeType, string>> lt = new List<TypePair<LexemeType, string>>();
            for (int i = 0; i < _sides.Count; ++i)
            {
                List<string> variables = _sides[i].ToAlgTerm().GetAllAlgebraCompsStr();
                foreach (string variable in variables)
                {
                    lt.Add(new TypePair<LexemeType, string>(LexemeType.Identifier, variable));
                }
                if (i + 1 < _sides.Count)
                    lt.Add(new TypePair<LexemeType, string>(_comparisonOps[i], Restriction.ComparisonOpToStr(_comparisonOps[i])));
            }

            return lt;
        }

        public bool ReparseInfo(out EqSet eqSet, ref TermType.EvalData pEvalData)
        {
            eqSet = new EqSet();

            List<List<TypePair<LexemeType, string>>> lts;
            List<string> garbageParseErrors = new List<string>();

            LexicalParser lexParser = new LexicalParser(pEvalData);
            List<EqSet> eqs = lexParser.ParseInput(_strContent, out lts, ref garbageParseErrors);

            if (eqs == null)
                return false;

            if (eqs.Count == 1)
            {
                _sides = eqs[0]._sides;
                _comparisonOps = eqs[0]._comparisonOps;
                _strContent = eqs[0]._strContent;

                eqSet = this;

                return true;
            }

            return false;
        }

        public void Substitute(ExComp subOut, ExComp subIn)
        {
            for (int i = 0; i < _sides.Count; ++i)
            {
                if (_sides[i] == null)
                    continue;
                AlgebraTerm subbed = _sides[i].ToAlgTerm().Substitute(subOut, subIn);
                subbed = subbed.ApplyOrderOfOperations();
                _sides[i] = subbed.MakeWorkable();
            }
        }

        public bool IsLinearAlgebraTerm()
        {
            ExComp leftEx = GetLeft();
            ExComp rightEx = GetRight();

            return leftEx is ExMatrix || rightEx is ExMatrix ||
                MatrixHelper.TermContainsMatrices(leftEx) ||
                MatrixHelper.TermContainsMatrices(leftEx);
        }
    }
}