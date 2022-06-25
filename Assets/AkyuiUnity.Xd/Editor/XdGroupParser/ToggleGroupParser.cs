using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ToggleGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject, XdObjectJson[] parents)
        {
            return xdObject.NameEndsWith("toggle");
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            return (new IComponent[]
            {
                new ToggleComponent()
            }, new IAsset[] { });
        }
    }
}