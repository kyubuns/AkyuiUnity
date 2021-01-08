using UnityEngine;

namespace AkyuiUnity.Editor
{
    [CreateAssetMenu(menuName = "Akyui/ImportSettings", fileName = "AkyuiImportSettings")]
    public class AkyuiImportSettings : ScriptableObject
    {
        public string OutputPath => outputPath;
        [SerializeField] private string outputPath = "Assets";
    }
}