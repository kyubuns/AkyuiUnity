using System.Collections.Generic;
using UnityEngine;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public static class GeneratorExtensions
    {
        public static GameObject[] GetDirectChildren(this GameObject gameObject)
        {
            var list = new List<GameObject>();
            for (var i = 0; i < gameObject.transform.childCount; ++i)
            {
                var child = gameObject.transform.GetChild(i);
                list.Add(child.gameObject);
            }
            return list.ToArray();
        }

        public static T[] GetComponentsInDirectChildren<T>(this GameObject gameObject) where T : Component
        {
            var list = new List<T>();
            for (var i = 0; i < gameObject.transform.childCount; ++i)
            {
                var child = gameObject.transform.GetChild(i);
                var component = child.GetComponent<T>();
                if (component != null)
                {
                    list.Add(component);
                }
            }
            return list.ToArray();
        }
    }
}