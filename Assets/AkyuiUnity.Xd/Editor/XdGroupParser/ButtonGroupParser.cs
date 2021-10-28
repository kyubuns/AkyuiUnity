using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ButtonGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject, XdObjectJson[] parents)
        {
            return xdObject.NameEndsWith("button");
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            return (new IComponent[]
            {
                new ButtonComponent()
            }, new IAsset[] { }, new IElement[] { });
        }
    }
}