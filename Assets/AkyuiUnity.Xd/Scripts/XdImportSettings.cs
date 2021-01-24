using AkyuiUnity.Editor.ScriptableObject;
using UnityEngine;

namespace AkyuiUnity.Xd
{
    [CreateAssetMenu(menuName = "AkyuiXd/XdImportSettings", fileName = "XdImportSettings")]
    public class XdImportSettings : AkyuiImportSettings
    {
        public AkyuiXdObjectParser[] ObjectParsers => objectParsers ?? new AkyuiXdObjectParser[] { };
        [SerializeField] private AkyuiXdObjectParser[] objectParsers = default;

        public AkyuiXdGroupParser[] GroupParsers => groupParsers ?? new AkyuiXdGroupParser[] { };
        [SerializeField] private AkyuiXdGroupParser[] groupParsers = default;

        public AkyuiXdImportTrigger[] XdTriggers => xdTriggers ?? new AkyuiXdImportTrigger[] { };
        [SerializeField] private AkyuiXdImportTrigger[] xdTriggers = default;

        public float SvgSaveScale => svgSaveScale;
        [SerializeField] private float svgSaveScale = 1.0f;

        public string AkyuiOutputPath => akyuiOutputPath;
        [SerializeField] private string akyuiOutputPath = "";
    }
}