using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class TextObjectParser : IXdObjectParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.Type == "text";
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position)
        {
            var font = xdObject.Style.Font;
            var fontSize = font.Size;
            var rawText = xdObject.Text.RawText;

            var findFont = AssetDatabase.FindAssets($"{font.PostscriptName}");
            var fontAsset = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (findFont.Length == 1)
            {
                var fontGuid = findFont.Single();
                var fontPath = AssetDatabase.GUIDToAssetPath(fontGuid);
                fontAsset = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            }

            var settings = new TextGenerationSettings
            {
                generationExtents = Vector2.zero,
                textAnchor = TextAnchor.MiddleCenter,
                alignByGeometry = false,
                scaleFactor = 1.0f,
                color = Color.white,
                font = fontAsset,
                pivot = Vector2.zero,
                richText = false,
                lineSpacing = 0,
                resizeTextForBestFit = false,
                updateBounds = false,
                horizontalOverflow = HorizontalWrapMode.Overflow,
                verticalOverflow = VerticalWrapMode.Overflow
            };

            var scale = 1.0f;
            if (fontAsset.dynamic)
            {
                settings.fontSize = Mathf.RoundToInt(fontSize);
                settings.fontStyle = FontStyle.Normal;
            }
            else
            {
                scale = fontSize / fontAsset.fontSize;
            }

            position.y -= fontAsset.ascent * (fontSize / fontAsset.fontSize);

            var textGenerator = new TextGenerator();
            var width = textGenerator.GetPreferredWidth(rawText, settings) * scale;
            var height = textGenerator.GetPreferredHeight(rawText, settings) * scale;

            var lineJson = xdObject.Text.Paragraphs[0].Lines[0][0];
            position.x += lineJson.X;
            position.y += lineJson.Y;

            var size = new Vector2(width, height);
            return new Rect(position, size);
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder)
        {
            var components = new List<IComponent>();

            var font = xdObject.Style.Font;
            var fontSize = font.Size;
            var color = xdObject.GetFillColor();
            var rawText = xdObject.Text.RawText;

            var textAlign = TextComponent.TextAlign.MiddleLeft;
            var paragraphAlign = xdObject.Style?.TextAttributes?.ParagraphAlign ?? "left";
            if (paragraphAlign == "left") textAlign = TextComponent.TextAlign.MiddleLeft;
            if (paragraphAlign == "center") textAlign = TextComponent.TextAlign.MiddleCenter;
            if (paragraphAlign == "right") textAlign = TextComponent.TextAlign.MiddleRight;
            components.Add(new TextComponent(0, rawText, fontSize, color, textAlign, font.PostscriptName));

            return (components.ToArray(), new IAsset[] { });
        }
    }
}