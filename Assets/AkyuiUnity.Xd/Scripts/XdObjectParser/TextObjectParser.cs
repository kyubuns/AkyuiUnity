using System.Collections.Generic;
using UnityEngine;
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
            var size = new Vector2(100f, 100f);
            return new Rect(position, size);
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder)
        {
            var components = new List<IComponent>();

            var font = xdObject.Style.Font;
            var fontSize = font.Size;
            var color = xdObject.GetFillColor();
            var rawText = xdObject.Text.RawText;
            components.Add(new TextComponent(0, rawText, fontSize, color, TextComponent.TextAlign.MiddleCenter));

            return (components.ToArray(), new IAsset[] { });
        }
    }
}