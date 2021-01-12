using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    public abstract class AkyuiImportTrigger : UnityEngine.ScriptableObject
    {
        public virtual void OnPostprocessAsset(byte[] bytes, string savePath, IAsset asset)
        {
        }

        public virtual void OnPostprocessPrefab(GameObject prefab, GameObjectWithId[] meta)
        {
        }
    }
}