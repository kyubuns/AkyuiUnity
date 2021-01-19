using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ButtonGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson instanceObject, XdObjectJson symbolObject)
        {
            return instanceObject.Name.EndsWith("Button");
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject, ref XdObjectJson[] children)
        {
            return new IComponent[]
            {
                new ButtonComponent(0)
            };
        }
    }
}