using System;
using System.Collections.Generic;
using System.Linq;
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

        public Vector2 CalcParentLocalCenterPosition()
        {
            var parentObb = Parent ?? new Obb();
            var localRect = CalcLocalRect();
            return localRect.position + localRect.size / 2f - parentObb.Size / 2f;
        }

        public Rect CalcLocalRect()
        {
            if (Mathf.Abs(Rotation) < 0.0001f) return new Rect(LocalLeftTopPosition, Size);

            var rad = Rotation * Mathf.Deg2Rad;
            var radCos = Mathf.Cos(rad);
            var radSin = Mathf.Sin(rad);
            var rectSize = new Vector2(
                Size.x * radCos + Size.y * radSin,
                Size.x * radSin + Size.y * radCos
            );

            var x2 = Size.x / 2;
            var y2 = Size.y / 2;
            var r = Mathf.Sqrt(x2 * x2 + y2 * y2);
            var atan = Mathf.Atan2(Size.y, Size.x);
            var leftTop = rectSize / -2f;
            var pivot = new Vector2(
                r * Mathf.Cos(atan + rad),
                r * Mathf.Sin(atan + rad)
            );
            return new Rect(LocalLeftTopPosition + pivot + leftTop, rectSize);
        }

        public Rect CalcGlobalRect()
        {
            // todo: 計算する
            return CalcLocalRect();
        }

        public void ApplyRect(Rect rect)
        {
            // todo: 角度を考慮する
            LocalLeftTopPosition += rect.position;
            Size = new Vector2(rect.width, rect.height);
        }

        public static Rect MinMaxRect(Obb[] obbList)
        {
            // todo: 角度を考慮する
            if (obbList.Length == 0) return Rect.zero;

            var xMin = obbList.Select(x => x.LocalLeftTopPosition.x).Min();
            var yMin = obbList.Select(x => x.LocalLeftTopPosition.y).Min();
            var xMax = obbList.Select(x => x.LocalLeftTopPosition.x + x.Size.x).Max();
            var yMax = obbList.Select(x => x.LocalLeftTopPosition.y + x.Size.y).Max();

            var position = new Vector2(xMin, yMin);
            var size = new Vector2(xMax - xMin, yMax - yMin);
            return new Rect(position, size);
        }
    }

    public interface IObbGetter
    {
        Obb Get(XdObjectJson xdObject);
        void ChangeParent(XdObjectJson target, XdObjectJson parent);
    }

    public class ObbHolder : IObbGetter
    {
        private readonly Dictionary<string, Obb> _obb = new Dictionary<string, Obb>();

        public void Set(XdObjectJson xdObject, Obb obb)
        {
            var key = xdObject.Id ?? xdObject.Guid;
            if (_obb.ContainsKey(key)) throw new Exception($"{key} already exists");
            _obb[key] = obb;
        }

        public Obb Get(XdObjectJson xdObject)
        {
            var key = xdObject.Id ?? xdObject.Guid;
            return _obb[key];
        }

        public void ChangeParent(XdObjectJson target, XdObjectJson parent)
        {
            // todo:
        }
    }
}