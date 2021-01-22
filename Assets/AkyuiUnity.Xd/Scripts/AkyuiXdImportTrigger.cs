using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public abstract class AkyuiXdImportTrigger : UnityEngine.ScriptableObject
    {
        public virtual XdObjectJson OnCreateXdObject(XdObjectJson xdObject)
        {
            return xdObject;
        }
    }
}