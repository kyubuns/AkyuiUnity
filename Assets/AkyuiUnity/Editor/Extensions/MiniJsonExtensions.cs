using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AkyuiUnity.Editor.Extensions
{
    public static class MiniJsonExtensions
    {
        public static string JsonString(this object o)
        {
            return (string) o;
        }

        public static int JsonInt(this object o)
        {
            if (o is long l) return (int) l;
            if (o is double d) return (int) d;
            throw new Exception($"{o} is {o.GetType()}");
        }

        public static int[] JsonIntArray(this object o)
        {
            var a = (List<object>) o;
            return a.Select(x =>
            {
                if (x is long l) return (int) l;
                if (x is double d) return (int) d;
                throw new Exception($"{x} is {x.GetType()}");
            }).ToArray();
        }

        public static Color JsonColor(this object o)
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

        public static Vector2 JsonVector2(this object o)
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
    }
}