﻿using System;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Operators
{
    internal class DotOperator : AgOp
    {
        public static ExComp StaticCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is FunctionDefinition && ex2 is FunctionDefinition)
            {
                FunctionDefinition funcDef1 = ex1 as FunctionDefinition;
                FunctionDefinition funcDef2 = ex2 as FunctionDefinition;

                if (funcDef1.GetHasCallArgs() || funcDef1.GetInputArgCount() != 1)
                    return null;

                ExComp[] callArgs = new ExComp[] { funcDef2 };
                funcDef1.SetCallArgs(callArgs);

                return funcDef1;
            }

            return null;
        }

        public static ExComp StaticWeakCombine(ExComp ex1, ExComp ex2)
        {
            if (ex1 is FunctionDefinition && ex2 is FunctionDefinition)
                return StaticCombine(ex1, ex2);
            // The open dot is not used for dot products. The closed dot is.
            //if (ex1 is ExVector && ex2 is ExVector)
            //{
            //    return new AlgebraTerm(ex1, new DotOperator(), ex2);
            //}

            return null;
        }

        public override ExComp CloneEx()
        {
            return new AddOp();
        }

        public override ExComp Combine(ExComp ex1, ExComp ex2)
        {
            return StaticCombine(ex1, ex2);
        }

        public override ExComp WeakCombine(ExComp ex1, ExComp ex2)
        {
            return StaticWeakCombine(ex1, ex2);
        }
    }
}