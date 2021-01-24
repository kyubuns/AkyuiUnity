using System.Linq;
using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    [CreateAssetMenu(menuName = "Akyui/ImportSettings", fileName = "AkyuiImportSettings")]
    public class AkyuiImportSettings : UnityEngine.ScriptableObject, IAkyuiImportSettings
    {
        public string PrefabOutputPath => prefabOutputPath;
        [SerializeField] private string prefabOutputPath = "Assets/{name}";

        public string AssetOutputDirectoryPath => assetOutputDirectoryPath;
        [SerializeField] private string assetOutputDirectoryPath = "Assets/{name}/";

        public string MetaOutputPath => metaOutputPath;
        [SerializeField] private string metaOutputPath = "Assets/{name}Meta";

        public string FontDirectoryPath => fontDirectoryPath;
        [SerializeField] private string fontDirectoryPath = "Assets/Fonts/";

        public bool CheckAssetHash => checkAssetHash;
        [SerializeField] private bool checkAssetHash = true;

        public IAkyuiImportTrigger[] Triggers => triggers?.Cast<IAkyuiImportTrigger>().ToArray() ?? new IAkyuiImportTrigger[] { };
        [SerializeField] private AkyuiImportTrigger[] triggers = default;
    }

    public interface IAkyuiImportSettings
    {
        string PrefabOutputPath { get; }
        string AssetOutputDirectoryPath { get; }
        string MetaOutputPath { get; }
        string FontDirectoryPath { get; }
        bool CheckAssetHash { get; }
        IAkyuiImportTrigger[] Triggers { get; }
    }
}