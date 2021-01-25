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
            var shapeObjectParser = new ShapeObjectParser();
            var (components, assets) = shapeObjectParser.Render(xdObject, obb, assetHolder);

            var scrollbar = new VerticalScrollbarComponent((ImageComponent) components[0]);
            return (new IComponent[] { scrollbar }, assets);
        }
    }
}