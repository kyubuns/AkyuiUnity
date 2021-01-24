using UnityEngine;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public static class GeneratorExtensions
    {
        public static T GetComponentInDirectChildren<T>(this GameObject gameObject) where T : Component
        {
            for (var i = 0; i < gameObject.transform.childCount; ++i)
            {
                var child = gameObject.transform.GetChild(i);
                var component = child.GetComponent<T>();
                if (component != null) return component;
            }
            return null;
        }
    }
}