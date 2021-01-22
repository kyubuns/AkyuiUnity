using System;
using AkyuiUnity.Editor.ScriptableObject;
using UnityEditor;
using UnityEngine;

namespace AkyuiUnity.Sample
{
    public class PostProcessImportAsset : AssetPostprocessor
    {
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var importedAsset in importedAssets) Debug.Log($"[Debug] importedAsset {importedAsset}");
            foreach (var deletedAsset in deletedAssets) Debug.Log($"[Debug] deletedAsset {deletedAsset}");
            foreach (var movedAsset in movedAssets) Debug.Log($"[Debug] movedAsset {movedAsset}");
            foreach (var movedFromAssetPath in movedFromAssetPaths) Debug.Log($"[Debug] movedFromAssetPath {movedFromAssetPath}");
        }
    }
}