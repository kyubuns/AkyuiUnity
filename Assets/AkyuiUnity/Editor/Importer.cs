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
                    var assetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);

                    // assets
                    var assetsJson = GetJson(zipFile, Path.Combine(fileName, "assets.json"));
                    var assets = (List<object>) assetsJson["assets"];
                    ImportAssets(zipFile, assetOutputDirectoryPath, assets.Select(x => (Dictionary<string, object>) x).ToArray());

                    // layout
                    var layoutJson = GetJson(zipFile, Path.Combine(fileName, "layout.json"));
                    var elements = (List<object>) layoutJson["elements"];
                    var rootId = layoutJson["root"].JsonInt();
                    var (gameObject, idAndGameObjects) = CreateGameObject(assetOutputDirectoryPath, elements.Select(x => (Dictionary<string, object>) x).ToArray(), rootId);

                    // meta
                    var metaGameObject = new GameObject(fileName);
                    var meta = metaGameObject.AddComponent<AkyuiMeta>();
                    gameObject.transform.SetParent(metaGameObject.transform);
                    meta.idAndGameObjects = idAndGameObjects;

                    var prefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
                    var metaSavePath = settings.MetaOutputPath.Replace("{name}", fileName) + ".prefab";
                    PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabSavePath, InteractionMode.AutomatedAction);
                    PrefabUtility.SaveAsPrefabAsset(metaGameObject, metaSavePath);

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

        private static void ImportAssets(ZipFile zipFile, string assetOutputDirectoryPath, Dictionary<string, object>[] elements)
        {
            var fileName = Path.GetFileNameWithoutExtension(zipFile.Name);
            var assetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";

            var assetOutputDirectoryFullPath = Path.Combine(assetsParentPath, assetOutputDirectoryPath);
            if (!Directory.Exists(assetOutputDirectoryFullPath)) Directory.CreateDirectory(assetOutputDirectoryFullPath);

            foreach (var element in elements)
            {
                var type = element["type"].JsonString();
                if (type == "sprite")
                {
                    var file = element["file"].JsonString();

                    var assetEntry = zipFile.FindEntry(Path.Combine(fileName, "assets", file), true);
                    var stream = zipFile.GetInputStream(assetEntry);
                    var savePath = Path.Combine(assetOutputDirectoryPath, file);

                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        var bytes = memoryStream.ToArray();
                        File.WriteAllBytes(Path.Combine(assetsParentPath, savePath), bytes);
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

        private static (GameObject, IdAndGameObject[]) CreateGameObject(string assetOutputDirectoryPath, Dictionary<string, object>[] elements, int rootId)
        {
            var idToElement = new Dictionary<int, Dictionary<string, object>>();

            foreach (var element in elements)
            {
                var id = element["id"].JsonInt();
                idToElement[id] = element;
            }

            var meta = new List<IdAndGameObject>();
            var gameObject = CreateGameObject(assetOutputDirectoryPath, idToElement, rootId, null, ref meta);
            return (gameObject, meta.ToArray());
        }

        private static GameObject CreateGameObject(string assetOutputDirectoryPath, Dictionary<int, Dictionary<string, object>> idToElement, int id, Transform parent, ref List<IdAndGameObject> meta)
        {
            var element = idToElement[id];
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
                createdComponents.Add(CreateComponent(assetOutputDirectoryPath, gameObject, component));
            }

            meta.Add(new IdAndGameObject
            {
                id = id,
                gameObject = gameObject,
                components = createdComponents.ToArray(),
            });

            foreach (var child in children)
            {
                CreateGameObject(assetOutputDirectoryPath, idToElement, child, gameObject.transform, ref meta);
            }

            return gameObject;
        }

        private static Component CreateComponent(string assetOutputDirectoryPath, GameObject gameObject, Dictionary<string, object> component)
        {
            var type = component["type"].JsonString();

            if (type == "image")
            {
                var image = gameObject.AddComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(assetOutputDirectoryPath, component["sprite"].JsonString()));
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
}