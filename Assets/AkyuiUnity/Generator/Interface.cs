using UnityEngine;

namespace AkyuiUnity.Generator
{
    public interface IAssetLoader
    {
        (GameObject, AkyuiPrefabMeta) LoadPrefab(Transform parent, string referenceName);
        Sprite LoadSprite(string name);
        Font LoadFont(string name);
    }
}