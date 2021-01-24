using System.Linq;
using AkyuiUnity.Xd;
using TMPro;
using UnityEditor;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Sample.XdParser
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
                return CalcSizeFromText(xdObject, position);
            }

            var textParser = new TextObjectParser();
            return textParser.CalcSize(xdObject, position);
        }

        public static Rect CalcSizeFromText(XdObjectJson xdObject, Vector2 position)
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
            var textMeshPro = dummyObject.AddComponent<TextMeshProUGUI>();
            textMeshPro.font = fontAsset;
            textMeshPro.fontSize = fontSize;
            textMeshPro.text = rawText;
            var size = new Vector2(textMeshPro.preferredWidth, textMeshPro.preferredHeight);
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