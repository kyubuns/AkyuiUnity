using System.Collections.Generic;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public static class SvgUtil
    {
        public static string CreateSvg(XdObjectJson xdObject)
        {
            var svgArgs = new List<string>();

            var fill = xdObject.Style?.Fill;
            if (fill != null)
            {
                var color = new Color32((byte) fill.Color.Value.R, (byte) fill.Color.Value.G, (byte) fill.Color.Value.B, 255);
                svgArgs.Add($@"fill=""#{ColorUtility.ToHtmlStringRGB(color)}""");
            }

            var stroke = xdObject.Style?.Stroke;
            if (stroke != null)
            {
                var color = new Color32((byte) stroke.Color.Value.R, (byte) stroke.Color.Value.G, (byte) stroke.Color.Value.B, 255);
                svgArgs.Add($@"stroke=""#{ColorUtility.ToHtmlStringRGB(color)}""");
                svgArgs.Add($@"stroke-width=""{stroke.Width}""");
            }

            if (!string.IsNullOrWhiteSpace(xdObject.Shape.Winding))
            {
                svgArgs.Add($@"fill-rule=""{xdObject.Shape.Winding}""");
            }
            
            var svg = $@"<svg><path d=""{xdObject.Shape.Path}"" {string.Join(" ", svgArgs)} /></svg>";
            return svg;
        }
    }
}
