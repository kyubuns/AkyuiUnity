using UnityEngine;

namespace AkyuiUnity
{
    public static class ExtensionsUtil
    {
        public static string GetGameObjectPath(GameObject gameObject)
        {
            if (gameObject == null) return "";
            return $"{GetGameObjectPath(gameObject.transform.parent.gameObject)}/{gameObject.name}";
        }
    }
}