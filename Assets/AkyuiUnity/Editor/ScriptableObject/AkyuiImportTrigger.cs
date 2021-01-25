using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    public interface IAkyuiImportTrigger : IAkyuiGenerateTrigger
    {
        void OnPreprocessAsset(ref byte[] bytes, ref IAsset asset);
        void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset);
        void OnPostprocessPrefab(ref GameObject prefab);
        void OnPostprocessAllAssets(string outputDirectoryPath, Object[] importAssets);
    }

    public abstract class AkyuiImportTrigger : UnityEngine.ScriptableObject, IAkyuiImportTrigger
    {
        public virtual void OnPreprocessAsset(ref byte[] bytes, ref IAsset asset)
        {
        }

        public virtual void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset)
        {
        }

        public virtual void OnPostprocessPrefab(ref GameObject prefab)
        {
        }

        public virtual void OnPostprocessAllAssets(string outputDirectoryPath, Object[] importAssets)
        {
        }

        public virtual Component CreateComponent(GameObject gameObject, IComponent component, IAssetLoader assetLoader)
        {
            return null;
        }

        public virtual void OnPostprocessComponent(GameObject gameObject, IComponent component)
        {
        }
    }
}