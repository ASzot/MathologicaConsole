using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus;
using MathSolverWebsite.MathSolverLibrary.Solving.Diff_Eqs;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary.TermType
{
    internal class DiffEqGenTermType : GenTermType
    {
        private string _errorMsg = null;

        private List<InitialVal> _initValues = new List<InitialVal>();

        private ExComp _left;

        private int _order;

        private ExComp _right;

        public DiffEqGenTermType()
        {
        }

        public override SolveResult ExecuteCommand(string command, ref EvalData pEvalData)
        {
            if (_errorMsg != null)
            {
                pEvalData.AddFailureMsg(_errorMsg);
                return SolveResult.Failure();
            }

            if (command.StartsWith("Solve for d"))
            {
                command = StringFunc.Rm(command, 0, "Solve for d".Length);
                string[] tmps = command.Split('/');

                // Remove the first 'd'
                tmps[1] = StringFunc.Rm(tmps[1], 0, 1);
                AlgebraComp funcSolveFor = new AlgebraComp(tmps[0]);
                AlgebraComp withRespect = new AlgebraComp(tmps[1]);

                SolveResult diffAgSolve = DiffAgSolver.Solve(_left, _right, funcSolveFor, withRespect, _order, ref pEvalData);
                return diffAgSolve;
            }

            SolveResult invalidCmdSolveResult = SolveResult.InvalidCmd(ref pEvalData);
            return invalidCmdSolveResult;
        }

        public bool Init(EqSet eqSet, Dictionary<string, int> solveVars, string probSolveVar, ref EvalData pEvalData)
        {
            _left = eqSet.GetLeft();
            _right = eqSet.GetRight();

            List<Derivative> totalDerivs = new List<Derivative>();
            totalDerivs.AddRange(GetDerivs(_left, ref pEvalData));
            totalDerivs.AddRange(GetDerivs(_right, ref pEvalData));

            if (totalDerivs.Count == 0)
                return false;

            AlgebraComp funcIden = null;
            AlgebraComp withRespect = null;
            _order = int.MinValue;
            foreach (Derivative deriv in totalDerivs)
            {
                if (deriv.GetIsPartial())
                {
                    _errorMsg = "Partial differential equations are not supported";
                    return true;
                }

                if (funcIden == null)
                    funcIden = deriv.GetDerivOf();
                else if (!funcIden.IsEqualTo(deriv.GetDerivOf()))
                {
                    _errorMsg = "Derivatives of different functions";
                    return true;
                }

                if (withRespect == null)
                    withRespect = deriv.GetWithRespectTo();
                else if (deriv.GetWithRespectTo() != null && !withRespect.IsEqualTo(deriv.GetWithRespectTo()))
                {
                    _errorMsg = "Derivatives are not with respect to same variable";
                    return true;
                }

                int currentOrder = deriv.GetOrderInt();
                if (currentOrder == -1)
                {
                    _errorMsg = "Must have numeric order of differential equation";
                    return true;
                }

                if (currentOrder > _order)
                    _order = currentOrder;
            }

            if (funcIden == null)
            {
                _errorMsg = "Internal error";
                return true;
            }

            List<string> cmds = new List<string>();

            if (probSolveVar == null || probSolveVar == funcIden.GetVar().GetVar())
                probSolveVar = "x";

            if (withRespect == null)
            {
                cmds.Add("Solve for d" + funcIden.ToTexString() + "/d" + probSolveVar);
                foreach (string solveVar in solveVars.Keys)
                {
                    if (solveVar == probSolveVar || solveVar == funcIden.GetVar().GetVar())
                        continue;
                    cmds.Add("Solve for d" + funcIden.ToTexString() + "/d" + solveVar);
                }
            }
            else
            {
                cmds.Add("Solve for d" + funcIden.ToTexString() + "/d" + withRespect.ToTexString());
            }

            _cmds = cmds.ToArray();

            return true;
        }

        public bool Init(List<EqSet> eqSets)
        {
            // Determine which is the actual differential equation and which others are just initial values.
            int eqIndex = -1;
            for (int i = 0; i < eqSets.Count; ++i)
            {
                EqSet eqSet = eqSets[i];
                ExComp left = eqSet.GetLeft();
                ExComp right = eqSet.GetRight();

                FunctionDefinition funcDef = null;
                ExComp val = null;
                if (left is FunctionDefinition && !DiffAgSolver.ContainsDerivative(right))
                {
                    funcDef = left as FunctionDefinition;
                    val = right;
                }
                else if (right is FunctionDefinition && !DiffAgSolver.ContainsDerivative(left))
                {
                    funcDef = right as FunctionDefinition;
                    val = left;
                }

                if (funcDef == null || val == null)
                {
                    if (eqIndex != -1)
                        return false;
                    eqIndex = i;
                    continue;
                }

                if (funcDef.GetCallArgs() == null || funcDef.GetCallArgs().Length != 1)
                {
                    return false;
                }

                InitialVal initVal = new InitialVal(funcDef.GetIden(), val, funcDef.GetCallArgs()[0]);
                _initValues.Add(initVal);
            }

            if (eqIndex == -1)
                return false;

            EqSet diffEqSet = eqSets[eqIndex];

            return false;
        }

        private List<Derivative> GetDerivs(ExComp ex, ref EvalData pEvalData)
        {
            List<Derivative> derivs = new List<Derivative>();
            if (ex is Derivative)
            {
                Derivative deriv = ex as Derivative;

                bool isAlreadyDefined = false;
                if (deriv.GetDerivOf() != null && pEvalData.GetFuncDefs().IsFuncDefined(deriv.GetDerivOf().GetVar().GetVar()))
                    isAlreadyDefined = true;
                else if (_multiLineHelper != null && deriv.GetDerivOf() != null && _multiLineHelper.IsPreDefined(deriv.GetDerivOf()))
                    isAlreadyDefined = true;

                if (!isAlreadyDefined && deriv.GetDerivOf() != null)
                    derivs.Add(deriv);
            }
            else if (ex is AlgebraTerm)
            {
                AlgebraTerm term = ex as AlgebraTerm;
                foreach (ExComp termEx in term.GetSubComps())
                {
                    List<Derivative> subDerivs = GetDerivs(termEx, ref pEvalData);
                    derivs.AddRange(subDerivs);
                }
            }

            return derivs;
        }

        private struct InitialVal
        {
            public AlgebraComp FuncIden;
            public ExComp FuncValue;
            public ExComp InputValue;

            public InitialVal(AlgebraComp funcIden, ExComp funcVal, ExComp inputVal)
            {
                FuncIden = funcIden;
                FuncValue = funcVal;
                InputValue = inputVal;
            }
        }
    }
}