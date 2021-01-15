using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor;
using AkyuiUnity.Loader;
using Newtonsoft.Json;
using Unity.VectorGraphics;
using Unity.VectorGraphics.Editor;
using UnityEditor;
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
            var loaders = xdFilePaths
                .Select(x => new XdFile(x))
                .SelectMany(x => x.Artworks.Select((y => (x, y))))
                .Where(x => x.y.Name != "pasteboard")
                .Select(x => (IAkyuiLoader) new XdAkyuiLoader(x.x, x.y))
                .ToArray();
            Importer.Import(settings, loaders);
            foreach (var loader in loaders) loader.Dispose();
        }
    }

    public class XdAkyuiLoader : IAkyuiLoader
    {
        private XdFile _xdFile;
        private readonly Dictionary<string, XdStyleFillPatternMetaJson> _fileNameToMeta;
        private readonly Dictionary<string, byte[]> _fileNameToBytes;

        public XdAkyuiLoader(XdFile xdFile, XdArtboard xdArtboard)
        {
            _xdFile = xdFile;
            (LayoutInfo, AssetsInfo, _fileNameToMeta, _fileNameToBytes) = Create(xdArtboard);
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
            if (_fileNameToMeta.ContainsKey(fileName))
            {
                var meta = _fileNameToMeta[fileName];
                return _xdFile.GetResource(meta);
            }

            if (_fileNameToBytes.ContainsKey(fileName))
            {
                return _fileNameToBytes[fileName];
            }

            throw new Exception($"Unknown asset {fileName}");
        }

        private (LayoutInfo, AssetsInfo, Dictionary<string, XdStyleFillPatternMetaJson>, Dictionary<string, byte[]>) Create(XdArtboard xdArtboard)
        {
            var renderer = new XdRenderer(xdArtboard);
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
            return (layoutInfo, assetsInfo, renderer.FileNameToMeta, renderer.FileNameToBytes);
        }

        private class XdRenderer
        {
            public string Name { get; }
            public long Hash => Random.Range(0, 100000); // 今は必ず更新する
            public Meta Meta => new Meta(Const.AkyuiVersion, "XdToAkyui", "0.0.0");
            public int Root => 0;
            public List<IElement> Elements { get; }
            public List<IAsset> Assets { get; }
            public Dictionary<string, XdStyleFillPatternMetaJson> FileNameToMeta { get; }
            public Dictionary<string, byte[]> FileNameToBytes { get; }

            private int _nextEid = 1;
            private Dictionary<string, Rect> _size;

            public XdRenderer(XdArtboard xdArtboard)
            {
                var resources = xdArtboard.Resources;

                Name = xdArtboard.Name;
                Elements = new List<IElement>();
                Assets = new List<IAsset>();
                FileNameToMeta = new Dictionary<string, XdStyleFillPatternMetaJson>();
                FileNameToBytes = new Dictionary<string, byte[]>();
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

            private string CreateSvg(XdObjectJson xdObject)
            {
                var svgArgs = new List<string>();
                var fill = xdObject.Style?.Fill;
                if (fill != null)
                {
                    var color = new Color32((byte) fill.Color.Value.R, (byte) fill.Color.Value.G, (byte) fill.Color.Value.B, 255);
                    svgArgs.Add($@"fill=""#{ColorUtility.ToHtmlStringRGB(color)}""");
                }
                var svg = $@"<svg><path d=""{xdObject.Shape.Path}"" {string.Join(" ", svgArgs)} /></svg>";
                return svg;
            }

            private string CalcPosition(XdObjectJson xdObject, Vector2 rootSize, Vector2 parentPosition)
            {
                var position = new Vector2((xdObject.Transform?.Tx ?? 0f) + parentPosition.x, (xdObject.Transform?.Ty ?? 0f) + parentPosition.y);

                var children = new string[] { };
                if (xdObject.Group != null)
                {
                    children = CalcPosition(xdObject.Group.Children, rootSize, position);
                }

                if (xdObject.Type == "shape")
                {
                    var size = new Vector2(xdObject.Shape.Width, xdObject.Shape.Height);

                    var shapeType = xdObject.Shape?.Type;
                    if (shapeType == "path")
                    {
                        var svg = CreateSvg(xdObject);
                        using (var reader = new StringReader(svg))
                        {
                            var sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions.DontPreserve);
                            var tessOptions = new VectorUtils.TessellationOptions {
                                StepDistance = 100.0f,
                                MaxCordDeviation = 0.5f,
                                MaxTanAngleDeviation = 0.1f,
                                SamplingStepSize = 0.01f
                            };
                            var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
                            var vertices = geometry.SelectMany(geom => geom.Vertices.Select(x => (geom.WorldTransform * x))).ToArray();
                            var bounds = VectorUtils.Bounds(vertices);
                            size = new Vector2(bounds.width, bounds.height);
                            position = new Vector2(position.x + bounds.x, position.y + bounds.y);
                        }
                    }

                    position -= rootSize / 2f;
                    _size[xdObject.Id] = new Rect(position, size);
                    return xdObject.Id;
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

                if (xdObject.Type == "shape")
                {
                    var components = new List<IComponent>();

                    var spriteUid = xdObject.Style.Fill.Pattern?.Meta?.Ux?.Uid;
                    var shapeType = xdObject.Shape?.Type;

                    if (!string.IsNullOrWhiteSpace(spriteUid))
                    {
                        spriteUid = $"{spriteUid.Substring(0, 8)}.png";
                        Assets.Add(new SpriteAsset(spriteUid, Random.Range(0, 10000), null));
                        components.Add(new ImageComponent(
                            0,
                            spriteUid,
                            Color.white
                        ));
                        FileNameToMeta[spriteUid] = xdObject.Style.Fill.Pattern.Meta;
                    }
                    else if (shapeType == "path")
                    {
                        spriteUid = $"path_{xdObject.Id.Substring(0, 8)}.svg";
                        var userData = new SvgPostProcessImportAsset.SvgImportUserData { Width = Mathf.RoundToInt(size.x), Height = Mathf.RoundToInt(size.y) };
                        Assets.Add(new SpriteAsset(spriteUid, Random.Range(0, 10000), JsonConvert.SerializeObject(userData)));
                        components.Add(new ImageComponent(
                            0,
                            spriteUid,
                            Color.white
                        ));

                        var svg = CreateSvg(xdObject);
                        FileNameToBytes[spriteUid] = System.Text.Encoding.UTF8.GetBytes(svg);
                    }

                    var sprite = new ObjectElement(
                        eid,
                        xdObject.Name,
                        position,
                        size,
                        anchorX,
                        anchorY,
                        components.ToArray(),
                        children.Select(x => x.Eid).ToArray()
                    );

                    Elements.Add(sprite);
                    return new IElement[] { sprite };
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

    public class SvgPostProcessImportAsset : AssetPostprocessor
    {
        public void OnPreprocessAsset()
        {
            if (PostProcessImportAsset.ProcessingFile != assetPath) return;

            if (assetImporter is SVGImporter svgImporter)
            {
                var userData = JsonConvert.DeserializeObject<SvgImportUserData>(PostProcessImportAsset.UserData);
                svgImporter.SvgType = SVGType.TexturedSprite;
                svgImporter.KeepTextureAspectRatio = false;
                svgImporter.TextureWidth = userData.Width;
                svgImporter.TextureHeight = userData.Height;
            }
        }

        public class SvgImportUserData
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}