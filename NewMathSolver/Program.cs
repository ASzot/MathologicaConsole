using MathSolverWebsite.MathSolverLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using InOut = System.Tuple<string, string>;

namespace MathSolverWebsite
{
    internal class Program
    {
        private static InOut[] _inOuts =
            {
                new InOut("x-4=10", "x = 14"),                                                          // 1
                new InOut("2x-4=10", "x = 7"),                                                          // 2
                new InOut("5x-6=3x-8", "x = -1"),                                                       // 3
                new InOut("(3/4)*x+(5/6)=5x-(125/3)", "x = 10"),                                        // 4
                new InOut("((6x-7)/4)+((3x-5)/7)=((5x+78)/28)", "x = 3"),                               // 5
                new InOut("2(3x-7)+4(3x+2)=6(5x+9)+3", @"x = \frac{-21}{4}"),                           // 6
                new InOut("(2^(0.5))x+(3^(0.5))=5^(0.5)", @"x = \frac{\sqrt{5}-\sqrt{3}}{\sqrt{2}}"),  // 7
                new InOut("4(x-2)^3=w", @"x = \sqrt[3]{\frac{w}{4}}+2"),                                // 8
                new InOut("(x-10)^(1/2)-4=0", "x = 26"),                                                // 9
                new InOut("4*|x-3|+12.5=20.5", "x = 5; x = 1"),                                             // 10
                new InOut("(3x-2)*(4x+5)", "12x^{2}+7x-10"),                                            // 11
                new InOut("-2*|4x-8|-12=-8", "x = No solution"),                                        // 12
                new InOut("x^2-6x=-9", "x = 3, M: 2"),                                                        // 13
                new InOut("12x^2=10-7x", @"x = \frac{-5}{4}; x = \frac{2}{3}"),                             // 14
                new InOut("x^(1/2)+(x-5)^(1/2)=1", "x = 9"),                                            // 15 (9 is an extraneous solution).
                new InOut("(3x-4)(5x+2)(x^2-4)=0", @"x = \frac{4}{3}; x = \frac{-2}{5}; x = 2; x = -2"),                // 16
                new InOut("(5x-9)^4", "625x^{4}-4500x^{3}+12150x^{2}-14580x+6561"),                     // 17
                new InOut("4x^2-2x-5=0", @"x = \frac{\sqrt{21}+1}{4}; x = \frac{-\sqrt{21}+1}{4}"), // 18
                new InOut("(3)^(1/2)x^2+(5)^(1/2)x=12", @"x = \frac{\sqrt{(48\sqrt{3}+5)}-\sqrt{5}}{2\sqrt{3}}; x = \frac{-\sqrt{(48\sqrt{3}+5)}-\sqrt{5}}{2\sqrt{3}}"),    // 19
                new InOut("|2x-1|=|4x+3|", @"x = -2; x = \frac{-1}{3}"),                                    // 20
                new InOut("(x+1)^(1/2)-3x=1", @"x = 0; x = \frac{-5}{9}"),                 // 21 note -5/9 is actually extraneous.
                new InOut("(x^2-8)/(x^2-4)+2/(x+2)=5/(x-2)", @"x = \frac{\sqrt{97}+3}{2}; x = \frac{-\sqrt{97}+3}{2}"),  // 22
                new InOut("((4x+3)(3x-2))/(3x-2)", "4x+3"),                                             // 23
                new InOut("sin(pi/4)", @"\frac{\sqrt{2}}{2}; 0.707106781186548"),                                          // 24
                new InOut("pi*e", "pie; 8.53973422267357"),                                                             // 25
                new InOut("cos(0)", "1"),                                                               // 26
                new InOut("cos((11pi)/6)", @"\frac{\sqrt{3}}{2}; 0.866025403784439"),                                      // 27
                new InOut("(2x^2-6x+8)/2", "x^{2}-3x+4"),                                           // 28
                new InOut("x^3+3x^2-6x-18=0", @"x = \sqrt{6}; x = -\sqrt{6}; x = -3"),                                                    // 29
                new InOut("(18x^3y^5)^0.5", @"3y^{2}x\sqrt{2yx}"),                                  // 30
                new InOut("x^2+3x+3=-3", @"x = \frac{i\sqrt{15}-3}{2}; x = \frac{-i\sqrt{15}-3}{2}"),           // 31
                new InOut("1/(6-3i)", @"\frac{2+i}{15}; (0.133333333333333+0.0666666666666667i)"),          // 32
                new InOut("a(b^(2x+1))+k=c", @"x = \frac{ln((-k+c))-ln(a)-ln(b)}{2ln(b)}"),          // 33
                new InOut("e^x=72", @"x = ln(72)"),             // 34
                new InOut("100^(x^2-6x+1)+5=10", @"x = \sqrt{(\frac{ln(5)}{ln(100)}+8)}+3; x = -\sqrt{(\frac{ln(5)}{ln(100)}+8)}+3"),       // 35
                new InOut("2^(1/2)/2", @"\frac{\sqrt{2}}{2}; 0.707106781186548"),     // 36
                new InOut("sec((4pi)/3)", "-2"),    // 37
                new InOut("cot((5pi)/6)", @"-\sqrt{3}; -1.73205080756888"),     // 38
                new InOut("((2pi)/3)+2pi", @"\frac{8pi}{3}; 8.37758040957278"),        // 39

                new InOut("sin(7pi)", "0"),         // 40
                new InOut("cos(9pi)", "-1"),        // 41
                new InOut("cot((43pi)/6)", @"\sqrt{3}; 1.73205080756888"),      // 42
                new InOut("asin(1/2)", @"\frac{pi}{6}; 0.523598775598299"),     // 43
                new InOut("arccos((-1*3^(1/2))/2)", @"\frac{5pi}{6}; 2.61799387799149"),    // 44

                new InOut("ae^(2x)+be^x+c=0", @"x = -ln(2a)+ln((-b+\sqrt{(b^{2}-4ca)})); x = -ln(2a)+ln((-b-\sqrt{(b^{2}-4ca)}))"),       // 45
                new InOut("ax^4+bx^2+c=0", @"x = \sqrt{\frac{(-b+\sqrt{(b^{2}-4ca)})}{2a}}; x = -\sqrt{\frac{(-b+\sqrt{(b^{2}-4ac)})}{2a}}; x = \sqrt{\frac{(-b-\sqrt{(b^{2}-4ca)})}{2a}}; x = -\sqrt{\frac{(-b-\sqrt{(b^{2}-4ac)})}{2a}}"),  // 46
                new InOut("ln(x) = 3", "x = e^{3}"),    // 47
                new InOut("2ln(3x) = 4", @"x = \frac{e^{2}}{3}"),       // 48
                new InOut("ln(x-2)+ln(2x-3)=2ln(x)", "x = 6; x = 1"),   // 49
                new InOut("log_(4)((3x-7)^2)=10", @"x = \frac{1031}{3}; x = -339"),      // 50
                new InOut("ln(x^2-6x-16)=5", @"x = \sqrt{(e^{5}+25)}+3; x = -\sqrt{(e^{5}+25)}+3"), // 51
                new InOut("4^(x+2)=8^(2x-y)", @"x = \frac{3y+4}{4}"),         // 52
                new InOut("9^(2x-1)=16^(4x+1)", @"x = \frac{2ln(2)+ln(3)}{2ln(3)-8ln(2)}"),       // 53
                new InOut("(3x+yx)/(3+y)", "x"),        // 54
                new InOut("ab^(mx+h)-k = j", @"x = \frac{-hln(b)+ln((k+j))-ln(a)}{mln(b)}"),     // 55
                new InOut("100(14^(2x))+6=10", @"x = \frac{-ln(25)}{2ln(14)}"),       // 56    finalize the power solving with this problem which is harder than it appears.
                new InOut("(sin(x)^2+sin(x))/sin(x)", "sin(x)+1"),          // 57
                new InOut("14*3^(x+4)-14*7^(yx)=0", @"x = \frac{-4ln(3)}{-yln(7)+ln(3)}"),  // 58
                new InOut("(x^2+4)/(2x+3)+y/(5x-6)", @"\frac{5x^{3}-6x^{2}+3y+2yx+20x-24}{10x^{2}+3x-18}"),           // 59
                new InOut("(x^2-4)/(x-2)=0", "x = 2; x = -2"),      // 60
                new InOut("(x^2-4)/(x-2)", "x+2"),      // 61
                new InOut("log_x(32) = 5", @"x = 2; x = 2(-isin(\frac{2pi}{5})+cos(\frac{2pi}{5})); x = 2(-isin(\frac{4pi}{5})+cos(\frac{4pi}{5})); x = 2(-isin(\frac{6pi}{5})+cos(\frac{6pi}{5})); x = 2(-isin(\frac{8pi}{5})+cos(\frac{8pi}{5}))"),      // 62
                new InOut("|log(x)|=4", @"x = 10000; x = \frac{1}{10000}"),        // 63
                new InOut("log(x)^2=4", @"x = 100; x = \frac{1}{100}"),        // 64
                new InOut("(ln(3)x-4xln(2))/(ln(3)-4ln(2))", "x"),    // 65
                new InOut("-2x^5 + 6x^4 + 10x^3 - 6x^2 - 9x + 4 = 0", "Partial Sols. x = 4 with (-2x^{4}-2x^{3}+2x^{2}+2x-1)(x-4)"), // 66
                new InOut("sin(-pi/4)", @"\frac{-\sqrt{2}}{2}; -0.707106781186548"),    // 67
                new InOut("2(x+2)^2-8=0", "x = 0; x = -4"),          // 68
                new InOut("-2(-x+2)^2-8 = 0", "x = (2-2i); x = (2+2i)"),         // 69
                new InOut("x^3 = 1", @"x = 1; x = \frac{-i\sqrt{3}-1}{2}; x = \frac{i\sqrt{3}-1}{2}"),        // 70
                new InOut("2(x+2)^2-8 >= 0", "restricted by x >= 0; x <= -4"),         // 71
                new InOut("(x+2)^2 > h/2+4", @"restricted by x > \sqrt{\frac{(h+8)}{2}}-2; x < -\sqrt{\frac{(h+8)}{2}}-2"),           // 72
                new InOut("-2(x - 5)^4 + 32 > 0", "restricted by 3 < x < 7"),          // 73
                //new InOut("-2(-x-5)^4+32 < 0", "restricted by x < -7; x > -3"),         // 74 WOW THIS WASN'T EVEN RIGHT!
                new InOut("-8(11n + 6) + 6(8 + 5n) = 2n + n", "n = 0"),   // 75
                new InOut("-8(11n + 6) + 6(8 + 5n) > 2n + n", "restricted by n < 0"),     // 76
                new InOut("x^3+3x^2+x+3 <= 0", "restricted by x <= -3"),       // 77
                new InOut("(x-5)^3(x+7)(x-1)^2 = 0", "x = 5, M: 3; x = -7; x = 1, M: 2"),    // 78
                new InOut("x^4-x^3-7x^2+13x-6 = 0", "x = 2; x = 1, M: 2; x = -3"),        // 79
                new InOut("(x-1)(x-1)(x+2)(x-1)(x+3) = 0", "x = 1, M: 3; x = -2; x = -3"),     // 80  
                new InOut("x^4 = 16", "x = 2; x = -2i; x = -2; x = 2i"),    // 81
                new InOut("(x-5)^3(2x+7)(3x-1)^2 > 0", @"restricted by -oo < x < \frac{-7}{2}; 5 < x < oo"),       // 82
                new InOut("(x-5)^2(2x+7)^2(3x-1)^2 > 0", @"restricted by -oo < x < \frac{-7}{2}; \frac{-7}{2} < x < \frac{1}{3}; \frac{1}{3} < x < 5; 5 < x < oo"),   // 83
                new InOut("-(1/4000)(x-5)^2(2x+7)^2(3x-1)^2 = 0", @"x = \frac{1}{3}, M: 2; x = \frac{-7}{2}, M: 2; x = 5, M: 2"),      // 84
                new InOut("-(1/4000)(x-5)^2(2x+7)^2(3x-1)^2 < 0", @"restricted by -oo < x < \frac{-7}{2}; \frac{-7}{2} < x < \frac{1}{3}; \frac{1}{3} < x < 5; 5 < x < oo"),  // 85
                new InOut("-2(x-2)^2(x+3)^2(2x-3)^2 > 0", "No solution"),                                   // 86
                new InOut("(x^3-x^2)/(x^2)", "x-1"),                                                       // 87
                new InOut("x^3-x^2 = 0", "x = 0, M: 2; x = 1"),                                           // 88
                new InOut("4 < x^2 < 9", "restricted by 2 < x < 3; -3 < x < -2"),                        // 89
                new InOut("-2 < (6-2x)/3 < 4", "restricted by -3 < x < 6"),                             // 90
                new InOut("(-x^4-5x^3+7x^2-12x)/x", "-x^{3}-5x^{2}+7x-12"),                            // 91
                new InOut("ln(x)=1", "x = e"),                                                        // 92
                new InOut("x^(-3)", @"\frac{1}{x^{3}}"),                                             // 93
                new InOut("ln(x) = -5", @"x = \frac{1}{e^{5}}"),                                    // 94
                new InOut("0 < ln(x) < 1", "restricted by 1 < x < e"),                             // 95                     
                new InOut("3x+2 <= 4", @"restricted by x <= \frac{2}{3}"),                        // 96
                new InOut("2sin(3x)-1=0", @"x = \frac{pi}{18}+\frac{2pin}{3}; x = \frac{5pi}{18}+\frac{2pin}{3}"),         // 97
                new InOut("2sin(x)+2^(1/2)=0", @"x = \frac{7pi}{4}+2pin; x = \frac{5pi}{4}+2pin"),        // 98
                new InOut("-sin(x) = 1", @"x = \frac{3pi}{2}+2pin"),     // 99
                new InOut("cos(x)=1", @"x = 2pin"),  // 100
                new InOut("cos(x)=0", @"x = \frac{pi}{2}+2pin; x = \frac{3pi}{2}+2pin"),     // 101
                new InOut("tan(x) = 1/(3^(1/2))", @"x = \frac{pi}{6}+pin"),  // 102
                new InOut("3tan(x)^2-1 = 0", @"x = \frac{pi}{6}+pin; x = \frac{11pi}{6}+pin"),      // 103
                new InOut("cot(x) = 0", @"x = \frac{pi}{2}+pin"),       // 104
                new InOut("sin(x)^2 - 5 = 0", "No solution"),      // 105
                new InOut("cot(x)cos(x)^2=2cot(x)", @"x = \frac{pi}{2}+pin"),   // 106
                new InOut("2sin(x)^2-sin(x)-1=0", @"x = \frac{pi}{2}+2pin; x = \frac{11pi}{6}+2pin; x = \frac{7pi}{6}+2pin"),  // 107
                new InOut("2sin(x)^2 + 3cos(x) - 3 = 0", @"x = 2pin; x = \frac{pi}{3}+2pin; x = \frac{5pi}{3}+2pin"),   // 108
                new InOut("2cos(3x-1)=0", @"x = \frac{2+pi}{6}+\frac{2pin}{3}; x = \frac{2+3pi}{6}+\frac{2pin}{3}"),     // 109 
                new InOut("3tan(x/2-1)+3 = 0", @"x = \frac{7pi+4}{2}+2pin"),   // 110     output not correct the displayed angle is not normalized
                new InOut("3tan((pix)/2)+3 = 0", @"x = \frac{7}{2}+2n"),     // 111    output not correct the displayed angle is not normalized.
                new InOut("(sin(x)+1)/cos(x)", "sec(x)+tan(x)"),    // 112  
                new InOut("sin(x)cos(x)", "cos(x)sin(x)"),          // 113
                new InOut("sin(x)cot(x)", "cos(x)"),                // 114
                new InOut("-cot(x)tan(x)", "-1"),                   // 115
                new InOut("sin(x)/tan(x)", "cos(x)"),               // 116
                new InOut("((-cos(x)/cot(x)) + (sin(x)/tan(x)) + ((cot(x)/tan(x))/cos(x)) )/cos(x)", "csc^{2}(x)-tan(x)+1"),        // 117
                new InOut("cos(x)=sin(x)", @"x = \frac{pi}{4}+pin"),                 // 118
                new InOut("-x+3>2x+1", @"restricted by x < \frac{2}{3}"),          // 119
                new InOut("|3x+2|/|x-1|=2", "x = -4; x = 0"),            // 120
                new InOut("(5x)/(x-1)>0", "restricted by -oo < x < 0; 1 < x < oo"),                          // 121
                new InOut("|3x+2|/|x-1|>2", "restricted by -oo < x < -4; 0 < x < 1; 1 < x < oo"),            // 122
                new InOut("(2+x-5x^2+2x^3)/(x^2-6x+9)>0", @"restricted by \frac{-1}{2} < x < 1; 2 < x < 3; 3 < x < oo"),         // 123
                new InOut("sin(4x)-(3^(1/2))/2=0", @"x = \frac{pi}{12}+\frac{pin}{2}; x = \frac{pi}{6}+\frac{pin}{2}"),       // 124
                new InOut("3tan(x)^3-tan(x)=0", @"x = pin; x = \frac{pi}{6}+pin; x = \frac{11pi}{6}+pin"),    // 125
                new InOut("((2)^(1/2))*((171)^(1/2))", @"3\sqrt{38}; 18.4932420089069"),             // 126
                new InOut("(x-2)(x-1)+27=0", @"x = \frac{-i\sqrt{107}+3}{2}; x = \frac{i\sqrt{107}+3}{2}"),       // 127
                
                // These are the inputs we can sorta do.
                //new InOut("2x+y=3; 4y-x=9", @"x = \frac{1}{3}; y = \frac{7}{3}"),       // 65

                //new InOut("sin(x)^4+cos(x)^2=2", "x = asin(\sqrt{"),       // The answer for this input is too complex for me to type. 
                // I think right now it is correct there should be 8 solutions and they look about right.

                //new InOut("x+3y-z=6; 2x-y+2z=1; 3x+2y-z=2", "x = ; y = ; z ="),   // 66
                //new InOut("2x^4 - 3x^3 - 5x^2 + 3x + 8 = 0", "x = "),   // 65 we shouldn't be able to solve this.
                //new InOut("a*sin(x)^2+b*sin(x)+c=0", "x = "),
                //new InOut("ln(x)^4+ln(x)+23=0", "x = "),
                //new InOut("(34)/(x^2-3x+7)+5-2x=-3", "x="),  has a cubic in it
            };

        private const int TEST_NUM = 126;

        private static Version _version = new Version(1, 2, 5, 3);

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
                if (sol.AlternateResult != null)
                {
                    Console.WriteLine("Alternate Result:");
                    outputStr = starterStr + sol.AlternateToTexStr();
                    outputStr = MathSolver.FinalizeOutput(outputStr);
                    Console.WriteLine(" " + outputStr);
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
                else if (inputStr == "test")
                {
                    // Put any repetative test input here.
                    inputStr = "\\int4x^3-8x^2+2x-5\\dx";
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
                if (solution.AlternateResult != null)
                {
                    resultStr += "; ";
                    resultStr += solution.AlternateToTexStr();
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
            MathSolverLibrary.TermType.EvalData evalData = new MathSolverLibrary.TermType.EvalData(true, new WorkMgr(), funcDefHelper);
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
            for (int i = 0; i < termEval.CmdCount; ++i)
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
            var result = termEval.ExecuteCommandIndex(optionIndex - 1, ref evalData);
            stopwatch.Stop();

            WriteLineColor(ConsoleColor.DarkCyan, "Evaluating took " + stopwatch.ElapsedMilliseconds.ToString() + "ms");

            if (evalData.Msgs != null)
            {
                foreach (string msg in evalData.Msgs)
                {
                    WriteLineColor(ConsoleColor.DarkYellow, " " + msg);
                }
            }

            if (!result.Success)
            {
                WriteLineColor(ConsoleColor.DarkRed, "Failure");
                foreach (string msg in evalData.FailureMsgs)
                {
                    WriteLineColor(ConsoleColor.Red, "  " + msg);
                }
            }
            else
            {
                if (result.Solutions == null)
                    return;

                int solCount = result.Solutions.Count;
                if (evalData.HasPartialSolutions)
                {
                    Console.WriteLine("The input was partially evaluated to...");
                    for (int i = 0; i < evalData.PartialSolutions.Count; ++i)
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
                    if (result.HasRestrictions)
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