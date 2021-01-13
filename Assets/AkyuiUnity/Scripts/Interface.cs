using JetBrains.Annotations;
using UnityEngine;

namespace AkyuiUnity
{
    public class LayoutInfo
    {
        [NotNull] public string Name;
        public readonly int Timestamp;
        [NotNull] public readonly Meta Meta;
        public readonly int Root;
        [NotNull] public readonly IElement[] Elements;

        public LayoutInfo([NotNull] string name, int timestamp, [NotNull] Meta meta, int root, [NotNull] IElement[] elements)
        {
            Name = name;
            Timestamp = timestamp;
            Meta = meta;
            Root = root;
            Elements = elements;
        }
    }

    public class Meta
    {
        [NotNull] public readonly string AkyuiVersion;
        [NotNull] public readonly string App;
        [NotNull] public readonly string AppVersion;

        public Meta([NotNull] string akyuiVersion, [NotNull] string app, [NotNull] string appVersion)
        {
            AkyuiVersion = akyuiVersion;
            App = app;
            AppVersion = appVersion;
        }
    }

    public interface IAsset
    {
        [NotNull] string FileName { get; }
        int Timestamp { get; }
    }

    public class SpriteAsset : IAsset
    {
        public const string TypeString = "sprite";

        public string FileName { get; }
        public int Timestamp { get; }

        public SpriteAsset([NotNull] string fileName, int timestamp)
        {
            FileName = fileName;
            Timestamp = timestamp;
        }
    }

    public interface IElement
    {
        int Eid { get; }
    }

    public class ObjectElement : IElement
    {
        public const string TypeString = "object";

        public int Eid { get; }

        [NotNull] public readonly string Name;
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly AnchorXType AnchorX;
        public readonly AnchorYType AnchorY;
        [NotNull] public readonly IComponent[] Components;
        [NotNull] public readonly int[] Children;

        public ObjectElement(int eid, [NotNull] string name, Vector2 position, Vector2 size, AnchorXType anchorX, AnchorYType anchorY, [NotNull] IComponent[] components, [NotNull] int[] children)
        {
            Eid = eid;
            Name = name;
            Position = position;
            Size = size;
            AnchorX = anchorX;
            AnchorY = anchorY;
            Components = components;
            Children = children;
        }
    }

    public class PrefabElement : IElement
    {
        public const string TypeString = "prefab";

        public int Eid { get; }
        [NotNull] public readonly string Reference;
        public readonly int Timestamp;
        [NotNull] public readonly Override[] Overrides;

        public PrefabElement(int eid, [NotNull] string reference, int timestamp, [NotNull] Override[] overrides)
        {
            Eid = eid;
            Reference = reference;
            Timestamp = timestamp;
            Overrides = overrides;
        }
    }

    public class Override
    {
        [NotNull] public int[] Eid { get; }
        [CanBeNull] public readonly string Name;
        [CanBeNull] public readonly Vector2? Position;
        [CanBeNull] public readonly Vector2? Size;
        [CanBeNull] public readonly AnchorXType? AnchorX;
        [CanBeNull] public readonly AnchorYType? AnchorY;
        [CanBeNull] public readonly IComponent[] Components;

        public Override([NotNull] int[] eid, [CanBeNull] string name, [CanBeNull] Vector2? position, [CanBeNull] Vector2? size, [CanBeNull] AnchorXType? anchorX, [CanBeNull] AnchorYType? anchorY, [CanBeNull] IComponent[] components)
        {
            Eid = eid;
            Name = name;
            Position = position;
            Size = size;
            AnchorX = anchorX;
            AnchorY = anchorY;
            Components = components;
        }
    }

    public interface IComponent
    {
        int Cid { get; }
    }

    public class ImageComponent : IComponent
    {
        public const string TypeString = "image";

        public int Cid { get; }
        [CanBeNull] public readonly string Sprite;
        [CanBeNull] public readonly Color? Color;

        public ImageComponent(int cid, [CanBeNull] string sprite, [CanBeNull] Color? color)
        {
            Cid = cid;
            Sprite = sprite;
            Color = color;
        }
    }

    public class TextComponent : IComponent
    {
        public const string TypeString = "text";

        public int Cid { get; }
        [CanBeNull] public readonly string Text;
        [CanBeNull] public readonly float? Size;
        [CanBeNull] public readonly Color? Color;
        [CanBeNull] public readonly TextAlign? Align;

        public enum TextAlign
        {
            MiddleCenter,
        }

        public TextComponent(int cid, [CanBeNull] string text, [CanBeNull] float? size, [CanBeNull] Color? color, [CanBeNull] TextAlign? align)
        {
            Cid = cid;
            Text = text;
            Size = size;
            Color = color;
            Align = align;
        }
    }

    public class ButtonComponent : IComponent
    {
        public const string TypeString = "button";

        public int Cid { get; }

        public ButtonComponent(int cid)
        {
            Cid = cid;
        }
    }

    public class HorizontalLayoutComponent : IComponent
    {
        public const string TypeString = "horizontal_layout";

        public int Cid { get; }

        public HorizontalLayoutComponent(int cid)
        {
            Cid = cid;
        }
    }

    public class VerticalLayoutComponent : IComponent
    {
        public const string TypeString = "vertical_layout";

        public int Cid { get; }

        public VerticalLayoutComponent(int cid)
        {
            Cid = cid;
        }
    }

    public enum AnchorXType
    {
        Left,
        Center,
        Right,
        Stretch
    }

    public enum AnchorYType
    {
        Top,
        Middle,
        Bottom,
        Stretch
    }
}