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

        public Rect CalcSize(XdObjectJson xdObject, Rect rect)
        {
            var scrollingType = xdObject.Meta?.Ux?.ScrollingType;

            if (scrollingType == "vertical")
            {
                var offsetY = xdObject.Meta?.Ux?.OffsetY ?? 0f;
                var viewportHeight = xdObject.Meta?.Ux?.ViewportHeight ?? 0f;
                return new Rect(0f, offsetY, rect.size.x, viewportHeight);
            }
            else
            {
                var offsetX = xdObject.Meta?.Ux?.OffsetX ?? 0f;
                var viewportWidth = xdObject.Meta?.Ux?.ViewportWidth ?? 0f;
                return new Rect(offsetX, 0f, viewportWidth, rect.size.y);
            }
        }

        public (IComponent[], IAsset[]) Render(XdObjectJson xdObject, XdAssetHolder assetHolder, IObbGetter obbGetter)
        {
            var spacing = 0f;
            var scrollingType = xdObject.Meta?.Ux?.ScrollingType;

            var (paddingTop, paddingBottom) = CalcPadding(xdObject, obbGetter);

            var specialSpacings = new List<SpecialSpacing>();
            var repeatGrid = xdObject.Group?.Children?.FirstOrDefault(x => RepeatGridGroupParser.Is(x));
            if (repeatGrid != null)
            {
                XdObjectJson[] newChildren;
                (newChildren, spacing) = ExpandRepeatGridGroup(xdObject, repeatGrid, scrollingType, obbGetter, ref specialSpacings);

                foreach (var newChild in newChildren)
                {
                    obbGetter.ChangeParent(newChild, xdObject);
                }

                xdObject.Group.Children = xdObject.Group.Children.Where(x => x != repeatGrid).Concat(newChildren).ToArray();

                var rootObb = obbGetter.Get(xdObject);
                paddingTop = Mathf.Max(newChildren.Select(x => rootObb.Size.y / 2f + obbGetter.Get(x).LocalLeftTopPosition.y).Min(), 0f);
            }

            return (new IComponent[]
            {
                new VerticalListComponent(spacing, paddingTop, paddingBottom, specialSpacings.ToArray()),
            }, new IAsset[] { });
        }

        private static (float Top, float Bottom) CalcPadding(XdObjectJson xdObject, IObbGetter obbGetter)
        {
            var top = 0f;
            var bottom = 0f;
            var spacer = xdObject.Group?.Children?.FirstOrDefault(x => x.NameEndsWith("spacer"));
            if (spacer != null)
            {
                bottom = Mathf.Max(obbGetter.Get(spacer).Size.y, 0f);
                xdObject.Group.Children = xdObject.Group.Children.Where(x => x != spacer).ToArray();
            }

            return (top, bottom);
        }

        private static (XdObjectJson[], float Spacing) ExpandRepeatGridGroup(XdObjectJson xdObject, XdObjectJson repeatGrid, string scrollingType, IObbGetter obbGetter, ref List<SpecialSpacing> specialSpacings)
        {
            var spacing = repeatGrid.GetRepeatGridSpacing(scrollingType);

            var listElement = repeatGrid.Group.Children[0].Group.Children[0];
            var listItems = new[] { listElement };
            if (xdObject.HasParameter("multiitems"))
            {
                listItems = ExpandMultiItemsList(listElement, scrollingType, obbGetter, ref specialSpacings);
            }

            foreach (var listItem in listItems)
            {
                listItem.RemoveConstraint();
            }

            return (listItems.ToArray(), spacing);
        }

        private static XdObjectJson[] ExpandMultiItemsList(XdObjectJson listItemRoot, string scrollingType, IObbGetter obbGetter, ref List<SpecialSpacing> specialSpacings)
        {
            var listItems = new List<XdObjectJson>();

            // 孫を解析して、それもRepeatGridなら更に子供
            var tmp = listItemRoot.Group.Children.ToList();
            var size = new List<(string Name, Obb Obb)>();

            foreach (var listItem in tmp)
            {
                if (RepeatGridGroupParser.Is(listItem, scrollingType))
                {
                    var listListItem = listItem.Group.Children[0].Group.Children[0];

                    // 登録
                    specialSpacings.Add(new SpecialSpacing(listListItem.Name, listListItem.Name, listItem.GetRepeatGridSpacing(scrollingType)));
                    listItems.Add(listListItem);

                    // 名前は参照されるので子供の名前を使うが、サイズは親のものとして計算する。
                    // XDのデザイン上は親のサイズなので。
                    size.Add((listListItem.Name, obbGetter.Get(listItem)));
                }
                else
                {
                    listItems.Add(listItem);
                    size.Add((listItem.Name, obbGetter.Get(listItem)));
                }
            }

            var orderedSize = size.OrderBy(x => x.Obb.CalcGlobalRect().yMin).ToArray();
            foreach (var ((name1, obb1), (name2, obb2)) in orderedSize.Zip(orderedSize.Skip(1), (x, y) => (x, y)))
            {
                specialSpacings.Add(new SpecialSpacing(name1, name2, obb2.CalcGlobalRect().yMax - obb1.CalcGlobalRect().yMin));
            }

            return listItems.ToArray();
        }
    }
}