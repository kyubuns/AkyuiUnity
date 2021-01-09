using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    [CreateAssetMenu(menuName = "Akyui/ImportSettings", fileName = "AkyuiImportSettings")]
    public class AkyuiImportSettings : UnityEngine.ScriptableObject
    {
        public string PrefabOutputPath => prefabOutputPath;
        [SerializeField] private string prefabOutputPath = "Assets/{name}";

        public string AssetOutputDirectoryPath => assetOutputDirectoryPath;
        [SerializeField] private string assetOutputDirectoryPath = "Assets/{name}/";

        public string MetaOutputPath => metaOutputPath;
        [SerializeField] private string metaOutputPath = "Assets/{name}Meta";

        public bool CheckTimestamp => checkTimestamp;
        [SerializeField] private bool checkTimestamp = true;

        public bool CheckAssetBinary => checkAssetBinary;
        [SerializeField] private bool checkAssetBinary = true;
    }
}