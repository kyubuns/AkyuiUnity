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

        public IComponent[] Render(XdObjectJson xdObject, ref XdObjectJson[] children)
        {
            var spacing = 0f;
            var paddingTop = 0f;
            var paddingBottom = 0f;

            var scrollingType = xdObject?.Meta?.Ux?.ScrollingType;
            if (children.Length == 1 && RepeatGridGroupParser.Is(children[0]))
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

                var listItems = new List<XdObjectJson> { repeatGrid.Group.Children[0].Group.Children[0] };

                // @MultiItems なら孫要素を解析する
                if (xdObject.GetParameters().Contains("multiitems"))
                {
                    // 孫を解析して、それもRepeatGridなら更に子供
                    var tmp = listItems[0].Group.Children.ToList();
                    listItems.Clear();

                    foreach (var listItem in tmp)
                    {
                        if (RepeatGridGroupParser.Is(listItem, xdObject?.Meta?.Ux?.ScrollingType))
                        {
                            listItems.AddRange(listItem.Group.Children[0].Group.Children);
                        }
                        else
                        {
                            listItems.Add(listItem);
                        }
                    }
                }

                // 変なconstraintが付いてたらリスト作るときに死ぬので解除
                foreach (var listItem in listItems)
                {
                    if (listItem?.Meta?.Ux != null)
                    {
                        listItem.Meta.Ux.ConstraintRight = false;
                        listItem.Meta.Ux.ConstraintLeft = false;
                        listItem.Meta.Ux.ConstraintTop = false;
                        listItem.Meta.Ux.ConstraintBottom = false;
                    }
                }
                children = listItems.ToArray();
            }

            return new IComponent[]
            {
                new VerticalListComponent(0, spacing, paddingTop, paddingBottom),
            };
        }
    }
}