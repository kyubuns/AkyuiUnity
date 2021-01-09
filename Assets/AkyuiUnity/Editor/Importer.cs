using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Editor.MiniJSON;
using AkyuiUnity.Editor.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;
using ICSharpCode.SharpZipLib.Zip;
using UnityEditor;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static void Import(string[] filePaths, AkyuiImportSettings settings)
        {
            foreach (var filePath in filePaths)
            {
                Debug.Log($"Import Start: {filePath}");
                using (var zipFile = new ZipFile(filePath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(zipFile.Name);
                    var layoutJson = GetJson(zipFile, Path.Combine(fileName, "layout.json"));
                    var assetsJson = GetJson(zipFile, Path.Combine(fileName, "assets.json"));

                    var metaTimestamp = layoutJson["timestamp"].JsonInt();
                    var pathGetter = new PathGetter(settings, fileName, metaTimestamp);

                    if (settings.CheckTimestamp)
                    {
                        var prevMetaFullPath = pathGetter.GetMetaFullPath(fileName);
                        if (File.Exists(prevMetaFullPath))
                        {
                            var prevMetaObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.GetMetaPath(fileName));
                            var prevMeta = prevMetaObject.GetComponent<AkyuiMeta>();
                            if (prevMeta.timestamp == pathGetter.Timestamp)
                            {
                                Debug.Log($"Skip (same timestamp)");
                                continue;
                            }
                        }
                    }

                    // assets
                    var assets = (List<object>) assetsJson["assets"];
                    ImportAssets(settings, zipFile, pathGetter, assets.Select(x => (Dictionary<string, object>) x).ToArray());

                    // layout
                    var elements = (List<object>) layoutJson["elements"];
                    var rootId = layoutJson["root"].JsonInt();
                    var (gameObject, idAndGameObjects) = CreateGameObject(pathGetter, elements.Select(x => (Dictionary<string, object>) x).ToArray(), rootId);
                    foreach (var trigger in settings.Triggers) trigger.OnPostprocessPrefab(gameObject);

                    // meta
                    var metaGameObject = new GameObject(fileName);
                    var meta = metaGameObject.AddComponent<AkyuiMeta>();
                    gameObject.transform.SetParent(metaGameObject.transform);
                    meta.timestamp = metaTimestamp;
                    meta.root = gameObject;
                    meta.idAndGameObjects = idAndGameObjects;

                    PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, pathGetter.PrefabSavePath, InteractionMode.AutomatedAction);
                    PrefabUtility.SaveAsPrefabAsset(metaGameObject, pathGetter.MetaSavePath);

                    Object.DestroyImmediate(gameObject);
                    Object.DestroyImmediate(metaGameObject);
                }
                AssetDatabase.Refresh();
                Debug.Log($"Import Finish: {filePath}");
            }
        }

        private static Dictionary<string, object> GetJson(ZipFile zipFile, string name)
        {
            var layoutJson = zipFile.FindEntry(name, true);

            var stream = zipFile.GetInputStream(layoutJson);
            using (var reader = new StreamReader(stream))
            {
                var jsonString = reader.ReadToEnd();
                var json = (Dictionary<string, object>) Json.Deserialize(jsonString);
                return json;
            }
        }

        private static void ImportAssets(AkyuiImportSettings settings, ZipFile zipFile, PathGetter pathGetter, Dictionary<string, object>[] elements)
        {
            var fileName = Path.GetFileNameWithoutExtension(zipFile.Name);
            var assetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";

            var assetOutputDirectoryFullPath = Path.Combine(assetsParentPath, pathGetter.AssetOutputDirectoryPath);
            if (!Directory.Exists(assetOutputDirectoryFullPath)) Directory.CreateDirectory(assetOutputDirectoryFullPath);

            foreach (var element in elements)
            {
                var type = element["type"].JsonString();
                if (type == "sprite")
                {
                    var file = element["file"].JsonString();

                    var assetEntry = zipFile.FindEntry(Path.Combine(fileName, "assets", file), true);
                    var stream = zipFile.GetInputStream(assetEntry);
                    var savePath = Path.Combine(pathGetter.AssetOutputDirectoryPath, file);

                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        var bytes = memoryStream.ToArray();
                        var assetSaveFullPath = Path.Combine(assetsParentPath, savePath);

                        if (settings.CheckAssetBinary)
                        {
                            if (File.Exists(assetSaveFullPath))
                            {
                                var prevBytes = File.ReadAllBytes(assetSaveFullPath);
                                if (IsSame(prevBytes, bytes))
                                {
                                    Debug.Log($"{file} Skip (same bytes)");
                                    continue;
                                }
                            }
                        }

                        File.WriteAllBytes(assetSaveFullPath, bytes);
                    }

                    PostProcessImportAsset.ProcessingFile = savePath;
                    using (Disposable.Create(() => PostProcessImportAsset.ProcessingFile = ""))
                    {
                        AssetDatabase.ImportAsset(savePath);
                    }
                }
                else
                {
                    Debug.LogWarning($"Unknown type {type}");
                }
            }
        }

        private static bool IsSame(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static (GameObject, IdAndGameObject[]) CreateGameObject(PathGetter pathGetter, Dictionary<string, object>[] elements, int rootId)
        {
            var idToElement = new Dictionary<int, Dictionary<string, object>>();

            foreach (var element in elements)
            {
                var id = element["id"].JsonInt();
                idToElement[id] = element;
            }

            var meta = new List<IdAndGameObject>();
            var gameObject = CreateGameObject(pathGetter, idToElement, rootId, null, ref meta);
            return (gameObject, meta.ToArray());
        }

        private static GameObject CreateGameObject(PathGetter pathGetter, Dictionary<int, Dictionary<string, object>> idToElement, int id, Transform parent, ref List<IdAndGameObject> meta)
        {
            var element = idToElement[id];
            var type = element["type"].JsonString();

            if (type == "object")
            {
                var name = element["name"].JsonString();
                var position = element["position"].JsonVector2();
                var size = element["size"].JsonVector2();
                var anchorMin = element["anchor_min"].JsonVector2();
                var anchorMax = element["anchor_max"].JsonVector2();
                var pivot = element["pivot"].JsonVector2();
                var children = element["children"].JsonIntArray();
                var components = ((List<object>) element["components"]).Select(x => (Dictionary<string, object>) x).ToArray();

                var gameObject = new GameObject(name);
                gameObject.transform.SetParent(parent);

                var rectTransform = gameObject.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = size;
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.pivot = pivot;

                var createdComponents = new List<Component>();
                foreach (var component in components)
                {
                    createdComponents.Add(CreateComponent(pathGetter, gameObject, component));
                }

                meta.Add(new IdAndGameObject
                {
                    id = new[] { id },
                    gameObject = gameObject,
                    components = createdComponents.ToArray(),
                });

                foreach (var child in children)
                {
                    CreateGameObject(pathGetter, idToElement, child, gameObject.transform, ref meta);
                }

                return gameObject;
            }

            if (type == "prefab")
            {
                var reference = element["reference"].JsonString();
                var referenceTimestamp = element["timestamp"].JsonInt();
                if (referenceTimestamp != pathGetter.Timestamp)
                {
                    Debug.LogWarning($"Reference {reference} timestamp mismatch {referenceTimestamp} != {pathGetter.Timestamp}");
                }

                var metaGameObject = (GameObject) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.GetMetaPath(reference)));
                PrefabUtility.UnpackPrefabInstance(metaGameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                var referenceMeta = metaGameObject.GetComponent<AkyuiMeta>();
                var prefabGameObject = referenceMeta.root.gameObject;
                prefabGameObject.transform.SetParent(parent);

                var overrides = ((List<object>) element["overrides"]).Select(x => (Dictionary<string, object>) x).ToArray();
                foreach (var @override in overrides)
                {
                    var idList = @override["id"].JsonIntArray();
                    var target = referenceMeta.Find(idList);
                    var rectTransform = target.gameObject.GetComponent<RectTransform>();

                    if (@override.ContainsKey("name"))
                    {
                        var name = @override["name"].JsonString();
                        target.gameObject.name = name;
                    }
                    if (@override.ContainsKey("position"))
                    {
                        var position = @override["position"].JsonVector2();
                        rectTransform.anchoredPosition = position;
                    }
                    if (@override.ContainsKey("size"))
                    {
                        var size = @override["size"].JsonVector2();
                        rectTransform.sizeDelta = size;
                    }
                    if (@override.ContainsKey("anchor_min"))
                    {
                        var anchorMin = @override["anchor_min"].JsonVector2();
                        rectTransform.anchorMin = anchorMin;
                    }
                    if (@override.ContainsKey("anchor_max"))
                    {
                        var anchorMax = @override["anchor_max"].JsonVector2();
                        rectTransform.anchorMax = anchorMax;
                    }
                    if (@override.ContainsKey("pivot"))
                    {
                        var pivot = @override["pivot"].JsonVector2();
                        rectTransform.pivot = pivot;
                    }

                    if (@override.ContainsKey("components"))
                    {
                        var components = ((List<object>) @override["components"]).Select(x => (Dictionary<string, object>) x).ToArray();
                        foreach (var component in components)
                        {
                            var index = component["index"].JsonInt();
                            var componentType = component["type"].JsonString();

                            if (componentType == "text")
                            {
                                var targetText = (Text) target.components[index];
                                targetText.text = component["text"].JsonString();
                            }
                        }
                    }
                }

                foreach (var idAndGameObject in referenceMeta.idAndGameObjects)
                {
                    meta.Add(new IdAndGameObject
                    {
                        id = new[] { id }.Concat(idAndGameObject.id).ToArray(),
                        gameObject = idAndGameObject.gameObject,
                        components = idAndGameObject.components
                    });
                }

                Object.DestroyImmediate(metaGameObject);
                return prefabGameObject;
            }

            Debug.LogWarning($"Unknown type {type}");
            return null;
        }

        private static Component CreateComponent(PathGetter pathGetter, GameObject gameObject, Dictionary<string, object> component)
        {
            var type = component["type"].JsonString();

            if (type == "image")
            {
                var image = gameObject.AddComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(pathGetter.AssetOutputDirectoryPath, component["sprite"].JsonString()));
                image.color = component["color"].JsonColor();
                return image;
            }

            if (type == "text")
            {
                var text = gameObject.AddComponent<Text>();
                text.text = component["text"].JsonString();
                text.fontSize = component["size"].JsonInt();
                text.color = component["color"].JsonColor();
                switch (component["align"].JsonString())
                {
                    case "middle_center":
                        text.alignment = TextAnchor.MiddleCenter;
                        break;

                    default:
                        Debug.LogWarning($"Unknown align {component["align"].JsonString()}");
                        break;
                }

                return text;
            }

            if (type == "button")
            {
                var button = gameObject.AddComponent<Button>();
                return button;
            }

            Debug.LogWarning($"Unknown component {type}");
            return null;
        }
    }

    public class PostProcessImportAsset : AssetPostprocessor
    {
        public static string ProcessingFile { get; set; }

        public void OnPreprocessTexture()
        {
            if (ProcessingFile != assetPath) return;

            var textureImporter = (TextureImporter) assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
        }
    }

    public class PathGetter
    {
        public string AssetOutputDirectoryPath { get; }
        public string PrefabSavePath { get; }
        public string MetaSavePath { get; }
        public int Timestamp { get; }

        public string GetMetaPath(string fileName) => _settings.MetaOutputPath.Replace("{name}", fileName) + ".prefab";

        public string GetMetaFullPath(string fileName)
        {
            var assetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";
            return Path.Combine(assetsParentPath, GetMetaPath(fileName));
        }

        private readonly AkyuiImportSettings _settings;

        public PathGetter(AkyuiImportSettings settings, string fileName, int timestamp)
        {
            _settings = settings;

            AssetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);
            PrefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
            MetaSavePath = GetMetaPath(fileName);
            Timestamp = timestamp;
        }
    }
}