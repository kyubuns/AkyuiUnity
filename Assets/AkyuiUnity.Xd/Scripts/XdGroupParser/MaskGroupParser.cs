using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class MaskGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.Meta?.Ux?.ClipPathResources?.Type == "clipPath";
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            var shapeObjectParser = new ShapeObjectParser();
            var clipPath = xdObject.Meta.Ux.ClipPathResources.Children[0];
            var shapeRect = shapeObjectParser.CalcSize(clipPath);
            shapeRect.position += new Vector2(clipPath.Transform?.Tx ?? 0f, clipPath.Transform?.Ty ?? 0f);
            return shapeRect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            var obb = obbGetter.Get(xdObject);
            var clipPath = xdObject.Meta.Ux.ClipPathResources.Children[0];
            var (imageComponent, assets) = ShapeObjectParser.RenderImage(clipPath, obb, assetHolder);
            return (new IComponent[] { new MaskComponent(imageComponent.Sprite) }, assets);
        }
    }
}