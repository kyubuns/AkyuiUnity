using UnityEngine;
using Object = UnityEngine.Object;

namespace AkyuiUnity.Generator
{
    public interface IAssetLoader
    {
        T LoadAsset<T>(string name) where T : Object;
        (GameObject, PrefabMeta) LoadPrefab(Transform parent, string referenceName);
    }
}