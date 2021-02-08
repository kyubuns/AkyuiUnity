using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AkyuiUnity.Editor;
using AkyuiUnity.Editor.Extensions;
using AkyuiUnity.Loader;
using UnityEditor;
using UnityEngine;
using XdParser;

namespace AkyuiUnity.Xd
{
    public static class XdImporter
    {
        public static XdImportSettings Settings { get; private set; }
        public static XdFile XdFile { get; private set; }

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
                        var (imported, skipped) = ImportedArtboards(xdSettings, logger, xdFilePath, progress, loaders);
                        if (imported == 0 && skipped == 0)
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
                var stopWatch = Stopwatch.StartNew();
                foreach (var loader in loaders)
                {
                    using (logger.SetCategory(loader.LayoutInfo.Name))
                    {
                        var bytes = AkyuiCompressor.Compress(loader);
                        var outputPath = Path.Combine(xdSettings.AkyuiOutputPath, loader.LayoutInfo.Name + ".aky");
                        File.WriteAllBytes(outputPath, bytes);
                    }
                }
                stopWatch.Stop();
                logger.Log($"Export Akyui", ("time", $"{stopWatch.Elapsed.TotalSeconds:0.00}s"));
            }
        }

        private static (int Imported, int Skipped) ImportedArtboards(XdImportSettings xdSettings, AkyuiLogger logger, string xdFilePath, IAkyuiProgress progress, List<IAkyuiLoader> loaders)
        {
            logger.Log($"Xd Import Start");
            var stopWatch = Stopwatch.StartNew();
            var file = new XdFile(xdFilePath);
            XdFile = file;
            using (Disposable.Create(() => XdFile = null))
            {
                var imported = 0;
                var skipped = 0;

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
                        var name = artwork.Name;
                        var xdHash = artwork.Hash;
                        var akyuiXdObjectParsers = xdSettings.ObjectParsers ?? new AkyuiXdObjectParser[] { };
                        var akyuiXdGroupParsers = xdSettings.GroupParsers ?? new AkyuiXdGroupParser[] { };
                        var triggers = xdSettings.XdTriggers ?? new AkyuiXdImportTrigger[] { };

                        var userData = new Dictionary<string, string>
                        {
                            { "xd_hash", xdHash.ToString() }
                        };

                        var pathGetter = new PathGetter(xdSettings, name);
                        var prevMetaGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(pathGetter.MetaSavePath);
                        var prevMeta = prevMetaGameObject != null ? prevMetaGameObject.GetComponent<AkyuiMeta>() : null;
                        var prevMetaUserData = prevMeta != null ? prevMeta.FindUserData("xd_hash") : null;

                        if (!xdSettings.ReimportLayout && !xdSettings.ReimportAsset && prevMetaUserData != null && prevMetaUserData.value == xdHash.ToString())
                        {
                            logger.Log("Skip", ("hash", xdHash));
                            skipped++;
                            continue;
                        }

                        loaders.Add(new XdAkyuiLoader(file, artwork, name, userData, akyuiXdObjectParsers, akyuiXdGroupParsers, triggers));
                        imported++;
                    }
                }

                stopWatch.Stop();
                logger.Log($"Xd Import Finish", ("imported", imported), ("skipped", skipped), ("time", $"{stopWatch.Elapsed.TotalSeconds:0.00}s"));
                return (imported, skipped);
            }
        }
    }
}