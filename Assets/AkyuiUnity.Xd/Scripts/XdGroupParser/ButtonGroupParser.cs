using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ButtonGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.NameEndsWith("button");
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter)
        {
            return (new IComponent[]
            {
                new ButtonComponent(0)
            }, new IAsset[] { });
        }
    }
}