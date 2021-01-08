using UnityEngine;

namespace AkyuiUnity.Editor
{
    [CreateAssetMenu(menuName = "Akyui/ImportSettings", fileName = "AkyuiImportSettings")]
    public class AkyuiImportSettings : ScriptableObject
    {
        public string PrefabOutputPath => prefabOutputPath;
        [SerializeField] private string prefabOutputPath = "Assets/";

        public string AssetOutputPath => assetOutputPath;
        [SerializeField] private string assetOutputPath = "Assets/";
    }
}