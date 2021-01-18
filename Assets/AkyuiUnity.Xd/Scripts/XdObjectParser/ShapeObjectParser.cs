using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Xd.Libraries;
using Newtonsoft.Json;
using Unity.VectorGraphics;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ShapeObjectParser : IXdObjectParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.Type == "shape";
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position)
        {
            var size = new Vector2(xdObject.Shape.Width, xdObject.Shape.Height);

            var shapeType = xdObject.Shape?.Type;
            if (SvgUtil.Types.Contains(shapeType))
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
                    size = new Vector2(bounds.width, bounds.height);
                    position = new Vector2(position.x + bounds.x, position.y + bounds.y);
                }
            }

            return new Rect(position, size);
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, XdAssetHolder assetHolder)
        {
            var components = new List<IComponent>();
            var assets = new List<IAsset>();

            var color = Color.white;
            color.a = xdObject.Style?.Opacity ?? 1f;

            var spriteUid = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.Uid;
            var shapeType = xdObject.Shape?.Type;

            if (!string.IsNullOrWhiteSpace(spriteUid))
            {
                spriteUid = $"{xdObject.Name}_{spriteUid.Substring(0, 8)}.png";
                assets.Add(new SpriteAsset(spriteUid, xdObject.Style.Fill.Pattern.Meta.Ux.HrefLastModifiedDate, null));
                components.Add(new ImageComponent(
                    0,
                    spriteUid,
                    color
                ));
                assetHolder.Save(spriteUid, xdObject.Style.Fill.Pattern.Meta);
            }
            else if (SvgUtil.Types.Contains(shapeType))
            {
                spriteUid = $"{xdObject.Name}_{xdObject.Id.Substring(0, 8)}.svg";
                var svg = SvgUtil.CreateSvg(xdObject);
                var userData = new SvgPostProcessImportAsset.SvgImportUserData { Width = Mathf.RoundToInt(size.x), Height = Mathf.RoundToInt(size.y) };
                assets.Add(new SpriteAsset(spriteUid, FastHash.CalculateHash(svg), JsonConvert.SerializeObject(userData)));
                components.Add(new ImageComponent(
                    0,
                    spriteUid,
                    color
                ));

                assetHolder.Save(spriteUid, System.Text.Encoding.UTF8.GetBytes(svg));
            }

            return (components.ToArray(), assets.ToArray());
        }
    }
}