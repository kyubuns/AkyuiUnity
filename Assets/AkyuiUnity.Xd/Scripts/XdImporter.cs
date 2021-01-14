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

        public XdAkyuiLoader(XdFile xdFile, XdArtboard xdArtboard)
        {
            _xdFile = xdFile;
            (LayoutInfo, AssetsInfo, _fileNameToMeta) = Create(xdArtboard);
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
            var meta = _fileNameToMeta[fileName];
            return _xdFile.GetResource(meta);
        }

        private (LayoutInfo, AssetsInfo, Dictionary<string, XdStyleFillPatternMetaJson>) Create(XdArtboard xdArtboard)
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
            return (layoutInfo, assetsInfo, renderer.FileNameToMeta);
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

            private readonly XdResourcesJson _resources;
            private int _nextEid = 1;

            public XdRenderer(XdArtboard xdArtboard)
            {
                _resources = xdArtboard.Resources;

                Name = xdArtboard.Name;
                Elements = new List<IElement>();
                Assets = new List<IAsset>();
                FileNameToMeta = new Dictionary<string, XdStyleFillPatternMetaJson>();

                var xdResourcesArtboardsJson = _resources.Artboards[xdArtboard.Manifest.Path.Replace("artboard-", "")];
                var rootSize = new Vector2(xdResourcesArtboardsJson.Width, xdResourcesArtboardsJson.Height);
                var children = Render(xdArtboard.Artboard.Children.SelectMany(x => x.Artboard.Children).ToArray(), rootSize);
                var root = new ObjectElement(
                    0,
                    xdArtboard.Name,
                    Vector2.zero,
                    rootSize,
                    AnchorXType.Center,
                    AnchorYType.Middle,
                    new IComponent[] { },
                    children
                );
                Elements.Add(root);
            }

            private int[] Render(XdObjectJson[] xdObjects, Vector2 rootSize)
            {
                var children = new List<int>();
                foreach (var xdObject in xdObjects) children.AddRange(Render(xdObject, rootSize));
                return children.ToArray();
            }

            private int[] Render(XdObjectJson xdObject, Vector2 rootSize)
            {
                var eid = _nextEid;
                _nextEid++;

                var children = new int[] { };
                if (xdObject.Group != null)
                {
                    children = Render(xdObject.Group.Children, rootSize);
                }

                if (xdObject.Type == "shape")
                {
                    var components = new List<IComponent>();
                    var spriteUid = xdObject.Style.Fill.Pattern.Meta?.Ux?.Uid;
                    if (!string.IsNullOrWhiteSpace(spriteUid))
                    {
                        spriteUid = $"{spriteUid}.png";
                        Assets.Add(new SpriteAsset(spriteUid, Random.Range(0, 10000)));
                        components.Add(new ImageComponent(
                            0,
                            spriteUid,
                            Color.white
                        ));
                        FileNameToMeta[spriteUid] = xdObject.Style.Fill.Pattern.Meta;
                    }

                    var size = new Vector2(xdObject.Shape.Width, xdObject.Shape.Height);
                    var position = new Vector2(xdObject.Transform.Tx - rootSize.x / 2f + size.x / 2f, -(xdObject.Transform.Ty - rootSize.y / 2f + size.y / 2f));
                    var sprite = new ObjectElement(
                        eid,
                        xdObject.Name,
                        position,
                        size,
                        AnchorXType.Center,
                        AnchorYType.Middle,
                        components.ToArray(),
                        children
                    );

                    Elements.Add(sprite);
                }
                else if (xdObject.Type == "group")
                {
                    var sprite = new ObjectElement(
                        eid,
                        xdObject.Name,
                        Vector2.zero,
                        Vector2.zero,
                        AnchorXType.Center,
                        AnchorYType.Middle,
                        new IComponent[] { },
                        children
                    );

                    Elements.Add(sprite);
                }
                else
                {
                    throw new Exception($"Unknown object type {xdObject.Type}");
                }
                return new[] { eid };
            }
        }
    }
}