using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ScrollGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson instanceObject, XdObjectJson symbolObject)
        {
            var scrollingType = instanceObject?.Meta?.Ux?.ScrollingType;
            return !string.IsNullOrWhiteSpace(scrollingType);
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            var scrollingType = xdObject?.Meta?.Ux?.ScrollingType;

            if (scrollingType == "horizontal")
            {
                var viewportWidth = xdObject?.Meta?.Ux?.ViewportWidth ?? 0f;
                var offsetX = xdObject?.Meta?.Ux?.OffsetX ?? 0f;
                return new Rect(position.x + offsetX, position.y, viewportWidth, rect.height);
            }

            var viewportHeight = xdObject?.Meta?.Ux?.ViewportHeight ?? 0f;
            var offsetY = xdObject?.Meta?.Ux?.OffsetY ?? 0f;
            return new Rect(position.x, position.y + offsetY, rect.width, viewportHeight);
        }

        public IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject, ref XdObjectJson[] children)
        {
            if (children.Length == 1 && RepeatGridGroupParser.Is(children[0], null))
            {
                var repeatGrid = children[0];
                children = new[] { repeatGrid.Group.Children[0].Group.Children[0] };
            }

            return new IComponent[]
            {
                new VerticalListComponent(0)
            };
        }
    }
}