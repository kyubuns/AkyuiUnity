using System.Collections.Generic;
using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
using AkyuiUnity.Loader;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    public interface IAkyuiImportTrigger : IAkyuiGenerateTrigger
    {
        void OnPreprocessAllAssets(IAkyuiLoader loader, ref List<IAsset> assets);
        void OnPreprocessAsset(IAkyuiLoader loader, ref byte[] bytes, ref IAsset asset, ref Dictionary<string, object> userData);
        void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset, ref Dictionary<string, object> userData);
        void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab);
        void OnPostprocessAllAssets(IAkyuiLoader loader, Object[] importAssets);
        string OnLoadAsset(string fileName);
    }

    public abstract class AkyuiImportTrigger : UnityEngine.ScriptableObject, IAkyuiImportTrigger
    {
        public virtual void OnPreprocessAllAssets(IAkyuiLoader loader, ref List<IAsset> assets)
        {
        }

        public virtual void OnPreprocessAsset(IAkyuiLoader loader, ref byte[] bytes, ref IAsset asset, ref Dictionary<string, object> userData)
        {
        }

        public virtual void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset, ref Dictionary<string, object> userData)
        {
        }

        public virtual void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab)
        {
        }

        public virtual void OnPostprocessAllAssets(IAkyuiLoader loader, Object[] importAssets)
        {
        }

        public virtual Component CreateComponent(GameObject gameObject, IComponent component, IAssetLoader assetLoader)
        {
            return null;
        }

        public virtual void OnPostprocessComponent(GameObject gameObject, IComponent component)
        {
        }

        public virtual string OnLoadAsset(string fileName)
        {
            return null;
        }
    }
}