using MathSolverWebsite.MathSolverLibrary.Equation;
using System;
using System.Collections.Generic;
using MathSolverWebsite.MathSolverLibrary;
using MathSolverWebsite.MathSolverLibrary.LangCompat;

namespace MathSolverWebsite.MathSolverLibrary
{
    public static class DoubleHelper
    {
        public static bool IsInteger(double d)
        {
            if (DoubleFunc.IsInfinity(d) || double.IsNaN(d))
                return false;
            string str = d.ToString();
            return !str.Contains(".");
        }
    }

    public static class StringHelper
    {
        public static string RemoveSurroundingParas(string str)
        {
            if (str.StartsWith("(") && str.EndsWith(")"))
            {
                str = StringFunc.Rm(str, 0, 1);
                str = StringFunc.Rm(str, str.Length - 1, 1);
            }
            return str;
        }

        public static string SurroundWithParas(string str)
        {
            return StringFunc.Ins(StringFunc.Ins(str, str.Length, ")"), 0, "(");
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random GetThisThreadsRandom()
        {
            return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Environment.CurrentManagedThreadId)));
        }
    }

    public class TypePair<T, K>
    {
        private T _data1;
        private K _data2;

        public void SetData1(T value)
        {
            _data1 = value;
        }

        public T GetData1()
        {
            return _data1;
        }

        public void SetData2(K value)
        {
            _data2 = value;
        }

        public K GetData2()
        {
            return _data2;
        }

        public TypePair(T data1, K data2)
        {
            _data1 = data1;
            _data2 = data2;
        }

        public TypePair()
        {
        }

        public override string ToString()
        {
            return GetData1().ToString() + ":" + GetData2().ToString();
        }
    }

    internal static class ListExtensions
    {
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.GetThisThreadsRandom().Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    internal static class ObjectHelper
    {
        public static bool ContainsEx(List<Equation.ExComp> exs, Equation.ExComp ex)
        {
            foreach (ExComp compareEx in exs)
            {
                if (compareEx.IsEqualTo(ex))
                    return true;
            }

            return false;
        }
    }
}