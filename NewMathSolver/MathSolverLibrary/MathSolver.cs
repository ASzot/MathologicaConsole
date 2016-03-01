using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.TermType;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal static class MathSolver
    {
        public const bool USE_TEX_DEBUG = true;
        
        /// <summary>
        /// Whether the input text is in latex or plain text.
        /// Integrals, derivatives are parsed differently.
        /// When TRUE
        ///     Derivatives are (d)/(dx)
        ///     Integrals can be entered like 'int x dx'
        /// When FALSE
        ///     Derivatives are \frac{d}{dx}
        ///     Integrals can be entered like \int x dx
        /// </summary>
        public const bool PLAIN_TEXT = true;

        public static TermType.GenTermType DetermineSingularEqSet(EqSet singularEqSet, List<TypePair<LexemeType, string>> completeLexemeTable,
            Dictionary<string, int> solveVars, MultiLineHelper mlh, ref TermType.EvalData pEvalData)
        {
            string probSolveVar = AlgebraSolver.GetProbableVar(solveVars);

            DiffEqGenTermType diffEqGenTt = new DiffEqGenTermType();
            diffEqGenTt.AttachMultiLineHelper(mlh);
            if (!singularEqSet.GetIsSingular() && diffEqGenTt.Init(singularEqSet, solveVars, probSolveVar, ref pEvalData))
                return diffEqGenTt;

            EquationInformation eqInfo = singularEqSet.GetIsSingular() ? new EquationInformation(singularEqSet.GetLeftTerm(), new AlgebraComp(probSolveVar)) :
                new EquationInformation(singularEqSet.GetLeftTerm(), singularEqSet.GetRightTerm(), new AlgebraComp(probSolveVar));

            // Error type for equations with no comparison sign.
            if (singularEqSet.GetComparisonOp() == LexemeType.EqualsOp || singularEqSet.GetComparisonOp() == LexemeType.ErrorType)
            {
                QuadraticGenTermType qtt = new QuadraticGenTermType();
                if (qtt.Init(eqInfo, singularEqSet.GetLeft(), singularEqSet.GetRight(), completeLexemeTable, solveVars, probSolveVar, ref pEvalData))
                    return qtt;

                SinusodalGenTermType stt = new SinusodalGenTermType();
                if (stt.Init(eqInfo, singularEqSet.GetLeft(), singularEqSet.GetRight(), completeLexemeTable, solveVars, probSolveVar, ref pEvalData))
                    return stt;
            }

            if (singularEqSet.GetIsSingular())
            {
                bool isFuncDef = false;
                if (singularEqSet.GetLeft() is FunctionDefinition)
                    isFuncDef = true;

                if (!singularEqSet.FixEqFuncDefs(ref pEvalData))
                    return null;
                // The single term is always in the left component.
                return new SimplifyGenTermType(singularEqSet.GetLeft(), completeLexemeTable, solveVars, probSolveVar, singularEqSet.GetStartingType(), isFuncDef);
            }
            else
            {
                if (singularEqSet.GetComparisonOp() == LexemeType.EqualsOp && singularEqSet.GetComparisonOps().Count == 1)
                {
                    FunctionGenTermType funcType = new FunctionGenTermType();
                    if (funcType.Init(singularEqSet, completeLexemeTable, solveVars, probSolveVar))
                        return funcType;
                }

                if (solveVars.Count == 0 && singularEqSet.GetComparisonOps().Count == 1)
                {
                    // There are no variables in this expression.
                    return new EqualityCheckGenTermType(singularEqSet.GetLeft(), singularEqSet.GetRight(), singularEqSet.GetComparisonOp());
                }

                if (!singularEqSet.FixEqFuncDefs(ref pEvalData))
                    return null;

                if (singularEqSet.IsLinearAlgebraTerm())
                {
                    return new LinearAlgebraSolve(singularEqSet, probSolveVar, ref pEvalData);
                }

                return new SolveGenTermType(singularEqSet, completeLexemeTable, solveVars, probSolveVar);
            }
        }

        public static string FinalizeOutput(string outputStr)
        {
            outputStr = outputStr.Replace("+-", "-");
            outputStr = outputStr.Replace("-1*", "-");

            return outputStr;
        }

        public static void Init()
        {
            Information_Helpers.UnitCircle.Init();
        }

        public static TermType.GenTermType ParseInput(string input, ref TermType.EvalData pEvalData, ref List<string> pParseErrors)
        {
            LexicalParser lexParser = new LexicalParser(pEvalData);
            List<TypePair<LexemeType, string>> completeLexemeTable = lexParser.CreateLexemeTable(input, ref pParseErrors);
            if (completeLexemeTable == null)
                return null;
            TermType.GenTermType result = ParseInput(completeLexemeTable, lexParser, input, ref pEvalData, ref pParseErrors);

            return result;
        }

        public static TermType.GenTermType ParseInput(List<TypePair<LexemeType, string>> completeLexemeTable, LexicalParser lexParser, string input, ref TermType.EvalData pEvalData, ref List<string> pParseErrors)
        {
            List<List<TypePair<LexemeType, string>>> lexemeTables;
            List<EqSet> terms = lexParser.ParseInput(input, out lexemeTables, ref pParseErrors);
            if (terms == null)
                return null;

            bool recheckSolveVars = MathSolverLibrary.Solving.EquationSystemSolve.DoAssignments(ref terms);

            Dictionary<string, int> solveVars = AlgebraSolver.GetIdenOccurances(completeLexemeTable);

            if (recheckSolveVars)
            {
                if (terms.Count == 1)
                    terms[0].GetAdditionVarFors(ref solveVars);
                RecheckSolveVars(ref solveVars, terms);
            }

            if (terms.Count == 1)
            {
                EqSet singularEqSet = terms[0];

                GenTermType gtt = DetermineSingularEqSet(singularEqSet, completeLexemeTable, solveVars, null, ref pEvalData);
                return gtt;
            }
            else
            {
                DiffEqGenTermType diffEqGenTt = new DiffEqGenTermType();
                if (diffEqGenTt.Init(terms))
                    return diffEqGenTt;

                MultiLineHelper mlh = new MultiLineHelper();
                int prevCount = terms.Count;
                terms = mlh.AssignLines(terms, ref lexemeTables, ref solveVars, out completeLexemeTable, ref pEvalData);
                if (terms.Count == prevCount)
                    mlh = null;

                for (int i = 0; i < terms.Count; ++i)
                {
                    if (!terms[i].FixEqFuncDefs(ref pEvalData))
                        return null;
                }

                if (terms.Count == 1)
                {
                    TermType.GenTermType singularGenTermType = DetermineSingularEqSet(terms[0], completeLexemeTable, solveVars, mlh, ref pEvalData);
                    singularGenTermType.AttachMultiLineHelper(mlh);
                    return singularGenTermType;
                }
                else if (terms.Count == 0)
                {
                    SimplifyGenTermType simp = new SimplifyGenTermType();
                    simp.AttachMultiLineHelper(mlh);
                    return simp;
                }

                int simpTermCount = 0;
                foreach (EqSet term in terms)
                {
                    if (term.GetSides().Count > 2)
                        return null;
                    if (term.GetLeft() == null || term.GetRight() == null)
                    {
                        simpTermCount++;
                    }
                }

                if (simpTermCount <= 1)
                {
                    EquationSystemGenTermType estt = new EquationSystemGenTermType(terms, lexemeTables, solveVars);
                    estt.AttachMultiLineHelper(mlh);
                    if (estt.Init(ref pEvalData))
                        return estt;
                }
                else if (simpTermCount == terms.Count)
                {
                    EquationSystemGenTermType estt = new EquationSystemGenTermType(terms, lexemeTables, solveVars);
                    estt.AttachMultiLineHelper(mlh);
                    if (estt.InitGraphingOnly(ref pEvalData))
                        return estt;
                }
                return null;
            }
        }

        private static void RecheckSolveVars(ref Dictionary<string, int> solveVars, List<EqSet> terms)
        {
            List<string> keys = solveVars.Keys.ToList();
            for (int i = 0; i < keys.Count; ++i)
            {
                bool contains = false;
                foreach (EqSet eqSet in terms)
                {
                    if (eqSet.ContainsVar(new AlgebraComp(keys[i])))
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    solveVars.Remove(keys[i]);
                }
            }
        }
    }
}