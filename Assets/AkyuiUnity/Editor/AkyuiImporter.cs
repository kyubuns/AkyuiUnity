using UnityEngine;

namespace AkyuiUnity.Editor
{
    [CreateAssetMenu(menuName = "Akyui/Importer", fileName = "AkyuiImporter")]
    public class AkyuiImporter : ScriptableObject
    {
        public string OutputPath => outputPath;
        [SerializeField] private string outputPath = "Assets";

        public void Import(string[] filePaths)
        {
            Debug.Log($"Import: {string.Join(", ", filePaths)}");
        }
    }
}