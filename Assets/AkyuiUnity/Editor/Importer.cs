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
            var logger = new AkyuiLogger("Akyui");
            using (var progressBar = new AkyuiProgressBar("Akyui"))
            {
                progressBar.SetTotal(loaders.Length);
                foreach (var loader in loaders)
                {
                    using (logger.SetCategory(loader.LayoutInfo.Name))
                    using (var progress = progressBar.TaskStart($"Importing {loader.LayoutInfo.Name}"))
                    {
                        logger.Log($"Import Start");
                        Import(settings, loader, logger, progress);
                        logger.Log($"Import Finish");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
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

        private static void Import(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, AkyuiLogger logger, IAkyuiProgress progress)
        {
            CheckVersion(akyuiLoader);

            var pathGetter = new PathGetter(settings, akyuiLoader.LayoutInfo.Name);
            var assets = ImportAssets(settings, akyuiLoader, pathGetter, logger, progress);
            var (gameObject, hash) = ImportLayout(settings, akyuiLoader, pathGetter, logger);

            var prevMetaGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.MetaSavePath);
            var prevAssets = prevMetaGameObject != null ? prevMetaGameObject.GetComponent<AkyuiMeta>().assets : new Object[] { };

            DeleteUnusedAssets(prevAssets, assets, logger);

            var metaGameObject = new GameObject(akyuiLoader.LayoutInfo.Name);
            gameObject.transform.SetParent(metaGameObject.transform);
            var akyuiMeta = metaGameObject.AddComponent<AkyuiMeta>();
            akyuiMeta.hash = hash;
            akyuiMeta.root = gameObject;
            akyuiMeta.assets = assets;

            CreateDirectory(Path.GetDirectoryName(pathGetter.PrefabSavePath), logger);
            CreateDirectory(Path.GetDirectoryName(pathGetter.MetaSavePath), logger);

            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, pathGetter.PrefabSavePath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(metaGameObject, pathGetter.MetaSavePath);

            Object.DestroyImmediate(metaGameObject);
        }

        private static void CreateDirectory(string path, AkyuiLogger logger)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path);
            CreateDirectory(parent, logger);
            logger.Log($"CreateDirectory {path}");
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void DeleteUnusedAssets(Object[] prevAssets, Object[] newAssets, AkyuiLogger logger)
        {
            var deletedUnusedAssets = new List<string>();
            foreach (var prevAsset in prevAssets)
            {
                if (prevAsset == null) continue;
                if (newAssets.Any(x => x.name == prevAsset.name)) continue;

                var prevAssetPath = AssetDatabase.GetAssetPath(prevAsset);
                deletedUnusedAssets.Add(Path.GetFileName(prevAssetPath));
                AssetDatabase.DeleteAsset(prevAssetPath);
            }

            if (deletedUnusedAssets.Count > 0)
            {
                logger.Log($"Delete unused asset", ("assets", string.Join(", ", deletedUnusedAssets)));
            }
        }

        private static Object[] ImportAssets(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, PathGetter pathGetter, AkyuiLogger logger, IAkyuiProgress progress)
        {
            using (logger.SetCategory("Assets"))
            {
                logger.Log($"Import Start");

                var assets = new List<Object>();
                var unityAssetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";

                var assetOutputDirectoryFullPath = Path.Combine(unityAssetsParentPath, pathGetter.AssetOutputDirectoryPath);
                if (!Directory.Exists(assetOutputDirectoryFullPath)) Directory.CreateDirectory(assetOutputDirectoryFullPath);

                var importAssetNames = new List<string>();
                var skipAssetNames = new List<string>();

                progress.SetTotal(akyuiLoader.AssetsInfo.Assets.Length);
                foreach (var t in akyuiLoader.AssetsInfo.Assets)
                {
                    var asset = t;
                    using (progress.TaskStart(asset.FileName))
                    {
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
                                    skipAssetNames.Add(asset.FileName);
                                    assets.Add(AssetDatabase.LoadAssetAtPath<Object>(import.assetPath));
                                    continue;
                                }
                            }
                        }
                        importAssetNames.Add(asset.FileName);

                        foreach (var trigger in settings.Triggers) trigger.OnPreprocessAsset(akyuiLoader, ref bytes, ref asset);
                        ImportAsset(settings, asset, savePath, saveFullPath, bytes, settings, logger);
                        assets.Add(AssetDatabase.LoadAssetAtPath<Object>(savePath));
                    }
                }

                var importAssets = assets.ToArray();
                foreach (var trigger in settings.Triggers) trigger.OnPostprocessAllAssets(akyuiLoader, pathGetter.AssetOutputDirectoryPath, importAssets);

                logger.Log($"Import Finish", ("import", importAssetNames.Count), ("skip", skipAssetNames.Count));

                return importAssets;
            }
        }

        private static void ImportAsset(IAkyuiImportSettings settings, IAsset asset, string savePath, string saveFullPath, byte[] bytes, IAkyuiImportSettings importSettings, AkyuiLogger logger)
        {
            PostProcessImportAsset.ProcessingFile = savePath;
            PostProcessImportAsset.Asset = asset;
            PostProcessImportAsset.Triggers = new IAkyuiImportTrigger[] { new SpriteImportTrigger(settings.SpriteSaveScale) }
                .Concat(importSettings.Triggers)
                .ToArray();

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

            logger.Error($"Unknown asset type {asset}");
        }

        private static (GameObject, long Hash) ImportLayout(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, PathGetter pathGetter, AkyuiLogger logger)
        {
            using (logger.SetCategory("Layout"))
            {
                logger.Log($"Import Start");
                var layoutInfo = akyuiLoader.LayoutInfo;
                var triggers = settings.Triggers.Select(x => (IAkyuiGenerateTrigger) x).ToArray();
                var (gameObject, hash) = AkyuiGenerator.GenerateGameObject(new EditorAssetLoader(pathGetter, logger), layoutInfo, triggers);
                foreach (var trigger in settings.Triggers) trigger.OnPostprocessPrefab(akyuiLoader, ref gameObject);
                logger.Log($"Import Finish");
                return (gameObject, hash);
            }
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

    public class SpriteImportTrigger : IAkyuiImportTrigger
    {
        private readonly float _saveScale;

        public SpriteImportTrigger(float saveScale)
        {
            _saveScale = saveScale;
        }

        public void OnUnityPreprocessAsset(AssetImporter assetImporter, IAsset asset)
        {
            if (!(assetImporter is TextureImporter textureImporter)) return;

            var spriteAsset = (SpriteAsset) PostProcessImportAsset.Asset;
            textureImporter.maxTextureSize = Mathf.RoundToInt(Mathf.Max(spriteAsset.Size.x, spriteAsset.Size.y) * _saveScale);
        }

        public void OnPreprocessAsset(IAkyuiLoader loader, ref byte[] bytes, ref IAsset asset) { }
        public void OnPostprocessPrefab(IAkyuiLoader loader, ref GameObject prefab) { }
        public void OnPostprocessAllAssets(IAkyuiLoader loader, string outputDirectoryPath, Object[] importAssets) { }
        public Component CreateComponent(GameObject gameObject, IComponent component, IAssetLoader assetLoader) => null;
        public void OnPostprocessComponent(GameObject gameObject, IComponent component) { }
    }

    public class EditorAssetLoader : IAssetLoader
    {
        private readonly PathGetter _pathGetter;
        private readonly AkyuiLogger _logger;

        public EditorAssetLoader(PathGetter pathGetter, AkyuiLogger logger)
        {
            _pathGetter = pathGetter;
            _logger = logger;
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

            _logger.Warning($"Font {pathWithoutExtension} is not found");
            return null;
        }
    }

    public class PathGetter
    {
        public string AssetOutputDirectoryPath { get; }
        public string PrefabSavePath { get; }
        public string MetaSavePath { get; }
        public string FontDirectoryPath { get; }

        public PathGetter(IAkyuiImportSettings settings, string fileName)
        {
            var assetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);
            if (!assetOutputDirectoryPath.EndsWith("/")) assetOutputDirectoryPath += "/";
            AssetOutputDirectoryPath = assetOutputDirectoryPath;

            PrefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
            MetaSavePath = settings.MetaOutputPath.Replace("{name}", fileName) + ".prefab";
            FontDirectoryPath = settings.FontDirectoryPath;
        }
    }
}