using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using XdParser.Internal;

namespace AkyuiUnity.Xd
{
    public class Obb
    {
        public Vector2 LocalLeftTopPosition { get; set; }
        public Vector2 Size { get; set; }
        public float Rotation { get; set; }
        public Obb Parent { get; set; }

        public Obb CalcObbInWorld([CanBeNull] Obb world)
        {
            var rotation = Rotation;
            var leftTopPosition = LocalLeftTopPosition;

            var parent = Parent;
            while (parent != world)
            {
                if (Mathf.Abs(leftTopPosition.x) > 0.0001f || Mathf.Abs(leftTopPosition.y) > 0.0001f)
                {
                    var rad = parent.Rotation * Mathf.Deg2Rad;
                    var atan = Mathf.Atan2(leftTopPosition.y, leftTopPosition.x);
                    var radCos = Mathf.Cos(rad + atan);
                    var radSin = Mathf.Sin(rad + atan);
                    var r = Mathf.Sqrt(leftTopPosition.x * leftTopPosition.x + leftTopPosition.y * leftTopPosition.y);
                    leftTopPosition = new Vector2(
                        parent.LocalLeftTopPosition.x + r * radCos,
                        parent.LocalLeftTopPosition.y + r * radSin
                    );
                }
                rotation += parent.Rotation;

                parent = parent.Parent;
            }

            return new Obb
            {
                LocalLeftTopPosition = leftTopPosition,
                Size = Size,
                Rotation = rotation,
                Parent = world
            };
        }

        public Rect CalcLocalRect()
        {
            return CalcRect(LocalLeftTopPosition, Size, Rotation);
        }

        public static Rect CalcRect(Vector2 leftTopPosition, Vector2 size, float rotation)
        {
            if (Mathf.Abs(rotation) < 0.0001f) return new Rect(leftTopPosition, size);

            var rad = rotation * Mathf.Deg2Rad;
            var radCos = Mathf.Cos(rad);
            var radSin = Mathf.Sin(rad);
            var rectSize = new Vector2(
                size.x * radCos + size.y * radSin,
                size.x * radSin + size.y * radCos
            );

            var x2 = size.x / 2;
            var y2 = size.y / 2;
            var r = Mathf.Sqrt(x2 * x2 + y2 * y2);
            var atan = Mathf.Atan2(size.y, size.x);
            var leftTop = rectSize / -2f;
            var pivot = new Vector2(
                r * Mathf.Cos(atan + rad),
                r * Mathf.Sin(atan + rad)
            );
            var rectPosition = leftTopPosition + pivot + leftTop;

            if (rectSize.x < 0.0001f)
            {
                rectPosition.x += rectSize.x;
                rectSize.x *= -1f;
            }
            if (rectSize.y < 0.0001f)
            {
                rectPosition.y += rectSize.y;
                rectSize.y *= -1f;
            }
            return new Rect(rectPosition, rectSize);
        }

        public static Rect MinMaxRect(Obb[] obbList)
        {
            if (obbList.Length == 0) return Rect.zero;

            var rects = obbList.Select(x => x.CalcLocalRect()).ToArray();
            return Rect.MinMaxRect(
                rects.Select(x => x.xMin).Min(),
                rects.Select(x => x.yMin).Min(),
                rects.Select(x => x.xMax).Max(),
                rects.Select(x => x.yMax).Max()
            );
        }

        public void ApplyRect(Rect rect)
        {
            Size = new Vector2(rect.width, rect.height);

            if (Mathf.Abs(Rotation) < 0.0001f)
            {
                LocalLeftTopPosition += rect.position;
                return;
            }

            var rad = Rotation * Mathf.Deg2Rad;
            var radCos = Mathf.Cos(rad);
            var radSin = Mathf.Sin(rad);
            LocalLeftTopPosition += new Vector2(
                rect.position.x * radCos - rect.position.y * radSin,
                rect.position.x * radSin + rect.position.y * radCos
            );
        }
    }

    public interface IObbGetter
    {
        Obb Get(XdObjectJson xdObject);
    }

    public class ObbHolder : IObbGetter
    {
        private readonly Dictionary<string, Obb> _obb = new Dictionary<string, Obb>();

        public void Set(XdObjectJson xdObject, Obb obb)
        {
            var key = xdObject.Guid ?? xdObject.Id;
            if (_obb.ContainsKey(key)) throw new Exception($"{key} already exists");
            _obb[key] = obb;
        }

        public Obb Get(XdObjectJson xdObject)
        {
            var key = xdObject.Guid ?? xdObject.Id;
            return _obb[key];
        }
    }
}