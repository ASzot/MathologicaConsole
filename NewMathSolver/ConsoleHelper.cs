using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MathSolverWebsite.MathSolverLibrary;

namespace MathSolverWebsite
{
	class ConsoleHelper
	{
		private static Version _version = new Version(1, 5, 5, 3);

		public static void DisplayHelpScreen()
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

		public static void DisplayUserFriendlySols(List<MathSolverLibrary.Equation.Solution> solutions)
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

		public static void DisplayUserFreindlyRests(List<MathSolverLibrary.Equation.Restriction> rests)
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

		public static void SetConsoleWindow()
		{
			Console.Title = "Mathologica";
		}

		public static string SolutionsToStr(List<MathSolverLibrary.Equation.Solution> solutions)
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

		public static string RestrictionsToStr(List<MathSolverLibrary.Equation.Restriction> restrictions)
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

		public static void UserFriendlyDisplay(MathSolverLibrary.Equation.SolveResult result, MathSolverLibrary.TermType.EvalData evalData)
		{
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

		public static void WriteColor(ConsoleColor color, string txt)
		{
			Console.ForegroundColor = color;
			Console.Write(txt);
			Console.ResetColor();
		}

		public static void WriteLineColor(ConsoleColor color, string txt)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(txt);
			Console.ResetColor();
		}
	}
}
