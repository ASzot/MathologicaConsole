using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathSolverWebsite.MathSolverLibrary;
using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Information_Helpers;
using MathSolverWebsite.MathSolverLibrary.TermType;
using MathSolverWebsite.MathSolverLibrary.Parsing;
using MathSolverWebsite.MathSolverLibrary.Equation.Operators;

namespace MathSolverWebsite
{
	class MathTest
	{
		private static bool _useRad = true;

		private static EvalData ConstructEvalData()
		{
			// The temporary data used to store user defined functions and constants.
			FuncDefHelper funcDefHelper = new FuncDefHelper();

			// The evaluation options for the expression.
			// Also constains all of the data for the work steps.
			return new EvalData(_useRad, new WorkMgr(), funcDefHelper);
		}

		/// <summary>
		/// The most general way to parse a data structure using the math evaluation engine.
		/// Can return an equation, a single expression, or a system of equations.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="evalData"></param>
		/// <returns></returns>
		private static List<EqSet> GenCreate(string str, EvalData evalData)
		{
			LexicalParser lexParser = new LexicalParser(evalData);

			List<List<TypePair<LexemeType, string>>> lexemeTables;
			List<string> pParseErrors = new List<string>();

			// This will contain an array of the expressions entered by the user.
			// This generic parse function works for equations and system of equations 
			// which is why a list of equation sides is returned.
			List<EqSet> sides = lexParser.ParseInput(str, out lexemeTables, ref pParseErrors);

			return sides;
		}

		/// <summary>
		/// Will construct an symbolic expression from a mathematical 
		/// expression represented in a string. For the purposes of convenience
		/// and demonstration this function will only return expressions and not entire 
		/// equations. Please refer to GenCreate for parsing entire equations and 
		/// system of equations.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="evalData"></param>
		/// <returns></returns>
		private static ExComp ConstructEx(string str, EvalData evalData)
		{
			List<EqSet> sides = GenCreate(str, evalData);

			ExComp ex = sides[0].GetLeft();

			return ex;
		}

		public static void Test()
		{
			// Initialize the math solving engine.
			MathSolver.Init();

			EvalData evalData = ConstructEvalData();

			AlgebraComp x = new AlgebraComp("x");

			ExComp complexExpression = ConstructEx("3x^2 - 3", evalData);

			ExComp combined = AddOp.StaticCombine(x, complexExpression);

			// Square the expression.
			ExComp squared = PowOp.RaiseToPower(complexExpression, new ExNumber(2.0), ref evalData, false);

			// String containing the evaluated result.
			string result = squared.ToAlgTerm().FinalToDispStr();


		}
	}
}
