using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
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

        public Component SetOrCreateComponentValue(GameObject gameObject, TargetComponentGetter componentGetter, IComponent component, IAssetLoader assetLoader)
        {
            return null;
        }
    }
}