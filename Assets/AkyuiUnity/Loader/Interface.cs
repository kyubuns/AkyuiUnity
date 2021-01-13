using System;
using JetBrains.Annotations;
using UnityEngine;

namespace AkyuiUnity.Loader
{
    public interface IAkyuiLoader : IDisposable
    {
        [NotNull] LayoutInfo LayoutInfo { get; }
        [NotNull] AssetsInfo AssetsInfo { get; }
        [NotNull] byte[] LoadAsset(string fileName);
    }

    public class AssetsInfo
    {
        [NotNull] public readonly IAsset[] Assets;

        public AssetsInfo([NotNull] IAsset[] assets)
        {
            Assets = assets;
        }
    }
}