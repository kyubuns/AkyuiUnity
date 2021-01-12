using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Generator
{
    public static class AkyuiGenerator
    {
        public static (GameObject, GameObjectWithId[]) GenerateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo)
        {
            var meta = new List<GameObjectWithId>();
            var gameObject = CreateGameObject(assetLoader, layoutInfo, layoutInfo.Root, null, ref meta);
            return (gameObject, meta.ToArray());
        }

        private static GameObject CreateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo, int eid, Transform parent, ref List<GameObjectWithId> meta)
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
                    createdComponents.Add(CreateComponent(assetLoader, gameObject, component));
                }

                meta.Add(new GameObjectWithId
                {
                    eid = new[] { objectElement.Eid },
                    gameObject = gameObject,
                    idAndComponents = createdComponents.ToArray(),
                });

                foreach (var child in objectElement.Children)
                {
                    CreateGameObject(assetLoader, layoutInfo, child, gameObject.transform, ref meta);
                }

                return gameObject;
            }

            if (element is PrefabElement prefabElement)
            {
                var (prefabGameObject, referenceMeta) = assetLoader.LoadPrefab(parent, prefabElement.Reference);

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
                            SetOrCreateComponentValue(targetComponent.component, assetLoader, targetObject.gameObject, component);
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

                return prefabGameObject;
            }

            Debug.LogError($"Unknown element type {element}");
            return null;
        }

        private static ComponentWithId CreateComponent(IAssetLoader assetLoader, GameObject gameObject, IComponent component)
        {
            return new ComponentWithId { cid = component.Cid, component = SetOrCreateComponentValue(null, assetLoader, gameObject, component) };
        }

        private static Component SetOrCreateComponentValue([CanBeNull] Component target, IAssetLoader assetLoader, GameObject gameObject, IComponent component)
        {
            if (component is ImageComponent imageComponent)
            {
                var image = target == null ? gameObject.AddComponent<Image>() : (Image) target;
                if (imageComponent.Sprite != null) image.sprite = assetLoader.LoadSprite(imageComponent.Sprite);
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

            if (component is HorizontalLayoutComponent)
            {
                var horizontalLayoutGroup = target == null ? gameObject.AddComponent<HorizontalLayoutGroup>() : (HorizontalLayoutGroup) target;
                horizontalLayoutGroup.childForceExpandWidth = false;
                horizontalLayoutGroup.childForceExpandHeight = false;
                return horizontalLayoutGroup;
            }

            if (component is VerticalLayoutComponent)
            {
                var verticalLayoutGroup = target == null ? gameObject.AddComponent<VerticalLayoutGroup>() : (VerticalLayoutGroup) target;
                verticalLayoutGroup.childForceExpandWidth = false;
                verticalLayoutGroup.childForceExpandHeight = false;
                return verticalLayoutGroup;
            }

            Debug.LogError($"Unknown component type {component}");
            return null;
        }
    }
}