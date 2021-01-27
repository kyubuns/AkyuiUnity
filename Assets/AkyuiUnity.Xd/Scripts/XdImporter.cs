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
            var logger = new AkyuiLogger("Akyui.Xd");
            var settings = new XdImportSettingsWrapper(xdSettings);
            var loaders = new List<IAkyuiLoader>();
            using (var progressBar = new AkyuiProgressBar("Akyui.Xd"))
            {
                progressBar.SetTotal(xdFilePaths.Length);
                foreach (var xdFilePath in xdFilePaths)
                {
                    using (var progress = progressBar.TaskStart(Path.GetFileName(xdFilePath)))
                    using (logger.SetCategory(Path.GetFileName(xdFilePath)))
                    {
                        logger.Log($"Xd Import Start");
                        var file = new XdFile(xdFilePath);
                        var importedArtboards = 0;

                        var targets = new List<XdArtboard>();
                        foreach (var artwork in file.Artworks)
                        {
                            if (artwork.Artboard.Children.Length == 0) continue;
                            var markForExport = artwork.Artboard.Children[0].Meta?.Ux?.MarkedForExport ?? false;
                            if (!markForExport) continue;
                            targets.Add(artwork);
                        }

                        progress.SetTotal(targets.Count);
                        foreach (var artwork in targets)
                        {
                            using (progress.TaskStart(artwork.Name))
                            {
                                var akyuiXdObjectParsers = xdSettings.ObjectParsers ?? new AkyuiXdObjectParser[] { };
                                var akyuiXdGroupParsers = xdSettings.GroupParsers ?? new AkyuiXdGroupParser[] { };
                                var triggers = xdSettings.XdTriggers ?? new AkyuiXdImportTrigger[] { };
                                loaders.Add(new XdAkyuiLoader(file, artwork, akyuiXdObjectParsers, akyuiXdGroupParsers, triggers));
                                importedArtboards++;
                            }
                        }

                        logger.Log($"Xd Import Finish", ("artboards", importedArtboards));

                        if (importedArtboards == 0)
                        {
                            logger.Warning($"The artboard to be imported was not found. Please set Mark for Export.");
                        }
                    }
                }
            }

            Importer.Import(settings, loaders.ToArray());

            if (!string.IsNullOrWhiteSpace(xdSettings.AkyuiOutputPath))
            {
                foreach (var loader in loaders)
                {
                    using (logger.SetCategory(loader.LayoutInfo.Name))
                    {
                        var bytes = AkyuiCompressor.Compress(loader);
                        var outputPath = Path.Combine(xdSettings.AkyuiOutputPath, loader.LayoutInfo.Name + ".aky");
                        File.WriteAllBytes(outputPath, bytes);
                        logger.Log($"Export Akyui");
                    }
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