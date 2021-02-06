using System.Linq;
using AkyuiUnity.Editor.ScriptableObject;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    [CreateAssetMenu(menuName = "AkyuiXd/XdImportSettings", fileName = "XdImportSettings")]
    public class XdImportSettings : AkyuiImportSettings
    {
        public AkyuiXdObjectParser[] ObjectParsers => objectParsers.Where(x => x != null).ToArray() ?? new AkyuiXdObjectParser[] { };
        [SerializeField] private AkyuiXdObjectParser[] objectParsers = default;

        public AkyuiXdGroupParser[] GroupParsers => groupParsers.Where(x => x != null).ToArray() ?? new AkyuiXdGroupParser[] { };
        [SerializeField] private AkyuiXdGroupParser[] groupParsers = default;

        public AkyuiXdImportTrigger[] XdTriggers => xdTriggers.Where(x => x != null).ToArray() ?? new AkyuiXdImportTrigger[] { };
        [SerializeField] private AkyuiXdImportTrigger[] xdTriggers = default;

        public string AkyuiOutputPath => akyuiOutputPath;
        [SerializeField] private string akyuiOutputPath = "";
    }
}