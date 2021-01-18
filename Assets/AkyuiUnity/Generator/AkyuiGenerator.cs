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
        public static (GameObject, AkyuiPrefabMeta) GenerateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo)
        {
            var metaList = new List<GameObjectWithId>();
            var gameObject = CreateGameObject(assetLoader, layoutInfo, layoutInfo.Root, null, ref metaList);
            var meta = new AkyuiPrefabMeta
            {
                hash = layoutInfo.Hash,
                root = gameObject,
                idAndGameObjects = metaList.ToArray()
            };
            return (gameObject, meta);
        }

        private static GameObject CreateGameObject(IAssetLoader assetLoader, LayoutInfo layoutInfo, int eid, Transform parent, ref List<GameObjectWithId> meta)
        {
            (Vector2 Min, Vector2 Max) CalcAnchor(AnchorXType x, AnchorYType y)
            {
                var anchorMin = Vector2.zero;
                var anchorMax = Vector2.zero;

                switch (x)
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

                switch (y)
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

                return (anchorMin, anchorMax);
            }

            var element = layoutInfo.Elements.Single(x => x.Eid == eid);

            if (element is ObjectElement objectElement)
            {
                var gameObject = new GameObject(objectElement.Name);
                gameObject.transform.SetParent(parent);

                var rectTransform = gameObject.AddComponent<RectTransform>();
                var (anchorMin, anchorMax) = CalcAnchor(objectElement.AnchorX, objectElement.AnchorY);
                rectTransform.anchoredPosition = objectElement.Position;
                var p = rectTransform.localPosition;
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.localPosition = p;
                rectTransform.SetSize(objectElement.Size);

                var createdComponents = new List<ComponentWithId>();
                foreach (var component in objectElement.Components)
                {
                    createdComponents.Add(CreateComponent(assetLoader, gameObject, component, ref rectTransform));
                }

                meta.Add(new GameObjectWithId
                {
                    eid = new[] { objectElement.Eid },
                    gameObject = gameObject,
                    idAndComponents = createdComponents.ToArray(),
                });

                foreach (var child in objectElement.Children)
                {
                    CreateGameObject(assetLoader, layoutInfo, child, rectTransform, ref meta);
                }

                return gameObject;
            }

            if (element is PrefabElement prefabElement)
            {
                var (prefabGameObject, referenceMeta) = assetLoader.LoadPrefab(parent, prefabElement.Reference);

                if (prefabElement.Hash != referenceMeta.hash)
                {
                    Debug.LogWarning($"Reference {prefabElement.Reference} hash mismatch {prefabElement.Hash} != {referenceMeta.hash}");
                }

                {
                    var rectTransform = prefabGameObject.GetComponent<RectTransform>();
                    var (anchorMin, anchorMax) = CalcAnchor(prefabElement.AnchorX, prefabElement.AnchorY);
                    rectTransform.anchoredPosition = prefabElement.Position;
                    var p = rectTransform.localPosition;
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.localPosition = p;
                    rectTransform.SetSize(prefabElement.Size);
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
                            var transform = targetObject.gameObject.GetComponent<RectTransform>();
                            SetOrCreateComponentValue(targetComponent.component, assetLoader, targetObject.gameObject, component, ref transform);
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

        private static ComponentWithId CreateComponent(IAssetLoader assetLoader, GameObject gameObject, IComponent component, ref RectTransform parentTransform)
        {
            return new ComponentWithId { cid = component.Cid, component = SetOrCreateComponentValue(null, assetLoader, gameObject, component, ref parentTransform) };
        }

        private static Component SetOrCreateComponentValue([CanBeNull] Component target, IAssetLoader assetLoader, GameObject gameObject, IComponent component, ref RectTransform parentTransform)
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
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;

                if (textComponent.Text != null) text.text = textComponent.Text;
                if (textComponent.Size != null) text.fontSize = Mathf.RoundToInt(textComponent.Size.Value);
                if (textComponent.Color != null) text.color = textComponent.Color.Value;
                if (textComponent.Align != null)
                {
                    switch (textComponent.Align.Value)
                    {
                        case TextComponent.TextAlign.UpperLeft:
                            text.alignment = TextAnchor.UpperLeft;
                            break;
                        case TextComponent.TextAlign.UpperCenter:
                            text.alignment = TextAnchor.UpperCenter;
                            break;
                        case TextComponent.TextAlign.UpperRight:
                            text.alignment = TextAnchor.UpperRight;
                            break;
                        case TextComponent.TextAlign.MiddleLeft:
                            text.alignment = TextAnchor.MiddleLeft;
                            break;
                        case TextComponent.TextAlign.MiddleCenter:
                            text.alignment = TextAnchor.MiddleCenter;
                            break;
                        case TextComponent.TextAlign.MiddleRight:
                            text.alignment = TextAnchor.MiddleRight;
                            break;
                        case TextComponent.TextAlign.LowerLeft:
                            text.alignment = TextAnchor.LowerLeft;
                            break;
                        case TextComponent.TextAlign.LowerCenter:
                            text.alignment = TextAnchor.LowerCenter;
                            break;
                        case TextComponent.TextAlign.LowerRight:
                            text.alignment = TextAnchor.LowerRight;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (textComponent.Font != null)
                    {
                        text.font = assetLoader.LoadFont(textComponent.Font);
                    }
                }
                return text;
            }

            if (component is ButtonComponent)
            {
                var button = target == null ? gameObject.AddComponent<Button>() : (Button) target;

                Graphic graphic;
                if (gameObject.GetComponent<Graphic>() == null)
                {
                    var image = gameObject.AddComponent<Image>();
                    image.color = Color.clear;
                    graphic = image;
                }
                else
                {
                    graphic = gameObject.GetComponent<Graphic>();
                }

                button.targetGraphic = graphic;
                return button;
            }

            if (component is VerticalListComponent)
            {
                var scrollRect = target == null ? gameObject.AddComponent<ScrollRect>() : (ScrollRect) target;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;

                if (gameObject.GetComponent<RectMask2D>() == null)
                {
                    gameObject.AddComponent<RectMask2D>();
                }

                if (target == null)
                {
                    var content = new GameObject("Content");
                    content.transform.SetParent(gameObject.transform);

                    var contentRectTransform = content.AddComponent<RectTransform>();
                    contentRectTransform.anchoredPosition = Vector2.zero;
                    contentRectTransform.pivot = new Vector2(0.5f, 1f);
                    contentRectTransform.sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;

                    scrollRect.content = contentRectTransform;
                    parentTransform = contentRectTransform;
                }

                return scrollRect;
            }

            if (component is HorizontalLayoutComponent horizontalLayoutComponent)
            {
                var horizontalLayoutGroup = target == null ? gameObject.AddComponent<HorizontalLayoutGroup>() : (HorizontalLayoutGroup) target;
                horizontalLayoutGroup.childForceExpandWidth = false;
                horizontalLayoutGroup.childForceExpandHeight = false;
                if (horizontalLayoutComponent.Spacing != null) horizontalLayoutGroup.spacing = horizontalLayoutComponent.Spacing.Value;
                return horizontalLayoutGroup;
            }

            if (component is VerticalLayoutComponent verticalLayoutComponent)
            {
                var verticalLayoutGroup = target == null ? gameObject.AddComponent<VerticalLayoutGroup>() : (VerticalLayoutGroup) target;
                verticalLayoutGroup.childForceExpandWidth = false;
                verticalLayoutGroup.childForceExpandHeight = false;
                if (verticalLayoutComponent.Spacing != null) verticalLayoutGroup.spacing = verticalLayoutComponent.Spacing.Value;
                return verticalLayoutGroup;
            }

            Debug.LogError($"Unknown component type {component}");
            return null;
        }
    }

    public static class AkyuiGeneratorExtensions
    {
        public static void SetSize(this RectTransform rectTransform, Vector2 size)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }
    }
}