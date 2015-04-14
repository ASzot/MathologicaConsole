using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Information_Helpers;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.TermType;
using System.Collections.Generic;
using System.Linq;
using LexemeTable = System.Collections.Generic.List<
MathSolverWebsite.MathSolverLibrary.TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>>;

namespace MathSolverWebsite.MathSolverLibrary
{
    internal static class MathSolver
    {
        public const bool USE_TEX_DEBUG = true;
        public const bool PLAIN_TEXT = true;

        public static TermType.TermType DetermineSingularEqSet(EquationSet singularEqSet, List<TypePair<LexemeType, string>> completeLexemeTable,
            Dictionary<string, int> solveVars, ref TermType.EvalData pEvalData)
        {
            string probSolveVar = AlgebraSolver.GetProbableVar(solveVars);

            EquationInformation eqInfo = singularEqSet.IsSingular ? new EquationInformation(singularEqSet.LeftTerm, new AlgebraComp(probSolveVar)) :
                new EquationInformation(singularEqSet.LeftTerm, singularEqSet.RightTerm, new AlgebraComp(probSolveVar));

            // Error type for equations with no comparison sign.
            if (singularEqSet.ComparisonOp == LexemeType.EqualsOp || singularEqSet.ComparisonOp == LexemeType.ErrorType)
            {
                QuadraticTermType qtt = new QuadraticTermType();
                if (qtt.Init(eqInfo, singularEqSet.Left, singularEqSet.Right, completeLexemeTable, solveVars, probSolveVar, ref pEvalData))
                    return qtt;

                SinusodalTermType stt = new SinusodalTermType();
                if (stt.Init(eqInfo, singularEqSet.Left, singularEqSet.Right, completeLexemeTable, solveVars, probSolveVar, ref pEvalData))
                    return stt;
            }

            if (singularEqSet.IsSingular)
            {
                if (!singularEqSet.FixEqFuncDefs(ref pEvalData))
                    return null;
                // The single term is always in the left component.
                return new SimplifyTermType(singularEqSet.Left, completeLexemeTable, solveVars, probSolveVar);
            }
            else
            {
                if (solveVars.Count == 0 && singularEqSet.ComparisonOps.Count == 1)
                {
                    // There are no variables in this expression.
                    return new EqualityCheckTermType(singularEqSet.Left, singularEqSet.Right, singularEqSet.ComparisonOp);
                }

                if (singularEqSet.ComparisonOp == LexemeType.EqualsOp && singularEqSet.ComparisonOps.Count == 1)
                {
                    FunctionTermType funcType = new FunctionTermType();
                    if (funcType.Init(singularEqSet, completeLexemeTable, solveVars, probSolveVar))
                        return funcType;
                }

                if (!singularEqSet.FixEqFuncDefs(ref pEvalData))
                    return null;

                if (singularEqSet.IsLinearAlgebraTerm())
                {
                    return new LinearAlgebraSolve(singularEqSet);
                }

                return new SolveTermType(singularEqSet, completeLexemeTable, solveVars, probSolveVar);
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

        public static TermType.TermType ParseInput(string input, ref TermType.EvalData pEvalData, ref List<string> pParseErrors)
        {
            LexicalParser lexParser = new LexicalParser(pEvalData);
            List<TypePair<LexemeType, string>> completeLexemeTable = lexParser.CreateLexemeTable(input, ref pParseErrors);
            if (completeLexemeTable == null)
                return null;

            List<LexemeTable> lexemeTables;
            List<EquationSet> terms = lexParser.ParseInput(input, out lexemeTables, ref pParseErrors);
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
                EquationSet singularEqSet = terms[0];

                return DetermineSingularEqSet(singularEqSet, completeLexemeTable, solveVars, ref pEvalData);
            }
            else
            {
                for (int i = 0; i < terms.Count; ++i)
                {
                    if (!terms[i].FixEqFuncDefs(ref pEvalData))
                        return null;
                }

                int simpTermCount = 0;
                foreach (var term in terms)
                {
                    if (term.Sides.Count > 2)
                        return null;
                    if (term.Left == null || term.Right == null)
                    {
                        simpTermCount++;
                    }
                }

                if (simpTermCount <= 1)
                {
                    EquationSystemTermType estt = new EquationSystemTermType(terms, lexemeTables, solveVars);
                    if (estt.Init(ref pEvalData))
                        return estt;
                }
                else if (simpTermCount == terms.Count)
                {
                    EquationSystemTermType estt = new EquationSystemTermType(terms, lexemeTables, solveVars);
                    if (estt.InitGraphingOnly(ref pEvalData))
                        return estt;
                }
                return null;
            }
        }

        private static void RecheckSolveVars(ref Dictionary<string, int> solveVars, List<EquationSet> terms)
        {
            List<string> keys = solveVars.Keys.ToList();
            for (int i = 0; i < keys.Count; ++i)
            {
                bool contains = false;
                foreach (EquationSet eqSet in terms)
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