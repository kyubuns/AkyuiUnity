using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
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
            foreach (var loader in loaders)
            {
                Debug.Log($"Import Start: {loader.LayoutInfo.Name}");
                Import(settings, loader);
                Debug.Log($"Import Finish: {loader.LayoutInfo.Name}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void Save(IAkyuiLoader loader, string filePath)
        {
            var bytes = AkyuiCompressor.Compress(loader);
            File.WriteAllBytes(filePath, bytes);
        }

        private static void CheckVersion(IAkyuiLoader loader)
        {
            var loaderVersionFull = loader.LayoutInfo.Meta.AkyuiVersion;
            var e1 = loaderVersionFull.Split('.');
            var loaderVersion = $"{e1[0]}.{e1[1]}";

            var e2 = Const.AkyuiVersion.Split('.');
            var importerVersion = $"{e2[0]}.{e2[1]}";

            if (loaderVersion == importerVersion) return;
            throw new Exception($"Cannot load version {loaderVersionFull} file. (Importer version is {Const.AkyuiVersion})");
        }

        private static void Import(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader)
        {
            CheckVersion(akyuiLoader);

            var pathGetter = new PathGetter(settings, akyuiLoader.LayoutInfo.Name);
            var assets = ImportAssets(settings, akyuiLoader, pathGetter);
            var (gameObject, hash) = ImportLayout(settings, akyuiLoader, pathGetter);

            var prevMetaGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.MetaSavePath);
            var prevAssets = prevMetaGameObject != null ? prevMetaGameObject.GetComponent<AkyuiMeta>().assets : new Object[] { };

            DeleteUnusedAssets(prevAssets, assets);

            var metaGameObject = new GameObject(akyuiLoader.LayoutInfo.Name);
            gameObject.transform.SetParent(metaGameObject.transform);
            var akyuiMeta = metaGameObject.AddComponent<AkyuiMeta>();
            akyuiMeta.hash = hash;
            akyuiMeta.root = gameObject;
            akyuiMeta.assets = assets;

            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, pathGetter.PrefabSavePath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(metaGameObject, pathGetter.MetaSavePath);

            Object.DestroyImmediate(metaGameObject);
        }

        private static void DeleteUnusedAssets(Object[] prevAssets, Object[] newAssets)
        {
            foreach (var prevAsset in prevAssets)
            {
                if (prevAsset == null) continue;
                if (newAssets.Any(x => x.name == prevAsset.name)) continue;

                var prevAssetPath = AssetDatabase.GetAssetPath(prevAsset);
                Debug.Log($"Delete unused asset {prevAssetPath}");
                AssetDatabase.DeleteAsset(prevAssetPath);
            }
        }

        private static Object[] ImportAssets(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, PathGetter pathGetter)
        {
            var assets = new List<Object>();
            var unityAssetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";

            var assetOutputDirectoryFullPath = Path.Combine(unityAssetsParentPath, pathGetter.AssetOutputDirectoryPath);
            if (!Directory.Exists(assetOutputDirectoryFullPath)) Directory.CreateDirectory(assetOutputDirectoryFullPath);

            foreach (var t in akyuiLoader.AssetsInfo.Assets)
            {
                var asset = t;
                var savePath = Path.Combine(pathGetter.AssetOutputDirectoryPath, asset.FileName);
                var saveFullPath = Path.Combine(unityAssetsParentPath, savePath);
                var bytes = akyuiLoader.LoadAsset(asset.FileName);

                if (settings.CheckAssetHash)
                {
                    if (File.Exists(saveFullPath))
                    {
                        var import = AssetImporter.GetAtPath(savePath);
                        if (import.userData == t.Hash.ToString())
                        {
                            Debug.Log($"Asset {asset.FileName} / Skip (same hash)");
                            assets.Add(AssetDatabase.LoadAssetAtPath<Object>(import.assetPath));
                            continue;
                        }
                    }
                }
                Debug.Log($"Asset {asset.FileName} / Import");

                foreach (var trigger in settings.Triggers) trigger.OnPreprocessAsset(ref bytes, ref asset);
                ImportAsset(asset, savePath, saveFullPath, bytes, settings);
                assets.Add(AssetDatabase.LoadAssetAtPath<Object>(savePath));
            }

            var importAssets = assets.ToArray();
            foreach (var trigger in settings.Triggers) trigger.OnPostprocessAllAssets(pathGetter.AssetOutputDirectoryPath, importAssets);

            return importAssets;
        }

        private static void ImportAsset(IAsset asset, string savePath, string saveFullPath, byte[] bytes, IAkyuiImportSettings importSettings)
        {
            PostProcessImportAsset.ProcessingFile = savePath;
            PostProcessImportAsset.Asset = asset;
            PostProcessImportAsset.Triggers = importSettings.Triggers;

            using (Disposable.Create(() =>
            {
                PostProcessImportAsset.ProcessingFile = null;
                PostProcessImportAsset.Asset = null;
                PostProcessImportAsset.Triggers = null;
            }))
            {
                if (asset is SpriteAsset)
                {
                    File.WriteAllBytes(saveFullPath, bytes);
                    AssetDatabase.ImportAsset(savePath);
                    return;
                }
            }

            Debug.LogError($"Unknown asset type {asset}");
        }

        private static (GameObject, long Hash) ImportLayout(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, PathGetter pathGetter)
        {
            var layoutInfo = akyuiLoader.LayoutInfo;
            Debug.Log($"Layout {layoutInfo.Name} / Import");

            var triggers = settings.Triggers.Select(x => (IAkyuiGenerateTrigger) x).ToArray();
            var (gameObject, hash) = AkyuiGenerator.GenerateGameObject(new EditorAssetLoader(pathGetter), layoutInfo, triggers);
            foreach (var trigger in settings.Triggers) trigger.OnPostprocessPrefab(ref gameObject);

            return (gameObject, hash);
        }
    }

    public class PostProcessImportAsset : AssetPostprocessor
    {
        public static string ProcessingFile { get; set; }
        public static IAsset Asset { get; set; }
        public static IAkyuiImportTrigger[] Triggers { get; set; }

        public void OnPreprocessAsset()
        {
            if (ProcessingFile != assetPath) return;

            if (assetImporter is TextureImporter textureImporter)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
            }

            assetImporter.userData = Asset.Hash.ToString();
            foreach (var trigger in Triggers) trigger.OnUnityPreprocessAsset(assetImporter, Asset);
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

        public Font LoadFont(string name)
        {
            var pathWithoutExtension = Path.Combine(_pathGetter.FontDirectoryPath, name);

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(pathWithoutExtension + ".ttf");
            if (ttf != null) return ttf;

            var otf = AssetDatabase.LoadAssetAtPath<Font>(pathWithoutExtension + ".otf");
            if (otf != null) return otf;

            Debug.LogWarning($"Font {pathWithoutExtension} is not found");
            return null;
        }
    }

    public class PathGetter
    {
        public string AssetOutputDirectoryPath { get; }
        public string PrefabSavePath { get; }
        public string MetaSavePath { get; }
        public string FontDirectoryPath { get; }

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

            var assetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);
            if (!assetOutputDirectoryPath.EndsWith("/")) assetOutputDirectoryPath += "/";
            AssetOutputDirectoryPath = assetOutputDirectoryPath;

            PrefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
            MetaSavePath = GetMetaPath(fileName);
            FontDirectoryPath = settings.FontDirectoryPath;
        }
    }
}