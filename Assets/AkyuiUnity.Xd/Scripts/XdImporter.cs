using System;
using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Editor;
using AkyuiUnity.Loader;
using UnityEngine;
using XdParser;
using XdParser.Internal;
using Random = UnityEngine.Random;

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
                    if (artwork.Name == "pasteboard") continue;
                    loaders.Add((IAkyuiLoader) new XdAkyuiLoader(file, artwork));
                }
                Debug.Log($"Xd Import Finish: {xdFilePath}");
            }
            Importer.Import(settings, loaders.ToArray());
            foreach (var loader in loaders) loader.Dispose();
        }
    }

    public class XdAkyuiLoader : IAkyuiLoader
    {
        private XdFile _xdFile;
        private readonly XdAssetHolder _assetHolder;

        private static readonly IXdObjectParser[] Parsers = {
            new ShapeObjectParser(),
        };

        public XdAkyuiLoader(XdFile xdFile, XdArtboard xdArtboard)
        {
            _xdFile = xdFile;
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
            var renderer = new XdRenderer(xdArtboard, assetHolder);
            var layoutInfo = new LayoutInfo(
                renderer.Name,
                renderer.Hash,
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
            public long Hash => Random.Range(0, 100000); // 今は必ず更新する
            public Meta Meta => new Meta(Const.AkyuiVersion, "XdToAkyui", "0.0.0");
            public int Root => 0;
            public List<IElement> Elements { get; }
            public List<IAsset> Assets { get; }

            private readonly XdAssetHolder _xdAssetHolder;
            private int _nextEid = 1;
            private readonly Dictionary<string, Rect> _size;

            public XdRenderer(XdArtboard xdArtboard, XdAssetHolder xdAssetHolder)
            {
                var resources = xdArtboard.Resources;

                _xdAssetHolder = xdAssetHolder;
                Elements = new List<IElement>();
                Assets = new List<IAsset>();
                Name = xdArtboard.Name;
                _size = new Dictionary<string, Rect>();

                var xdResourcesArtboardsJson = resources.Artboards[xdArtboard.Manifest.Path.Replace("artboard-", "")];
                var rootSize = new Vector2(xdResourcesArtboardsJson.Width, xdResourcesArtboardsJson.Height);
                CalcPosition(xdArtboard.Artboard.Children.SelectMany(x => x.Artboard.Children).ToArray(), rootSize, Vector2.zero);
                var children = Render(xdArtboard.Artboard.Children.SelectMany(x => x.Artboard.Children).ToArray());
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

            private string[] CalcPosition(XdObjectJson[] xdObjects, Vector2 rootSize, Vector2 parentPosition)
            {
                return xdObjects.Select(xdObject => CalcPosition(xdObject, rootSize, parentPosition)).ToArray();
            }

            private string CalcPosition(XdObjectJson xdObject, Vector2 rootSize, Vector2 parentPosition)
            {
                var position = new Vector2((xdObject.Transform?.Tx ?? 0f) + parentPosition.x, (xdObject.Transform?.Ty ?? 0f) + parentPosition.y);

                var children = new string[] { };
                if (xdObject.Group != null)
                {
                    children = CalcPosition(xdObject.Group.Children, rootSize, position);
                }

                foreach (var parser in Parsers)
                {
                    if (parser.Is(xdObject))
                    {
                        var size = parser.CalcSize(xdObject, position);
                        size.position -= rootSize / 2f;
                        _size[xdObject.Id] = size;
                        return xdObject.Id;
                    }
                }

                if (xdObject.Type == "group")
                {
                    var childrenRects = children.Select(x => _size[x]).ToArray();
                    var minX = childrenRects.Length > 0 ? childrenRects.Min(x => x.min.x) : 0f;
                    var minY = childrenRects.Length > 0 ? childrenRects.Min(x => x.min.y) : 0f;
                    var maxX = childrenRects.Length > 0 ? childrenRects.Max(x => x.max.x) : 0f;
                    var maxY = childrenRects.Length > 0 ? childrenRects.Max(x => x.max.y) : 0f;
                    var groupRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
                    _size[xdObject.Id] = groupRect;
                    foreach (var c in children)
                    {
                        var t = _size[c];
                        t.center -= groupRect.center;
                        _size[c] = t;
                    }
                    return xdObject.Id;
                }

                throw new Exception($"Unknown object type {xdObject.Type}");
            }

            private IElement[] Render(XdObjectJson[] xdObjects)
            {
                var children = new List<IElement>();
                foreach (var xdObject in xdObjects) children.AddRange(Render(xdObject));
                return children.ToArray();
            }

            private IElement[] Render(XdObjectJson xdObject)
            {
                var eid = _nextEid;
                _nextEid++;

                var rect = _size[xdObject.Id];
                var position = new Vector2(rect.center.x, -rect.center.y);
                var size = rect.size;
                var children = new IElement[] { };
                if (xdObject.Group != null)
                {
                    children = Render(xdObject.Group.Children);
                }

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

                foreach (var parser in Parsers)
                {
                    if (parser.Is(xdObject))
                    {
                        var (components, assets) = parser.Render(xdObject, size, _xdAssetHolder);

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

                        Assets.AddRange(assets);
                        Elements.Add(element);
                        return new IElement[] { element };
                    }
                }

                if (xdObject.Type == "group")
                {
                    var group = new ObjectElement(
                        eid,
                        xdObject.Name,
                        position,
                        size,
                        anchorX,
                        anchorY,
                        new IComponent[] { },
                        children.Select(x => x.Eid).ToArray()
                    );

                    Elements.Add(group);
                    return new IElement[] { group };
                }

                throw new Exception($"Unknown object type {xdObject.Type}");
            }
        }
    }
}