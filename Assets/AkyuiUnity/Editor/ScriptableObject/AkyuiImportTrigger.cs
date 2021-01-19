using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
using AkyuiUnity.Loader;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    public abstract class AkyuiImportTrigger : UnityEngine.ScriptableObject, IAkyuiGenerateTrigger
    {
        public virtual void OnPostprocessAsset(ref byte[] bytes, ref IAsset asset)
        {
        }

        public virtual void OnPostprocessPrefab(ref GameObject prefab, ref GameObjectWithId[] meta)
        {
        }

        public virtual Component SetOrCreateComponentValue(Component target, IAssetLoader assetLoader, GameObject gameObject, IComponent component)
        {
            return null;
        }
    }
}