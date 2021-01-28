using System.Collections.Generic;
using UnityEngine;

namespace AkyuiUnity.Generator
{
    public interface IAssetLoader
    {
        Sprite LoadSprite(string name);
        Font LoadFont(string name);
        Dictionary<string, object> LoadMeta(string name);
    }
}