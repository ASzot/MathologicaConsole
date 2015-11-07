namespace MathSolverWebsite.MathSolverLibrary.Equation.Functions.Calculus
{
    internal class IntegrationInfo
    {
        public const int MAX_U_SUB_COUNT = 3;
        public const int MAX_BY_PARTS_COUNT = 3;

        public int USubCount = 0;
        public int ByPartsCount = 0;

        public void IncPartsCount()
        {
            ByPartsCount++;
        }

        public void IncSubCount()
        {
            USubCount++;
        }
    }
}