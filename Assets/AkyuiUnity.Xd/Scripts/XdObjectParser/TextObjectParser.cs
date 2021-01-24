using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XdParser.Internal;
using Object = UnityEngine.Object;

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
            if (xdObject.Text?.Frame?.Type == "area")
            {
                return CalcSizeFromFrame(xdObject, position);
            }

            if (xdObject.Text?.Frame?.Type == "positioned")
            {
                return CalcSizeFromText(xdObject, position);
            }

            if (xdObject.Text?.Frame?.Type == "autoHeight")
            {
                return CalcSizeAutoHeight(xdObject, position);
            }

            throw new NotSupportedException($"Unknown Text Type {xdObject.Text?.Frame?.Type}");
        }

        public static Rect CalcSizeFromFrame(XdObjectJson xdObject, Vector2 position)
        {
            var size = new Vector2(xdObject.Text.Frame.Width, xdObject.Text.Frame.Height);
            return new Rect(position, size);
        }

        public static Rect CalcSizeAutoHeight(XdObjectJson xdObject, Vector2 position)
        {
            var calcSizeFromText = CalcSizeFromText(xdObject, position);
            var size = new Vector2(xdObject.Text.Frame.Width, calcSizeFromText.height);
            return new Rect(position, size);
        }

        public static Rect CalcSizeFromText(XdObjectJson xdObject, Vector2 position)
        {
            var font = xdObject.Style.Font;
            var fontSize = font.Size;
            var rawText = xdObject.Text.RawText;

            var findFont = AssetDatabase.FindAssets($"{font.PostscriptName} t:Font")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Object>(path))
                .OfType<Font>()
                .ToArray();
            var fontAsset = findFont.FirstOrDefault();
            if (fontAsset == null)
            {
                Debug.LogWarning($"{font.PostscriptName} is not found in project");
                fontAsset = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
                updateBounds = true,
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
            textGenerator.Populate(rawText, settings);
            var preferredWidth = textGenerator.rectExtents.width;
            var preferredHeight = textGenerator.rectExtents.height;
            var width = preferredWidth * scale;
            var height = preferredHeight * scale;
            var size = new Vector2(width, height);

            var lineJson = xdObject.Text.Paragraphs[0].Lines[0][0];
            position.x += lineJson.X;
            position.y += lineJson.Y;

            return new Rect(position, size);
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder)
        {
            var components = new List<IComponent>();

            var font = xdObject.Style.Font;
            var fontSize = font.Size;
            var color = xdObject.GetFillUnityColor();
            var rawText = xdObject.Text.RawText;

            var textAlign = TextComponent.TextAlign.MiddleLeft;
            var wrap = false;
            var paragraphAlign = xdObject.Style?.TextAttributes?.ParagraphAlign ?? "left";

            if (xdObject.Text?.Frame?.Type == "positioned")
            {
                wrap = false;
                if (paragraphAlign == "left") textAlign = TextComponent.TextAlign.MiddleLeft;
                if (paragraphAlign == "center") textAlign = TextComponent.TextAlign.MiddleCenter;
                if (paragraphAlign == "right") textAlign = TextComponent.TextAlign.MiddleRight;
            }

            if (xdObject.Text?.Frame?.Type == "area" || xdObject.Text?.Frame?.Type == "autoHeight")
            {
                wrap = true;
                if (paragraphAlign == "left") textAlign = TextComponent.TextAlign.UpperLeft;
                if (paragraphAlign == "center") textAlign = TextComponent.TextAlign.UpperCenter;
                if (paragraphAlign == "right") textAlign = TextComponent.TextAlign.UpperRight;
            }

            components.Add(new TextComponent(0, rawText, fontSize, color, textAlign, font.PostscriptName, wrap));

            return (components.ToArray(), new IAsset[] { });
        }
    }
}