using System;
using System.Linq;
using AkyuiUnity.Loader.Internal;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public class DefaultGenerateTrigger : IAkyuiGenerateTrigger
    {
        public Component CreateComponent(GameObject gameObject, IComponent component, IAssetLoader assetLoader)
        {
            if (component is ImageComponent imageComponent) return CreateImage(gameObject, assetLoader, imageComponent);
            if (component is MaskComponent maskComponent) return CreateMask(gameObject, assetLoader, maskComponent);
            if (component is TextComponent textComponent) return CreateText(gameObject, assetLoader, textComponent);
            if (component is AlphaComponent alphaComponent) return CreateAlpha(gameObject, assetLoader, alphaComponent);
            if (component is ButtonComponent) return CreateButton(gameObject, assetLoader);
            if (component is ToggleComponent) return CreateToggle(gameObject, assetLoader);
            if (component is VerticalScrollbarComponent verticalScrollbarComponent) return CreateVerticalScrollbar(gameObject, assetLoader, verticalScrollbarComponent);
            if (component is HorizontalScrollbarComponent horizontalScrollbarComponent) return CreateHorizontalScrollbar(gameObject, assetLoader, horizontalScrollbarComponent);
            if (component is VerticalListComponent verticalListComponent) return CreateVerticalList(gameObject, assetLoader, verticalListComponent);
            if (component is HorizontalListComponent horizontalListComponent) return CreateHorizontalList(gameObject, assetLoader, horizontalListComponent);
            if (component is HorizontalLayoutComponent horizontalLayoutComponent) return CreateHorizontalLayout(gameObject, assetLoader, horizontalLayoutComponent);
            if (component is VerticalLayoutComponent verticalLayoutComponent) return CreateVerticalLayout(gameObject, assetLoader, verticalLayoutComponent);
            if (component is GridLayoutComponent gridLayoutComponent) return CreateGridLayout(gameObject, assetLoader, gridLayoutComponent);
            if (component is InputFieldComponent) return CreateInputField(gameObject, assetLoader);
            return null;
        }

        // TextMeshProTrigger.csと合わせる
        private static Component CreateInputField(GameObject gameObject, IAssetLoader assetLoader)
        {
            var inputField = gameObject.AddComponent<InputField>();
            inputField.transition = Selectable.Transition.None;

            var texts = gameObject.GetComponentsInDirectChildren<Text>();
            if (texts.Length > 0)
            {
                var text = texts[0];
                var originalText = text.text;
                inputField.text = string.Empty;
                text.text = string.Empty;
                inputField.textComponent = text;

                if (inputField.placeholder == null)
                {
                    var placeholder = Object.Instantiate(text.gameObject, text.transform, true);
                    var placeHolderText = placeholder.GetComponent<Text>();
                    inputField.placeholder = placeHolderText;
                    placeholder.name = "Placeholder";
                    placeHolderText.text = originalText;
                }
            }

            return inputField;
        }

        private static Component CreateGridLayout(GameObject gameObject, IAssetLoader assetLoader, GridLayoutComponent gridLayoutComponent)
        {
            var gridLayoutGroup = gameObject.AddComponent<GridLayoutGroup>();

            var spacing = Vector2.zero;
            if (gridLayoutComponent.SpacingX != null) spacing.x = gridLayoutComponent.SpacingX.Value;
            if (gridLayoutComponent.SpacingY != null) spacing.y = gridLayoutComponent.SpacingY.Value;
            gridLayoutGroup.spacing = spacing;

            var children = gameObject.GetDirectChildren();
            if (children.Length == 1)
            {
                var childRect = RectTransformUtility.CalculateRelativeRectTransformBounds(children[0].GetComponent<RectTransform>());

                var cellSize = Vector2.zero;
                cellSize.x = childRect.size.x;
                cellSize.y = childRect.size.y;
                gridLayoutGroup.cellSize = cellSize;
            }
            else
            {
                Debug.LogWarning($"need children.Length({children.Length}) == 1");
            }

            return gridLayoutGroup;
        }

        private static Component CreateVerticalLayout(GameObject gameObject, IAssetLoader assetLoader, VerticalLayoutComponent verticalLayoutComponent)
        {
            var verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            if (verticalLayoutComponent.Spacing != null) verticalLayoutGroup.spacing = verticalLayoutComponent.Spacing.Value;
            return verticalLayoutGroup;
        }

        private static Component CreateHorizontalLayout(GameObject gameObject, IAssetLoader assetLoader, HorizontalLayoutComponent horizontalLayoutComponent)
        {
            var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            if (horizontalLayoutComponent.Spacing != null) horizontalLayoutGroup.spacing = horizontalLayoutComponent.Spacing.Value;
            return horizontalLayoutGroup;
        }

        private static Component CreateVerticalList(GameObject gameObject, IAssetLoader assetLoader, VerticalListComponent verticalListComponent)
        {
            var scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(gameObject.transform);

            var contentRectTransform = content.AddComponent<RectTransform>();
            contentRectTransform.pivot = new Vector2(0.5f, 1f);
            contentRectTransform.sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;
            contentRectTransform.anchoredPosition = new Vector2(0f, contentRectTransform.sizeDelta.y / 2f);

            var image = content.AddComponent<Image>();
            image.color = Color.clear;

            var verticalLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            if (verticalListComponent.Spacing != null) verticalLayoutGroup.spacing = verticalListComponent.Spacing.Value;
            if (verticalListComponent.PaddingTop != null) verticalLayoutGroup.padding.top = Mathf.RoundToInt(verticalListComponent.PaddingTop.Value);
            if (verticalListComponent.PaddingBottom != null) verticalLayoutGroup.padding.bottom = Mathf.RoundToInt(verticalListComponent.PaddingBottom.Value);

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRectTransform;

            var scrollbars = gameObject.GetComponentsInDirectChildren<Scrollbar>();
            if (scrollbars.Length > 0)
            {
                var scrollbar = scrollbars[0];
                scrollRect.verticalScrollbar = scrollbar;
            }

            return scrollRect;
        }

        private static Component CreateHorizontalList(GameObject gameObject, IAssetLoader assetLoader, HorizontalListComponent horizontalListComponent)
        {
            var scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(gameObject.transform);

            var contentRectTransform = content.AddComponent<RectTransform>();
            contentRectTransform.pivot = new Vector2(0f, 0.5f);
            contentRectTransform.sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;
            contentRectTransform.anchoredPosition = new Vector2(contentRectTransform.sizeDelta.x / 2f, 0f);

            var image = content.AddComponent<Image>();
            image.color = Color.clear;

            var horizontalLayoutGroup = content.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            if (horizontalListComponent.Spacing != null) horizontalLayoutGroup.spacing = horizontalListComponent.Spacing.Value;
            if (horizontalListComponent.PaddingLeft != null) horizontalLayoutGroup.padding.left = Mathf.RoundToInt(horizontalListComponent.PaddingLeft.Value);
            if (horizontalListComponent.PaddingRight != null) horizontalLayoutGroup.padding.right = Mathf.RoundToInt(horizontalListComponent.PaddingRight.Value);

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = contentRectTransform;

            var scrollbars = gameObject.GetComponentsInDirectChildren<Scrollbar>();
            if (scrollbars.Length > 0)
            {
                var scrollbar = scrollbars[0];
                scrollRect.horizontalScrollbar = scrollbar;
            }

            return scrollRect;
        }

        private static Component CreateButton(GameObject gameObject, IAssetLoader assetLoader)
        {
            var button = gameObject.AddComponent<Button>();

            button.targetGraphic = gameObject
                .GetComponentsInChildren<Image>()
                .FirstOrDefault(x => x.color != Color.clear);

            if (button.targetGraphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                button.targetGraphic = image;
            }

            return button;
        }

        private static Component CreateToggle(GameObject gameObject, IAssetLoader assetLoader)
        {
            var toggle = gameObject.AddComponent<Toggle>();
            toggle.isOn = true;

            toggle.targetGraphic = gameObject
                .GetComponentsInChildren<Image>()
                .FirstOrDefault(x => x.color != Color.clear);

            toggle.graphic = gameObject
                .GetComponentsInChildren<Image>()
                .LastOrDefault(x => x.color != Color.clear);

            if (toggle.targetGraphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                toggle.targetGraphic = image;
            }

            return toggle;
        }

        private static Component CreateVerticalScrollbar(GameObject gameObject, IAssetLoader assetLoader, VerticalScrollbarComponent verticalScrollbarComponent)
        {
            var scrollbar = gameObject.AddComponent<Scrollbar>();
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            if (scrollbar.handleRect == null)
            {
                var handle = new GameObject("Handle");
                var handleRect = handle.AddComponent<RectTransform>();
                handleRect.SetParent(gameObject.transform);
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.anchoredPosition = Vector2.zero;
                handleRect.sizeDelta = Vector2.zero;
                scrollbar.handleRect = handleRect;

                handle.AddComponent<Image>();
            }

            if (verticalScrollbarComponent.Image != null)
            {
                var image = scrollbar.handleRect.GetComponent<Image>();
                UpdateImage(gameObject, image, verticalScrollbarComponent.Image, assetLoader);
            }

            return scrollbar;
        }

        private static Component CreateHorizontalScrollbar(GameObject gameObject, IAssetLoader assetLoader, HorizontalScrollbarComponent horizontalScrollbarComponent)
        {
            var scrollbar = gameObject.AddComponent<Scrollbar>();
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.RightToLeft;

            if (scrollbar.handleRect == null)
            {
                var handle = new GameObject("Handle");
                var handleRect = handle.AddComponent<RectTransform>();
                handleRect.SetParent(gameObject.transform);
                handleRect.anchorMin = Vector2.zero;
                handleRect.anchorMax = Vector2.one;
                handleRect.anchoredPosition = Vector2.zero;
                handleRect.sizeDelta = Vector2.zero;
                scrollbar.handleRect = handleRect;

                handle.AddComponent<Image>();
            }

            if (horizontalScrollbarComponent.Image != null)
            {
                var image = scrollbar.handleRect.GetComponent<Image>();
                UpdateImage(gameObject, image, horizontalScrollbarComponent.Image, assetLoader);
            }

            return scrollbar;
        }

        private static Component CreateAlpha(GameObject gameObject, IAssetLoader assetLoader, AlphaComponent alphaComponent)
        {
            var canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (alphaComponent.Alpha != null)
            {
                canvasGroup.alpha = alphaComponent.Alpha.Value;
            }

            return canvasGroup;
        }

        // TextMeshProTrigger.csと合わせる
        private static Component CreateText(GameObject gameObject, IAssetLoader assetLoader, TextComponent textComponent)
        {
            var text = gameObject.AddComponent<Text>();
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;

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
            }

            if (textComponent.Font != null)
            {
                text.font = assetLoader.LoadFont(textComponent.Font);
                if (text.font == null)
                {
                    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            if (textComponent.Wrap != null)
            {
                text.horizontalOverflow = textComponent.Wrap.Value ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            }

            return text;
        }

        private static Mask CreateMask(GameObject gameObject, IAssetLoader assetLoader, MaskComponent maskComponent)
        {
            var image = gameObject.AddComponent<Image>();
            image.raycastTarget = false;

            if (maskComponent.Sprite != null) UpdateImageSprite(gameObject, image, maskComponent.Sprite, assetLoader);

            var mask = gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            return mask;
        }

        private static Image CreateImage(GameObject gameObject, IAssetLoader assetLoader, ImageComponent imageComponent)
        {
            var image = gameObject.AddComponent<Image>();
            UpdateImage(gameObject, image, imageComponent, assetLoader);
            return image;
        }

        private static void UpdateImage(GameObject gameObject, Image image, ImageComponent imageComponent, IAssetLoader assetLoader)
        {
            var rectTransform = gameObject.GetComponent<RectTransform>();
            if (imageComponent.Sprite != null) UpdateImageSprite(gameObject, image, imageComponent.Sprite, assetLoader);
            if (imageComponent.Color != null) image.color = imageComponent.Color.Value;

            if (imageComponent.Direction != null)
            {
                rectTransform.localScale = new Vector3(imageComponent.Direction.Value.x, imageComponent.Direction.Value.y, 1f);
            }
        }

        private static void UpdateImageSprite(GameObject gameObject, Image image, string sprite, IAssetLoader assetLoader)
        {
            var rectTransform = gameObject.GetComponent<RectTransform>();
            image.sprite = assetLoader.LoadSprite(sprite);

            if (image.hasBorder)
            {
                var meta = assetLoader.LoadMeta(sprite);
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = JsonExtensions.ToFloat(meta["source_width"]) / rectTransform.rect.width;
            }
        }

        public void OnPostprocessComponent(GameObject gameObject, IComponent component)
        {
        }
    }
}