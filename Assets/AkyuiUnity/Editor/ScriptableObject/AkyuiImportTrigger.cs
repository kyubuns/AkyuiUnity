using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    public abstract class AkyuiImportTrigger : UnityEngine.ScriptableObject
    {
        public virtual void OnPostprocessAsset(ref byte[] bytes, ref IAsset asset)
        {
        }

        public virtual void OnPostprocessPrefab(ref GameObject prefab, ref GameObjectWithId[] meta)
        {
        }
    }
}