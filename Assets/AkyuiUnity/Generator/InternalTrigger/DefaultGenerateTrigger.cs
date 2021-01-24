using System;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public class DefaultGenerateTrigger : IAkyuiGenerateTrigger
    {
        public Component SetOrCreateComponentValue(GameObject gameObject, TargetComponentGetter componentGetter, IComponent component, GameObject[] children, IAssetLoader assetLoader)
        {
            if (component is ImageComponent imageComponent) return CreateImage(componentGetter, assetLoader, imageComponent);
            if (component is TextComponent textComponent) return CreateText(componentGetter, assetLoader, textComponent);
            if (component is AlphaComponent alphaComponent) return CreateAlpha(componentGetter, alphaComponent);
            if (component is ButtonComponent) return CreateButton(gameObject, componentGetter);
            if (component is VerticalScrollbarComponent scrollbarComponent) return CreateScrollbar(gameObject, scrollbarComponent, componentGetter, assetLoader);
            if (component is VerticalListComponent verticalListComponent) return CreateVerticalList(gameObject, componentGetter, verticalListComponent);
            if (component is HorizontalLayoutComponent horizontalLayoutComponent) return CreateHorizontalLayout(componentGetter, horizontalLayoutComponent);
            if (component is VerticalLayoutComponent verticalLayoutComponent) return CreateVerticalLayout(componentGetter, verticalLayoutComponent);
            if (component is GridLayoutComponent gridLayoutComponent) return CreateGridLayout(componentGetter, children, gridLayoutComponent);
            return null;
        }

        private static Component CreateGridLayout(TargetComponentGetter componentGetter, GameObject[] children, GridLayoutComponent gridLayoutComponent)
        {
            var gridLayoutGroup = componentGetter.GetComponent<GridLayoutGroup>();

            var spacing = Vector2.zero;
            if (gridLayoutComponent.SpacingX != null) spacing.x = gridLayoutComponent.SpacingX.Value;
            if (gridLayoutComponent.SpacingY != null) spacing.y = gridLayoutComponent.SpacingY.Value;
            gridLayoutGroup.spacing = spacing;

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

        private static Component CreateVerticalLayout(TargetComponentGetter componentGetter, VerticalLayoutComponent verticalLayoutComponent)
        {
            var verticalLayoutGroup = componentGetter.GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            if (verticalLayoutComponent.Spacing != null) verticalLayoutGroup.spacing = verticalLayoutComponent.Spacing.Value;
            return verticalLayoutGroup;
        }

        private static Component CreateHorizontalLayout(TargetComponentGetter componentGetter, HorizontalLayoutComponent horizontalLayoutComponent)
        {
            var horizontalLayoutGroup = componentGetter.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            if (horizontalLayoutComponent.Spacing != null) horizontalLayoutGroup.spacing = horizontalLayoutComponent.Spacing.Value;
            return horizontalLayoutGroup;
        }

        private static Component CreateVerticalList(GameObject gameObject, TargetComponentGetter componentGetter,
            VerticalListComponent verticalListComponent)
        {
            var scrollRect = componentGetter.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            if (gameObject.GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }

            if (scrollRect.content == null)
            {
                var content = new GameObject("Content");
                content.transform.SetParent(gameObject.transform);

                var contentRectTransform = content.AddComponent<RectTransform>();
                contentRectTransform.anchoredPosition = Vector2.zero;
                contentRectTransform.pivot = new Vector2(0.5f, 1f);
                contentRectTransform.sizeDelta = gameObject.GetComponent<RectTransform>().sizeDelta;

                var image = content.AddComponent<Image>();
                image.color = Color.clear;

                var verticalLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
                verticalLayoutGroup.childForceExpandWidth = false;
                verticalLayoutGroup.childForceExpandHeight = false;
                if (verticalListComponent.Spacing != null) verticalLayoutGroup.spacing = verticalListComponent.Spacing.Value;
                if (verticalListComponent.PaddingTop != null) verticalLayoutGroup.padding.top = Mathf.RoundToInt(verticalListComponent.PaddingTop.Value);
                if (verticalListComponent.PaddingBottom != null)
                    verticalLayoutGroup.padding.bottom = Mathf.RoundToInt(verticalListComponent.PaddingBottom.Value);

                scrollRect.content = contentRectTransform;
            }

            var scrollbar = gameObject.GetComponentInChildren<Scrollbar>();
            if (scrollbar != null)
            {
                scrollRect.verticalScrollbar = scrollbar;
            }

            return scrollRect;
        }

        private static Component CreateButton(GameObject gameObject, TargetComponentGetter componentGetter)
        {
            var button = componentGetter.GetComponent<Button>();

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

        private static Component CreateScrollbar(GameObject gameObject, VerticalScrollbarComponent verticalScrollbarComponent, TargetComponentGetter componentGetter, IAssetLoader assetLoader)
        {
            var scrollbar = componentGetter.GetComponent<Scrollbar>();
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
                UpdateImage(image, verticalScrollbarComponent.Image, assetLoader);
            }

            return scrollbar;
        }

        private static Component CreateAlpha(TargetComponentGetter componentGetter, AlphaComponent alphaComponent)
        {
            var canvasGroup = componentGetter.GetComponent<CanvasGroup>();

            if (alphaComponent.Alpha != null)
            {
                canvasGroup.alpha = alphaComponent.Alpha.Value;
            }

            return canvasGroup;
        }

        // TextMeshProTrigger.csと合わせる
        private static Component CreateText(TargetComponentGetter componentGetter, IAssetLoader assetLoader, TextComponent textComponent)
        {
            var text = componentGetter.GetComponent<Text>();
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

        private static Image CreateImage(TargetComponentGetter componentGetter, IAssetLoader assetLoader, ImageComponent imageComponent)
        {
            var image = componentGetter.GetComponent<Image>();
            UpdateImage(image, imageComponent, assetLoader);
            return image;
        }

        private static void UpdateImage(Image image, ImageComponent imageComponent, IAssetLoader assetLoader)
        {
            if (imageComponent.Sprite != null) image.sprite = assetLoader.LoadSprite(imageComponent.Sprite);
            if (imageComponent.Color != null) image.color = imageComponent.Color.Value;
        }

        public void OnPostprocessComponent(GameObject gameObject, IComponent component)
        {
        }
    }
}