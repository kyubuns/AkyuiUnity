using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkyuiUnity.Editor;
using AkyuiUnity.Editor.ScriptableObject;
using AkyuiUnity.Loader;
using UnityEngine;
using XdParser;

namespace AkyuiUnity.Xd
{
    public static class XdImporter
    {
        public static void Import(XdImportSettings xdSettings, string[] xdFilePaths)
        {
            var settings = new XdImportSettingsWrapper(xdSettings);
            var loaders = new List<IAkyuiLoader>();
            foreach (var xdFilePath in xdFilePaths)
            {
                Debug.Log($"Xd Import Start: {xdFilePath}");
                var file = new XdFile(xdFilePath);
                foreach (var artwork in file.Artworks)
                {
                    if (artwork.Artboard.Children.Length == 0) continue;
                    if (!(artwork.Artboard.Children[0].Meta?.Ux?.MarkedForExport ?? false)) continue;
                    var akyuiXdObjectParsers = xdSettings.ObjectParsers ?? new AkyuiXdObjectParser[] { };
                    var akyuiXdGroupParsers = xdSettings.GroupParsers ?? new AkyuiXdGroupParser[] { };
                    var triggers = xdSettings.XdTriggers ?? new AkyuiXdImportTrigger[] { };
                    loaders.Add(new XdAkyuiLoader(file, artwork, akyuiXdObjectParsers, akyuiXdGroupParsers, triggers));
                }
                Debug.Log($"Xd Import Finish: {xdFilePath}");
            }

            Importer.Import(settings, loaders.ToArray());

            if (!string.IsNullOrWhiteSpace(xdSettings.AkyuiOutputPath))
            {
                foreach (var loader in loaders)
                {
                    var bytes = AkyuiCompressor.Compress(loader);
                    var outputPath = Path.Combine(xdSettings.AkyuiOutputPath, loader.LayoutInfo.Name + ".aky");
                    File.WriteAllBytes(outputPath, bytes);
                    Debug.Log($"Export Akyui {outputPath}");
                }
            }

            foreach (var loader in loaders) loader.Dispose();
        }
    }

    public class XdImportSettingsWrapper : IAkyuiImportSettings
    {
        private readonly XdImportSettings _settings;

        public XdImportSettingsWrapper(XdImportSettings settings)
        {
            _settings = settings;
        }

        public string PrefabOutputPath => _settings.PrefabOutputPath;
        public string AssetOutputDirectoryPath => _settings.AssetOutputDirectoryPath;
        public string MetaOutputPath => _settings.MetaOutputPath;
        public string FontDirectoryPath => _settings.FontDirectoryPath;
        public bool CheckAssetHash => _settings.CheckAssetHash;

        public IAkyuiImportTrigger[] Triggers
        {
            get
            {
                return new IAkyuiImportTrigger[] { new SvgImportTrigger(_settings.SvgSaveScale) }.Concat(_settings.Triggers).ToArray();
            }
        }
    }
}