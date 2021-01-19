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
            foreach (var loader in AkyuiDependencyResolver.Resolve(loaders))
            {
                Debug.Log($"Import Start: {loader.LayoutInfo.Name}");
                Import(settings, loader);
                Debug.Log($"Import Finish: {loader.LayoutInfo.Name}");
            }

            AssetDatabase.Refresh();
        }

        public static void Save(IAkyuiLoader loader, string filePath)
        {
            var bytes = AkyuiCompressor.Compress(loader);
            File.WriteAllBytes(filePath, bytes);
        }

        private static void Import(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader)
        {
            var pathGetter = new PathGetter(settings, akyuiLoader.LayoutInfo.Name);
            ImportAssets(settings, akyuiLoader, pathGetter);
            ImportLayout(settings, akyuiLoader, pathGetter);
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

                if (settings.CheckAssetHash)
                {
                    if (File.Exists(saveFullPath))
                    {
                        var import = AssetImporter.GetAtPath(savePath);
                        if (import.userData == t.Hash.ToString())
                        {
                            Debug.Log($"Asset {asset.FileName} / Skip (same hash)");
                            continue;
                        }
                    }
                }
                Debug.Log($"Asset {asset.FileName} / Import");

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
                PostProcessImportAsset.Hash = asset.Hash;
                PostProcessImportAsset.UserData = asset.UserData;
                using (Disposable.Create(() => PostProcessImportAsset.ProcessingFile = ""))
                {
                    AssetDatabase.ImportAsset(savePath);
                }
                return;
            }

            Debug.LogError($"Unknown asset type {asset}");
        }

        private static void ImportLayout(IAkyuiImportSettings settings, IAkyuiLoader akyuiLoader, PathGetter pathGetter)
        {
            var layoutInfo = akyuiLoader.LayoutInfo;
            Debug.Log($"Layout {layoutInfo.Name} / Import");

            var triggers = settings.Triggers.Select(x => (IAkyuiGenerateTrigger) x).ToArray();
            var (gameObject, meta) = AkyuiGenerator.GenerateGameObject(new EditorAssetLoader(pathGetter), layoutInfo, triggers);
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
    }

    public class PostProcessImportAsset : AssetPostprocessor
    {
        public static string ProcessingFile { get; set; }
        public static long Hash { get; set; }
        public static string UserData { get; set; }

        public void OnPreprocessAsset()
        {
            if (ProcessingFile != assetPath) return;

            assetImporter.userData = Hash.ToString();
        }

        public void OnPreprocessTexture()
        {
            if (ProcessingFile != assetPath) return;

            var textureImporter = (TextureImporter) assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
        }
    }

    public class EditorAssetLoader : IAssetLoader
    {
        private readonly PathGetter _pathGetter;

        public EditorAssetLoader(PathGetter pathGetter)
        {
            _pathGetter = pathGetter;
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

            AssetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);
            PrefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
            MetaSavePath = GetMetaPath(fileName);
            FontDirectoryPath = settings.FontDirectoryPath;
        }
    }
}