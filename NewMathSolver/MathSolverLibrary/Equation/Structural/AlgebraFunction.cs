using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using System.Collections.Generic;
using System.Linq;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    public enum FunctionType
    {
        Sinusodal,
        InverseSinusodal,
        Exponential,
        AbsoluteValue,
        Logarithm,
        LogarithmBase,      // This is only used in detecting the solve method.
        Summation,
        ChooseFunction,
        Factorial,
        Derivative,
        AntiDerivative,
        Limit,
        Deteriment, 
        Transpose,
        Gradient,
        Curl,
        Divergence,
    };

    internal abstract class AlgebraFunction : AlgebraTerm
    {
        public static ExComp operator *(AlgebraFunction a1, AlgebraFunction a2)
        {
            if (a1 is PowerFunction && a2 is PowerFunction)
            {
                return (a1 as PowerFunction) * (a2 as PowerFunction);
            }
            else if (a1.IsEqualTo(a2))
            {
                PowerFunction powFunc = new PowerFunction(a1, new Number(2.0));
                return powFunc;
            }
            else
            {
                AlgebraTerm term = new AlgebraTerm();
                term.Add(a1, new Operators.MulOp(), a2);
                return term;
            }
        }

        public static ExComp operator *(AlgebraFunction af, AlgebraComp comp)
        {
            if (af is Functions.PowerFunction)
            {
                return (af as PowerFunction) * comp;
            }

            AlgebraTerm term = new AlgebraTerm();
            term.Add(comp, new Operators.MulOp(), af);
            return term;
        }

        public static ExComp operator *(AlgebraFunction af, AlgebraTerm term)
        {
            if (af is PowerFunction)
                return (af as PowerFunction) * term;

            //throw new ArgumentException();

            List<ExComp[]> groups = term.GetGroupsNoOps();
            for (int i = 0; i < groups.Count; ++i)
            {
                ExComp[] group = groups[i];

                bool combined = false;
                for (int j = 0; j < group.Length; ++j)
                {
                    ExComp groupComp = group[j];

                    if (groupComp.IsEqualTo(af))
                    {
                        combined = true;
                        group[j] = new PowerFunction(group[j], new Number(2.0));
                        break;
                    }
                    else if (groupComp is PowerFunction && (groupComp as PowerFunction).Base.IsEqualTo(af))
                    {
                        combined = true;
                        group[j] = new PowerFunction(af,
                            Operators.AddOp.StaticCombine((groupComp as PowerFunction).Power, Number.One));
                        break;
                    }
                }

                if (!combined)
                {
                    List<ExComp> groupList = group.ToList();
                    groupList.Add(af);
                    groups[i] = groupList.ToArray().RemoveOneCoeffs();
                }
            }

            return new AlgebraTerm(groups.ToArray());
        }

        public static ExComp operator +(AlgebraFunction a1, AlgebraFunction a2)
        {
            AlgebraTerm term = new AlgebraTerm();
            term.Add(a1, new Operators.AddOp(), a2);
            return term;
        }

        protected virtual ExComp CancelWith(ExComp innerEx, ref TermType.EvalData evalData)
        {
            return null;
        }

        public abstract ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData);

        public override double GetCompareVal()
        {
            return 1.0;
        }

        public abstract List<Restriction> GetDomain(AlgebraVar varFor, AlgebraSolver agSolver, ref TermType.EvalData pEvalData);

        public override bool IsOne()
        {
            return false;
        }

        public override bool IsZero()
        {
            return false;
        }
    }
}