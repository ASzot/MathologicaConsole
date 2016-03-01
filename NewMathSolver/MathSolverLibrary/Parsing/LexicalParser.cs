using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;
using MathSolverWebsite.MathSolverLibrary.TermType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.Parsing
{
    internal class LexicalParser
    {
        private const string MATH_EMPTY_GP = "EMPTYGP";

        public const string IDEN_MATCH =
            @"alpha|beta|gamma|delta|epsilon|varepsilon|zeta|eta|theta|vartheta|iota|kappa|lambda|mu|nu|xi|rho|sigma|tau|usilon|phi|varphi|" +
            "chi|psi|omega|Gamma|Theta|Lambda|Xi|Phsi|Psi|Omega|Sigma|[a-zA-Z]";

        public const string NUM_MATCH = @"((?<![\d])\.[\d]+)|([\d]+([.][\d]+)?)";
        public const string INF_MATCH = @"inf";
        public const string OPTIONAL_REALIMAG_NUM_PATTERN = @"^(-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?$";
        public const string REAL_NUM_PATTERN = @"-?[\d]+([.,][\d]+)?";

        public const string REALIMAG_FRAC_NUM_PATTERN =
            @"^-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?(\/-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?)?$";

        public const string REALIMAG_NUM_PATTERN = @"^-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?$";

        public const string REALIMAG_OPPOW_NUM_PATTERN =
            @"^-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?(\^\(-?[\d]+([.,][\d]+)?((\+|\-)i[0-9])?\))?$";

        public const int DIFF_RULE_INDEX = 24;
        private readonly List<string> _definedFuncs = new List<string>();

        private TypePair<string, LexemeType>[] _rulesets =
            new TypePair<string, LexemeType>[]
        {
            new TypePair<string, LexemeType>(@"\+|\-|\^|\/|\*|circ|text\(P\)|text\(C\)|" + CrossProductOp.IDEN,
                LexemeType.Operator),
            new TypePair<string, LexemeType>(@"\=", LexemeType.EqualsOp),
            new TypePair<string, LexemeType>(@"i", LexemeType.I_Number),
            new TypePair<string, LexemeType>(@"pi|e", LexemeType.Constant),
            new TypePair<string, LexemeType>(@"\(", LexemeType.StartPara),
            new TypePair<string, LexemeType>(@"\)", LexemeType.EndPara),
            new TypePair<string, LexemeType>(
                "(?<!lo)((" + IDEN_MATCH + ")((_((" + IDEN_MATCH + ")|(" + NUM_MATCH + ")))|)?)", LexemeType.Identifier),
            new TypePair<string, LexemeType>("(" + IDEN_MATCH + @")\((" + IDEN_MATCH + @")\)", LexemeType.FunctionDef),
            new TypePair<string, LexemeType>(NUM_MATCH, LexemeType.Number),
            new TypePair<string, LexemeType>(
                @"\!|asin|arcsin|acos|arccos|atan|arctan|acsc|arccsc|asec|arcsec|acot|arccot|sin|cos|tan|csc|sec|cot|log_|log|ln|" +
                "sqrt|root|frac|det|div|curl|nabla*|nabla" + CrossProductOp.IDEN +
                "|nabla|binom|vectora|vectorb|vectorb", LexemeType.Function),
            new TypePair<string, LexemeType>(@"\[", LexemeType.StartBracket),
            new TypePair<string, LexemeType>(@"\]", LexemeType.EndBracket),
            new TypePair<string, LexemeType>(@"\|", LexemeType.Bar),
            new TypePair<string, LexemeType>(@"\;", LexemeType.EquationSeperator),
            new TypePair<string, LexemeType>(@">", LexemeType.Greater),
            new TypePair<string, LexemeType>(@">=", LexemeType.GreaterEqual),
            new TypePair<string, LexemeType>(@"<", LexemeType.Less),
            new TypePair<string, LexemeType>(@"<=", LexemeType.LessEqual),
            new TypePair<string, LexemeType>(@",", LexemeType.Comma),
            new TypePair<string, LexemeType>("", LexemeType.Derivative),
            new TypePair<string, LexemeType>(
                "(" + IDEN_MATCH + @")(((\')+)|((\^((" + NUM_MATCH + @")|(" + IDEN_MATCH + @")))\((" + IDEN_MATCH +
                @")\)))", LexemeType.FuncDeriv),
            new TypePair<string, LexemeType>(@"sum_\((" + IDEN_MATCH + ")=",
                LexemeType.Summation),
            new TypePair<string, LexemeType>(@"lim_\((" + IDEN_MATCH + @")to",
                LexemeType.Limit),
            new TypePair<string, LexemeType>(@"(oint_)|(int_)|(int)", LexemeType.Integral),
            new TypePair<string, LexemeType>(@"\$d(" + IDEN_MATCH + @")", LexemeType.Differential),
            new TypePair<string, LexemeType>(INF_MATCH, LexemeType.Infinity),
            new TypePair<string, LexemeType>(@"(sum)|(lim)", LexemeType.ErrorType)
        };

        private bool _fixIntegrals = true;
        private Dictionary<string, List<List<TypePair<LexemeType,string>>>> _funcStore = new Dictionary<string, List<List<TypePair<LexemeType,string>>>>();
        private int _vecStoreIndex;
        private Dictionary<string, List<TypePair<LexemeType,string>>> _vectorStore = new Dictionary<string, List<TypePair<LexemeType,string>>>();
        private EvalData p_EvalData;

        public LexicalParser(EvalData pEvalData)
        {
            p_EvalData = pEvalData;
            // Index 19 should be the index of the Derivative LexemeType. 
            _rulesets[19] = new TypePair<string, LexemeType>(p_EvalData.GetPlainTextInput()
                ? @"\(((partial)|d)(\^(" + NUM_MATCH + "))?(" + IDEN_MATCH + @")?\)\/\(((partial)|d)(" + IDEN_MATCH +
                  @")(\^(" + NUM_MATCH + @"))?\)"
                : @"frac\(((partial)|d)(\^(" + NUM_MATCH + "))?(" + IDEN_MATCH + @")?\)\(((partial)|d)(" + IDEN_MATCH +
                  @")(\^(" + NUM_MATCH + @"))?\)", LexemeType.Derivative);
        }

        private void ResetDiffParsing()
        {
            _rulesets[DIFF_RULE_INDEX] = new TypePair<string, LexemeType>(@"\$d(" + IDEN_MATCH + @")",
                LexemeType.Differential);
        }

        public static List<TypePair<LexemeType,string>> CompoundLexemeTable(List<TypePair<LexemeType,string>> lt0, List<TypePair<LexemeType,string>> lt1)
        {
            List<TypePair<LexemeType,string>> finalTable = new List<TypePair<LexemeType,string>>();
            finalTable.AddRange(lt0);
            finalTable.Add(new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EqualsOp, "="));
            finalTable.AddRange(lt1);

            return finalTable;
        }

        public List<TypePair<LexemeType, string>> CreateLexemeTable(string inputStr, ref List<string> pParseErrors)
        {
            inputStr = CleanTexInput(inputStr);

            List<TypePair<LexemeType, MatchTolken>> tolkenMatches = new List<TypePair<LexemeType, MatchTolken>>();
            foreach (TypePair<string, LexemeType> rulset in _rulesets)
            {
                MatchCollection matches = TypeHelper.Matches(inputStr, rulset.GetData1());
                foreach (Match match in matches)
                    tolkenMatches.Add(new TypePair<LexemeType, MatchTolken>(rulset.GetData2(), new MatchTolken(match)));
            }

            IEnumerable<TypePair<LexemeType, MatchTolken>> tmpTolkenMatches = (from t in tolkenMatches
                             orderby t.GetData2().Index
                             select t);

            tolkenMatches = tmpTolkenMatches.ToList();

            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                TypePair<LexemeType, MatchTolken> tolkenMatch = tolkenMatches[i];

                if (tolkenMatch.GetData1() == LexemeType.Identifier && tolkenMatch.GetData2().Value.Contains("=") &&
                    tolkenMatch.GetData2().Value.Contains("="))
                {
                    string splitVal = tolkenMatch.GetData2().Value.Split('_')[0];
                    tolkenMatches[i].SetData2(new MatchTolken(splitVal.Length,
                        tolkenMatch.GetData2().Index, splitVal));
                }
                else if (tolkenMatch.GetData1() == LexemeType.Integral)
                {
                    string tolkenStr = tolkenMatch.GetData2().Value;

                    if (tolkenStr.EndsWith("("))
                    {
                        // Swallow up the next tolkens leading to the last paranthese.
                        int endIndex = -1;
                        int depth = 1;

                        int minIndex = tolkenMatch.GetData2().Index + tolkenMatch.GetData2().Length;

                        for (int j = i + 1; j < tolkenMatches.Count; ++j)
                        {
                            TypePair<LexemeType, MatchTolken> compareTolken = tolkenMatches[j];
                            if (compareTolken.GetData2().Index < minIndex)
                            {
                                ArrayFunc.RemoveIndex(tolkenMatches, j--);
                                continue;
                            }
                            if (compareTolken.GetData2().Value == ")")
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    endIndex = j;
                                    break;
                                }
                            }
                            else if (compareTolken.GetData2().Value == "(")
                                depth++;
                        }

                        if (endIndex == -1)
                            return null;

                        List<TypePair<LexemeType, MatchTolken>> tolkenRange = tolkenMatches.GetRange(i + 1, endIndex - i);
                        tolkenMatches.RemoveRange(i + 1, endIndex - i);
                        for (int j = 0; j < tolkenRange.Count; ++j)
                        {
                            TypePair<LexemeType, MatchTolken> tolken = tolkenRange[j];
                            for (int k = 0; k < tolkenRange.Count; ++k)
                            {
                                if (j == k)
                                    continue;
                                TypePair<LexemeType, MatchTolken> compTolken = tolkenRange[k];
                                if (tolken.GetData2().Index <= compTolken.GetData2().Index &&
                                    (tolken.GetData2().Length + tolken.GetData2().Index) >=
                                    (compTolken.GetData2().Index + compTolken.GetData2().Length))
                                {
                                    ArrayFunc.RemoveIndex(tolkenRange, k--);
                                    if (k < j)
                                        j--;
                                }
                            }
                        }

                        string addStr = "";
                        foreach (TypePair<LexemeType, MatchTolken> tolken in tolkenRange)
                        {
                            addStr += tolken.GetData2().Value;
                        }

                        tolkenMatches[i].GetData2().Value += addStr;
                    }
                }
            }

            List<TypePair<LexemeType, MatchTolken>> tolkensToRemove = new List<TypePair<LexemeType, MatchTolken>>();
            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                TypePair<LexemeType, MatchTolken> tolken = tolkenMatches[i];
                int length = tolken.GetData2().Length;
                int startIndex = tolken.GetData2().Index;

                // Check if the index checking against is already to be removed.
                if (tolkensToRemove.Contains(tolkenMatches[i]))
                    continue;

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

                            TypePair<LexemeType, MatchTolken> compareTolken = tolkenMatches[k];

                            if (tolken.GetData2().Index + tolken.GetData2().Length >= compareTolken.GetData2().Index &&
                                tolken.GetData1() == LexemeType.Integral && compareTolken.GetData1() == LexemeType.Identifier &&
                                compareTolken.GetData2().Value.Contains("t_"))
                            {
                                // This was a mismatched lexeme.
                                string idenStr = compareTolken.GetData2().Value.Split('_')[1];
                                if (!Regex.IsMatch(idenStr, NUM_MATCH) && !Regex.IsMatch(idenStr, INF_MATCH))
                                {
                                    tolkenMatches[k] = new TypePair<LexemeType, MatchTolken>(LexemeType.Identifier,
                                        new MatchTolken(idenStr.Length, tolken.GetData2().Index + 1, idenStr));
                                    continue;
                                }
                            }

                            if (compareTolken.GetData2().Index == index &&
                                !((compareTolken.GetData1() == LexemeType.FunctionDef ||
                                   compareTolken.GetData1() == LexemeType.Derivative) &&
                                  compareTolken.GetData2().Value.StartsWith("g(")))
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
                TypePair<LexemeType, MatchTolken> tolkenMatch = tolkenMatches[i];
                if (tolkenMatch.GetData1() == LexemeType.Constant && tolkenMatch.GetData2().Value == "e")
                {
                    // Is there an identifier with this same index?
                    for (int j = 0; j < tolkenMatches.Count; ++j)
                    {
                        if (j == i)
                            continue;
                        TypePair<LexemeType, MatchTolken> compareTolkenMatch = tolkenMatches[j];
                        if (compareTolkenMatch.GetData1() == LexemeType.Identifier && compareTolkenMatch.GetData2().Value == "e"
                            && compareTolkenMatch.GetData2().Index == tolkenMatch.GetData2().Index)
                        {
                            tolkensToRemove.Add(compareTolkenMatch);
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < tolkenMatches.Count; ++i)
            {
                TypePair<LexemeType, MatchTolken> tolkenMatch = tolkenMatches[i];
                // Is there another token out there which has the same index?
                if (tolkenMatch.GetData1() == LexemeType.I_Number && tolkenMatch.GetData2().Value == "i")
                {
                    for (int j = 0; j < tolkenMatches.Count; ++j)
                    {
                        if (j == i)
                            continue;

                        TypePair<LexemeType, MatchTolken> compareTolkenMatch = tolkenMatches[j];
                        if (compareTolkenMatch.GetData1() == LexemeType.Identifier && compareTolkenMatch.GetData2().Value == "i"
                            && compareTolkenMatch.GetData2().Index == tolkenMatch.GetData2().Index)
                        {
                            tolkensToRemove.Add(compareTolkenMatch);
                            break;
                        }
                    }
                }
            }

            foreach (TypePair<LexemeType, MatchTolken> tolkenToRemove in tolkensToRemove)
            {
                tolkenMatches.Remove(tolkenToRemove);
            }

            // Order the tokens in the order they appear in the string.
            tolkenMatches = ArrayFunc.OrderList(tolkenMatches);

            List<TypePair<LexemeType, string>> orderedTolkensList = new List<TypePair<LexemeType, string>>();
            for (int i = 0; i < tolkenMatches.Count; ++i)
                orderedTolkensList.Add(new TypePair<LexemeType, string>(tolkenMatches[i].GetData1(), tolkenMatches[i].GetData2().Value));
            
            for (int i = 0; i < orderedTolkensList.Count; ++i)
            {
                if (orderedTolkensList[i].GetData1() == LexemeType.FunctionDef && i > 0 &&
                    (orderedTolkensList[i - 1].GetData1() == LexemeType.Function ||
                     orderedTolkensList[i - 1].GetData1() == LexemeType.Derivative ||
                     orderedTolkensList[i - 1].GetData1() == LexemeType.Integral))
                {
                    string funcDef = orderedTolkensList[i].GetData2();
                    string func = orderedTolkensList[i - 1].GetData2();

                    // Actually get the starting identifier not character.
                    int funcParaStartIndex = funcDef.IndexOf("(");
                    int funcParaEndIndex = funcDef.IndexOf(")");
                    string funcStartStr = funcDef.Substring(0, funcParaStartIndex);
                    string funcParamStr = funcDef.Substring(funcParaStartIndex + 1,
                        funcParaEndIndex - (funcParaStartIndex + 1));

                    if (func.EndsWith(funcStartStr))
                    {
                        ArrayFunc.RemoveIndex(orderedTolkensList, i);
                        orderedTolkensList.Insert(i, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartPara, "("));
                        LexemeType lt;
                        if (funcParamStr == "e")
                            lt = LexemeType.Constant;
                        else
                            lt = LexemeType.Identifier;
                        orderedTolkensList.Insert(i + 1, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(lt, funcParamStr));
                        orderedTolkensList.Insert(i + 2, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EndPara, ")"));
                        i += 2;
                    }
                    else if (func.EndsWith("_"))
                    {
                        ArrayFunc.RemoveIndex(orderedTolkensList, i);
                        orderedTolkensList.Insert(i, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Identifier, funcStartStr));
                        orderedTolkensList.Insert(i + 1, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartPara, "("));
                        orderedTolkensList.Insert(i + 2, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Identifier, funcParamStr));
                        orderedTolkensList.Insert(i + 3, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EndPara, ")"));
                    }
                }
                else if (orderedTolkensList[i].GetData1() == LexemeType.FuncDeriv && i > 0 &&
                         (orderedTolkensList[i - 1].GetData1() == LexemeType.Function))
                {
                    if (orderedTolkensList[i].GetData2().Length < 5)
                        continue;

                    int funcParaStartIndex = orderedTolkensList[i].GetData2().IndexOf("(");
                    if (funcParaStartIndex == -1)
                        continue;
                    int funcParaEndIndex = orderedTolkensList[i].GetData2().IndexOf(")");
                    if (funcParaStartIndex == -1)
                        continue;
                    int raiseIndex = orderedTolkensList[i].GetData2().IndexOf("^");
                    if (raiseIndex == -1)
                        continue;
                    string funcStartStr = orderedTolkensList[i].GetData2().Substring(0, raiseIndex);
                    string raisedStr = orderedTolkensList[i].GetData2().Substring(raiseIndex + 1,
                        funcParaStartIndex - (raiseIndex + 1));
                    string funcParamStr = orderedTolkensList[i].GetData2().Substring(funcParaStartIndex + 1,
                        funcParaEndIndex - (funcParaStartIndex + 1));

                    if (!orderedTolkensList[i - 1].GetData2().EndsWith(funcStartStr))
                        continue;

                    ArrayFunc.RemoveIndex(orderedTolkensList, i);
                    orderedTolkensList.Insert(i, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Operator, "^"));
                    orderedTolkensList.Insert(i + 1, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Number, raisedStr));
                    orderedTolkensList.Insert(i + 2, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartPara, "("));
                    orderedTolkensList.Insert(i + 3, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Identifier, funcParamStr));
                    orderedTolkensList.Insert(i + 4, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EndPara, ")"));
                }
            }

            for (int i = 0; i < orderedTolkensList.Count - 1; ++i)
            {
                if (orderedTolkensList[i].GetData1() == LexemeType.Identifier &&
                    orderedTolkensList[i + 1].GetData1() == LexemeType.StartPara)
                {
                    if (true) //p_EvalData.FuncDefs.IsFuncDefined(orderedTolkensList[i].Data2))
                    {
                        // Find the ending paranthese.
                        int endIndex = -1;
                        int depth = 0;
                        for (int j = i + 2; j < orderedTolkensList.Count; ++j)
                        {
                            if (orderedTolkensList[j].GetData1() == LexemeType.EndPara)
                            {
                                if (depth == 0)
                                {
                                    endIndex = j;
                                    break;
                                }

                                depth--;
                            }
                            else if (orderedTolkensList[j].GetData1() == LexemeType.StartPara)
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
                            if (orderedTolkensList[j].GetData1() == LexemeType.StartPara)
                                depthCount++;
                            else if (orderedTolkensList[j].GetData1() == LexemeType.EndPara)
                                depthCount--;
                            if (orderedTolkensList[j].GetData1() == LexemeType.Comma && depthCount == 0)
                                commaCount++;
                        }

                        if (commaCount == 0 &&
                            (p_EvalData.GetFuncDefs().IsValidFuncCall(orderedTolkensList[i].GetData2(), commaCount + 1) ||
                             _definedFuncs.Contains(orderedTolkensList[i].GetData2())))
                        {
                            // Replace with a function call.
                            orderedTolkensList[i].SetData1(LexemeType.FuncIden);
                            orderedTolkensList[i + 1].SetData1(LexemeType.FuncArgStart);
                            orderedTolkensList[endIndex].SetData1(LexemeType.FuncArgEnd);
                        }
                    }
                }
                if (i + 1 < orderedTolkensList.Count && orderedTolkensList[i].GetData1() == LexemeType.FunctionDef &&
                    orderedTolkensList[i + 1].GetData1() == LexemeType.EqualsOp
                    && (i == 0 || orderedTolkensList[i - 1].GetData1() == LexemeType.EquationSeperator))
                {
                    _definedFuncs.Add(orderedTolkensList[i].GetData2().Split('(')[0]);
                }
            }

            for (int i = 0; i < orderedTolkensList.Count; ++i)
            {
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> tolken = orderedTolkensList[i];
                if (!p_EvalData.GetPlainTextInput())
                {
                    if (tolken.GetData1() == LexemeType.Derivative)
                    {
                        if (i > 0)
                        {
                            ArrayFunc.RemoveIndex(orderedTolkensList, i - 1);
                        }
                        if (i < orderedTolkensList.Count && !tolken.GetData2().Contains("^") &&
                            orderedTolkensList[i].GetData1() == LexemeType.FunctionDef)
                            ArrayFunc.RemoveIndex(orderedTolkensList, i--);
                    }
                }

                if (tolken.GetData1() == LexemeType.Limit && i < orderedTolkensList.Count - 1 &&
                    orderedTolkensList[i + 1].GetData2() == "lim")
                    ArrayFunc.RemoveIndex(orderedTolkensList, i + 1);

                if (tolken.GetData1() == LexemeType.Summation && i < orderedTolkensList.Count - 1 &&
                    orderedTolkensList[i + 1].GetData2() == "sum")
                    ArrayFunc.RemoveIndex(orderedTolkensList, i + 1);
                else if (tolken.GetData1() == LexemeType.Derivative && i < orderedTolkensList.Count - 1 &&
                         orderedTolkensList[i + 1].GetData1() == LexemeType.Differential &&
                         tolken.GetData2().Contains(orderedTolkensList[i + 1].GetData2()))
                {
                    ArrayFunc.RemoveIndex(orderedTolkensList, i + 1);
                }
                else if (tolken.GetData1() == LexemeType.FuncDeriv && i < orderedTolkensList.Count - 1 &&
                         orderedTolkensList[i + 1].GetData1() == LexemeType.FunctionDef &&
                         tolken.GetData2().EndsWith(orderedTolkensList[i + 1].GetData2()))
                {
                    ArrayFunc.RemoveIndex(orderedTolkensList, i + 1);
                }
                else if (i >= 2 && tolken.GetData1() == LexemeType.FuncIden && orderedTolkensList[i - 1].GetData2() == "^" &&
                         orderedTolkensList[i - 2].GetData1() == LexemeType.Identifier)
                {
                    string potentialFuncIden = orderedTolkensList[i - 2].GetData2();

                    // If the function is defined or if there is only one lexeme contained in between the parentheses then use.
                    if ((i < orderedTolkensList.Count - 3 &&
                         orderedTolkensList[i + 3].GetData1() == LexemeType.FuncArgEnd) ||
                        (_definedFuncs.Contains(potentialFuncIden) ||
                         p_EvalData.GetFuncDefs().IsFuncDefined(potentialFuncIden)))
                    {
                        orderedTolkensList[i - 2].SetData1(LexemeType.FuncDeriv);

                        // Use the proper parsing notation.
                        string temp = orderedTolkensList[i - 1].GetData2() + orderedTolkensList[i].GetData2();
                        orderedTolkensList[i - 1].SetData2(temp);
                        orderedTolkensList[i - 2].SetData2(orderedTolkensList[i - 2].GetData2() + temp);
                        ArrayFunc.RemoveIndex(orderedTolkensList, i--);
                        ArrayFunc.RemoveIndex(orderedTolkensList, i--);
                    }
                }
            }

            for (int i = 0; i < orderedTolkensList.Count; ++i)
            {
                // Sallow up the next lexemes.
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> tolken = orderedTolkensList[i];
                if (tolken.GetData1() == LexemeType.Limit)
                {
                    int depth = 1;
                    int endIndex = -1;
                    string totalStr = "";
                    for (int j = i + 1; j < orderedTolkensList.Count; ++j)
                    {
                        totalStr += orderedTolkensList[j].GetData2();
                        if (orderedTolkensList[j].GetData1() == LexemeType.StartPara)
                        {
                            depth++;
                        }
                        else if (orderedTolkensList[j].GetData1() == LexemeType.EndPara)
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
                        return null;
                    orderedTolkensList[i].SetData2(orderedTolkensList[i].GetData2() + totalStr);
                    i++;
                    orderedTolkensList.RemoveRange(i, endIndex + 1 - i);
                    i = endIndex;
                }
            }

            // Make sure there are no lone summations.
            foreach (TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> tolken in orderedTolkensList)
            {
                if (tolken.GetData1() == LexemeType.ErrorType)
                {
                    if (tolken.GetData2() == "sum")
                        pParseErrors.Add("Incorrectly formatted summation.");
                    else if (tolken.GetData2() == "inf")
                        pParseErrors.Add("Incorrect usage of infinity.");
                    else if (tolken.GetData2() == "lim")
                        pParseErrors.Add("Incorrect usage of limit.");

                    return null;
                }
            }

            _funcStore = new Dictionary<string, List<List<TypePair<LexemeType,string>>>>();
            if (!CreateMultiVariableFuncs(ref orderedTolkensList))
                return null;

            return orderedTolkensList;
        }

        /// <summary>
        ///     Checks that the coefficients are always at the front of the algebra group.
        ///     4x^(2)y will pass x^(2)4y will not.
        /// </summary>
        /// <returns></returns>
        private static bool CheckCoeffCorrectness(List<TypePair<LexemeType,string>> lexTable, ref List<string> pParseErrors)
        {
            LexemeType lexType = LexemeType.ErrorType;
            for (int i = 0; i < lexTable.Count; ++i)
            {
                if (lexTable[i].GetData1() == LexemeType.Integral)
                    lexType = LexemeType.Integral;
                else if (lexTable[i].GetData1() == LexemeType.Summation)
                    lexType = LexemeType.Summation;
                else if (lexTable[i].GetData1() == LexemeType.Operator && lexTable[i].GetData2() == "^" &&
                         i < lexTable.Count - 1 &&
                         lexTable[i + 1].GetData1() == LexemeType.Number && lexTable[i + 1].GetData2().Length > 1)
                {
                    if (lexType == LexemeType.Summation)
                        pParseErrors.Add("A number cannot follow a summation, put parentheses.");
                    else if (lexType == LexemeType.Integral)
                        pParseErrors.Add("A number cannot follow an integral, put parentheses.");
                    return false;
                }
            }

            return true;
        }

        public List<EqSet> ParseInput(string inputStr, out List<List<TypePair<LexemeType,string>>> lexemeTables, ref List<string> pParseErrors)
        {
            _vectorStore = new Dictionary<string, List<TypePair<LexemeType,string>>>();
            _vecStoreIndex = 0;
            string[] equationSets = inputStr.Split(';');
            lexemeTables = new List<List<TypePair<LexemeType,string>>>();

            List<EqSet> eqSets = new List<EqSet>();

            foreach (string equationSet in equationSets)
            {
                List<TypePair<LexemeType,string>> setLexemeTable = CreateLexemeTable(equationSet, ref pParseErrors);

                if (setLexemeTable == null || setLexemeTable.Count == 0)
                    return null;

                if (!p_EvalData.GetPlainTextInput() && !CheckCoeffCorrectness(setLexemeTable, ref pParseErrors))
                {
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
                    ExComp finalNoRedun = algebraTerm.RemoveRedundancies(true);
                    if (finalNoRedun is AgOp)
                    {
                        return null;
                    }
                    Type startingType = finalNoRedun.GetType();
                    if (finalNoRedun is AlgebraTerm)
                    {
                        AlgebraTerm finalTerm = finalNoRedun as AlgebraTerm;
                        finalNoRedun = finalTerm.WeakMakeWorkable(ref pParseErrors, ref p_EvalData);
                        if (finalNoRedun == null)
                            return null;
                    }
                    EqSet addEqSet = new EqSet(finalNoRedun, equationSet);
                    addEqSet.SetStartingType(startingType);
                    eqSets.Add(addEqSet);
                    continue;
                }
                List<LexemeType> solveTypes = new List<LexemeType>();
                List<TypePair<LexemeType,string>>[] parsedTables = SplitLexemeTable(setLexemeTable, out solveTypes, ref pParseErrors);
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
                    foreach (List<TypePair<LexemeType,string>> lt in parsedTables)
                    {
                        if (lt.Count == 0)
                            return null;
                        lexemeTables.Add(lt);
                        if (LexemeTableContains(lt, LexemeType.EqualsOp))
                            return null;

                        AlgebraTerm algebraTerm = LexemeTableToAlgebraTerm(lt, ref pParseErrors, true);
                        if (algebraTerm == null)
                            return null;
                        ExComp finalEx = algebraTerm.RemoveRedundancies(true);
                        if (finalEx is AlgebraTerm)
                        {
                            AlgebraTerm finalTerm = finalEx as AlgebraTerm;
                            finalEx = finalTerm.WeakMakeWorkable(ref pParseErrors, ref p_EvalData);
                            if (finalEx == null)
                                return null;
                        }

                        parsedTerms.Add(finalEx);
                    }

                    EqSet singleEqSet = new EqSet(parsedTerms, solveTypes);

                    eqSets.Add(singleEqSet);
                    continue;
                }

                ExComp[] parsedExs = new ExComp[2];

                for (int i = 0; i < parsedTables.Length; ++i)
                {
                    List<TypePair<LexemeType,string>> lexemeTable = parsedTables[i];
                    if (lexemeTable.Count == 0)
                        return null;
                    lexemeTables.Add(lexemeTable);
                    if (LexemeTableContains(lexemeTable, LexemeType.EqualsOp))
                        return null;

                    AlgebraTerm algebraTerm = LexemeTableToAlgebraTerm(lexemeTable, ref pParseErrors, true);
                    if (algebraTerm == null)
                        return null;
                    ExComp finalExNoRedun = algebraTerm.RemoveRedundancies(true);
                    if (finalExNoRedun is AlgebraTerm)
                    {
                        AlgebraTerm finalTerm = finalExNoRedun as AlgebraTerm;
                        finalExNoRedun = finalTerm.WeakMakeWorkable(ref pParseErrors, ref p_EvalData);
                        if (finalExNoRedun == null)
                            return null;
                    }

                    parsedExs[i] = finalExNoRedun;
                }

                eqSets.Add(new EqSet(parsedExs[0], parsedExs[1], solveType));
            }

            return eqSets;
        }

        public static List<TypePair<LexemeType,string>>[] SplitLexemeTable(List<TypePair<LexemeType,string>> lt, out List<LexemeType> splitLexTypes,
            ref List<string> pParseErrors)
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

                List<int> inequalityIndexPairs = GetInequalityOpIndices(lt);

                List<List<TypePair<LexemeType,string>>> lexemeTables = new List<List<TypePair<LexemeType,string>>>();
                int prevEndIndex = 0;
                foreach (int inequalityIndex in inequalityIndexPairs)
                {
                    LexemeType inequalityType = lt[inequalityIndex].GetData1();
                    splitLexTypes.Add(inequalityType);

                    List<TypePair<LexemeType,string>> lexemeTableRange = lt.GetRange(prevEndIndex, inequalityIndex - prevEndIndex);
                    lexemeTables.Add(lexemeTableRange);
                    prevEndIndex = inequalityIndex + 1;
                }

                // Also add the ending part as it doesn't have a inequality after it.
                List<TypePair<LexemeType,string>> finalLtRange = lt.GetRange(prevEndIndex, lt.Count - prevEndIndex);
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

            List<TypePair<LexemeType,string>> left = lt.GetRange(0, splitIndex);
            int startIndex = splitIndex + 1;
            List<TypePair<LexemeType,string>> right = lt.GetRange(startIndex, lt.Count - startIndex);

            List<TypePair<LexemeType,string>>[] leftRight = new List<TypePair<LexemeType,string>>[] { left, right };

            return leftRight;
        }

        private bool FixIntegralDif(ref List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            int diffCount = 0;
            int intCount = 0;
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.Differential)
                    diffCount++;
                else if (lt[i].GetData1() == LexemeType.Integral)
                {
                    intCount++;
                    if (i > 0 && lt[i - 1].GetData1() == LexemeType.StartPara)
                        continue;
                    // Search for the corresponding differential.
                    int foundIndex = -1;
                    int depth = 0;
                    for (int j = i + 1; j < lt.Count; ++j)
                    {
                        if (lt[j].GetData1() == LexemeType.Integral)
                        {
                            depth++;
                        }
                        if (lt[j].GetData1() == LexemeType.Differential)
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

                    lt.Insert(i, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartPara, "("));
                    lt.Insert(foundIndex + 2, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EndPara, ")"));
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

        private static bool ApplyOrderingToOp(string opToOrder, string[] breakingOps, List<TypePair<LexemeType,string>> lexemeTable)
        {
            List<string> breakingOpsList = ArrayFunc.ToList(breakingOps);

            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lexeme = lexemeTable[i];

                if (lexeme.GetData2() == opToOrder)
                {
                    if (i == 0)
                        return false;

                    if (i > 0)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> prevLexeme = lexemeTable[i - 1];
                        // To allow for the alternate notation of raising trig functions to powers.
                        if ((TrigFunction.IsValidType(prevLexeme.GetData2()) ||
                             InverseTrigFunction.IsValidType(prevLexeme.GetData2())) && opToOrder == "^")
                            continue;
                    }

                    bool after = false;

                    int depth = 0;

                    // Does this instance occur after any of the breaking operators?
                    for (int j = 0; j < i; ++j)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lex = lexemeTable[j];
                        if (lex.GetData1() == LexemeType.StartPara)
                            depth++;
                        else if (lex.GetData1() == LexemeType.EndPara)
                            depth--;

                        if (depth == 0 && breakingOpsList.Contains(lexemeTable[j].GetData2()))
                        {
                            after = true;
                            break;
                        }
                    }

                    if (!after)
                        continue;

                    TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> beforeLexeme = lexemeTable[i - 1];
                    if (i + 1 > lexemeTable.Count - 1)
                        return false;
                    TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> afterLexeme = lexemeTable[i + 1];

                    int startPos = -1;
                    // Navigate backwards.
                    for (int j = i; j >= 0; --j)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> searchLexeme = lexemeTable[j];
                        LexemeType type = searchLexeme.GetData1();

                        if (type == LexemeType.StartPara)
                            depth++;
                        else if (type == LexemeType.EndPara)
                            depth--;

                        bool validBreakingOp = type == LexemeType.Operator
                            ? breakingOpsList.Contains(searchLexeme.GetData2())
                            : false;

                        if ((depth == 0 && validBreakingOp) || depth > 0)
                        {
                            startPos = j + 1;
                            i++;
                            j++;
                            break;
                        }
                        if (depth == 0 && j == 0)
                        {
                            startPos = j;
                            i++;
                            j++;
                            break;
                        }
                    }

                    depth = 0;

                    int endPos = -1;
                    // Navigate forward.
                    for (int j = i; j < lexemeTable.Count; ++j)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> searchLexeme = lexemeTable[j];
                        LexemeType type = searchLexeme.GetData1();

                        bool validBreakingOp = type == LexemeType.Operator
                            ? breakingOpsList.Contains(searchLexeme.GetData2())
                            : false;

                        if (type == LexemeType.StartPara)
                            depth++;
                        else if (type == LexemeType.EndPara)
                            depth--;

                        if ((depth == 0 && validBreakingOp) || depth < 0)
                        {
                            endPos = j + 1;
                            break;
                        }
                        if (depth == 0 && j == lexemeTable.Count - 1)
                        {
                            endPos = j + 2;
                            break;
                        }
                    }

                    if (endPos == -1 || startPos == -1)
                        continue;

                    lexemeTable.Insert(startPos, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartPara, "("));

                    if (endPos > 0 && lexemeTable[endPos - 1].GetData1() == LexemeType.Differential)
                        endPos--;

                    lexemeTable.Insert(lexemeTable[endPos - 1].GetData2() == "|" ? endPos - 1 : endPos,
                        new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EndPara, ")"));
                }
            }

            return true;
        }

        private bool ApplyOrderOfOperationsToLexemeTable(List<TypePair<LexemeType,string>> lexemeTable, ref List<string> pParseErrors,
            bool fixIntegrals)
        {
            //if (fixIntegrals)
            //{
            //    // Integrals and differentials screw up the entire PEMDAS process so parantheses have to be put around integrals.
            //    if (!FixIntegralDif(ref lexemeTable, ref pParseErrors))       // Will return false if there are mismatched integrals and differentials.
            //        return false;
            //}

            string[] pBreakingOps = new string[] { "*", "/", "+", "-" };
            if (!ApplyOrderingToOp("^", pBreakingOps, lexemeTable))
                return false;

            string[] mdBreakingOps = new string[] { "+", "-" };

            if (!ApplyOrderingToOp("*", mdBreakingOps, lexemeTable))
                return false;
            if (!ApplyOrderingToOp("/", mdBreakingOps, lexemeTable))
                return false;

            return true;
        }

        /// <summary>
        /// Basic replace functionality to make the input more usable.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string CleanTexInput(string str)
        {
            // Replace the formatted LaTeX math with the ASCII represenation for easier parsing.
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


            if (!p_EvalData.GetPlainTextInput())
            {
                // Remove the empty group identifier. This is used for formatting purposes on the web.
                str = str.Replace(MATH_EMPTY_GP, "");
            }
            else
            {
                str = Regex.Replace(str, @"(?<!(\\|o))int", "\\int ");
                str = Regex.Replace(str, @"(?<!\\)oint", "\\oint");
            }


            if (str.Contains("\\int") || str.Contains("\\oint"))
            {
                _rulesets[DIFF_RULE_INDEX] = new TypePair<string, LexemeType>("d(" + IDEN_MATCH + ")",
                    LexemeType.Differential);
                // This is somewhat of a hack together but it should work.
                str = str.Replace("dA", "dxdy");
                str = str.Replace("dV", "dxdydz");
            }
            else
                ResetDiffParsing();

            // Another dirty technique that should probably be changed.
            str = str.Replace("\\vec(i)", "[1,0,0]");
            str = str.Replace("\\vec(j)", "[0,1,0]");
            str = str.Replace("\\vec(k)", "[0,0,1]");

            str = str.Replace("\\", "");
            str = str.Replace(" ", "");
            str = str.Replace("->", "to");

            str = str.Replace("sin^(-1)", "asin");
            str = str.Replace("cos^(-1)", "acos");
            str = str.Replace("tan^(-1)", "atan");
            str = str.Replace("csc^(-1)", "acsc");
            str = str.Replace("sec^(-1)", "asec");
            str = str.Replace("cot^(-1)", "acot");
            str = str.Replace("infty", "inf");
            // Remove the vector notation for now it can removed later.
            str = str.Replace("vec(", "(");

            return str;
        }

        private static void FixLexemeTableUserInput(ref List<TypePair<LexemeType,string>> lexemeTable)
        {
            // Fixes things like 'xy' to 'x*y'.
            // Also fixes the negation operator versus minus operator problem.
            bool multipliablePreceeding = false;
            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lexeme = lexemeTable[i];

                // Special case for the absolute value.
                if (i > 0 && lexemeTable[i].GetData1() == LexemeType.Bar && lexemeTable[i - 1].GetData1() == LexemeType.Bar)
                {
                    lexemeTable.Insert(i, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Operator, "*"));
                }

                if (i > 0 && lexemeTable[i - 1].GetData1() == LexemeType.EndBracket &&
                    lexemeTable[i].GetData1() == LexemeType.StartBracket)
                {
                    lexemeTable.Insert(i, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Operator, CompWiseMul.IDEN));
                }

                if ((lexeme.GetData1() == LexemeType.Identifier ||
                     lexeme.GetData1() == LexemeType.VectorStore ||
                     lexeme.GetData1() == LexemeType.I_Number ||
                     lexeme.GetData1() == LexemeType.Number ||
                     lexeme.GetData1() == LexemeType.Constant ||
                     lexeme.GetData1() == LexemeType.StartPara ||
                     lexeme.GetData1() == LexemeType.StartBracket ||
                     lexeme.GetData1() == LexemeType.Function ||
                     lexeme.GetData1() == LexemeType.Derivative ||
                     lexeme.GetData1() == LexemeType.Limit ||
                     lexeme.GetData1() == LexemeType.Integral ||
                     lexeme.GetData1() == LexemeType.FunctionDef ||
                     lexeme.GetData1() == LexemeType.FuncIden)
                    && multipliablePreceeding)
                {
                    lexemeTable.Insert(i++, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Operator, "*"));

                    if (lexeme.GetData1() == LexemeType.StartPara ||
                        lexeme.GetData1() == LexemeType.Function ||
                        lexeme.GetData1() == LexemeType.Derivative ||
                        lexeme.GetData1() == LexemeType.Limit ||
                        lexeme.GetData1() == LexemeType.Integral ||
                        lexeme.GetData1() == LexemeType.FunctionDef ||
                        lexeme.GetData1() == LexemeType.FuncIden ||
                        lexeme.GetData1() == LexemeType.StartBracket)
                        multipliablePreceeding = false;
                    continue;
                }

                if (lexeme.GetData1() == LexemeType.Identifier ||
                    lexeme.GetData1() == LexemeType.VectorStore ||
                    lexeme.GetData1() == LexemeType.I_Number ||
                    lexeme.GetData1() == LexemeType.Number ||
                    lexeme.GetData1() == LexemeType.Constant ||
                    lexeme.GetData1() == LexemeType.Number ||
                    lexeme.GetData1() == LexemeType.EndPara ||
                    lexeme.GetData1() == LexemeType.EndBracket)
                    multipliablePreceeding = true;
                else
                    multipliablePreceeding = false;
            }

            bool operatorPreceeding = true;
            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lexeme = lexemeTable[i];
                if (operatorPreceeding && lexeme.GetData1() == LexemeType.Operator && lexeme.GetData2() == "-")
                {
                    if (i == lexemeTable.Count - 1)
                    {
                        lexemeTable = null;
                        return;
                    }
                    TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> nextLexeme = lexemeTable[i + 1];
                    if (nextLexeme.GetData1() == LexemeType.Number)
                    {
                        lexemeTable[i + 1].SetData2(nextLexeme.GetData2().Insert(0, "-"));
                        ArrayFunc.RemoveIndex(lexemeTable, i--);
                    }
                    else
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> negateNumLexeme = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Number, "-1");
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> multiplyLexeme = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Operator, "*");
                        ArrayFunc.RemoveIndex(lexemeTable, i);
                        lexemeTable.Insert(i, negateNumLexeme);
                        lexemeTable.Insert(i + 1, multiplyLexeme);
                    }
                    continue;
                }

                if (lexeme.GetData1() == LexemeType.Operator || (lexeme.GetData1() == LexemeType.StartPara))
                    operatorPreceeding = true;
                else
                    operatorPreceeding = false;
            }
        }

        private static int GetFirstIndexOccur(List<TypePair<LexemeType,string>> lt, LexemeType lexType)
        {
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == lexType)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// </summary>
        /// <param name="lexemeTable"></param>
        /// <param name="value"></param>
        /// <param name="allowGrouped">If the lexeme must be found when the depth count of the parantheses is zero.</param>
        /// <returns></returns>
        private static List<int> GetIndicesOfValueInLexemeTable(List<TypePair<LexemeType,string>> lexemeTable, string value, bool allowGrouped)
        {
            List<int> indices = new List<int>();
            int depth = 0;
            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                if (lexemeTable[i].GetData1() == LexemeType.StartPara)
                    depth++;
                else if (lexemeTable[i].GetData1() == LexemeType.EndPara)
                    depth--;
                else if (lexemeTable[i].GetData2() == value && (depth == 0 || allowGrouped))
                    indices.Add(i);
            }

            return indices;
        }

        private static List<int> GetInequalityOpIndices(List<TypePair<LexemeType,string>> lt)
        {
            List<int> compTypeIndexPairs = new List<int>();
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.Greater ||
                    lt[i].GetData1() == LexemeType.GreaterEqual ||
                    lt[i].GetData1() == LexemeType.Less ||
                    lt[i].GetData1() == LexemeType.LessEqual)
                    compTypeIndexPairs.Add(i);
            }

            return compTypeIndexPairs;
        }

        private static int GetOccurCount(List<TypePair<LexemeType,string>> lt, LexemeType lexType)
        {
            int count = 0;
            foreach (TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lex in lt)
            {
                if (lex.GetData1() == lexType)
                    count++;
            }

            return count;
        }

        private static bool LexemeTableContains(List<TypePair<LexemeType,string>> lexemeTable, LexemeType lexemeType)
        {
            foreach (TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lexeme in lexemeTable)
            {
                if (lexeme.GetData1() == lexemeType)
                    return true;
            }

            return false;
        }

        private static bool LexemeTableContainsComparisonOp(List<TypePair<LexemeType,string>> lt)
        {
            foreach (TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lex in lt)
            {
                LexemeType tp = lex.GetData1();
                if (tp == LexemeType.EqualsOp || tp == LexemeType.Less || tp == LexemeType.LessEqual ||
                    tp == LexemeType.Greater || tp == LexemeType.GreaterEqual)
                {
                    return true;
                }
            }

            return false;
        }

        private AlgebraTerm LexemeTableToAlgebraTerm(List<TypePair<LexemeType,string>> lexemeTable, ref List<string> pParseErrors, bool fixIntegrals)
        {
            if (lexemeTable.Count > 0 && lexemeTable[0].GetData1() == LexemeType.Integral)
            {
                int depth = 0;
                int endIndex = -1;

                for (int i = 0; i < lexemeTable.Count; ++i)
                {
                    if (lexemeTable[i].GetData1() == LexemeType.Differential)
                    {
                        depth--;

                        if (depth == 0)
                        {
                            endIndex = i;
                            break;
                        }
                    }
                    else if (lexemeTable[i].GetData1() == LexemeType.Integral)
                    {
                        bool nextIden = false;
                        if (i + 1 < lexemeTable.Count && lexemeTable[i + 1].GetData1() == LexemeType.Identifier)
                            nextIden = true;
                        else if (i + 3 < lexemeTable.Count &&
                                 lexemeTable[i + 1].GetData1() == LexemeType.StartPara &&
                                 lexemeTable[i + 2].GetData1() == LexemeType.Identifier &&
                                 lexemeTable[i + 3].GetData1() == LexemeType.EndPara)
                            nextIden = true;

                        // Check for surface integrals which break the rule of one integral to one differential.
                        if (
                            !(i > 0 && i + 2 < lexemeTable.Count && lexemeTable[i - 1].GetData1() == LexemeType.Integral &&
                              lexemeTable[i].GetData2().Contains("_") &&
                              !lexemeTable[i - 1].GetData2().Contains("_") && nextIden &&
                              lexemeTable[i + 2].GetData1() != LexemeType.Operator))
                        {
                            depth++;
                        }
                    }
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

            if (!CorrectFunctions(ref lexemeTable))
                return null;

            if (!ApplyOrderOfOperationsToLexemeTable(lexemeTable, ref pParseErrors, fixIntegrals))
                return null;

            AlgebraTerm algebraTerm = new AlgebraTerm();

            for (int i = 0; i < lexemeTable.Count; ++i)
            {
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lexeme = lexemeTable[i];
                ExComp toAdd = LexemeToExComp(lexemeTable, ref i, ref pParseErrors);
                if (toAdd == null)
                    return null;
                algebraTerm.Add(toAdd);
            }

            if (algebraTerm.GetTermCount() == 1 && algebraTerm[0] is AgOp)
                return null;

            return algebraTerm;
        }

        private bool CreateVectorStore(ref List<TypePair<LexemeType,string>> lt)
        {
            int depth = 0;
            int prevIndex = -1;
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.StartBracket)
                {
                    if (depth == 0)
                        prevIndex = i;
                    depth++;
                }
                else if (lt[i].GetData1() == LexemeType.EndBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        // Change into a vector store.
                        if (prevIndex == -1)
                            return false;
                        List<TypePair<LexemeType,string>> vecStoreLt = lt.GetRange(prevIndex + 1, i - (prevIndex + 1));

                        _vectorStore.Add(_vecStoreIndex.ToString(), vecStoreLt);
                        // Also remove the brackets.
                        int remCount = i - (prevIndex - 1);
                        lt.RemoveRange(prevIndex, remCount);
                        lt.Insert(prevIndex, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.VectorStore, _vecStoreIndex.ToString()));
                        _vecStoreIndex++;
                        i -= remCount;
                        // To skip over the vector store itself.
                        i++;
                    }
                }
            }

            return true;
        }

        private int GetGroupRange(int dimen, int i, List<TypePair<LexemeType,string>> lt)
        {
            int depth = 0;
            int endIndex = -1;
            for (int j = i; j < lt.Count; ++j)
            {
                if (lt[j].GetData1() == LexemeType.StartPara)
                {
                    depth++;
                }
                else if (lt[j].GetData1() == LexemeType.EndPara)
                {
                    depth--;
                    if (depth == 0)
                    {
                        dimen--;
                        if (dimen == 0)
                        {
                            endIndex = j;
                            break;
                        }
                    }
                }
            }

            return endIndex;
        }

        private bool CorrectFunctions(ref List<TypePair<LexemeType,string>> lt)
        {
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() != LexemeType.Function)
                    continue;

                int depth = -1;
                if (lt[i].GetData2() == "binom" || lt[i].GetData2() == "vectora")
                    depth = 2;
                else if (lt[i].GetData2() == "vectorb")
                    depth = 3;
                else if (lt[i].GetData2() == "vectorc")
                    depth = 4;

                if (depth == -1)
                    continue;

                int endIndex = GetGroupRange(depth, i, lt);
                if (endIndex == -1)
                    return false;
                List<TypePair<LexemeType,string>> funcRange = lt.GetRange(i + 1, endIndex - i);
                lt.RemoveRange(i + 1, endIndex - i);
                lt[i] = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Function, lt[i].GetData2() + "|" + (++_vecStoreIndex));
                _vectorStore[_vecStoreIndex.ToString()] = funcRange;
            }

            return true;
        }

        private bool CorrectFactorials(ref List<TypePair<LexemeType,string>> lt)
        {
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData2() != "!" || lt[i].GetData1() != LexemeType.Function)
                    continue;

                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lex = lt[i];
                lt[i].SetData2("FACT");

                // Find the end of the group going in the opposite direction.
                if (i == 0)
                    return false;

                // Remove the operator that is probably preceeding it.
                if (lt[i - 1].GetData1() == LexemeType.Operator)
                {
                    ArrayFunc.RemoveIndex(lt, i - 1);
                    i--;
                }

                if (i != lt.Count - 1 && lt[i + 1].GetData1() == LexemeType.Operator)
                {
                    ArrayFunc.RemoveIndex(lt, i + 1);
                }

                if (!(lt[i - 1].GetData1() == LexemeType.EndPara ||
                      lt[i - 1].GetData1() == LexemeType.EndBracket ||
                      lt[i - 1].GetData1() == LexemeType.Bar))
                {
                    // Just the single lexeme.
                    if (lt[i - 1].GetData1() == LexemeType.Operator)
                        return false;
                    ArrayFunc.RemoveIndex(lt, i);
                    lt.Insert(i - 1, lex);
                    continue;
                }

                if (lt.Count < 4)
                    return false;

                LexemeType search = lt[i - 1].GetData1();
                LexemeType endType = LexemeTypeHelper.GetOpposite(search);

                int depth = 1;
                int endIndex = -1;
                for (int j = i - 2; j >= 0; --j)
                {
                    if (lt[j].GetData1() == search)
                        depth++;
                    else if (lt[j].GetData1() == endType)
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

                ArrayFunc.RemoveIndex(lt, i);
                lt.Insert(endIndex, lex);
            }

            return true;
        }

        private bool CreateMultiVariableFuncs(ref List<TypePair<LexemeType,string>> lt)
        {
            int totalVectorDepth = 0;
            for (int i = 0; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.Function && lt[i].GetData2().StartsWith("vector"))
                {
                    // Skip to the end of this vector function.
                    string removed = StringFunc.Rm(lt[i].GetData2(), 0, "vector".Length);
                    int dimen;
                    if (removed == "a")
                        dimen = 2;
                    else if (removed == "b")
                        dimen = 3;
                    else
                        dimen = 4;

                    int matDeclDepth = 0;
                    for (int j = i; j < lt.Count; ++j)
                    {
                        if (lt[j].GetData1() == LexemeType.StartPara)
                        {
                            matDeclDepth++;
                        }
                        else if (lt[j].GetData1() == LexemeType.EndPara)
                        {
                            matDeclDepth--;
                            if (matDeclDepth == 0 && (--dimen) == 0)
                            {
                                i = j;
                                break;
                            }
                        }
                    }
                }
                if (lt[i].GetData1() == LexemeType.Identifier)
                {
                    if (lt[i].GetData2().Contains("_"))
                    {
                        // This could be the partial derivative notation of F_x.
                        string[] split = lt[i].GetData2().Split('_');
                        string funcIden = split[0];
                        FunctionDefinition funcDef = p_EvalData.GetFuncDefs().GetFuncDef(funcIden);
                        if (funcDef != null)
                        {
                            // The next part should be the variable with respect to.
                            if (split[1].StartsWith("(") && split.Length > 3)
                                split[1] = split[1].Substring(1, split[1].Length - 2);
                            if (funcDef.IsArg(split[1]))
                            {
                                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> insertLex = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Derivative, p_EvalData.GetPlainTextInput()
                                    ? "(partial" + funcIden + ")/(partial" + split[1] + ")"
                                    : "frac(partial" + funcIden + ")(partial" + split[1] + ")");
                                ArrayFunc.RemoveIndex(lt, i);
                                lt.Insert(i, insertLex);
                            }
                        }
                    }
                }
                else if (lt[i].GetData1() == LexemeType.StartBracket)
                    totalVectorDepth++;
                else if (lt[i].GetData1() == LexemeType.EndBracket)
                    totalVectorDepth--;
                if (lt[i].GetData1() != LexemeType.Comma || totalVectorDepth != 0)
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
                    if ((lt[j].GetData1() == LexemeType.StartPara || lt[j].GetData1() == LexemeType.FuncArgStart) &&
                        vectorDepth == 0)
                    {
                        depth++;
                        if (depth == 0)
                        {
                            funcStart = j;
                            break;
                        }
                    }
                    else if ((lt[j].GetData1() == LexemeType.EndPara || lt[j].GetData1() == LexemeType.FuncArgEnd) &&
                             vectorDepth == 0)
                        depth--;
                    else if (lt[j].GetData1() == LexemeType.StartBracket)
                        vectorDepth++;
                    else if (lt[j].GetData1() == LexemeType.EndBracket)
                        vectorDepth--;
                }

                // The function start cannot be zero as there needs to be an identifier as well.
                if (funcStart <= 0)
                    return false;

                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> iden = lt[funcStart - 1];
                if ((i - 1) > 0 && iden.GetData1() == LexemeType.Operator && iden.GetData2() == "*")
                    iden = lt[funcStart - 2];

                if (iden.GetData1() != LexemeType.Identifier && iden.GetData1() != LexemeType.FuncIden)
                    return false;

                string funcName = iden.GetData2();

                List<List<TypePair<LexemeType,string>>> args = new List<List<TypePair<LexemeType,string>>>();

                List<TypePair<LexemeType,string>> firstLexRange = lt.GetRange(funcStart + 1, i - (funcStart + 1));
                args.Add(firstLexRange);

                int funcEnd = -1;
                // The comma now acts like a start paranthese.
                depth = 1;
                int prevCommaIndex = i;
                vectorDepth = 0;
                for (int j = i + 1; j < lt.Count; ++j)
                {
                    if (depth == 1 && lt[j].GetData1() == LexemeType.Comma && vectorDepth == 0)
                    {
                        List<TypePair<LexemeType,string>> lexRange = lt.GetRange(prevCommaIndex + 1, j - (prevCommaIndex + 1));
                        args.Add(lexRange);
                        prevCommaIndex = j;
                    }
                    else if (lt[j].GetData1() == LexemeType.StartBracket)
                        vectorDepth++;
                    else if (lt[j].GetData1() == LexemeType.EndBracket)
                        vectorDepth--;
                    else if ((lt[j].GetData1() == LexemeType.StartPara || lt[j].GetData1() == LexemeType.FuncArgStart) &&
                             vectorDepth == 0)
                    {
                        depth++;
                    }
                    else if ((lt[j].GetData1() == LexemeType.EndPara || lt[j].GetData1() == LexemeType.FuncArgEnd) &&
                             vectorDepth == 0)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            List<TypePair<LexemeType,string>> lexRange = lt.GetRange(prevCommaIndex + 1, j - (prevCommaIndex + 1));
                            args.Add(lexRange);
                            funcEnd = j;
                            break;
                        }
                    }
                }

                if (funcEnd == -1)
                    return false;

                lt.RemoveRange(funcStart - 1, funcEnd - (funcStart - 2));

                lt.Insert(funcStart - 1, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.MultiVarFuncStore, funcName));
                _funcStore[funcName] = args;
                i = funcStart;
            }

            return true;
        }

        private ExComp LexemeToExComp(List<TypePair<LexemeType, string>> lexemeTable, ref int currentIndex,
            ref List<string> pParseErrors)
        {
            if (currentIndex >= lexemeTable.Count)
                return null;
            TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lexeme = lexemeTable[currentIndex];
            int endIndex = -1;
            int depth = 0;
            int startIndex;
            AlgebraTerm algebraTerm;
            List<TypePair<LexemeType,string>> algebraTermLexemeTable;
            switch (lexeme.GetData1())
            {
                case LexemeType.VectorStore:
                    ExComp parsedVector = ParseVector(ref currentIndex, lexemeTable, ref pParseErrors);
                    return parsedVector;

                case LexemeType.MultiVarFuncStore:
                    ExComp multiValFunc = ParseMultiVarFunc(ref currentIndex, lexemeTable, ref pParseErrors);
                    return multiValFunc;
                case LexemeType.Infinity:
                    return ExNumber.GetPosInfinity();

                case LexemeType.Function:
                    endIndex = -1;
                    depth = 0;

                    if (lexeme.GetData2() == "log_")
                    {
                        ExComp parsedLogBaseInner = ParseLogBaseInner(ref currentIndex, lexemeTable, ref pParseErrors);
                        return parsedLogBaseInner;
                    }
                    if (lexeme.GetData2() == "root")
                    {
                        ExComp rootParsed = ParseRootInner(ref currentIndex, lexemeTable, ref pParseErrors);
                        return rootParsed;
                    }
                    if (lexeme.GetData2() == "frac")
                    {
                        ExComp parsedFraction = ParseFraction(ref currentIndex, lexemeTable, ref pParseErrors);
                        return parsedFraction;
                    }
                    if (lexeme.GetData2().StartsWith("vector"))
                    {
                        ExComp parsedVectorEx = ParseVectorNotation(ref currentIndex, lexemeTable, ref pParseErrors);
                        return parsedVectorEx;
                    }
                    if (lexeme.GetData2().StartsWith("binom"))
                    {
                        ExComp parsedBinom = ParseBinomNotation(ref currentIndex, lexemeTable, ref pParseErrors);
                        return parsedBinom;
                    }

                    if (TrigFunction.IsValidType(lexeme.GetData2()) ||
                        InverseTrigFunction.IsValidType(lexeme.GetData2()))
                    {
                        if (currentIndex + 2 > lexemeTable.Count - 1)
                            return null;

                        if (lexemeTable[currentIndex + 1].GetData1() == LexemeType.Operator)
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

                            ExComp trigFunc = BasicAppliedFunc.Parse(lexeme.GetData2(), inner, ref pParseErrors);
                            if (trigFunc == null)
                                return null;

                            if (power is AlgebraTerm)
                                power = (power as AlgebraTerm).RemoveRedundancies(false);

                            if (ExNumber.GetNegOne().IsEqualTo(power) && trigFunc is TrigFunction)
                            {
                                return (trigFunc as TrigFunction).GetInverseOf();
                            }

                            return new AlgebraTerm(trigFunc, new PowOp(), power);
                        }
                    }

                    for (int i = currentIndex + 2; i < lexemeTable.Count; ++i)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> compareLexeme = lexemeTable[i];
                        if (compareLexeme.GetData1() == LexemeType.StartPara)
                            depth++;
                        if (compareLexeme.GetData1() == LexemeType.EndPara)
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
                        if ((lexeme.GetData2() == "FACT" || lexeme.GetData2().StartsWith("nabla")) &&
                            currentIndex != lexemeTable.Count - 1)
                        {
                            currentIndex++;
                            if (lexemeTable[currentIndex].GetData1() == LexemeType.Operator)
                            {
                                if (lexemeTable[currentIndex].GetData2() == "*" ||
                                    lexemeTable[currentIndex].GetData2() == CrossProductOp.IDEN)
                                {
                                    lexeme.SetData2(lexeme.GetData2() + lexemeTable[currentIndex].GetData2());
                                    currentIndex++;
                                }
                                else
                                    return null;
                            }

                            // Just parse the next lexeme.
                            ExComp innerEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);

                            currentIndex = currentIndex + 1;
                            if (lexeme.GetData2() == "FACT")
                                return new FactorialFunction(innerEx);
                            string parseLexeme;
                            if (lexeme.GetData2().Contains(CrossProductOp.IDEN))
                                parseLexeme = "curl";
                            else if (lexeme.GetData2().Contains("*"))
                                parseLexeme = "div";
                            else
                            {
                                // Gradient.
                                if (innerEx is ExMatrix)
                                {
                                    pParseErrors.Add("Cannot take the gradient of a vector.");
                                    return null;
                                }
                                return new GradientFunc(innerEx);
                            }

                            ExComp basicAppliedParsed = BasicAppliedFunc.Parse(parseLexeme, innerEx, ref pParseErrors);
                            return basicAppliedParsed;
                        }
                        return null;
                    }

                    startIndex = currentIndex + 2;
                    algebraTermLexemeTable = lexemeTable.GetRange(startIndex, endIndex - startIndex);
                    currentIndex = endIndex;
                    if (algebraTermLexemeTable.Count == 0)
                        return null;
                    algebraTerm = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors, false);
                    if (algebraTerm == null)
                        return null;

                    ExComp func = BasicAppliedFunc.Parse(lexeme.GetData2(), algebraTerm, ref pParseErrors);
                    return func;

                case LexemeType.FunctionDef:
                    FunctionDefinition funcDef = new FunctionDefinition();
                    if (!funcDef.Parse(lexeme.GetData2()))
                        return null;

                    return funcDef;

                case LexemeType.FuncIden:

                    List<List<TypePair<LexemeType,string>>> args = new List<List<TypePair<LexemeType,string>>>();

                    depth = 0;
                    for (int i = currentIndex + 2; i < lexemeTable.Count; ++i)
                    {
                        if (lexemeTable[i].GetData1() == LexemeType.FuncArgStart)
                            depth++;
                        else if (lexemeTable[i].GetData1() == LexemeType.FuncArgEnd)
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
                    List<TypePair<LexemeType,string>> funcCallLt = lexemeTable.GetRange(startIndex, endIndex - startIndex);

                    currentIndex = endIndex;

                    // Split the func call into the arguments which are seperated by commas.
                    int argStartIndex = 0;
                    for (int i = 0; i < funcCallLt.Count; ++i)
                    {
                        if (funcCallLt[i].GetData1() == LexemeType.Comma)
                        {
                            args.Add(funcCallLt.GetRange(argStartIndex, i - argStartIndex));
                            argStartIndex = i;
                        }
                    }

                    args.Add(funcCallLt.GetRange(argStartIndex, funcCallLt.Count - argStartIndex));

                    ExComp[] argExs = new ExComp[args.Count];
                    for (int i = 0; i < args.Count; ++i)
                    {
                        argExs[i] = LexemeTableToAlgebraTerm(args[i], ref pParseErrors, false);
                        if (argExs[i] == null)
                            return null;
                    }

                    FunctionDefinition tmp = p_EvalData.GetFuncDefs().GetFuncDef(lexeme.GetData2());
                    if (tmp == null)
                    {
                        if (!_definedFuncs.Contains(lexeme.GetData2()))
                        {
                            pParseErrors.Add("Function is not defined.");
                            return null;
                        }

                        AlgebraComp[] inputArgs = new AlgebraComp[argExs.Length];
                        for (int i = 0; i < inputArgs.Length; ++i)
                        {
                            inputArgs[i] = new AlgebraComp("$x");
                        }
                        return new FunctionDefinition(new AlgebraComp(lexeme.GetData2()), inputArgs, argExs, true);
                    }

                    FunctionDefinition funcCall = (FunctionDefinition)tmp.CloneEx();

                    if (funcCall.GetInputArgCount() != argExs.Length)
                    {
                        pParseErrors.Add("Invalid number of input arguments for defined function.");
                        return null;
                    }

                    funcCall.SetCallArgs(argExs);
                    return funcCall;

                case LexemeType.Bar:
                    endIndex = -1;
                    bool hitBar = false;
                    for (int i = currentIndex + 1; i < lexemeTable.Count; ++i)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> compareLexeme = lexemeTable[i];
                        if (compareLexeme.GetData1() == LexemeType.Bar)
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
                    if (algebraTermLexemeTable.Count == 1 && algebraTermLexemeTable[0].GetData1() == LexemeType.Operator &&
                        algebraTermLexemeTable[0].GetData2() == "*")
                        return null;
                    algebraTerm = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors, false);
                    if (algebraTerm == null)
                        return null;

                    AbsValFunction absVal = new AbsValFunction(algebraTerm);
                    return absVal;

                case LexemeType.StartPara:
                    endIndex = -1;
                    depth = 0;
                    for (int i = currentIndex + 1; i < lexemeTable.Count; ++i)
                    {
                        TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> compareLexeme = lexemeTable[i];
                        if (compareLexeme.GetData1() == LexemeType.StartPara)
                            depth++;
                        if (compareLexeme.GetData1() == LexemeType.EndPara)
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
                    algebraTerm = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors, false);
                    if (algebraTerm == null)
                        return null;
                    return algebraTerm;

                case LexemeType.Derivative:
                    ExComp parsedDeriv = ParseDerivative(ref currentIndex, lexemeTable, ref pParseErrors);
                    return parsedDeriv;
                case LexemeType.FuncDeriv:
                    ExComp parsedFuncDeriv = ParseFuncDerivative(ref currentIndex, lexemeTable, ref pParseErrors);
                    return parsedFuncDeriv;
                case LexemeType.Summation:
                    ExComp parsedSum = ParseSummation(ref currentIndex, lexemeTable, ref pParseErrors);
                    return parsedSum;
                case LexemeType.Limit:
                    ExComp parsedLim = ParseLimit(ref currentIndex, lexemeTable, ref pParseErrors);
                    return parsedLim;
                case LexemeType.Integral:
                    ExComp parsedInt = ParseIntegral(ref currentIndex, lexemeTable, ref pParseErrors);
                    return parsedInt;
                case LexemeType.Differential:
                    // If the lexeme before is a derivative just ignore this, it is probably an error.
                    if (currentIndex == 0 || lexemeTable[currentIndex - 1].GetData1() != LexemeType.Derivative ||
                        !lexemeTable[currentIndex - 1].GetData2().Contains(lexeme.GetData2()))
                    {
                        pParseErrors.Add("Cannot have lone differential.");
                        return null;
                    }

                    if (currentIndex + 1 < lexemeTable.Count)
                    {
                        currentIndex++;
                        ExComp lexToEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
                        return lexToEx;
                    }
                    return null;

                case LexemeType.I_Number:
                case LexemeType.Number:
                    ExNumber number = ExNumber.Parse(lexeme.GetData2());
                    return number;

                case LexemeType.Operator:
                    AgOp algebraOp = AgOp.ParseOperator(lexeme.GetData2());

                    // This is due to a function definition also being parsed as part of the combination or permutation.
                    if (lexeme.GetData2().Contains("text") && currentIndex < lexemeTable.Count - 1)
                        ArrayFunc.RemoveIndex(lexemeTable, currentIndex + 1);
                    return algebraOp;

                case LexemeType.Identifier:
                    AlgebraComp algebraComp = AlgebraComp.Parse(lexeme.GetData2());
                    return algebraComp;

                case LexemeType.Constant:
                    Constant constant = Constant.ParseConstant(lexeme.GetData2());
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

        private AlgebraTerm ParseMultiVarFunc(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lex = lt[currentIndex];

            if (!_funcStore.ContainsKey(lex.GetData2()))
                return null;

            List<List<TypePair<LexemeType,string>>> argLts = _funcStore[lex.GetData2()];

            ExComp[] argExs = new ExComp[argLts.Count];

            for (int i = 0; i < argLts.Count; ++i)
            {
                AlgebraTerm tmp = LexemeTableToAlgebraTerm(argLts[i], ref pParseErrors, false);
                if (tmp == null)
                    return null;

                argExs[i] = tmp.RemoveRedundancies(false);
            }

            FunctionDefinition funcDef;
            if (p_EvalData.GetFuncDefs().IsFuncDefined(lex.GetData2()))
            {
                FunctionDefinition tmp = p_EvalData.GetFuncDefs().GetFuncDef(lex.GetData2());
                if (tmp == null)
                    return null;

                funcDef = (FunctionDefinition)tmp.CloneEx();

                funcDef.SetCallArgs(argExs);

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

            funcDef = new FunctionDefinition(new AlgebraComp(lex.GetData2()), areCallArgs ? null : defInputArgs,
                areCallArgs ? argExs : null, true);
            return funcDef.ToAlgTerm();
        }

        private AlgebraTerm ParseVector(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            if (lt[currentIndex].GetData1() != LexemeType.VectorStore)
                return null;
            string vecStoreKey = lt[currentIndex].GetData2();
            if (!_vectorStore.ContainsKey(vecStoreKey))
                return null;
            List<TypePair<LexemeType,string>> subsetLt = _vectorStore[vecStoreKey];

            // Split by commas.
            List<List<TypePair<LexemeType,string>>> vecCompLts = new List<List<TypePair<LexemeType,string>>>();
            int prevIndex = 0;
            int depth = 0;
            for (int i = 0; i < subsetLt.Count; ++i)
            {
                if (subsetLt[i].GetData1() == LexemeType.StartBracket)
                    depth++;
                else if (subsetLt[i].GetData1() == LexemeType.EndBracket)
                    depth--;
                else if (subsetLt[i].GetData1() == LexemeType.Comma && depth == 0)
                {
                    List<TypePair<LexemeType,string>> compLt = subsetLt.GetRange(prevIndex, i - (prevIndex));
                    prevIndex = i + 1;
                    vecCompLts.Add(compLt);
                }
            }
            vecCompLts.Add(subsetLt.GetRange(prevIndex, subsetLt.Count - prevIndex));

            if (vecCompLts.Count == 0)
                return null;

            // Then parse each individual component.
            List<ExComp> vecCompExs = new List<ExComp>();
            foreach (List<TypePair<LexemeType,string>> vecCompLt in vecCompLts)
            {
                if (vecCompLt.Count == 0)
                {
                    pParseErrors.Add("Nothing following comma");
                    return null;
                }
                AlgebraTerm term = LexemeTableToAlgebraTerm(vecCompLt, ref pParseErrors, false);

                if (term == null)
                    return null;
                ExComp addEx = term.WeakMakeWorkable(ref pParseErrors, ref p_EvalData);
                if (addEx == null)
                    return null;
                if (addEx is AlgebraTerm)
                    addEx = (addEx as AlgebraTerm).RemoveRedundancies(true);

                vecCompExs.Add(addEx);
            }

            ExMatrix mat = MatrixHelper.CreateMatrix(vecCompExs);
            if (mat == null)
                return null;

            return mat;
        }

        private AlgebraTerm ParseDerivative(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            if (p_EvalData.GetPlainTextInput())
            {
                AlgebraTerm parsedDerivText = ParseDerivativePlainText(ref currentIndex, lt, ref pParseErrors);
                return parsedDerivText;
            }
            AlgebraTerm parsedTeXDeriv = ParseDerivativeTeX(ref currentIndex, lt, ref pParseErrors);
            return parsedTeXDeriv;
        }

        private AlgebraTerm ParseDerivativePlainText(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            // Should be in the form d/dx(...) or df/dx
            string derivStr = lt[currentIndex].GetData2();

            string[] topBottom = derivStr.Split('/');
            if (topBottom.Length != 2)
                return null;
            string top = StringFunc.Rm(topBottom[0], 0, 1);
            top = StringFunc.Rm(top, top.Length - 1, 1);
            string bottom = StringFunc.Rm(topBottom[1], 0, 1);
            bottom = StringFunc.Rm(bottom, bottom.Length - 1, 1);

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
                Match match = TypeHelper.Match(top, NUM_MATCH);
                if (!match.Success)
                    return null;
                string matchStr = match.Value;
                if (!int.TryParse(match.Value, out indexOfDeriv))
                    return null;

                if (indexOfDeriv < 1)
                    return null;

                if (!bottom.Contains("^"))
                    return null;
                match = TypeHelper.Match(bottom, NUM_MATCH);
                if (!match.Success)
                    return null;
                if (match.Value != matchStr)
                    return null;
            }

            top = StringFunc.Rm(top, 0, 1);
            bottom = StringFunc.Rm(bottom, 0, 1);

            if (indexOfDeriv != -1)
            {
                Match topSymbMatch = TypeHelper.Match(top, IDEN_MATCH);
                Match bottomSymbMatch = TypeHelper.Match(bottom, IDEN_MATCH);
                if (!bottomSymbMatch.Success)
                    return null;

                top = topSymbMatch.Value;
                bottom = bottomSymbMatch.Value;
            }
            else
                indexOfDeriv = 1;

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

                Derivative parsedDeriv = Derivative.Parse(top, bottom, new ExNumber(indexOfDeriv), isPartial, ref p_EvalData);
                return parsedDeriv;
            }

            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            // We have d/dx

            ExComp innerEx;
            // Just takes the derivative of the rest of the expression.
            List<TypePair<LexemeType,string>> remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
            if (remainingLt.Count == 0)
            {
                pParseErrors.Add("Nothing following derivative.");
                return null;
            }
            currentIndex = lt.Count - 1;
            innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors, false);

            if (innerEx == null)
            {
                return null;
            }

            return Derivative.Parse(bottom, innerEx, new ExNumber(indexOfDeriv), isPartial);
        }

        private AlgebraTerm ParseDerivativeTeX(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            // Should be in the form d/dx(...) or df/dx
            string derivStr = lt[currentIndex].GetData2();

            string[] topBottom = derivStr.Split(')');
            if (topBottom.Length != 3)
                return null;
            string top = StringFunc.Rm(topBottom[0], 0, "frac(".Length);
            string bottom = StringFunc.Rm(topBottom[1], 0, 1);

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
                Match match = TypeHelper.Match(top, NUM_MATCH);
                if (!match.Success)
                    return null;
                string matchStr = match.Value;
                if (!int.TryParse(match.Value, out indexOfDeriv))
                    return null;

                if (indexOfDeriv < 1)
                    return null;

                if (!bottom.Contains("^"))
                    return null;
                match = TypeHelper.Match(bottom, NUM_MATCH);
                if (!match.Success)
                    return null;
                if (match.Value != matchStr)
                    return null;
            }

            top = StringFunc.Rm(top, 0, 1);
            bottom = StringFunc.Rm(bottom, 0, 1);

            if (indexOfDeriv != -1)
            {
                Match topSymbMatch = TypeHelper.Match(top, IDEN_MATCH);
                Match bottomSymbMatch = TypeHelper.Match(bottom, IDEN_MATCH);
                if (!bottomSymbMatch.Success)
                    return null;

                top = topSymbMatch.Value;
                bottom = bottomSymbMatch.Value;
            }
            else
                indexOfDeriv = 1;

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

                Derivative derivParsed = Derivative.Parse(top, bottom, new ExNumber(indexOfDeriv), isPartial, ref p_EvalData);
                return derivParsed;
            }

            currentIndex++;
            if (currentIndex > lt.Count - 1)
                return null;

            // We have d/dx

            ExComp innerEx;
            // Just takes the derivative of the rest of the expression.
            List<TypePair<LexemeType,string>> remainingLt = lt.GetRange(currentIndex, lt.Count - currentIndex);
            if (remainingLt.Count == 0)
            {
                pParseErrors.Add("Nothing following derivative.");
                return null;
            }
            currentIndex = lt.Count - 1;
            innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors, false);

            if (innerEx == null)
            {
                return null;
            }

            return Derivative.Parse(bottom, innerEx, new ExNumber(indexOfDeriv), isPartial);
        }

        private AlgebraTerm ParseFraction(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            currentIndex++;

            // Should be in the form \frac(num)(den)

            ExComp numEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            if (numEx == null || numEx is AgOp)
                return null;

            currentIndex++;

            if (lt[currentIndex].GetData1() == LexemeType.Operator && lt[currentIndex].GetData2() == "*")
                currentIndex++;

            if (currentIndex > lt.Count - 1)
                return null;

            if (lt[currentIndex].GetData1() != LexemeType.StartPara)
                return null;

            ExComp denEx = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            if (denEx == null || denEx is AgOp)
                return null;

            return new AlgebraTerm(numEx, new DivOp(), denEx);
        }

        private AlgebraTerm ParseFuncDerivative(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> lex = lt[currentIndex];
            string str;
            string withRespectTo = null;
            if (lex.GetData2().Contains("("))
            {
                string[] tmps = lex.GetData2().Split('(');
                str = tmps[0];
                withRespectTo = StringFunc.Rm(tmps[1], tmps[1].Length - 1, 1);
            }
            else
                str = lex.GetData2();

            // Convert this over to the Leibniz notation.
            Match funcMatch = TypeHelper.Match(str, IDEN_MATCH);
            if (!funcMatch.Success)
                return null;

            str = StringFunc.Rm(str, 0, funcMatch.Length);
            ExComp order = null;
            if (str.Contains("'"))
                order = new ExNumber(str.Length); // Just the number of primes we have.
            else
            {
                str = StringFunc.Rm(str, 0, 1);
                // This is in the notation f^2(x)

                int iTmp;
                if (int.TryParse(str, out iTmp))
                    order = new ExNumber(iTmp);
                else if (Regex.IsMatch(str, IDEN_MATCH))
                    order = new AlgebraComp(str);
                else
                    return null;
            }

            if (withRespectTo == null && currentIndex + 3 < lt.Count)
            {
                if (lt[currentIndex + 1].GetData1() == LexemeType.StartPara ||
                    lt[currentIndex + 1].GetData1() == LexemeType.FuncArgStart)
                {
                    LexemeType lexType = lt[currentIndex + 1].GetData1();
                    LexemeType oppositeLt = LexemeTypeHelper.GetOpposite(lexType);

                    if (lt[currentIndex + 2].GetData1() != LexemeType.Identifier)
                    {
                        // Trying to evaluate the function at a given value.
                        int depth = 1;
                        currentIndex += 2;
                        int endIndex = -1;
                        for (int i = currentIndex; i < lt.Count; ++i)
                        {
                            if (lt[i].GetData1() == lexType)
                                depth++;
                            else if (lt[i].GetData1() == oppositeLt)
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    endIndex = i;
                                    break;
                                }
                            }
                        }

                        if (endIndex == -1)
                            return null;

                        List<TypePair<LexemeType,string>> inputValLt = lt.GetRange(currentIndex, endIndex - currentIndex);
                        AlgebraTerm inputTerm = LexemeTableToAlgebraTerm(inputValLt, ref pParseErrors, false);
                        if (inputTerm == null)
                            return null;

                        currentIndex = endIndex;

                        inputTerm = inputTerm.ApplyOrderOfOperations();
                        ExComp inputEx = inputTerm.MakeWorkable();

                        return Derivative.ConstructDeriv(new AlgebraComp(funcMatch.Value), inputEx, order);
                    }
                    withRespectTo = lt[currentIndex + 2].GetData2();
                    if (lt[currentIndex + 3].GetData1() != oppositeLt)
                        return null;

                    currentIndex = currentIndex + 4;

                    return Derivative.ConstructDeriv(new AlgebraComp(funcMatch.Value), new AlgebraComp(withRespectTo),
                        order);
                }
            }

            Derivative deriv = Derivative.ConstructDeriv(new AlgebraComp(funcMatch.Value), new AlgebraComp(withRespectTo),
                order);
            //Equation.Functions.Calculus.Derivative deriv = Equation.Functions.Calculus.Derivative.Parse(funcMatch.Value, withRespectTo, order, false, ref p_EvalData);

            if (deriv == null)
                pParseErrors.Add("Incorrect derivative notation for multivariable functions.");

            return deriv;
        }

        private AlgebraTerm ParseLimit(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            string limitStr = StringFunc.Rm(lt[currentIndex].GetData2(), 0, "lim_(".Length);

            int index = limitStr.IndexOf("to");
            if (index == -1)
                return null;

            AlgebraComp varFor = new AlgebraComp(limitStr.Substring(0, index));
            string valToStr = limitStr.Substring(index + 2, limitStr.Length - (index + 2));

            List<TypePair<LexemeType,string>> limToLt = CreateLexemeTable(valToStr, ref pParseErrors);
            if (limToLt == null || limToLt.Count == 0)
                return null;

            limToLt.Insert(0, new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartPara, "("));
            AlgebraTerm limTo = LexemeTableToAlgebraTerm(limToLt, ref pParseErrors, false);
            if (limTo == null)
                return null;

            ExComp tmpLimTo = limTo.WeakMakeWorkable(ref pParseErrors, ref p_EvalData);
            if (tmpLimTo == null)
                return null;
            if (tmpLimTo is AlgebraTerm)
            {
                limTo = tmpLimTo as AlgebraTerm;
                limTo = limTo.ApplyOrderOfOperations();
                limTo = limTo.MakeWorkable().ToAlgTerm();
            }
            else
                limTo = tmpLimTo.ToAlgTerm();

            currentIndex++;

            if (currentIndex < lt.Count - 1 && lt[currentIndex].GetData1() == LexemeType.Operator &&
                lt[currentIndex].GetData2() == "*")
                currentIndex++;

            ExComp innerEx;
            int endIndex = lt.Count;
            int depth = 0;
            int prevOp = -1;
            for (int i = currentIndex; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.StartPara)
                    depth++;
                else if (lt[i].GetData1() == LexemeType.EndPara)
                    depth--;
                else if (lt[i].GetData1() == LexemeType.Operator)
                    prevOp = i;
                else if (lt[i].GetData1() == LexemeType.Limit && depth == 0)
                {
                    // there will always be an operator before that needs to be parsed
                    endIndex = prevOp != -1 ? prevOp : i;
                    break;
                }
            }

            List<TypePair<LexemeType,string>> remainingLt = lt.GetRange(currentIndex, endIndex - currentIndex);
            if (remainingLt.Count == 0)
                return null;
            innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors, false);
            if (innerEx == null)
                return null;

            currentIndex = endIndex == lt.Count - 1 ? endIndex : endIndex - 1;

            return Limit.Create(innerEx, varFor, limTo.RemoveRedundancies(false));
        }

        private AlgebraTerm ParseLogBaseInner(ref int currentIndex, List<TypePair<LexemeType,string>> lexemeTable,
            ref List<string> pParseErrors)
        {
            currentIndex++;
            if (currentIndex > lexemeTable.Count - 1)
                return null;

            // This should be in the form log_(base)(inner)
            if (lexemeTable[currentIndex].GetData1() == LexemeType.FuncIden)
                lexemeTable[currentIndex].SetData1(LexemeType.Identifier);

            ExComp baseEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (baseEx == null)
                return null;

            currentIndex++;
            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].GetData1() == LexemeType.FuncArgStart)
            {
                int depth = 0;
                int endIndex = -1;
                for (int i = currentIndex; i < lexemeTable.Count; ++i)
                {
                    if (lexemeTable[i].GetData1() == LexemeType.FuncArgStart)
                        depth++;
                    else if (lexemeTable[i].GetData1() == LexemeType.FuncArgEnd)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            endIndex = i;
                            break;
                        }
                    }
                }

                if (endIndex == -1)
                    return null;

                currentIndex++;
                List<TypePair<LexemeType,string>> innerLt = lexemeTable.GetRange(currentIndex, endIndex - currentIndex);
                if (innerLt.Count == 0)
                    return null;
                AlgebraTerm innerTerm = LexemeTableToAlgebraTerm(innerLt, ref pParseErrors, false);
                if (innerTerm == null)
                    return null;

                currentIndex = endIndex;

                LogFunction retLog = new LogFunction(innerTerm);
                retLog.SetBase(baseEx);
                return retLog;
            }

            // Skip past the multiplication operator which has been placed here on default.
            currentIndex++;

            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].GetData1() != LexemeType.StartPara)
                return null;

            ExComp innerEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (innerEx == null)
                return null;

            LogFunction log = new LogFunction(innerEx);
            log.SetBase(baseEx);

            return log;
        }

        private AlgebraTerm ParseParaGroup(ref int currentIndex, List<TypePair<LexemeType,string>> lexemeTable, ref List<string> pParseErrors)
        {
            int endIndex = -1;
            int depth = 0;
            int startIndex;

            endIndex = -1;
            depth = 0;
            for (int i = currentIndex + 1; ; ++i)
            {
                TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string> compareLexeme = lexemeTable[i];
                if (compareLexeme.GetData1() == LexemeType.StartPara)
                    depth++;
                if (compareLexeme.GetData1() == LexemeType.EndPara)
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
            List<TypePair<LexemeType,string>> algebraTermLexemeTable = lexemeTable.GetRange(startIndex, endIndex - startIndex);
            currentIndex = endIndex;
            if (algebraTermLexemeTable.Count == 0)
                return null;
            AlgebraTerm lexToAlg = LexemeTableToAlgebraTerm(algebraTermLexemeTable, ref pParseErrors, false);
            return lexToAlg;
        }

        private AlgebraTerm ParseRootInner(ref int currentIndex, List<TypePair<LexemeType,string>> lexemeTable, ref List<string> pParseErrors)
        {
            currentIndex++;

            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].GetData1() != LexemeType.StartPara)
                return null;

            currentIndex++;
            // For the first starting para.
            int depth = 1;
            int endIndex = -1;
            for (int i = currentIndex; i < lexemeTable.Count; ++i)
            {
                if (lexemeTable[i].GetData1() == LexemeType.StartBracket)
                    depth++;
                else if (lexemeTable[i].GetData1() == LexemeType.EndBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            if (endIndex == -1)
                return null;

            List<TypePair<LexemeType,string>> rootIndexLt = lexemeTable.GetRange(currentIndex, endIndex - currentIndex);
            if (rootIndexLt.Count == 0)
                return null;
            AlgebraTerm rootTerm = LexemeTableToAlgebraTerm(rootIndexLt, ref pParseErrors, false);
            if (rootTerm == null)
                return null;

            ExComp rootEx = rootTerm.RemoveRedundancies(true);
            if (rootEx is AgOp)
                return null;

            currentIndex = endIndex + 2;

            if (currentIndex > lexemeTable.Count - 1)
                return null;

            if (lexemeTable[currentIndex].GetData1() != LexemeType.StartPara)
                return null;

            ExComp innerEx = LexemeToExComp(lexemeTable, ref currentIndex, ref pParseErrors);
            if (innerEx == null || innerEx is AgOp)
                return null;

            AlgebraTerm retVal = new AlgebraTerm(innerEx, new PowOp(), new AlgebraTerm(ExNumber.GetOne(), new DivOp(), rootEx));

            return retVal;
        }

        private AlgebraTerm ParseSummation(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            string varStr = StringFunc.Rm(lt[currentIndex].GetData2(), 0, "sum_(".Length);
            // Remove the equals sign.
            varStr = StringFunc.Rm(varStr, varStr.Length - 1, 1);

            AlgebraComp iterVar = new AlgebraComp(varStr);

            int endParaIndex = -1;
            int depth = 1;
            for (int i = currentIndex; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.StartPara)
                {
                    depth++;
                }
                if (lt[i].GetData1() == LexemeType.EndPara)
                {
                    depth--;
                    if (depth == 0)
                    {
                        endParaIndex = i;
                        break;
                    }
                }
            }

            if (endParaIndex == -1)
                return null;

            List<TypePair<LexemeType,string>> startValLt = lt.GetRange(currentIndex + 1, endParaIndex - (currentIndex + 1));
            AlgebraTerm tmp = LexemeTableToAlgebraTerm(startValLt, ref pParseErrors, false);
            if (tmp == null)
                return null;
            ExComp startVal = tmp;

            currentIndex = endParaIndex + 1;

            if (currentIndex + 1 >= lt.Count)
                return null;

            if (lt[currentIndex].GetData1() != LexemeType.Operator || lt[currentIndex].GetData2() != "^")
                return null;

            currentIndex++;

            ExComp endVal = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
            if (endVal == null)
                return null;

            currentIndex++;
            if (currentIndex < lt.Count && lt[currentIndex].GetData1() == LexemeType.Operator &&
                lt[currentIndex].GetData2() == "*")
            {
                currentIndex++;
            }

            // Parse the rest of the expression.

            int endIndex = lt.Count;
            depth = 0;
            int prevOp = -1;

            if (endIndex == currentIndex)
            {
                pParseErrors.Add("Nothing following summation");
                return null;
            }

            for (int i = currentIndex; i < lt.Count; ++i)
            {
                if (lt[i].GetData1() == LexemeType.StartPara)
                    depth++;
                else if (lt[i].GetData1() == LexemeType.EndPara)
                    depth--;
                else if (lt[i].GetData1() == LexemeType.Operator)
                    prevOp = i;
                else if (lt[i].GetData1() == LexemeType.Summation && depth == 0)
                {
                    // there will always be an operator before that needs to be parsed
                    endIndex = prevOp != -1 ? prevOp : i;
                    break;
                }
            }

            List<TypePair<LexemeType,string>> remainingLt = lt.GetRange(currentIndex, endIndex - currentIndex);
            if (remainingLt.Count == 0)
                return null;
            AlgebraTerm
                innerEx = LexemeTableToAlgebraTerm(remainingLt, ref pParseErrors, false);
            if (innerEx == null)
                return null;

            currentIndex = endIndex == lt.Count - 1 ? endIndex : endIndex - 1;

            if (startVal is AlgebraTerm)
            {
                startVal = (startVal as AlgebraTerm).ApplyOrderOfOperations();
                startVal = (startVal as AlgebraTerm).MakeWorkable();
            }
            if (endVal is AlgebraTerm)
            {
                endVal = (endVal as AlgebraTerm).ApplyOrderOfOperations();
                endVal = (endVal as AlgebraTerm).MakeWorkable();
            }

            SumFunction sumFunc = new SumFunction(innerEx, iterVar, startVal, endVal);

            return sumFunc;
        }

        private ExComp ParseIntegral(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            ExComp lower = null, upper = null;
            if (lt[currentIndex].GetData2().Contains("_"))
            {
                currentIndex++;
                // Get the next lexeme which will be the lower bound.
                lower = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
                if (lower == null || lower is AgOp)
                    return null;
                AlgebraTerm lowerTerm = lower.ToAlgTerm();

                lower = lowerTerm.WeakMakeWorkable(ref p_EvalData).ToAlgTerm();
                if (lower == null)
                    return null;
                lower = SimplifyGenTermType.BasicSimplify(lower, ref p_EvalData, true);

                if (currentIndex + 1 < lt.Count &&
                    (lt[currentIndex + 1].GetData1() != LexemeType.Operator || lt[currentIndex + 1].GetData2() != "^"))
                {
                    // Don't necessarily return null as the user could be declaring a surface or line integral.
                    if (!(lower is AlgebraComp))
                        return null;
                }
                else
                {
                    currentIndex += 2;

                    upper = LexemeToExComp(lt, ref currentIndex, ref pParseErrors);
                    if (upper == null || upper is AgOp)
                        return null;

                    AlgebraTerm upperTerm = upper.ToAlgTerm();

                    upper = upperTerm.WeakMakeWorkable(ref p_EvalData).ToAlgTerm();
                    if (upper == null)
                        return null;
                    upper = SimplifyGenTermType.BasicSimplify(upper, ref p_EvalData, true);
                }
            }

            if (currentIndex + 1 < lt.Count && lt[currentIndex + 1].GetData1() == LexemeType.Operator &&
                lt[currentIndex + 1].GetData2() == "*")
                currentIndex++;

            currentIndex++;

            int startIndex = currentIndex;
            int endIndex = -1;

            // Next should be the expression.
            // Parse until the differential.
            int depth = 1;
            for (; currentIndex < lt.Count; ++currentIndex)
            {
                if (lt[currentIndex].GetData1() == LexemeType.Integral)
                {
                    depth++;
                }
                if (lt[currentIndex].GetData1() == LexemeType.Differential)
                {
                    depth--;
                    if (depth == 0)
                    {
                        endIndex = currentIndex;
                        break;
                    }
                }
            }

            currentIndex = endIndex == -1 ? lt.Count : endIndex;

            List<TypePair<LexemeType,string>> integralTerm = lt.GetRange(startIndex, currentIndex - startIndex);

            // This could be in the line integral vector path notation.
            if (integralTerm.Count > 0 && integralTerm[integralTerm.Count - 1].GetData2() == "*")
            {
                ArrayFunc.RemoveIndex(integralTerm, integralTerm.Count - 1);
            }

            AlgebraTerm innerTerm = LexemeTableToAlgebraTerm(integralTerm, ref pParseErrors, false);
            if (innerTerm == null)
                return null;
            ExComp innerEx = innerTerm.RemoveRedundancies(true);

            if (endIndex == -1)
            {
                if (lower == null && upper == null && innerEx is LineIntegral)
                {
                    LineIntegral lineIntegral = innerEx as LineIntegral;

                    return SurfaceIntegral.ConstructSurfaceIntegral(lineIntegral.GetInnerEx(), lineIntegral.GetLineIden(),
                        lineIntegral.GetDVar());
                }
                pParseErrors.Add("Couldn't find the variable of integration.");
                return null;
            }

            // Remove the first 'd' character from the differential.
            string withRespectVar = StringFunc.Rm(lt[endIndex].GetData2(), 0, 1);
            AlgebraComp dVar = new AlgebraComp(withRespectVar);

            if (upper == null && lower != null)
                return LineIntegral.ConstructLineIntegral(innerTerm, (AlgebraComp)lower, dVar);

            return Integral.ConstructIntegral(innerEx, dVar, lower, upper, false, true);
        }

        private ExComp ParseVectorNotation(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            // Get the index of the lexeme table to be parsed.
            string[] tmps = lt[currentIndex].GetData2().Split('|');
            List<TypePair<LexemeType,string>> parseLt = _vectorStore[tmps[1]];

            // Get the dimensionality of this vector
            string endCharacter = StringFunc.Rm(tmps[0], 0, "vector".Length);
            int dimen = 0;
            if (endCharacter == "a")
                dimen = 2;
            else if (endCharacter == "b")
                dimen = 3;
            else if (endCharacter == "c")
                dimen = 4;
            else
                return null;

            int depth = 0;
            for (int i = 0; i < parseLt.Count; ++i)
            {
                if (parseLt[i].GetData1() == LexemeType.StartPara)
                {
                    if ((depth) == 0)
                    {
                        parseLt[i] = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.StartBracket, "[");
                    }
                    depth++;
                }
                else if (parseLt[i].GetData1() == LexemeType.EndPara)
                {
                    if ((--depth) == 0)
                    {
                        parseLt[i] = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.EndBracket, "]");
                        if (i + 1 < parseLt.Count && parseLt[i + 1].GetData1() == LexemeType.Operator && dimen > 1)
                            parseLt[i + 1] = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.Comma, ",");
                        if ((--dimen) == 0)
                        {
                            break;
                        }
                    }
                }
            }

            string storeKey = "tempStore" + (_vecStoreIndex++);
            _vectorStore[storeKey] = parseLt;

            lt[currentIndex] = new TypePair<MathSolverWebsite.MathSolverLibrary.Parsing.LexemeType, string>(LexemeType.VectorStore, storeKey);

            ExComp parsedVector = ParseVector(ref currentIndex, lt, ref pParseErrors);
            return parsedVector;
        }

        private ExComp ParseBinomNotation(ref int currentIndex, List<TypePair<LexemeType,string>> lt, ref List<string> pParseErrors)
        {
            // Get the index in the dictionary.
            string[] tmps = lt[currentIndex].GetData2().Split('|');
            List<TypePair<LexemeType,string>> parseLt = _vectorStore[tmps[1]];

            // Parse the next two groups.
            int parseCurrentIndex = 0;
            if (parseLt[parseCurrentIndex].GetData1() != LexemeType.StartPara)
                return null;
            ExComp top = LexemeToExComp(parseLt, ref parseCurrentIndex, ref pParseErrors);
            if (top == null || top is AgOp)
                return null;
            parseCurrentIndex++;

            // Skip over the multiplication operator that was automatically inserted into the expression.
            if (parseLt[parseCurrentIndex].GetData1() == LexemeType.Operator)
                parseCurrentIndex++;

            if (parseLt[parseCurrentIndex].GetData1() != LexemeType.StartPara)
                return null;
            ExComp bottom = LexemeToExComp(parseLt, ref parseCurrentIndex, ref pParseErrors);
            if (bottom == null || bottom is AgOp)
                return null;

            return new ChooseFunction(top, bottom);
        }

        public class MatchTolken
        {
            public readonly int Index;
            public readonly int Length;
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
    }
}