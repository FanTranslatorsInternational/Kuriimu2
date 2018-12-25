using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Interfaces;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kontract.Interfaces.VirtualFS;
using Kore.SamplePlugins;

namespace Kore.Batch
{
    public sealed class BatchExport<T>
    {
        public string InputDirectory { get; set; } = string.Empty;

        public string SearchPattern { get; set; } = string.Empty;

        public SearchOption SearchOption { get; set; } = SearchOption.TopDirectoryOnly;

        public string OutputDirectory { get; set; } = string.Empty;

        public bool OverwriteDestinationFiles { get; set; } = true;

        public async Task<bool> Export(Kore kore, IProgress<ProgressReport> progress)
        {
            if (kore == null)
            {
                progress.Report(new ProgressReport { Message = "Kore was not initialized.", Percentage = 0 });
                return false;
            }

            if (InputDirectory == string.Empty)
            {
                progress.Report(new ProgressReport { Message = "An input directory was not provided.", Percentage = 0 });
                return false;
            }
            if (!Directory.Exists(InputDirectory))
            {
                progress.Report(new ProgressReport { Message = "The input directory provided doesn't exist.", Percentage = 0 });
                return false;
            }

            if (OutputDirectory != string.Empty && !Directory.Exists(OutputDirectory))
            {
                progress.Report(new ProgressReport { Message = "The output directory provided doesn't exist.", Percentage = 0 });
                return false;
            }

            IEnumerable<string> patterns; // = SearchPattern.Trim() != string.Empty ? SearchPattern.Trim() : "";
            IList<string> files;
            var max = 0.0;
            var current = 0.0;

            switch (typeof(T).Name)
            {
                case nameof(ITextAdapter):
                    patterns = kore.FileExtensionsByType<ITextAdapter>();
                    files = Directory.EnumerateFiles(InputDirectory, "*", SearchOption).Where(f => patterns.Any(p => f.EndsWith(p, StringComparison.OrdinalIgnoreCase))).ToList();
                    max = files.Count;

                    foreach (var file in files)
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                var outFile = file + Utilities.Common.GetAdapterExtension<KupAdapter>();
                                //TODO
                                Utilities.Text.ExportKup((ITextAdapter)kore.LoadFile(file, false).Adapter, outFile);
                                current++;
                                progress.Report(new ProgressReport { Message = $"Exported {Path.GetFileName(outFile)}...", Percentage = current / max * 100, Data = ((int)current, (int)max) });
                            }
                            catch (Exception e)
                            {
                                current++;
                                progress.Report(new ProgressReport { Percentage = current / max * 100 });
                            }
                        });
                    }
                    break;
                case nameof(IImageAdapter):
                    patterns = kore.FileExtensionsByType<IImageAdapter>();
                    files = Directory.EnumerateFiles(InputDirectory, "*", SearchOption).Where(f => patterns.Any(p => f.EndsWith(p, StringComparison.OrdinalIgnoreCase))).ToList();
                    max = files.Count;

                    foreach (var file in files)
                    {
                        await Task.Run(() =>
                        {
                            try
                            {
                                // TODO: Make an image export utility function out of this code
                                //TODO
                                var kfi = kore.LoadFile(file, false);
                                var adapter = (IImageAdapter)kfi.Adapter;

                                foreach (var info in adapter.BitmapInfos)
                                {
                                    if (info.MipMapCount <= 1)
                                        info.Bitmaps.First().Save(file + $".{info.Name.Replace(" ", "_")}.{adapter.BitmapInfos.IndexOf(info)}.png", ImageFormat.Png);
                                    else
                                        foreach (var bitmap in info.Bitmaps)
                                            bitmap.Save(file + $".{info.Name.Replace(" ", "_")}.{adapter.BitmapInfos.IndexOf(info)}.{info.Bitmaps.IndexOf(bitmap)}.png", ImageFormat.Png);
                                }
                                current++;
                                progress.Report(new ProgressReport { Message = $"Exported {Path.GetFileName(file)}.png...", Percentage = current / max * 100, Data = ((int)current, (int)max) });
                            }
                            catch (Exception e)
                            {
                                current++;
                                progress.Report(new ProgressReport { Percentage = current / max * 100 });
                            }
                        });
                    }
                    break;
                default:
                    progress.Report(new ProgressReport { Message = $"The type {typeof(T).Name} is not a valid plugin interface or is not supported by this batch processor.", Percentage = 0 });
                    return false;
            }

            return true;
        }
    }
}
