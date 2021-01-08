using UnityEngine;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static void Import(string[] filePaths, AkyuiImportSettings settings)
        {
            foreach (var filePath in filePaths)
            {
                Debug.Log($"Import: {filePath}");
            }
        }
    }
}