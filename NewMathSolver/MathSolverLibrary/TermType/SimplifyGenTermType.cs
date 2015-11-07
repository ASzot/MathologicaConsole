using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class SimplifyGenTermType : GenTermType
    {
        public const string KEY_SIMPLIFY = "Simplify";
        private AlgebraSolver _agSolver;
        private PolynomialExt _denPolyInfo = null;
        private PolynomialExt _numPolyInfo = null;
        private ExComp _term;

        public SimplifyGenTermType()
        {
            _cmds = new string[] { KEY_SIMPLIFY };
        }

        public SimplifyGenTermType(ExComp term)
        {
            _term = term;
            _agSolver = new AlgebraSolver();
        }

        public SimplifyGenTermType(ExComp term, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, string probSolveVar, Type startingType, bool isFuncDef)
            : base()
        {
            _term = term;

            if (_term is AlgebraTerm)
            {
                AlgebraTerm agTerm = _term as AlgebraTerm;

                AlgebraTerm[] numDen = agTerm.GetNumDenFrac();
                if (numDen != null)
                {
                    _numPolyInfo = new PolynomialExt();
                    _denPolyInfo = new PolynomialExt();

                    if (!_numPolyInfo.Init(numDen[0]) || !_denPolyInfo.Init(numDen[1]) ||
                        _numPolyInfo.GetInfo().GetVar() == null || _denPolyInfo.GetInfo().GetVar() == null ||
                        !_numPolyInfo.GetInfo().GetVar().IsEqualTo(_denPolyInfo.GetInfo().GetVar()))
                    {
                        _numPolyInfo = null;
                        _denPolyInfo = null;
                    }
                }
            }

            List<string> solveVarKeys = ArrayFunc.Distinct(solveVars);

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == null)
                    ArrayFunc.RemoveIndex(solveVarKeys, i--);
                else if (solveVarKeys[i] == probSolveVar)
                {
                    ArrayFunc.RemoveIndex(solveVarKeys, i);
                    break;
                }
            }

            if (probSolveVar != null)
                solveVarKeys.Insert(0, probSolveVar);

            int logOptionsCount = 0;
            if (term.ToAlgTerm().HasLogFunctions())
                logOptionsCount = 2;

            List<string> tmpCmds = new List<string>();
            if (isFuncDef)
                tmpCmds.Add(KEY_SIMPLIFY);

            if (_numPolyInfo != null && _denPolyInfo != null && _numPolyInfo.GetMaxPow() > _denPolyInfo.GetMaxPow())
            {
                tmpCmds.Add("Divide");
            }

            if (term is SumFunction)
            {
                SumFunction sum = term as SumFunction;
                if (sum.GetIsInfiniteSeries())
                    tmpCmds.Add("Test for convergence");
            }

            if ((startingType != null && startingType != typeof(ExMatrix) && startingType != typeof(ExVector)) && !tmpCmds.Contains(KEY_SIMPLIFY))
                tmpCmds.Add(KEY_SIMPLIFY);

            if (_term is ExMatrix)
            {
                if (_term is ExVector)
                {
                    tmpCmds.Add("Normalize");
                }
                else
                {
                    ExMatrix mat = _term as ExMatrix;
                    if (mat.GetIsSquare())
                    {
                        tmpCmds.Add("Find inverse");
                        tmpCmds.Add("Find determinant");
                    }
                    tmpCmds.Add("Transpose");
                }
            }

            if (!tmpCmds.Contains(KEY_SIMPLIFY))
                tmpCmds.Add(KEY_SIMPLIFY);

            if (!(_term is ExMatrix) && _numPolyInfo == null && _denPolyInfo == null && !(term is AlgebraFunction) &&
                solveVarKeys.Count != 0)
                tmpCmds.Add("Factor");

            if (term is ExNumber)
            {
                ExNumber num = term as ExNumber;
                if (num.HasImaginaryComp())
                {
                    tmpCmds.Add("To polar form");
                    tmpCmds.Add("To exponential form");
                }
            }

            if (!(_term is Equation.Functions.Calculus.Derivative ||
                term is Equation.Functions.Calculus.Limit ||
                term is Equation.Functions.SumFunction ||
                term is Equation.Functions.ChooseFunction ||
                term is Equation.Functions.FactorialFunction ||
                term is Equation.Functions.Calculus.Vector.FieldTransformation ||
                term is Equation.Functions.Calculus.Integral))
            {
                for (int i = 1; i < solveVarKeys.Count + 1; ++i)
                {
                    if (solveVarKeys[i - 1] != null)
                        tmpCmds.Add("Domain of " + solveVarKeys[i - 1]);
                }

                foreach (string solveKey in solveVarKeys)
                {
                    if (solveKey != null)
                        tmpCmds.Add("Derivative d/d" + solveKey);
                }

                if (logOptionsCount != 0)
                {
                    tmpCmds.Add("Condense logs");
                    tmpCmds.Add("Expand logs");
                }
            }

            AlgebraTerm at = term.ToAlgTerm();
            if (at.GetAllAlgebraCompsStr().Count == 1 && at.ToJavaScriptString(true) != null)
                tmpCmds.Add("Graph");

            _cmds = tmpCmds.ToArray();

            _agSolver = new AlgebraSolver();
            _agSolver.CreateUSubTable(solveVars);
        }

        public override void AttachMultiLineHelper(MultiLineHelper mlh)
        {
            base.AttachMultiLineHelper(mlh);

            if (_term == null && mlh.GetShouldGraph())
            {
                // Adjust to allow for graphing.
                _cmds = new string[] { "Graph", KEY_SIMPLIFY };
            }
        }

        public static ExComp BasicSimplify(ExComp term, ref EvalData pEvalData, bool factor)
        {
            AlgebraTerm tmpTerm = term.ToAlgTerm();

            // Surround in AlgebraTerm body to ensure that all of the functions are called.
            tmpTerm = new AlgebraTerm(tmpTerm);
            tmpTerm.CallFunctions(ref pEvalData);
            term = tmpTerm.RemoveRedundancies(false);

            AlgebraTerm agTerm;
            if (term is AlgebraTerm)
            {
                agTerm = term as AlgebraTerm;

                agTerm = agTerm.ApplyOrderOfOperations();
                agTerm = agTerm.WeakMakeWorkable(ref pEvalData).ToAlgTerm();
                if (factor)
                    agTerm = Simplifier.AttemptCancelations(agTerm, ref pEvalData).ToAlgTerm();

                agTerm = agTerm.ApplyOrderOfOperations();
                term = agTerm.MakeWorkable();
                if (term is AlgebraTerm)
                {
                    if ((term as AlgebraTerm).HasTrigFunctions())
                    {
                        // There are trig functions in this expression.
                        term = AdvAlgebraTerm.TrigSimplify((term as AlgebraTerm), ref pEvalData);
                    }
                    term = (term as AlgebraTerm).CompoundFractions();
                }

                term = Equation.Functions.PowerFunction.FixFraction(term);
                if (term is AlgebraTerm)
                    term = (term as AlgebraTerm).RemoveRedundancies(false);
            }

            agTerm = term.ToAlgTerm();
            // Surround with an algebra term just to make sure everything is checked.
            AlgebraTerm surroundedAgTerm = new AlgebraTerm(agTerm);
            ExComp simpEx = Simplifier.Simplify(surroundedAgTerm, ref pEvalData);

            if (simpEx is AlgebraTerm)
                simpEx = (simpEx as AlgebraTerm).RemoveRedundancies(false);
            return simpEx;
        }

        public static SolveResult SimplfyTerm(ExComp term, ref EvalData pEvalData)
        {
            if (ExNumber.IsUndef(term))
                return SolveResult.Simplified(ExNumber.GetUndefined());
            ExComp simpEx = BasicSimplify(term, ref pEvalData, true);

            Solution solution;

            solution = new Solution(simpEx);
            if (simpEx is AlgebraTerm)
            {
                ExComp harshSimpEx = Simplifier.HarshSimplify(simpEx.CloneEx() as AlgebraTerm, ref pEvalData, true);
                if (!harshSimpEx.IsEqualTo(simpEx))
                {
                    // Display the harsh simplified equation as well.
                    solution.ApproximateResult = harshSimpEx;
                }
            }
            else if (simpEx is Constant)
                solution.ApproximateResult = (simpEx as Constant).GetValue();

            return solution.ToSolveResult();
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            if (_agSolver != null)
                _agSolver.ResetIterCount();

            if (command == KEY_SIMPLIFY)
            {
                if (_term == null)
                    return SolveResult.Solved();
                return SimplfyTerm(_term.CloneEx(), ref pEvalData);
            }
            else if (command == "To polar form")
            {
                ExNumber num = _term as ExNumber;
                if (num == null)
                    return SolveResult.Failure();

                SolveResult polarResult = SolveResult.Simplified(num.ToPolarForm(ref pEvalData));
                return polarResult;
            }
            else if (command == "To exponential form")
            {
                ExNumber num = _term as ExNumber;
                if (num == null)
                    return SolveResult.Failure();

                SolveResult expResult = SolveResult.Simplified(num.ToExponentialForm(ref pEvalData));
                return expResult;
            }
            else if (command == "Normalize")
            {
                ExVector vec = _term as ExVector;
                if (vec == null)
                    return SolveResult.Failure();

                SolveResult normResult = SolveResult.SimplifiedCalcApprox(vec.Normalize(), ref pEvalData);
                return normResult;
            }
            else if (command == "Find determinant")
            {
                ExMatrix mat = _term as ExMatrix;
                if (mat == null)
                    return SolveResult.Failure();

                Determinant det = new Determinant(mat);

                SolveResult detResult = SolveResult.SimplifiedCalcApprox(det.Evaluate(false, ref pEvalData), ref pEvalData);
                return detResult;
            }
            else if (command == "Find inverse")
            {
                ExMatrix mat = _term as ExMatrix;
                if (mat == null)
                    return SolveResult.Failure();

                ExMatrix inverse = mat.GetInverse();
                if (inverse == null)
                {
                    pEvalData.AddMsg("Inverse doesn't exist");
                    return SolveResult.Solved();
                }
                return SolveResult.Simplified(inverse);
            }
            else if (command == "Transpose")
            {
                ExMatrix mat = _term as ExMatrix;
                if (mat == null)
                    return SolveResult.Failure();

                return SolveResult.Simplified(mat.Transpose());
            }
            else if (command == "Factor")
            {
                ExComp factorized = AdvAlgebraTerm.FactorizeTerm(_term.CloneEx().ToAlgTerm(), ref pEvalData, true);
                if (factorized is AlgebraTerm)
                    factorized = (factorized as AlgebraTerm).RemoveRedundancies(false);

                return SolveResult.Simplified(factorized);
            }
            else if (command.StartsWith("Derivative d/d"))
            {
                string varForKey = command.Substring("Derivative d/d".Length, command.Length - "Derivative d/d".Length);

                Equation.Functions.Calculus.Derivative deriv = new Equation.Functions.Calculus.Derivative(_term);
                deriv.SetWithRespectTo(new AlgebraComp(varForKey));

                SolveResult derivResult = SolveResult.Simplified(deriv.Evaluate(false, ref pEvalData));
                return derivResult;
            }
            else if (command == "Condense logs")
            {
                return SolveResult.Simplified(AdvAlgebraTerm.CompoundLogs(_term.ToAlgTerm(), null));
            }
            else if (command == "Expand logs")
            {
                return SolveResult.Simplified(AdvAlgebraTerm.ExpandLogs(_term.ToAlgTerm()));
            }
            else if (command == "Divide" && _numPolyInfo != null && _denPolyInfo != null)
            {
                ExComp divided = Equation.Operators.DivOp.AttemptPolyDiv(_numPolyInfo.Clone(), _denPolyInfo.Clone(), ref pEvalData);
                if (divided != null)
                    return SolveResult.Simplified(divided);
                else
                    return SolveResult.Failure();
            }
            else if (command.StartsWith("Domain of "))
            {
                string varForKey = command.Substring("Domain of ".Length, command.Length - "Domain of ".Length);
                AlgebraVar varFor = new AlgebraVar(varForKey);

                SolveResult domainResult = _agSolver.CalculateDomain(_term, varFor, ref pEvalData);
                return domainResult;
            }
            else if (command == "Graph")
            {
                if (_term == null && _multiLineHelper.GetShouldGraph())
                {
                    pEvalData.AttemptSetGraphData(_multiLineHelper.GetGraphStrs(), _multiLineHelper.GetGraphVar());
                    return SolveResult.Solved();
                }
                string graphStr = _term.ToAlgTerm().GetAllAlgebraCompsStr()[0];
                if (pEvalData.AttemptSetGraphData(_term, graphStr))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }
            else if (command == "Test for convergence")
            {
                SumFunction sum = _term as SumFunction;
                ExComp result = null;
                bool? converges = sum.Converges(ref pEvalData, out result);
                if (converges == null)
                    return SolveResult.Failure("Cannot determine convergence or divergence", ref pEvalData);

                pEvalData.AddMsg(converges.Value ? "Converges" : "Diverges");

                return result == null ? SolveResult.Solved() : SolveResult.Simplified(result);
            }

            SolveResult invalidResult = SolveResult.InvalidCmd(ref pEvalData);
            return invalidResult;
        }

        public void SetToSimpOnly()
        {
            _cmds = new string[1];
            _cmds[0] = KEY_SIMPLIFY;
        }
    }
}