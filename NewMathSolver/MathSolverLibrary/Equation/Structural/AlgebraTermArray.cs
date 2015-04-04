using System;
using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class AlgebraTermArray : ExComp
    {
        private string[] _solveDescs = null;
        private List<AlgebraTerm> _terms = new List<AlgebraTerm>();

        public string[] SolveDescs
        {
            set { _solveDescs = value; }
        }

        public int TermCount
        {
            get { return _terms.Count; }
        }

        public List<AlgebraTerm> Terms
        {
            get { return _terms; }
        }

        public ExComp this[int i]
        {
            get
            {
                return _terms[i];
            }
        }

        public AlgebraTermArray(params AlgebraTerm[] terms)
        {
            _terms.AddRange(terms);
        }

        public AlgebraTermArray(List<ExComp> exTerms)
        {
            foreach (ExComp exTerm in exTerms)
            {
                _terms.Add(exTerm.ToAlgTerm());
            }
        }

        public AlgebraTermArray()
        {
        }

        public void Add(ExComp comp)
        {
            if (comp is AlgebraTermArray)
                _terms.AddRange((comp as AlgebraTermArray).Terms);
            else
                _terms.Add(comp.ToAlgTerm());
        }

        public override ExComp Clone()
        {
            List<AlgebraTerm> clonedTerms = new List<AlgebraTerm>();
            foreach (AlgebraTerm term in _terms)
                clonedTerms.Add(term.Clone() as AlgebraTerm);
            return new AlgebraTerm(clonedTerms.ToArray());
        }

        public override double GetCompareVal()
        {
            return _terms[0].GetCompareVal();
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is AlgebraTermArray)
            {
                AlgebraTermArray exTermArray = ex as AlgebraTermArray;
                if (this.TermCount != exTermArray.TermCount)
                    return false;
                for (int i = 0; i < exTermArray.Terms.Count; ++i)
                {
                    if (!exTermArray.Terms[i].IsEqualTo(this.Terms[i]))
                        return false;
                }

                return true;
            }
            return false;
        }

        public void RemoveComplexSolutions()
        {
            for (int i = 0; i < _terms.Count; ++i)
            {
                if (_terms[i].IsComplex())
                    _terms.RemoveAt(i--);
            }
        }

        public void RemoveDuplicateTerms()
        {
            for (int i = 0; i < _terms.Count; ++i)
            {
                ExComp term = _terms[i];

                for (int j = 0; j < _terms.Count; ++j)
                {
                    if (j == i)
                        continue;

                    ExComp compareTerm = _terms[j];

                    if (term.IsEqualTo(compareTerm))
                    {
                        _terms.RemoveAt(i--);
                        break;
                    }
                }
            }
        }

        public AlgebraTermArray SimulSolve(AlgebraTerm otherSide, AlgebraVar solveFor, AlgebraSolver solver, ref TermType.EvalData pEvalData, out bool allSols, bool switchSides = false)
        {
            allSols = false;

            List<AlgebraTerm> solvedTerms = new List<AlgebraTerm>();
            int termCount = _terms.Count;
            int startNegDivCount = pEvalData.NegDivCount;
            for (int i = 0; i < termCount; ++i)
            {
                AlgebraTerm left, right;

                if (switchSides)
                {
                    left = _terms[i];
                    right = otherSide.Clone().ToAlgTerm();
                }
                else
                {
                    left = otherSide.Clone().ToAlgTerm();
                    right = _terms[i];
                }

                pEvalData.IsWorkable = false;
                if (_solveDescs != null && i < _solveDescs.Length)
                {
                    pEvalData.WorkMgr.FromSides(left, right, _solveDescs[i]);
                }

                ExComp solved = solver.SolveEq(solveFor, left, right, ref pEvalData, false, true);
                if (solved == null)
                    return null;
                else if (solved is NoSolutions)
                    continue;
                else if (solved is AllSolutions)
                {
                    allSols = true;
                    return null;
                }
                if (solved is AlgebraTermArray)
                {
                    AlgebraTermArray mergeArray = solved as AlgebraTermArray;
                    solvedTerms.AddRange(mergeArray.Terms);
                }
                else
                    solvedTerms.Add(solved.ToAlgTerm());
            }

            if (startNegDivCount < pEvalData.NegDivCount)
            {
                int changeCount = pEvalData.NegDivCount - startNegDivCount;
                // The div count was incremented for each branch of equation.
                // Only increment it overall for each instance of negative number,
                // as each negative number was only divided once.
                if (changeCount % termCount == 0)
                {
                    int incrementCount = changeCount / termCount;
                    pEvalData.NegDivCount = startNegDivCount + incrementCount;
                }
                else
                {
                    // There is a problem.
                    // I see no reason why this should happen.
                    //TODO:
                    // Add error handeling code here.
                }
            }

            return new AlgebraTermArray(solvedTerms.ToArray());
        }

        public override AlgebraTerm ToAlgTerm()
        {
            throw new InvalidOperationException("AlgebraTermArray cannot be a AlgebraTerm!");
        }

        public override string ToMathAsciiString()
        {
            string texStr = "";
            for (int i = 0; i < _terms.Count; ++i)
            {
                AlgebraTerm term = _terms[i];

                texStr += term.ToMathAsciiString();

                if (i != _terms.Count - 1)
                    texStr += "; ";
            }

            return texStr;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToTexString()
        {
            string texStr = "";
            for (int i = 0; i < _terms.Count; ++i)
            {
                AlgebraTerm term = _terms[i];

                texStr += term.ToTexString();

                if (i != _terms.Count - 1)
                    texStr += "; ";
            }

            return texStr;
        }
    }
}