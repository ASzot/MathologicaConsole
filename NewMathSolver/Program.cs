using MathSolverWebsite.MathSolverLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;

///////////////////////////////////////////////////////////////////////////////////////////////////////////
// Example of dynamic evaluation. 
// In the command prompt the user types in math expression. 
//The expression can then be evaluated a number of ways based on the context of the expression.
/////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace MathSolverWebsite
{
    internal class Program
    {
        private static bool _useRad = true;

		private static bool ProcessSpecialCommand(string input)
		{
			if (input == "help")
			{
				ConsoleHelper.DisplayHelpScreen();
			}
			else if (input == "clear")
			{
				Console.Clear();
			}
			else if (input == "UseRad")
			{
				_useRad = true;
				Console.WriteLine("Set to radians");
			}
			else if (input == "UseDeg")
			{
				_useRad = false;
				Console.WriteLine("Set to use degrees");
			}
			else
				return false;

			return true;
		}

		private static MathSolverLibrary.TermType.GenTermType ParseInput(string input, ref MathSolverLibrary.Information_Helpers.FuncDefHelper funcDefHelper, ref MathSolverLibrary.TermType.EvalData evalData)
		{
			Stopwatch stopwatch = new Stopwatch();

			// Start timing how long the parsing process takes.
			stopwatch.Start();

			// Will contain the list of parsing errors if any.
			List<string> parseErrors = new List<string>();

			// Parse the input using the math parsing engine.
			var termEval = MathSolver.ParseInput(input, ref evalData, ref parseErrors);

			// Stop the timing.
			stopwatch.Stop();

			if (termEval == null)
			{
				// The user's input was invalid.
				ConsoleHelper.WriteLineColor(ConsoleColor.Red, "Cannot interpret.");
				return null;
			}

			ConsoleHelper.WriteLineColor(ConsoleColor.DarkCyan, "Parsing took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

			return termEval;
		}

		private static MathSolverLibrary.Equation.SolveResult EvaluateTerm(MathSolverLibrary.TermType.GenTermType termEval, int optionIndex, ref MathSolverLibrary.TermType.EvalData evalData)
		{
			Stopwatch stopwatch = new Stopwatch();

			stopwatch.Start();
			MathSolverLibrary.Equation.SolveResult result = termEval.ExecuteCommandIndex(optionIndex - 1, ref evalData);
			stopwatch.Stop();

			ConsoleHelper.WriteLineColor(ConsoleColor.DarkCyan, "Evaluating took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

			return result;
		}

        private static void Main(string[] args)
        {
			// The caching mechanism for saving any user defined constants or functions.
            MathSolverLibrary.Information_Helpers.FuncDefHelper funcDefHelper = new MathSolverLibrary.Information_Helpers.FuncDefHelper();

			// Display some messages to the user.
            ConsoleHelper.SetConsoleWindow();
            ConsoleHelper.DisplayHelpScreen();

			// Initialize the math solving engine.
            MathSolver.Init();

            for (; ; )
            {
				// Poll user input.
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(">");
                Console.ResetColor();
                string inputStr = Console.ReadLine();

				// Check if the user wants to quit the program.
				if (inputStr == "quit")
					break;

				if (ProcessSpecialCommand(inputStr))
					continue;
				else if (inputStr == "test")
					inputStr = "\\int (x+1)/(\\sqrt(x)) dx";

				// The temporary data necessary for the math evaluation engine. 
				// Necessary in the parsing stage to determine the context and meaning of the expression. 
				MathSolverLibrary.TermType.EvalData evalData = new MathSolverLibrary.TermType.EvalData(_useRad, new WorkMgr(), funcDefHelper);


				var termEval = ParseInput(inputStr, ref funcDefHelper, ref evalData);
				if (termEval == null)
					continue;

				// Display the possible methods of evaluation to the user.
				Console.WriteLine("Input desired evaluation option:");
				for (int i = 0; i < termEval.GetCmdCount(); ++i)
				{
					Console.WriteLine(" " + (i + 1).ToString() + ")" + termEval.GetCommands()[i]);
				}

				// Get the command the user wants to evaluate.
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.Write(">");
				Console.ResetColor();
				string optionStr = Console.ReadLine();
				int optionIndex;
				if (!int.TryParse(optionStr, out optionIndex))
					return;

				MathSolverLibrary.Equation.SolveResult solveResult = EvaluateTerm(termEval, optionIndex, ref evalData);
				ConsoleHelper.UserFriendlyDisplay(solveResult, evalData);
            }
        }

    }
}