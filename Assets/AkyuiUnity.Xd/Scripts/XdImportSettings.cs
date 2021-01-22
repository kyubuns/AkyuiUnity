using AkyuiUnity.Editor.ScriptableObject;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    [CreateAssetMenu(menuName = "Akyui/XdImportSettings", fileName = "XdImportSettings")]
    public class XdImportSettings : AkyuiImportSettings
    {
        public AkyuiXdObjectParser[] ObjectParsers => objectParsers;
        [SerializeField] private AkyuiXdObjectParser[] objectParsers = default;

        public AkyuiXdGroupParser[] GroupParsers => groupParsers;
        [SerializeField] private AkyuiXdGroupParser[] groupParsers = default;

        public AkyuiXdImportTrigger[] XdTriggers => xdTriggers;
        [SerializeField] private AkyuiXdImportTrigger[] xdTriggers = default;

        public string AkyuiOutputPath => akyuiOutputPath;
        [SerializeField] private string akyuiOutputPath = "";
    }
}