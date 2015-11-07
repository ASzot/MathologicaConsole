using System.Collections.Generic;

namespace MathSolverWebsite.MathSolverLibrary.Equation.Group
{
    internal static class GroupUtil
    {
        public static bool GpsEqual(ExComp[] gp1, ExComp[] gp2)
        {
            if (gp1.Length != gp2.Length)
                return false;

            List<TypePair<ExComp, bool>> matches = new List<TypePair<ExComp, bool>>();
            for (int i = 0; i < gp1.Length; ++i)
            {
                matches.Add(new TypePair<ExComp, bool>(gp1[i], false));
            }

            for (int i = 0; i < gp2.Length; ++i)
            {
                bool matchFound = false;
                for (int j = 0; j < matches.Count; ++j)
                {
                    if (matches[j].GetData2())
                        continue;
                    if (matches[j].GetData1().GetType() == gp2[i].GetType() &&
                        matches[j].GetData1().IsEqualTo(gp2[i]))
                    {
                        matches[j].SetData2(true);
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                    return false;
            }

            foreach (TypePair<ExComp, bool> match in matches)
            {
                if (!match.GetData2())
                    return false;
            }

            return true;
        }
    }
}