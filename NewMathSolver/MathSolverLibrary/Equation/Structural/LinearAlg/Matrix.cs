using System.Collections.Generic;
using System; 

namespace MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg
{
    internal class ExMatrix : AlgebraFunction
    {
        public class ExTrash : ExComp
        {
            public override ExComp Clone()
            {
                return this;
            }

            public override double GetCompareVal()
            {
                return 1.0;
            }

            public override bool IsEqualTo(ExComp ex)
            {
                return false;
            }

            public override AlgebraTerm ToAlgTerm()
            {
                return new AlgebraTerm(this);
            }

            public override string ToAsciiString()
            {
                return "EVAL_ERROR";
            }

            public override string ToJavaScriptString(bool useRad)
            {
                return null;
            }

            public override string ToTexString()
            {
                return "EVAL_ERROR";
            }

            public override string ToString()
            {
                return "EVAL_ERROR";
            }
        }

        protected ExComp[][] _exData;

        /// <summary>
        /// The 'first' dimension of the array.
        /// If 'i' is the ith row it can be 
        /// accessed through _exData[i][0]
        /// </summary>
        public int Rows
        {
            get { return _exData.Length; }
        }

        /// <summary>
        /// The 'second' dimension of the array.
        /// If 'i' s the ith row it can be accessed
        /// through _exData[0][i].
        /// </summary>
        public int Cols
        {
            get { return _exData.Length > 0 ? _exData[0].Length : 0; }
        }

        public bool IsSquare
        {
            get { return Cols == Rows; }
        }

        public ExMatrix(int rows, int cols)
        {
            _exData = new ExComp[rows][];
            for (int i = 0; i < rows; ++i)
            {
                _exData[i] = new ExComp[cols];
            }

            _subComps = new List<ExComp>();
            _subComps.Add(new ExTrash());
        }

        public ExMatrix(params ExComp[][] exData)
        {
            _exData = exData;
            _subComps = new List<ExComp>();
            _subComps.Add(new ExTrash());
        }

        public ExComp Get(int row, int col)
        {
            return _exData[row][col];
        }

        public void Set(int row, int col, ExComp val)
        {
            _exData[row][col] = val;
        }

        public ExVector GetRowVec(int row)
        {
            return new ExVector(_exData[row]);
        }

        public ExVector GetColVec(int col)
        {
            ExComp[] data = new ExComp[Rows];
            for (int i = 0; i < Rows; ++i)
            {
                data[i] = _exData[i][col];
            }

            return new ExVector(data);
        }

        public ExMatrix GetMatrixMinor(int cancelRow, int cancelCol)
        {
            ExComp[][] comps = new ExComp[Rows - 1][];
            for (int i = 0; i < Rows - 1; ++i)
                comps[i] = new ExComp[Cols - 1];

            for (int i = 0; i < Rows; ++i)
            {
                if (i == cancelRow)
                    continue;
                for (int j = 0; j < Cols; ++j)
                {
                    if (j == cancelCol)
                        continue;
                    comps[i][j] = _exData[i][j];
                }
            }

            return new ExMatrix(comps);
        }

        public ExMatrix Transpose()
        {
            ExComp[][] transposedData = new ExComp[Cols][];
            for (int i = 0; i < Cols; ++i)
            {
                transposedData[i] = new ExComp[Rows];
                for (int j = 0; j < Rows; ++j)
                {
                    transposedData[i][j] = _exData[j][i];
                }
            }

            return new ExMatrix(transposedData);
        }

        public void ModifyEach(Func<ExComp, ExComp> func)
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    _exData[i][j] = func(_exData[i][j]);
                }
            }
        }

        public override AlgebraTerm ApplyOrderOfOperations()
        {
            for (int i = 0; i < _exData.Length; ++i)
            {
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).ApplyOrderOfOperations();
                }
            }

            return this;
        }

        public override AlgebraTerm CompoundFractions()
        {
            for (int i = 0; i < _exData.Length; ++i)
            {
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).CompoundFractions();
                }
            }

            return this;
        }

        public override AlgebraTerm CompoundFractions(out bool valid)
        {
            if (Cols == 0 && Rows == 0)
            {
                valid = false;
                return this;
            }

            _exData[0][0] = _exData[0][0].ToAlgTerm().CompoundFractions(out valid);

            for (int i = 0; i < _exData.Length; ++i)
            {
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (i == 0 && j == 0)
                        continue;
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).CompoundFractions(out valid);
                }
            }

            return this;
        }

        public override bool Contains(AlgebraComp varFor)
        {
            for (int i = 0; i < _exData.Length; ++i)
            {
                for (int j = 0; j < _exData.Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm && (_exData[i][j] as AlgebraTerm).Contains(varFor))
                        return true;
                }
            }

            return false;
        }

        public override AlgebraTerm ConvertImaginaryToVar()
        {
            for (int i = 0; i < _exData.Length; ++i)
            {
                for (int j = 0; j < _exData.Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).ConvertImaginaryToVar();
                }
            }

            return this;
        }

        public override string FinalToAsciiKeepFormatting()
        {
            string totalStr = "[";
            for (int i = 0; i < _exData.Length; ++i)
            {
                if (Rows != 1)
                    totalStr += "[";
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        totalStr += (_exData[i][j] as AlgebraTerm).FinalToAsciiKeepFormatting();
                    else
                        totalStr += _exData[i][j].ToAsciiString();

                    if (j != _exData[i].Length - 1)
                        totalStr += ",";
                }

                if (Rows != 1)
                    totalStr += "]";
                if (i != _exData.Length - 1)
                    totalStr += ",";
            }
            totalStr += "]";

            return totalStr;
        }

        public override string FinalToTexKeepFormatting()
        {
            string totalStr = "[";
            for (int i = 0; i < _exData.Length; ++i)
            {
                if (Rows != 1)
                    totalStr += "[";
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        totalStr += (_exData[i][j] as AlgebraTerm).FinalToTexKeepFormatting();
                    else
                        totalStr += _exData[i][j].ToTexString();

                    if (j != _exData[i].Length - 1)
                        totalStr += ",";
                }

                if (Rows != 1)
                    totalStr += "]";
                if (i != _exData.Length - 1)
                    totalStr += ",";
            }
            totalStr += "]";

            return totalStr;
        }

        public override AlgebraTerm ForceCombineExponents()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).ForceCombineExponents();
                }
            }

            return this;
        }

        public override List<FunctionType> GetAppliedFunctionsNoPow(AlgebraComp varFor)
        {
            List<FunctionType> totalFuncs = new List<FunctionType>();
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                    {
                        List<FunctionType>
							tmp = (_exData[i][j] as AlgebraTerm).GetAppliedFunctionsNoPow(varFor);
                        totalFuncs.AddRange(tmp);
                    }
                }
            }

            return totalFuncs;
        }

        public override List<ExComp[]> GetGroups()
        {
            // Handle the same way as an AppliedFunction.
            List<ExComp[]> singleGp = new List<ExComp[]>();
            singleGp.Add(new ExComp[] { this });
            return singleGp;
        }

        public override List<ExComp> GetPowersOfVar(AlgebraComp varFor)
        {
            List<ExComp> totalFuncs = new List<ExComp>();
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                    {
                        List<ExComp> tmp = (_exData[i][j] as AlgebraTerm).GetPowersOfVar(varFor);
                        totalFuncs.AddRange(tmp);
                    }
                }
            }

            return totalFuncs;
        }

        public override AlgebraTerm HarshEvaluation()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).HarshEvaluation();
                }
            }

            return this;
        }

        public override bool HasVariablePowers(AlgebraComp varFor)
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm && (_exData[i][j] as AlgebraTerm).HasVariablePowers(varFor))
                        return true;
                }
            }

            return false;
        }

        public override bool IsOne()
        {
            return false;
        }

        public override bool IsUndefined()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm && (_exData[i][j] as AlgebraTerm).IsUndefined())
                        return true;
                }
            }

            return false;
        }

        public override bool IsZero()
        {
            return false;
        }
        
        public override ExComp MakeWorkable()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).MakeWorkable();
                }
            }

            return this;
        }

        public override AlgebraTerm Order()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).Order();
                }
            }

            return this;
        }

        public override AlgebraTerm RemoveOneCoeffs()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).RemoveOneCoeffs();
                }
            }

            return this;
        }

        public override ExComp RemoveRedundancies(bool postWorkable = false)
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).RemoveRedundancies(postWorkable);
                }
            }

            return this;
        }

        public override AlgebraTerm RemoveZeros()
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).RemoveZeros();
                }
            }

            return this;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn)
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    _exData[i][j] = _exData[i][j].ToAlgTerm().Substitute(subOut, subIn);
                }
            }

            return this;
        }

        public override AlgebraTerm Substitute(ExComp subOut, ExComp subIn, ref bool success)
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    _exData[i][j] = _exData[i][j].ToAlgTerm().Substitute(subOut, subIn, ref success);
                }
            }

            return this;
        }

        public override bool TermsRelatable(ExComp comp)
        {
            return false;
        }

        public override void AssignTo(AlgebraTerm algebraTerm)
        {
            if (algebraTerm is ExMatrix)
            {
                ExMatrix mat = algebraTerm as ExMatrix;
            }
        }

        public override ExComp Clone()
        {
            ExComp[][] clonedExData = new ExComp[Rows][];
            for (int i = 0; i < clonedExData.Length; ++i)
                clonedExData[i] = new ExComp[Cols];
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    clonedExData[i][j] = _exData[i][j].Clone();
                }
            }

            return new ExMatrix(clonedExData);
        }

        public override double GetCompareVal()
        {
            return 1.0;
        }

        public override ExComp WeakMakeWorkable(ref List<string> pParseErrors)
        {
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        _exData[i][j] = (_exData[i][j] as AlgebraTerm).WeakMakeWorkable(ref pParseErrors);
                }
            }

            return this;
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is ExMatrix)
            {
                ExMatrix mat = ex as ExMatrix;
                if (this.Rows != mat.Rows || this.Cols != mat.Cols)
                    return false;
                for (int i = 0; i < this.Rows; ++i)
                {
                    for (int j = 0; j < this.Cols; ++j)
                    {
                        if (_exData[i][j] == mat._exData[i][j])
                            continue;
                        if ((_exData[i][j] == null && mat._exData[i][j] != null) ||
                            (_exData[i][j] == null && mat._exData[i][j] != null))
                            return false;
                        if (!_exData[i][j].IsEqualTo(mat._exData[i][j]))
                            return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override AlgebraTerm ToAlgTerm()
        {
            return this;
        }

        public override string FinalToAsciiString()
        {
            string totalStr = "[";
            for (int i = 0; i < _exData.Length; ++i)
            {
                if (Rows != 1)
                    totalStr += "[";
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        totalStr += (_exData[i][j] as AlgebraTerm).FinalToAsciiString();
                    else
                        totalStr += _exData[i][j].ToAsciiString();

                    if (j != _exData[i].Length - 1)
                        totalStr += ",";
                }

                if (Rows != 1)
                    totalStr += "]";
                if (i != _exData.Length - 1)
                    totalStr += ",";
            }
            totalStr += "]";

            return totalStr;
        }

        public override string ToAsciiString()
        {
            string totalStr = "[";
            for (int i = 0; i < _exData.Length; ++i)
            {
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    totalStr += _exData[i][j].ToAsciiString();

                    if (j != _exData.Length - 1)
                        totalStr += ",";
                }
            }
            totalStr += "]";

            return totalStr;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return ToTexString();
        }

        public override string FinalToTexString()
        {
            string totalStr = "[";
            for (int i = 0; i < _exData.Length; ++i)
            {
                if (Rows != 1)
                    totalStr += "[";
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    if (_exData[i][j] is AlgebraTerm)
                        totalStr += (_exData[i][j] as AlgebraTerm).FinalToTexString();
                    else
                        totalStr += _exData[i][j].ToTexString();

                    if (j != _exData[i].Length - 1)
                        totalStr += ",";
                }

                if (Rows != 1)
                    totalStr += "]";
                if (i != _exData.Length - 1)
                    totalStr += ",";
            }
            totalStr += "]";

            return totalStr;
        }

        public override string ToTexString()
        {
            string totalStr = "[";
            for (int i = 0; i < _exData.Length; ++i)
            {
                if (Rows != 1)
                    totalStr += "[";
                for (int j = 0; j < _exData[i].Length; ++j)
                {
                    totalStr += _exData[i][j].ToTexString();

                    if (j != _exData[i].Length - 1)
                        totalStr += ",";
                }

                if (Rows != 1)
                    totalStr += "]";
                if (i != _exData.Length - 1)
                    totalStr += ",";
            }
            totalStr += "]";

            return totalStr;
        }

        public override List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData)
        {
            List<Restriction> allDomain = new List<Restriction>();
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraFunction)
                    {
                        List<Restriction> addRest = (_exData[i][j] as AlgebraFunction).GetDomain(varFor, agSolver, ref pEvalData);
                        allDomain.AddRange(addRest);
                    }
                }
            }

            return Restriction.CompoundRestrictions(allDomain, ref pEvalData);
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            // A matrix cannot 'evaluate' as it isn't technically a function.
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Cols; ++j)
                {
                    if (_exData[i][j] is AlgebraFunction)
                        _exData[i][j] = (_exData[i][j] as AlgebraFunction).Evaluate(harshEval, ref pEvalData);
                    else if (_exData[i][j] is AlgebraTerm)
                        (_exData[i][j] as AlgebraTerm).EvaluateFunctions(harshEval, ref pEvalData);
                }
            }
            return this;
        }
    }
}