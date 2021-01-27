using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Xd.Libraries;
using Newtonsoft.Json;
using Unity.VectorGraphics;
using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ShapeObjectParser : IXdObjectParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.Type == "shape";
        }

        public Rect CalcSize(XdObjectJson xdObject)
        {
            var position = Vector2.zero;
            var size = new Vector2(xdObject.Shape.Width, xdObject.Shape.Height);
            var scaleBehavior = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.ScaleBehavior ?? "fill";
            var spriteUid = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.Uid;

            var shapeType = xdObject.Shape?.Type;
            if (!string.IsNullOrWhiteSpace(spriteUid))
            {
                // nothing
            }
            else if (SvgUtil.Types.Contains(shapeType))
            {
                var svg = SvgUtil.CreateSvg(xdObject);
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
                    if (bounds.width > 0.0001f && bounds.height > 0.0001f)
                    {
                        size = new Vector2(bounds.width, bounds.height);
                        position = new Vector2(bounds.x, bounds.y);
                    }
                }
            }

            if (scaleBehavior == "cover")
            {
                var imageWidth = xdObject.Style?.Fill?.Pattern?.Width ?? 0f;
                var imageHeight = xdObject.Style?.Fill?.Pattern?.Height ?? 0f;
                var imageSize = new Vector2(imageWidth, imageHeight);
                var imageAspect = imageSize.x / imageSize.y;
                var instanceAspect = size.x / size.y;
                var offsetX = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.OffsetX ?? 0f;
                var offsetY = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.OffsetY ?? 0f;
                var scale = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.Scale ?? 1.0f;

                if (imageAspect > instanceAspect)
                {
                    var prev = size.x;
                    size.x = size.y * (imageSize.x / imageSize.y);
                    position.x -= (size.x - prev) / 2f;

                    position.x += offsetX * xdObject.Shape.Width * imageAspect / instanceAspect;
                    position.y += offsetY * xdObject.Shape.Height;
                }
                else
                {
                    var prev = size.y;
                    size.y = size.x * (imageSize.y / imageSize.x);
                    position.y -= (size.y - prev) / 2f;

                    position.x += offsetX * xdObject.Shape.Width;
                    position.y += offsetY * xdObject.Shape.Height * imageAspect / instanceAspect;
                }

                {
                    var prev = size;
                    size *= scale;
                    position -= (size - prev) / 2f;
                }
            }

            return new Rect(position, size);

        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder)
        {
            var (imageComponent, assets) = RenderImage(xdObject, obb, assetHolder);
            return (new IComponent[] { imageComponent }, assets);
        }

        public static (ImageComponent, IAsset[]) RenderImage(XdObjectJson xdObject, Obb obb, XdAssetHolder assetHolder)
        {
            ImageComponent imageComponent = null;
            SpriteAsset asset = null;

            var color = xdObject.GetFillUnityColor();
            var ux = xdObject.Style?.Fill?.Pattern?.Meta?.Ux;
            var spriteUid = ux?.Uid;
            var flipX = ux?.FlipX ?? false;
            var flipY = ux?.FlipY ?? false;
            var direction = new Vector2Int(flipX ? -1 : 1, flipY ? -1 : 1);
            var shapeType = xdObject.Shape?.Type;

            if (!string.IsNullOrWhiteSpace(spriteUid))
            {
                spriteUid = $"{xdObject.GetSimpleName()}_{spriteUid.Substring(0, 8)}.png";
                asset = new SpriteAsset(spriteUid, xdObject.Style.Fill.Pattern.Meta.Ux.HrefLastModifiedDate, null);
                imageComponent = new ImageComponent(
                    spriteUid,
                    color,
                    direction
                );
                assetHolder.Save(spriteUid, xdObject.Style.Fill.Pattern.Meta);
            }
            else if (SvgUtil.Types.Contains(shapeType))
            {
                spriteUid = $"{xdObject.GetSimpleName()}_{xdObject.Id.Substring(0, 8)}.svg";
                var svg = SvgUtil.CreateSvg(xdObject);
                var userData = new SvgImportTrigger.SvgImportUserData { Width = Mathf.RoundToInt(obb.Size.x), Height = Mathf.RoundToInt(obb.Size.y) };
                asset = new SpriteAsset(spriteUid, FastHash.CalculateHash(svg), JsonConvert.SerializeObject(userData));
                imageComponent = new ImageComponent(
                    spriteUid,
                    new Color(1f, 1f, 1f, color.a),
                    direction
                );

                assetHolder.Save(spriteUid, System.Text.Encoding.UTF8.GetBytes(svg));
            }
            else
            {
                Debug.LogError($"Unknown shape type {shapeType}");
            }

            var assets = new List<IAsset>();
            if (!xdObject.HasParameter("placeholder") && asset != null) assets.Add(asset);
            return (imageComponent, assets.ToArray());
        }
    }
}