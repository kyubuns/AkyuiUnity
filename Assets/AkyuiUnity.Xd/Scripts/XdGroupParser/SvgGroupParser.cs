using System.Collections.Generic;
using AkyuiUnity.Xd.Libraries;
using Newtonsoft.Json;
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

            var spriteUid = $"{xdObject.GetSimpleName()}_{xdObject.Id.Substring(0, 8)}.svg";
            var color = xdObject.GetFillUnityColor();
            var svg = SvgUtil.CreateSvg(xdObject);
            xdObject.Group.Children = new XdObjectJson[] { };

            var size = obbGetter.Get(xdObject).Size;
            assets.Add(new SpriteAsset(spriteUid, FastHash.CalculateHash(svg), size, null));
            components.Add(new ImageComponent(
                spriteUid,
                new Color(1f, 1f, 1f, color.a),
                Vector2Int.one
            ));

            assetHolder.Save(spriteUid, System.Text.Encoding.UTF8.GetBytes(svg));
            return (components.ToArray(), assets.ToArray());
        }
    }
}