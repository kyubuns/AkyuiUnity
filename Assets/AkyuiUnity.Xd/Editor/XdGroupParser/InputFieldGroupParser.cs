using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class InputFieldGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject, XdObjectJson[] parents)
        {
            return xdObject.NameEndsWith("inputfield");
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            return (new IComponent[]
            {
                new InputFieldComponent()
            }, new IAsset[] { });
        }
    }
}