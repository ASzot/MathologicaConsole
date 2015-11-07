namespace MathSolverWebsite.MathSolverLibrary.Equation
{
    internal class GeneralSolution : AlgebraTerm
    {
        private ExComp _result;
        private ExComp _interval;
        private AlgebraComp _iterVar;

        public ExComp GetResult()
        {
            return _result;
        }

        public GeneralSolution(ExComp result, ExComp interval, AlgebraComp iterationVar)
        {
            _result = result;
            if (result is GeneralSolution && (result as GeneralSolution)._iterVar.IsEqualTo(iterationVar))
            {
                _result = (result as GeneralSolution)._result;
                interval = Operators.AddOp.StaticCombine((result as GeneralSolution)._interval, interval);
            }
            _interval = interval;
            _iterVar = iterationVar;
        }

        public override ExComp CloneEx()
        {
            return new GeneralSolution(_result.CloneEx(), _interval.CloneEx(), (AlgebraComp)_iterVar.CloneEx());
        }

        public override AlgebraTerm Order()
        {
            ExComp orderedResult = _result is AlgebraTerm ? (_result as AlgebraTerm).Order() : _result;
            ExComp orderedInterval = _interval is AlgebraTerm ? (_interval as AlgebraTerm).Order() : _interval;

            return new GeneralSolution(orderedResult, orderedInterval, _iterVar);
        }

        public bool IsResultUndef()
        {
            return ExNumber.IsUndef(_result);
        }

        public override bool IsEqualTo(ExComp ex)
        {
            if (ex is GeneralSolution)
            {
                GeneralSolution compareEx = ex as GeneralSolution;
                if (!compareEx._interval.IsEqualTo(_interval))
                    return false;
                if (!compareEx._iterVar.IsEqualTo(_iterVar))
                    return false;
                if (!compareEx._result.IsEqualTo(_result))
                    return false;
                return true;
            }
            return false;
        }

        public override string ToJavaScriptString(bool useRad)
        {
            return null;
        }

        public override string ToString()
        {
            return _result.ToString() + "+" + _interval.ToString();
        }

        public override string ToTexString()
        {
            return _result.ToTexString() + "+" + _interval.ToTexString();
        }

        public string IterVarToTexString()
        {
            return _iterVar.ToTexString();
        }

        public string IterVarToDispString()
        {
            return _iterVar.ToDispString();
        }

        public override string FinalToAsciiString()
        {
            string finalStr = "";

            if (ExNumber.IsUndef(_result))
                return ExNumber.GetUndefined().ToAsciiString();

            if (!ExNumber.GetZero().IsEqualTo(_result) && !(_result is AlgebraTerm && (_result as AlgebraTerm).IsZero()))
            {
                if (_result is AlgebraTerm)
                    finalStr += (_result as AlgebraTerm).FinalToDispStr();
                else
                    finalStr += _result.ToAsciiString();

                finalStr += "+";
            }

            if (_interval is AlgebraTerm)
                finalStr += (_interval as AlgebraTerm).FinalToDispStr();
            else
                finalStr += _interval.ToAsciiString();

            return finalStr;
        }

        public override string FinalToTexString()
        {
            string finalStr = "";

            if (ExNumber.IsUndef(_result))
                return ExNumber.GetUndefined().ToTexString();

            if (!ExNumber.GetZero().IsEqualTo(_result) && !(_result is AlgebraTerm && (_result as AlgebraTerm).IsZero()))
            {
                if (_result is AlgebraTerm)
                    finalStr += (_result as AlgebraTerm).FinalToTexString();
                else
                    finalStr += _result.ToTexString();

                finalStr += "+";
            }

            if (_interval is AlgebraTerm)
                finalStr += (_interval as AlgebraTerm).FinalToTexString();
            else
                finalStr += _interval.ToTexString();

            return finalStr;
        }
    }
}