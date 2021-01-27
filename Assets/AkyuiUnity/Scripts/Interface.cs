using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace AkyuiUnity
{
    public static class Const
    {
        public const string AkyuiVersion = "0.1.0";
    }

    public class LayoutInfo
    {
        [NotNull] public string Name;
        public readonly long Hash;
        [NotNull] public readonly Meta Meta;
        public readonly int Root;
        [NotNull] public readonly IElement[] Elements;

        public LayoutInfo([NotNull] string name, long hash, [NotNull] Meta meta, int root, [NotNull] IElement[] elements)
        {
            Name = name;
            Hash = hash;
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
        long Hash { get; }
        [CanBeNull] string UserData { get; }
    }

    public class SpriteAsset : IAsset
    {
        public const string TypeString = "sprite";

        public string FileName { get; set; }
        public long Hash { get; set; }
        public Vector2 Size { get; set; }
        [CanBeNull] public string UserData { get; set; }
        [CanBeNull] public Border Border { get; set; }

        public SpriteAsset([NotNull] string fileName, long hash, Vector2 size, [CanBeNull] string userData, [CanBeNull] Border border)
        {
            FileName = fileName;
            Hash = hash;
            Size = size;
            UserData = userData;
            Border = border;
        }
    }

    public class Border
    {
        public Border(int top, int right, int bottom, int left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Left { get; }

        public Vector4 ToVector4()
        {
            return new Vector4(Left, Bottom, Right, Top);
        }
    }

    public interface IElement
    {
        int Eid { get; }
        Vector2 Position { get; }
        Vector2 Size { get; }
        AnchorXType AnchorX { get; }
        AnchorYType AnchorY { get; }
        float Rotation { get; }
    }

    public class ObjectElement : IElement
    {
        public const string TypeString = "object";

        public int Eid { get; }

        [NotNull] public readonly string Name;
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public AnchorXType AnchorX { get; }
        public AnchorYType AnchorY { get; }
        public float Rotation { get; }
        public bool Visible { get; }
        [NotNull] public readonly IComponent[] Components;
        [NotNull] public readonly int[] Children;

        public ObjectElement(int eid, [NotNull] string name, Vector2 position, Vector2 size, AnchorXType anchorX, AnchorYType anchorY, float rotation, bool visible, [NotNull] IComponent[] components, [NotNull] int[] children)
        {
            Eid = eid;
            Name = name;
            Position = position;
            Size = size;
            AnchorX = anchorX;
            AnchorY = anchorY;
            Rotation = rotation;
            Visible = visible;
            Components = components;
            Children = children;
        }
    }

    public interface IComponent
    {
    }

    public class ImageComponent : IComponent
    {
        public const string TypeString = "image";

        [CanBeNull] public readonly string Sprite;
        [CanBeNull] public readonly Color? Color;
        [CanBeNull] public readonly Vector2Int? Direction;

        public ImageComponent([CanBeNull] string sprite, [CanBeNull] Color? color, [CanBeNull] Vector2Int? direction)
        {
            Sprite = sprite;
            Color = color;
            Direction = direction;
        }
    }

    public class MaskComponent : IComponent
    {
        public const string TypeString = "mask";

        [CanBeNull] public readonly string Sprite;

        public MaskComponent([CanBeNull] string sprite)
        {
            Sprite = sprite;
        }
    }

    public class AlphaComponent : IComponent
    {
        public const string TypeString = "alpha";

        [CanBeNull] public readonly float? Alpha;

        public AlphaComponent(float? alpha)
        {
            Alpha = alpha;
        }
    }

    public class TextComponent : IComponent
    {
        public const string TypeString = "text";

        [CanBeNull] public readonly string Text;
        [CanBeNull] public readonly float? Size;
        [CanBeNull] public readonly Color? Color;
        [CanBeNull] public readonly TextAlign? Align;
        [CanBeNull] public readonly string Font;
        [CanBeNull] public readonly bool? Wrap;
        [CanBeNull] public readonly float? LineHeight;

        public enum TextAlign
        {
            UpperLeft,
            UpperCenter,
            UpperRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            LowerLeft,
            LowerCenter,
            LowerRight,
        }

        public TextComponent([CanBeNull] string text, [CanBeNull] float? size, [CanBeNull] Color? color, [CanBeNull] TextAlign? align, [CanBeNull] string font, [CanBeNull] bool? wrap, [CanBeNull] float? lineHeight)
        {
            Text = text;
            Size = size;
            Color = color;
            Align = align;
            Font = font;
            Wrap = wrap;
            LineHeight = lineHeight;
        }
    }

    public class ButtonComponent : IComponent
    {
        public const string TypeString = "button";

        public ButtonComponent()
        {
        }
    }

    public class VerticalScrollbarComponent : IComponent
    {
        public const string TypeString = "vertical_scrollbar";

        [CanBeNull] public readonly ImageComponent Image;

        public VerticalScrollbarComponent(ImageComponent image)
        {
            Image = image;
        }
    }

    public class VerticalListComponent : IComponent
    {
        public const string TypeString = "vertical_layout";

        [CanBeNull] public readonly float? Spacing;
        [CanBeNull] public readonly float? PaddingTop;
        [CanBeNull] public readonly float? PaddingBottom;
        [CanBeNull] public readonly SpecialSpacing[] SpacialSpacings;

        public VerticalListComponent([CanBeNull] float? spacing, [CanBeNull] float? paddingTop, [CanBeNull] float? paddingBottom, [CanBeNull] SpecialSpacing[] spacialSpacings)
        {
            Spacing = spacing;
            PaddingTop = paddingTop;
            PaddingBottom = paddingBottom;
            SpacialSpacings = spacialSpacings;
        }
    }

    public class SpecialSpacing
    {
        public readonly string Item1;
        public readonly string Item2;
        public readonly float Spacing;

        public SpecialSpacing(string item1, string item2, float spacing)
        {
            Item1 = item1;
            Item2 = item2;
            Spacing = spacing;
        }
    }

    public class HorizontalLayoutComponent : IComponent
    {
        public const string TypeString = "horizontal_layout";

        [CanBeNull] public readonly float? Spacing;

        public HorizontalLayoutComponent([CanBeNull] float? spacing)
        {
            Spacing = spacing;
        }
    }

    public class VerticalLayoutComponent : IComponent
    {
        public const string TypeString = "vertical_layout";

        [CanBeNull] public readonly float? Spacing;

        public VerticalLayoutComponent([CanBeNull] float? spacing)
        {
            Spacing = spacing;
        }
    }

    public class GridLayoutComponent : IComponent
    {
        public const string TypeString = "grid_layout";

        [CanBeNull] public readonly float? SpacingX;
        [CanBeNull] public readonly float? SpacingY;

        public GridLayoutComponent([CanBeNull] float? spacingX, [CanBeNull] float? spacingY)
        {
            SpacingX = spacingX;
            SpacingY = spacingY;
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

    public class InputFieldComponent : IComponent
    {
        public const string TypeString = "inputfield";

        public InputFieldComponent()
        {
        }
    }
}