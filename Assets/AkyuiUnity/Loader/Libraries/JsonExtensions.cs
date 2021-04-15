using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AkyuiUnity.Loader.Internal
{
    public static class JsonExtensions
    {
        public static string ToString(object o)
        {
            return (string) o;
        }

        public static bool ToBool(object o)
        {
            if (o is bool b) return b;
            throw new Exception($"{o} is {o.GetType()}");
        }

        public static int ToInt(object o)
        {
            if (o is long l) return (int) l;
            if (o is double d) return (int) d;
            throw new Exception($"{o} is {o.GetType()}");
        }

        public static uint ToUint(object o)
        {
            if (o is long l) return (uint) l;
            if (o is double d) return (uint) d;
            throw new Exception($"{o} is {o.GetType()}");
        }

        public static float ToFloat(object o)
        {
            if (o is long l) return (float) l;
            if (o is double d) return (float) d;
            throw new Exception($"{o} is {o.GetType()}");
        }

        public static int[] ToIntArray(object o)
        {
            var a = (List<object>) o;
            return a.Select(x =>
            {
                if (x is long l) return (int) l;
                if (x is double d) return (int) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
        }

        public static uint[] ToUintArray(object o)
        {
            var a = (List<object>) o;
            return a.Select(x =>
            {
                if (x is long l) return (uint) l;
                if (x is double d) return (uint) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
        }

        public static long[] ToLongArray(object o)
        {
            var a = (List<object>) o;
            return a.Select(x =>
            {
                if (x is long l) return l;
                if (x is double d) return (long) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
        }

        public static Color ToColor(object o)
        {
            var a = (List<object>) o;
            var b = a.Select(x =>
            {
                if (x is long l) return (byte) l;
                if (x is double d) return (byte) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
            return new Color32(b[0], b[1], b[2], b[3]);
        }

        public static Vector2Int ToVector2Int(object o)
        {
            var a = ToIntArray(o);
            return new Vector2Int(a[0], a[1]);
        }

        public static Vector2 ToVector2(object o)
        {
            var a = (List<object>) o;
            var b = a.Select(x =>
            {
                if (x is long l) return (float) l;
                if (x is double d) return (float) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();

            return new Vector2(b[0], b[1]);
        }

        public static Dictionary<string, string> ToStringDictionary(object o)
        {
            var a = (Dictionary<string, object>) o;
            return a.ToDictionary(x => x.Key, x => (string) x.Value);
        }

        public static Dictionary<string, object>[] ToDictionaryArray(object o)
        {
            var a = (List<object>) o;
            return a.Select(x => (Dictionary<string, object>) x).ToArray();
        }

        public static Dictionary<string, object> ToDictionary(object o)
        {
            return (Dictionary<string, object>) o;
        }
    }
}