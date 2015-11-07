using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MathSolverWebsite.MathSolverLibrary.Equation;
using MathSolverWebsite.MathSolverLibrary.Equation.Functions;
using MathSolverWebsite.MathSolverLibrary.Parsing;

namespace MathSolverWebsite.MathSolverLibrary.LangCompat
{
    internal static class ArrayFunc
    {
        public static List<T> ToList<T>(T[] arr)
        {
            return arr.ToList();
        }

        public static List<int> ToList(int[] arr)
        {
            return arr.ToList();
        }

    public static T[] ToArray<T>(IEnumerable<T> list)
        {
            return list.ToArray();
        }

        public static List<TypePair<LexemeType, LexicalParser.MatchTolken>> OrderList(List<TypePair<LexemeType, LexicalParser.MatchTolken>> list)
        {
            return (from ele in list
                orderby ele.GetData2().Index
                select ele).ToList();
        }

        public static bool ContainsKey<T, K>(Dictionary<T, K> dict, T key)
        {
            return dict.ContainsKey(key);
        }

        public static List<string> Distinct(Dictionary<string, int> dict)
        {
            return (from dictPair in dict
                    select dictPair.Key).Distinct().ToList();
        }

        public static List<TypePair<int, TrigFunction>> OrderList(List<TypePair<int, TrigFunction>> list)
        {
            return list.OrderBy(trigFuncIntPow => trigFuncIntPow.GetData1()).ToList();
        }

        public static List<TypePair<ExComp, int>> OrderList(List<TypePair<ExComp, int>> list)
        {
            return list.OrderBy(x => x.GetData2()).ToList();
        }

        public static List<TypePair<double, ExComp>> OrderList(List<TypePair<double, ExComp>> list)
        {
            return list.OrderBy(x => x.GetData1()).ToList();
        }

        public static bool Contains<T>(T[] arr, T val)
        {
            return arr.Contains(val);
        }

        public static void IntersectLists<T>(List<T> list1, List<T> list2)
        {
            list1.Intersect(list2);
        }

        public static List<ExComp[]> OrderListReverse(List<ExComp[]> list)
        {
            return list.OrderBy(g => GroupHelper.GetHighestPower(g)).Reverse().ToList();
        }

        public static List<ExComp> OrderListReverse(List<ExComp> list)
        {
            return list.OrderBy(x => x.GetCompareVal()).Reverse().ToList();
        }

        public static void Reverse<T>(List<T> list)
        {
            list.Reverse();
        }

        public static void RemoveIndex<T>(List<T> list, int index)
        {
            list.RemoveAt(index);
        }

        public static T GetAt<T>(List<T> list, int index)
        {
            return list[index];
        }

        public static void SetAt<T>(List<T> list, int index, T value)
        {
            list[index] = value;
        }

        public static int GetCount<T>(List<T> list)
        {
            return list.Count;
        }

        public static int GetCount<T>(IEnumerable<T> list)
        {
            return list.Count();
        }

        public static KeyValuePair<T, K> CreateKeyValuePair<T, K>(T key, K val)
        {
            return new KeyValuePair<T, K>(key, val);
        }
    }
}