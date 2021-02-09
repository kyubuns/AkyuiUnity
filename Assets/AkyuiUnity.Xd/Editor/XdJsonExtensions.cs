using System.Linq;
using AkyuiUnity.Editor;
using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public static class XdJsonExtensions
    {
        public static string GetSimpleName(this XdObjectJson xdObjectJson)
        {
            if (xdObjectJson.Name == null) return string.Empty;
            return AkyuiEditorUtil.ValidFileName(xdObjectJson.Name.Split('@')[0]);
        }

        public static bool NameEndsWith(this XdObjectJson xdObjectJson, string name)
        {
            return GetSimpleName(xdObjectJson).ToLowerInvariant().EndsWith(name.ToLowerInvariant());
        }

        public static bool HasParameter(this XdObjectJson xdObjectJson, string name)
        {
            return xdObjectJson.GetParameters().Contains(name.ToLowerInvariant());
        }

        private static string[] GetParameters(this XdObjectJson xdObjectJson)
        {
            if (xdObjectJson.Name == null) return new string[] { };

            var e = xdObjectJson.Name.Split('@');
            if (e.Length <= 1) return new string[] { };

            return e[1].Split(',').Select(x => x.ToLowerInvariant().Trim()).ToArray();
        }

        public static float GetRepeatGridSpacing(this XdObjectJson xdObjectJson, string scrollingType)
        {
            float spacing;

            if (scrollingType == "vertical")
            {
                spacing = xdObjectJson.Meta?.Ux?.RepeatGrid?.PaddingY ?? 0f;
            }
            else
            {
                spacing = xdObjectJson.Meta?.Ux?.RepeatGrid?.PaddingX ?? 0f;
            }

            return spacing;
        }

        public static void RemoveConstraint(this XdObjectJson xdObjectJson)
        {
            if (xdObjectJson?.Meta?.Ux != null)
            {
                xdObjectJson.Meta.Ux.ConstraintRight = false;
                xdObjectJson.Meta.Ux.ConstraintLeft = false;
                xdObjectJson.Meta.Ux.ConstraintTop = false;
                xdObjectJson.Meta.Ux.ConstraintBottom = false;
            }
        }

        public static string ToSvgColorString(this Color color)
        {
            var color32 = (Color32) color;
            return $"rgba({color32.r},{color32.g},{color32.b},{color.a})";
        }

        public static Color ToUnityColor(this XdStyleFillJson xdStyleFillJson)
        {
            var color = new Color32 { r = 255, g = 255, b = 255, a = 255 };
            if (xdStyleFillJson == null || xdStyleFillJson.Type == "none") return color;

            var xdColorJson = xdStyleFillJson.Color;
            if (xdColorJson?.Value == null) return color;

            color.r = (byte) xdColorJson.Value.R;
            color.g = (byte) xdColorJson.Value.G;
            color.b = (byte) xdColorJson.Value.B;
            color.a = xdColorJson.Alpha == null ? (byte) 255 : (byte) (255 * xdColorJson.Alpha);

            return color;
        }

        public static Color ToUnityColor(this XdStyleStrokeJson xdStyleStrokeJson)
        {
            var color = new Color32 { r = 255, g = 255, b = 255, a = 255 };
            if (xdStyleStrokeJson == null || xdStyleStrokeJson.Type == "none") return color;

            var xdColorJson = xdStyleStrokeJson.Color;
            if (xdColorJson?.Value == null) return color;

            color.r = (byte) xdColorJson.Value.R;
            color.g = (byte) xdColorJson.Value.G;
            color.b = (byte) xdColorJson.Value.B;
            color.a = xdColorJson.Alpha == null ? (byte) 255 : (byte) (255 * xdColorJson.Alpha);

            return color;
        }

        public static Color GetFillUnityColor(this XdObjectJson xdObjectJson)
        {
            var color = xdObjectJson.Style.Fill.ToUnityColor();
            color.a *= xdObjectJson.Style?.Opacity ?? 1f;
            return color;
        }

        public static Color GetFillUnityColor(this XdArtboardChildJson xdArtboardChildJson)
        {
            var color = xdArtboardChildJson.Style.Fill.ToUnityColor();
            color.a *= xdArtboardChildJson.Style?.Opacity ?? 1f;
            return color;
        }
    }
}