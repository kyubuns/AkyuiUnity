using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdGroupParser
    {
        bool Is(XdObjectJson instanceObject, XdObjectJson symbolObject);
        Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect);
        IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject, ref XdObjectJson[] children);
    }
}