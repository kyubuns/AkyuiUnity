using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ScrollbarObjectParser : IXdObjectParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            var shapeObjectParser = new ShapeObjectParser();
            return xdObject.NameEndsWith("scrollbar") && shapeObjectParser.Is(xdObject);
        }

        public Rect CalcSize(XdObjectJson xdObject)
        {
            var shapeObjectParser = new ShapeObjectParser();
            return shapeObjectParser.CalcSize(xdObject);
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder)
        {
            var (imageComponent, assets) = ShapeObjectParser.RenderImage(xdObject, obb, assetHolder);

            var scrollbar = new VerticalScrollbarComponent(imageComponent);
            return (new IComponent[] { scrollbar }, assets);
        }
    }
}