using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
using AkyuiUnity.Loader;
using AkyuiUnity.Loader.Internal;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static IAkyuiImportSettings Settings { get; private set; }

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

            Settings = settings;
            using (Disposable.Create(() => Settings = null))
            {
                var pathGetter = new PathGetter(settings, akyuiLoader.LayoutInfo.Name);
                var prevMetaGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.MetaSavePath);
                var prevMeta = prevMetaGameObject != null ? prevMetaGameObject.GetComponent<AkyuiMeta>() : null;

                if (prevMeta != null && prevMeta.hash == akyuiLoader.LayoutInfo.Hash)
                {
                    logger.Log("Skip", ("hash", akyuiLoader.LayoutInfo.Hash));
                    return;
                }

                var assets = ImportAssets(settings, akyuiLoader, pathGetter, logger, progress);
                var (gameObject, hash) = ImportLayout(settings, akyuiLoader, pathGetter, logger);
                var prevAssets = prevMeta != null ? prevMeta.assets : new Object[] { };
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

                var allAssets = akyuiLoader.AssetsInfo.Assets.ToList();
                foreach (var trigger in settings.Triggers) trigger.OnPreprocessAllAssets(akyuiLoader, ref allAssets);

                progress.SetTotal(allAssets.Count);
                foreach (var tmp in allAssets)
                {
                    var asset = tmp;
                    using (progress.TaskStart(asset.FileName))
                    {
                        var savePath = Path.Combine(pathGetter.AssetOutputDirectoryPath, asset.FileName);
                        var saveFullPath = Path.Combine(unityAssetsParentPath, savePath);
                        var bytes = akyuiLoader.LoadAsset(asset.FileName);

                        if (settings.CheckAssetHash && File.Exists(saveFullPath))
                        {
                            var import = AssetImporter.GetAtPath(savePath);
                            var prevUserData = MiniJSON.Json.Deserialize(import.userData).JsonDictionary();
                            if (prevUserData["hash"].JsonLong() == asset.Hash)
                            {
                                skipAssetNames.Add(asset.FileName);
                                assets.Add(AssetDatabase.LoadAssetAtPath<Object>(import.assetPath));
                                continue;
                            }
                        }

                        var userData = new Dictionary<string, object>();
                        userData["hash"] = asset.Hash;

                        if (asset is SpriteAsset)
                        {
                            var texture = new Texture2D(2, 2);
                            texture.LoadImage(bytes);

                            userData["source_width"] = texture.width;
                            userData["source_height"] = texture.height;
                        }

                        foreach (var trigger in settings.Triggers) trigger.OnPreprocessAsset(akyuiLoader, ref bytes, ref asset, ref userData);
                        ImportAsset(asset, savePath, saveFullPath, bytes, userData, settings, logger);
                        assets.Add(AssetDatabase.LoadAssetAtPath<Object>(savePath));
                        importAssetNames.Add(asset.FileName);
                    }
                }

                var importAssets = assets.ToArray();
                foreach (var trigger in settings.Triggers) trigger.OnPostprocessAllAssets(akyuiLoader, pathGetter.AssetOutputDirectoryPath, importAssets);

                logger.Log($"Import Finish", ("import", importAssetNames.Count), ("skip", skipAssetNames.Count));

                return importAssets;
            }
        }

        private static void ImportAsset(IAsset asset, string savePath, string saveFullPath, byte[] bytes, Dictionary<string, object> userData, IAkyuiImportSettings importSettings, AkyuiLogger logger)
        {
            PostProcessImportAsset.ProcessingFile = savePath;
            PostProcessImportAsset.Asset = asset;
            PostProcessImportAsset.UserData = userData;
            PostProcessImportAsset.Triggers = importSettings.Triggers;

            using (Disposable.Create(() =>
            {
                PostProcessImportAsset.ProcessingFile = null;
                PostProcessImportAsset.Asset = null;
                PostProcessImportAsset.UserData = null;
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
                var (gameObject, hash) = AkyuiGenerator.GenerateGameObject(new EditorAssetLoader(pathGetter, logger, settings.Triggers), layoutInfo, triggers);
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
        public static Dictionary<string, object> UserData { get; set; }
        public static IAkyuiImportTrigger[] Triggers { get; set; }

        public void OnPreprocessAsset()
        {
            if (ProcessingFile != assetPath) return;

            var userData = UserData;

            if (assetImporter is TextureImporter textureImporter)
            {
                textureImporter.textureType = TextureImporterType.Sprite;

                if (Asset is SpriteAsset spriteAsset)
                {
                    if (spriteAsset.Border != null) textureImporter.spriteBorder = spriteAsset.Border.ToVector4();
                    textureImporter.maxTextureSize = Mathf.RoundToInt(Mathf.Max(spriteAsset.Size.x, spriteAsset.Size.y) * Importer.Settings.SpriteSaveScale);
                }
            }

            foreach (var trigger in Triggers) trigger.OnUnityPreprocessAsset(assetImporter, Asset, ref userData);
            assetImporter.userData = MiniJSON.Json.Serialize(userData);
        }
    }

    public class EditorAssetLoader : IAssetLoader
    {
        private readonly PathGetter _pathGetter;
        private readonly AkyuiLogger _logger;
        private readonly IAkyuiImportTrigger[] _triggers;

        public EditorAssetLoader(PathGetter pathGetter, AkyuiLogger logger, IAkyuiImportTrigger[] triggers)
        {
            _pathGetter = pathGetter;
            _logger = logger;
            _triggers = triggers;
        }

        private string ConvertName(string fileName)
        {
            foreach (var trigger in _triggers)
            {
                var newName = trigger.OnLoadAsset(fileName);
                if (!string.IsNullOrWhiteSpace(newName)) fileName = newName;
            }
            return fileName;
        }

        public Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(_pathGetter.AssetOutputDirectoryPath, ConvertName(name)));
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

        public Dictionary<string, object> LoadMeta(string name)
        {
            var importer = AssetImporter.GetAtPath(Path.Combine(_pathGetter.AssetOutputDirectoryPath, ConvertName(name)));
            return MiniJSON.Json.Deserialize(importer.userData).JsonDictionary();
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