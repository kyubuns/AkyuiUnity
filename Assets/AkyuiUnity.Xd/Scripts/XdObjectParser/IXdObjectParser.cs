using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdObjectParser
    {
        bool Is(XdObjectJson xdObject);
        Rect CalcSize(XdObjectJson xdObject, Vector2 position);
        (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder);
    }

    public abstract class AkyuiXdObjectParser : ScriptableObject, IXdObjectParser
    {
        public abstract bool Is(XdObjectJson xdObject);
        public abstract Rect CalcSize(XdObjectJson xdObject, Vector2 position);
        public abstract (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder);
    }

    public static class XdObjectParserExtension
    {
        public static Color GetFillColor(this XdObjectJson xdObject)
        {
            var color = Color.white;
            if (xdObject.Style != null)
            {
                var fill = xdObject.Style.Fill;
                if (fill?.Color?.Value?.R != null)
                {
                    color = new Color32((byte) fill.Color.Value.R, (byte) fill.Color.Value.G, (byte) fill.Color.Value.B, 255);
                }
                color.a = xdObject.Style.Opacity ?? 1f;
            }
            return color;
        }
    }
}