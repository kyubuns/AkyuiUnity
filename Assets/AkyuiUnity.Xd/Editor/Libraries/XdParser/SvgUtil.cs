using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static string CreateSvg(XdObjectJson xdObject, [CanBeNull] Obb obb)
        {
            var defs = new List<IDefElement>();
            var body = CreateSvgLine(xdObject, defs);

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
                    body.Parameter.Transform.X = -bounds.x;
                    body.Parameter.Transform.Y = -bounds.y;
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

            var tx = xdObject.Transform?.Tx ?? 0f;
            var ty = xdObject.Transform?.Ty ?? 0f;
            parameter.Transform.X = tx;
            parameter.Transform.Y = ty;

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

            XdStyleFillPatternJson image = null;
            var fill = xdObject.Style?.Fill;
            parameter.EnableFill = true;
            if (fill != null && fill.Type != "none")
            {
                var color = xdObject.GetFillColor();
                parameter.Fill = color;

                if (fill.Type == "solid")
                {
                    // nothing to do
                }
                else if (fill.Type == "pattern")
                {
                    image = fill.Pattern;
                    parameter.Fill = null;
                }
                else
                {
                    Debug.LogWarning($"Unknown fill type {fill.Type}");
                }

                if (!string.IsNullOrWhiteSpace(shape.Winding))
                {
                    parameter.FillRule = shape.Winding;
                }
            }

            float? shapeR = null;
            if (shape.R != null)
            {
                if (shape.R is List<object> list) shapeR = (float) (double) list[0];
                else if (shape.R is double d) shapeR = (float) d;
                else throw new NotSupportedException($"Unknown shape.r type {shape.R.GetType()}");
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
                parameter.Stroke = stroke.Color.Value;
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

                if (stroke.Align == null) strokeAlign = null;
                else if (stroke.Align == "outside") strokeAlign = "outside";
                else if (stroke.Align == "inside") strokeAlign = "inside";
                else throw new NotSupportedException($"{xdObject} has unknown align type {stroke.Align}");
            }

            if (image != null)
            {
                var imageBytes = XdImporter.XdFile.GetResource(fill.Pattern.Meta);
                return new ImageElement { Parameter = parameter, ImageBytes = imageBytes, Width = shape.Width, Height = shape.Height };
            }

            if (shape.Type == PathElement.Name) return new PathElement { Parameter = parameter, D = shape.Path };

            if (shape.Type == CompoundElement.Name) return new CompoundElement { Parameter = parameter, D = shape.Path };

            if (shape.Type == LineElement.Name) return new LineElement { Parameter = parameter, X1 = shape.X1, Y1 = shape.Y1, X2 = shape.X2, Y2 = shape.Y2 };

            if (shape.Type == RectElement.Name)
            {
                if (strokeAlign == "outside") return RectElement.Outside(shape, parameter);
                if (strokeAlign == "inside") return RectElement.Inside(shape, parameter);
                return new RectElement { Parameter = parameter, Width = shape.Width, Height = shape.Height };
            }

            if (shape.Type == CircleElement.Name)
            {
                if (strokeAlign == "outside") return CircleElement.Outside(shape, parameter, shapeR);
                if (strokeAlign == "inside") return CircleElement.Inside(shape, parameter, shapeR);
                return new CircleElement { Parameter = parameter, Cx = shape.Cx, Cy = shape.Cy, R = shapeR.Value };
            }

            if (shape.Type == EllipseElement.Name)
            {
                if (strokeAlign == "outside") return EllipseElement.Outside(shape, parameter);
                if (strokeAlign == "inside") return EllipseElement.Inside(shape, parameter);
                return new EllipseElement { Parameter = parameter, Cx = shape.Cx, Cy = shape.Cy, Rx = shape.Rx, Ry = shape.Ry };
            }

            throw new NotSupportedException($"Unknown type {shape.Type}");
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
            public XdColorValueJson Fill { get; set; }
            public string FillRule { get; set; }
            public bool EnableStroke { get; set; }
            public XdColorValueJson Stroke { get; set; }
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
                if (!EnableFill) return null;
                if (Fill == null) return @"fill=""none""";
                return $@"fill=""{Fill.ToColorString()}""";
            }

            private string FillRuleToSvg()
            {
                if (!EnableFill) return null;
                if (string.IsNullOrWhiteSpace(FillRule)) return null;
                return $@"fill-rule=""{FillRule}""";
            }

            private string StrokeToSvg()
            {
                if (!EnableStroke) return null;
                if (Stroke == null) return @"stroke=""none""";
                return $@"stroke=""{Stroke.ToColorString()}""";
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
                return $@"rx=""{Rx:0.###}""";
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
            public float X { get; set; }
            public float Y { get; set; }

            public string ToSvg()
            {
                if (Math.Abs(X) < 0.0001f && Math.Abs(Y) < 0.0001f) return null;
                return $@"transform=""translate({X:0.###} {Y:0.###})""";
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
                if (Size != null) sizeString = $@"width=""{Size.Value.x}"" height=""{Size.Value.y}"" viewBox=""0 0 {Size.Value.x} {Size.Value.y}""";

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

            public static IElement Outside(XdShapeJson shape, ElementParameter parameter)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;
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

            public static IElement Inside(XdShapeJson shape, ElementParameter parameter)
            {
                var strokeWidth = parameter.StrokeWidth ?? 1f;
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
                return $@"<{Name} cx=""{Cx:0.###}"" cy=""{Cy:0.###}"" r=""{R:0.###}"" {Parameter.GetString()} />";
            }

            public static IElement Outside(XdShapeJson shape, ElementParameter parameter, float? shapeR)
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
                            R = shapeR ?? 1f,
                        },
                        new CircleElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableFill = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            R = (shapeR ?? 1f) + strokeWidth / 2f,
                        },
                    }
                };
            }

            public static IElement Inside(XdShapeJson shape, ElementParameter parameter, float? shapeR)
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
                            R = shapeR ?? 1f,
                        },
                        new CircleElement
                        {
                            Parameter = new ElementParameter
                            {
                                EnableFill = true,
                            },
                            Cx = shape.Cx,
                            Cy = shape.Cy,
                            R = (shapeR ?? 1f) - strokeWidth / 2f,
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
                return $@"<{Name} cx=""{Cx:0.###}"" cy=""{Cy:0.###}"" rx=""{Rx:0.###}"" ry=""{Ry:0.###}"" {Parameter.GetString()} />";
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

        private class ImageElement : IElement
        {
            public const string Name = "image";
            public ElementParameter Parameter { get; set; } = new ElementParameter();

            public float Width { get; set; }
            public float Height { get; set; }
            public byte[] ImageBytes { get; set; }

            public string ToSvg()
            {
                return $@"<{Name} width=""{Width}"" height=""{Height}"" xlink:href=""data:image/png;base64,{Convert.ToBase64String(ImageBytes)}"" {Parameter.GetString()} />";
            }
        }
    }
}
