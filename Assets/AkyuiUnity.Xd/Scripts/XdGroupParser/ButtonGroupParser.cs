using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ButtonGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.GetSimpleName().EndsWith("Button");
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public IComponent[] Render(XdObjectJson xdObject, ref XdObjectJson[] children)
        {
            return new IComponent[]
            {
                new ButtonComponent(0)
            };
        }
    }
}