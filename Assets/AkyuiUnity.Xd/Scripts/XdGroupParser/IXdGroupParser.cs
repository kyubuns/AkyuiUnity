using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdGroupParser
    {
        bool Is(XdObjectJson xdObject);
        Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect);
        IComponent[] Render(XdObjectJson xdObject, ref XdObjectJson[] children);
    }
}