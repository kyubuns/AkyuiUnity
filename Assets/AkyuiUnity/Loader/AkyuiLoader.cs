using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Akyui.Loader.Internal;
using AkyuiUnity.Loader.Internal;
using UnityEngine;
using Utf8Json;

namespace AkyuiUnity.Loader
{
    public class AkyuiLoader : IAkyuiLoader
    {
        public LayoutInfo LayoutInfo { get; }
        public AssetsInfo AssetsInfo { get; }

        private readonly string _fileName;
        private ZipArchive _zipFile;

        public AkyuiLoader(string filePath)
        {
            _zipFile = ZipFile.Open(filePath, ZipArchiveMode.Read, Encoding.UTF8);

            _fileName = "";
            foreach (var e in _zipFile.Entries)
            {
                if (Path.GetFileName(e.Name) == "layout.json")
                {
                    _fileName = Path.GetDirectoryName(e.FullName);
                }
            }

            LayoutInfo = LoadLayoutInfo();
            AssetsInfo = LoadAssetsInfo();
        }

        public void Dispose()
        {
            _zipFile.Dispose();
            _zipFile = null;
        }

        public byte[] LoadAsset(string assetFileName)
        {
            var entryPath = Path.Combine(_fileName, "assets", assetFileName);
            return _zipFile.ReadBytes(entryPath);
        }

        private LayoutInfo LoadLayoutInfo()
        {
            var layoutJson = GetJson(_zipFile, Path.Combine(_fileName, "layout.json"));

            var metaJson = JsonExtensions.ToStringDictionary(layoutJson["meta"]);
            var meta = new Meta(
                JsonExtensions.ToString(metaJson["akyui_version"]),
                JsonExtensions.ToString(metaJson["app"]),
                JsonExtensions.ToString(metaJson["app_version"])
            );

            var elements = new List<IElement>();
            foreach (var elementJson in JsonExtensions.ToDictionaryArray(layoutJson["elements"]))
            {
                elements.Add(ParseElement(elementJson));
            }

            return new LayoutInfo(
                JsonExtensions.ToString(layoutJson["name"]),
                JsonExtensions.ToUint(layoutJson["hash"]),
                meta,
                JsonExtensions.ToStringDictionary(layoutJson["userdata"]),
                JsonExtensions.ToUint(layoutJson["root"]),
                elements.ToArray()
            );
        }

        private AssetsInfo LoadAssetsInfo()
        {
            var assetsJson = GetJson(_zipFile, Path.Combine(_fileName, "assets.json"));
            Debug.Log(Path.Combine(_fileName, "assets.json"));
            Debug.Log(string.Join(", ", assetsJson.Keys));

            var assets = new List<IAsset>();
            foreach (var assetJson in JsonExtensions.ToDictionaryArray(assetsJson["assets"]))
            {
                assets.Add(ParseAsset(assetJson));
            }

            return new AssetsInfo(
                assets.ToArray()
            );
        }

        private static Dictionary<string, object> GetJson(ZipArchive zipFile, string name)
        {
            var jsonString = zipFile.ReadString(name);
            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            return json;
        }

        private IAsset ParseAsset(Dictionary<string, object> assetJson)
        {
            var assetType = JsonExtensions.ToString(assetJson["type"]);

            if (assetType == SpriteAsset.TypeString)
            {
                Border border = null;

                if (assetJson.ContainsKey("border"))
                {
                    var borderDict = JsonExtensions.ToDictionary(assetJson["border"]);
                    border = new Border(
                        JsonExtensions.ToInt(borderDict["top"]),
                        JsonExtensions.ToInt(borderDict["right"]),
                        JsonExtensions.ToInt(borderDict["bottom"]),
                        JsonExtensions.ToInt(borderDict["left"])
                    );
                }

                return new SpriteAsset(
                    JsonExtensions.ToString(assetJson["file"]),
                    JsonExtensions.ToUint(assetJson["hash"]),
                    JsonExtensions.ToVector2(assetJson["size"]),
                    assetJson.ContainsKey("userdata") ? JsonExtensions.ToString(assetJson["userdata"]) : null,
                    border
                );
            }

            throw new NotSupportedException($"Asset type {assetType} is not supported");
        }

        private IElement ParseElement(Dictionary<string, object> elementJson)
        {
            var elementType = JsonExtensions.ToString(elementJson["type"]);

            if (elementType == ObjectElement.TypeString)
            {
                var components = new List<IComponent>();

                foreach (var componentJson in JsonExtensions.ToDictionaryArray(elementJson["components"]))
                {
                    components.Add(ParseComponent(componentJson));
                }

                var anchorX = (AnchorXType) Enum.Parse(typeof(AnchorXType), JsonExtensions.ToString(elementJson["anchor_x"]), true);
                var anchorY = (AnchorYType) Enum.Parse(typeof(AnchorYType), JsonExtensions.ToString(elementJson["anchor_y"]), true);

                return new ObjectElement(
                    JsonExtensions.ToUint(elementJson["eid"]),
                    JsonExtensions.ToString(elementJson["name"]),
                    JsonExtensions.ToVector2(elementJson["position"]),
                    JsonExtensions.ToVector2(elementJson["size"]),
                    anchorX,
                    anchorY,
                    JsonExtensions.ToFloat(elementJson["rotation"]),
                    JsonExtensions.ToBool(elementJson["visible"]),
                    components.ToArray(),
                    JsonExtensions.ToUintArray(elementJson["children"])
                );
            }

            throw new NotSupportedException($"Element type {elementType} is not supported");
        }

        private IComponent ParseComponent(Dictionary<string, object> componentJson)
        {
            var componentType = JsonExtensions.ToString(componentJson["type"]);

            if (componentType == ImageComponent.TypeString) return ParseImage(componentJson);
            if (componentType == MaskComponent.TypeString) return ParseMask(componentJson);
            if (componentType == AlphaComponent.TypeString) return ParseAlpha(componentJson);
            if (componentType == TextComponent.TypeString) return ParseText(componentJson);
            if (componentType == ButtonComponent.TypeString) return ParseButton(componentJson);
            if (componentType == ToggleComponent.TypeString) return ParseToggle(componentJson);
            if (componentType == VerticalScrollbarComponent.TypeString) return ParseVerticalScrollbar(componentJson);
            if (componentType == HorizontalScrollbarComponent.TypeString) return ParseHorizontalScrollbar(componentJson);
            if (componentType == VerticalListComponent.TypeString) return ParseVerticalList(componentJson);
            if (componentType == HorizontalListComponent.TypeString) return ParseHorizontalList(componentJson);
            if (componentType == HorizontalLayoutComponent.TypeString) return ParseHorizontalLayout(componentJson);
            if (componentType == VerticalLayoutComponent.TypeString) return ParseVerticalLayout(componentJson);
            if (componentType == GridLayoutComponent.TypeString) return ParseGridLayout(componentJson);
            if (componentType == InputFieldComponent.TypeString) return ParseInputField(componentJson);

            throw new NotSupportedException($"Component type {componentType} is not supported");
        }

        private static InputFieldComponent ParseInputField(Dictionary<string, object> componentJson)
        {
            return new InputFieldComponent(
            );
        }

        private static GridLayoutComponent ParseGridLayout(Dictionary<string, object> componentJson)
        {
            return new GridLayoutComponent(
                componentJson.ContainsKey("spacing_x") ? JsonExtensions.ToFloat(componentJson["spacing_x"]) : (float?) null,
                componentJson.ContainsKey("spacing_y") ? JsonExtensions.ToFloat(componentJson["spacing_y"]) : (float?) null
            );
        }

        private static VerticalLayoutComponent ParseVerticalLayout(Dictionary<string, object> componentJson)
        {
            return new VerticalLayoutComponent(
                componentJson.ContainsKey("spacing") ? JsonExtensions.ToFloat(componentJson["spacing"]) : (float?) null
            );
        }

        private static HorizontalLayoutComponent ParseHorizontalLayout(Dictionary<string, object> componentJson)
        {
            return new HorizontalLayoutComponent(
                componentJson.ContainsKey("spacing") ? JsonExtensions.ToFloat(componentJson["spacing"]) : (float?) null
            );
        }

        private static VerticalListComponent ParseVerticalList(Dictionary<string, object> componentJson)
        {
            return new VerticalListComponent(
                componentJson.ContainsKey("spacing") ? JsonExtensions.ToFloat(componentJson["spacing"]) : (float?) null,
                componentJson.ContainsKey("padding_top") ? JsonExtensions.ToFloat(componentJson["padding_top"]) : (float?) null,
                componentJson.ContainsKey("padding_bottom") ? JsonExtensions.ToFloat(componentJson["padding_bottom"]) : (float?) null,
                componentJson.ContainsKey("spacial_spacings")
                    ? JsonExtensions.ToDictionaryArray(componentJson["spacial_spacings"]).Select(x =>
                        new SpecialSpacing(
                            JsonExtensions.ToString(x["item1"]),
                            JsonExtensions.ToString(x["item2"]),
                            JsonExtensions.ToFloat(x["spacing"])
                        )
                    ).ToArray()
                    : null
            );
        }

        private static HorizontalListComponent ParseHorizontalList(Dictionary<string, object> componentJson)
        {
            return new HorizontalListComponent(
                componentJson.ContainsKey("spacing") ? JsonExtensions.ToFloat(componentJson["spacing"]) : (float?) null,
                componentJson.ContainsKey("padding_left") ? JsonExtensions.ToFloat(componentJson["padding_left"]) : (float?) null,
                componentJson.ContainsKey("padding_right") ? JsonExtensions.ToFloat(componentJson["padding_right"]) : (float?) null,
                componentJson.ContainsKey("spacial_spacings")
                    ? JsonExtensions.ToDictionaryArray(componentJson["spacial_spacings"]).Select(x =>
                        new SpecialSpacing(
                            JsonExtensions.ToString(x["item1"]),
                            JsonExtensions.ToString(x["item2"]),
                            JsonExtensions.ToFloat(x["spacing"])
                        )
                    ).ToArray()
                    : null
            );
        }

        private static VerticalScrollbarComponent ParseVerticalScrollbar(Dictionary<string, object> componentJson)
        {
            return new VerticalScrollbarComponent(
                componentJson.ContainsKey("image") ? ParseImage(JsonExtensions.ToDictionary(componentJson["image"])) : null
            );
        }

        private static HorizontalScrollbarComponent ParseHorizontalScrollbar(Dictionary<string, object> componentJson)
        {
            return new HorizontalScrollbarComponent(
                componentJson.ContainsKey("image") ? ParseImage(JsonExtensions.ToDictionary(componentJson["image"])) : null
            );
        }

        private static ButtonComponent ParseButton(Dictionary<string, object> componentJson)
        {
            return new ButtonComponent(
            );
        }

        private static ToggleComponent ParseToggle(Dictionary<string, object> componentJson)
        {
            return new ToggleComponent(
            );
        }

        private static TextComponent ParseText(Dictionary<string, object> componentJson)
        {
            TextComponent.TextAlign? align = null;
            if (componentJson.ContainsKey("align"))
            {
                align = (TextComponent.TextAlign) Enum.Parse(typeof(TextComponent.TextAlign),
                    JsonExtensions.ToString(componentJson["align"]).Replace("_", ""), true);
            }

            return new TextComponent(
                componentJson.ContainsKey("text") ? JsonExtensions.ToString(componentJson["text"]) : null,
                componentJson.ContainsKey("size") ? JsonExtensions.ToFloat(componentJson["size"]) : (float?) null,
                componentJson.ContainsKey("color") ? JsonExtensions.ToColor(componentJson["color"]) : (Color?) null,
                align,
                componentJson.ContainsKey("font") ? JsonExtensions.ToString(componentJson["font"]) : null,
                componentJson.ContainsKey("wrap") ? JsonExtensions.ToBool(componentJson["wrap"]) : (bool?) null,
                componentJson.ContainsKey("line_height") ? JsonExtensions.ToFloat(componentJson["line_height"]) : (float?) null
            );
        }

        private static AlphaComponent ParseAlpha(Dictionary<string, object> componentJson)
        {
            return new AlphaComponent(
                componentJson.ContainsKey("alpha") ? JsonExtensions.ToFloat(componentJson["alpha"]) : (float?) null
            );
        }

        private static ImageComponent ParseImage(Dictionary<string, object> componentJson)
        {
            return new ImageComponent(
                componentJson.ContainsKey("sprite") ? JsonExtensions.ToString(componentJson["sprite"]) : null,
                componentJson.ContainsKey("color") ? JsonExtensions.ToColor(componentJson["color"]) : (Color?) null,
                componentJson.ContainsKey("direction") ? JsonExtensions.ToVector2Int(componentJson["direction"]) : (Vector2Int?) null,
                componentJson.ContainsKey("hash") ? JsonExtensions.ToUint(componentJson["hash"]) : (uint?) null
            );
        }

        private static MaskComponent ParseMask(Dictionary<string, object> componentJson)
        {
            return new MaskComponent(
                componentJson.ContainsKey("sprite") ? JsonExtensions.ToString(componentJson["sprite"]) : null
            );
        }
    }
}

namespace Akyui.Loader.Internal
{
    public static class ZipExtensions
    {
        public static string ReadString(this ZipArchive self, string filePath)
        {
            var manifestZipEntry = self.GetEntry(filePath);
            if (manifestZipEntry == null) throw new Exception($"manifestZipEntry({filePath}) == null");
            using (var reader = new StreamReader(manifestZipEntry.Open()))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] ReadBytes(this ZipArchive self, string filePath)
        {
            var manifestZipEntry = self.GetEntry(filePath);
            if (manifestZipEntry == null) throw new Exception($"manifestZipEntry({filePath}) == null");
            using (var reader = new BinaryReader(manifestZipEntry.Open()))
            {
                // https://stackoverflow.com/questions/8613187/an-elegant-way-to-consume-all-bytes-of-a-binaryreader
                const int bufferSize = 4096;
                using (var ms = new MemoryStream())
                {
                    var buffer = new byte[bufferSize];
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) != 0) ms.Write(buffer, 0, count);
                    return ms.ToArray();
                }
            }
        }
    }
}