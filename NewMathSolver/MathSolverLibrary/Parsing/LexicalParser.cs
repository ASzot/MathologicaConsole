using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lexeme = MathSolverWebsite.MathSolverLibrary.TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>;
using LexemeTable = System.Collections.Generic.List<
MathSolverWebsite.MathSolverLibrary.TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>>;

namespace MathSolverWebsite.MathSolverLibrary.Parsing
{
    

    internal class LexicalParser
    {
        private TermType.EvalData p_EvalData;
        private Dictionary<string, LexemeTable> _vectorStore = new Dictionary<string, LexemeTable>();
        private Dictionary<string, List<LexemeTable>> _funcStore = new Dictionary<string, List<LexemeTable>>();
        private int _vecStoreIndex = 0;
        private bool _fixIntegrals = true;

        public const string IDEN_MATCH = @"alpha|beta|gamma|delta|epsilon|varepsilon|zeta|eta|theta|vartheta|iota|kappa|lambda|mu|nu|xi|rho|sigma|tau|usilon|phi|varphi|" +
                "chi|psi|omega|Gamma|Theta|Lambda|Xi|Phsi|Psi|Omega|[a-zA-Z]";

        public const string NUM_MATCH = @"((?<![\d])\.[\d]+)|([\d]+([.][\d]+)?)";

        public const string OPTIONAL_REALIMAG_NUM_PATTERN = @"^(-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?$";
        public const string REAL_NUM_PATTERN = @"-?[\d]+([.,][\d]+)?";
        public const string REALIMAG_FRAC_NUM_PATTERN = @"^-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?(\/-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?)?$";
        public const string REALIMAG_NUM_PATTERN = @"^-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?$";
        public const string REALIMAG_OPPOW_NUM_PATTERN = @"^-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?(\^\(-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?\))?$";

        public const int DIFF_RULE_INDEX = 24;

        private TypePair<string, LexemeType>[] _rulesets =
        {
            new TypePair<string, LexemeType>(@"\+|\-|\^|\/|\*|circ|text\(P\)|text\(C\)", LexemeType.Operator),
            new TypePair<string, LexemeType>(@"\=", LexemeType.EqualsOp),
            new TypePair<string, LexemeType>(@"i", LexemeType.I_Number),
            new TypePair<string, LexemeType>(@"pi|e", LexemeType.Constant),
            new TypePair<string, LexemeType>(@"\(", LexemeType.StartPara),
            new TypePair<string, LexemeType>(@"\)", LexemeType.EndPara),
            new TypePair<string, LexemeType>("(?<!lo)((" + IDEN_MATCH + ")((_((" + IDEN_MATCH + ")|(" + NUM_MATCH + ")))|((_\\((.+?)\\))))?)", LexemeType.Identifier),
            new TypePair<string, LexemeType>("(" + IDEN_MATCH + @")\((" + IDEN_MATCH + @")\)", LexemeType.FunctionDef),
            new TypePair<string, LexemeType>(NUM_MATCH, LexemeType.Number),
            new TypePair<string, LexemeType>(@"\!|asin|arcsin|acos|arccos|atan|arctan|acsc|arccsc|asec|arcsec|acot|arccot|sin|cos|tan|csc|sec|cot|log_|log|ln|" + 
                "sqrt|root|frac|det|div|curl|nabla*|nabla" + Equation.Operators.CrossProductOp.IDEN + "|nabla|" + 
                Equation.Operators.CrossProductOp.IDEN, LexemeType.Function),
            new TypePair<string, LexemeType>(@"\[", LexemeType.StartBracket),
            new TypePair<string, LexemeType>(@"\]", LexemeType.EndBracket),
            new TypePair<string, LexemeType>(@"\|", LexemeType.Bar),
            new TypePair<string, LexemeType>(@"\;", LexemeType.EquationSeperator),
            new TypePair<string, LexemeType>(@">", LexemeType.Greater),
            new TypePair<string, LexemeType>(@">=", LexemeType.GreaterEqual),
            new TypePair<string, LexemeType>(@"<", LexemeType.Less),
            new TypePair<string, LexemeType>(@"<=", LexemeType.LessEqual),
            new TypePair<string, LexemeType>(@",", LexemeType.Comma),
            new TypePair<string, LexemeType>(MathSolver.PLAIN_TEXT ? 
                                             @"\(((partial)|d)(\^(" + NUM_MATCH + "))?(" + IDEN_MATCH + @")?\)\/\(((partial)|d)(" + IDEN_MATCH + @")(\^(" + NUM_MATCH + @"))?\)" :
                                             @"frac\(((partial)|d)(\^(" + NUM_MATCH + "))?(" + IDEN_MATCH + @")?\)\(((partial)|d)(" + IDEN_MATCH + @")(\^(" + NUM_MATCH + @"))?\)",
                                             LexemeType.Derivative),
            new TypePair<string, LexemeType>("(" + IDEN_MATCH + @")(((\')+)|((\^((" + NUM_MATCH + @")|(" + IDEN_MATCH + @")))\((" + IDEN_MATCH + @")\)))", LexemeType.FuncDeriv),
            new TypePair<string, LexemeType>(MathSolver.PLAIN_TEXT ? 
                                             @"sum_\((" + IDEN_MATCH + ")=(" + NUM_MATCH + @")\)\^\((" + NUM_MATCH + @")\)" : 
                                             @"(sum\^(\()?(" + NUM_MATCH + @")(\))?_\((" + IDEN_MATCH + ")=(" + NUM_MATCH + @")\))", 
                                             LexemeType.Summation),
			new TypePair<string, LexemeType>(@"lim_\((" + IDEN_MATCH + @")to(\-)?((inf)|(" + NUM_MATCH + @")|(" + IDEN_MATCH + @"))\)", 
                                             LexemeType.Limit),
            new TypePair<string, LexemeType>(@"(int_\()|(int_((" + NUM_MATCH + ")|(" + IDEN_MATCH + ")|(pi)))|(int)", LexemeType.Integral),
            new TypePair<string, LexemeType>(@"\$d(" + IDEN_MATCH + @")", LexemeType.Differential),
            new TypePair<string, LexemeType>(@"(sum)|(inf)|(lim)", LexemeType.ErrorType),
        };

        private class MatchTolken
        {
            public int Length;
            public int Index;
            public string Value;

            public MatchTolken(int length, int index, string value)
            {
                Length = length;
                Index = index;
                Value = value;
            }

            public MatchTolken(Match match)
                : this(match.Length, match.Index, match.Value)
            {

            }

            public override string ToString()
            {
                return Value;
            }
        }

        public LexicalParser(TermType.EvalData pEvalData)
        {
            p_EvalData = pEvalData;
        }

        private void ResetDiffParsing()
        {
            _rulesets[DIFF_RULE_INDEX] = new TypePair<string, LexemeType>(@"\$d(" + IDEN_MATCH + @")", LexemeType.Differential);
        }

        public static LexemeTable CompoundLexemeTable(LexemeTable lt0, LexemeTable lt1)
        {
            LexemeTable finalTable = new LexemeTable();
            finalTable.AddRange(lt0);
            finalTable.Add(new Lexeme(LexemeType.EqualsOp, "="));
            finalTable.AddRange(lt1);

            return finalTable;
        }

        public List<TypePair<LexemeType, string>> CreateLexemeTable(string inputStr, ref List<string> pParseErrors)
        {
            inputStr = CleanTexInput(inputStr);

            List<TypePair<LexemeType, MatchTolken>> tolkenMatches = new List<TypePair<LexemeType, MatchTolken>>();
            foreach (TypePair<string, LexemeType> rulset in _rulesets)
            {
                MatchCollection matches = Regex.Matches(inputStr, rulset.Data1);
                foreach (Match match in matches)
                    tolkenMatches.Add(new TypePair<LexemeType, MatchTolken>(rulset.Data2, new MatchTolken(match)));
            }

            tolkenMatches = (from t in tolkenMatches
                             orderby t.Data2.Index
                             select t).ToList();


            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                var tolkenMatch = tolkenMatches[i];

                if (tolkenMatch.Data1 == LexemeType.Integral)
                {
                    string tolkenStr = tolkenMatch.Data2.Value;

                    if (tolkenStr.EndsWith("("))
                    {
                        // Swallow up the next tolkens leading to the last paranthese.
                        int endIndex = -1;
                        int depth = 1;

                        int minIndex = tolkenMatch.Data2.Index + tolkenMatch.Data2.Length;

                        for (int j = i + 1; j < tolkenMatches.Count; ++j)
                        {
                            var compareTolken = tolkenMatches[j];
                            if (compareTolken.Data2.Index < minIndex)
                            {
                                tolkenMatches.RemoveAt(j--);
                                continue;
                            }
                            if (compareTolken.Data2.Value == ")")
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    endIndex = j;
                                    break;
                                }
                            }
                            else if (compareTolken.Data2.Value == "(")
                                depth++;
                        }

                        if (endIndex == -1)
                            return null;

                        var tolkenRange = tolkenMatches.GetRange(i + 1, endIndex - i);
                        tolkenMatches.RemoveRange(i + 1, endIndex - i);
                        for (int j = 0; j < tolkenRange.Count; ++j)
                        {
                            var tolken = tolkenRange[j];
                            for (int k = j + 1; k < tolkenRange.Count; ++k)
                            {
                                var compTolken = tolkenRange[k];
                                if (compTolken.Data2.Index < (tolken.Data2.Index + tolken.Data2.Length))
                                {
                                    tolkenRange.RemoveAt(k--);
                                    continue;
                                }
                            }
                        }

                        string addStr = "";
                        foreach (var tolken in tolkenRange)
                        {
                            addStr += tolken.Data2.Value;
                        }

                        tolkenMatches[i].Data2.Value += addStr;
                    }
                }
            }


            List<TypePair<LexemeType, MatchTolken>> tolkensToRemove = new List<TypePair<LexemeType, MatchTolken>>();
            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                var tolken = tolkenMatches[i];
                int length = tolken.Data2.Length;
                int startIndex = tolken.Data2.Index;
                if (length > 1)
                {
                    // This could be a more abstract description.
                    for (int j = 0; j < length; ++j)
                    {
                        int index = startIndex + j;

                        // Does another element possess the same index?
                        for (int k = 0; k < tolkenMatches.Count; ++k)
                        {
                            if (k == i)
                                continue;
                            var compareTolken = tolkenMatches[k];
                            if (compareTolken.Data2.Index == index &&
                                !((compareTolken.Data1 == LexemeType.FunctionDef || compareTolken.Data1 == LexemeType.Derivative) &&
                                compareTolken.Data2.Value.StartsWith("g(")))
                            {
                                tolkensToRemove.Add(compareTolken);
                                break;
                            }
                        }
                    }
                }
            }

            //IMPROVE:
            // Make one for loop allowing the user to change the behavior with the replacement of the 'i' and 'e' literals.

            // Remove any variable 'e' occurances and replace with the constant 'e'.
            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                var tolkenMatch = tolkenMatches[i];
                if (tolkenMatch.Data1 == LexemeType.Constant && tolkenMatch.Data2.Value == "e")
                {
                    // Is there an identifier with this same index?
                    for (int j = 0; j < tolkenMatches.Count; ++j)
                    {
                        if (j == i)
                            continue;
                        var compareTolkenMatch = tolkenMatches[j];
                        if (compareTolkenMatch.Data1 == LexemeType.Identifier && compareTolkenMatch.Data2.Value == "e"
                            && compareTolkenMatch.Data2.Index == tolkenMatch.Data2.Index)
                        {
                            tolkensToRemove.Add(compareTolkenMatch);
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                var tolkenMatch = tolkenMatches[i];
                // Is there another token out there which has the same index?
                if (tolkenMatch.Data1 == LexemeType.I_Number && tolkenMatch.Data2.Value == "i")
                {
                    for (int j = 0; j < tolkenMatches.Count; ++j)
                    {
                        if (j == i)
                            continue;

                        var compareTolkenMatch = tolkenMatches[j];
                        if (compareTolkenMatch.Data1 == LexemeType.Identifier && compareTolkenMatch.Data2.Value == "i"
                            && compareTolkenMatch.Data2.Index == tolkenMatch.Data2.Index)
                        {
                            tolkensToRemove.Add(compareTolkenMatch);
                            break;
                        }
                    }
                }
            }

            foreach (var tolkenToRemove in tolkensToRemove)
            {
                tolkenMatches.Remove(tolkenToRemove);
            }

            // Order the tokens in the order they appear in the string.
            var orderedTolkens = from tolkenMatch in tolkenMatches
                                 orderby tolkenMatch.Data2.Index
                                 select new TypePair<LexemeType, string>(tolkenMatch.Data1, tolkenMatch.Data2.Value);

            var orderedTolkensList = orderedTolkens.ToList();
            for (int i = 0; i < orderedTolkensList.Count; ++i)
            {
                if (orderedTolkensList[i].Data1 == LexemeType.FunctionDef && i > 0 &&
                    (orderedTolkensList[i - 1].Data1 == LexemeType.Function || 
                    orderedTolkensList[i - 1].Data1 == LexemeType.Derivative || 
                    orderedTolkensList[i - 1].Data1 == LexemeType.Integral))
                {
                    string funcDef = orderedTolkensList[i].Data2;
                    string func = orderedTolkensList[i - 1].Data2;

                    // Actually get the starting identifier not character.
                    int funcParaStartIndex = funcDef.IndexOf("(");
                    int funcParaEndIndex = funcDef.IndexOf(")");
                    string funcStartStr = funcDef.Substring(0, funcParaStartIndex);
                    string funcParamStr = funcDef.Substring(funcParaStartIndex + 1, funcParaEndIndex - (funcParaStartIndex + 1));

                    if (func.EndsWith(funcStartStr))
                    {
                        orderedTolkensList.RemoveAt(i);
                        orderedTolkensList.Insert(i, new Lexeme(LexemeType.StartPara, "("));
                        LexemeType lt;
                        if (funcParamStr == "e")
                            lt = LexemeType.Constant;
                        else
                            lt = LexemeType.Identifier;
                        orderedTolkensList.Insert(i + 1, new Lexeme(lt, funcParamStr));
                        orderedTolkensList.Insert(i + 2, new Lexeme(LexemeType.EndPara, ")"));
                        i += 2;
                    }
                    else if (func.EndsWith("_"))
                    {
                        orderedTolkensList.RemoveAt(i);
                        orderedTolkensList.Insert(i, new Lexeme(LexemeType.Identifier, funcStartStr));
                        orderedTolkensList.Insert(i + 1, new Lexeme(LexemeType.StartPara, "("));
                        orderedTolkensList.Insert(i + 2, new Lexeme(LexemeType.Identifier, funcParamStr));
                        orderedTolkensList.Insert(i + 3, new Lexeme(LexemeType.EndPara, ")"));
                    }
                }
                else if (orderedTolkensList[i].Data1 == LexemeType.FuncDeriv && i > 0 && (orderedTolkensList[i - 1].Data1 == LexemeType.Function))
                {
                    if (orderedTolkensList[i].Data2.Length < 5)
                        continue;

                    int funcParaStartIndex = orderedTolkensList[i].Data2.IndexOf("(");
                    if (funcParaStartIndex == -1)
                        continue;
                    int funcParaEndIndex = orderedTolkensList[i].Data2.IndexOf(")");
                    if (funcParaStartIndex == -1)
                        continue;
                    int raiseIndex = orderedTolkensList[i].Data2.IndexOf("^");
                    if (raiseIndex == -1)
                        continue;
                    string funcStartStr = orderedTolkensList[i].Data2.Substring(0, raiseIndex);
                    string raisedStr = orderedTolkensList[i].Data2.Substring(raiseIndex + 1, funcParaStartIndex - (raiseIndex + 1));
                    string funcParamStr = orderedTolkensList[i].Data2.Substring(funcParaStartIndex + 1, funcParaEndIndex - (funcParaStartIndex + 1));

                    if (!orderedTolkensList[i - 1].Data2.EndsWith(funcStartStr))
                        continue;

                    orderedTolkensList.RemoveAt(i);
                    orderedTolkensList.Insert(i, new Lexeme(LexemeType.Operator, "^"));
                    orderedTolkensList.Insert(i + 1, new Lexeme(LexemeType.Number, raisedStr));
                    orderedTolkensList.Insert(i + 2, new Lexeme(LexemeType.StartPara, "("));
                    orderedTolkensList.Insert(i + 3, new Lexeme(LexemeType.Identifier, funcParamStr));
                    orderedTolkensList.Insert(i + 4, new Lexeme(LexemeType.EndPara, ")"));
                }
            }

            for (int i = 0; i < orderedTolkensList.Count - 1; ++i)
            {
                if (orderedTolkensList[i].Data1 == LexemeType.Identifier && orderedTolkensList[i + 1].Data1 == LexemeType.StartPara)
                {
                    if (true) //p_EvalData.FuncDefs.IsFuncDefined(orderedTolkensList[i].Data2))
                    {
                        // Find the ending paranthese.
                        int endIndex = -1;
                        int depth = 0;
                        for (int j = i + 2; j < orderedTolkensList.Count; ++j)
                        {
                            if (orderedTolkensList[j].Data1 == LexemeType.EndPara)
                            {
                                if (depth == 0)
                                {
                                    endIndex = j;
                                    break;
                                }

                                depth--;
                            }
                            else if (orderedTolkensList[j].Data1 == LexemeType.StartPara)
                            {
                                depth++;
                            }
                        }

                        if (endIndex == -1)
                            break;

                        int depthCount = 0;
                        int commaCount = 0;
                        for (int j = i + 2; j < endIndex - i; ++j)
                        {
                            if (orderedTolkensList[j].Data1 == LexemeType.StartPara)
                                depthCount++;
                            else if (orderedTolkensList[j].Data1 == LexemeType.EndPara)
                                depthCount--;
                            if (orderedTolkensList[j].Data1 == LexemeType.Comma && depthCount == 0)
                                commaCount++;
                        }

                        if (commaCount == 0) //p_EvalData.FuncDefs.IsValidFuncCall(orderedTolkensList[i].Data2, commaCount + 1))
                        {
                            // Replace with a function call.
                            orderedTolkensList[i].Data1 = LexemeType.FuncIden;
                            orderedTolkensList[i + 1].Data1 = LexemeType.FuncArgStart;
                            orderedTolkensList[endIndex].Data1 = LexemeType.FuncArgEnd;
                        }
                    }
                }
            }

            for (int i = 0; i < orderedTolkensList.Count; ++i)
            {
                var tolken = orderedTolkensList[i];
                if (!MathSolver.PLAIN_TEXT)
                {
                    if (tolken.Data1 == LexemeType.Derivative)
                    {
                        if (i > 0)
                        {
                            orderedTolkensList.RemoveAt(i - 1);
                        }
                        if (i < orderedTolkensList.Count && !tolken.Data2.Contains("^"))
                            orderedTolkensList.RemoveAt(i);
                    }
                }

                if (tolken.Data1 == LexemeType.Limit && i < orderedTolkensList.Count - 1 && orderedTolkensList[i + 1].Data2 == "lim")
                    orderedTolkensList.RemoveAt(i + 1);

                if (tolken.Data1 == LexemeType.Summation && i < orderedTolkensList.Count - 1 && orderedTolkensList[i + 1].Data2 == "sum")
                    orderedTolkensList.RemoveAt(i + 1);
                else if (tolken.Data1 == LexemeType.Limit && i < orderedTolkensList.Count - 1 && orderedTolkensList[i + 1].Data2 == "inf")
                    orderedTolkensList.RemoveAt(i + 1);
            }

            // Make sure there are no lone summations.
            foreach (var tolken in orderedTolkensList)
            {
                if (tolken.Data1 == LexemeType.ErrorType)
                {
                    if (tolken.Data2 == "sum")
                        pParseErrors.Add("Incorrectly formatted summation.");
                    else if (tolken.Data2 == "inf")
                        pParseErrors.Add("Incorrect usage of infinity.");
                    else if (tolken.Data2 == "lim")
                        pParseErrors.Add("Incorrect usage of limit.");

                    return null;
                }
            }

            _funcStore = new Dictionary<string, List<LexemeTable>>();
            if (!CreateMultiVariableFuncs(ref orderedTolkensList))
                return null;

            return orderedTolkensList;
        }

        /// <summary>
        /// Checks that the coefficients are always at the front of the algebra group.
        /// 4x^(2)y will pass x^(2)4y will not.
        /// </summary>
        /// <returns></returns>
        private static bool CheckCoeffCorrectness(LexemeTable lexTable)
        {
            for (int i = 0; i < lexTable.Count; ++i)
            {
                if (lexTable[i].Data1 == LexemeType.Operator && lexTable[i].Data2 == "^" &&
                    i < lexTable.Count - 1 &&
                    lexTable[i + 1].Data1 == LexemeType.Number && lexTable[i + 1].Data2.Length > 1)
                    return false;
            }

            return true;
        }

        public List<EqSet> ParseInput(string inputStr, out List<LexemeTable> lexemeTables, ref List<string> pParseErrors)
        {
            _vectorStore = new Dictionary<string, LexemeTable>();
            _vecStoreIndex = 0;
            string[] equationSets = inputStr.Split(';');
            lexemeTables = new List<LexemeTable>();

            List<EqSet> eqSets = new List<EqSet>();

            foreach (string equationSet in equationSets)
            {
                LexemeTable setLexemeTable = CreateLexemeTable(equationSet, ref pParseErrors);

                if (setLexemeTable == null || setLexemeTable.Count == 0)
                    return null;

                if (!MathSolver.PLAIN_TEXT && !CheckCoeffCorrectness(setLexemeTable))
                {
                    pParseErrors.Add("Invalid number placement.");
                    return null;
                }
                if (setLexemeTable == null)
                    return null;
                if (!LexemeTableContainsComparisonOp(setLexemeTable))
                {
                    // We just have a simplify equation.
                    lexemeTables.Add(setLexemeTable);

                    AlgebraTerm algebraTerm = LexemeTableToAlgebraTerm(setLexemeTable, ref pParseErrors, true);
                    if (algebraTerm == null)
                        return null;
                    ExComp final = algebraTerm.RemoveRedundancies(true);
                    if (final is AgOp)
                    {
                        return null;
                    }
                    if (final is AlgebraTerm)
                    {
                        AlgebraTerm finalTerm = final as AlgebraTerm;
                        final = finalTerm.WeakMakeWorkable(ref pParseErrors);
                        if (final == null)
                            return null;
                    }

                    eqSets.Add(new EqSet(final, equationSet));
                    continue;
                }
                List<LexemeType> solveTypes = new List<LexemeType>();
                LexemeTable[] parsedTables = SplitLexemeTable(setLexemeTable, out solveTypes, ref pParseErrors);
                if (parsedTables == null || parsedTables.Length == 0 || solveTypes.Count == 0)
                    return null;

                LexemeType solveType = solveTypes[0];

                if (parsedTables == null)
                {
                    return null;
                }
                if (parsedTables.Length != 2)
                {
                    // We have an inequality with multiple sides.
                    // For instance -3 < 2x < 5
                    List<ExComp> parsedTerms = new List<ExComp>();
                    foreach (LexemeTable lt in parsedTables)
                    {
                        if (lt.Count == 0)
                            return null;
                        lexemeTables.Add(lt);
                        if (LexemeTableContains(lt, LexemeType.EqualsOp))
                            return null;

                        AlgebraTerm algebraTerm = LexemeTableToAlgebraTerm(lt, ref pParseErrors, true);
                        if (algebraTerm == null)
                            return null;
                        ExComp final = algebraTerm.RemoveRedundancies(true);
                        if (final is AlgebraTerm)
                        {
                            AlgebraTerm finalTerm = final as AlgebraTerm;
                            final = finalTerm.WeakMakeWorkable(ref pParseErrors);
                            if (final == null)
                                return null;
                        }

                        parsedTerms.Add(final);
                    }

                    EqSet singleEqSet = new EqSet(parsedTerms, solveTypes);

                    eqSets = new List<EqSet>();
                    eqSets.Add(singleEqSet);

                    return eqSets;
                }

                ExComp[] parsedExs = new ExComp[2];

                for (int i = 0; i < parsedTables.Length; ++i)
                {
                    LexemeTable lexemeTable = parsedTables[i];
                    if (lexemeTable.Count == 0)
                        return null;
                    lexemeTables.Add(lexemeTable);
                    if (LexemeTableContains(lexemeTable, LexemeType.EqualsOp))
                        return null;

                    AlgebraTerm algebraTerm = LexemeTableToAlgebraTerm(lexemeTable, ref pParseErrors, true);
                    if (algebraTerm == null)
                        return null;
                    ExComp final = algebraTerm.RemoveRedundancies(true);
                    if (final is AlgebraTerm)
                    {
                        AlgebraTerm finalTerm = final as AlgebraTerm;
                        final = finalTerm.WeakMakeWorkable(ref pParseErrors);
                        if (final == null)
                            return null;
                    }

                    parsedExs[i] = final;
                }

                eqSets.Add(new EqSet(parsedExs[0], parsedExs[1], solveType));
            }

            return eqSets;
        }

        public static LexemeTable[] SplitLexemeTable(LexemeTable lt, out List<LexemeType> splitLexTypes, ref List<string> pParseErrors)
        {
            int eCount = GetOccurCount(lt, LexemeType.EqualsOp);
            int gCount = GetOccurCount(lt, LexemeType.Greater);
            int geCount = GetOccurCount(lt, LexemeType.GreaterEqual);
            int lCount = GetOccurCount(lt, LexemeType.Less);
            int leCount = GetOccurCount(lt, LexemeType.LessEqual);

            splitLexTypes = new List<LexemeType>();

            if (eCount + gCount + geCount + lCount + leCount != 1)
            {
                // We might have an inequality with multiple comparison operators.
                if (eCount != 0)
                {
                    pParseErrors.Add("Cannot mix equality and inequality comparisons.");
                    return null;
                }

                var inequalityIndexPairs = GetInequalityOpIndices(lt);

                List<LexemeTable> lexemeTables = new List<LexemeTable>();
                int prevEndIndex = 0;
                foreach (var inequalityIndex in inequalityIndexPairs)
                {
                    var inequalityType = lt[inequalityIndex].Data1;
                    splitLexTypes.Add(inequalityType);

                    LexemeTable lexemeTableRange = lt.GetRange(prevEndIndex, inequalityIndex - prevEndIndex);
                    lexemeTables.Add(lexemeTableRange);
                    prevEndIndex = inequalityIndex + 1;
                }

                // Also add the ending part as it doesn't have a inequality after it.
                LexemeTable finalLtRange = lt.GetRange(prevEndIndex, lt.Count - prevEndIndex);
                lexemeTables.Add(finalLtRange);

                return lexemeTables.ToArray();
            }

            int splitIndex = -1;
            if (eCount == 1)
            {
                splitLexTypes.Add(LexemeType.EqualsOp);
                splitIndex = GetFirstIndexOccur(lt, LexemeType.EqualsOp);
            }
            else if (gCount == 1)
            {
                splitLexTypes.Add(LexemeType.Greater);
                splitIndex = GetFirstIndexOccur(lt, LexemeType.Greater);
            }
            else if (geCount == 1)
            {
                splitLexTypes.Add(LexemeType.GreaterEqual);
                splitIndex = GetFirstIndexOccur(lt, LexemeType.GreaterEqual);
            }
            else if (lCount == 1)
            {
                splitLexTypes.Add(LexemeType.Less);
                splitIndex = GetFirstIndexOccur(lt, LexemeType.Less);
            }
            else if (leCount == 1)
            {
                splitLexTypes.Add(LexemeType.LessEqual);
                splitIndex = GetFirstIndexOccur(lt, LexemeType.LessEqual);
            }
            else
                return null;

            if (splitIndex == -1)
                return null;

            LexemeTable left = lt.GetRange(0, splitIndex);
            int startIndex = splitIndex + 1;
            LexemeTable right = lt.GetRange(startIndex, lt.Count - startIndex);

            LexemeTable[] leftRight = { left, right };

            return leftRight;
        }

        private bool FixIntegralDif(ref LexemeTable lt, ref List<string> pParseErrors)
        {
            int diffCount = 0;
            int intCount = 0;
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].Data1 == LexemeType.Differential)
                    diffCount++;
                else if (lt[i].Data1 == LexemeType.Integral)
                {
                    intCount++;
                    if (i > 0 && lt[i - 1].Data1 == LexemeType.StartPara)
                        continue;
                    // Search for the corresponding differential.
                    int foundIndex = -1;
                    int depth = 0;
                    for (int j = i + 1; j < lt.Count; ++j)
                    {
                        if (lt[j].Data1 == LexemeType.Integral)
                        {
                            depth++;
                        }
                        if (lt[j].Data1 == LexemeType.Differential)
                        {
                            if (depth == 0)
                            {
                                foundIndex = j;
                                break;
                            }
                            depth--;
                        }
                    }

                    if (foundIndex < 0)
                    {
                        pParseErrors.Add("Couldn't find variable of integration.");
                        return false;
                    }

                    lt.Insert(i, new Lexeme(LexemeType.StartPara, "("));
                    lt.Insert(foundIndex + 2, new Lexeme(LexemeType.EndPara, ")"));
                    ++i;
                }
            }

            if (diffCount != intCount)
            {
                pParseErrors.Add("Lone differential.");
                return false;
            }

            return true;
        }

        private static bool ApplyOrderingToOp(string opToOrder, string[] breakingOps, LexemeTable lexemeTable)
        {
            int numBreakingOps = breakingOps.Count();
            List<int>[] indicesOfBreakingOps = new List<int>[numBreakingOps];
            for (int i = 0; i < numBreakingOps; ++i)
            {
                indicesOfBreakingOps[i] = GetIndicesOfValueInLexemeTable(lexemeTable, breakingOps[i], false);
            }

            bool allZero = true;
            foreach (var indicesOfBreakingOp in indicesOfBreakingOps)
            {
                if (indicesOfBreakingOp.Count != 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero)
                return true;

            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                Lexeme lexeme = lexemeTable[i];

                if (lexeme.Data2 == opToOrder)
                {
                    bool after = false;

                    if (i > 0)
                    {
                        Lexeme prevLexeme = lexemeTable[i - 1];
                        // To allow for the alternate notation of raising trig functions to powers.
                        if ((TrigFunction.IsValidType(prevLexeme.Data2) || InverseTrigFunction.IsValidType(prevLexeme.Data2)) && opToOrder == "^")
                            continue;
                    }

                    // Does this instance occur before any of the breaking operators?
                    foreach (List<int> indicesOfBreakingOp in indicesOfBreakingOps)
                    {
                        foreach (int index in indicesOfBreakingOp)
                        {
                            if (i > index)
                            {
                                after = true;
                                break;
                            }
                        }
                    }

                    if (!after)
                        continue;

                    if (i == 0)
                        return false;
                    Lexeme beforeLexeme = lexemeTable[i - 1];
                    if (i + 1 > lexemeTable.Count - 1)
                        return false;
                    Lexeme afterLexeme = lexemeTable[i + 1];

                    if ((beforeLexeme.Data1 == LexemeType.EndPara
                        && afterLexeme.Data1 == LexemeType.StartPara) ||
                        (beforeLexeme.Data1 == LexemeType.Bar
                        && afterLexeme.Data1 == LexemeType.Bar))
                    {
                        // This is surrounded by a group on either side.
                        // find the start of the group on the left and the end of the group on the
                        // left. Then insert a paranthese on either side.
                        // This problem with order of operations only seems to happen with multiplication.
                        if (opToOrder != "*" && (beforeLexeme.Data1 == LexemeType.EndPara))
                            continue;

                        // Search backwards.
                        int depth = 0;
                        int startIndex = -1;
                        for (int j = i; j >= 0; --j)
                        {
                            if (lexemeTable[j].Data1 == LexemeType.EndPara)
                            {
                                depth++;
                            }
                            else if (lexemeTable[j].Data1 == LexemeType.StartPara)
                            {
                                depth--;

                                if (depth == 0)
                                {
                                    startIndex = j;
                                    break;
                                }
                            }
                        }

                        if (startIndex == -1)
                            continue;

                        depth = 0;
                        int endIndex = -1;
                        for (int j = i; j < lexemeTable.Count; ++j)
                        {
                            if (lexemeTable[j].Data1 == LexemeType.StartPara)
                                depth++;
                            else if (lexemeTable[j].Data1 == LexemeType.EndPara)
                            {
                                depth--;

                                if (depth == 0)
                                {
                                    endIndex = j;
                                    break;
                                }
                            }
                        }

                        if (endIndex == -1)
                            continue;

                        if (i > 0 && lexemeTable[startIndex - 1].Data2 == "frac")
                            continue;
                        lexemeTable.Insert(startIndex, new Lexeme(LexemeType.StartPara, "("));
                        lexemeTable.Insert(endIndex + 1, new Lexeme(LexemeType.EndPara, ")"));

                        i++;

                        continue;
                    }

                    int startPos = -1;
                    // Navigate backwards.
                    for (int j = i; j >= 0; --j)
                    {
                        Lexeme searchLexeme = lexemeTable[j];
                        LexemeType type = searchLexeme.Data1;

                        bool validBreakingOp = false;
                        foreach (var indicesOfBreakingOp in indicesOfBreakingOps)
                        {
                            foreach (var indexOfBreakingOp in indicesOfBreakingOp)
                            {
                                if (indexOfBreakingOp == j)
                                    validBreakingOp = true;
                            }
                        }

                        if (validBreakingOp)
                        {
                            startPos = j + 1;
                            i++;
                            j++;
                            break;
                        }
                        else if (j == 0)
                        {
                            startPos = j;
                            i++;
                            j++;
                            break;
                        }
                    }

                    int endPos = -1;
                    // Navigate forward.
                    for (int j = i; j < lexemeTable.Count; ++j)
                    {
                        Lexeme searchLexeme = lexemeTable[j];
                        LexemeType type = searchLexeme.Data1;

                        bool validBreakingOp = false;
                        foreach (var indicesOfBreakingOp in indicesOfBreakingOps)
                        {
                            foreach (var indexOfBreakingOp in indicesOfBreakingOp)
                            {
                                if (indexOfBreakingOp == j)
                                    validBreakingOp = true;
                            }
                        }

                        if (validBreakingOp)
                        {
                            endPos = j + 1;
                            break;
                        }
                        else if (j == lexemeTable.Count - 1)
                        {
                            endPos = j + 2;
                            break;
                        }
                    }

                    if (endPos == -1 || startPos == -1)
                        continue;

                    if (i > 0 && lexemeTable[startPos].Data2 == "frac")
                        continue;
                    lexemeTable.Insert(startPos, new Lexeme(LexemeType.StartPara, "("));

                    lexemeTable.Insert(lexemeTable[endPos - 1].Data2 == "|" ? endPos - 1 : endPos, new Lexeme(LexemeType.EndPara, ")"));

                    // Recalculate the positions of indices as everything has been shifted.
                    // this could probably be optimized.

                    indicesOfBreakingOps = new List<int>[numBreakingOps];
                    for (int j = 0; j < numBreakingOps; ++j)
                    {
                        indicesOfBreakingOps[j] = GetIndicesOfValueInLexemeTable(lexemeTable, breakingOps[j], false);
                    }
                }
            }

            return true;
        }

        private bool ApplyOrderOfOperationsToLexemeTable(LexemeTable lexemeTable, ref List<string> pParseErrors, bool fixIntegrals)
        {
            //if (fixIntegrals)
            //{
            //    // Integrals and differentials screw up the entire PEMDAS process so parantheses have to be put around integrals.
            //    if (!FixIntegralDif(ref lexemeTable, ref pParseErrors))       // Will return false if there are mismatched integrals and differentials.
            //        return false;
            //}

            string[] pBreakingOps = { "*", "/", "+", "-" };
            if (!ApplyOrderingToOp("^", pBreakingOps, lexemeTable))
                return false;

            string[] mdBreakingOps = { "+", "-" };

            if (!ApplyOrderingToOp("*", mdBreakingOps, lexemeTable))
                return false;
            if (!ApplyOrderingToOp("/", mdBreakingOps, lexemeTable))
                return false;

            return true;
        }

        private string CleanTexInput(string str)
        {

            str = str.Replace(@"\cdot", "*");
            str = str.Replace(@"\left", "");
            str = str.Replace(@"\right", "");
            str = str.Replace(@"\lt", "<");
            str = str.Replace(@"\le", "<=");
            str = str.Replace(@"\gt", ">");
            str = str.Replace(@"\div", "/");
            str = str.Replace(@"\ge", ">=");
            str = str.Replace(@"\d", "$d");
            str = str.Replace("sqrt[", "root(");
            str = str.Replace('{', '(');
            str = str.Replace('}', ')');
            if (str.Contains("\\int"))
            {
                _rulesets[DIFF_RULE_INDEX] = new TypePair<string, LexemeType>("d(" + IDEN_MATCH + ")", LexemeType.Differential);
                // This is somewhat of a hack together but it should work.
                str = str.Replace("dA", "dxdy");
                str = str.Replace("dV", "dxdydz");
            }
            else
                ResetDiffParsing();

            // Another dirty technique that should probably be changed.
            str = str.Replace("\\bar(i)", "[1,0,0]");
            str = str.Replace("\\bar(j)", "[0,1,0]");
            str = str.Replace("\\bar(k)", "[0,0,1]");

            str = str.Replace("\\", "");
            str = str.Replace("->", "to");

            str = str.Replace("sin^(-1)", "asin");
            str = str.Replace("cos^(-1)", "acos");
            str = str.Replace("tan^(-1)", "atan");
            str = str.Replace("csc^(-1)", "acsc");
            str = str.Replace("sec^(-1)", "asec");
            str = str.Replace("cot^(-1)", "acot");
            str = str.Replace("infty", "inf");

            return str;
        }

        private static void FixLexemeTableUserInput(ref LexemeTable lexemeTable)
        {
            // Fixes things like 'xy' to 'x*y'.
            // Also fixes the negation operator versus minus operator problem.
            bool multipliablePreceeding = false;
            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                Lexeme lexeme = lexemeTable[i];

                // Special case for the absolute value.
                if (i > 0 && lexemeTable[i].Data1 == LexemeType.Bar && lexemeTable[i - 1].Data1 == LexemeType.Bar)
                {
                    lexemeTable.Insert(i, new Lexeme(LexemeType.Operator, "*"));
                }

                if (i > 0 && lexemeTable[i - 1].Data1 == LexemeType.EndBracket && lexemeTable[i].Data1 == LexemeType.StartBracket)
                {
                    lexemeTable.Insert(i, new Lexeme(LexemeType.Operator, Equation.Operators.CompWiseMul.IDEN));
                }

                if ((lexeme.Data1 == LexemeType.Identifier ||
                    lexeme.Data1 == LexemeType.VectorStore ||
                    lexeme.Data1 == LexemeType.I_Number ||
                    lexeme.Data1 == LexemeType.Number ||
                    lexeme.Data1 == LexemeType.Constant ||
                    lexeme.Data1 == LexemeType.StartPara ||
                    lexeme.Data1 == LexemeType.Function ||
                    lexeme.Data1 == LexemeType.Derivative ||
                    lexeme.Data1 == LexemeType.Limit ||
                    lexeme.Data1 == LexemeType.Integral ||
                    lexeme.Data1 == LexemeType.FunctionDef ||
                    lexeme.Data1 == LexemeType.FuncIden)
                    && multipliablePreceeding)
                {
                    lexemeTable.Insert(i++, new Lexeme(LexemeType.Operator, "*"));

                    if (lexeme.Data1 == LexemeType.StartPara ||
                        lexeme.Data1 == LexemeType.Function ||
                        lexeme.Data1 == LexemeType.Derivative ||
                        lexeme.Data1 == LexemeType.Limit ||
                        lexeme.Data1 == LexemeType.Integral ||
                        lexeme.Data1 == LexemeType.FunctionDef ||
                        lexeme.Data1 == LexemeType.FuncIden)
                        multipliablePreceeding = false;
                    continue;
                }

                if (lexeme.Data1 == LexemeType.Identifier ||
                    lexeme.Data1 == LexemeType.VectorStore ||
                    lexeme.Data1 == LexemeType.I_Number ||
                    lexeme.Data1 == LexemeType.Number ||
                    lexeme.Data1 == LexemeType.Constant ||
                    lexeme.Data1 == LexemeType.Number ||
                    lexeme.Data1 == LexemeType.EndPara)
                    multipliablePreceeding = true;
                else
                    multipliablePreceeding = false;
            }

            bool operatorPreceeding = true;
            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                var lexeme = lexemeTable[i];
                if (operatorPreceeding && lexeme.Data1 == LexemeType.Operator && lexeme.Data2 == "-")
                {
                    if (i == lexemeTable.Count - 1)
                    {
                        lexemeTable = null;
                        return;
                    }
                    Lexeme nextLexeme = lexemeTable[i + 1];
                    if (nextLexeme.Data1 == LexemeType.Number)
                    {
                        lexemeTable[i + 1].Data2 = nextLexeme.Data2.Insert(0, "-");
                        lexemeTable.RemoveAt(i--);
                    }
                    else
                    {
                        Lexeme negateNumLexeme = new Lexeme(LexemeType.Number, "-1");
                        Lexeme multiplyLexeme = new Lexeme(LexemeType.Operator, "*");
                        lexemeTable.RemoveAt(i);
                        lexemeTable.Insert(i, negateNumLexeme);
                        lexemeTable.Insert(i + 1, multiplyLexeme);
                    }
                    continue;
                }

                if (lexeme.Data1 == LexemeType.Operator || (lexeme.Data1 == LexemeType.StartPara))
                    operatorPreceeding = true;
                else
                    operatorPreceeding = false;
            }
        }

        private static int GetFirstIndexOccur(LexemeTable lt, LexemeType lexType)
        {
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].Data1 == lexType)
                    return i;
            }
            return -1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lexemeTable"></param>
        /// <param name="value"></param>
        /// <param name="allowGrouped">If the lexeme must be found when the depth count of the parantheses is zero.</param>
        /// <returns></returns>
        private static List<int> GetIndicesOfValueInLexemeTable(LexemeTable lexemeTable, string value, bool allowGrouped)
        {
            List<int> indices = new List<int>();
            int depth = 0;
            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                if (lexemeTable[i].Data1 == LexemeType.StartPara)
                    depth++;
                else if (lexemeTable[i].Data1 == LexemeType.EndPara)
                    depth--;
                else if (lexemeTable[i].Data2 == value && (depth == 0 || allowGrouped))
                    indices.Add(i);
            }

            return indices;
        }

        private static List<int> GetInequalityOpIndices(LexemeTable lt)
        {
            List<int> compTypeIndexPairs = new List<int>();
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].Data1 == LexemeType.Greater ||
                    lt[i].Data1 == LexemeType.GreaterEqual ||
                    lt[i].Data1 == LexemeType.Less ||
                    lt[i].Data1 == LexemeType.LessEqual)
                    compTypeIndexPairs.Add(i);
            }

            return compTypeIndexPairs;
        }

        private static int GetOccurCount(LexemeTable lt, LexemeType lexType)
        {
            int count = 0;
            foreach (Lexeme lex in lt)
            {
                if (lex.Data1 == lexType)
                    count++;
            }

            return count;
        }

        private static bool LexemeTableContains(LexemeTable lexemeTable, LexemeType lexemeType)
        {
            foreach (Lexeme lexeme in lexemeTable)
            {
                if (lexeme.Data1 == lexemeType)
                    return true;
            }

            return false;
        }

        private static bool LexemeTableContainsComparisonOp(LexemeTable lt)
        {
            foreach (Lexeme lex in lt)
            {
                var tp = lex.Data1;
                if (tp == LexemeType.EqualsOp || tp == LexemeType.Less || tp == LexemeType.LessEqual ||
                    tp == LexemeType.Greater || tp == LexemeType.GreaterEqual)
                {
                    return true;
                }
            }

            return false;
        }

        private AlgebraTerm LexemeTableToAlgebraTerm(LexemeTable lexemeTable, ref List<string> pParseErrors, bool fixIntegrals = false)
        {
            if (lexemeTable.Count > 0 && lexemeTable[0].Data1 == LexemeType.Integral)
            {
                int depth = 0;
                int endIndex = -1;

                for (int i = 0; i < lexemeTable.Count; ++i)
                {
                    if (lexemeTable[i].Data1 == LexemeType.Differential)
                    {
                        depth--;

                        if (depth == 0)
                        {
                            endIndex = i;
                            break;
                        }
                    }
                    else if (lexemeTable[i].Data1 == LexemeType.Integral)
                        depth++;
                }

                if (endIndex == -1)
                {
                    pParseErrors.Add("Missing differential.");
                    return null;
                }

                if (endIndex == lexemeTable.Count - 1)
                {
                    int index = 0;
                    ExComp parsedEx = ParseIntegral(ref index, lexemeTable, ref pParseErrors);
                    if (parsedEx == null)
                        return null;
                    return parsedEx.ToAlgTerm();
                }
            }

            FixLexemeTableUserInput(ref lexemeTable);
            if (lexemeTable == null)
                return null;

            if (!CreateVectorStore(ref lexemeTable))
                return null;

            if (!CorrectFactorials(ref lexemeTable))
                return null;

            if (!ApplyOrderOfOperationsToLexemeTable(lexemeTable, ref pParseErrors, fixIntegrals))
                return null;

            AlgebraTerm algebraTerm = new AlgebraTerm();

            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                var lexeme = lexemeTable[i];
                ExComp toAdd = LexemeToExComp(lexemeTable, ref i, ref pParseErrors);
                if (toAdd == null)
                    return null;
                algebraTerm.Add(toAdd);
            }

            return algebraTerm;
        }

        //private bool FixAbsVal(ref LexemeTable lt)
        //{
        //    for (int i = 0; i < lt.Count; ++i)
        //    {
        //        if (lt[i].Data1 == LexemeType.Bar)
        //        {
        //            int lastIndex = -1;
        //            // Find the furthermost bar.
        //            for (int j = 0; j < lt.Count; ++j)
        //            {
        //                if (lt[j].Data1 == LexemeType.Bar)
        //                {
        //                    lastIndex = j;
        //                }
        //            }
        //            if (lastIndex == -1)
        //                return false;

        //            lt.RemoveAt(i);
        //            lt.Insert(i, new Lexeme(LexemeType.StartBar, "(|"));
        //            lt.RemoveAt(lastIndex);
        //            lt.Insert(lastIndex, new Lexeme(LexemeType.EndBar, "|)"))
        //        }
        //    }

        //    return true;
        //}

        private bool CreateVectorStore(ref LexemeTable lt)
        {
            int depth = 0;
            int prevIndex = -1;
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].Data1 == LexemeType.StartBracket)
                {
                    if (depth == 0)
                        prevIndex = i;
                    depth++;
                }
                else if (lt[i].Data1 == LexemeType.EndBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        // Change into a vector store.
                        if (prevIndex == -1)
                            return false;
                        var vecStoreLt = lt.GetRange(prevIndex + 1, i - (prevIndex + 1));
                        if (vecStoreLt.Count < 3)
                            return false;

                        _vectorStore.Add(_vecStoreIndex.ToString(), vecStoreLt);
                        // Also remove the brackets.
                        int remCount = i - (prevIndex - 1);
                        lt.RemoveRange(prevIndex, remCount);
                        lt.Insert(prevIndex, new Lexeme(LexemeType.VectorStore, _vecStoreIndex.ToString()));
                        _vecStoreIndex++;
                        i -= remCount;
                        // To skip over the vector store itself.
                        i++;
                    }
                }
            }

            return true;
        }

        private bool CorrectFactorials(ref LexemeTable lt)
        {
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].Data2 != "!" || lt[i].Data1 != LexemeType.Function)
                    continue;

                Lexeme lex = lt[i];

                // Find the end of the group going in the opposite direction.
                if (i == 0)
                    return false;

                // Remove the operator that is probably preceeding it.
                if (lt[i - 1].Data1 == LexemeType.Operator)
                {
                    lt.RemoveAt(i - 1);
                    i--;
                }

                if (i != lt.Count - 1 && lt[i + 1].Data1 == LexemeType.Operator)
                {
                    lt.RemoveAt(i + 1);
                }

                if (!(lt[i - 1].Data1 == LexemeType.EndPara ||
                    lt[i - 1].Data1 == LexemeType.EndBracket ||
                    lt[i - 1].Data1 == LexemeType.Bar))
                {
                    // Just the single lexeme.
                    if (lt[i - 1].Data1 == LexemeType.Operator)
                        return false;
                    lt.RemoveAt(i);
                    lt.Insert(i - 1, lex);
                    continue;
                }

                if (i < 4)
                    return false;

                LexemeType search = lt[i - 1].Data1;
                LexemeType endType = LexemeTypeHelper.GetOpposite(search);

                int depth = 1;
                int endIndex = -1;
                for (int j = i - 2; j >= 0; --j)
                {
                    if (lt[j].Data1 == search)
                        depth++;
                    else if (lt[j].Data1 == endType)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            endIndex = j;
                            break;
                        }
                    }
                }
                if (endIndex == -1)
                    return false;

                lt.RemoveAt(i);
                lt.Insert(endIndex, lex);
            }

            return true;
        }

        private bool CreateMultiVariableFuncs(ref LexemeTable lt)
        {
            int totalVectorDepth = 0;
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].Data1 == LexemeType.Identifier)
                {
                    if (lt[i].Data2.Contains("_"))
                    {
                        // This could be the partial derivative notation of F_x.
                        string[] split = lt[i].Data2.Split('_');
                        string funcIden = split[0];
                        var funcDef = p_EvalData.FuncDefs.GetFuncDef(funcIden);
                        if (funcDef != null)
                        {
                            // The next part should be the variable with respect to.
                            if (split[1].StartsWith("(") && split.Length > 3)
                                split[1] = split[1].Substring(1, split[1].Length - 2);
                            if (funcDef.IsArg(split[1]))
                            {
                                Lexeme insertLex = new Lexeme(LexemeType.Derivative, MathSolver.PLAIN_TEXT ?
                                    "(partial" + funcIden + ")/(partial" + split[1] + ")" :
                                    "\\frac(partial" + funcIden + ")(partial" + split[1] + ")");
                                lt.RemoveAt(i);
                                lt.Insert(i, insertLex);
                            }
                        }
                    }
                }
                else if (lt[i].Data1 == LexemeType.StartBracket)
                    totalVectorDepth++;
                else if (lt[i].Data1 == LexemeType.EndBracket)
                    totalVectorDepth--;
                if (lt[i].Data1 != LexemeType.Comma || totalVectorDepth != 0)
                    continue;

                if (i < 3)
                    return false;

                // Back up to the start of the function declaration.
                int funcStart = -1;
                // The comma is treated like a end paranthese.
                int depth = -1;
                // Avoid commas from vectors.
                int vectorDepth = 0;
                for (int j = i - 1; j >= 0; --j)
                {
                    if ((lt[j].Data1 == LexemeType.StartPara || lt[j].Data1 == LexemeType.FuncArgStart) && vectorDepth == 0)
                    {
                        depth++;
                        if (depth == 0)
                        {
                            funcStart = j;
                            break;
                        }
                    }
                    else if ((lt[j].Data1 == LexemeType.EndPara || lt[j].Data1 == LexemeType.FuncArgEnd) && vectorDepth == 0)
                        depth--;
                    else if (lt[j].Data1 == LexemeType.StartBracket)
                        vectorDepth++;
                    else if (lt[j].Data1 == LexemeType.EndBracket)
                        vectorDepth--;
                }

                // The function start cannot be zero as there needs to be an identifier as well.
                if (funcStart <= 0)
                    return false;

                Lexeme iden = lt[funcStart - 1];
                if ((i - 1) > 0 && iden.Data1 == LexemeType.Operator && iden.Data2 == "*")
                    iden = lt[funcStart - 2];

                if (iden.Data1 != LexemeType.Identifier && iden.Data1 != LexemeType.FuncIden)
                    return false;

                string funcName = iden.Data2;

                List<LexemeTable> args = new List<LexemeTable>();

                var firstLexRange = lt.GetRange(funcStart + 1, i - (funcStart + 1));
                args.Add(firstLexRange);

                int funcEnd = -1;
                // The comma now acts like a start paranthese.
                depth = 1;
                int prevCommaIndex = i;
                vectorDepth = 0;
                for (int j = i + 1; j < lt.Count; ++j)
                {
                    if (depth == 1 && lt[j].Data1 == LexemeType.Comma && vectorDepth == 0)
                    {
                        var lexRange = lt.GetRange(prevCommaIndex + 1, j - (prevCommaIndex + 1));
                        args.Add(lexRange);
                        prevCommaIndex = j;
                    }
                    else if (lt[j].Data1 == LexemeType.StartBracket)
                        vectorDepth++;
                    else if (lt[j].Data1 == LexemeType.EndBracket)
                        vectorDepth--;
                    else if ((lt[j].Data1 == LexemeType.StartPara || lt[j].Data1 == LexemeType.FuncArgStart) &&
                        vectorDepth == 0)
                    {
                        depth++;
                    }
                    else if ((lt[j].Data1 == LexemeType.EndPara || lt[j].Data1 == LexemeType.FuncArgEnd) &&
                        vectorDepth == 0)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            var lexRange = lt.GetRange(prevCommaIndex + 1, j - (prevCommaIndex + 1));
                            args.Add(lexRange);
                            funcEnd = j;
                            break;
                        }
                    }
                }

                if (funcEnd == -1)
                    return false;

                lt.RemoveRange(funcStart - 1, funcEnd - (funcStart - 2));

                lt.Insert(funcStart - 1, new Lexeme(LexemeType.MultiVarFuncStore, funcName));
                _funcStore[funcName] = args;
                i = funcStart;
            }

            return true;
        }

        private ExComp LexemeToExComp(List<TypePair<LexemeType, string>> lexemeTable, ref int currentIndex, ref List<string> pParseErrors)
        {
            var lexeme = lexemeTable[currentIndex];
            int endIndex = -1;
            int depth = 0;
            int startIndex;
            AlgebraTerm algebraTerm;
            LexemeTable algebraTermLexemeTable;
            switch (lexeme.Data1)
            {
                case LexemeType.VectorStore:
                    return ParseVector(ref currentIndex, lexemeTable, ref pParseErrors);
                case LexemeType.MultiVarFuncStore:
                    return ParseMultiVarFunc(ref currentIndex, lexemeTable, ref pParseErrors);

                case LexemeType.Function:
                    endIndex = -1;
                    depth = 0;

                    if (lexeme.Data2 == "log_")
                        return ParseLogBaseInner(ref currentIndex, lexemeTable, ref pParseErrors);
                    if (lexeme.Data2 == "root")
                        return ParseRootInner(ref currentIndex, lexemeTable, ref pParseErrors);
                    else if (lexeme.Data2 == "frac")
                        return ParseFraction(ref currentIndex, lexemeTable, ref pParseErrors);

                    if (TrigFunction.IsValidType(lexeme.Data2) ||
                        InverseTrigFunction.IsValidType(lexeme.Data2))
                    {
                        if (currentIndex + 2 > lexemeTable.Count - 1)
                            return null;

                        if (lexemeTable[currentIndex + 1].Data1 == LexemeType.Operator)
                        {
                            // The sinusodal function is in the form.
                            currentIndex += 2;
                            ExComp power = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
                            if (power == null)
                                return null;

                            if (currentIndex + 1 > lexemeTable.Count - 1)
                                return null;
                            currentIndex += 2;
                            ExComp inner = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
                            if (inner == null)
                                return null;

                            var trigFunc = Equation.Functions.BasicAppliedFunc.Parse(lexeme.Data2, inner, ref pParseErrors);
                            if (trigFunc == null)
                                return null;

                            if (power is AlgebraTerm)
                                power = (power as AlgebraTerm).RemoveRedundancies();

                            if (Number.NegOne.IsEqualTo(power) && trigFunc is TrigFunction)
                            {
                                return (trigFunc as TrigFunction).GetInverseOf();
                            }

                            return new AlgebraTerm(trigFunc, new Equation.Operators.PowOp(), power);
                        }
                    }

                    for (int i = currentIndex + 2; i < lexemeTable.Count; ++i)
                    {
                        var compareLexeme = lexemeTable[i];
                        if (compareLexeme.Data1 == LexemeType.StartPara)
                            depth++;
                        if (compareLexeme.Data1 == LexemeType.EndPara)
                        {
                            if (depth == 0)
                            {
                                endIndex = i;
                                depth = -1;
                                break;
                            }
                            depth--;
                        }
                    }

                    if (depth != -1 || endIndex == -1)
                    {
                        if ((lexeme.Data2 == "!" || lexeme.Data2.StartsWith("nabla")) && currentIndex != lexemeTable.Count - 1)
                        {
                            // Just parse the next lexeme.
                            LexemeTable lt = new LexemeTable();

                            lt.Add(lexemeTable[currentIndex + 1]);
                            int tmpCurIndex = 0;
                            ExComp innerEx = LexemeToExComp(lt, ref tmpCurIndex, ref pParseErrors);

                            currentIndex = currentIndex + 1;
                            if (lexeme.Data2 == "!")
                                return new FactorialFunction(innerEx);
                            else
                            {
                                string parseLexeme;
                                if (lexeme.Data2.Contains(Equation.Operators.CrossProductOp.IDEN))
                                    parseLexeme = "curl";
                                else if (lexeme.Data2.Contains("*"))
                                    parseLexeme = "div";
                                else
                                {
                                    // Gradient.
                                    if (innerEx is Equation.Structural.LinearAlg.ExMatrix)
                                    {
                                        pParseErrors.Add("Cannot take the gradient of a vector.");
                                        return null;
                                    }
                                    return new Equation.Functions.Calculus.Vector.GradientFunc(innerEx);
                                }

                                return Equation.Functions.BasicAppliedFunc.Parse(parseLexeme, innerEx, ref pParseErrors);
                            }
                        }
                        return null;
                    }

                    startIndex = currentIndex + 2;
                    algebraTermLexemeTable = lexemeTable.GetRange(startIndex, endIndex - startIndex);
                    currentIndex = endIndex;
                    if (algebraTermLexemeTable.Count == 0)
                        return null;
                    algebraTerm = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors);
                    if (algebraTerm == null)
                        return null;

                    var func = Equation.Functions.BasicAppliedFunc.Parse(lexeme.Data2, algebraTerm, ref pParseErrors);
                    return func;

                case LexemeType.FunctionDef:
                    FunctionDefinition funcDef = new FunctionDefinition();
                    if (!funcDef.Parse(lexeme.Data2))
                        return null;
                    return funcDef;

                case LexemeType.FuncIden:
                    FunctionDefinition callFunc = new FunctionDefinition();

                    List<LexemeTable> args = new List<LexemeTable>();

                    depth = 0;
                    for (int i = currentIndex + 2; i < lexemeTable.Count; ++i)
                    {
                        if (lexemeTable[i].Data1 == LexemeType.FuncArgStart)
                            depth++;
                        else if (lexemeTable[i].Data1 == LexemeType.FuncArgEnd)
                        {
                            if (depth == 0)
                            {
                                endIndex = i;
                                break;
                            }
                            depth--;
                        }
                    }

                    if (endIndex == -1)
                        return null;

                    if (endIndex < currentIndex + 3)
                        return null;

                    startIndex = currentIndex + 2;
                    LexemeTable funcCallLt = lexemeTable.GetRange(startIndex, endIndex - startIndex);

                    currentIndex = endIndex;

                    // Split the func call into the arguments which are seperated by commas.
                    int argStartIndex = 0;
                    for (int i = 0; i < funcCallLt.Count; ++i)
                    {
                        if (funcCallLt[i].Data1 == LexemeType.Comma)
                        {
                            args.Add(funcCallLt.GetRange(argStartIndex, i - argStartIndex));
                            argStartIndex = i;
                        }
                    }

                    args.Add(funcCallLt.GetRange(argStartIndex, funcCallLt.Count - argStartIndex));

                    ExComp[] argExs = new ExComp[args.Count];
                    for (int i = 0; i < args.Count; ++i)
                    {
                        argExs[i] = LexemeTableToAlgebraTerm(args[i], ref pParseErrors);
                        if (argExs[i] == null)
                            return null;
                    }

                    FunctionDefinition tmp = p_EvalData.FuncDefs.GetFuncDef(lexeme.Data2);
                    if (tmp == null)
                        return null;

                    FunctionDefinition funcCall = (FunctionDefinition)tmp.Clone();

                    if (funcCall.InputArgCount != argExs.Length)
                        return null;

                    funcCall.CallArgs = argExs;
                    return funcCall;

                case LexemeType.Bar:
                    endIndex = -1;
                    bool hitBar = false;
                    for (int i = currentIndex + 1; i < lexemeTable.Count; ++i)
                    {
                        var compareLexeme = lexemeTable[i];
                        if (compareLexeme.Data1 == LexemeType.Bar)
                        {
                            endIndex = i;
                            hitBar = true;
                            break;
                        }
                    }

                    if (!hitBar || endIndex == -1)
                        return null;
                    startIndex = currentIndex + 1;
                    algebraTermLexemeTable = lexemeTable.GetRange(startIndex, endIndex - startIndex);
                    currentIndex = endIndex;
                    if (algebraTermLexemeTable.Count == 0)
                        return null;
                    if (algebraTermLexemeTable.Count == 1 && algebraTermLexemeTable[0].Data1 == LexemeType.Operator &&
                        algebraTermLexemeTable[0].Data2 == "*")
                        return null;
                    algebraTerm = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors);
                    if (algebraTerm == null)
                        return null;

                    var absVal = new Equation.Functions.AbsValFunction(algebraTerm);
                    return absVal;

                case LexemeType.StartPara:
                    endIndex = -1;
                    depth = 0;
                    for (int i = currentIndex + 1; i < lexemeTable.Count; ++i)
                    {
                        var compareLexeme = lexemeTable[i];
                        if (compareLexeme.Data1 == LexemeType.StartPara)
                            depth++;
                        if (compareLexeme.Data1 == LexemeType.EndPara)
                        {
                            if (depth == 0)
                            {
                                endIndex = i;
                                depth = -1;
                                break;
                            }
                            depth--;
                        }
                    }

                    if (depth != -1 || endIndex == -1)
                        return null;

                    startIndex = currentIndex + 1;
                    algebraTermLexemeTable = lexemeTable.GetRange(startIndex, endIndex - startIndex);
                    currentIndex = endIndex;
                    if (algebraTermLexemeTable.Count == 0)
                    {
                        pParseErrors.Add("Empty parentheses.");
                        return null;
                    }
                    algebraTerm = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors);
                    if (algebraTerm == null)
                        return null;
                    return algebraTerm;

                case LexemeType.Derivative:
                    return ParseDerivative(ref currentIndex, lexemeTable, ref pParseErrors);

                case LexemeType.FuncDeriv:
                    return ParseFuncDerivative(ref currentIndex, lexemeTable, ref pParseErrors);

                case LexemeType.Summation:
                    if (MathSolver.PLAIN_TEXT)
                        return ParseSummationPlainText(ref currentIndex, lexemeTable, ref pParseErrors);
                    return ParseSummationTeX(ref currentIndex, lexemeTable, ref pParseErrors);

                case LexemeType.Limit:
                    return ParseLimit(ref currentIndex, lexemeTable, ref pParseErrors);

                case LexemeType.Integral:
                    return ParseIntegral(ref currentIndex, lexemeTable, ref pParseErrors);

                case LexemeType.Differential:
                    pParseErrors.Add("Cannot have lone differential.");
                    return null;

                case LexemeType.I_Number:
                case LexemeType.Number:
                    Number number = Number.Parse(lexeme.Data2);
                    return number;

                case LexemeType.Operator:
                    AgOp algebraOp = AgOp.ParseOperator(lexeme.Data2);

                    // This is due to a function definition also being parsed as part of the combination or permutation.
                    if (lexeme.Data2.Contains("text") && currentIndex < lexemeTable.Count - 1)
                        lexemeTable.RemoveAt(currentIndex + 1);
                    return algebraOp;

                case LexemeType.Identifier:
                    AlgebraComp algebraComp = AlgebraComp.Parse(lexeme.Data2);
                    return algebraComp;

                case LexemeType.Constant:
                    Constant constant = Constant.ParseConstant(lexeme.Data2);
                    return constant;

                case LexemeType.FuncArgEnd:
                case LexemeType.FuncArgStart:
                    return null;
            }

            return null;
        }

        // This was for the notation where the underscore was used in conjunction with the combination and permutation operators.
        //private AlgebraTerm ParseUnderscore(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        //{
        //    if (currentIndex < lt.Count - 2)
        //        return null;
        //    // Search for the permutation or combination.
        //    ExComp before = null;
        //    int foundIndex = -1;
        //    bool isChooseFunc = false;

        //    if (lt[currentIndex + 1].Data1 != LexemeType.StartPara)
        //    {
        //        // The next lexeme needs to be P or C.
        //        if (lt[currentIndex + 2].Data2 != "text{P}" && lt[currentIndex + 2].Data2 != "text{C}")
        //            return null;
        //        before = LexemeTableToAlgebraTerm(lt.GetRange(currentIndex + 1, 1), ref pParseErrors);
        //        foundIndex = currentIndex + 2;
        //    }
        //    else
        //    {
        //        int depth = 0;
        //        for (int i = currentIndex + 1; i < lt.Count; ++i)
        //        {
        //            if (lt[i].Data1 == LexemeType.StartPara)
        //                depth++;
        //            else if (lt[i].Data1 == LexemeType.EndPara)
        //            {
        //                depth--;
        //                if (depth == 0)
        //                {
        //                    if (lt[i + 2].Data2 != "text{P}" && lt[i + 2].Data2 != "text{C}")
        //                        return null;
        //                    isChooseFunc = lt[i + 2].Data2 == "text{C}";
        //                    before = LexemeTableToAlgebraTerm(lt.GetRange(currentIndex + 1, (i) - (currentIndex + 1)), ref pParseErrors);
        //                    foundIndex = i + 1;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    if (foundIndex == 0 || before == null)
        //        return null;

        //    ExComp after = null;

        //    if (foundIndex < lt.Count - 2)
        //        return null;

        //    if (lt[foundIndex + 1].Data1 != LexemeType.Underscore)
        //        return null;

        //    currentIndex = foundIndex + 2;

        //    foundIndex = -1;

        //    if (lt[currentIndex].Data1 != LexemeType.StartPara)
        //    {
        //        // The next lexeme needs to be P or C.
        //        after = LexemeTableToAlgebraTerm(lt.GetRange(currentIndex, 1), ref pParseErrors);
        //    }
        //    else
        //    {
        //        int depth = 0;
        //        int startIndex = currentIndex + 1;
        //        for (int i = currentIndex + 1; i < lt.Count; ++i)
        //        {
        //            if (lt[i].Data1 == LexemeType.StartPara)
        //                depth++;
        //            else if (lt[i].Data1 == LexemeType.EndPara)
        //            {
        //                depth--;
        //                if (depth == 0)
        //                {
        //                    after = LexemeTableToAlgebraTerm(lt.GetRange(startIndex, (i) - (startIndex)), ref pParseErrors);
        //                    currentIndex = i + 1;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    if (after == null || foundIndex == -1)
        //        return null;

        //    return isChooseFunc ? new ChooseFunction(before, after) : new PermutationFunction(before, after);
        //}

        private AlgebraTerm ParseMultiVarFunc(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            Lexeme lex = lt[currentIndex];

            if (!_funcStore.ContainsKey(lex.Data2))
                return null;

            List<LexemeTable> argLts = _funcStore[lex.Data2];

            ExComp[] argExs = new ExComp[argLts.Count];

            for (int i = 0; i < argLts.Count; ++i)
            {
                AlgebraTerm tmp = LexemeTableToAlgebraTerm(argLts[i], ref pParseErrors);
                if (tmp == null)
                    return null;

                argExs[i] = tmp.RemoveRedundancies();
            }

            FunctionDefinition funcDef;
            if (p_EvalData.FuncDefs.IsFuncDefined(lex.Data2))
            {
                FunctionDefinition tmp = p_EvalData.FuncDefs.GetFuncDef(lex.Data2);
                if (tmp == null)
                    return null;

                funcDef = (FunctionDefinition)tmp.Clone();

                funcDef.CallArgs = argExs;

                return funcDef.ToAlgTerm();
            }

            // There can only be a function definition at this point. 
            // Supplied input arguments are not allowed.

            AlgebraComp[] defInputArgs = new AlgebraComp[argExs.Length];
            bool areCallArgs = false;
            for (int i = 0; i < argExs.Length; ++i)
            {
                defInputArgs[i] = argExs[i] as AlgebraComp;
                if (defInputArgs[i] == null)
                {
                    areCallArgs = true;
                    break;
                }
            }

            funcDef = new FunctionDefinition(new AlgebraComp(lex.Data2), areCallArgs ? null : defInputArgs, areCallArgs ? argExs : null);
            return funcDef.ToAlgTerm();
        }

        private AlgebraTerm ParseVector(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            if (lt[currentIndex].Data1 != LexemeType.VectorStore)
                return null;
            string vecStoreKey = lt[currentIndex].Data2;
            if (!_vectorStore.ContainsKey(vecStoreKey))
                return null;
            LexemeTable subsetLt = _vectorStore[vecStoreKey];

            // Split by commas.
            List<LexemeTable> vecCompLts = new List<LexemeTable>();
            int prevIndex = 0;
            int depth = 0;
            for (int i = 0; i < subsetLt.Count; ++i)
            {
                if (subsetLt[i].Data1 == LexemeType.StartBracket)
                    depth++;
                else if (subsetLt[i].Data1 == LexemeType.EndBracket)
                    depth--;
                else if (subsetLt[i].Data1 == LexemeType.Comma && depth == 0)
                {
                    var compLt = subsetLt.GetRange(prevIndex, i - (prevIndex));
                    prevIndex = i + 1;
                    vecCompLts.Add(compLt);
                }
            }
            vecCompLts.Add(subsetLt.GetRange(prevIndex, subsetLt.Count - prevIndex));

            if (vecCompLts.Count == 0 || vecCompLts.Count == 1)
                return null;

            // Then parse each individual component.
            List<ExComp> vecCompExs = new List<ExComp>();
            foreach (LexemeTable vecCompLt in vecCompLts)
            {
                AlgebraTerm term = LexemeTableToAlgebraTerm(vecCompLt,  ref pParseErrors);
                if (term == null)
                    return null;
                vecCompExs.Add(term);
            }

            var mat = Equation.Structural.LinearAlg.MatrixHelper.CreateMatrix(vecCompExs);
            string str = mat.ToTexString();

            return mat;
        }

        private AlgebraTerm ParseDerivative(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            if (MathSolver.PLAIN_TEXT)
                return ParseDerivativePlainText(ref currentIndex, lt, ref pParseErrors);
            return ParseDerivativeTeX(ref currentIndex, lt, ref pParseErrors);
        }

        private AlgebraTerm ParseDerivativePlainText(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            // Should be in the form d/dx(...) or df/dx
            string derivStr = lt[currentIndex].Data2;

            string[] topBottom = derivStr.Split('/');
            if (topBottom.Length != 2)
                return null;
            string top = topBottom[0].Remove(0, 1);
            top = top.Remove(top.Length - 1, 1);
            string bottom = topBottom[1].Remove(0, 1);
            bottom = bottom.Remove(bottom.Length - 1, 1);

            bool isPartial = top.Contains("partial");
            if (isPartial != bottom.Contains("partial"))
            {
                pParseErrors.Add("Incorrect use of the partial derivative.");
                return null; 
            }

            if (isPartial)
            {
                top = top.Replace("partial", "d");
                bottom = bottom.Replace("partial", "d");
            }

            int indexOfDeriv = -1;
            if (top.Contains("^"))
            {
                Match match = Regex.Match(top, NUM_MATCH);
                if (!match.Success)
                    return null;
                string matchStr = match.Value;
                if (!int.TryParse(match.Value, out indexOfDeriv))
                    return null;

                if (indexOfDeriv < 1)
                    return null;

                if (!bottom.Contains("^"))
                    return null;
                match = Regex.Match(bottom, NUM_MATCH);
                if (!match.Success)
                    return null;
                if (match.Value != matchStr)
                    return null;
            }

            top = top.Remove(0, 1);
            bottom = bottom.Remove(0, 1);

            if (indexOfDeriv != -1)
            {
                Match topSymbMatch = Regex.Match(top, IDEN_MATCH);
                Match bottomSymbMatch = Regex.Match(bottom, IDEN_MATCH);
                if (!bottomSymbMatch.Success)
                    return null;

                top = topSymbMatch.Value;
                bottom = bottomSymbMatch.Value;
            }

            if (top.Length != 0)
            {
                // We have df/dx

                // Top is now the function.
                // Bottom is now the variable with respect to.

                if (top == bottom)
                {
                    pParseErrors.Add("Incorrect derivative.");
                    return null;
                }

                return Equation.Functions.Calculus.Derivative.Parse(top, bottom, new Number(indexOfDeriv), isPartial, ref p_EvalData);
            }


            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            // We have d/dx

            ExComp innerEx;
            // Just takes the derivative of the rest of the expression.
            var remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
            if (remainingLt.Count == 0)
            {
                pParseErrors.Add("Nothing following derivative.");
                return null;
            }
            currentIndex = lt.Count - 1;
            innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors);

            if (innerEx == null)
            {
                return null;
            }

            return Equation.Functions.Calculus.Derivative.Parse(bottom, innerEx, new Number(indexOfDeriv), isPartial);
        }

        private AlgebraTerm ParseDerivativeTeX(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            // Should be in the form d/dx(...) or df/dx
            string derivStr = lt[currentIndex].Data2;

            string[] topBottom = derivStr.Split(')');
            if (topBottom.Length != 3)
                return null;
            string top = topBottom[0].Remove(0, "frac(".Length);
            string bottom = topBottom[1].Remove(0, 1);

            bool isPartial = top.Contains("partial");
            if (isPartial != bottom.Contains("partial"))
            {
                pParseErrors.Add("Incorrect use of the partial derivative.");
                return null;
            }

            if (isPartial)
            {
                top = top.Replace("partial", "d");
                bottom = bottom.Replace("partial", "d");
            }

            int indexOfDeriv = -1;
            if (top.Contains("^"))
            {
                Match match = Regex.Match(top, NUM_MATCH);
                if (!match.Success)
                    return null;
                string matchStr = match.Value;
                if (!int.TryParse(match.Value, out indexOfDeriv))
                    return null;

                if (indexOfDeriv < 1)
                    return null;

                if (!bottom.Contains("^"))
                    return null;
                match = Regex.Match(bottom, NUM_MATCH);
                if (!match.Success)
                    return null;
                if (match.Value != matchStr)
                    return null;
            }

            top = top.Remove(0, 1);
            bottom = bottom.Remove(0, 1);

            if (indexOfDeriv != -1)
            {
                Match topSymbMatch = Regex.Match(top, IDEN_MATCH);
                Match bottomSymbMatch = Regex.Match(bottom, IDEN_MATCH);
                if (!bottomSymbMatch.Success)
                    return null;

                top = topSymbMatch.Value;
                bottom = bottomSymbMatch.Value;
            }

            if (top.Length != 0)
            {
                // We have df/dx

                // Top is now the function.
                // Bottom is now the variable with respect to.

                if (top == bottom)
                {
                    pParseErrors.Add("Incorrect derivative.");
                    return null;
                }

                return Equation.Functions.Calculus.Derivative.Parse(top, bottom, new Number(indexOfDeriv), isPartial, ref p_EvalData);
            }

            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            // We have d/dx

            ExComp innerEx;
            // Just takes the derivative of the rest of the expression.
            var remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
            if (remainingLt.Count == 0)
            {
                pParseErrors.Add("Nothing following derivative.");
                return null;
            }
            currentIndex = lt.Count - 1;
            innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors);

            if (innerEx == null)
            {
                return null;
            }

            return Equation.Functions.Calculus.Derivative.Parse(bottom, innerEx, new Number(indexOfDeriv), isPartial);
        }

        private AlgebraTerm ParseFraction(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            currentIndex++;

            // Should be in the form \frac(num)(den)

            ExComp numEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            if (numEx == null)
                return null;

            currentIndex += 2;

            if (currentIndex > lt.Count - 1)
                return null;

            if (lt[currentIndex].Data1 != LexemeType.StartPara)
                return null;

            ExComp denEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            if (denEx == null)
                return null;

            return new AlgebraTerm(numEx, new Equation.Operators.DivOp(), denEx);
        }

        private AlgebraTerm ParseFuncDerivative(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            Lexeme lex = lt[currentIndex];
            string str;
            string withRespectTo = null;
            if (lex.Data2.Contains("("))
            {
                string[] tmps = lex.Data2.Split('(');
                str = tmps[0];
                withRespectTo = tmps[1].Remove(tmps[1].Length - 1, 1);
            }
            else
                str = lex.Data2;

            // Convert this over to the Leibniz notation.
            Match funcMatch = Regex.Match(str, IDEN_MATCH);
            if (!funcMatch.Success)
                return null;

            str = str.Remove(0, funcMatch.Length);
            ExComp order = null;
            if (str.Contains("'"))
                order = new Number(str.Length);        // Just the number of primes we have.
            else
            {
                str = str.Remove(0, 1);
                // This is in the notation f^2(x)

                int iTmp;
                if (int.TryParse(str, out iTmp))
                    order = new Number(iTmp);
                else if (Regex.IsMatch(str, IDEN_MATCH))
                    order = new AlgebraComp(str);
                else
                    return null;
            }

            if (withRespectTo == null && currentIndex + 3 < lt.Count)
            {
                if (lt[currentIndex + 1].Data1 == LexemeType.StartPara)
                {
                    if (lt[currentIndex + 2].Data1 != LexemeType.Identifier)
                        return null;
                    withRespectTo = lt[currentIndex + 2].Data2;
                    if (lt[currentIndex + 3].Data1 != LexemeType.EndPara)
                        return null;

                    currentIndex = currentIndex + 4;
                }
            }

            var deriv = Equation.Functions.Calculus.Derivative.Parse(funcMatch.Value, withRespectTo, order, false, 
                ref p_EvalData, true);

            if (deriv == null)
                pParseErrors.Add("Incorrect derivative notation for multivaraible functions.");

            return deriv;
        }

        private AlgebraTerm ParseLimit(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            string limitStr = lt[currentIndex].Data2.Remove(0, "lim_(".Length);
            limitStr = limitStr.Remove(limitStr.Length - 1, 1);

            int index = limitStr.IndexOf("to");
            if (index == -1)
                return null;
            string[] limitParts = { limitStr.Substring(0, index), limitStr.Substring(index + 2, limitStr.Length - (index + 2)) };

            AlgebraComp varFor = new AlgebraComp(limitParts[0]);

            bool negate = limitParts[1].StartsWith("-");
            if (negate)
                limitParts[1] = limitParts[1].Remove(0, 1);

            ExComp parsedValTo = null;
            if (limitParts[1] == "inf")
                parsedValTo = Number.PosInfinity;
            else if (Regex.IsMatch(limitParts[1], IDEN_MATCH))
            {
                parsedValTo = new AlgebraComp(limitParts[1]);
            }
            else if (Regex.IsMatch(limitParts[1], NUM_MATCH))
            {
                parsedValTo = Number.Parse(limitParts[1]);
            }

            if (parsedValTo == null)
            {
                pParseErrors.Add("Incorrect limit approach value.");
                return null;
            }

            if (negate)
                parsedValTo = Equation.Operators.MulOp.Negate(parsedValTo);

            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            ExComp innerEx;
            if (lt[currentIndex].Data1 == LexemeType.StartPara)
                innerEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            else
            {
                // Just takes the derivative of the rest of the expression.
                var remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
                currentIndex = lt.Count - 1;
                innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors);
            }

            if (innerEx == null)
                return null;

            return Equation.Functions.Calculus.Limit.Create(innerEx, varFor, parsedValTo);
        }

        private AlgebraTerm ParseLogBaseInner(ref int currentIndex, LexemeTable lexemeTable, ref List<string> pParseErrors)
        {
            currentIndex++;
            if (currentIndex > lexemeTable.Count - 1)
                return null;

            // This should be in the form log_(base)(inner)
            ExComp baseEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (baseEx == null)
                return null;

            // Skip past the multiplication operator which has been placed here on default.
            currentIndex += 2;

            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].Data1 != LexemeType.StartPara)
                return null;

            ExComp innerEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (innerEx == null)
                return null;

            Equation.Functions.LogFunction log = new Equation.Functions.LogFunction(innerEx);
            log.Base = baseEx;

            return log;
        }

        private AlgebraTerm ParseParaGroup(ref int currentIndex, LexemeTable lexemeTable, ref List<string> pParseErrors)
        {
            int endIndex = -1;
            int depth = 0;
            int startIndex;

            endIndex = -1;
            depth = 0;
            for (int i = currentIndex + 1; ; ++i)
            {
                var compareLexeme = lexemeTable[i];
                if (compareLexeme.Data1 == LexemeType.StartPara)
                    depth++;
                if (compareLexeme.Data1 == LexemeType.EndPara)
                {
                    if (depth == 0)
                    {
                        endIndex = i;
                        break;
                    }
                    depth--;
                }
            }

            if (endIndex == -1)
                throw new ArgumentException("Invalid end index!");

            startIndex = currentIndex + 1;
            LexemeTable algebraTermLexemeTable = lexemeTable.GetRange(startIndex, endIndex - startIndex);
            currentIndex = endIndex;
            if (algebraTermLexemeTable.Count == 0)
                return null;
            return LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors);
        }

        private AlgebraTerm ParseRootInner(ref int currentIndex, LexemeTable lexemeTable, ref List<string> pParseErrors)
        {
            currentIndex++;

            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].Data1 != LexemeType.StartPara)
                return null;

            ExComp rootEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (rootEx == null)
                return null;

            currentIndex += 2;

            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].Data1 != LexemeType.StartPara)
                return null;

            ExComp innerEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (innerEx == null)
                return null;

            return new AlgebraTerm(innerEx, new Equation.Operators.PowOp(), Equation.Operators.DivOp.StaticCombine(Number.One, rootEx));
        }

        private AlgebraTerm ParseSummationPlainText(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            string sumData = lt[currentIndex].Data2.Split('_')[1];

            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            string[] split = sumData.Split('^');

            // Remove the starting '('
            split[0] = split[0].Remove(0, 1);
            // Remove the ending ')'
            split[0] = split[0].Remove(split[0].Length - 1, 1);

            // Remove the starting '('
            split[1] = split[1].Remove(0, 1);
            split[1] = split[1].Remove(split[1].Length - 1, 1);

            string[] initStrs = split[0].Split('=');
            Number nInit = Number.Parse(initStrs[1]);
            AlgebraComp iterVar = new AlgebraComp(initStrs[0]);
            Number nIterCount = Number.Parse(split[1]);

            if (nInit == null || nIterCount == null)
                return null;

            ExComp innerEx;
            if (lt[currentIndex].Data1 == LexemeType.StartPara)
                innerEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            else
            {
                // Just takes the derivative of the rest of the expression.
                var remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
                currentIndex = lt.Count - 1;
                innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors);
            }

            if (innerEx == null)
            {
                pParseErrors.Add("Expression must follow summation.");
                return null;
            }

            Equation.Functions.SumFunction sumFunc = new SumFunction(innerEx, iterVar, nInit, nIterCount);

            return sumFunc;
        }

        private AlgebraTerm ParseSummationTeX(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            string[] split = lt[currentIndex].Data2.Split('_');

            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            split[0] = split[0].Remove(0, "sum^".Length);

            split[1] = split[1].Remove(0, 1);
            split[1] = split[1].Remove(split[1].Length - 1, 1);

            if (split[0].StartsWith("("))
            {
                split[0] = split[0].Remove(0, 1);
                split[0] = split[0].Remove(split[0].Length - 1, 1);
            }

            string[] initStrs = split[1].Split('=');

            Number nInit = Number.Parse(initStrs[1]);
            AlgebraComp iterVar = new AlgebraComp(initStrs[0]);
            Number nIterCount = Number.Parse(split[0]);

            if (nInit == null || nIterCount == null)
                return null;

            ExComp innerEx;
            if (lt[currentIndex].Data1 == LexemeType.StartPara)
                innerEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            else
            {
                // Just takes the derivative of the rest of the expression.
                var remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
                currentIndex = lt.Count - 1;
                innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors);
            }

            if (innerEx == null)
            {
                pParseErrors.Add("Expression must follow summation.");
                return null;
            }

            Equation.Functions.SumFunction sumFunc = new SumFunction(innerEx, iterVar, nInit, nIterCount);

            return sumFunc;
        }

        private ExComp ParseIntegral(ref int currentIndex, LexemeTable lt, ref List<string> pParseErrors)
        {
            ExComp lower = null, upper = null;
            if (lt[currentIndex].Data2.Contains("_"))
            {
                // Parse the lower bound.
                string[] integralData = lt[currentIndex].Data2.Split('_');
                if (integralData.Length != 2)
                    return null;

                string lowerBound = integralData[1];
                if (lowerBound.StartsWith("("))
                {
                    lowerBound = lowerBound.Remove(0, 1);
                    lowerBound = lowerBound.Remove(lowerBound.Length - 1, 1);
                }

                var lowerLexTable = CreateLexemeTable(lowerBound, ref pParseErrors);
                if (lowerLexTable == null)
                    return null;
                var lowerTerm = LexemeTableToAlgebraTerm(lowerLexTable, ref pParseErrors);
                if (lowerTerm == null)
                    return null;

                lowerTerm = lowerTerm.WeakMakeWorkable().ToAlgTerm();
                lowerTerm = lowerTerm.ApplyOrderOfOperations();
                lowerTerm = lowerTerm.MakeWorkable().ToAlgTerm();
                lowerTerm = lowerTerm.CompoundFractions();

                lower = lowerTerm.RemoveRedundancies();


                if (lt[currentIndex + 1].Data1 != LexemeType.Operator || lt[currentIndex + 1].Data2 != "^")
                    return null;

                currentIndex += 2;

                upper = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
                if (upper == null)
                    return null;

                AlgebraTerm upperTerm = upper.ToAlgTerm();
                upperTerm = upperTerm.WeakMakeWorkable().ToAlgTerm();
                upperTerm = upperTerm.ApplyOrderOfOperations();
                upperTerm = upperTerm.MakeWorkable().ToAlgTerm();
                upperTerm = upperTerm.CompoundFractions();

                upper = upperTerm.RemoveRedundancies();
            }


            int startIndex = currentIndex + 1;
            int endIndex = -1;

            // Next should be the expression.
            // Parse until the differential.
            int depth = 0;
            for (; currentIndex < lt.Count; ++currentIndex)
            {
                if (lt[currentIndex].Data1 == LexemeType.Integral)
                {
                    depth++;
                }
                if (lt[currentIndex].Data1 == LexemeType.Differential) 
                {
                    depth--;
                    if (depth == 0)
                    {
                        endIndex = currentIndex;
                        break;
                    }
                }
            }

            if (endIndex == -1)
            {
                pParseErrors.Add("Couldn't find the variable of integration.");
                return null;
            }

            currentIndex = endIndex;

            LexemeTable integralTerm = lt.GetRange(startIndex, endIndex - startIndex);
            AlgebraTerm innerTerm = LexemeTableToAlgebraTerm(integralTerm, ref pParseErrors);
            if (innerTerm == null)
                return null;

            string withRespectVar = lt[endIndex].Data2.Remove(0, lt[endIndex].Data2.Length - 1);

            AlgebraComp dVar = new AlgebraComp(withRespectVar);

            return MathSolverLibrary.Equation.Functions.Calculus.Integral.ConstructIntegral(innerTerm, dVar, lower, upper);
        }
    }
}