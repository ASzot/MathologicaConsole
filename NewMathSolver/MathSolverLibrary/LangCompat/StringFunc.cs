using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MathSolverWebsite.MathSolverLibrary.LangCompat
{
    public static class StringFunc
    {
        public static string Rm(string str, int start, int count)
        {
            return str.Remove(start, count);
        }

        public static string Format(string str, params object[] objs)
        {
            return String.Format(str, objs);
        }

        public static string Ins(string str, int index, string insertStr)
        {
            return str.Insert(index, insertStr);
        }
    }
}