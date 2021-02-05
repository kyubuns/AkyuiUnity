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

        public IAkyuiImportTrigger[] Triggers => triggers?.Cast<IAkyuiImportTrigger>().Where(x => x != null).ToArray() ?? new IAkyuiImportTrigger[] { };
        [SerializeField] private AkyuiImportTrigger[] triggers = default;

        public float SpriteSaveScale => spriteSaveScale;
        [SerializeField] private float spriteSaveScale = 1.0f;

        public bool ReimportLayout => reimportLayout;
        [SerializeField] private bool reimportLayout = false;

        public bool ReimportAsset => reimportAsset;
        [SerializeField] private bool reimportAsset = false;
    }

    public interface IAkyuiImportSettings
    {
        string PrefabOutputPath { get; }
        string AssetOutputDirectoryPath { get; }
        string MetaOutputPath { get; }
        string FontDirectoryPath { get; }
        IAkyuiImportTrigger[] Triggers { get; }
        float SpriteSaveScale { get; }
        bool ReimportAsset { get; }
        bool ReimportLayout { get; }
    }
}