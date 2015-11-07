using MathSolverWebsite.MathSolverLibrary.Equation.Term;
using MathSolverWebsite.MathSolverLibrary.TermType;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal static class Simplifier
    {
        public static bool AreEqual(AlgebraTerm term0, AlgebraTerm term1, ref TermType.EvalData pEvalData)
        {
            bool areEqual = (Simplify(term0, ref pEvalData).IsEqualTo(Simplify(term1, ref pEvalData)));
            return areEqual;
        }

        public static ExComp AttemptCancelations(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            if (term.IsZero())
                return term;
            SimpleFraction simpFrac = new SimpleFraction();
            if (!simpFrac.Init(term))
                return term;

            if (simpFrac.IsDenOne())
                return term;

            ExComp num = simpFrac.GetNumEx();
            ExComp den = simpFrac.GetDenEx();

            ExComp divided = Operators.DivOp.StaticCombine(num, den);

            if (divided.IsEqualTo(term))
            {
                if (num is AlgebraTerm)
                    num = AdvAlgebraTerm.FactorizeTerm((num as AlgebraTerm), ref pEvalData, false);
                if (den is AlgebraTerm)
                    den = AdvAlgebraTerm.FactorizeTerm((den as AlgebraTerm), ref pEvalData, false);

                divided = Operators.DivOp.StaticCombine(num, den);
            }

            return divided;
        }

        /// <summary>
        /// Simplifies the term to the most primitive expression possible.
        /// This includes function evaluations, dividing fractions, and making the expression workable to ensure that everything possible is in the approximate form.
        /// </summary>
        /// <param name="term"></param>
        /// <param name="pEvalData"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static ExComp HarshSimplify(AlgebraTerm term, ref EvalData pEvalData, bool order)
        {
            // Harsh simplify per component.
            for (int i = 0; i < term.GetTermCount(); ++i)
            {
                if (term[i] is AlgebraTerm)
                    term[i] = HarshSimplify(term[i] as AlgebraTerm, ref pEvalData, order);
            }

            term = term.ApplyOrderOfOperations();
            term = term.MakeWorkable().ToAlgTerm();

            // Enclose the term to make sure functions work properly.
            AlgebraTerm enclosedTerm = new AlgebraTerm(term);

            term = enclosedTerm.HarshEvaluation();

            term.EvaluateFunctions(true, ref pEvalData);

            term = term.RemoveRedundancies(false).ToAlgTerm();

            term = term.ApplyOrderOfOperations();

            term = term.MakeWorkable().ToAlgTerm();

            term = term.HarshEvaluation();

            if (!order)
                return term.RemoveRedundancies(false);

            term = term.RemoveRedundancies(false).ToAlgTerm();

            term = term.Order();

            return term;
        }

        public static ExComp Simplify(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {
            bool noPowEval = AdvAlgebraTerm.ContainsOneOfFuncs(term, typeof(Functions.Calculus.Derivative), typeof(Functions.Calculus.Integral));

            term.EvaluateFunctions(false, ref pEvalData);

            if (!noPowEval)
                term = AdvAlgebraTerm.EvaluatePowers(term, ref pEvalData);

            term = term.ApplyOrderOfOperations();
            term = term.MakeWorkable().ToAlgTerm();

            if (term.HasTrigFunctions())
            {
                // There are trig functions in this expression.
                term = AdvAlgebraTerm.TrigSimplify(term, ref pEvalData);
            }

            term = term.Order();

            return term;
        }
    }
}