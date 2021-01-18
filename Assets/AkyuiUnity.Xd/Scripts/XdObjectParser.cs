using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.VectorGraphics;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public interface IXdObjectParser
    {
        bool Is(XdObjectJson xdObject);
        Rect CalcSize(XdObjectJson xdObject, Vector2 position);
        (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, Dictionary<string, XdStyleFillPatternMetaJson> fileNameToMeta, Dictionary<string, byte[]> fileNameToBytes);
    }

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

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, Vector2 size, Dictionary<string, XdStyleFillPatternMetaJson> fileNameToMeta, Dictionary<string, byte[]> fileNameToBytes)
        {
            var components = new List<IComponent>();
            var assets = new List<IAsset>();

            var color = Color.white;
            color.a = xdObject.Style?.Opacity ?? 1f;

            var spriteUid = xdObject.Style?.Fill?.Pattern?.Meta?.Ux?.Uid;
            var shapeType = xdObject.Shape?.Type;

            if (!string.IsNullOrWhiteSpace(spriteUid))
            {
                spriteUid = $"{spriteUid.Substring(0, 8)}.png";
                assets.Add(new SpriteAsset(spriteUid, Random.Range(0, 10000), null));
                components.Add(new ImageComponent(
                    0,
                    spriteUid,
                    color
                ));
                fileNameToMeta[spriteUid] = xdObject.Style.Fill.Pattern.Meta;
            }
            else if (SvgUtil.Types.Contains(shapeType))
            {
                spriteUid = $"path_{xdObject.Id.Substring(0, 8)}.svg";
                var userData = new SvgPostProcessImportAsset.SvgImportUserData { Width = Mathf.RoundToInt(size.x), Height = Mathf.RoundToInt(size.y) };
                assets.Add(new SpriteAsset(spriteUid, Random.Range(0, 10000), JsonConvert.SerializeObject(userData)));
                components.Add(new ImageComponent(
                    0,
                    spriteUid,
                    color
                ));

                var svg = SvgUtil.CreateSvg(xdObject);
                fileNameToBytes[spriteUid] = System.Text.Encoding.UTF8.GetBytes(svg);
            }

            return (components.ToArray(), assets.ToArray());
        }
    }
}