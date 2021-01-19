using System;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Generator.InternalTrigger
{
    public class DefaultGenerateTrigger : IAkyuiGenerateTrigger
    {
        public Component SetOrCreateComponentValue(Component target, IAssetLoader assetLoader, GameObject gameObject, IComponent component)
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

            if (component is VerticalListComponent verticalListComponent)
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

                    var image = content.AddComponent<Image>();
                    image.color = Color.clear;

                    var verticalLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
                    verticalLayoutGroup.childForceExpandWidth = false;
                    verticalLayoutGroup.childForceExpandHeight = false;
                    if (verticalListComponent.Spacing != null) verticalLayoutGroup.spacing = verticalListComponent.Spacing.Value;

                    scrollRect.content = contentRectTransform;
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

            return null;
        }
    }
}