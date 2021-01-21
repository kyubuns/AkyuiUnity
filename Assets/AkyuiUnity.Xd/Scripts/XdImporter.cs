using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor;
using AkyuiUnity.Loader;
using AkyuiUnity.Xd.Libraries;
using Newtonsoft.Json;
using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public static class XdImporter
    {
        public static void Import(XdImportSettings settings, string[] xdFilePaths)
        {
            var loaders = new List<IAkyuiLoader>();
            foreach (var xdFilePath in xdFilePaths)
            {
                Debug.Log($"Xd Import Start: {xdFilePath}");
                var file = new XdFile(xdFilePath);
                foreach (var artwork in file.Artworks)
                {
                    if (artwork.Artboard.Children.Length == 0) continue;
                    if (!(artwork.Artboard.Children[0].Meta?.Ux?.MarkedForExport ?? false)) continue;
                    var akyuiXdObjectParsers = settings.ObjectParsers ?? new AkyuiXdObjectParser[] { };
                    var akyuiXdGroupParsers = settings.GroupParsers ?? new AkyuiXdGroupParser[] { };
                    loaders.Add(new XdAkyuiLoader(file, artwork, akyuiXdObjectParsers, akyuiXdGroupParsers));
                }
                Debug.Log($"Xd Import Finish: {xdFilePath}");
            }
            Importer.Import(settings, loaders.ToArray());

            if (!string.IsNullOrWhiteSpace(settings.AkyuiOutputPath))
            {
                foreach (var loader in loaders)
                {
                    var bytes = AkyuiCompressor.Compress(loader);
                    var outputPath = Path.Combine(settings.AkyuiOutputPath, loader.LayoutInfo.Name + ".aky");
                    File.WriteAllBytes(outputPath, bytes);
                    Debug.Log($"Export Akyui {outputPath}");
                }
            }

            foreach (var loader in loaders) loader.Dispose();
        }
    }

    public class XdAkyuiLoader : IAkyuiLoader
    {
        private XdFile _xdFile;
        private readonly IXdObjectParser[] _objectParsers;
        private readonly IXdGroupParser[] _groupParsers;
        private readonly XdAssetHolder _assetHolder;

        private static readonly IXdObjectParser[] DefaultObjectParsers = {
            new ShapeObjectParser(),
            new TextObjectParser(),
        };

        private static readonly IXdGroupParser[] DefaultGroupParsers = {
            new ButtonGroupParser(),
            new RepeatGridGroupParser(),
            new ScrollGroupParser(),
        };

        public XdAkyuiLoader(XdFile xdFile, XdArtboard xdArtboard, AkyuiXdObjectParser[] objectParsers, AkyuiXdGroupParser[] groupParsers)
        {
            _xdFile = xdFile;
            _objectParsers = objectParsers.Concat(DefaultObjectParsers).ToArray();
            _groupParsers = groupParsers.Concat(DefaultGroupParsers).ToArray();
            _assetHolder = new XdAssetHolder(_xdFile);
            (LayoutInfo, AssetsInfo) = Create(xdArtboard, _assetHolder);
        }

        public void Dispose()
        {
            _xdFile?.Dispose();
            _xdFile = null;
        }

        public LayoutInfo LayoutInfo { get; }
        public AssetsInfo AssetsInfo { get; }

        public byte[] LoadAsset(string fileName)
        {
            return _assetHolder.Load(fileName);
        }

        private (LayoutInfo, AssetsInfo) Create(XdArtboard xdArtboard, XdAssetHolder assetHolder)
        {
            var renderer = new XdRenderer(xdArtboard, assetHolder, _objectParsers, _groupParsers);
            var layoutInfo = new LayoutInfo(
                renderer.Name,
                FastHash.CalculateHash(JsonConvert.SerializeObject(xdArtboard.Artboard)),
                renderer.Meta,
                renderer.Root,
                renderer.Elements.ToArray()
            );
            var assetsInfo = new AssetsInfo(
                renderer.Assets.ToArray()
            );
            return (layoutInfo, assetsInfo);
        }

        private class XdRenderer
        {
            public string Name { get; }
            public Meta Meta => new Meta(Const.AkyuiVersion, "AkyuiUnity.Xd", "0.0.0");
            public int Root => 0;
            public List<IElement> Elements { get; }
            public List<IAsset> Assets { get; }

            private readonly XdAssetHolder _xdAssetHolder;
            private readonly IXdObjectParser[] _objectParsers;
            private readonly IXdGroupParser[] _groupParsers;
            private int _nextEid = 1;
            private readonly Dictionary<string, Rect> _size;
            private Dictionary<string, XdObjectJson> _sourceGuidToObject;

            public XdRenderer(XdArtboard xdArtboard, XdAssetHolder xdAssetHolder, IXdObjectParser[] objectParsers, IXdGroupParser[] groupParsers)
            {
                var resources = xdArtboard.Resources;
                _xdAssetHolder = xdAssetHolder;
                _objectParsers = objectParsers;
                _groupParsers = groupParsers;
                Elements = new List<IElement>();
                Assets = new List<IAsset>();
                Name = xdArtboard.Name;
                _size = new Dictionary<string, Rect>();

                CreateRefObjectMap(resources.Resources);

                var xdResourcesArtboardsJson = resources.Artboards[xdArtboard.Manifest.Path.Replace("artboard-", "")];
                var rootSize = new Vector2(xdResourcesArtboardsJson.Width, xdResourcesArtboardsJson.Height);
                var rootOffset = rootSize / -2f - new Vector2(xdResourcesArtboardsJson.X, xdResourcesArtboardsJson.Y);
                var xdObjectJsons = xdArtboard.Artboard.Children.SelectMany(x => x.Artboard.Children).ToArray();
                var convertedXdObjectJsons = ConvertRefObject(xdObjectJsons);
                CalcPosition(convertedXdObjectJsons, rootOffset, Vector2.zero);
                var children = Render(convertedXdObjectJsons);
                var root = new ObjectElement(
                    0,
                    xdArtboard.Name,
                    Vector2.zero,
                    rootSize,
                    AnchorXType.Center,
                    AnchorYType.Middle,
                    new IComponent[] { },
                    children.Select(x => x.Eid).ToArray()
                );
                Elements.Add(root);
            }

            private void CreateRefObjectMap(XdResourcesResourcesJson resource)
            {
                _sourceGuidToObject = new Dictionary<string, XdObjectJson>();

                var symbols = resource.Meta.Ux.Symbols;
                foreach (var symbol in symbols)
                {
                    CreateRefObjectMapInternal(symbol);
                }
            }

            private void CreateRefObjectMapInternal(XdObjectJson xdObject)
            {
                _sourceGuidToObject[xdObject.Id] = xdObject;
                if (xdObject.Group != null)
                {
                    foreach (var child in xdObject.Group.Children)
                    {
                        CreateRefObjectMapInternal(child);
                    }
                }

                if (xdObject.Meta?.Ux?.States != null)
                {
                    foreach (var child in xdObject.Meta.Ux.States)
                    {
                        CreateRefObjectMapInternal(child);
                    }
                }
            }

            private XdObjectJson ConvertRefObject(XdObjectJson xdObject)
            {
                var newXdObjectJson = GetRefObject(xdObject);

                if (newXdObjectJson.Group != null)
                {
                    newXdObjectJson.Group = new XdObjectGroupJson { Children = ConvertRefObject(newXdObjectJson.Group.Children) };
                }

                return newXdObjectJson;
            }

            private XdObjectJson[] ConvertRefObject(XdObjectJson[] xdObject)
            {
                var a = new List<XdObjectJson>();
                foreach (var x in xdObject)
                {
                    a.Add(ConvertRefObject(x));
                }
                return a.ToArray();
            }

            private XdObjectJson GetRefObject(XdObjectJson xdObject)
            {
                if (xdObject.Type != "syncRef") return xdObject;

                var newXdObjectJson = new XdObjectJson();
                var source = _sourceGuidToObject[xdObject.SyncSourceGuid];
                var propertyInfos = typeof(XdObjectJson).GetProperties();
                foreach (var propertyInfo in propertyInfos)
                {
                    var value = propertyInfo.GetValue(xdObject);
                    if (value == null) value = propertyInfo.GetValue(source);

                    propertyInfo.SetValue(newXdObjectJson, value);
                }
                newXdObjectJson.Name = source.Name;
                newXdObjectJson.Type = source.Type;

                return newXdObjectJson;
            }

            private string[] CalcPosition(XdObjectJson[] xdObjects, Vector2 rootOffset, Vector2 parentPosition)
            {
                return xdObjects.Select(xdObject => CalcPosition(xdObject, rootOffset, parentPosition)).ToArray();
            }

            private IElement[] Render(XdObjectJson[] xdObjects)
            {
                var children = new List<IElement>();
                foreach (var xdObject in xdObjects) children.AddRange(Render(xdObject));
                return children.ToArray();
            }

            private string CalcPosition(XdObjectJson xdObject, Vector2 rootOffset, Vector2 parentPosition)
            {
                var id = xdObject.Id ?? xdObject.Guid;
                var position = new Vector2((xdObject.Transform?.Tx ?? 0f) + parentPosition.x, (xdObject.Transform?.Ty ?? 0f) + parentPosition.y);

                var children = new string[] { };
                if (xdObject.Group != null)
                {
                    children = CalcPosition(xdObject.Group.Children, rootOffset, position);
                }

                position += rootOffset;
                foreach (var parser in _objectParsers)
                {
                    if (!parser.Is(xdObject)) continue;
                    var size = parser.CalcSize(xdObject, position);
                    _size[id] = size;
                    return id;
                }

                if (xdObject.Type == "group")
                {
                    var childrenRects = children.Select(x => _size[x]).ToArray();
                    var minX = childrenRects.Length > 0 ? childrenRects.Min(x => x.min.x) : 0f;
                    var minY = childrenRects.Length > 0 ? childrenRects.Min(x => x.min.y) : 0f;
                    var maxX = childrenRects.Length > 0 ? childrenRects.Max(x => x.max.x) : 0f;
                    var maxY = childrenRects.Length > 0 ? childrenRects.Max(x => x.max.y) : 0f;
                    var groupRect = Rect.MinMaxRect(minX, minY, maxX, maxY);

                    foreach (var parser in _groupParsers)
                    {
                        if (!parser.Is(xdObject)) continue;
                        groupRect = parser.CalcSize(xdObject, position, groupRect);
                    }

                    _size[id] = groupRect;
                    foreach (var c in children)
                    {
                        var t = _size[c];
                        t.center -= groupRect.center;
                        _size[c] = t;
                    }

                    return id;
                }

                throw new Exception($"Unknown object type {xdObject.Type}");
            }

            private IElement[] Render(XdObjectJson xdObject)
            {
                var id = xdObject.Id ?? xdObject.Guid;
                var eid = _nextEid;
                _nextEid++;

                var rect = _size[id];
                var position = new Vector2(rect.center.x, -rect.center.y);
                var size = rect.size;
                var anchorX = AnchorXType.Center;
                var anchorY = AnchorYType.Middle;
                var constRight = xdObject.Meta.Ux.ConstraintRight;
                var constLeft = xdObject.Meta.Ux.ConstraintLeft;
                if (constRight && constLeft) anchorX = AnchorXType.Stretch;
                else if (constRight) anchorX = AnchorXType.Right;
                else if (constLeft) anchorX = AnchorXType.Left;

                var constTop = xdObject.Meta.Ux.ConstraintTop;
                var constBottom = xdObject.Meta.Ux.ConstraintBottom;
                if (constTop && constBottom) anchorY = AnchorYType.Stretch;
                else if (constTop) anchorY = AnchorYType.Top;
                else if (constBottom) anchorY = AnchorYType.Bottom;

                foreach (var parser in _objectParsers)
                {
                    if (!parser.Is(xdObject)) continue;
                    var (components, assets) = parser.Render(xdObject, size, _xdAssetHolder);

                    var children = new IElement[] { };
                    if (xdObject.Group != null) children = Render(xdObject.Group.Children);

                    var element = new ObjectElement(
                        eid,
                        xdObject.Name,
                        position,
                        size,
                        anchorX,
                        anchorY,
                        components,
                        children.Select(x => x.Eid).ToArray()
                    );

                    foreach (var asset in assets)
                    {
                        if (Assets.Any(x => x.FileName == asset.FileName)) continue;
                        Assets.Add(asset);
                    }
                    Elements.Add(element);
                    return new IElement[] { element };
                }

                if (xdObject.Type == "group")
                {
                    var components = new List<IComponent>();
                    var children = new XdObjectJson[] { };
                    if (xdObject.Group != null) children = xdObject.Group.Children;

                    foreach (var parser in _groupParsers)
                    {
                        if (!parser.Is(xdObject)) continue;
                        components.AddRange(parser.Render(xdObject, ref children));
                    }

                    var generatedChildren = new IElement[] { };
                    if (xdObject.Group != null) generatedChildren = Render(children);

                    var group = new ObjectElement(
                        eid,
                        xdObject.Name,
                        position,
                        size,
                        anchorX,
                        anchorY,
                        components.ToArray(),
                        generatedChildren.Select(x => x.Eid).ToArray()
                    );

                    Elements.Add(group);
                    return new IElement[] { group };
                }

                throw new Exception($"Unknown object type {xdObject.Type}");
            }
        }
    }
}