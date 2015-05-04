using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus.Vector
{
    class SurfaceIntegral : Integral
    {
        /// <summary>
        /// In the case _withRespect1 is null this is the surface differential.
        /// </summary>
        private AlgebraComp _withRespect0;
        private AlgebraComp _withRespect1;

        public SurfaceIntegral(ExComp innerEx)
            : base(innerEx)
        {
            
        }

        public static SurfaceIntegral ConstructSurfaceIntegral(ExComp innerEx, AlgebraComp surfaceDifferential)
        {
            return ConstructSurfaceIntegral(innerEx, surfaceDifferential, null);
        }

        public static SurfaceIntegral ConstructSurfaceIntegral(ExComp innerEx, AlgebraComp withRespect0, AlgebraComp withRespect1)
        {
            SurfaceIntegral surfaceIntegral = new SurfaceIntegral(innerEx);
            surfaceIntegral._withRespect0 = withRespect0;
            surfaceIntegral._withRespect1 = withRespect1;

            return surfaceIntegral;
        }
    }
}