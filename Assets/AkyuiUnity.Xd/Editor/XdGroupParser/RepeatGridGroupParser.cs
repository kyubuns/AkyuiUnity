using System;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class RepeatGridGroupParser : IXdGroupParser
    {
        bool IXdGroupParser.Is(XdObjectJson xdObject)
        {
            return Is(xdObject);
        }

        public static bool Is(XdObjectJson xdObject)
        {
            var repeatGrid = xdObject?.Meta?.Ux?.RepeatGrid;
            return repeatGrid != null;
        }

        public static bool Is(XdObjectJson xdObject, string scrollingType)
        {
            var repeatGrid = xdObject?.Meta?.Ux?.RepeatGrid;
            if (repeatGrid == null) return false;

            if (scrollingType == "vertical" && repeatGrid.Rows > 1) return true;
            if (scrollingType == "horizontal" && repeatGrid.Columns > 1) return true;
            return false;
        }

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            return rect;
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            var repeatGrid = xdObject.Meta?.Ux?.RepeatGrid ?? new XdRepeatGridJson();

            var item = xdObject.Group.Children[0].Group.Children[0];
            xdObject.Group.Children = new[] { item };

            if (repeatGrid.Columns > 1 && repeatGrid.Rows > 1)
            {
                return (new IComponent[]
                {
                    new GridLayoutComponent(repeatGrid.PaddingX, repeatGrid.PaddingY)
                }, new IAsset[] { });
            }
            else if (repeatGrid.Columns > 1)
            {
                return (new IComponent[]
                {
                    new HorizontalLayoutComponent(repeatGrid.PaddingX)
                }, new IAsset[] { });
            }
            else
            {
                return (new IComponent[]
                {
                    new VerticalLayoutComponent(repeatGrid.PaddingY)
                }, new IAsset[] { });
            }

            throw new Exception();
        }
    }
}