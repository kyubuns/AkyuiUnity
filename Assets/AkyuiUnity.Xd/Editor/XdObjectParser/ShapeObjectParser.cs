using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Loader;
using Unity.VectorGraphics;
using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ShapeObjectParser : IXdObjectParser
    {
        bool IXdObjectParser.Is(XdObjectJson xdObject)
        {
            return Is(xdObject);
        }

        public static bool Is(XdObjectJson xdObject)
        {
            return xdObject.Type == "shape";
        }

        Rect IXdObjectParser.CalcSize(XdObjectJson xdObject)
        {
            return CalcSize(xdObject);
        }

        public static Rect CalcSize(XdObjectJson xdObject)
        {
            var position = Vector2.zero;
            var size = Vector2.zero;

            var xdObjectShape = xdObject.Shape;
            var shapeType = xdObjectShape.Type;
            if (SvgUtil.Types.Contains(shapeType))
            {
                var svg = SvgUtil.CreateSvg(xdObject, null, true);
                var bounds = SvgUtil.CalcBounds(svg);
                if (bounds.width > 0.0001f && bounds.height > 0.0001f)
                {
                    size = new Vector2(bounds.width, bounds.height);
                    position = new Vector2(bounds.x, bounds.y);
                }
            }

            return new Rect(position, size);
        }

        public (IComponent[], IAsset[], IElement[]) Render(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder)
        {
            var scaleBehavior = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.ScaleBehavior ?? "fill";
            if (scaleBehavior == "cover" && obb.Size.x > 0.0001f && obb.Size.y > 0.0001f)
            {
                var originalSize = obb.Size;
                var imageWidth = xdObject.Style?.Fill?.Pattern?.Width ?? 1f;
                var imageHeight = xdObject.Style?.Fill?.Pattern?.Height ?? 1f;
                var imageSize = new Vector2(imageWidth, imageHeight);
                var imageAspect = imageSize.x / imageSize.y;
                var offsetX = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.OffsetX ?? 0f;
                var offsetY = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.OffsetY ?? 0f;
                var scale = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.Scale ?? 1.0f; // Widthに対するスケール

                var innerImageSize = new Vector2(
                    originalSize.x * scale,
                    originalSize.x * scale / imageAspect
                );
                var innerImagePosition = new Vector2(
                    originalSize.x * offsetX,
                    originalSize.x / imageAspect * offsetY
                );

                var (innerImage, innerAssets) = RenderImage(xdObject, obb, assetHolder, false);
                var (maskImage, maskAssets) = RenderImage(xdObject, obb, assetHolder, true);
                var eid = FastHash.CalculateHash((xdObject.Guid ?? xdObject.Id) + "Image");
                return (new IComponent[] { new MaskComponent(maskImage.Sprite) }, maskAssets.Concat(innerAssets).ToArray(), new IElement[]
                {
                    new ObjectElement(
                        eid,
                        "Inner Image",
                        innerImagePosition,
                        innerImageSize,
                        AnchorXType.Center,
                        AnchorYType.Middle,
                        0f,
                        true,
                        new IComponent[] { innerImage },
                        new uint[] { }
                    )
                });
            }

            var (imageComponent, assets) = RenderImage(xdObject, obb, assetHolder, false);
            return (new IComponent[] { imageComponent }, assets, new IElement[] { });
        }

        public static (ImageComponent, IAsset[]) RenderImage(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder, bool ignoreUid)
        {
            ImageComponent imageComponent = null;
            SpriteAsset asset = null;

            var color = xdObject.GetFillUnityColor();
            var ux = xdObject.Style?.Fill?.Pattern?.Meta?.Ux;
            var flipX = ux?.FlipX ?? false;
            var flipY = ux?.FlipY ?? false;
            var direction = new Vector2Int(flipX ? -1 : 1, flipY ? -1 : 1);
            var shapeType = xdObject.Shape?.Type;
            var border = xdObject.HasParameter("NoSlice") ? new Border(0, 0, 0, 0) : null;
            var isPlaceholder = xdObject.HasParameter("placeholder");
            var uxUid = ignoreUid ? null : ux?.Uid ?? "";

            if (!string.IsNullOrWhiteSpace(uxUid))
            {
                string spriteUid = null;
                if (!isPlaceholder)
                {
                    spriteUid = $"{xdObject.GetSimpleName()}_{uxUid.Substring(0, 8)}.png";
                    asset = new SpriteAsset(spriteUid, xdObject.Style.Fill.Pattern.Meta.Ux.HrefLastModifiedDate, obb.Size, null, border);
                    assetHolder.Save(spriteUid, xdObject.Style.Fill.Pattern.Meta);
                }
                imageComponent = new ImageComponent(
                    spriteUid,
                    color,
                    direction
                );
            }
            else if (SvgUtil.Types.Contains(shapeType))
            {
                string spriteUid = null;
                if (!isPlaceholder)
                {
                    spriteUid = $"{xdObject.GetSimpleName()}_{xdObject.Id.Substring(0, 8)}.png";
                    var svg = SvgUtil.CreateSvg(xdObject, null, false);
                    var svgHash = FastHash.CalculateHash(svg);

                    var cachedSvg = assetHolder.GetCachedSvg(svgHash);
                    if (cachedSvg != null)
                    {
                        spriteUid = cachedSvg.SpriteUid;
                    }
                    else
                    {
                        asset = new SpriteAsset(spriteUid, svgHash, obb.Size, null, border);
                        var xdImportSettings = XdImporter.Settings;
                        assetHolder.Save(spriteUid, () => SvgToPng.Convert(svg, obb.Size, ViewportOptions.DontPreserve, xdImportSettings));
                        assetHolder.SaveCacheSvg(spriteUid, svgHash);
                    }
                }
                imageComponent = new ImageComponent(
                    spriteUid,
                    new Color(1f, 1f, 1f, xdObject.Style?.Opacity ?? 1f),
                    direction
                );
            }
            else
            {
                Debug.LogError($"Unknown shape type {shapeType} in {xdObject.Name}({xdObject.Id}, {xdObject.Guid})");
            }

            var assets = new List<IAsset>();
            if (!isPlaceholder && asset != null) assets.Add(asset);
            return (imageComponent, assets.ToArray());
        }
    }
}