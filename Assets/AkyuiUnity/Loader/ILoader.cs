using System;
using JetBrains.Annotations;
using UnityEngine;

namespace AkyuiUnity.Loader
{
    public interface ILoader : IDisposable
    {
        [NotNull] string FileName { get; }
        [NotNull] LayoutInfo LayoutInfo { get; }
        [NotNull] AssetsInfo AssetsInfo { get; }
        [NotNull] byte[] LoadAsset(string fileName);
    }

    public class LayoutInfo
    {
        [NotNull] public readonly Meta Meta;
        public readonly int Timestamp;
        public readonly int Root;
        [NotNull] public readonly IElement[] Elements;

        public LayoutInfo([NotNull] Meta meta, int timestamp, int root, [NotNull] IElement[] elements)
        {
            Meta = meta;
            Timestamp = timestamp;
            Root = root;
            Elements = elements;
        }
    }

    public class AssetsInfo
    {
        [NotNull] public readonly IAsset[] Assets;

        public AssetsInfo([NotNull] IAsset[] assets)
        {
            Assets = assets;
        }
    }

    public class Meta
    {
        [NotNull] public readonly string Version;
        [NotNull] public readonly string GeneratedBy;

        public Meta([NotNull] string version, [NotNull] string generatedBy)
        {
            Version = version;
            GeneratedBy = generatedBy;
        }
    }
}