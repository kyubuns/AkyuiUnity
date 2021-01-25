using UnityEngine;

namespace AkyuiUnity.Generator
{
    public interface IAssetLoader
    {
        Sprite LoadSprite(string name);
        Font LoadFont(string name);
    }
}