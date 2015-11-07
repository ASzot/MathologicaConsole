using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions
{
    internal class AbsValFunction : AppliedFunction
    {
        public AbsValFunction(ExComp ex)
            : base(ex, FunctionType.AbsoluteValue, typeof(AbsValFunction))
        {
        }

        public static ExComp MakePositive(ExComp ex)
        {
            if (ex is ExNumber)
                return ExNumber.Abs(ex as ExNumber);
            else if (ex is AlgebraTerm)
            {
                AlgebraTerm term = ex as AlgebraTerm;
                for (int i = 0; i < term.GetTermCount(); ++i)
                {
                    if (term[i] is ExNumber)
                    {
                        term[i] = ExNumber.Abs(term[i] as ExNumber);
                    }
                    if (term[i] is AlgebraTerm)
                    {
                        term[i] = MakePositive(term[i] as AlgebraTerm);
                    }
                }

                return term;
            }

            return ex;
        }

        public static ExComp[] MakePositive(ExComp[] group)
        {
            for (int i = 0; i < group.Length; ++i)
            {
                if (group[i] is ExNumber && ExNumber.OpLT((group[i] as ExNumber), 0.0))
                {
                    group[i] = ExNumber.OpMul((group[i] as ExNumber), -1.0);
                }
            }

            return group;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            CallChildren(harshEval, ref pEvalData);

            ExComp innerEx = GetInnerEx();

            if (ExNumber.IsUndef(innerEx))
                return ExNumber.GetUndefined();
            if (innerEx is ExNumber)
            {
                ExNumber absInner = ExNumber.Abs(innerEx as ExNumber);
                return absInner;
            }
            else if (innerEx is Equation.Structural.LinearAlg.ExVector)
            {
                // Vector magnitude.
                return (innerEx as Equation.Structural.LinearAlg.ExVector).GetVecLength();
            }
            else if (innerEx is AlgebraTerm && !(innerEx is AlgebraFunction))
            {
                AlgebraTerm innerTerm = innerEx as AlgebraTerm;
                List<ExComp[]> groups = innerTerm.GetGroupsNoOps();
                if (groups.Count == 1)
                {
                    ExComp[] gp = groups[0];
                    ExNumber coeff = GroupHelper.GetCoeff(gp);
                    if (coeff != null)
                    {
                        coeff = ExNumber.Abs(coeff);
                        GroupHelper.AssignCoeff(gp, coeff);
                        return new AbsValFunction(GroupHelper.ToAlgTerm(gp));
                    }
                }
            }

            return this;
        }

        public override string FinalToAsciiString()
        {
            return "|" + GetInnerTerm().FinalToAsciiString() + "|";
        }

        public override string ToAsciiString()
        {
            return "|" + GetInnerTerm().FinalToAsciiString() + "|";
        }

        public override string ToJavaScriptString(bool useRad)
        {
            string baseStr = base.ToJavaScriptString(useRad);
            if (baseStr == null)
                return null;
            return "Math.abs(" + baseStr + ")";
        }

        public override string ToString()
        {
            return ToTexString();
        }

        public override string ToTexString()
        {
            return "|" + GetInnerTerm().ToTexString() + "|";
        }
    }
}