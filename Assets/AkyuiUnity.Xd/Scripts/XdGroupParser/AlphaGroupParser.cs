using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class AlphaGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            var opacity = xdObject.Style?.Opacity ?? 1.0f;
            if (Mathf.Abs(opacity - 1.0f) < 0.0001f) return false;
            return true;
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter)
        {
            var opacity = xdObject.Style?.Opacity ?? 1.0f;
            return (new IComponent[]
            {
                new AlphaComponent(0, opacity)
            }, new IAsset[] { });
        }
    }
}