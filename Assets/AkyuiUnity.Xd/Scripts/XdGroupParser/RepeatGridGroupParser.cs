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

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            return rect;
        }

        public IComponent[] Render(XdObjectJson xdObject, ref XdObjectJson[] children)
        {
            var repeatGrid = xdObject?.Meta?.Ux?.RepeatGrid ?? new XdRepeatGridJson();

            var item = children[0].Group.Children[0];
            children = new[] { item };

            if (repeatGrid.Columns > 1)
            {
                return new IComponent[]
                {
                    new HorizontalLayoutComponent(0, repeatGrid.PaddingX)
                };
            }

            return new IComponent[]
            {
                new VerticalLayoutComponent(0, repeatGrid.PaddingY)
            };
        }
    }
}