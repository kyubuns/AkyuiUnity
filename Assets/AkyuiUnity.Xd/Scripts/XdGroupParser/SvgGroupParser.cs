using System.Collections.Generic;
using AkyuiUnity.Xd.Libraries;
using UnityEngine;
using XdParser;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class SvgGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.HasParameter("vector");
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            var components = new List<IComponent>();
            var assets = new List<IAsset>();

            var color = xdObject.GetFillUnityColor();
            var svg = SvgUtil.CreateSvg(xdObject);
            xdObject.Group.Children = new XdObjectJson[] { };

            var size = obbGetter.Get(xdObject).Size;
            var spriteUid = $"{xdObject.GetSimpleName()}_{xdObject.Id.Substring(0, 8)}.png";
            var svgHash = FastHash.CalculateHash(svg);

            var cachedSvg = assetHolder.GetCachedSvg(svgHash);
            if (cachedSvg != null)
            {
                spriteUid = cachedSvg.SpriteUid;
            }
            else
            {
                assets.Add(new SpriteAsset(spriteUid, svgHash, size, null, null));
                assetHolder.Save(spriteUid, SvgToPng.Convert(svg, size));
                assetHolder.SaveCacheSvg(spriteUid, svgHash);
            }
            components.Add(new ImageComponent(
                spriteUid,
                new Color(1f, 1f, 1f, color.a),
                Vector2Int.one
            ));

            return (components.ToArray(), assets.ToArray());
        }
    }
}