using System.IO;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Generator;
using AkyuiUnity.Loader;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static void Import(IAkyuiImportSettings settings, string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                Debug.Log($"Import Start: {filePath}");

                using (IAkyuiLoader akyuiLoader = new AkyuiLoader(filePath))
                {
                    Import(settings, akyuiLoader);
                }

                Debug.Log($"Import Finish: {filePath}");
            }

            AssetDatabase.Refresh();
        }

        public static void Import(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader)
        {
            var pathGetter = new PathGetter(settings, akyuiLoader.FileName);

            var layoutInfo = akyuiLoader.LayoutInfo;
            if (settings.CheckTimestamp)
            {
                var prevMetaFullPath = pathGetter.GetMetaFullPath(akyuiLoader.FileName);
                if (File.Exists(prevMetaFullPath))
                {
                    var prevMetaObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.GetMetaPath(akyuiLoader.FileName));
                    var prevMeta = prevMetaObject.GetComponent<AkyuiMeta>().meta;
                    if (prevMeta.timestamp == layoutInfo.Timestamp)
                    {
                        Debug.Log($"Skip (same timestamp)");
                        return;
                    }
                }
            }

            ImportAssets(settings, akyuiLoader, pathGetter);
            var (gameObject, idAndGameObjects) = AkyuiGenerator.GenerateGameObject(new EditorAssetLoader(pathGetter), layoutInfo);
            foreach (var trigger in settings.Triggers) trigger.OnPostprocessPrefab(ref gameObject, ref idAndGameObjects);

            // meta
            var metaGameObject = new GameObject(akyuiLoader.FileName);
            gameObject.transform.SetParent(metaGameObject.transform);
            var akyuiMeta = metaGameObject.AddComponent<AkyuiMeta>();
            akyuiMeta.meta = new PrefabMeta
            {
                timestamp = layoutInfo.Timestamp,
                root = gameObject,
                idAndGameObjects = idAndGameObjects,
            };

            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, pathGetter.PrefabSavePath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(metaGameObject, pathGetter.MetaSavePath);

            Object.DestroyImmediate(metaGameObject);
        }

        private static void ImportAssets(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, PathGetter pathGetter)
        {
            var unityAssetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";

            var assetOutputDirectoryFullPath = Path.Combine(unityAssetsParentPath, pathGetter.AssetOutputDirectoryPath);
            if (!Directory.Exists(assetOutputDirectoryFullPath)) Directory.CreateDirectory(assetOutputDirectoryFullPath);

            foreach (var t in akyuiLoader.AssetsInfo.Assets)
            {
                var asset = t;
                var savePath = Path.Combine(pathGetter.AssetOutputDirectoryPath, asset.FileName);
                var saveFullPath = Path.Combine(unityAssetsParentPath, savePath);
                var bytes = akyuiLoader.LoadAsset(asset.FileName);

                if (settings.CheckTimestamp)
                {
                    if (File.Exists(saveFullPath))
                    {
                        var import = AssetImporter.GetAtPath(savePath);
                        if (import.userData == akyuiLoader.LayoutInfo.Timestamp.ToString())
                        {
                            Debug.Log($"Asset {asset.FileName} Skip (same timestamp)");
                            continue;
                        }
                    }
                }

                foreach (var trigger in settings.Triggers) trigger.OnPostprocessAsset(ref bytes, ref asset);
                ImportAsset(asset, savePath, saveFullPath, bytes);
            }
        }

        private static void ImportAsset(IAsset asset, string savePath, string saveFullPath, byte[] bytes)
        {
            if (asset is SpriteAsset)
            {
                File.WriteAllBytes(saveFullPath, bytes);

                PostProcessImportAsset.ProcessingFile = savePath;
                PostProcessImportAsset.Timestamp = asset.Timestamp;
                using (Disposable.Create(() => PostProcessImportAsset.ProcessingFile = ""))
                {
                    AssetDatabase.ImportAsset(savePath);
                }
                return;
            }

            Debug.LogError($"Unknown asset type {asset}");
        }
    }

    public class PostProcessImportAsset : AssetPostprocessor
    {
        public static string ProcessingFile { get; set; }
        public static int Timestamp { get; set; }

        public void OnPreprocessTexture()
        {
            if (ProcessingFile != assetPath) return;

            var textureImporter = (TextureImporter) assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.userData = Timestamp.ToString();
        }
    }

    public class EditorAssetLoader : IAssetLoader
    {
        private readonly PathGetter _pathGetter;

        public EditorAssetLoader(PathGetter pathGetter)
        {
            _pathGetter = pathGetter;
        }

        public T LoadAsset<T>(string name) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(Path.Combine(_pathGetter.AssetOutputDirectoryPath, name));
        }

        public (GameObject, PrefabMeta) LoadPrefab(Transform parent, string referenceName)
        {
            var metaGameObject = (GameObject) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(_pathGetter.GetMetaPath(referenceName)));
            PrefabUtility.UnpackPrefabInstance(metaGameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var referenceMeta = metaGameObject.GetComponent<AkyuiMeta>().GetCopiedMeta();
            var prefabGameObject = referenceMeta.root.gameObject;
            prefabGameObject.transform.SetParent(parent);
            Object.DestroyImmediate(metaGameObject);
            return (prefabGameObject, referenceMeta);
        }
    }

    public class PathGetter
    {
        public string AssetOutputDirectoryPath { get; }
        public string PrefabSavePath { get; }
        public string MetaSavePath { get; }

        public string GetMetaPath(string fileName) => _settings.MetaOutputPath.Replace("{name}", fileName) + ".prefab";

        public string GetMetaFullPath(string fileName)
        {
            var assetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";
            return Path.Combine(assetsParentPath, GetMetaPath(fileName));
        }

        private readonly IAkyuiImportSettings _settings;

        public PathGetter(IAkyuiImportSettings settings, string fileName)
        {
            _settings = settings;

            AssetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);
            PrefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
            MetaSavePath = GetMetaPath(fileName);
        }
    }
}