using System;
using System.Collections.Generic;
using System.IO;
using AkyuiUnity.Editor.MiniJSON;
using AkyuiUnity.Loader.Internal;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace AkyuiUnity.Loader
{
    public class AkyuiAkyuiLoader : IAkyuiLoader
    {
        public string FileName { get; }
        public LayoutInfo LayoutInfo { get; }
        public AssetsInfo AssetsInfo { get; }

        private readonly ZipFile _zipFile;

        public AkyuiAkyuiLoader(string filePath)
        {
            _zipFile = new ZipFile(filePath);
            FileName = Path.GetFileNameWithoutExtension(_zipFile.Name);
            LayoutInfo = LoadLayoutInfo();
            AssetsInfo = LoadAssetsInfo();
        }

        public void Dispose()
        {
            _zipFile?.Close();
        }

        public byte[] LoadAsset(string assetFileName)
        {
            var assetEntry = _zipFile.FindEntry(Path.Combine(FileName, "assets", assetFileName), true);
            var stream = _zipFile.GetInputStream(assetEntry);

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                return bytes;
            }
        }

        private LayoutInfo LoadLayoutInfo()
        {
            var layoutJson = GetJson(_zipFile, Path.Combine(FileName, "layout.json"));

            var metaJson = layoutJson["meta"].JsonStringDictionary();
            var meta = new Meta(
                metaJson["version"].JsonString(),
                metaJson["generated_by"].JsonString()
            );

            var elements = new List<IElement>();
            foreach (var elementJson in layoutJson["elements"].JsonDictionaryArray())
            {
                elements.Add(ParseElement(elementJson));
            }

            return new LayoutInfo(
                meta,
                layoutJson["timestamp"].JsonInt(),
                layoutJson["root"].JsonInt(),
                elements.ToArray()
            );
        }

        private AssetsInfo LoadAssetsInfo()
        {
            var assetsJson = GetJson(_zipFile, Path.Combine(FileName, "assets.json"));

            var assets = new List<IAsset>();
            foreach (var assetJson in assetsJson["assets"].JsonDictionaryArray())
            {
                assets.Add(ParseAsset(assetJson));
            }

            return new AssetsInfo(
                assets.ToArray()
            );
        }

        private static Dictionary<string, object> GetJson(ZipFile zipFile, string name)
        {
            var layoutJson = zipFile.FindEntry(name, true);

            var stream = zipFile.GetInputStream(layoutJson);
            using (var reader = new StreamReader(stream))
            {
                var jsonString = reader.ReadToEnd();
                var json = (Dictionary<string, object>) Json.Deserialize(jsonString);
                return json;
            }
        }

        private IAsset ParseAsset(Dictionary<string, object> assetJson)
        {
            var assetType = assetJson["type"].JsonString();

            if (assetType == "sprite")
            {
                return new SpriteAsset(
                    assetJson["file"].JsonString(),
                    assetJson["timestamp"].JsonInt()
                );
            }

            throw new NotSupportedException($"Asset type {assetType} is not supported");
        }

        private IElement ParseElement(Dictionary<string, object> elementJson)
        {
            var elementType = elementJson["type"].JsonString();

            if (elementType == "object")
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
                    components.ToArray(),
                    elementJson["children"].JsonIntArray()
                );
            }

            if (elementType == "prefab")
            {
                var overrides = new List<Override>();
                foreach (var overrideJson in elementJson["overrides"].JsonDictionaryArray())
                {
                    List<IComponent> overrideComponents = null;
                    if (overrideJson.ContainsKey("components"))
                    {
                        overrideComponents = new List<IComponent>();
                        foreach (var overrideComponentJson in overrideJson["components"].JsonDictionaryArray())
                        {
                            overrideComponents.Add(ParseComponent(overrideComponentJson));
                        }
                    }

                    AnchorXType? anchorX = null;
                    if (overrideJson.ContainsKey("anchor_x")) anchorX = (AnchorXType) Enum.Parse(typeof(AnchorXType), overrideJson["anchor_x"].JsonString(), true);

                    AnchorYType? anchorY = null;
                    if (overrideJson.ContainsKey("anchor_y")) anchorY = (AnchorYType) Enum.Parse(typeof(AnchorYType), overrideJson["anchor_y"].JsonString(), true);

                    overrides.Add(new Override(
                        overrideJson["eid"].JsonIntArray(),
                        overrideJson.ContainsKey("name") ? overrideJson["name"].JsonString() : null,
                        overrideJson.ContainsKey("position") ? overrideJson["position"].JsonVector2() : (Vector2?) null,
                        overrideJson.ContainsKey("size") ? overrideJson["size"].JsonVector2() : (Vector2?) null,
                        anchorX,
                        anchorY,
                        overrideComponents?.ToArray()
                    ));
                }

                return new PrefabElement(
                    elementJson["eid"].JsonInt(),
                    elementJson["reference"].JsonString(),
                    elementJson["timestamp"].JsonInt(),
                    overrides.ToArray()
                );
            }

            throw new NotSupportedException($"Element type {elementType} is not supported");
        }

        private IComponent ParseComponent(Dictionary<string, object> componentJson)
        {
            var componentType = componentJson["type"].JsonString();

            if (componentType == "image")
            {
                return new ImageComponent(
                    componentJson["cid"].JsonInt(),
                    componentJson.ContainsKey("sprite") ? componentJson["sprite"].JsonString() : null,
                    componentJson.ContainsKey("color") ? componentJson["color"].JsonColor() : (Color?) null
                );
            }

            if (componentType == "text")
            {
                TextComponent.TextAlign? align = null;
                if (componentJson.ContainsKey("align"))
                {
                    align = (TextComponent.TextAlign) Enum.Parse(typeof(TextComponent.TextAlign),
                        componentJson["align"].JsonString().Replace("_", ""), true);
                }
                return new TextComponent(
                    componentJson["cid"].JsonInt(),
                    componentJson.ContainsKey("text") ? componentJson["text"].JsonString() : null,
                    componentJson.ContainsKey("size") ? componentJson["size"].JsonFloat() : (float?) null,
                    componentJson.ContainsKey("color") ? componentJson["color"].JsonColor() : (Color?) null,
                    align
                );
            }

            if (componentType == "button")
            {
                return new ButtonComponent(
                    componentJson["cid"].JsonInt()
                );
            }

            if (componentType == "horizontal_layout")
            {
                return new HorizontalLayoutComponent(
                    componentJson["cid"].JsonInt()
                );
            }

            if (componentType == "vertical_layout")
            {
                return new VerticalLayoutComponent(
                    componentJson["cid"].JsonInt()
                );
            }

            throw new NotSupportedException($"Component type {componentType} is not supported");
        }
    }
}