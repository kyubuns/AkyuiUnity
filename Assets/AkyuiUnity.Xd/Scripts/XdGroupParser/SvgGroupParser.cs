using System.Collections.Generic;
using System.Linq;
using AkyuiUnity.Xd.Libraries;
using Newtonsoft.Json;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class SvgGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            return xdObject.GetParameters().Contains("vector");
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter)
        {
            var components = new List<IComponent>();
            var assets = new List<IAsset>();

            var spriteUid = $"{xdObject.GetSimpleName()}_{xdObject.Id.Substring(0, 8)}.svg";
            var svg = SvgUtil.CreateSvg(GetAllChildren(children));
            children = new XdObjectJson[] { };
            var size = sizeGetter.Get(xdObject).size;
            var userData = new SvgImportTrigger.SvgImportUserData { Width = Mathf.RoundToInt(size.x), Height = Mathf.RoundToInt(size.y) };
            assets.Add(new SpriteAsset(spriteUid, FastHash.CalculateHash(svg), JsonConvert.SerializeObject(userData)));
            components.Add(new ImageComponent(
                0,
                spriteUid,
                Color.white
            ));

            assetHolder.Save(spriteUid, System.Text.Encoding.UTF8.GetBytes(svg));
            return (components.ToArray(), assets.ToArray());
        }

        private XdObjectJson[] GetAllChildren(XdObjectJson[] xdObjects)
        {
            var all = new List<XdObjectJson>();
            foreach (var xdObject in xdObjects)
            {
                if (xdObject.Group != null)
                {
                    all.AddRange(GetAllChildren(xdObject.Group.Children));
                }
                else
                {
                    all.Add(xdObject);
                }
            }
            return all.ToArray();
        }
    }
}