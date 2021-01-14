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

        public bool CheckHash => checkHash;
        [SerializeField] private bool checkHash = true;

        public AkyuiImportTrigger[] Triggers => triggers;
        [SerializeField] private AkyuiImportTrigger[] triggers = default;
    }

    public interface IAkyuiImportSettings
    {
        string PrefabOutputPath { get; }
        string AssetOutputDirectoryPath { get; }
        string MetaOutputPath { get; }
        bool CheckHash { get; }
        AkyuiImportTrigger[] Triggers { get; }
    }
}