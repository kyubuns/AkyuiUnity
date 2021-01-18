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

            if (scrollingType == "vertical")
            {
                var offsetY = xdObject?.Meta?.Ux?.OffsetY ?? 0f;
                var viewportHeight = xdObject?.Meta?.Ux?.ViewportHeight ?? 0f;
                return new Rect(rect.position.x, position.y + offsetY, rect.size.x, viewportHeight);
            }
            else
            {
                var offsetX = xdObject?.Meta?.Ux?.OffsetX ?? 0f;
                var viewportWidth = xdObject?.Meta?.Ux?.ViewportWidth ?? 0f;
                return new Rect(position.x + offsetX, rect.position.y, viewportWidth, rect.size.y);
            }
        }

        public IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject, ref XdObjectJson[] children)
        {
            var spacing = 0f;

            var scrollingType = instanceObject?.Meta?.Ux?.ScrollingType;
            if (children.Length == 1 && RepeatGridGroupParser.Is(children[0], null))
            {
                var repeatGrid = children[0];
                if (scrollingType == "vertical")
                {
                    spacing = repeatGrid.Meta?.Ux?.RepeatGrid?.PaddingY ?? 0f;
                }
                else
                {
                    spacing = repeatGrid.Meta?.Ux?.RepeatGrid?.PaddingX ?? 0f;
                }

                children = new[] { repeatGrid.Group.Children[0].Group.Children[0] };
            }

            return new IComponent[]
            {
                new VerticalListComponent(0, spacing),
            };
        }
    }
}