using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Utf8Json;
using XdParser.Internal;
using System.IO.Compression;
using System.Text;
using Akyui.Loader.Internal;
using AkyuiUnity.Loader;

namespace XdParser
{
    public class XdFile : IDisposable
    {
        private ZipArchive _zipFile;
        public XdArtboard[] Artworks { get; }

        public XdFile(string xdFilePath)
        {
            _zipFile = ZipFile.Open(xdFilePath, ZipArchiveMode.Read, Encoding.UTF8);
            var jsonObjectCache = new Dictionary<string, object>();
            var jsonTextCache = new Dictionary<string, string>();
            var manifestJsonString = _zipFile.ReadString("manifest");
            var xdManifestJson = JsonSerializer.Deserialize<XdManifestJson>(manifestJsonString);

            var artworks = new List<XdArtboard>();
            foreach (var xdManifestArtwork in xdManifestJson.Children.Single(x => x.Path == "artwork").Children)
            {
                var artworkJsonFilePath = $"artwork/{xdManifestArtwork.Path}/graphics/graphicContent.agc";
                if (!jsonTextCache.ContainsKey(artworkJsonFilePath)) jsonTextCache.Add(artworkJsonFilePath, _zipFile.ReadString(artworkJsonFilePath));
                var artworkJsonString = jsonTextCache[artworkJsonFilePath];
                if (!jsonObjectCache.ContainsKey(artworkJsonFilePath)) jsonObjectCache.Add(artworkJsonFilePath, JsonSerializer.Deserialize<XdArtboardJson>(artworkJsonString));
                var artworkJson = (XdArtboardJson) jsonObjectCache[artworkJsonFilePath];

                var resourceJsonFilePath = artworkJson.Resources.Href.TrimStart('/');
                if (!jsonTextCache.ContainsKey(resourceJsonFilePath)) jsonTextCache.Add(resourceJsonFilePath, _zipFile.ReadString(resourceJsonFilePath));
                var resourcesJsonString = jsonTextCache[resourceJsonFilePath];
                if (!jsonObjectCache.ContainsKey(resourceJsonFilePath)) jsonObjectCache.Add(resourceJsonFilePath, JsonSerializer.Deserialize<XdResourcesJson>(resourcesJsonString));
                var resourceJson = (XdResourcesJson) jsonObjectCache[resourceJsonFilePath];

                var xdHash = FastHash.CalculateHash(artworkJsonString + resourcesJsonString);
                artworks.Add(new XdArtboard(xdManifestArtwork.Name, xdManifestArtwork, artworkJson, resourceJson, xdHash));
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
            _zipFile?.Dispose();
            _zipFile = null;
        }
    }

    public class XdArtboard
    {
        public XdManifestChildJson Manifest { get; }
        public XdArtboardJson Artboard { get; }
        public XdResourcesJson Resources { get; }

        public string Name { get; }
        public uint Hash { get; }

        public XdArtboard(string name, XdManifestChildJson manifest, XdArtboardJson artboard, XdResourcesJson resources, uint hash)
        {
            Name = name;
            Manifest = manifest;
            Artboard = artboard;
            Resources = resources;
            Hash = hash;
        }
    }
}

namespace XdParser.Internal
{
    public class XdColorJson
    {
        [DataMember(Name = "mode")]
        public string Mode { get; set; }

        [DataMember(Name = "value")]
        public XdColorValueJson Value { get; set; }

        [DataMember(Name = "alpha")]
        public float? Alpha { get; set; }
    }

    public class XdColorValueJson
    {
        [DataMember(Name = "r")]
        public int R { get; set; }

        [DataMember(Name = "g")]
        public int G { get; set; }

        [DataMember(Name = "b")]
        public int B { get; set; }
    }

    public class XdTransformJson
    {
        [DataMember(Name = "a")]
        public float A { get; set; }

        [DataMember(Name = "b")]
        public float B { get; set; }

        [DataMember(Name = "c")]
        public float C { get; set; }

        [DataMember(Name = "d")]
        public float D { get; set; }

        [DataMember(Name = "tx")]
        public float Tx { get; set; }

        [DataMember(Name = "ty")]
        public float Ty { get; set; }
    }

    public class XdSizeJson
    {
        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }
    }

    public class XdPositionJson
    {
        [DataMember(Name = "x")]
        public float X { get; set; }

        [DataMember(Name = "y")]
        public float Y { get; set; }
    }

    public class XdStyleJson
    {
        [DataMember(Name = "fill")]
        public XdStyleFillJson Fill { get; set; }

        [DataMember(Name = "stroke")]
        public XdStyleStrokeJson Stroke { get; set; }

        [DataMember(Name = "font")]
        public XdStyleFontJson Font { get; set; }

        [DataMember(Name = "textAttributes")]
        public XdStyleTextAttributesJson TextAttributes { get; set; }

        [DataMember(Name = "opacity")]
        public float? Opacity { get; set; }

        [DataMember(Name = "isolation")]
        public string Isolation { get; set; }

        [DataMember(Name = "blendMode")]
        public string BlendMode { get; set; }
    }

    public class XdStyleFillJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "color")]
        public XdColorJson Color { get; set; }

        [DataMember(Name = "pattern")]
        public XdStyleFillPatternJson Pattern { get; set; }
    }

    public class XdStyleStrokeJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "color")]
        public XdColorJson Color { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "align")]
        public string Align { get; set; }

        [DataMember(Name = "cap")]
        public string Cap { get; set; }

        [DataMember(Name = "join")]
        public string Join { get; set; }

        [DataMember(Name = "miterLimit")]
        public float? MiterLimit { get; set; }

        [DataMember(Name = "dash")]
        public float[] Dash { get; set; }
    }

    public class XdStyleFontJson
    {
        [DataMember(Name = "family")]
        public string Family { get; set; }

        [DataMember(Name = "postscriptName")]
        public string PostscriptName { get; set; }

        [DataMember(Name = "size")]
        public float Size { get; set; }

        [DataMember(Name = "style")]
        public string Style { get; set; }
    }

    public class XdStyleTextAttributesJson
    {
        [DataMember(Name = "paragraphAlign")]
        public string ParagraphAlign { get; set; } // default = left

        [DataMember(Name = "lineHeight")]
        public float? LineHeight { get; set; }
    }

    public class XdStyleFillPatternJson
    {
        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }

        [DataMember(Name = "meta")]
        public XdStyleFillPatternMetaJson Meta { get; set; }

        [DataMember(Name = "href")]
        public string Href { get; set; }
    }

    public class XdStyleFillPatternMetaJson
    {
        [DataMember(Name = "ux")]
        public XdStyleFillPatternMetaUxJson Ux { get; set; }
    }

    public class XdStyleFillPatternMetaUxJson
    {
        [DataMember(Name = "scaleBehavior")]
        public string ScaleBehavior { get; set; }

        [DataMember(Name = "uid")]
        public string Uid { get; set; }

        [DataMember(Name = "hrefLastModifiedDate")]
        public uint HrefLastModifiedDate { get; set; }

        [DataMember(Name = "flipX")]
        public bool FlipX { get; set; }

        [DataMember(Name = "flipY")]
        public bool FlipY { get; set; }

        [DataMember(Name = "offsetX")]
        public float OffsetX { get; set; }

        [DataMember(Name = "offsetY")]
        public float OffsetY { get; set; }

        [DataMember(Name = "scale")]
        public float? Scale { get; set; }
    }

    public class XdShapeJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "x")]
        public float X { get; set; }

        [DataMember(Name = "y")]
        public float Y { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "winding")]
        public string Winding { get; set; }

        [DataMember(Name = "cx")]
        public float Cx { get; set; }

        [DataMember(Name = "cy")]
        public float Cy { get; set; }

        [DataMember(Name = "rx")]
        public float Rx { get; set; }

        [DataMember(Name = "ry")]
        public float Ry { get; set; }

        [DataMember(Name = "x1")]
        public float X1 { get; set; }

        [DataMember(Name = "y1")]
        public float Y1 { get; set; }

        [DataMember(Name = "x2")]
        public float X2 { get; set; }

        [DataMember(Name = "y2")]
        public float Y2 { get; set; }

        [DataMember(Name = "r")]
        public object R { get; set; }

        [DataMember(Name = "operation")]
        public string Operation { get; set; }

        [DataMember(Name = "points")]
        public XdPositionJson[] Points { get; set; }
    }

    public class XdTextJson
    {
        [DataMember(Name = "frame")]
        public XdTextFrameJson Frame { get; set; }

        [DataMember(Name = "paragraphs")]
        public XdTextParagraphJson[] Paragraphs { get; set; }

        [DataMember(Name = "rawText")]
        public string RawText { get; set; }
    }

    public class XdTextFrameJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }
    }

    public class XdTextParagraphJson
    {
        [DataMember(Name = "lines")]
        public XdTextParagraphLineJson[][] Lines { get; set; }
    }

    public class XdTextParagraphLineJson
    {
        [DataMember(Name = "from")]
        public float From { get; set; }

        [DataMember(Name = "to")]
        public float To { get; set; }

        [DataMember(Name = "x")]
        public float X { get; set; }

        [DataMember(Name = "y")]
        public float Y { get; set; }
    }

    public class XdManifestJson
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "manifest-format-version")]
        public int ManifestFormatVersion { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "components")]
        public XdManifestComponentJson[] Components { get; set; }

        [DataMember(Name = "children")]
        public XdManifestChildJson[] Children { get; set; }
    }

    public class XdManifestComponentJson
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "rel")]
        public string Rel { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }
    }

    public class XdManifestChildJson
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "path")]
        public string Path { get; set; }

        [DataMember(Name = "children")]
        public XdManifestChildJson[] Children { get; set; }

        [DataMember(Name = "components")]
        public XdManifestComponentJson[] Components { get; set; }
    }

    public class XdObjectJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "meta")]
        public XdObjectMetaJson Meta { get; set; }

        [DataMember(Name = "transform")]
        public XdTransformJson Transform { get; set; }

        [DataMember(Name = "group")]
        public XdObjectGroupJson Group { get; set; }

        [DataMember(Name = "style")]
        public XdStyleJson Style { get; set; }

        [DataMember(Name = "shape")]
        public XdShapeJson Shape { get; set; }

        [DataMember(Name = "text")]
        public XdTextJson Text { get; set; }

        [DataMember(Name = "guid")]
        public string Guid { get; set; }

        [DataMember(Name = "syncSourceGuid")]
        public string SyncSourceGuid { get; set; }

        [DataMember(Name = "visible")]
        public bool? Visible { get; set; }

        [DataMember(Name = "markedForExport")]
        public bool? MarkedForExport { get; set; }
    }

    public class XdArtboardJson
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "children")]
        public XdArtboardChildJson[] Children { get; set; }

        [DataMember(Name = "resources")]
        public XdArtboardResourcesJson Resources { get; set; }

        [DataMember(Name = "artboards")]
        public XdArtboardArtboardsJson Artboards { get; set; }
    }

    public class XdArtboardChildJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "meta")]
        public XdObjectMetaJson Meta { get; set; }

        [DataMember(Name = "style")]
        public XdStyleJson Style { get; set; }

        [DataMember(Name = "artboard")]
        public XdArtboardChildArtboardJson Artboard { get; set; }
    }

    public class XdArtboardChildArtboardJson
    {
        [DataMember(Name = "children")]
        public XdObjectJson[] Children { get; set; }

        [DataMember(Name = "meta")]
        public XdObjectMetaJson Meta { get; set; }

        [DataMember(Name = "ref")]
        public string Ref { get; set; }
    }

    public class XdObjectMetaJson
    {
        [DataMember(Name = "ux")]
        public XdObjectMetaUxJson Ux { get; set; }
    }

    public class XdObjectMetaUxJson
    {
        [DataMember(Name = "nameL10N")]
        public string NameL10N { get; set; }

        [DataMember(Name = "symbolId")]
        public string SymbolId { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }

        [DataMember(Name = "componentType")]
        public string ComponentType { get; set; }

        [DataMember(Name = "isMaster")]
        public bool IsMaster { get; set; }

        [DataMember(Name = "syncMap")]
        public Dictionary<string, string> SyncMap { get; set; }

        [DataMember(Name = "hasCustomName")]
        public bool HasCustomName { get; set; }

        [DataMember(Name = "aspectLock")]
        public XdSizeJson AspectLock { get; set; }

        [DataMember(Name = "customConstraints")]
        public bool CustomConstraints { get; set; }

        [DataMember(Name = "constraintWidth")]
        public bool ConstraintWidth { get; set; }

        [DataMember(Name = "constraintHeight")]
        public bool ConstraintHeight { get; set; }

        [DataMember(Name = "constraintRight")]
        public bool ConstraintRight { get; set; }

        [DataMember(Name = "constraintLeft")]
        public bool ConstraintLeft { get; set; }

        [DataMember(Name = "constraintTop")]
        public bool ConstraintTop { get; set; }

        [DataMember(Name = "constraintBottom")]
        public bool ConstraintBottom { get; set; }

        [DataMember(Name = "localTransform")]
        public XdTransformJson LocalTransform { get; set; }

        [DataMember(Name = "modTime")]
        public ulong ModTime { get; set; }

        [DataMember(Name = "stateId")]
        public string StateId { get; set; }

        [DataMember(Name = "states")]
        public XdObjectJson[] States { get; set; }

        [DataMember(Name = "interactions")]
        public XdInteractionJson[] Interactions { get; set; }

        [DataMember(Name = "repeatGrid")]
        public XdRepeatGridJson RepeatGrid { get; set; }

        [DataMember(Name = "scrollingType")]
        public string ScrollingType { get; set; }

        [DataMember(Name = "viewportWidth")]
        public float ViewportWidth { get; set; }

        [DataMember(Name = "viewportHeight")]
        public float ViewportHeight { get; set; }

        [DataMember(Name = "offsetX")]
        public float OffsetX { get; set; }

        [DataMember(Name = "offsetY")]
        public float OffsetY { get; set; }

        [DataMember(Name = "markedForExport")]
        public bool MarkedForExport { get; set; }

        [DataMember(Name = "clipPathResources")]
        public XdClipPathResourcesJson ClipPathResources { get; set; }

        [DataMember(Name = "rotation")]
        public float Rotation { get; set; }

        [DataMember(Name = "linkedContentScale")]
        public XdPositionJson LinkedContentScale { get; set; }

        [DataMember(Name = "linkedElementRef")]
        public string LinkedElementRef { get; set; }

        [DataMember(Name = "linkedElementLastModified")]
        public ulong? LinkedElementLastModified { get; set; }

        [DataMember(Name = "linkedElementWasImported")]
        public bool LinkedElementWasImported { get; set; }

        [DataMember(Name = "listViewPosition")]
        public int? ListViewPosition { get; set; }
    }

    public class XdClipPathResourcesJson
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "children")]
        public XdObjectJson[] Children { get; set; }
    }

    public class XdRepeatGridJson
    {
        [DataMember(Name = "cellWidth")]
        public float? CellWidth { get; set; }

        [DataMember(Name = "cellHeight")]
        public float? CellHeight { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }

        [DataMember(Name = "paddingX")]
        public float PaddingX { get; set; }

        [DataMember(Name = "paddingY")]
        public float PaddingY { get; set; }

        [DataMember(Name = "columns")]
        public int Columns { get; set; }

        [DataMember(Name = "rows")]
        public int Rows { get; set; }
    }

    public class XdInteractionJson
    {
        [DataMember(Name = "data")]
        public XdInteractionDataJson Data { get; set; }

        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; }

        [DataMember(Name = "guid")]
        public string Guid { get; set; }

        [DataMember(Name = "inherited")]
        public bool Inherited { get; set; }

        [DataMember(Name = "valid")]
        public bool Valid { get; set; }
    }

    public class XdInteractionDataJson
    {
        [DataMember(Name = "interaction")]
        public XdInteractionDataInteractionJson Interaction { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }

    public class XdInteractionDataInteractionJson
    {
        [DataMember(Name = "action")]
        public string Action { get; set; }

        [DataMember(Name = "properties")]
        public XdInteractionDataInteractionPropertiesJson Properties { get; set; }

        [DataMember(Name = "triggerEvent")]
        public string TriggerEvent { get; set; }
    }

    public class XdInteractionDataInteractionPropertiesJson
    {
        [DataMember(Name = "destination")]
        public string Destination { get; set; }

        [DataMember(Name = "duration")]
        public float Duration { get; set; }

        [DataMember(Name = "easing")]
        public string Easing { get; set; }

        [DataMember(Name = "transition")]
        public string Transition { get; set; }

        [DataMember(Name = "voiceLocale")]
        public string VoiceLocale { get; set; }
    }

    public class XdObjectGroupJson
    {
        [DataMember(Name = "children")]
        public XdObjectJson[] Children { get; set; }
    }

    public class XdArtboardResourcesJson
    {
        [DataMember(Name = "href")]
        public string Href { get; set; }
    }

    public class XdArtboardArtboardsJson
    {
        [DataMember(Name = "href")]
        public string Href { get; set; }
    }

    public class XdResourcesJson
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "children")]
        public XdResourcesChildJson[] Children { get; set; }

        [DataMember(Name = "resources")]
        public XdResourcesResourcesJson Resources { get; set; }

        [DataMember(Name = "artboards")]
        public Dictionary<string, XdResourcesArtboardsJson> Artboards { get; set; }
    }

    public class XdResourcesChildJson
    {
    }

    public class XdResourcesResourcesJson
    {
        [DataMember(Name = "meta")]
        public XdResourcesResourcesMetaJson Meta { get; set; }

        [DataMember(Name = "gradients")]
        public XdResourcesResourcesGradientsJson Gradients { get; set; }

        [DataMember(Name = "clipPaths")]
        public XdResourcesResourcesClipPathsJson ClipPaths { get; set; }
    }

    public class XdResourcesResourcesMetaJson
    {
        [DataMember(Name = "ux")]
        public XdResourcesResourcesMetaUxJson Ux { get; set; }
    }

    public class XdResourcesResourcesMetaUxJson
    {
        [DataMember(Name = "colorSwatches")]
        public XdResourcesResourcesMetaUxColorSwatcheJson[] ColorSwatches { get; set; }

        [DataMember(Name = "documentLibrary")]
        public XdResourcesResourcesMetaUxDocumentLibraryJson DocumentLibrary { get; set; }

        [DataMember(Name = "gridDefaults")]
        public XdResourcesResourcesMetaUxGridDefaultsJson GridDefaults { get; set; }

        [DataMember(Name = "symbols")]
        public XdObjectJson[] Symbols { get; set; }

        [DataMember(Name = "symbolsMetadata")]
        public XdResourcesResourcesMetaUxSymbolsMetadataJson SymbolsMetadata { get; set; }
    }

    public class XdResourcesResourcesMetaUxColorSwatcheJson
    {
    }

    public class XdResourcesResourcesMetaUxDocumentLibraryJson
    {
        [DataMember(Name = "version")]
        public int Version { get; set; }

        [DataMember(Name = "isStickerSheet")]
        public bool IsStickerSheet { get; set; }

        [DataMember(Name = "hashedMetadata")]
        public XdResourcesResourcesMetaUxDocumentLibraryHashedMetadataJson HashedMetadata { get; set; }

        [DataMember(Name = "elements")]
        public XdResourcesResourcesMetaUxDocumentLibraryHashedElementJson[] Elements { get; set; }
    }

    public class XdResourcesResourcesMetaUxGridDefaultsJson
    {
    }

    public class XdResourcesResourcesMetaUxSymbolsMetadataJson
    {
        [DataMember(Name = "usingNestedSymbolSyncing")]
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
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "x")]
        public float X { get; set; }

        [DataMember(Name = "y")]
        public float Y { get; set; }

        [DataMember(Name = "width")]
        public float Width { get; set; }

        [DataMember(Name = "height")]
        public float Height { get; set; }

        [DataMember(Name = "viewportHeight")]
        public float ViewportHeight { get; set; }
    }
}