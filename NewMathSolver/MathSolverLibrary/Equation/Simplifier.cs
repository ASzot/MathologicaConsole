using MathSolverWebsite.MathSolverLibrary.Equation.Term;

namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal static class Simplifier
    {
        public static bool AreEqual(AlgebraTerm term0, AlgebraTerm term1, ref TermType.EvalData pEvalData)
        {
            return (Simplify(term0, ref pEvalData).IsEqualTo(Simplify(term1, ref pEvalData)));
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

            ExComp num = simpFrac.NumEx;
            ExComp den = simpFrac.DenEx;

            ExComp divided = Operators.DivOp.StaticCombine(num, den);

            if (divided.IsEqualTo(term))
            {
                if (num is AlgebraTerm)
                    num = (num as AlgebraTerm).FactorizeTerm(ref pEvalData);
                if (den is AlgebraTerm)
                    den = (den as AlgebraTerm).FactorizeTerm(ref pEvalData);

                divided = Operators.DivOp.StaticCombine(num, den);
            }

            return divided;
        }

        public static ExComp HarshSimplify(AlgebraTerm term, ref TermType.EvalData pEvalData, bool order = true)
        {
            // Harsh simplify per component.
            for (int i = 0; i < term.TermCount; ++i)
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

            term = term.RemoveRedundancies().ToAlgTerm();

            term = term.ApplyOrderOfOperations();

            term = term.MakeWorkable().ToAlgTerm();

            term = term.HarshEvaluation();

            if (!order)
                return term.RemoveRedundancies();

            term = term.RemoveRedundancies().ToAlgTerm();

            term = term.Order();

            return term;
        }

        public static ExComp Simplify(AlgebraTerm term, ref TermType.EvalData pEvalData)
        {


            bool noPowEval = term.ContainsOneOfFuncs(typeof(Functions.Calculus.Derivative), typeof(Functions.Calculus.Integral));

            term.EvaluateFunctions(false, ref pEvalData);

            if (!noPowEval)
                term = term.EvaluatePowers(ref pEvalData);

            term = term.ApplyOrderOfOperations();
            term = term.MakeWorkable().ToAlgTerm();

            if (term.HasTrigFunctions())
            {
                // There are trig functions in this expression.
                term = term.TrigSimplify();
            }

            term = term.Order();

            return term;
        }
    }
}