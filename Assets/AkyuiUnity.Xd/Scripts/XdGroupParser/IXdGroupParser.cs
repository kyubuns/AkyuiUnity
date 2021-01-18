using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdGroupParser
    {
        bool Is(XdObjectJson instanceObject, XdObjectJson symbolObject);
        IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject);
    }
}