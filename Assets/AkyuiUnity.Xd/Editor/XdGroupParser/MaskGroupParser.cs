using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class MaskGroupParser : IXdGroupParser
    {
        bool IXdGroupParser.Is(XdObjectJson xdObject, XdObjectJson[] parents)
        {
            return Is(xdObject);
        }

        public static bool Is(XdObjectJson xdObject)
        {
            return xdObject.Meta?.Ux?.ClipPathResources?.Type == "clipPath";
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            var clipPath = xdObject.Meta.Ux.ClipPathResources.Children[0];
            var shapeRect = ShapeObjectParser.CalcSize(clipPath);
            shapeRect.position += new Vector2(clipPath.Transform?.Tx ?? 0f, clipPath.Transform?.Ty ?? 0f);
            return shapeRect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            var obb = obbGetter.Get(xdObject);
            var clipPath = xdObject.Meta.Ux.ClipPathResources.Children[0];
            if (SvgUtil.IsAlphaOnly(clipPath)) return (new IComponent[] { }, new IAsset[] { });

            var (imageComponent, assets) = ShapeObjectParser.RenderImage(clipPath, obb, assetHolder);
            return (new IComponent[] { new MaskComponent(imageComponent.Sprite) }, assets);
        }
    }
}