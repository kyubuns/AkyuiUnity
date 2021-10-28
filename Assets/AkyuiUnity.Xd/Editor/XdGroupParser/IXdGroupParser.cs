using JetBrains.Annotations;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdGroupParser
    {
        bool Is(XdObjectJson xdObject, XdObjectJson[] parents);
        Rect CalcSize(XdObjectJson xdObject, Rect rect);
        (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter);
    }

    public abstract class AkyuiXdGroupParser : ScriptableObject, IXdGroupParser
    {
        public abstract bool Is(XdObjectJson xdObject, XdObjectJson[] parents);
        public abstract Rect CalcSize(XdObjectJson xdObject, Rect rect);
        public abstract (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter);
    }
}