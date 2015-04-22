using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using LexemeTable = System.Collections.Generic.List<
MathSolverWebsite.MathSolverLibrary.TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>>;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal struct EqSet
    {
        private List<LexemeType> _comparisonOps;
        private List<ExComp> _sides;
        private string _strContent;

        public LexemeType ComparisonOp
        {
            get { return _comparisonOps[0]; }
            set
            {
                if (_comparisonOps.Count < 1)
                    _comparisonOps.Add(value);
                else
                    _comparisonOps[0] = value;
            }
        }

        public List<LexemeType> ComparisonOps
        {
            get { return _comparisonOps; }
        }

        public string ContentStr
        {
            get { return _strContent; }
        }

        public bool IsSingular
        {
            get { return Right == null; }
        }

        public ExComp Left
        {
            get
            {
                if (_sides.Count < 1)
                    return null;
                else
                    return _sides[0];
            }
            set
            {
                if (_sides.Count < 1)
                    _sides.Add(value);
                else
                    _sides[0] = value;
            }
        }

        public AlgebraTerm LeftTerm
        {
            get { return Left.ToAlgTerm(); }
        }

        public ExComp Right
        {
            get
            {
                if (_sides.Count < 2)
                    return null;
                else
                    return _sides[1];
            }
            set
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
        }

        public AlgebraTerm RightTerm
        {
            get { return Right.ToAlgTerm(); }
        }

        public List<ExComp> Sides
        {
            get { return _sides; }
        }

        public EqSet(ExComp singleEx, string strContent)
        {
            _sides = new List<ExComp>();
            _comparisonOps = new List<LexemeType>();
            _strContent = strContent;
            ComparisonOp = LexemeType.ErrorType;
            Left = singleEx;
            Right = null;
        }

        public EqSet(List<ExComp> sides, List<LexemeType> comparionOps)
        {
            _sides = sides;
            _comparisonOps = comparionOps;
            _strContent = null;
        }

        public EqSet(ExComp left, ExComp right, LexemeType comparisonOp)
        {
            _sides = new List<ExComp>();
            _comparisonOps = new List<LexemeType>();
            _strContent = null;
            ComparisonOp = comparisonOp;
            Left = left;
            Right = right;
        }

        public EqSet(ExComp singleEx)
        {
            _sides = new List<ExComp>();
            _comparisonOps = new List<LexemeType>();
            _strContent = null;
            ComparisonOp = LexemeType.ErrorType;
            Left = singleEx;
            Right = null;
        }

        public static IEnumerable<AlgebraTerm> GetSides(List<EqSet> eqSets)
        {
            foreach (EqSet eqSet in eqSets)
            {
                yield return eqSet.LeftTerm;
                yield return eqSet.RightTerm;
            }
        }

        public EqSet Clone()
        {
            List<ExComp> clonedSides = new List<ExComp>();
            foreach (ExComp side in _sides)
            {
                clonedSides.Add(side.Clone());
            }

            return new EqSet(clonedSides, _comparisonOps);
        }

        public ExComp[] GetFuncDefComps()
        {
            if ((Left is FunctionDefinition && !(Right is FunctionDefinition)) ||
                Left is AlgebraComp)
                return new ExComp[] { Left, Right };

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

                finalStr += WorkMgr.ExFinalToAsciiStr(_sides[i]);
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
                    if ((funcDef.CallArgs != null && funcDef.InputArgs != null &&
                        funcDef.CallArgs.Length == funcDef.InputArgs.Length))
                    {
                        for (int j = 0; j < funcDef.CallArgs.Length; ++j)
                        {
                            if (!funcDef.CallArgs[j].IsEqualTo(funcDef.InputArgs[j]))
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

        public void GetAdditionVarFors(ref Dictionary<string, int> varFors)
        {
            foreach (ExComp side in _sides)
            {
                if (side == null)
                    continue;
                AlgebraTerm sideTerm = side.ToAlgTerm();
                sideTerm.GetAdditionalVarFors(ref varFors);
            }
        }

        public List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> domainRests = new List<Restriction>();
            foreach (ExComp side in _sides)
            {
                if (side == null)
                    continue;
                side.Clone().ToAlgTerm().GetDomain(agSolver, varFor, ref domainRests, ref pEvalData);
                if (domainRests == null)
                    return null;
            }

            return Restriction.CompoundRestrictions(domainRests, ref pEvalData);
        }

        public SolveResult ImplicitDifferentiation(string derivativeOfStr, string withRespectToStr, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            if (Left == null || Right == null)
            {
                pEvalData.AddFailureMsg("Internal error.");
                return SolveResult.Failure();
            }

            pEvalData.AttemptSetInputType(TermType.InputType.DerivImp);

            AlgebraComp withRespectTo = new AlgebraComp(withRespectToStr);
            AlgebraComp derivOf = new AlgebraComp(derivativeOfStr);

            var derivLeft = Equation.Functions.Calculus.Derivative.ConstructDeriv(Left, withRespectTo, derivOf);
            var derivRight = Equation.Functions.Calculus.Derivative.ConstructDeriv(Right, withRespectTo, derivOf);

            pEvalData.WorkMgr.FromSides(derivLeft, derivRight, "Take the implicit derivative of each side.");
            pEvalData.WorkMgr.FromFormatted("`{0}`", "First take the derivative of the left side.", Left);
            ExComp left = derivLeft.Evaluate(false, ref pEvalData);
            pEvalData.WorkMgr.FromFormatted("`{0}`", "Now take the derivative of the right side.", Right);
            ExComp right = derivRight.Evaluate(false, ref pEvalData);

            AlgebraComp solveFor = derivLeft.ConstructImplicitDerivAgCmp();

            return agSolver.SolveEquationEquality(solveFor.Var, left.ToAlgTerm(), right.ToAlgTerm(), ref pEvalData);
        }

        public bool ReparseInfo(out EqSet eqSet, ref TermType.EvalData pEvalData)
        {
            eqSet = new EqSet();

            List<LexemeTable> lts;
            var garbageParseErrors = new List<string>();

            LexicalParser lexParser = new LexicalParser(pEvalData);
            var eqs = lexParser.ParseInput(_strContent, out lts, ref garbageParseErrors);

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

        public void SetLeft(ExComp left)
        {
            Left = left;
        }

        public void SetRight(ExComp right)
        {
            Right = right;
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
            ExComp leftEx = Left;
            ExComp rightEx = Right;

            return leftEx is ExMatrix || rightEx is ExMatrix ||
                MatrixHelper.TermContainsMatrices(leftEx) ||
                MatrixHelper.TermContainsMatrices(leftEx);
        }
    }
}