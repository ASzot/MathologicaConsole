using MathSolverWebsite.MathSolverLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using InOut = System.Tuple<string, string>;

namespace MathSolverWebsite
{
    internal class Program
    {
        private static bool _useRad = true;

        private static Version _version = new Version(1, 5, 5, 3);

        private static void DisplayHelpScreen()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Mathologica:");
            Console.ResetColor();
            Console.WriteLine("Version: " + _version.ToString());
            Console.WriteLine("Enter 'help' to see this text again");
            Console.WriteLine("Enter 'quit' to quit");
            Console.WriteLine("Enter 'clear' to clear the screen");
            Console.WriteLine("Enter math input to evaluate");
            Console.WriteLine();
            Console.WriteLine("Output will be in standard TeX markup language.");
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void DisplayUserFriendlySols(List<MathSolverLibrary.Equation.Solution> solutions)
        {
            if (solutions.Count > 0)
            {
                Console.WriteLine();
                WriteLineColor(ConsoleColor.DarkYellow, "Solutions:");
            }

            foreach (var sol in solutions)
            {
                Console.WriteLine();
                string starterStr = "";
                if (sol.SolveFor != null)
                    starterStr += sol.SolveForToTexStr() + " " + sol.ComparisonOpToTexStr() + " ";
                string outputStr;
                if (sol.Result != null)
                {
                    Console.WriteLine("Result:");
                    outputStr = starterStr + sol.ResultToTexStr();
                    outputStr = MathSolver.FinalizeOutput(outputStr);
                    Console.WriteLine(" " + outputStr);
                    if (sol.Multiplicity != 1)
                        Console.WriteLine(" " + "Multiplicity of " + sol.Multiplicity.ToString() + ".");
                }
                if (sol.GeneralResult != null)
                {
                    Console.WriteLine("General Result:");
                    outputStr = starterStr + sol.GeneralToTexStr();
                    outputStr = MathSolver.FinalizeOutput(outputStr);
                    Console.WriteLine(" " + outputStr);
                    Console.WriteLine("     Where " + sol.GeneralResult.IterVarToTexString() + " is a real integer.");
                }
                if (sol.ApproximateResult != null)
                {
                    Console.WriteLine("Approximate Result:");
                    outputStr = starterStr + sol.ApproximateToTexStr();
                    outputStr = MathSolver.FinalizeOutput(outputStr);
                    Console.WriteLine(" " + outputStr);
                }
            }

            Console.WriteLine();
        }

        private static void DisplayUserFreindlyRests(List<MathSolverLibrary.Equation.Restriction> rests)
        {
            if (rests.Count > 0)
            {
                Console.WriteLine();
                WriteLineColor(ConsoleColor.DarkYellow, "Restrictions:");
            }

            foreach (var rest in rests)
            {
                Console.WriteLine();
                Console.WriteLine(" " + rest.ToMathAsciiStr());
            }

            Console.WriteLine();
        }

        private static void Main(string[] args)
        {
            MathSolverLibrary.Information_Helpers.FuncDefHelper funcDefHelper = new MathSolverLibrary.Information_Helpers.FuncDefHelper();

            SetConsoleWindow();

            DisplayHelpScreen();

            MathSolver.Init();

            for (; ; )
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(">");
                Console.ResetColor();
                string inputStr = Console.ReadLine();
                if (inputStr == "quit")
                    break;
                if (inputStr == "help")
                {
                    DisplayHelpScreen();
                    continue;
                }
                else if (inputStr == "clear")
                {
                    Console.Clear();
                    continue;
                }
                else if (inputStr == "UseRad")
                {
                    _useRad = true;
                    Console.WriteLine("Set to radians");
                }
                else if (inputStr == "UseDeg")
                {
                    _useRad = false;
                    Console.WriteLine("Set to use degrees");
                }

                UserFriendlyDisplay(inputStr, funcDefHelper);
            }
        }

        private static void SetConsoleWindow()
        {
            Console.Title = "Mathologica";
        }

        private static string SolutionsToStr(List<MathSolverLibrary.Equation.Solution> solutions)
        {
            string finalStr = "";
            for (int i = 0; i < solutions.Count; ++i)
            {
                var solution = solutions[i];
                if (solution.SolveFor != null)
                {
                    finalStr += solution.SolveFor.ToTexString();
                    finalStr += " " + solution.ComparisonOpToTexStr() + " ";
                }

                string resultStr = "";
                if (solution.Result != null)
                {
                    resultStr += solution.ResultToTexStr();
                }
                if (solution.GeneralResult != null)
                {
                    if (solution.Result != null)
                        resultStr += "; ";
                    resultStr += solution.GeneralToTexStr();
                }
                if (solution.ApproximateResult != null)
                {
                    resultStr += "; ";
                    resultStr += solution.ApproximateToTexStr();
                }

                if (solution.Multiplicity != 1)
                    resultStr += ", M: " + solution.Multiplicity.ToString();

                finalStr += resultStr;

                if (i != solutions.Count - 1)
                    finalStr += "; ";
            }

            return finalStr;
        }

        private static string RestrictionsToStr(List<MathSolverLibrary.Equation.Restriction> restrictions)
        {
            string finalStr = "";
            for (int i = 0; i < restrictions.Count; ++i)
            {
                var restriction = restrictions[i];
                finalStr += restriction.ToMathAsciiStr();

                if (i != restrictions.Count - 1)
                    finalStr += "; ";
            }

            return finalStr;
        }

        private static void UserFriendlyDisplay(string inputStr, MathSolverLibrary.Information_Helpers.FuncDefHelper funcDefHelper)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            MathSolverLibrary.TermType.EvalData evalData = new MathSolverLibrary.TermType.EvalData(_useRad, new WorkMgr(), funcDefHelper);
            List<string> parseErrors = new List<string>();
            var termEval = MathSolver.ParseInput(inputStr, ref evalData, ref parseErrors);
            stopwatch.Stop();
            if (termEval == null)
            {
                WriteLineColor(ConsoleColor.Red, "Cannot interpret.");
                return;
            }

            WriteLineColor(ConsoleColor.DarkCyan, "Parsing took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

            Console.WriteLine("Input desired evaluation option:");
            for (int i = 0; i < termEval.GetCmdCount(); ++i)
            {
                Console.WriteLine(" " + (i + 1).ToString() + ")" + termEval.GetCommands()[i]);
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(">");
            Console.ResetColor();
            string optionStr = Console.ReadLine();
            int optionIndex;
            if (!int.TryParse(optionStr, out optionIndex))
                return;

            stopwatch.Restart();
            MathSolverLibrary.Equation.SolveResult result = termEval.ExecuteCommandIndex(optionIndex - 1, ref evalData);
            stopwatch.Stop();

            WriteLineColor(ConsoleColor.DarkCyan, "Evaluating took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

            if (evalData.GetMsgs() != null)
            {
                foreach (string msg in evalData.GetMsgs())
                {
                    WriteLineColor(ConsoleColor.DarkYellow, " " + msg);
                }
            }

            if (evalData.GetInputTypeStr() != null)
            {
                WriteLineColor(ConsoleColor.DarkGreen, "Topic is " + evalData.GetInputTypeStr());
            }

            if (evalData.GetGraphEqStrs() != null)
            {
                string finalGraphStr = "";
                for (int i = 0; i < evalData.GetGraphEqStrs().Length; ++i)
                {
                    finalGraphStr += evalData.GetGraphEqStrs()[i];
                    if (i != evalData.GetGraphEqStrs().Length - 1)
                        finalGraphStr += "; ";
                }

                WriteLineColor(ConsoleColor.White, "Graph " + finalGraphStr);
            }

            if (!result.Success)
            {
                WriteLineColor(ConsoleColor.DarkRed, "Failure");
                foreach (string msg in evalData.GetFailureMsgs())
                {
                    WriteLineColor(ConsoleColor.Red, "  " + msg);
                }
            }
            else
            {
                if (result.Solutions == null)
                    return;

                int solCount = result.Solutions.Count;
                if (evalData.GetHasPartialSolutions())
                {
                    Console.WriteLine("The input was partially evaluated to...");
                    for (int i = 0; i < evalData.GetPartialSolutions().Count; ++i)
                    {
                        string partialSolStr = evalData.PartialSolToTexStr(i);
                        partialSolStr = MathSolver.FinalizeOutput(partialSolStr);
                        Console.WriteLine(" " + partialSolStr);
                    }
                    Console.WriteLine();
                    if (solCount > 0)
                    {
                        string pluralStr = solCount > 1 ? "s were" : " was";
                        Console.WriteLine("The following " + solCount.ToString() + " solution" + pluralStr +
                            " also obtained...");
                        DisplayUserFriendlySols(result.Solutions);
                    }
                }
                else
                {
                    Console.WriteLine("The input was successfully evaluated.");
                    if (solCount > 0)
                        DisplayUserFriendlySols(result.Solutions);
                    if (result.GetHasRestrictions())
                        DisplayUserFreindlyRests(result.Restrictions);
                }
            }
        }

        private static void WriteColor(ConsoleColor color, string txt)
        {
            Console.ForegroundColor = color;
            Console.Write(txt);
            Console.ResetColor();
        }

        private static void WriteLineColor(ConsoleColor color, string txt)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(txt);
            Console.ResetColor();
        }
    }
}