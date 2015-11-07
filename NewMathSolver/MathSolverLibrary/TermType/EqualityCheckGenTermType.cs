using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Parsing;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class EqualityCheckGenTermType : GenTermType
    {
        private LexemeType _comparison;
        private ExComp _side0;
        private ExComp _side1;

        public EqualityCheckGenTermType(ExComp side0, ExComp side1, LexemeType comparison)
            : base("Verify")
        {
            _side0 = side0;
            _side1 = side1;
            _comparison = comparison;
        }

        public static bool EvalComparison(ExComp side0, ExComp side1, LexemeType comparison)
        {
            bool valid = false;

            if (comparison == LexemeType.EqualsOp)
                valid = side0.IsEqualTo(side1);
            else if (comparison == LexemeType.Greater)
            {
                if (side0 is ExNumber && side1 is ExNumber)
                    valid = ExNumber.OpGT((side0 as ExNumber), (side1 as ExNumber));
            }
            else if (comparison == LexemeType.GreaterEqual)
            {
                if (side0 is ExNumber && side1 is ExNumber)
                    valid = ExNumber.OpGE((side0 as ExNumber),(side1 as ExNumber));
                else
                    valid = side0.IsEqualTo(side1);
            }
            else if (comparison == LexemeType.Less)
            {
                if (side0 is ExNumber && side1 is ExNumber)
                    valid = ExNumber.OpLT((side0 as ExNumber), (side1 as ExNumber));
            }
            else if (comparison == LexemeType.LessEqual)
            {
                if (side0 is ExNumber && side1 is ExNumber)
                    valid = ExNumber.OpLE((side0 as ExNumber), (side1 as ExNumber));
                else
                    valid = side0.IsEqualTo(side1);
            }

            return valid;
        }

        public override Equation.SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            base.ExecuteCommand(command, ref pEvalData);

            // There is only one thing to be done.
            _side0 = SimpTerm(_side0, ref pEvalData);
            _side1 = SimpTerm(_side1, ref pEvalData);

            bool valid = EvalComparison(_side0, _side1, _comparison);

            pEvalData.AddMsg(valid ? "True" : "False");

            return SolveResult.Solved();
        }

        private ExComp SimpTerm(ExComp side, ref EvalData pEvalData)
        {
            ExComp originalTerm = side.CloneEx();
            AlgebraTerm agTerm;
            if (side is AlgebraTerm)
            {
                agTerm = side as AlgebraTerm;

                agTerm = Simplifier.AttemptCancelations(agTerm, ref pEvalData).ToAlgTerm();

                agTerm = agTerm.ApplyOrderOfOperations();
                side = agTerm.MakeWorkable();
                if (side is AlgebraTerm)
                    side = (side as AlgebraTerm).CompoundFractions();
                side = Equation.Functions.PowerFunction.FixFraction(side);
                if (side is AlgebraTerm)
                    side = (side as AlgebraTerm).RemoveRedundancies(false);
            }

            agTerm = side.ToAlgTerm();
            // Surround with an algebra term just to make sure everything is checked.
            AlgebraTerm surroundedAgTerm = new AlgebraTerm(agTerm);
            ExComp simpEx = Simplifier.Simplify(surroundedAgTerm, ref pEvalData);

            if (simpEx is AlgebraTerm)
                simpEx = (simpEx as AlgebraTerm).RemoveRedundancies(false);

            return simpEx;
        }
    }
}