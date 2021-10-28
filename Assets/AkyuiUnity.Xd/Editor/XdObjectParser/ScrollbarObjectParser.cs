using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ScrollbarObjectParser : IXdObjectParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.NameEndsWith("scrollbar") && ShapeObjectParser.Is(xdObject);
        }

        public Rect CalcSize(XdObjectJson xdObject)
        {
            return ShapeObjectParser.CalcSize(xdObject);
        }

        public (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder)
        {
            var (imageComponent, assets) = ShapeObjectParser.RenderImage(xdObject, obb, assetHolder, false);

            IComponent scrollbar = new VerticalScrollbarComponent(imageComponent);
            if (xdObject.HasParameter("horizontal"))
            {
                scrollbar = new HorizontalScrollbarComponent(imageComponent);
            }

            return (new[] { scrollbar }, assets, new IElement[] { });
        }
    }
}