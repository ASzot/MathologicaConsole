using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class SimplifyTermType : TermType
    {
        public const string KEY_SIMPLIFY = "Simplify";
        private AlgebraSolver _agSolver;
        private PolynomialExt _denPolyInfo = null;
        private PolynomialExt _numPolyInfo = null;
        private ExComp _term;

        public SimplifyTermType(ExComp term)
        {
            _term = term;
            _agSolver = new AlgebraSolver();
        }

        public SimplifyTermType(ExComp term, List<TypePair<LexemeType, string>> lt, Dictionary<string, int> solveVars, string probSolveVar)
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
                        _numPolyInfo.Info.Var == null || _denPolyInfo.Info.Var == null ||
                        !_numPolyInfo.Info.Var.IsEqualTo(_denPolyInfo.Info.Var))
                    {
                        _numPolyInfo = null;
                        _denPolyInfo = null;
                    }
                }
            }

            List<string> solveVarKeys = (from solveVar in solveVars
                                         select solveVar.Key).Distinct().ToList();

            for (int i = 0; i < solveVarKeys.Count; ++i)
            {
                if (solveVarKeys[i] == null)
                    solveVarKeys.RemoveAt(i--);
                else if (solveVarKeys[i] == probSolveVar)
                {
                    solveVarKeys.RemoveAt(i);
                    break;
                }
            }

            if (probSolveVar != null)
                solveVarKeys.Insert(0, probSolveVar);

            int logOptionsCount = 0;
            if (term.ToAlgTerm().HasLogFunctions())
                logOptionsCount = 2;

            List<string> tmpCmds = new List<string>();

            if (_numPolyInfo != null && _denPolyInfo != null && _numPolyInfo.MaxPow > _denPolyInfo.MaxPow)
            {
                tmpCmds.Add("Divide");
            }

            if (_term is ExMatrix)
            {
                if (_term is ExVector)
                {
                    tmpCmds.Add("Normalize");
                }
                else
                {
                    tmpCmds.Add("Find determinant");
                    tmpCmds.Add("Transpose");
                }
            }


            tmpCmds.Add(KEY_SIMPLIFY);

            if (!(_term is ExMatrix) && _numPolyInfo == null && _denPolyInfo == null && !(term is AlgebraFunction) && 
                solveVarKeys.Count != 0)
                tmpCmds.Add("Factor");

            if (term is Number)
            {
                Number num = term as Number;
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
                term is Equation.Functions.Calculus.Integral))
            {
                for (int i = 1; i < solveVarKeys.Count + 1; ++i)
                {
                    if (solveVarKeys[i - 1] != null)
                        tmpCmds.Add("Domain of " + solveVarKeys[i - 1]);
                }

                foreach (var solveKey in solveVarKeys)
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

        public static ExComp BasicSimplify(ExComp term, ref EvalData pEvalData)
        {
            AlgebraTerm agTerm;
            if (term is AlgebraTerm)
            {
                agTerm = term as AlgebraTerm;

                agTerm = agTerm.ApplyOrderOfOperations();
                agTerm = agTerm.WeakMakeWorkable().ToAlgTerm();
                agTerm = Simplifier.AttemptCancelations(agTerm, ref pEvalData).ToAlgTerm();

                agTerm = agTerm.ApplyOrderOfOperations();
                term = agTerm.MakeWorkable();
                if (term is AlgebraTerm)
                {
                    if ((term as AlgebraTerm).HasTrigFunctions())
                    {
                        // There are trig functions in this expression.
                        term = (term as AlgebraTerm).TrigSimplify();
                    }
                    term = (term as AlgebraTerm).CompoundFractions();
                }

                term = Equation.Functions.PowerFunction.FixFraction(term);
                if (term is AlgebraTerm)
                    term = (term as AlgebraTerm).RemoveRedundancies();
            }

            agTerm = term.ToAlgTerm();
            // Surround with an algebra term just to make sure everything is checked.
            AlgebraTerm surroundedAgTerm = new AlgebraTerm(agTerm);
            ExComp simpEx = Simplifier.Simplify(surroundedAgTerm, ref pEvalData);

            if (simpEx is AlgebraTerm)
                simpEx = (simpEx as AlgebraTerm).RemoveRedundancies();
            return simpEx;
        }

        public static SolveResult SimplfyTerm(ExComp term, ref EvalData pEvalData)
        {
            if (Number.IsUndef(term))
                return SolveResult.Simplified(Number.Undefined);
            ExComp simpEx = BasicSimplify(term, ref pEvalData);

            Solution solution;

            solution = new Solution(simpEx);
            if (simpEx is AlgebraTerm)
            {
                ExComp harshSimpEx = Simplifier.HarshSimplify(simpEx.Clone() as AlgebraTerm, ref pEvalData);
                if (!harshSimpEx.IsEqualTo(simpEx))
                {
                    // Display the harsh simplified equation as well.
                    solution.ApproximateResult = harshSimpEx;
                }
            }
            else if (simpEx is Constant)
                solution.ApproximateResult = (simpEx as Constant).Value;

            return solution.ToSolveResult();
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            _agSolver.ResetIterCount();

            if (command == KEY_SIMPLIFY)
            {
                return SimplfyTerm(_term.Clone(), ref pEvalData);
            }
            else if (command == "To polar form")
            {
                Number num = _term as Number;
                if (num == null)
                    return SolveResult.Failure();

                return SolveResult.Simplified(num.ToPolarForm(ref pEvalData));
            }
            else if (command == "To exponential form")
            {
                Number num = _term as Number;
                if (num == null)
                    return SolveResult.Failure();

                return SolveResult.Simplified(num.ToExponentialForm(ref pEvalData));
            }
            else if (command == "Normalize")
            {
                ExVector vec = _term as ExVector;
                if (vec == null)
                    return SolveResult.Failure();

                return SolveResult.Simplified(vec.Normalize());
            }
            else if (command == "Find determinant")
            {
                ExMatrix mat = _term as ExMatrix;
                if (mat == null)
                    return SolveResult.Failure();

                Determinant det = new Determinant(mat);

                return SolveResult.Simplified(det.Evaluate(false, ref pEvalData));
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
                ExComp factorized = _term.Clone().ToAlgTerm().FactorizeTerm(ref pEvalData);
                if (factorized is AlgebraTerm)
                    factorized = (factorized as AlgebraTerm).RemoveRedundancies();

                return SolveResult.Simplified(factorized);
            }
            else if (command.StartsWith("Derivative d/d"))
            {
                string varForKey = command.Substring("Derivative d/d".Length, command.Length - "Derivative d/d".Length);

                Equation.Functions.Calculus.Derivative deriv = new Equation.Functions.Calculus.Derivative(_term);
                deriv.WithRespectTo = new AlgebraComp(varForKey);

                return SolveResult.Simplified(deriv.Evaluate(false, ref pEvalData));
            }
            else if (command == "Condense logs")
            {
                return SolveResult.Simplified(_term.ToAlgTerm().CompoundLogs());
            }
            else if (command == "Expand logs")
            {
                return SolveResult.Simplified(_term.ToAlgTerm().ExpandLogs());
            }
            else if (command == "Divide" && _numPolyInfo != null && _denPolyInfo != null)
            {
                ExComp divided = Equation.Operators.DivOp.AttemptPolyDiv(_numPolyInfo.Clone(), _denPolyInfo.Clone(), ref pEvalData);
                return SolveResult.Simplified(divided);
            }
            else if (command.StartsWith("Domain of "))
            {
                string varForKey = command.Substring("Domain of ".Length, command.Length - "Domain of ".Length);
                AlgebraVar varFor = new AlgebraVar(varForKey);

                return _agSolver.CalculateDomain(_term, varFor, ref pEvalData);
            }
            else if (command == "Graph")
            {
                if (pEvalData.AttemptSetGraphData(_term))
                    return SolveResult.Solved();
                else
                    return SolveResult.Failure();
            }

            return SolveResult.InvalidCmd(ref pEvalData);
        }

        public void SetToSimpOnly()
        {
            _cmds = new string[1];
            _cmds[0] = KEY_SIMPLIFY;
        }
    }
}