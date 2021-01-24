#if AKYUIUNITY_TEXTMESHPRO_SUPPORT
using System;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Generator;
using AkyuiUnity.Generator.InternalTrigger;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AkyuiUnity.Extensions.TextMeshPro
{
    // DefaultGenerateTrigger.csのText部分と合わせる
    [CreateAssetMenu(menuName = "Akyui/Triggers/SetupTextMeshPro", fileName = nameof(SetupTextMeshProTrigger))]
    public class SetupTextMeshProTrigger : AkyuiImportTrigger
    {
        [SerializeField] private string fontFilePath = "Assets/Fonts/{name} SDF";

        public override Component SetOrCreateComponentValue(GameObject gameObject, TargetComponentGetter componentGetter, IComponent component, GameObject[] children, IAssetLoader assetLoader)
        {
            if (component is InputFieldComponent) return CreateInputField(gameObject, componentGetter);
            if (component is TextComponent textComponent) return CreateText(componentGetter, textComponent);
            return null;
        }

        private Component CreateText(TargetComponentGetter componentGetter, TextComponent textComponent)
        {
            var text = componentGetter.GetComponent<TextMeshProUGUI>();
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;

            if (textComponent.Text != null) text.text = textComponent.Text;
            if (textComponent.Size != null) text.fontSize = Mathf.RoundToInt(textComponent.Size.Value);
            if (textComponent.Color != null) text.color = textComponent.Color.Value;
            if (textComponent.Align != null)
            {
                switch (textComponent.Align.Value)
                {
                    case TextComponent.TextAlign.UpperLeft:
                        text.alignment = TextAlignmentOptions.TopLeft;
                        break;
                    case TextComponent.TextAlign.UpperCenter:
                        text.alignment = TextAlignmentOptions.Top;
                        break;
                    case TextComponent.TextAlign.UpperRight:
                        text.alignment = TextAlignmentOptions.TopRight;
                        break;
                    case TextComponent.TextAlign.MiddleLeft:
                        text.alignment = TextAlignmentOptions.MidlineLeft;
                        break;
                    case TextComponent.TextAlign.MiddleCenter:
                        text.alignment = TextAlignmentOptions.Midline;
                        break;
                    case TextComponent.TextAlign.MiddleRight:
                        text.alignment = TextAlignmentOptions.MidlineRight;
                        break;
                    case TextComponent.TextAlign.LowerLeft:
                        text.alignment = TextAlignmentOptions.BottomLeft;
                        break;
                    case TextComponent.TextAlign.LowerCenter:
                        text.alignment = TextAlignmentOptions.Bottom;
                        break;
                    case TextComponent.TextAlign.LowerRight:
                        text.alignment = TextAlignmentOptions.BottomRight;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (textComponent.Font != null)
            {
                var fontPath = fontFilePath.Replace("{name}", textComponent.Font) + ".asset";
                var loadFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                if (loadFont != null)
                {
                    text.font = loadFont;
                }
                else
                {
                    Debug.LogWarning($"TextMeshPro Font {fontPath} is not found");
                }
            }

            if (textComponent.Wrap != null)
            {
                text.enableWordWrapping = true;
            }

            return text;
        }

        private static Component CreateInputField(GameObject gameObject, TargetComponentGetter componentGetter)
        {
            var inputField = componentGetter.GetComponent<TMP_InputField>();
            inputField.transition = Selectable.Transition.None;

            var texts = gameObject.GetComponentsInDirectChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                var text = texts[0];
                var originalText = text.text;
                inputField.text = string.Empty;
                text.text = Convert.ToChar(0x200b).ToString(); // ゼロ幅スペース、これにしないとPrefabに差分が出る
                inputField.textComponent = text;

                if (inputField.placeholder == null)
                {
                    var placeholder = Instantiate(text.gameObject, text.transform, true);
                    var placeHolderText = placeholder.GetComponent<TextMeshProUGUI>();
                    inputField.placeholder = placeHolderText;
                    placeholder.name = "Placeholder";
                    placeHolderText.text = originalText;
                }
            }

            return inputField;
        }
    }
}
#endif
