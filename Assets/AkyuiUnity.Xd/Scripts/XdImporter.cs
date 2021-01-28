using System.Collections.Generic;
using System.IO;
using AkyuiUnity.Editor;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Loader;
using XdParser;

namespace AkyuiUnity.Xd
{
    public static class XdImporter
    {
        public static XdImportSettings Settings { get; private set; }

        public static void Import(XdImportSettings xdSettings, string[] xdFilePaths)
        {
            var logger = new AkyuiLogger("Akyui.Xd");
            var loaders = new List<IAkyuiLoader>();
            Settings = xdSettings;
            using (Disposable.Create(() => Settings = null))
            using (var progressBar = new AkyuiProgressBar("Akyui.Xd"))
            {
                progressBar.SetTotal(xdFilePaths.Length);
                foreach (var xdFilePath in xdFilePaths)
                {
                    using (var progress = progressBar.TaskStart(Path.GetFileName(xdFilePath)))
                    using (logger.SetCategory(Path.GetFileName(xdFilePath)))
                    {
                        var importedArtboards = ImportedArtboards(xdSettings, logger, xdFilePath, progress, loaders);
                        if (importedArtboards == 0)
                        {
                            logger.Warning($"The artboard to be imported was not found. Please set Mark for Export.");
                        }
                    }
                }
            }

            Importer.Import(xdSettings, loaders.ToArray());
            ExportAkyui(xdSettings, loaders, logger);
            foreach (var loader in loaders) loader.Dispose();
        }

        private static void ExportAkyui(XdImportSettings xdSettings, List<IAkyuiLoader> loaders, AkyuiLogger logger)
        {
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
        }

        private static int ImportedArtboards(XdImportSettings xdSettings, AkyuiLogger logger, string xdFilePath, IAkyuiProgress progress, List<IAkyuiLoader> loaders)
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
            return importedArtboards;
        }
    }
}