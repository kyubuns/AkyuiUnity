using System;
using System.Collections.Generic;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public static class SvgUtil
    {
        public static readonly string[] Types = { "path", "rect", "ellipse", "line" };

        public static string CreateSvg(XdObjectJson xdObject)
        {
            var svgArgs = new List<string>();
            var shape = xdObject.Shape;

            var fill = xdObject.Style?.Fill;
            if (fill != null)
            {
                if (fill.Color?.Value?.R != null)
                {
                    var color = new Color32((byte) fill.Color.Value.R, (byte) fill.Color.Value.G, (byte) fill.Color.Value.B, 255);
                    svgArgs.Add($@"fill=""#{ColorUtility.ToHtmlStringRGB(color)}""");
                }
            }
            else
            {
                svgArgs.Add(@"fill=""none""");
            }

            var stroke = xdObject.Style?.Stroke;
            if (stroke != null)
            {
                var color = new Color32((byte) stroke.Color.Value.R, (byte) stroke.Color.Value.G, (byte) stroke.Color.Value.B, 255);
                svgArgs.Add($@"stroke=""#{ColorUtility.ToHtmlStringRGB(color)}""");
                svgArgs.Add($@"stroke-width=""{stroke.Width}""");
            }

            if (!string.IsNullOrWhiteSpace(shape.Winding))
            {
                svgArgs.Add($@"fill-rule=""{shape.Winding}""");
            }

            string body = null;
            if (shape.Type == "path")
            {
                body = $@"path d=""{shape.Path}""";
            }

            if (shape.Type == "rect")
            {
                body = $@"rect width=""{shape.Width}"" height=""{shape.Height}""";
            }

            if (shape.Type == "ellipse")
            {
                body = $@"ellipse cx=""{shape.Cx}"" cy=""{shape.Cy}"" rx=""{shape.Rx}"" ry=""{shape.Ry}""";
            }

            if (shape.Type == "line")
            {
                body = $@"line x1=""{shape.X1}"" y1=""{shape.Y1}"" x2=""{shape.X2}"" y2=""{shape.Y2}""";
            }

            if (body == null) throw new NotSupportedException($"Unknown type {shape.Type}");
            return $@"<svg><{body} {string.Join(" ", svgArgs)} /></svg>";
        }
    }
}
