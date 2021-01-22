using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public static class SvgUtil
    {
        public static readonly string[] Types = { "path", "rect", "ellipse", "line" };

        public static string CreateSvg(XdObjectJson xdObject)
        {
            var defs = new List<string>();
            var body = CreateSvgLine(xdObject, false, defs);

            var defsString = "";
            if (defs.Count > 0) defsString = $@"<defs>{string.Join("", defs)}</defs>";

            var svg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"">{defsString}{body}</svg>";
            return svg;
        }

        private static string CreateSvgLine(XdObjectJson xdObject, bool withTransform, List<string> defs)
        {
            var transform = $@"transform=""translate({xdObject.Transform.Tx:0.000} {xdObject.Transform.Ty:0.000})""";
            var id = xdObject.Name.Replace(" ", "_");
            var dataName = xdObject.Name;
            var data = $@"id=""{id}"" data-name=""{dataName}""";
            var svgArgs = new List<string>();

            if (withTransform)
            {
                svgArgs.Add(transform);
            }

            if (xdObject.Group != null)
            {
                if (xdObject.Meta?.Ux?.ClipPathResources?.Type == "clipPath")
                {
                    var clipPath = xdObject.Meta.Ux.ClipPathResources.Children[0];
                    svgArgs.Add($@"clip-path=""url(#clip-path)""");
                    defs.Add($@"<clipPath id=""clip-path""><path id=""_Clipping_Path_"" data-name=""Clipping Path"" d=""{clipPath.Shape.Path}"" transform=""translate({clipPath.Transform.Tx:0.000} {clipPath.Transform.Ty:0.000})"" fill=""{clipPath.Style.Stroke.Type}""/></clipPath>");
                }
                var children = string.Join("", xdObject.Group.Children.Select(x => CreateSvgLine(x, true, defs)));
                return $@"<g {data} {string.Join(" ", svgArgs)}>{children}</g>";
            }

            var shape = xdObject.Shape;

            var fill = xdObject.Style?.Fill;
            if (fill != null && fill.Type != "none")
            {
                var color = xdObject.GetFillColor();
                svgArgs.Add($@"fill=""#{ColorUtility.ToHtmlStringRGB(color)}""");
            }
            else
            {
                svgArgs.Add(@"fill=""none""");
            }

            var stroke = xdObject.Style?.Stroke;
            if (stroke != null && stroke.Type != "none")
            {
                var color = new Color32((byte) stroke.Color.Value.R, (byte) stroke.Color.Value.G, (byte) stroke.Color.Value.B, 255);
                svgArgs.Add($@"stroke=""#{ColorUtility.ToHtmlStringRGB(color)}""");
                svgArgs.Add($@"stroke-width=""{stroke.Width}""");

                if (!string.IsNullOrWhiteSpace(stroke.Join))
                {
                    svgArgs.Add($@"stroke-linejoin=""{stroke.Join}""");
                }

                if (!string.IsNullOrWhiteSpace(stroke.Cap))
                {
                    svgArgs.Add($@"stroke-linecap=""{stroke.Cap}""");
                }

                if (stroke.Dash != null)
                {
                    svgArgs.Add($@"stroke-dasharray=""{stroke.Dash[0]:0.000} {stroke.Dash[1]:0.000}""");
                }

                if (stroke.Type != "solid")
                {
                    Debug.LogWarning($"{xdObject} has unknown stroke type {stroke.Type}");
                }
            }

            if (!string.IsNullOrWhiteSpace(shape.Winding))
            {
                svgArgs.Add($@"fill-rule=""{shape.Winding}""");
            }

            string body = null;
            if (shape.Type == "path")
            {
                body = $@"path {data} d=""{shape.Path}""";
            }

            if (shape.Type == "rect")
            {
                body = $@"rect {data} width=""{shape.Width}"" height=""{shape.Height}""";
            }

            if (shape.Type == "ellipse")
            {
                body = $@"ellipse {data} cx=""{shape.Cx}"" cy=""{shape.Cy}"" rx=""{shape.Rx}"" ry=""{shape.Ry}""";
            }

            if (shape.Type == "line")
            {
                body = $@"line {data} x1=""{shape.X1}"" y1=""{shape.Y1}"" x2=""{shape.X2}"" y2=""{shape.Y2}""";
            }

            if (body == null) throw new NotSupportedException($"Unknown type {shape.Type}");

            return $@"<{body} {string.Join(" ", svgArgs)} />";
        }
    }
}
