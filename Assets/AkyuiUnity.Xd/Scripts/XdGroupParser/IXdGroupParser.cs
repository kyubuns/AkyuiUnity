using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdGroupParser
    {
        bool Is(XdObjectJson xdObject);
        Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect);
        (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter);
    }

    public abstract class AkyuiXdGroupParser : ScriptableObject, IXdGroupParser
    {
        public abstract bool Is(XdObjectJson xdObject);
        public abstract Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect);
        public abstract (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter);
    }
}