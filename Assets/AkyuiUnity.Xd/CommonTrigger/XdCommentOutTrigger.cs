using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd.CommonTrigger
{
    [CreateAssetMenu(menuName = "AkyuiXd/Triggers/XdCommentOut", fileName = nameof(XdCommentOutTrigger))]
    public class XdCommentOutTrigger : AkyuiXdImportTrigger
    {
        [SerializeField] private string startsWith = "#";

        public override XdObjectJson OnCreateXdObject(XdObjectJson xdObject)
        {
            var xdObjectName = xdObject?.Name ?? "";
            Debug.Log($"Trigger! {xdObjectName}");
            if (xdObjectName.StartsWith(startsWith)) return null;

            return xdObject;
        }
    }
}