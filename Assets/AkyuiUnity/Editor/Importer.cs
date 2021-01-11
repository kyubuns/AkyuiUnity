using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AkyuiUnity.Editor
{
    public static class Importer
    {
        public static void Import(AkyuiImportSettings settings, string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                Debug.Log($"Import Start: {filePath}");

                using (ILoader loader = new AkyuiLoader(filePath))
                {
                    Import(settings, loader);
                }

                Debug.Log($"Import Finish: {filePath}");
            }

            AssetDatabase.Refresh();
        }

        public static void Import(AkyuiImportSettings settings, ILoader loader)
        {
            var pathGetter = new PathGetter(settings, loader.FileName);

            var layoutInfo = loader.LayoutInfo;
            if (settings.CheckTimestamp)
            {
                var prevMetaFullPath = pathGetter.GetMetaFullPath(loader.FileName);
                if (File.Exists(prevMetaFullPath))
                {
                    var prevMetaObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.GetMetaPath(loader.FileName));
                    var prevMeta = prevMetaObject.GetComponent<AkyuiMeta>();
                    if (prevMeta.timestamp == layoutInfo.Timestamp)
                    {
                        Debug.Log($"Skip (same timestamp)");
                        return;
                    }
                }
            }

            ImportAssets(settings, loader, pathGetter);
            var (gameObject, idAndGameObjects) = CreateGameObject(loader, pathGetter);
            foreach (var trigger in settings.Triggers) trigger.OnPostprocessPrefab(gameObject, idAndGameObjects);

            // meta
            var metaGameObject = new GameObject(loader.FileName);
            var meta = metaGameObject.AddComponent<AkyuiMeta>();
            gameObject.transform.SetParent(metaGameObject.transform);
            meta.timestamp = layoutInfo.Timestamp;
            meta.root = gameObject;
            meta.idAndGameObjects = idAndGameObjects;

            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, pathGetter.PrefabSavePath, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(metaGameObject, pathGetter.MetaSavePath);

            Object.DestroyImmediate(metaGameObject);
        }

        private static void ImportAssets(AkyuiImportSettings settings, ILoader loader, PathGetter pathGetter)
        {
            var unityAssetsParentPath = Path.GetDirectoryName(Application.dataPath) ?? "";

            var assetOutputDirectoryFullPath = Path.Combine(unityAssetsParentPath, pathGetter.AssetOutputDirectoryPath);
            if (!Directory.Exists(assetOutputDirectoryFullPath)) Directory.CreateDirectory(assetOutputDirectoryFullPath);

            foreach (var asset in loader.AssetsInfo.Assets)
            {
                var savePath = Path.Combine(pathGetter.AssetOutputDirectoryPath, asset.FileName);
                var saveFullPath = Path.Combine(unityAssetsParentPath, savePath);
                var bytes = loader.LoadAsset(asset.FileName);

                if (settings.CheckTimestamp)
                {
                    if (File.Exists(saveFullPath))
                    {
                        var import = AssetImporter.GetAtPath(savePath);
                        if (import.userData == loader.LayoutInfo.Timestamp.ToString())
                        {
                            Debug.Log($"Asset {asset.FileName} Skip (same timestamp)");
                            continue;
                        }
                    }
                }

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

        private static (GameObject, GameObjectWithId[]) CreateGameObject(ILoader loader, PathGetter pathGetter)
        {
            var meta = new List<GameObjectWithId>();
            var gameObject = CreateGameObject(pathGetter, loader.LayoutInfo, loader.LayoutInfo.Root, null, ref meta);
            return (gameObject, meta.ToArray());
        }

        private static GameObject CreateGameObject(PathGetter pathGetter, LayoutInfo layoutInfo, int eid, Transform parent, ref List<GameObjectWithId> meta)
        {
            var element = layoutInfo.Elements.Single(x => x.Eid == eid);

            if (element is ObjectElement objectElement)
            {
                var gameObject = new GameObject(objectElement.Name);
                gameObject.transform.SetParent(parent);

                var rectTransform = gameObject.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = objectElement.Position;
                rectTransform.sizeDelta = objectElement.Size;

                var anchorMin = rectTransform.anchorMin;
                var anchorMax = rectTransform.anchorMax;

                switch (objectElement.AnchorX)
                {
                    case AnchorXType.Left:
                        anchorMin.x = 0.0f;
                        anchorMax.x = 0.0f;
                        break;
                    case AnchorXType.Center:
                        anchorMin.x = 0.5f;
                        anchorMax.x = 0.5f;
                        break;
                    case AnchorXType.Right:
                        anchorMin.x = 1.0f;
                        anchorMax.x = 1.0f;
                        break;
                    case AnchorXType.Stretch:
                        anchorMin.x = 0.0f;
                        anchorMax.x = 1.0f;
                        break;
                }

                switch (objectElement.AnchorY)
                {
                    case AnchorYType.Top:
                        anchorMin.y = 1.0f;
                        anchorMax.y = 1.0f;
                        break;
                    case AnchorYType.Middle:
                        anchorMin.y = 0.5f;
                        anchorMax.y = 0.5f;
                        break;
                    case AnchorYType.Bottom:
                        anchorMin.y = 0.0f;
                        anchorMax.y = 0.0f;
                        break;
                    case AnchorYType.Stretch:
                        anchorMin.y = 0.0f;
                        anchorMax.y = 1.0f;
                        break;
                }

                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;

                var createdComponents = new List<ComponentWithId>();
                foreach (var component in objectElement.Components)
                {
                    createdComponents.Add(CreateComponent(pathGetter, gameObject, component));
                }

                meta.Add(new GameObjectWithId
                {
                    eid = new[] { objectElement.Eid },
                    gameObject = gameObject,
                    idAndComponents = createdComponents.ToArray(),
                });

                foreach (var child in objectElement.Children)
                {
                    CreateGameObject(pathGetter, layoutInfo, child, gameObject.transform, ref meta);
                }

                return gameObject;
            }

            if (element is PrefabElement prefabElement)
            {
                var metaGameObject = (GameObject) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.GetMetaPath(prefabElement.Reference)));
                PrefabUtility.UnpackPrefabInstance(metaGameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                var referenceMeta = metaGameObject.GetComponent<AkyuiMeta>();
                var prefabGameObject = referenceMeta.root.gameObject;
                prefabGameObject.transform.SetParent(parent);

                if (prefabElement.Timestamp != referenceMeta.timestamp)
                {
                    Debug.LogWarning($"Reference {prefabElement.Reference} timestamp mismatch {prefabElement.Timestamp} != {referenceMeta.timestamp}");
                }

                foreach (var @override in prefabElement.Overrides)
                {
                    var targetObject = referenceMeta.Find(@override.Eid);
                    var rectTransform = targetObject.gameObject.GetComponent<RectTransform>();

                    if (@override.Name != null) targetObject.gameObject.name = @override.Name;
                    if (@override.Position != null) rectTransform.anchoredPosition = @override.Position.Value;
                    if (@override.Size != null) rectTransform.sizeDelta = @override.Size.Value;


                    var anchorMin = rectTransform.anchorMin;
                    var anchorMax = rectTransform.anchorMax;

                    if (@override.AnchorX != null)
                    {
                        switch (@override.AnchorX.Value)
                        {
                            case AnchorXType.Left:
                                anchorMin.x = 0.0f;
                                anchorMax.x = 0.0f;
                                break;
                            case AnchorXType.Center:
                                anchorMin.x = 0.5f;
                                anchorMax.x = 0.5f;
                                break;
                            case AnchorXType.Right:
                                anchorMin.x = 1.0f;
                                anchorMax.x = 1.0f;
                                break;
                            case AnchorXType.Stretch:
                                anchorMin.x = 0.0f;
                                anchorMax.x = 1.0f;
                                break;
                        }
                    }

                    if (@override.AnchorY != null)
                    {
                        switch (@override.AnchorY.Value)
                        {
                            case AnchorYType.Top:
                                anchorMin.y = 1.0f;
                                anchorMax.y = 1.0f;
                                break;
                            case AnchorYType.Middle:
                                anchorMin.y = 0.5f;
                                anchorMax.y = 0.5f;
                                break;
                            case AnchorYType.Bottom:
                                anchorMin.y = 0.0f;
                                anchorMax.y = 0.0f;
                                break;
                            case AnchorYType.Stretch:
                                anchorMin.y = 0.0f;
                                anchorMax.y = 1.0f;
                                break;
                        }
                    }

                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;

                    if (@override.Components != null)
                    {
                        foreach (var component in @override.Components)
                        {
                            var targetComponent = targetObject.idAndComponents.Single(x => x.cid == component.Cid);
                            SetOrCreateComponentValue(targetComponent.component, pathGetter, targetObject.gameObject, component);
                        }
                    }
                }

                foreach (var idAndGameObject in referenceMeta.idAndGameObjects)
                {
                    meta.Add(new GameObjectWithId
                    {
                        eid = new[] { prefabElement.Eid }.Concat(idAndGameObject.eid).ToArray(),
                        gameObject = idAndGameObject.gameObject,
                        idAndComponents = idAndGameObject.idAndComponents
                    });
                }

                Object.DestroyImmediate(metaGameObject);
                return prefabGameObject;
            }

            Debug.LogError($"Unknown element type {element}");
            return null;
        }

        private static ComponentWithId CreateComponent(PathGetter pathGetter, GameObject gameObject, IComponent component)
        {
            return new ComponentWithId { cid = component.Cid, component = SetOrCreateComponentValue(null, pathGetter, gameObject, component) };
        }

        private static Component SetOrCreateComponentValue([CanBeNull] Component target, PathGetter pathGetter, GameObject gameObject, IComponent component)
        {
            if (component is ImageComponent imageComponent)
            {
                var image = target == null ? gameObject.AddComponent<Image>() : (Image) target;
                if (imageComponent.Sprite != null) image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(pathGetter.AssetOutputDirectoryPath, imageComponent.Sprite));
                if (imageComponent.Color != null) image.color = imageComponent.Color.Value;
                return image;
            }

            if (component is TextComponent textComponent)
            {
                var text = target == null ? gameObject.AddComponent<Text>() : (Text) target;
                if (textComponent.Text != null) text.text = textComponent.Text;
                if (textComponent.Size != null) text.fontSize = Mathf.RoundToInt(textComponent.Size.Value);
                if (textComponent.Color != null) text.color = textComponent.Color.Value;
                if (textComponent.Align != null)
                {
                    switch (textComponent.Align.Value)
                    {
                        case TextComponent.TextAlign.MiddleCenter:
                            text.alignment = TextAnchor.MiddleCenter;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return text;
            }

            if (component is ButtonComponent)
            {
                var button = target == null ? gameObject.AddComponent<Button>() : (Button) target;
                return button;
            }

            if (component is LayoutComponent layoutComponent)
            {
                var layoutGroup = target == null ? null : (LayoutGroup) target;
                if (layoutComponent.Direction == LayoutComponent.LayoutDirection.LeftToRight)
                {
                    var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
                    horizontalLayoutGroup.childForceExpandWidth = false;
                    horizontalLayoutGroup.childForceExpandHeight = false;
                    layoutGroup = horizontalLayoutGroup;
                }
                else if (layoutComponent.Direction == LayoutComponent.LayoutDirection.TopToBottom)
                {
                    var verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
                    verticalLayoutGroup.childForceExpandWidth = false;
                    verticalLayoutGroup.childForceExpandHeight = false;
                    layoutGroup = verticalLayoutGroup;
                }
                else
                {
                    Debug.LogWarning($"Unknown direction {layoutComponent.Direction}");
                }
                return layoutGroup;
            }

            Debug.LogError($"Unknown component type {component}");
            return null;
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

        private readonly AkyuiImportSettings _settings;

        public PathGetter(AkyuiImportSettings settings, string fileName)
        {
            _settings = settings;

            AssetOutputDirectoryPath = settings.AssetOutputDirectoryPath.Replace("{name}", fileName);
            PrefabSavePath = settings.PrefabOutputPath.Replace("{name}", fileName) + ".prefab";
            MetaSavePath = GetMetaPath(fileName);
        }
    }
}