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

        public static Color GetFillUnityColor(this XdObjectJson xdObjectJson)
        {
            var colorJson = xdObjectJson.GetFillColor();
            Color color = new Color32((byte) colorJson.R, (byte) colorJson.G, (byte) colorJson.B, 255);
            color.a = xdObjectJson.Style?.Fill?.Color?.Alpha ?? 1f;
            color.a *= xdObjectJson.Style?.Opacity ?? 1f;
            return color;
        }

        public static Color GetFillUnityColor(this XdArtboardChildJson xdArtboardChildJson)
        {
            var colorJson = xdArtboardChildJson.Style.GetFillColor();
            Color color = new Color32((byte) colorJson.R, (byte) colorJson.G, (byte) colorJson.B, 255);
            color.a = xdArtboardChildJson.Style?.Fill?.Color?.Alpha ?? 1f;
            color.a *= xdArtboardChildJson.Style?.Opacity ?? 1f;
            return color;
        }
    }
}