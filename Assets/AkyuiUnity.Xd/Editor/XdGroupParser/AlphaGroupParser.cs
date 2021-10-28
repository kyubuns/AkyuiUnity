using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class AlphaGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject, XdObjectJson[] parents)
        {
            var opacity = xdObject.Style?.Opacity ?? 1.0f;
            if (Mathf.Abs(opacity - 1.0f) < 0.0001f) return false;
            return true;
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            var opacity = xdObject.Style?.Opacity ?? 1.0f;
            return (new IComponent[]
            {
                new AlphaComponent(opacity)
            }, new IAsset[] { }, new IElement[] { });
        }
    }
}