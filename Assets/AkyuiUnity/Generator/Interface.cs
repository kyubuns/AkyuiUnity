using UnityEngine;
using Object = UnityEngine.Object;

namespace AkyuiUnity.Generator
{
    public interface IAssetLoader
    {
        Sprite LoadSprite(string name);
        (GameObject, AkyuiPrefabMeta) LoadPrefab(Transform parent, string referenceName);
    }
}