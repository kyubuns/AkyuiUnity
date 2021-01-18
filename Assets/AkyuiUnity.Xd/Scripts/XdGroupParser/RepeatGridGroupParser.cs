using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class RepeatGridGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson instanceObject, XdObjectJson symbolObject)
        {
            var repeatGrid = instanceObject?.Meta?.Ux?.RepeatGrid;
            return repeatGrid != null;
        }

        public IComponent[] Render(XdObjectJson instanceObject, XdObjectJson symbolObject, ref XdObjectJson[] children)
        {
            var repeatGrid = instanceObject?.Meta?.Ux?.RepeatGrid ?? new XdRepeatGridJson();

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