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
                        { "components", objectElement.Components.Select(ToSerializable).ToArray() },
                        { "children", ToSerializable(objectElement.Children) },
                    });
                }
                else if (element is PrefabElement prefabElement)
                {
                    elements.Add(new Dictionary<string, object>
                    {
                        { "eid", ToSerializable(prefabElement.Eid) },
                        { "type", ToSerializable(PrefabElement.TypeString) },
                        { "reference", ToSerializable(prefabElement.Reference) },
                        { "hash", ToSerializable(prefabElement.Hash) },
                        { "overrides", prefabElement.Overrides.Select(ToSerializable).ToArray() }
                    });
                }
                else
                {
                    throw new NotSupportedException($"Element type {element} is not supported");
                }
            }

            return dict;
        }

        private static Dictionary<string, object> ToSerializable(Override source)
        {
            var dict = new Dictionary<string, object>
            {
                { "eid", source.Eid },
            };

            if (source.Name != null) dict["name"] = ToSerializable(source.Name);
            if (source.Position != null) dict["position"] = ToSerializable(source.Position.Value);
            if (source.Size != null) dict["size"] = ToSerializable(source.Size.Value);
            if (source.AnchorX != null) dict["anchor_x"] = ToSerializable(source.AnchorX.Value);
            if (source.AnchorY != null) dict["anchor_y"] = ToSerializable(source.AnchorY.Value);
            if (source.Components != null) dict["components"] = source.Components.Select(ToSerializable).ToArray();

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
            var dict = new Dictionary<string, object>
            {
                { "cid", ToSerializable(source.Cid) },
            };

            if (source is ImageComponent imageComponent)
            {
                dict["type"] = ImageComponent.TypeString;
                if (imageComponent.Sprite != null) dict["sprite"] = ToSerializable(imageComponent.Sprite);
                if (imageComponent.Color != null) dict["color"] = ToSerializable(imageComponent.Color.Value);
            }
            else if (source is TextComponent textComponent)
            {
                dict["type"] = TextComponent.TypeString;
                if (textComponent.Text != null) dict["text"] = ToSerializable(textComponent.Text);
                if (textComponent.Size != null) dict["size"] = ToSerializable(textComponent.Size.Value);
                if (textComponent.Color != null) dict["color"] = ToSerializable(textComponent.Color.Value);
                if (textComponent.Align != null) dict["align"] = ToSerializable(textComponent.Align.Value);
            }
            else if (source is ButtonComponent)
            {
                dict["type"] = ButtonComponent.TypeString;
            }
            else if (source is HorizontalLayoutComponent)
            {
                dict["type"] = HorizontalLayoutComponent.TypeString;
            }
            else if (source is VerticalLayoutComponent)
            {
                dict["type"] = VerticalLayoutComponent.TypeString;
            }
            else
            {
                throw new NotSupportedException($"Component type {source} is not supported");
            }

            return dict;
        }

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

        private static string ToSerializable(AnchorXType x) => x.ToString().ToLower();
        private static string ToSerializable(AnchorYType x) => x.ToString().ToLower();

        private static string ToSerializable(TextComponent.TextAlign textAlign)
        {
            switch (textAlign)
            {
                case TextComponent.TextAlign.MiddleCenter: return "middle_center";
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAlign), textAlign, null);
            }
        }
    }
}