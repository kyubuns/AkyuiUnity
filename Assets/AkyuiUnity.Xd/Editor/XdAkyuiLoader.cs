using System;
using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Loader;
using JetBrains.Annotations;
using UnityEngine;
using Utf8Json;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class XdAkyuiLoader : IAkyuiLoader
    {
        private XdFile _xdFile;
        private readonly IXdObjectParser[] _objectParsers;
        private readonly IXdGroupParser[] _groupParsers;
        private readonly AkyuiXdImportTrigger[] _triggers;
        private readonly XdAssetHolder _assetHolder;

        private static readonly IXdObjectParser[] DefaultObjectParsers =
        {
            new ScrollbarObjectParser(), // ShapeObjectParserより前
            new ShapeObjectParser(),
            new TextObjectParser(),
        };

        private static readonly IXdGroupParser[] DefaultGroupParsers =
        {
            new ButtonGroupParser(),
            new RepeatGridGroupParser(),
            new ScrollGroupParser(),
            new InputFieldGroupParser(),

            new SvgGroupParser(),
            new AlphaGroupParser(), // SvgGroupParserより後
            new MaskGroupParser(),
        };

        public XdAkyuiLoader(XdFile xdFile, XdArtboard xdArtboard, string name, Dictionary<string, string> userData, AkyuiXdObjectParser[] objectParsers, AkyuiXdGroupParser[] groupParsers,
            AkyuiXdImportTrigger[] triggers)
        {
            _xdFile = xdFile;
            _objectParsers = objectParsers.Concat(DefaultObjectParsers).ToArray();
            _groupParsers = groupParsers.Concat(DefaultGroupParsers).ToArray();
            _triggers = triggers;
            _assetHolder = new XdAssetHolder(_xdFile);
            (LayoutInfo, AssetsInfo) = Create(xdArtboard, _assetHolder, name, userData);
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

        private (LayoutInfo, AssetsInfo) Create(XdArtboard xdArtboard, XdAssetHolder assetHolder, string name, Dictionary<string, string> userData)
        {
            var renderer = new XdRenderer(xdArtboard, assetHolder, _objectParsers, _groupParsers, _triggers);

            var layoutInfoForCalcHash = new LayoutInfo(
                name,
                0,
                renderer.Meta,
                new Dictionary<string, string>(),
                renderer.Root,
                renderer.Elements.ToArray()
            );
            var hash = FastHash.CalculateHash(JsonSerializer.Serialize(AkyuiCompressor.ToSerializable(layoutInfoForCalcHash)));

            var layoutInfo = new LayoutInfo(
                name,
                hash,
                renderer.Meta,
                userData,
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
            public Meta Meta => new Meta(Const.AkyuiVersion, "AkyuiUnity.Xd", "0.1.0");
            public uint Root => 0;
            public List<IElement> Elements { get; }
            public List<IAsset> Assets { get; }

            private readonly XdAssetHolder _xdAssetHolder;
            private readonly IXdObjectParser[] _objectParsers;
            private readonly IXdGroupParser[] _groupParsers;
            private readonly ObbHolder _obbHolder;
            private Dictionary<string, XdObjectJson> _sourceGuidToObject;

            public XdRenderer(XdArtboard xdArtboard, XdAssetHolder xdAssetHolder, IXdObjectParser[] objectParsers, IXdGroupParser[] groupParsers, AkyuiXdImportTrigger[] triggers)
            {
                var resources = xdArtboard.Resources;
                _xdAssetHolder = xdAssetHolder;
                _objectParsers = objectParsers;
                _groupParsers = groupParsers;
                Elements = new List<IElement>();
                Assets = new List<IAsset>();
                _obbHolder = new ObbHolder();

                CreateRefObjectMap(resources.Resources);

                var xdResourcesArtboardsJson = resources.Artboards[xdArtboard.Manifest.Path.Replace("artboard-", "")];
                var rootObb = new Obb { Size = new Vector2(xdResourcesArtboardsJson.Width, xdResourcesArtboardsJson.Height) };

                var rootArtboard = xdArtboard.Artboard.Children[0];
                var xdObjectJsons = rootArtboard.Artboard.Children;
                var convertedXdObjectJsons = ConvertRefObject(xdObjectJsons, triggers);
                var childrenObbs = CalcPosition(convertedXdObjectJsons, rootObb, new XdObjectJson[] { });
                foreach (var childObb in childrenObbs) childObb.LocalLeftTopPosition -= new Vector2(xdResourcesArtboardsJson.X, xdResourcesArtboardsJson.Y);
                var children = Render(convertedXdObjectJsons, rootObb, new XdObjectJson[] { });

                var rootComponents = new List<IComponent>();
                if (rootArtboard.Style?.Fill != null && rootArtboard.Style.Fill.Type == "solid")
                {
                    var color = rootArtboard.GetFillUnityColor();
                    rootComponents.Add(new ImageComponent(null, color, Vector2Int.one));
                }

                var root = new ObjectElement(
                    0,
                    xdArtboard.Name,
                    Vector2.zero,
                    rootObb.Size,
                    AnchorXType.Center,
                    AnchorYType.Middle,
                    0f,
                    true,
                    rootComponents.ToArray(),
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

            private XdObjectJson ConvertRefObject(XdObjectJson xdObject, AkyuiXdImportTrigger[] triggers)
            {
                var newXdObjectJson = GetRefObject(xdObject, triggers);

                if (newXdObjectJson?.Group != null)
                {
                    newXdObjectJson.Group = new XdObjectGroupJson { Children = ConvertRefObject(newXdObjectJson.Group.Children, triggers) };
                }

                return newXdObjectJson;
            }

            private XdObjectJson[] ConvertRefObject(XdObjectJson[] xdObject, AkyuiXdImportTrigger[] triggers)
            {
                var a = new List<XdObjectJson>();
                foreach (var x in xdObject)
                {
                    var tmp = ConvertRefObject(x, triggers);
                    if (tmp != null) a.Add(tmp);
                }

                return a.ToArray();
            }

            private XdObjectJson GetRefObject(XdObjectJson xdObject, AkyuiXdImportTrigger[] triggers)
            {
                var newXdObjectJson = xdObject;

                if (xdObject.Type == "syncRef")
                {
                    newXdObjectJson = new XdObjectJson();
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
                    newXdObjectJson.Shape = source.Shape; // compoundのchildrenだけ上書きされるケースがあるが計算出来ないので戻す
                }

                foreach (var trigger in triggers)
                {
                    newXdObjectJson = trigger.OnCreateXdObject(newXdObjectJson);
                    if (newXdObjectJson == null) break;
                }

                return newXdObjectJson;
            }

            // CalcPositionは子供から確定させていく
            private Obb[] CalcPosition(XdObjectJson[] xdObjects, Obb parentObb, XdObjectJson[] parents)
            {
                var obbList = new List<Obb>();
                foreach (var xdObject in xdObjects)
                {
                    try
                    {
                        obbList.Add(CalcPosition(xdObject, parentObb, parents));
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"Cause exception in CalcPosition {xdObject.Name}(id: {xdObject.Id}, guid: {xdObject.Guid})");
                        throw;
                    }
                }
                return obbList.ToArray();
            }

            private Obb CalcPosition(XdObjectJson xdObject, Obb parentObb, XdObjectJson[] parents)
            {
                var obb = new Obb
                {
                    Parent = parentObb,
                    LocalLeftTopPosition = new Vector2(xdObject.Transform?.Tx ?? 0f, xdObject.Transform?.Ty ?? 0f),
                    Rotation = xdObject.Meta?.Ux?.Rotation ?? 0f
                    // サイズは子供のサイズが無いと決まらない
                };

                var children = new Obb[] { };
                if (xdObject.Group != null)
                {
                    children = CalcPosition(xdObject.Group.Children, obb, parents.Concat(new[] { xdObject }).ToArray());
                }

                foreach (var parser in _objectParsers)
                {
                    if (!parser.Is(xdObject)) continue;
                    var rect = parser.CalcSize(xdObject);

                    obb.ApplyRect(rect);
                    foreach (var child in children) child.LocalLeftTopPosition -= rect.position;
                    _obbHolder.Set(xdObject, obb);
                    return obb;
                }

                if (xdObject.Type == "group")
                {
                    var rect = Obb.MinMaxRect(children);

                    foreach (var parser in _groupParsers)
                    {
                        if (!parser.Is(xdObject, parents)) continue;
                        rect = parser.CalcSize(xdObject, rect);
                        break;
                    }

                    obb.ApplyRect(rect);
                    foreach (var child in children) child.LocalLeftTopPosition -= rect.position;
                    _obbHolder.Set(xdObject, obb);
                    return obb;
                }

                throw new Exception($"Unknown object type {xdObject.Type}");
            }

            private IElement[] Render(XdObjectJson[] xdObjects, [CanBeNull] Obb parentObb, XdObjectJson[] parents)
            {
                var children = new List<IElement>();
                foreach (var xdObject in xdObjects)
                {
                    try
                    {
                        children.AddRange(Render(xdObject, parentObb, parents));
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"Cause exception in Render {xdObject.Name}(id: {xdObject.Id}, guid: {xdObject.Guid})");
                        throw;
                    }
                }

                return children.ToArray();
            }

            // Renderは親から子供の順番
            private IElement[] Render(XdObjectJson xdObject, [CanBeNull] Obb parentObb, XdObjectJson[] parents)
            {
                var eid = FastHash.CalculateHash(xdObject.Guid ?? xdObject.Id);
                var originalObb = _obbHolder.Get(xdObject);
                var obb = originalObb.CalcObbInWorld(parentObb);
                var position = obb.CalcLocalRect().center - (parentObb?.Size ?? Vector2.zero) / 2f;
                var size = obb.Size;
                var anchorX = AnchorXType.Center;
                var anchorY = AnchorYType.Middle;
                var rotation = obb.Rotation;
                if (Mathf.Abs(rotation) < 0.0001f) rotation = 0f;
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
                    var (components, assets) = parser.Render(xdObject, obb, _xdAssetHolder);

                    var children = new IElement[] { };
                    if (xdObject.Group != null) children = Render(xdObject.Group.Children, originalObb, parents.Concat(new[] { xdObject }).ToArray());

                    var element = new ObjectElement(
                        eid,
                        xdObject.GetSimpleName(),
                        position,
                        size,
                        anchorX,
                        anchorY,
                        rotation,
                        xdObject.Visible ?? true,
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
                    foreach (var parser in _groupParsers)
                    {
                        if (!parser.Is(xdObject, parents)) continue;
                        var (c, assets) = parser.Render(xdObject, _xdAssetHolder, _obbHolder);
                        components.AddRange(c);

                        foreach (var asset in assets)
                        {
                            if (Assets.Any(x => x.FileName == asset.FileName)) continue;
                            Assets.Add(asset);
                        }

                        break;
                    }

                    var generatedChildren = new IElement[] { };
                    if (xdObject.Group != null)
                    {
                        generatedChildren = Render(xdObject.Group.Children, originalObb, parents.Concat(new[] { xdObject }).ToArray());
                    }

                    var group = new ObjectElement(
                        eid,
                        xdObject.GetSimpleName(),
                        position,
                        size,
                        anchorX,
                        anchorY,
                        rotation,
                        xdObject.Visible ?? true,
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