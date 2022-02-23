using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AkyuiUnity.Xd;
using JetBrains.Annotations;
using Unity.VectorGraphics;
using UnityEngine;
using XdParser.Internal;

namespace XdParser
{
    public static class SvgUtil
    {
        public static readonly string[] Types =
        {
            PathElement.Name,
            PolygonElement.Name,
            RectElement.Name,
            EllipseElement.Name,
            LineElement.Name,
            CircleElement.Name,
            CompoundElement.Name,
        };

        public static bool IsAlphaOnly(XdObjectJson xdObject)
        {
            if (xdObject.Group != null)
            {
                if (xdObject.Group.Children.Any(x => !IsAlphaOnly(x))) return false;
            }

            var stroke = xdObject.Style?.Stroke;
            if (stroke != null && stroke.Type != "none") return false;

            var fill = xdObject.Style?.Fill;
            if (fill != null && fill.Type != "none") return false;

            return true;
        }

        public static string CreateSvg(XdObjectJson xdObject, [CanBeNull] Obb obb, bool allowEmpty)
        {
            var defs = new List<IDefElement>();
            var body = CreateSvgLine(xdObject, defs);

            if (!allowEmpty)
            {
                if (!(body is GroupElement)
                    && ((body.Parameter.EnableFill == false || body.Parameter.Fill == null) &&
                        (body.Parameter.EnableStroke == false || body.Parameter.Stroke == null)))
                {
                    return string.Empty;
                }
            }

            body.Parameter.Transform = new Transform();
            if (obb != null)
            {
                var rootForCalc = new RootElement
                {
                    Defs = defs.ToArray(),
                    Body = body,
                    Size = null,
                };
                var bounds = CalcBounds(rootForCalc.ToSvg());
                if (bounds.width > 0.0001f && bounds.height > 0.0001f)
                {
                    body.Parameter.Transform.Value.Tx = -bounds.x;
                    body.Parameter.Transform.Value.Ty = -bounds.y;
                }
            }

            var root = new RootElement
            {
                Defs = defs.ToArray(),
                Body = body,
                Size = obb?.Size,
            };
            body.Parameter.Opacity = 1.0f;
            return root.ToSvg();
        }

        public static Rect CalcBounds(string svg)
        {
            using (var reader = new StringReader(svg))
            {
                var sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions.DontPreserve);
                var tessOptions = SvgToPng.TessellationOptions;
                var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
                var vertices = geometry.SelectMany(geom => geom.Vertices.Select(x => (geom.WorldTransform * x))).ToArray();
                var bounds = VectorUtils.Bounds(vertices);
                return bounds;
            }
        }

        private static IElement CreateSvgLine(XdObjectJson xdObject, List<IDefElement> defs)
        {
            var id = xdObject.GetSimpleName().Replace(" ", "_");
            var dataName = xdObject.GetSimpleName();
            var shape = xdObject.Shape;

            var parameter = new ElementParameter
            {
                Id = id
            };

            parameter.Transform = new Transform { Value = xdObject.Transform };

            var opacity = xdObject.Style?.Opacity;
            parameter.Opacity = opacity;

            if (xdObject.Group != null)
            {
                if (MaskGroupParser.Is(xdObject))
                {
                    var clipPathId = $"clip-path{defs.Count}";
                    parameter.ClipPath = $"url(#{clipPathId})";
                    var clipPathChildren = xdObject.Meta.Ux.ClipPathResources.Children.Select(x => CreateSvgLine(x, defs));
                    defs.Add(new ClipPathDefElement { Id = clipPathId, Children = clipPathChildren.ToArray() });
                }

                parameter.DataName = dataName;
                var blendMode = xdObject.Style?.BlendMode;
                var isolation = xdObject.Style?.Isolation;
                var children = xdObject.Group.Children.Select(x => CreateSvgLine(x, defs)).ToArray();
                return new GroupElement { Parameter = parameter, Children = children, BlendMode = blendMode, Isolation = isolation };
            }

            var fill = xdObject.Style?.Fill;
            parameter.EnableFill = true;
            if (fill != null && fill.Type != "none")
            {
                var color = xdObject.Style.Fill.ToUnityColor();
                parameter.Fill = color;

                if (fill.Type == "solid")
                {
                    // nothing to do
                }
                else if (fill.Type == "gradient")
                {
                    if (fill.Gradient.Meta.Ux.GradientResources.Type == "linear")
                    {
                        var fillId = $"linear-gradient{defs.Count}";
                        defs.Add(new LinearGradientDefElement
                        {
                            Id = fillId,
                            X1 = fill.Gradient.X1,
                            X2 = fill.Gradient.X2,
                            Y1 = fill.Gradient.Y1,
                            Y2 = fill.Gradient.Y2,
                            Units = fill.Gradient.Units,
                            Stops = fill.Gradient.Meta.Ux.GradientResources.Stops.Select(x => new GradientStop
                            {
                                Offset = x.Offset,
                                StopColor = x.Color.ToUnityColor(),
                            }).ToArray(),
                        });
                        parameter.FillUrl = fillId;
                    }
                    else if (fill.Gradient.Meta.Ux.GradientResources.Type == "radial")
                    {
                        var fillId = $"radial-gradient{defs.Count}";
                        defs.Add(new RadialGradientDefElement
                        {
                            Id = fillId,
                            Cx = fill.Gradient.Cx ?? 0.5f,
                            Cy = fill.Gradient.Cy ?? 0.5f,
                            Fx = fill.Gradient.Fx ?? 0.5f,
                            Fy = fill.Gradient.Fy ?? 0.5f,
                            R = fill.Gradient.R ?? 0.5f,
                            Units = fill.Gradient.Units,
                            GradientTransform = fill.Gradient.Transform,
                            Stops = fill.Gradient.Meta.Ux.GradientResources.Stops.Select(x => new GradientStop
                            {
                                Offset = x.Offset,
                                StopColor = x.Color.ToUnityColor(),
                            }).ToArray(),
                        });
                        parameter.FillUrl = fillId;
                    }
                    else
                    {
                        XdImporter.Logger.Warning($"Unknown fill gradient type {fill.Gradient.Meta.Ux.GradientResources.Type} in {xdObject.Name}");
                    }
                }
                else if (fill.Type == "pattern")
                {
                    parameter.Fill = Color.black;
                }
                else
                {
                    XdImporter.Logger.Warning($"Unknown fill type {fill.Type} in {xdObject.Name}");
                }

                if (!string.IsNullOrWhiteSpace(shape.Winding))
                {
                    parameter.FillRule = shape.Winding;
                }
            }

            float? shapeR = null;
            float[] corners = null;
            if (shape.R != null)
            {
                if (shape.R is List<object> list)
                {
                    var floatArray = list.Select(x => (float) (double) x).ToArray();
                    if (floatArray.All(x => Mathf.Approximately(floatArray[0], x))) shapeR = floatArray[0];
                    else corners = floatArray;
                }
                else if (shape.R is double d)
                {
                    shapeR = (float) d;
                }
                else
                {
                    throw new NotSupportedException($"Unknown shape.r type {shape.R.GetType()}");
                }
            }

            if (shapeR != null && shape.Type != CircleElement.Name)
            {
                parameter.Rx = shapeR;
                if (parameter.Rx > shape.Width / 2f) parameter.Rx = shape.Width / 2f;
                if (parameter.Rx > shape.Height / 2f) parameter.Rx = shape.Height / 2f;
            }

            var stroke = xdObject.Style?.Stroke;
            string strokeAlign = null;
            if (stroke != null && stroke.Type != "none")
            {
                parameter.EnableStroke = true;
                parameter.Stroke = stroke.ToUnityColor();
                parameter.StrokeWidth = stroke.Width;
                parameter.StrokeMiterLimit = stroke.MiterLimit;

                if (!string.IsNullOrWhiteSpace(stroke.Join))
                {
                    parameter.StrokeLinejoin = stroke.Join;
                }

                if (!string.IsNullOrWhiteSpace(stroke.Cap))
                {
                    parameter.StrokeLinecap = stroke.Cap;
                }

                if (stroke.Dash != null)
                {
                    parameter.StrokeDasharray = stroke.Dash;
                }

                // ReSharper disable once RedundantAssignment
                if (stroke.Align == null) strokeAlign = null;
                else if (stroke.Align == "outside") strokeAlign = "outside";
                else if (stroke.Align == "inside") strokeAlign = "inside";
                else throw new NotSupportedException($"{xdObject} has unknown align type {stroke.Align}");
            }

            if (shape.Type == PathElement.Name) return new PathElement { Parameter = parameter, D = shape.Path };

            if (shape.Type == CompoundElement.Name) return new CompoundElement { Parameter = parameter, D = shape.Path };

            if (shape.Type == LineElement.Name) return new LineElement { Parameter = parameter, X1 = shape.X1, Y1 = shape.Y1, X2 = shape.X2, Y2 = shape.Y2 };

            if (shape.Type == PolygonElement.Name)
            {
                if (strokeAlign == "outside") return PolygonElement.Outside(dataName, shape, parameter);
                if (strokeAlign == "inside") return PolygonElement.Inside(dataName, shape, parameter);
                return PolygonElement.Basic(dataName, shape, parameter);
            }

            if (shape.Type == RectElement.Name)
            {
                if (strokeAlign == "outside") return RectElement.Outside(shape, parameter, corners);
                if (strokeAlign == "inside") return RectElement.Inside(shape, parameter, corners);
                return RectElement.Basic(shape, parameter, corners);
            }

            if (shape.Type == CircleElement.Name)
            {
                if (strokeAlign == "outside") return CircleElement.Outside(shape, parameter, shapeR.Value);
                if (strokeAlign == "inside") return CircleElement.Inside(shape, parameter, shapeR.Value);
                return CircleElement.Basic(shape, parameter, shapeR.Value);
            }

            if (shape.Type == EllipseElement.Name)
            {
                if (strokeAlign == "outside") return EllipseElement.Outside(shape, parameter);
                if (strokeAlign == "inside") return EllipseElement.Inside(shape, parameter);
                return EllipseElement.Basic(shape, parameter);
            }

            throw new NotSupportedException($"Unknown type {shape.Type}");
        }

        private static double ToCeilingString(float f)
        {
            // R関連はwidth=10, r=4.999とかになったときに変な線が出るため切り上げる
            return Math.Ceiling(f * 1000.0) / 1000.0;
        }

        private interface IElement
        {
            ElementParameter Parameter { get; set; }
            string ToSvg();
        }

        private class ElementParameter
        {
            public string Id { get; set; }
            public string DataName { get; set; }
            public string ClipPath { get; set; }
            public float? X { get; set; }
            public float? Y { get; set; }
            public Transform Transform { get; set; } = new Transform();
            public bool EnableFill { get; set; }
            public Color? Fill { get; set; }
            public string FillUrl { get; set; } // ColorのFillより優先される
            public string FillRule { get; set; }
            public bool EnableStroke { get; set; }
            public Color? Stroke { get; set; }
            public float? StrokeWidth { get; set; }
            public float? StrokeMiterLimit { get; set; }
            public string StrokeLinejoin { get; set; }
            public string StrokeLinecap { get; set; }
            public float[] StrokeDasharray { get; set; }
            public float? Rx { get; set; }
            public float? Opacity { get; set; }

            public string GetString()
            {
                var parameters = new[]
                {
                    IdToSvg(),
                    DataNameToSvg(),
                    ClipPathToSvg(),
                    XToSvg(),
                    YToSvg(),
                    Transform.ToSvg(),
                    FillToSvg(),
                    FillRuleToSvg(),
                    StrokeToSvg(),
                    StrokeWidthToSvg(),
                    StrokeMiterLimitToSvg(),
                    StrokeLinejoinToSvg(),
                    StrokeLinecapToSvg(),
                    StrokeDasharrayToSvg(),
                    RxToSvg(),
                    OpacityToSvg(),
                }.Where(x => !string.IsNullOrWhiteSpace(x));
                return string.Join(" ", parameters);
            }

            private string IdToSvg()
            {
                if (string.IsNullOrWhiteSpace(Id)) return null;
                return $@"id=""{Id}""";
            }

            private string DataNameToSvg()
            {
                if (string.IsNullOrWhiteSpace(DataName)) return null;
                return $@"data-name=""{DataName}""";
            }

            private string ClipPathToSvg()
            {
                if (string.IsNullOrWhiteSpace(ClipPath)) return null;
                return $@"clip-path=""{ClipPath}""";
            }

            private string XToSvg()
            {
                if (X == null) return null;
                return $@"x=""{X:0.###}""";
            }

            private string YToSvg()
            {
                if (Y == null) return null;
                return $@"y=""{Y:0.###}""";
            }

            private string FillToSvg()
            {
                if (FillUrl != null) return $@"fill=""url(#{FillUrl})""";
                if (!EnableFill) return null;
                if (Fill == null) return @"fill=""none""";
                return $@"fill=""{Fill.Value.ToSvgColorString()}""";
            }

            private string FillRuleToSvg()
            {
                if (FillUrl != null) return null;
                if (!EnableFill) return null;
                if (string.IsNullOrWhiteSpace(FillRule)) return null;
                return $@"fill-rule=""{FillRule}""";
            }

            private string StrokeToSvg()
            {
                if (!EnableStroke) return null;
                if (Stroke == null) return @"stroke=""none""";
                return $@"stroke=""{Stroke.Value.ToSvgColorString()}""";
            }

            private string StrokeWidthToSvg()
            {
                if (!EnableStroke) return null;
                if (StrokeWidth == null) return null;
                return $@"stroke-width=""{StrokeWidth.Value:0.###}""";
            }

            private string StrokeMiterLimitToSvg()
            {
                if (!EnableStroke) return null;
                if (StrokeMiterLimit == null) return null;
                return $@"stroke-miterlimit=""{StrokeMiterLimit.Value:0.###}""";
            }

            private string StrokeLinejoinToSvg()
            {
                if (!EnableStroke) return null;
                if (string.IsNullOrWhiteSpace(StrokeLinejoin)) return null;
                return $@"stroke-linejoin=""{StrokeLinejoin}""";
            }

            private string StrokeLinecapToSvg()
            {
                if (!EnableStroke) return null;
                if (string.IsNullOrWhiteSpace(StrokeLinecap)) return null;
                return $@"stroke-linecap=""{StrokeLinecap}""";
            }

            private string StrokeDasharrayToSvg()
            {
                if (!EnableStroke) return null;
                if (StrokeDasharray == null) return null;
                return $@"stroke-dasharray=""{string.Join(" ", StrokeDasharray.Select(x => $"{x:0.###}"))}""";
            }

            private string RxToSvg()
            {
                if (Rx == null) return null;
                return $@"rx=""{ToCeilingString(Rx.Value):0.###}""";
            }

            private string OpacityToSvg()
            {
                if (Opacity == null) return null;
                if (Math.Abs(Opacity.Value - 1.0f) < 0.0001f) return null;
                return $@"opacity=""{Opacity:0.###}""";
            }
        }

        private class Transform
        {
            public XdTransformJson Value { get; set; } = new XdTransformJson
            {
                A = 1f,
                B = 0f,
                C = 0f,
                D = 1f,
                Tx = 0f,
                Ty = 0f,
            };

            public string ToSvg()
            {
                if (Value == null) return null;
                return $@"transform=""{ToSvg(Value)}""";
            }

            public static string ToSvg(XdTransformJson value)
            {
                if (
                    Mathf.Abs(value.A - 1f) < 0.0001f
                    && Mathf.Abs(value.B) < 0.0001f
                    && Mathf.Abs(value.C) < 0.0001f
                    && Mathf.Abs(value.D - 1f) < 0.0001f
                )
                {
                    if (Math.Abs(value.Tx) < 0.0001f && Math.Abs(value.Ty) < 0.0001f) return null;
                    return $@"translate({value.Tx:0.###} {value.Ty:0.###})";
                }

                if (
                    Mathf.Abs(value.B) < 0.0001f
                    && Mathf.Abs(value.C) < 0.0001f
                )
                {
                    if (Math.Abs(value.Tx) < 0.0001f && Math.Abs(value.Ty) < 0.0001f) return null;
                    return $@"translate({value.Tx:0.###} {value.Ty:0.###}) scale({value.A:0.###} {value.D:0.###})";
                }

                return $@"matrix({value.A:0.###}, {value.B:0.###}, {value.C:0.###}, {value.D:0.###}, {value.Tx:0.###}, {value.Ty:0.###})";
            }
        }

        private interface IDefElement
        {
            string ToSvg();
        }

        private class ClipPathDefElement : IDefElement
        {
            public string Id { get; set; }
            public IElement[] Children { get; set; }

            public string ToSvg()
            {
                var children = string.Join("", Children.Select(x => x.ToSvg()));
                return $@"<clipPath id=""{Id}"">{children}</clipPath>";
            }
        }

        private class LinearGradientDefElement : IDefElement
        {
            public string Id { get; set; }
            public float X1 { get; set; }
            public float Y1 { get; set; }
            public float X2 { get; set; }
            public float Y2 { get; set; }
            public string Units { get; set; }
            public GradientStop[] Stops { get; set; }

            public string ToSvg()
            {
                var stops = string.Join("", Stops.Select(x => x.ToSvg()));
                return $@"<linearGradient id=""{Id}"" x1=""{X1:0.###}"" x2=""{X2:0.###}"" y1=""{Y1:0.###}"" y2=""{Y2:0.###}"" gradientUnits=""{Units}"">{stops}</linearGradient>";
            }
        }

        private class RadialGradientDefElement : IDefElement
        {
            public string Id { get; set; }
            public float Cx { get; set; }
            public float Cy { get; set; }
            public float Fx { get; set; }
            public float Fy { get; set; }
            public float R { get; set; }
            public string Units { get; set; }
            public XdTransformJson GradientTransform { get; set; }
            public GradientStop[] Stops { get; set; }

            public string ToSvg()
            {
                var stops = string.Join("", Stops.Select(x => x.ToSvg()));
                var transformJson = "";
                if (GradientTransform != null)
                {
                    transformJson = $@"gradientTransform=""{Transform.ToSvg(GradientTransform)}""";
                }
                return $@"<radialGradient id=""{Id}"" cx=""{Cx:0.###}"" cy=""{Cy:0.###}"" fx=""{Fx:0.###}"" fy=""{Fy:0.###}"" r=""{ToCeilingString(R):0.###}"" {transformJson} gradientUnits=""{Units}"">{stops}</radialGradient>";
            }
        }

        private class GradientStop
        {
            public float Offset { get; set; }
            public Color StopColor { get; set; }

            public string ToSvg()
            {
                return $@"<stop offset=""{Offset:0.###}"" stop-color=""{StopColor.ToSvgColorStringRgb()}"" stop-opacity=""{StopColor.a:0.###}""/>";
            }
        }

        private class RootElement : IElement
        {
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public IDefElement[] Defs { get; set; } = { };
            public IElement Body { get; set; }
            public Vector2? Size { get; set; }

            public string ToSvg()
            {
                var defsString = "";
                if (Defs.Length > 0) defsString = $@"<defs>{string.Join("", Defs.Select(x => x.ToSvg()))}</defs>";

                var sizeString = "";
                if (Size != null) sizeString = $@"width=""{Size.Value.x:0.###}"" height=""{Size.Value.y:0.###}"" viewBox=""0 0 {Size.Value.x:0.###} {Size.Value.y:0.###}""";

                var svg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" {sizeString}>{defsString}{Body.ToSvg()}</svg>";
                return svg;
            }
        }

        private class GroupElement : IElement
        {
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public IElement[] Children { get; set; } = { };
            public string BlendMode { get; set; }
            public string Isolation { get; set; }

            public string ToSvg()
            {
                var children = string.Join("", Children.Select(x => x.ToSvg()));

                var style = new List<string>();
                if (!string.IsNullOrWhiteSpace(BlendMode)) style.Add($"mix-blend-mode: {BlendMode}");
                if (!string.IsNullOrWhiteSpace(Isolation)) style.Add($"isolation: {Isolation}");
                var styleString = style.Count == 0 ? "" : $@"style=""{string.Join("; ", style)}""";

                return $@"<g {styleString} {Parameter.GetString()}>{children}</g>";
            }
        }

        private class PathElement : IElement
        {
            public const string Name = "path";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public string D { get; set; } = "";

            public string ToSvg()
            {
                return $@"<{Name} d=""{D}"" {Parameter.GetString()} />";
            }

            public static string GenerateD(params ID[] ds)
            {
                var stringBuilder = new StringBuilder();
                foreach (var d in ds)
                {
                    stringBuilder.Append(d);
                }
                return stringBuilder.ToString();
            }

            public interface ID
            {
            }

            public interface IDOption
            {
            }

            public readonly struct M : ID
            {
                public float X { get; }
                public float Y { get; }

                public M(float x, float y)
                {
                    X = x;
                    Y = y;
                }

                public override string ToString() => $"M{X:0.###},{Y:0.###}";
            }

            public readonly struct L : ID
            {
                public float X { get; }
                public float Y { get; }

                public L(float x, float y)
                {
                    X = x;
                    Y = y;
                }

                public override string ToString() => $"L{X:0.###},{Y:0.###}";
            }

            public readonly struct H : ID
            {
                public float X { get; }
                public IDOption Option { get; }

                public H(float x, IDOption option = null)
                {
                    X = x;
                    Option = option;
                }

                public override string ToString() => $"H{X:0.###}{(Option == null ? "" : Option.ToString())}";
            }

            public readonly struct V : ID
            {
                public float Y { get; }
                public IDOption Option { get; }

                public V(float y, IDOption option = null)
                {
                    Y = y;
                    Option = option;
                }

                public override string ToString() => $"V{Y:0.###}{(Option == null ? "" : Option.ToString())}";
            }

            public readonly struct A : ID
            {
                public float Rx { get; }
                public float Ry { get; }
                public float Angle { get; }
                public bool LargeArc { get; }
                public bool Sweep { get; }
                public float Dx { get; }
                public float Dy { get; }

                public A(float rx, float ry, float angle, bool largeArc, bool sweep, float dx, float dy)
                {
                    Rx = rx;
                    Ry = ry;
                    Angle = angle;
                    LargeArc = largeArc;
                    Sweep = sweep;
                    Dx = dx;
                    Dy = dy;
                }

                public override string ToString() => $"A{Rx:0.###},{Ry:0.###},{Angle:0.###},{(LargeArc ? 1 : 0)},{(Sweep ? 1 : 0)},{Dx:0.###},{Dy:0.###}";
            }

            // ReSharper disable once InconsistentNaming
            public readonly struct a : IDOption
            {
                public float Rx { get; }
                public float Ry { get; }
                public float Angle { get; }
                public bool LargeArc { get; }
                public bool Sweep { get; }
                public float Dx { get; }
                public float Dy { get; }

                public a(float rx, float ry, float angle, bool largeArc, bool sweep, float dx, float dy)
                {
                    Rx = rx;
                    Ry = ry;
                    Angle = angle;
                    LargeArc = largeArc;
                    Sweep = sweep;
                    Dx = dx;
                    Dy = dy;
                }

                public override string ToString() => $"a{Rx:0.###},{Ry:0.###},{Angle:0.###},{(LargeArc ? 1 : 0)},{(Sweep ? 1 : 0)},{Dx:0.###},{Dy:0.###}";
            }

            public struct Z : ID
            {
                public override string ToString() => "Z";
            }
        }

        private class CompoundElement : IElement
        {
            public const string Name = "compound";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public string D { get; set; } = "";

            public string ToSvg()
            {
                return $@"<{PathElement.Name} d=""{D}"" {Parameter.GetString()} />";
            }
        }

        private static class PolygonElement
        {
            public const string Name = "polygon";

            public static IElement Basic(string name, XdShapeJson shape, ElementParameter parameter)
            {
                if (shape.UxdesignCornerRadius != null && !Mathf.Approximately(shape.UxdesignCornerRadius.Value, 0f))
                {
                    XdImporter.Logger.Warning($"CornerRadius of Polygon Object is not supported in {name} (CornerCount = {shape.UxdesignCornerCount}, CornerRadius = {shape.UxdesignCornerRadius})");
                }

                var dp = new List<PathElement.ID>();
                dp.Add(new PathElement.M(shape.Points[0].X, shape.Points[0].Y));
                foreach (var a in shape.Points.Skip(1))
                {
                    dp.Add(new PathElement.L(a.X, a.Y));
                }
                dp.Add(new PathElement.Z());

                return new PathElement
                {
                    D = PathElement.GenerateD(dp.ToArray()),
                    Parameter = parameter,
                };
            }

            public static IElement Outside(string name, XdShapeJson shape, ElementParameter parameter)
            {
                XdImporter.Logger.Warning($"Outside boarder of Polygon Object is not supported in {name}");
                parameter.Rx = null;
                return Basic(name, shape, parameter);
            }

            public static IElement Inside(string name, XdShapeJson shape, ElementParameter parameter)
            {
                XdImporter.Logger.Warning($"Inside boarder of Polygon Object is not supported in {name}");
                parameter.Rx = null;
                return Basic(name, shape, parameter);
            }
        }

        private class RectElement : IElement
        {
            public const string Name = "rect";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public float Width { get; set; }
            public float Height { get; set; }

            public string ToSvg()
            {
                return $@"<{Name} width=""{Width:0.###}"" height=""{Height:0.###}"" {Parameter.GetString()} />";
            }

            public static IElement Basic(XdShapeJson shape, ElementParameter parameter, float[] corners)
            {
                if (corners != null)
                {
                    return new PathElement { Parameter = parameter, D = PathElement.GenerateD(WithCornersPath(shape, corners, 0f, 0f)) };
                }

                return new RectElement { Parameter = parameter, Width = shape.Width, Height = shape.Height };
            }

            public static IElement Outside(XdShapeJson shape, ElementParameter parameter, float[] corners)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;

                if (corners != null)
                {
                    return new GroupElement
                    {
                        Parameter = parameter, Children = new IElement[]
                        {
                            new PathElement
                            {
                                Parameter = new ElementParameter
                                {
                                    EnableStroke = true,
                                },
                                D = PathElement.GenerateD(WithCornersPath(shape, corners, 0f, 0f))
                            },
                            new PathElement
                            {
                                Parameter = new ElementParameter
                                {
                                    EnableFill = true,
                                },
                                D = PathElement.GenerateD(WithCornersPath(shape, corners, strokeWidth / 2f, 0f))
                            },
                        }
                    };
                }

                var rx = parameter.Rx;
                parameter.Rx = null;
                return new GroupElement
                {
                    Parameter = parameter, Children = new IElement[]
                    {
                        new RectElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableStroke = true,
                                Rx = rx,
                            },
                            Width = shape.Width,
                            Height = shape.Height,
                        },
                        new RectElement
                        {
                            Parameter = new ElementParameter
                            {
                                X = -strokeWidth / 2f,
                                Y = -strokeWidth / 2f,
                                EnableFill = true,
                                Rx = rx + strokeWidth / 2f,
                            },
                            Width = shape.Width + strokeWidth,
                            Height = shape.Height + strokeWidth,
                        },
                    }
                };
            }

            public static IElement Inside(XdShapeJson shape, ElementParameter parameter, float[] corners)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;

                if (corners != null)
                {
                    return new GroupElement
                    {
                        Parameter = parameter, Children = new IElement[]
                        {
                            new PathElement
                            {
                                Parameter = new ElementParameter
                                {
                                    EnableStroke = true,
                                },
                                D = PathElement.GenerateD(WithCornersPath(shape, corners, 0f, 0f))
                            },
                            new PathElement
                            {
                                Parameter = new ElementParameter
                                {
                                    EnableFill = true,
                                },
                                D = PathElement.GenerateD(WithCornersPath(shape, corners, 0f, strokeWidth / 2f))
                            },
                        }
                    };
                }

                var rx = parameter.Rx;
                parameter.Rx = null;
                return new GroupElement
                {
                    Parameter = parameter, Children = new IElement[]
                    {
                        new RectElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableStroke = true,
                                Rx = rx,
                            },
                            Width = shape.Width,
                            Height = shape.Height,
                        },
                        new RectElement
                        {
                            Parameter = new ElementParameter
                            {
                                X = strokeWidth / 2f,
                                Y = strokeWidth / 2f,
                                EnableFill = true,
                                Rx = rx - strokeWidth / 2f,
                            },
                            Width = shape.Width - strokeWidth,
                            Height = shape.Height - strokeWidth,
                        },
                    }
                };
            }

            private static PathElement.ID[] WithCornersPath(XdShapeJson shape, float[] corners, float outer, float inner)
            {
                var dp = new List<PathElement.ID>();
                dp.Add(new PathElement.M(Mathf.Max(corners[0], inner * 2), -outer + inner));
                dp.Add(new PathElement.H(Mathf.Min(shape.Width - corners[1], shape.Width - inner * 2),
                    new PathElement.a(
                        Mathf.Max(corners[1] + outer - inner, inner),
                        Mathf.Max(corners[1] + outer - inner, inner),
                        0, false, true,
                        Mathf.Max(corners[1] + outer - inner, inner),
                        Mathf.Max(corners[1] + outer - inner, inner)
                    )));
                dp.Add(new PathElement.V(Mathf.Min(shape.Height - corners[2], shape.Height - inner * 2),
                    new PathElement.a(
                        Mathf.Max(corners[2] + outer - inner, inner),
                        Mathf.Max(corners[2] + outer - inner, inner),
                        0, false, true,
                        -Mathf.Max(corners[2] + outer - inner, inner),
                        Mathf.Max(corners[2] + outer - inner, inner)
                    )));
                if (Mathf.Approximately(corners[3], 0f))
                {
                    dp.Add(new PathElement.H(inner * 2,
                        new PathElement.a(
                            outer + inner,
                            outer + inner,
                            0, false, true,
                            -outer - inner,
                            -outer - inner
                        )));
                }
                else
                {
                    dp.Add(new PathElement.H(corners[3]));
                    dp.Add(new PathElement.A(
                        corners[3] + outer - inner,
                        corners[3] + outer - inner,
                        0, false, true,
                        -outer + inner,
                        shape.Height - corners[3] - outer
                    ));
                }
                dp.Add(new PathElement.V(Mathf.Max(corners[0], inner * 2)));
                dp.Add(new PathElement.A(
                    Mathf.Max(corners[0] + outer - inner, inner),
                    Mathf.Max(corners[0] + outer - inner, inner),
                    0, false, true,
                    Mathf.Max(corners[0], inner * 2),
                    -outer + inner
                ));
                dp.Add(new PathElement.Z());
                return dp.ToArray();
            }
        }

        private class CircleElement : IElement
        {
            public const string Name = "circle";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public float Cx { get; set; }
            public float Cy { get; set; }
            public float R { get; set; }

            public string ToSvg()
            {
                return $@"<{Name} cx=""{Cx:0.###}"" cy=""{Cy:0.###}"" r=""{ToCeilingString(R):0.###}"" {Parameter.GetString()} />";
            }

            public static IElement Basic(XdShapeJson shape, ElementParameter parameter, float shapeR)
            {
                return new CircleElement { Parameter = parameter, Cx = shape.Cx, Cy = shape.Cy, R = shapeR };
            }

            public static IElement Outside(XdShapeJson shape, ElementParameter parameter, float shapeR)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;
                return new GroupElement
                {
                    Parameter = parameter, Children = new IElement[]
                    {
                        new CircleElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableStroke = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            R = shapeR,
                        },
                        new CircleElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableFill = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            R = shapeR + strokeWidth / 2f,
                        },
                    }
                };
            }

            public static IElement Inside(XdShapeJson shape, ElementParameter parameter, float shapeR)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;
                return new GroupElement
                {
                    Parameter = parameter, Children = new IElement[]
                    {
                        new CircleElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableStroke = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            R = shapeR,
                        },
                        new CircleElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableFill = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            R = shapeR - strokeWidth / 2f,
                        },
                    }
                };
            }
        }

        private class EllipseElement : IElement
        {
            public const string Name = "ellipse";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public float Cx { get; set; }
            public float Cy { get; set; }
            public float Rx { get; set; }
            public float Ry { get; set; }

            public string ToSvg()
            {
                return $@"<{Name} cx=""{Cx:0.###}"" cy=""{Cy:0.###}"" rx=""{ToCeilingString(Rx):0.###}"" ry=""{ToCeilingString(Ry):0.###}"" {Parameter.GetString()} />";
            }

            public static IElement Basic(XdShapeJson shape, ElementParameter parameter)
            {
                return new EllipseElement { Parameter = parameter, Cx = shape.Cx, Cy = shape.Cy, Rx = shape.Rx, Ry = shape.Ry };
            }

            public static IElement Outside(XdShapeJson shape, ElementParameter parameter)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;
                return new GroupElement
                {
                    Parameter = parameter, Children = new IElement[]
                    {
                        new EllipseElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableStroke = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            Rx = shape.Rx,
                            Ry = shape.Ry,
                        },
                        new EllipseElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableFill = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            Rx = shape.Rx + strokeWidth / 2f,
                            Ry = shape.Ry + strokeWidth / 2f,
                        },
                    }
                };
            }

            public static IElement Inside(XdShapeJson shape, ElementParameter parameter)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;
                return new GroupElement
                {
                    Parameter = parameter, Children = new IElement[]
                    {
                        new EllipseElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableStroke = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            Rx = shape.Rx,
                            Ry = shape.Ry,
                        },
                        new EllipseElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableFill = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            Rx = shape.Rx - strokeWidth / 2f,
                            Ry = shape.Ry - strokeWidth / 2f,
                        },
                    }
                };
            }
        }

        private class LineElement : IElement
        {
            public const string Name = "line";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public float X1 { get; set; }
            public float Y1 { get; set; }
            public float X2 { get; set; }
            public float Y2 { get; set; }

            public string ToSvg()
            {
                return $@"<{Name} x1=""{X1:0.###}"" y1=""{Y1:0.###}"" x2=""{X2:0.###}"" y2=""{Y2:0.###}"" {Parameter.GetString()} />";
            }
        }
    }
}
