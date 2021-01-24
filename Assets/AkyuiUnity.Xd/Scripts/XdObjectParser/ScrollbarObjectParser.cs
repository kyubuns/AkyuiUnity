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

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position)
        {
            var shapeObjectParser = new ShapeObjectParser();
            return shapeObjectParser.CalcSize(xdObject, position);
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder)
        {
            var shapeObjectParser = new ShapeObjectParser();
            var (components, assets) = shapeObjectParser.Render(xdObject, size, assetHolder);

            var scrollbar = new ScrollbarComponent(0, (ImageComponent) components[0]);
            return (new IComponent[] { scrollbar }, assets);
        }
    }
}