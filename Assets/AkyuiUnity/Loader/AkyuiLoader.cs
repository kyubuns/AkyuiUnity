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

            var metaJson = layoutJson["meta"].JsonStringDictionary();
            var meta = new Meta(
                metaJson["akyui_version"].JsonString(),
                metaJson["app"].JsonString(),
                metaJson["app_version"].JsonString()
            );

            var elements = new List<IElement>();
            foreach (var elementJson in layoutJson["elements"].JsonDictionaryArray())
            {
                elements.Add(ParseElement(elementJson));
            }

            return new LayoutInfo(
                layoutJson["name"].JsonString(),
                layoutJson["hash"].JsonLong(),
                meta,
                layoutJson["userdata"].JsonStringDictionary(),
                layoutJson["root"].JsonInt(),
                elements.ToArray()
            );
        }

        private AssetsInfo LoadAssetsInfo()
        {
            var assetsJson = GetJson(_zipFile, Path.Combine(_fileName, "assets.json"));
            Debug.Log(Path.Combine(_fileName, "assets.json"));
            Debug.Log(string.Join(", ", assetsJson.Keys));

            var assets = new List<IAsset>();
            foreach (var assetJson in assetsJson["assets"].JsonDictionaryArray())
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
            var assetType = assetJson["type"].JsonString();

            if (assetType == SpriteAsset.TypeString)
            {
                Border border = null;

                if (assetJson.ContainsKey("border"))
                {
                    var borderDict = assetJson["border"].JsonDictionary();
                    border = new Border(
                        borderDict["top"].JsonInt(),
                        borderDict["right"].JsonInt(),
                        borderDict["bottom"].JsonInt(),
                        borderDict["left"].JsonInt()
                    );
                }

                return new SpriteAsset(
                    assetJson["file"].JsonString(),
                    assetJson["hash"].JsonLong(),
                    assetJson["size"].JsonVector2(),
                    assetJson.ContainsKey("userdata") ? assetJson["userdata"].JsonString() : null,
                    border
                );
            }

            throw new NotSupportedException($"Asset type {assetType} is not supported");
        }

        private IElement ParseElement(Dictionary<string, object> elementJson)
        {
            var elementType = elementJson["type"].JsonString();

            if (elementType == ObjectElement.TypeString)
            {
                var components = new List<IComponent>();

                foreach (var componentJson in elementJson["components"].JsonDictionaryArray())
                {
                    components.Add(ParseComponent(componentJson));
                }

                var anchorX = (AnchorXType) Enum.Parse(typeof(AnchorXType), elementJson["anchor_x"].JsonString(), true);
                var anchorY = (AnchorYType) Enum.Parse(typeof(AnchorYType), elementJson["anchor_y"].JsonString(), true);

                return new ObjectElement(
                    elementJson["eid"].JsonInt(),
                    elementJson["name"].JsonString(),
                    elementJson["position"].JsonVector2(),
                    elementJson["size"].JsonVector2(),
                    anchorX,
                    anchorY,
                    elementJson["rotation"].JsonFloat(),
                    elementJson["visible"].JsonBool(),
                    components.ToArray(),
                    elementJson["children"].JsonIntArray()
                );
            }

            throw new NotSupportedException($"Element type {elementType} is not supported");
        }

        private IComponent ParseComponent(Dictionary<string, object> componentJson)
        {
            var componentType = componentJson["type"].JsonString();

            if (componentType == ImageComponent.TypeString) return ParseImage(componentJson);
            if (componentType == MaskComponent.TypeString) return ParseMask(componentJson);
            if (componentType == AlphaComponent.TypeString) return ParseAlpha(componentJson);
            if (componentType == TextComponent.TypeString) return ParseText(componentJson);
            if (componentType == ButtonComponent.TypeString) return ParseButton(componentJson);
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
                componentJson.ContainsKey("spacing_x") ? componentJson["spacing_x"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("spacing_y") ? componentJson["spacing_y"].JsonFloat() : (float?) null
            );
        }

        private static VerticalLayoutComponent ParseVerticalLayout(Dictionary<string, object> componentJson)
        {
            return new VerticalLayoutComponent(
                componentJson.ContainsKey("spacing") ? componentJson["spacing"].JsonFloat() : (float?) null
            );
        }

        private static HorizontalLayoutComponent ParseHorizontalLayout(Dictionary<string, object> componentJson)
        {
            return new HorizontalLayoutComponent(
                componentJson.ContainsKey("spacing") ? componentJson["spacing"].JsonFloat() : (float?) null
            );
        }

        private static VerticalListComponent ParseVerticalList(Dictionary<string, object> componentJson)
        {
            return new VerticalListComponent(
                componentJson.ContainsKey("spacing") ? componentJson["spacing"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("padding_top") ? componentJson["padding_top"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("padding_bottom") ? componentJson["padding_bottom"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("spacial_spacings")
                    ? componentJson["spacial_spacings"].JsonDictionaryArray().Select(x =>
                        new SpecialSpacing(
                            x["item1"].JsonString(),
                            x["item2"].JsonString(),
                            x["spacing"].JsonFloat()
                        )
                    ).ToArray()
                    : null
            );
        }

        private static HorizontalListComponent ParseHorizontalList(Dictionary<string, object> componentJson)
        {
            return new HorizontalListComponent(
                componentJson.ContainsKey("spacing") ? componentJson["spacing"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("padding_left") ? componentJson["padding_left"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("padding_right") ? componentJson["padding_right"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("spacial_spacings")
                    ? componentJson["spacial_spacings"].JsonDictionaryArray().Select(x =>
                        new SpecialSpacing(
                            x["item1"].JsonString(),
                            x["item2"].JsonString(),
                            x["spacing"].JsonFloat()
                        )
                    ).ToArray()
                    : null
            );
        }

        private static VerticalScrollbarComponent ParseVerticalScrollbar(Dictionary<string, object> componentJson)
        {
            return new VerticalScrollbarComponent(
                componentJson.ContainsKey("image") ? ParseImage(componentJson["image"].JsonDictionary()) : null
            );
        }

        private static HorizontalScrollbarComponent ParseHorizontalScrollbar(Dictionary<string, object> componentJson)
        {
            return new HorizontalScrollbarComponent(
                componentJson.ContainsKey("image") ? ParseImage(componentJson["image"].JsonDictionary()) : null
            );
        }

        private static ButtonComponent ParseButton(Dictionary<string, object> componentJson)
        {
            return new ButtonComponent(
            );
        }

        private static TextComponent ParseText(Dictionary<string, object> componentJson)
        {
            TextComponent.TextAlign? align = null;
            if (componentJson.ContainsKey("align"))
            {
                align = (TextComponent.TextAlign) Enum.Parse(typeof(TextComponent.TextAlign),
                    componentJson["align"].JsonString().Replace("_", ""), true);
            }

            return new TextComponent(
                componentJson.ContainsKey("text") ? componentJson["text"].JsonString() : null,
                componentJson.ContainsKey("size") ? componentJson["size"].JsonFloat() : (float?) null,
                componentJson.ContainsKey("color") ? componentJson["color"].JsonColor() : (Color?) null,
                align,
                componentJson.ContainsKey("font") ? componentJson["font"].JsonString() : null,
                componentJson.ContainsKey("wrap") ? componentJson["wrap"].JsonBool() : (bool?) null,
                componentJson.ContainsKey("line_height") ? componentJson["line_height"].JsonFloat() : (float?) null
            );
        }

        private static AlphaComponent ParseAlpha(Dictionary<string, object> componentJson)
        {
            return new AlphaComponent(
                componentJson.ContainsKey("alpha") ? componentJson["alpha"].JsonFloat() : (float?) null
            );
        }

        private static ImageComponent ParseImage(Dictionary<string, object> componentJson)
        {
            return new ImageComponent(
                componentJson.ContainsKey("sprite") ? componentJson["sprite"].JsonString() : null,
                componentJson.ContainsKey("color") ? componentJson["color"].JsonColor() : (Color?) null,
                componentJson.ContainsKey("direction") ? componentJson["direction"].JsonVector2Int() : (Vector2Int?) null
            );
        }

        private static MaskComponent ParseMask(Dictionary<string, object> componentJson)
        {
            return new MaskComponent(
                componentJson.ContainsKey("sprite") ? componentJson["sprite"].JsonString() : null
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