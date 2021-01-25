using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor.MiniJSON;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace AkyuiUnity.Loader
{
    public static class AkyuiCompressor
    {
        public static byte[] Compress(IAkyuiLoader loader)
        {
            using (var memoryStream = new MemoryStream())
            using (var zipStream = new ZipOutputStream(memoryStream))
            {
                {
                    var newEntry = new ZipEntry(Path.Combine(loader.LayoutInfo.Name, "layout.json"));
                    zipStream.PutNextEntry(newEntry);

                    var text = Json.Serialize(ToSerializable(loader.LayoutInfo));
                    var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
                    zipStream.Write(textBytes, 0, textBytes.Length);

                    zipStream.CloseEntry();
                }

                {
                    var newEntry = new ZipEntry(Path.Combine(loader.LayoutInfo.Name, "assets.json"));
                    zipStream.PutNextEntry(newEntry);

                    var text = Json.Serialize(ToSerializable(loader.AssetsInfo));
                    var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
                    zipStream.Write(textBytes, 0, textBytes.Length);

                    zipStream.CloseEntry();
                }

                foreach (var asset in loader.AssetsInfo.Assets)
                {
                    var newEntry = new ZipEntry(Path.Combine(loader.LayoutInfo.Name, "assets", asset.FileName));
                    zipStream.PutNextEntry(newEntry);

                    var bytes = loader.LoadAsset(asset.FileName);
                    zipStream.Write(bytes, 0, bytes.Length);

                    zipStream.CloseEntry();
                }

                zipStream.Close();

                return memoryStream.ToArray();
            }
        }

        private static Dictionary<string, object> ToSerializable(LayoutInfo source)
        {
            var dict = new Dictionary<string, object>();
            var elements = new List<object>();
            dict["name"] = ToSerializable(source.Name);
            dict["hash"] = ToSerializable(source.Hash);
            dict["meta"] = new Dictionary<string, object>
            {
                { "app", ToSerializable(source.Meta.App) },
                { "akyui_version", ToSerializable(source.Meta.AkyuiVersion) },
                { "app_version", ToSerializable(source.Meta.AppVersion) },
            };
            dict["root"] = ToSerializable(source.Root);
            dict["elements"] = elements;

            foreach (var element in source.Elements)
            {
                if (element is ObjectElement objectElement)
                {
                    elements.Add(new Dictionary<string, object>
                    {
                        { "eid", ToSerializable(objectElement.Eid) },
                        { "type", ObjectElement.TypeString },
                        { "name", ToSerializable(objectElement.Name) },
                        { "position", ToSerializable(objectElement.Position) },
                        { "size", ToSerializable(objectElement.Size) },
                        { "anchor_x", ToSerializable(objectElement.AnchorX) },
                        { "anchor_y", ToSerializable(objectElement.AnchorY) },
                        { "rotation", ToSerializable(objectElement.Rotation) },
                        { "visible", ToSerializable(objectElement.Visible) },
                        { "components", objectElement.Components.Select(ToSerializable).ToArray() },
                        { "children", ToSerializable(objectElement.Children) },
                    });
                }
                else
                {
                    throw new NotSupportedException($"Element type {element} is not supported");
                }
            }

            return dict;
        }

        private static Dictionary<string, object> ToSerializable(AssetsInfo source)
        {
            var dict = new Dictionary<string, object>();
            var assets = new List<object>();
            dict["assets"] = assets;

            foreach (var asset in source.Assets)
            {
                if (asset is SpriteAsset spriteAsset)
                {
                    assets.Add(new Dictionary<string, object>
                    {
                        { "type", SpriteAsset.TypeString },
                        { "hash", ToSerializable(spriteAsset.Hash) },
                        { "userdata", ToSerializable(spriteAsset.UserData) },
                        { "file", ToSerializable(spriteAsset.FileName) },
                    });
                }
                else
                {
                    throw new NotSupportedException($"Asset type {asset} is not supported");
                }
            }

            return dict;
        }

        private static Dictionary<string, object> ToSerializable(IComponent source)
        {
            var dict = new Dictionary<string, object>();

            if (source is ImageComponent imageComponent)
            {
                dict["type"] = ImageComponent.TypeString;
                if (imageComponent.Sprite != null) dict["sprite"] = ToSerializable(imageComponent.Sprite);
                if (imageComponent.Color != null) dict["color"] = ToSerializable(imageComponent.Color.Value);
                if (imageComponent.Direction != null) dict["direction"] = ToSerializable(imageComponent.Direction.Value);
            }
            else if (source is MaskComponent maskComponent)
            {
                dict["type"] = MaskComponent.TypeString;
                if (maskComponent.Sprite != null) dict["sprite"] = ToSerializable(maskComponent.Sprite);
            }
            else if (source is AlphaComponent alphaComponent)
            {
                dict["type"] = AlphaComponent.TypeString;
                if (alphaComponent.Alpha != null) dict["alpha"] = ToSerializable(alphaComponent.Alpha.Value);
            }
            else if (source is TextComponent textComponent)
            {
                dict["type"] = TextComponent.TypeString;
                if (textComponent.Text != null) dict["text"] = ToSerializable(textComponent.Text);
                if (textComponent.Size != null) dict["size"] = ToSerializable(textComponent.Size.Value);
                if (textComponent.Color != null) dict["color"] = ToSerializable(textComponent.Color.Value);
                if (textComponent.Align != null) dict["align"] = ToSerializable(textComponent.Align.Value);
                if (textComponent.Font != null) dict["font"] = ToSerializable(textComponent.Font);
                if (textComponent.Wrap != null) dict["wrap"] = ToSerializable(textComponent.Wrap.Value);
                if (textComponent.LineHeight != null) dict["line_height"] = ToSerializable(textComponent.LineHeight.Value);
            }
            else if (source is ButtonComponent)
            {
                dict["type"] = ButtonComponent.TypeString;
            }
            else if (source is VerticalScrollbarComponent scrollbarComponent)
            {
                dict["type"] = VerticalScrollbarComponent.TypeString;
                if (scrollbarComponent.Image != null) dict["image"] = ToSerializable(scrollbarComponent.Image);
            }
            else if (source is VerticalListComponent verticalListComponent)
            {
                dict["type"] = VerticalListComponent.TypeString;
                if (verticalListComponent.Spacing != null) dict["spacing"] = ToSerializable(verticalListComponent.Spacing.Value);
                if (verticalListComponent.PaddingTop != null) dict["padding_top"] = ToSerializable(verticalListComponent.PaddingTop.Value);
                if (verticalListComponent.PaddingBottom != null) dict["padding_bottom"] = ToSerializable(verticalListComponent.PaddingBottom.Value);
                if (verticalListComponent.SpacialSpacings != null)
                {
                    var inner = new List<Dictionary<string, object>>();
                    foreach (var a in verticalListComponent.SpacialSpacings)
                    {
                        inner.Add(new Dictionary<string, object>
                        {
                            { "item1", a.Item1 },
                            { "item2", a.Item2 },
                            { "spacing", a.Spacing },
                        });
                    }
                    dict["spacial_spacings"] = inner;
                }
            }
            else if (source is HorizontalLayoutComponent horizontalLayoutComponent)
            {
                dict["type"] = HorizontalLayoutComponent.TypeString;
                if (horizontalLayoutComponent.Spacing != null) dict["spacing"] = ToSerializable(horizontalLayoutComponent.Spacing.Value);
            }
            else if (source is VerticalLayoutComponent verticalLayoutComponent)
            {
                dict["type"] = VerticalLayoutComponent.TypeString;
                if (verticalLayoutComponent.Spacing != null) dict["spacing"] = ToSerializable(verticalLayoutComponent.Spacing.Value);
            }
            else if (source is GridLayoutComponent gridLayoutComponent)
            {
                dict["type"] = GridLayoutComponent.TypeString;
                if (gridLayoutComponent.SpacingX != null) dict["spacing_x"] = ToSerializable(gridLayoutComponent.SpacingX.Value);
                if (gridLayoutComponent.SpacingY != null) dict["spacing_y"] = ToSerializable(gridLayoutComponent.SpacingY.Value);
            }
            else if (source is InputFieldComponent)
            {
                dict["type"] = InputFieldComponent.TypeString;
            }
            else
            {
                throw new NotSupportedException($"Component type {source} is not supported");
            }

            return dict;
        }

        private static bool ToSerializable(bool flag) => flag;
        private static string ToSerializable(string text) => text;
        private static int ToSerializable(int number) => number;
        private static float ToSerializable(float number) => number;
        private static int[] ToSerializable(int[] numbers) => numbers;
        private static float[] ToSerializable(float[] numbers) => numbers;
        private static float[] ToSerializable(Vector2 v) => new[] { v[0], v[1] };

        private static int[] ToSerializable(Color color)
        {
            var c = (Color32)color;
            return new int[] { c.r, c.g, c.b, c.a };
        }

        private static int[] ToSerializable(Vector2Int v)
        {
            return new[] { v.x, v.y };
        }

        private static string ToSerializable(AnchorXType x) => x.ToString().ToLower();
        private static string ToSerializable(AnchorYType x) => x.ToString().ToLower();

        private static string ToSerializable(TextComponent.TextAlign textAlign)
        {
            switch (textAlign)
            {
                case TextComponent.TextAlign.UpperLeft: return "upper_left";
                case TextComponent.TextAlign.UpperCenter: return "upper_center";
                case TextComponent.TextAlign.UpperRight: return "upper_right";
                case TextComponent.TextAlign.MiddleLeft: return "middle_left";
                case TextComponent.TextAlign.MiddleCenter: return "middle_center";
                case TextComponent.TextAlign.MiddleRight: return "middle_right";
                case TextComponent.TextAlign.LowerLeft: return "lower_left";
                case TextComponent.TextAlign.LowerCenter: return "lower_center";
                case TextComponent.TextAlign.LowerRight: return "lower_right";
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAlign), textAlign, null);
            }
        }
    }
}