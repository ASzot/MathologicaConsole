using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathSolverWebsite.MathSolverLibrary.Equation.Operators;
using MathSolverWebsite.MathSolverLibrary.Equation.Structural.LinearAlg;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    class CurlFunc : FieldTransformation
    {
        public CurlFunc(ExComp innerEx)
            : base(innerEx, "curl", FunctionType.Curl, typeof(CurlFunc))
        {

        }

        public static bool IsSuitableField(ExComp innerEx)
        {
            if (innerEx is ExVector)
            {
                ExVector exVec = innerEx as ExVector;
                return exVec.Length > 1 && exVec.Length < 4;
            }
            else if (innerEx is FunctionDefinition)
            {
                FunctionDefinition funcDef = innerEx as FunctionDefinition;

                return funcDef.InputArgCount > 1 && funcDef.InputArgCount < 4;
            }
            else if (innerEx is AlgebraComp)
            {
                // The user is reasonably referring to a function not an individual variable.
                return true;
            }
            else
                return false;
        }

        protected override ExComp CancelWith(ExComp innerEx, ref TermType.EvalData pEvalData)
        {
            if (innerEx is GradientFunc)
            {
                pEvalData.WorkMgr.FromFormatted(this.FinalToDispStr() + "=0", "From the identity curl(\\nablaF)=0");
                return Number.Zero;
            }

            return null;
        }

        public override ExComp Evaluate(bool harshEval, ref TermType.EvalData pEvalData)
        {
            ExComp cancelWith = CancelWith(InnerEx, ref pEvalData);
            if (cancelWith != null)
                return cancelWith;

            ExComp p, q, r;
            ExComp innerEx = InnerEx;
            
            AlgebraComp x = null;
            AlgebraComp y = null;
            AlgebraComp z = null;

            bool isFuncDeriv;

            if (innerEx is AlgebraComp)
            {
                innerEx = new FunctionDefinition(innerEx as AlgebraComp, 
                    new AlgebraComp[] 
                    { 
                        new AlgebraComp("x"), 
                        new AlgebraComp("y"), 
                        new AlgebraComp("z") 
                    }, null);
            }

            if (innerEx is ExVector)
            {
                ExVector innerVec = innerEx as ExVector;
                p = innerVec.X;
                q = innerVec.Y;
                r = innerVec.Z;
                isFuncDeriv = false;
            }
            else if (innerEx is FunctionDefinition)
            {
                FunctionDefinition funcDef = innerEx as FunctionDefinition;
                p = new AlgebraComp("P");
                q = new AlgebraComp("Q");
                r = funcDef.InputArgCount == 3 ? new AlgebraComp("R") : null;

                x = funcDef.InputArgs[0];
                y = funcDef.InputArgs[1];
                z = funcDef.InputArgCount == 3 ? funcDef.InputArgs[2] : null;

                isFuncDeriv = true;
            }
            else
                return this;

            if (x == null)
            {
                x = new AlgebraComp("x");
                y = new AlgebraComp("y");
                z = new AlgebraComp("z");
            }

            if (isFuncDeriv)
            {
                pEvalData.WorkMgr.FromFormatted("", "Assuming " + WorkMgr.STM + innerEx.ToDispString() + WorkMgr.EDM + " is defined as the vector field " +
                    "P(x,y,z)" + ExVector.I + " + Q(x,y,z)" + ExVector.J + " + R(x,y,z)" + ExVector.K);
            }

            string formulaStr = "";
            string descStr = "";

            if (z != null)
            {
                formulaStr += "(\\frac{\\nabla R}{\\nabla y}  - \\frac{\\nabla Q}{\\nabla z})" + ExVector.I + 
                    "(\\frac{\\nabla P}{\\nabla z} - \\frac{\\nabla R}{\\nabla x})" + ExVector.J;
            }

            formulaStr += "(\\frac{\\nabal Q}{\\nabla x} - \\frac{\\nabla P}{\\nabla y})" + ExVector.K;
            if (!isFuncDeriv && innerEx is ExVector)
            {
                descStr += "Where ";
                string funcParamsStr = z == null ? "(x,y)" : "(x,y,z)";
                ExVector vec = innerEx as ExVector;
                descStr += WorkMgr.STM + "P" + formulaStr + " = " + WorkMgr.ExFinalToAsciiStr(vec.X) + ",Q" + formulaStr + "=" + 
                    WorkMgr.ExFinalToAsciiStr(vec.Y);
                if (z != null)
                    descStr += "," + "R" + funcParamsStr + "=" + WorkMgr.ExFinalToAsciiStr(vec.Z);
                descStr += WorkMgr.EDM;
            }

            pEvalData.WorkMgr.FromFormatted(WorkMgr.STM + this.FinalToDispStr() + "=" + formulaStr + WorkMgr.EDM, descStr);


            ExComp r_y;
            if (z != null)
                r_y = Derivative.TakeDeriv(r, y, ref pEvalData, true, isFuncDeriv);
            else
                r_y = Number.Zero;

            ExComp q_z;
            if (z != null)
                q_z = Derivative.TakeDeriv(q, z, ref pEvalData, true, isFuncDeriv);
            else
                q_z = Number.Zero;

            ExComp p_z;
            if (z != null)
                p_z = Derivative.TakeDeriv(p, z, ref pEvalData, true, isFuncDeriv);
            else
                p_z = Number.Zero;

            ExComp r_x;
            if (z != null)
                r_x = Derivative.TakeDeriv(r, x, ref pEvalData, true, isFuncDeriv);
            else
                r_x = Number.Zero;

            ExComp q_x = Derivative.TakeDeriv(q, x, ref pEvalData, true, isFuncDeriv);
            ExComp p_y = Derivative.TakeDeriv(p, y, ref pEvalData, true, isFuncDeriv);

            ExVector vec = new ExVector(
                SubOp.StaticCombine(r_y, q_z),
                SubOp.StaticCombine(p_z, r_x),
                SubOp.StaticCombine(q_x, p_y));

            return vec;
        }
    }
}
