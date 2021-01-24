using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class ScrollGroupParser : IXdGroupParser
    {
        public bool Is(XdObjectJson xdObject)
        {
            var scrollingType = xdObject?.Meta?.Ux?.ScrollingType;
            return !string.IsNullOrWhiteSpace(scrollingType);
        }

        public Rect CalcSize(XdObjectJson xdObject, Vector2 position, Rect rect)
        {
            var scrollingType = xdObject.Meta?.Ux?.ScrollingType;

            if (scrollingType == "vertical")
            {
                var offsetY = xdObject.Meta?.Ux?.OffsetY ?? 0f;
                var viewportHeight = xdObject.Meta?.Ux?.ViewportHeight ?? 0f;
                return new Rect(rect.position.x, position.y + offsetY, rect.size.x, viewportHeight);
            }
            else
            {
                var offsetX = xdObject.Meta?.Ux?.OffsetX ?? 0f;
                var viewportWidth = xdObject.Meta?.Ux?.ViewportWidth ?? 0f;
                return new Rect(position.x + offsetX, rect.position.y, viewportWidth, rect.size.y);
            }
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, ref XdObjectJson[] children, XdAssetHolder assetHolder, ISizeGetter sizeGetter)
        {
            var spacing = 0f;
            var scrollingType = xdObject?.Meta?.Ux?.ScrollingType;

            var (paddingTop, paddingBottom) = CalcPadding(xdObject, ref children, sizeGetter);

            var specialSpacings = new List<SpecialSpacing>();
            var repeatGrid = children.FirstOrDefault(x => RepeatGridGroupParser.Is(x));
            if (repeatGrid != null)
            {
                XdObjectJson[] newChildren;
                (newChildren, spacing) = ExpandRepeatGridGroup(xdObject, repeatGrid, scrollingType, sizeGetter, ref specialSpacings);

                children = children.Where(x => x != repeatGrid).Concat(newChildren).ToArray();
            }

            return (new IComponent[]
            {
                new VerticalListComponent(0, spacing, paddingTop, paddingBottom, specialSpacings.ToArray()),
            }, new IAsset[] { });
        }

        private static (float Top, float Bottom) CalcPadding(XdObjectJson xdObject, ref XdObjectJson[] children, ISizeGetter sizeGetter)
        {
            var rootRect = sizeGetter.Get(xdObject);

            var top = -children.Select(x => rootRect.yMin - sizeGetter.Get(x).yMin).Max();

            var bottom = 0f;
            var spacer = children.FirstOrDefault(x => x.NameEndsWith("spacer"));
            if (spacer != null)
            {
                bottom = sizeGetter.Get(spacer).height;
                children = children.Where(x => x != spacer).ToArray();
            }

            return (top, bottom);
        }

        private static (XdObjectJson[], float Spacing) ExpandRepeatGridGroup(XdObjectJson xdObject, XdObjectJson repeatGrid, string scrollingType, ISizeGetter sizeGetter, ref List<SpecialSpacing> specialSpacings)
        {
            var spacing = repeatGrid.GetRepeatGridSpacing(scrollingType);

            var listElement = repeatGrid.Group.Children[0].Group.Children[0];
            var xdObjectSize = sizeGetter.Get(xdObject);
            var repeatGridSize = sizeGetter.Get(repeatGrid);
            var offset = repeatGridSize.position - xdObjectSize.position;
            offset.y = 0f;
            sizeGetter.Offset(listElement, offset);

            var listItems = new[] { listElement };
            if (xdObject.GetParameters().Contains("multiitems"))
            {
                listItems = ExpandMultiItemsList(listItems[0], scrollingType, sizeGetter, ref specialSpacings);
            }

            foreach (var listItem in listItems)
            {
                listItem.RemoveConstraint();
            }

            return (listItems.ToArray(), spacing);
        }

        private static XdObjectJson[] ExpandMultiItemsList(XdObjectJson listItemRoot, string scrollingType, ISizeGetter sizeGetter, ref List<SpecialSpacing> specialSpacings)
        {
            var listItems = new List<XdObjectJson>();

            // 孫を解析して、それもRepeatGridなら更に子供
            var tmp = listItemRoot.Group.Children.ToList();
            var size = new List<(string Name, Rect Size)>();

            foreach (var listItem in tmp)
            {
                if (RepeatGridGroupParser.Is(listItem, scrollingType))
                {
                    var listListItem = listItem.Group.Children[0].Group.Children[0];
                    specialSpacings.Add(new SpecialSpacing(listListItem.Name, listListItem.Name, listItem.GetRepeatGridSpacing(scrollingType)));
                    listItems.Add(listListItem);

                    // 名前は参照されるので子供の名前を使うが、サイズは親のものとして計算する。
                    // XDのデザイン上は親のサイズなので。
                    size.Add((listListItem.Name, sizeGetter.Get(listItem)));
                }
                else
                {
                    listItems.Add(listItem);
                    size.Add((listItem.Name, sizeGetter.Get(listItem)));
                }
            }

            var orderedSize = size.OrderBy(x => x.Size.y).ToArray();
            foreach (var (item1, item2) in orderedSize.Zip(orderedSize.Skip(1), (x, y) => (x, y)))
            {
                specialSpacings.Add(new SpecialSpacing(item1.Name, item2.Name, item2.Size.yMin - item1.Size.yMax));
            }

            return listItems.ToArray();
        }
    }
}