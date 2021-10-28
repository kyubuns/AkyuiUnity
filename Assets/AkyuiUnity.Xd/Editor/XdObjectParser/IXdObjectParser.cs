using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdObjectParser
    {
        bool Is(XdObjectJson xdObject);
        Rect CalcSize(XdObjectJson xdObject);
        (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder);
    }

    public abstract class AkyuiXdObjectParser : ScriptableObject, IXdObjectParser
    {
        public abstract bool Is(XdObjectJson xdObject);
        public abstract Rect CalcSize(XdObjectJson xdObject);
        public abstract (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder);
    }
}