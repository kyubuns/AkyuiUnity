using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class InputFieldGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.NameEndsWith("inputfield");
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter)
        {
            return (new IComponent[]
            {
                new InputFieldComponent()
            }, new IAsset[] { });
        }
    }
}