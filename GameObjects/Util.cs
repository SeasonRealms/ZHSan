using System;
using System.Collections.Generic;

namespace GameObjects
{
    public static class Util
    {
        public static void SortByDictValueDesc<T1, T2>(this Dictionary<T1, T2> dict) where T2 : IComparable
        {
            List<KeyValuePair<T1, T2>> list = new List<KeyValuePair<T1, T2>>(dict);
            list.Sort((pair1, pair2) => -pair1.Value.CompareTo(pair2.Value));
            dict.Clear();
            foreach (var pair in list)
            {
                dict.Add(pair.Key, pair.Value);
            }
        }   
    }
}