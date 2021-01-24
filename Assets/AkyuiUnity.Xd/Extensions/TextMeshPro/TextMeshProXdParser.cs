#if AKYUIUNITY_XD_TEXTMESHPRO_SUPPORT
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XdParser.Internal;

namespace AkyuiUnity.Xd.TextMeshProExtension
{
    [CreateAssetMenu(menuName = "AkyuiXd/ObjectParsers/TextMeshProXdParser", fileName = nameof(TextMeshProXdParser))]
    public class TextMeshProXdParser : AkyuiXdObjectParser
    {
        public override bool Is(XdObjectJson xdObject)
        {
            var textParser = new TextObjectParser();
            return textParser.Is(xdObject);
        }

        public override Rect CalcSize(XdObjectJson xdObject, Vector2 position)
        {
            if (xdObject.Text?.Frame?.Type == "positioned")
            {
                return CalcSizeFromText(xdObject, position, null);
            }

            if (xdObject.Text?.Frame?.Type == "autoHeight")
            {
                return CalcSizeAutoHeight(xdObject, position);
            }

            var textParser = new TextObjectParser();
            return textParser.CalcSize(xdObject, position);
        }

        public static Rect CalcSizeAutoHeight(XdObjectJson xdObject, Vector2 position)
        {
            var calcSizeFromText = CalcSizeFromText(xdObject, position, xdObject.Text.Frame.Width);
            var size = new Vector2(xdObject.Text.Frame.Width, calcSizeFromText.height);
            return new Rect(position, size);
        }

        public static Rect CalcSizeFromText(XdObjectJson xdObject, Vector2 position, float? width)
        {
            var font = xdObject.Style.Font;
            var fontAsset = AssetDatabase.FindAssets($"{font.PostscriptName}")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Object>(path))
                .OfType<TMP_FontAsset>()
                .ToArray()
                .FirstOrDefault();

            if (fontAsset == null)
            {
                Debug.LogWarning($"TextMeshPro Asset {font.PostscriptName} is not found");
                var textParser = new TextObjectParser();
                return textParser.CalcSize(xdObject, position);
            }

            var fontSize = font.Size;
            var rawText = xdObject.Text.RawText;
            position.y -= fontAsset.faceInfo.ascentLine * (fontSize / fontAsset.faceInfo.pointSize);

            var dummyObject = new GameObject("Dummy");
            var dummyRectTransform = dummyObject.AddComponent<RectTransform>();
            dummyRectTransform.sizeDelta = new Vector2(width ?? 0f, 0f);

            var textMeshPro = dummyObject.AddComponent<TextMeshProUGUI>();
            textMeshPro.font = fontAsset;
            textMeshPro.fontSize = fontSize;
            textMeshPro.text = rawText;
            if (width != null) textMeshPro.enableWordWrapping = true;

            var size = new Vector2(LayoutUtility.GetPreferredSize(dummyRectTransform, 0), LayoutUtility.GetPreferredSize(dummyRectTransform, 1));
            DestroyImmediate(dummyObject);

            var lineJson = xdObject.Text.Paragraphs[0].Lines[0][0];
            position.x += lineJson.X;
            position.y += lineJson.Y;

            return new Rect(position, size);
        }

        public override (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder)
        {
            var textParser = new TextObjectParser();
            return textParser.Render(xdObject, size, assetHolder);
        }
    }
}
#endif
