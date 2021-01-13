using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var akyuiLoaders = filePaths
                .Select(x => (IAkyuiLoader) new AkyuiLoader(x))
                .ToArray();

            Import(settings, akyuiLoaders);

            foreach (var akyuiLoader in akyuiLoaders) akyuiLoader.Dispose();
        }

        public static void Import(IAkyuiImportSettings settings, IAkyuiLoader[] loaders)
        {
            var dependencies = new Dictionary<string, string[]>();
            var nameToLoader = loaders.ToDictionary(x => x.LayoutInfo.Name, x => x);
            var unImported = loaders.Select(x => x.LayoutInfo.Name).ToList();
            foreach (var loader in loaders)
            {
                var reference = new List<string>();
                foreach (var e in loader.LayoutInfo.Elements)
                {
                    if (e is PrefabElement prefabElement)
                    {
                        reference.Add(prefabElement.Reference);
                    }
                }
                dependencies[loader.LayoutInfo.Name] = reference.Distinct().ToArray();
            }

            while (unImported.Count > 0)
            {
                var import = dependencies
                    .Where(x => unImported.Contains(x.Key))
                    .Where(x => x.Value.Count(y => unImported.Contains(y)) == 0)
                    .ToArray();

                foreach (var i in import)
                {
                    Debug.Log($"Import Start: {i.Key}");
                    unImported.Remove(i.Key);
                    Import(settings, nameToLoader[i.Key]);
                    Debug.Log($"Import Finish: {i.Key}");
                }

                if (!import.Any())
                {
                    Debug.LogError($"dependencies error");
                }
            }

            AssetDatabase.Refresh();
        }

        private static void Import(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader)
        {
            var pathGetter = new PathGetter(settings, akyuiLoader.LayoutInfo.Name);

            var layoutInfo = akyuiLoader.LayoutInfo;
            if (settings.CheckTimestamp)
            {
                var prevMetaFullPath = pathGetter.GetMetaFullPath(akyuiLoader.LayoutInfo.Name);
                if (File.Exists(prevMetaFullPath))
                {
                    var prevMetaObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.GetMetaPath(akyuiLoader.LayoutInfo.Name));
                    var prevMeta = prevMetaObject.GetComponent<AkyuiMeta>().meta;
                    if (prevMeta.timestamp == layoutInfo.Timestamp)
                    {
                        Debug.Log($"Skip (same timestamp)");
                        return;
                    }
                }
            }

            ImportAssets(settings, akyuiLoader, pathGetter);
            var (gameObject, meta) = AkyuiGenerator.GenerateGameObject(new EditorAssetLoader(pathGetter), layoutInfo);
            foreach (var trigger in settings.Triggers) trigger.OnPostprocessPrefab(ref gameObject, ref meta.idAndGameObjects);

            // meta
            var metaGameObject = new GameObject(akyuiLoader.LayoutInfo.Name);
            gameObject.transform.SetParent(metaGameObject.transform);
            var akyuiMeta = metaGameObject.AddComponent<AkyuiMeta>();
            akyuiMeta.meta = meta;

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

        public Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(_pathGetter.AssetOutputDirectoryPath, name));
        }

        public (GameObject, AkyuiPrefabMeta) LoadPrefab(Transform parent, string referenceName)
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