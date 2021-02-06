using UnityEngine;

namespace AkyuiUnity.Editor
{
    public static class ExtensionsUtil
    {
        public static string GetGameObjectPath(GameObject gameObject)
        {
            if (gameObject == null) return "";
            return GetGameObjectPath(gameObject.transform);
        }

        public static string GetGameObjectPath(Transform transform)
        {
            if (transform == null) return "";
            return $"{GetGameObjectPath(transform.parent)}/{transform.name}";
        }
    }
}