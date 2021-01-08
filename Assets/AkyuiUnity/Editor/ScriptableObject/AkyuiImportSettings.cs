using UnityEngine;

namespace AkyuiUnity.Editor.ScriptableObject
{
    [CreateAssetMenu(menuName = "Akyui/ImportSettings", fileName = "AkyuiImportSettings")]
    public class AkyuiImportSettings : UnityEngine.ScriptableObject
    {
        public string PrefabOutputPath => prefabOutputPath;
        [SerializeField] private string prefabOutputPath = "Assets/{name}/";

        public string AssetOutputPath => assetOutputPath;
        [SerializeField] private string assetOutputPath = "Assets/{name}";
    }
}