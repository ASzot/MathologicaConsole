using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MathSolverWebsite.MathSolverLibrary.Equation;

namespace MathSolverWebsite.MathSolverLibrary
{
    public static class DoubleHelper
    {
        public static bool IsInteger(this double d)
        {
            if (double.IsInfinity(d) || double.IsNaN(d))
                return false;
            string str = d.ToString();
            return !str.Contains(".");
        }
    }

    public static class StringHelper
    {

        public static string RemoveSurroundingParas(this string str)
        {
            if (str.StartsWith("(") && str.EndsWith(")"))
            {
                str = str.Remove(0, 1);
                str = str.Remove(str.Length - 1, 1);
            }
            return str;
        }

        public static string SurroundWithParas(this string str)
        {
            return str.Insert(str.Length, ")").Insert(0, "(");
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Environment.CurrentManagedThreadId))); }
        }
    }

    public class TypePair<T, K>
    {
        private T _data1;
        private K _data2;

        public T Data1
        {
            get { return _data1; }
            set { _data1 = value; }
        }

        public K Data2
        {
            get { return _data2; }
            set { _data2 = value; }
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
            return Data1.ToString() + ":" + Data2.ToString();
        }
    }

    internal static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    internal static class ObjectHelper
    {
        public static bool ContainsEx(this List<Equation.ExComp> exs, Equation.ExComp ex)
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