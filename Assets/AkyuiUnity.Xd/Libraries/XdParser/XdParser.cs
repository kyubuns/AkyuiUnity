using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using XdParser.Internal;
using Newtonsoft.Json;

namespace XdParser
{
    public class XdFile : IDisposable
    {
        private ZipFile _zipFile;
        public XdArtboard[] Artworks { get; }

        public XdFile(string xdFilePath)
        {
            _zipFile = new ZipFile(xdFilePath);
            var manifestJsonString = _zipFile.ReadString("manifest");
            var xdManifestJson = JsonConvert.DeserializeObject<XdManifestJson>(manifestJsonString);

            var artworks = new List<XdArtboard>();
            foreach (var xdManifestArtwork in xdManifestJson.Children.Single(x => x.Path == "artwork").Children)
            {
                var artworkJsonString = _zipFile.ReadString($"artwork/{xdManifestArtwork.Path}/graphics/graphicContent.agc");
                var artworkJson = JsonConvert.DeserializeObject<XdArtboardJson>(artworkJsonString);
                var resourcesJsonString = _zipFile.ReadString(artworkJson.Resources.Href.TrimStart('/'));
                var resourceJson = JsonConvert.DeserializeObject<XdResourcesJson>(resourcesJsonString);
                artworks.Add(new XdArtboard(xdManifestArtwork, artworkJson, resourceJson));
            }
            Artworks = artworks.ToArray();
        }


        public byte[] GetResource(XdStyleFillPatternMetaJson styleFillPatternMetaJson)
        {
            var uid = styleFillPatternMetaJson?.Ux?.Uid;
            if (string.IsNullOrWhiteSpace(uid)) return null;
            return _zipFile.ReadBytes($"resources/{uid}");
        }

        public void Dispose()
        {
            _zipFile?.Close();
            _zipFile = null;
        }
    }

    public class XdArtboard
    {
        public XdManifestChildJson Manifest { get; }
        public XdArtboardJson Artboard { get; }
        public XdResourcesJson Resources { get; }

        public string Name => Manifest.Name;

        public XdArtboard(XdManifestChildJson manifest, XdArtboardJson artboard, XdResourcesJson resources)
        {
            Manifest = manifest;
            Artboard = artboard;
            Resources = resources;
        }
    }
}

namespace XdParser.Internal
{
    public static class ZipExtensions
    {
        public static string ReadString(this ZipFile self, string filePath)
        {
            var manifestZipEntry = self.GetEntry(filePath);
            using (var stream = self.GetInputStream(manifestZipEntry))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }


        public static byte[] ReadBytes(this ZipFile self, string filePath)
        {
            var manifestZipEntry = self.GetEntry(filePath);
            using(var stream = self.GetInputStream(manifestZipEntry))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public class XdColorJson
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("value")]
        public XdColorValueJson Value { get; set; }
    }

    public class XdColorValueJson
    {
        [JsonProperty("r")]
        public int R { get; set; }

        [JsonProperty("g")]
        public int G { get; set; }

        [JsonProperty("b")]
        public int B { get; set; }
    }

    public class XdTransformJson
    {
        [JsonProperty("a")]
        public float A { get; set; }

        [JsonProperty("b")]
        public float B { get; set; }

        [JsonProperty("c")]
        public float C { get; set; }

        [JsonProperty("d")]
        public float D { get; set; }

        [JsonProperty("tx")]
        public float Tx { get; set; }

        [JsonProperty("ty")]
        public float Ty { get; set; }
    }

    public class XdSizeJson
    {
        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }
    }

    public class XdStyleJson
    {
        [JsonProperty("fill")]
        public XdStyleFillJson Fill { get; set; }

        [JsonProperty("stroke")]
        public XdStyleStrokeJson Stroke { get; set; }

        [JsonProperty("font")]
        public XdStyleFontJson Font { get; set; }

        [JsonProperty("textAttributes")]
        public XdStyleTextAttributesJson TextAttributes { get; set; }

        [JsonProperty("opacity")]
        public float? Opacity { get; set; }

        [JsonProperty("isolation")]
        public string Isolation { get; set; }
    }

    public class XdStyleFillJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("color")]
        public XdColorJson Color { get; set; }

        [JsonProperty("pattern")]
        public XdStyleFillPatternJson Pattern { get; set; }
    }

    public class XdStyleStrokeJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("color")]
        public XdColorJson Color { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("align")]
        public string Align { get; set; }
    }

    public class XdStyleFontJson
    {
        [JsonProperty("family")]
        public string Family { get; set; }

        [JsonProperty("postscriptName")]
        public string PostscriptName { get; set; }

        [JsonProperty("size")]
        public float Size { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }
    }

    public class XdStyleTextAttributesJson
    {
        [JsonProperty("paragraphAlign")]
        public string ParagraphAlign { get; set; } // default = left
    }

    public class XdStyleFillPatternJson
    {
        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("meta")]
        public XdStyleFillPatternMetaJson Meta { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class XdStyleFillPatternMetaJson
    {
        [JsonProperty("ux")]
        public XdStyleFillPatternMetaUxJson Ux { get; set; }
    }

    public class XdStyleFillPatternMetaUxJson
    {
        [JsonProperty("scaleBehavior")]
        public string ScaleBehavior { get; set; }

        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("hrefLastModifiedDate")]
        public uint HrefLastModifiedDate { get; set; }
    }

    public class XdShapeJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("winding")]
        public string Winding { get; set; }

        [JsonProperty("cx")]
        public float Cx { get; set; }

        [JsonProperty("cy")]
        public float Cy { get; set; }

        [JsonProperty("rx")]
        public float Rx { get; set; }

        [JsonProperty("ry")]
        public float Ry { get; set; }

        [JsonProperty("x1")]
        public float X1 { get; set; }

        [JsonProperty("y1")]
        public float Y1 { get; set; }

        [JsonProperty("x2")]
        public float X2 { get; set; }

        [JsonProperty("y2")]
        public float Y2 { get; set; }
    }

    public class XdTextJson
    {
        [JsonProperty("frame")]
        public XdTextFrameJson Frame { get; set; }

        [JsonProperty("paragraphs")]
        public XdTextParagraphJson[] Paragraphs { get; set; }

        [JsonProperty("rawText")]
        public string RawText { get; set; }
    }

    public class XdTextFrameJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class XdTextParagraphJson
    {
        [JsonProperty("lines")]
        public XdTextParagraphLineJson[][] Lines { get; set; }
    }

    public class XdTextParagraphLineJson
    {
        [JsonProperty("from")]
        public float From { get; set; }

        [JsonProperty("to")]
        public float To { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }
    }

    public class XdManifestJson
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("manifest-format-version")]
        public string ManifestFormatVersion { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("components")]
        public XdManifestComponentJson[] Components { get; set; }

        [JsonProperty("children")]
        public XdManifestChildJson[] Children { get; set; }
    }

    public class XdManifestComponentJson
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }
    }

    public class XdMetaJson
    {
        // not implemented
    }

    public class XdManifestChildJson
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("children")]
        public XdManifestChildJson[] Children { get; set; }

        [JsonProperty("components")]
        public XdManifestComponentJson[] Components { get; set; }
    }

    public class XdObjectJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("meta")]
        public XdObjectMetaJson Meta { get; set; }

        [JsonProperty("transform")]
        public XdTransformJson Transform { get; set; }

        [JsonProperty("group")]
        public XdObjectGroupJson Group { get; set; }

        [JsonProperty("style")]
        public XdStyleJson Style { get; set; }

        [JsonProperty("shape")]
        public XdShapeJson Shape { get; set; }

        [JsonProperty("text")]
        public XdTextJson Text { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("syncSourceGuid")]
        public string SyncSourceGuid { get; set; }
    }

    public class XdArtboardJson
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("children")]
        public XdArtboardChildJson[] Children { get; set; }

        [JsonProperty("resources")]
        public XdArtboardResourcesJson Resources { get; set; }

        [JsonProperty("artboards")]
        public XdArtboardArtboardsJson Artboards { get; set; }
    }

    public class XdArtboardChildJson
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("meta")]
        public XdMetaJson Meta { get; set; }

        [JsonProperty("style")]
        public XdStyleJson Style { get; set; }

        [JsonProperty("artboard")]
        public XdArtboardChildArtboardJson Artboard { get; set; }
    }

    public class XdArtboardChildArtboardJson
    {
        [JsonProperty("children")]
        public XdObjectJson[] Children { get; set; }

        [JsonProperty("meta")]
        public XdMetaJson Meta { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }
    }

    public class XdObjectMetaJson
    {
        [JsonProperty("ux")]
        public XdObjectMetaUxJson Ux { get; set; }
    }

    public class XdObjectMetaUxJson
    {
        [JsonProperty("nameL10N")]
        public string NameL10N { get; set; }

        [JsonProperty("symbolId")]
        public string SymbolId { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        [JsonProperty("isMaster")]
        public bool IsMaster { get; set; }

        [JsonProperty("syncMap")]
        public Dictionary<string, string> SyncMap { get; set; }

        [JsonProperty("hasCustomName")]
        public string HasCustomName { get; set; }

        [JsonProperty("aspectLock")]
        public XdSizeJson AspectLock { get; set; }

        [JsonProperty("customConstraints")]
        public bool CustomConstraints { get; set; }

        [JsonProperty("constraintWidth")]
        public bool ConstraintWidth { get; set; }

        [JsonProperty("constraintHeight")]
        public bool ConstraintHeight { get; set; }

        [JsonProperty("constraintRight")]
        public bool ConstraintRight { get; set; }

        [JsonProperty("constraintLeft")]
        public bool ConstraintLeft { get; set; }

        [JsonProperty("constraintTop")]
        public bool ConstraintTop { get; set; }

        [JsonProperty("constraintBottom")]
        public bool ConstraintBottom { get; set; }

        [JsonProperty("localTransform")]
        public XdTransformJson LocalTransform { get; set; }

        [JsonProperty("modTime")]
        public ulong ModTime { get; set; }

        [JsonProperty("stateId")]
        public string StateId { get; set; }

        [JsonProperty("states")]
        public XdObjectJson[] States { get; set; }

        [JsonProperty("interactions")]
        public XdInteractionJson[] Interactions { get; set; }

        [JsonProperty("repeatGrid")]
        public XdRepeatGridJson RepeatGrid { get; set; }

        [JsonProperty("scrollingType")]
        public string ScrollingType { get; set; }

        [JsonProperty("viewportWidth")]
        public float ViewportWidth { get; set; }

        [JsonProperty("viewportHeight")]
        public float ViewportHeight { get; set; }

        [JsonProperty("offsetX")]
        public float OffsetX { get; set; }

        [JsonProperty("offsetY")]
        public float OffsetY { get; set; }
    }

    public class XdRepeatGridJson
    {
        [JsonProperty("cellWidth")]
        public float CellWidth { get; set; }

        [JsonProperty("cellHeight")]
        public float CellHeight { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("paddingX")]
        public float PaddingX { get; set; }

        [JsonProperty("paddingY")]
        public float PaddingY { get; set; }

        [JsonProperty("columns")]
        public int Columns { get; set; }

        [JsonProperty("rows")]
        public int Rows { get; set; }
    }

    public class XdInteractionJson
    {
        [JsonProperty("data")]
        public XdInteractionDataJson Data { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("inherited")]
        public bool Inherited { get; set; }

        [JsonProperty("valid")]
        public bool Valid { get; set; }
    }

    public class XdInteractionDataJson
    {
        [JsonProperty("interaction")]
        public XdInteractionDataInteractionJson Interaction { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class XdInteractionDataInteractionJson
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("properties")]
        public XdInteractionDataInteractionPropertiesJson Properties { get; set; }

        [JsonProperty("triggerEvent")]
        public string TriggerEvent { get; set; }
    }

    public class XdInteractionDataInteractionPropertiesJson
    {
        [JsonProperty("destination")]
        public string Destination { get; set; }

        [JsonProperty("duration")]
        public float Duration { get; set; }

        [JsonProperty("easing")]
        public string Easing { get; set; }

        [JsonProperty("transition")]
        public string Transition { get; set; }

        [JsonProperty("voiceLocale")]
        public string VoiceLocale { get; set; }
    }

    public class XdObjectGroupJson
    {
        [JsonProperty("children")]
        public XdObjectJson[] Children { get; set; }
    }

    public class XdArtboardResourcesJson
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class XdArtboardArtboardsJson
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class XdResourcesJson
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("children")]
        public XdResourcesChildJson[] Children { get; set; }

        [JsonProperty("resources")]
        public XdResourcesResourcesJson Resources { get; set; }

        [JsonProperty("artboards")]
        public Dictionary<string, XdResourcesArtboardsJson> Artboards { get; set; }
    }

    public class XdResourcesChildJson
    {
    }

    public class XdResourcesResourcesJson
    {
        [JsonProperty("meta")]
        public XdResourcesResourcesMetaJson Meta { get; set; }

        [JsonProperty("gradients")]
        public XdResourcesResourcesGradientsJson Gradients { get; set; }

        [JsonProperty("clipPaths")]
        public XdResourcesResourcesClipPathsJson ClipPaths { get; set; }
    }

    public class XdResourcesResourcesMetaJson
    {
        [JsonProperty("ux")]
        public XdResourcesResourcesMetaUxJson Ux { get; set; }
    }

    public class XdResourcesResourcesMetaUxJson
    {
        [JsonProperty("colorSwatches")]
        public XdResourcesResourcesMetaUxColorSwatcheJson[] ColorSwatches { get; set; }

        [JsonProperty("documentLibrary")]
        public XdResourcesResourcesMetaUxDocumentLibraryJson DocumentLibrary { get; set; }

        [JsonProperty("gridDefaults")]
        public XdResourcesResourcesMetaUxGridDefaultsJson GridDefaults { get; set; }

        [JsonProperty("symbols")]
        public XdObjectJson[] Symbols { get; set; }

        [JsonProperty("symbolsMetadata")]
        public XdResourcesResourcesMetaUxSymbolsMetadataJson SymbolsMetadata { get; set; }
    }

    public class XdResourcesResourcesMetaUxColorSwatcheJson
    {
    }

    public class XdResourcesResourcesMetaUxDocumentLibraryJson
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("isStickerSheet")]
        public bool IsStickerSheet { get; set; }

        [JsonProperty("hashedMetadata")]
        public XdResourcesResourcesMetaUxDocumentLibraryHashedMetadataJson HashedMetadata { get; set; }

        [JsonProperty("elements")]
        public XdResourcesResourcesMetaUxDocumentLibraryHashedElementJson[] Elements { get; set; }
    }

    public class XdResourcesResourcesMetaUxGridDefaultsJson
    {
    }

    public class XdResourcesResourcesMetaUxSymbolsMetadataJson
    {
        [JsonProperty("usingNestedSymbolSyncing")]
        public bool UsingNestedSymbolSyncing { get; set; }
    }

    public class XdResourcesResourcesMetaUxDocumentLibraryHashedMetadataJson
    {
    }

    public class XdResourcesResourcesMetaUxDocumentLibraryHashedElementJson
    {
    }

    public class XdResourcesResourcesGradientsJson
    {
    }

    public class XdResourcesResourcesClipPathsJson
    {
    }

    public class XdResourcesArtboardsJson
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("viewportHeight")]
        public float ViewportHeight { get; set; }
    }
}