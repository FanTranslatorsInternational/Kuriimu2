//using System;
//using System.Collections.Generic;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Kontract.Interfaces;
//using Kontract.Interfaces.Plugins.State.Image;

//namespace Kore.Batch
//{
//    public sealed class BatchExport<T>
//    {
//        public string InputDirectory { get; set; } = string.Empty;

//        public string SearchPattern { get; set; } = string.Empty;

//        public SearchOption SearchOption { get; set; } = SearchOption.TopDirectoryOnly;

//        public string OutputDirectory { get; set; } = string.Empty;

//        public bool OverwriteDestinationFiles { get; set; } = true;

//        public async Task<bool> Export(FileManager fileManager, IKuriimuProgress progress)
//        {
//            if (fileManager == null)
//            {
//                progress.Report("Kore was not initialized.",0);
//                return false;
//            }

//            if (InputDirectory == string.Empty)
//            {
//                progress.Report("An input directory was not provided.", 0);
//                return false;
//            }
//            if (!Directory.Exists(InputDirectory))
//            {
//                progress.Report("The input directory provided doesn't exist.", 0);
//                return false;
//            }

//            if (OutputDirectory != string.Empty && !Directory.Exists(OutputDirectory))
//            {
//                progress.Report("The output directory provided doesn't exist.", 0);
//                return false;
//            }

//            IEnumerable<string> patterns; // = SearchPattern.Trim() != string.Empty ? SearchPattern.Trim() : "";
//            IList<string> files;
//            var max = 0.0;
//            var current = 0.0;

//            switch (typeof(T).Name)
//            {
//                case nameof(IImageAdapter):
//                    patterns = fileManager.FileExtensionsByType<IImageAdapter>();
//                    files = Directory.EnumerateFiles(InputDirectory, "*", SearchOption).Where(f => patterns.Any(p => f.EndsWith(p, StringComparison.OrdinalIgnoreCase))).ToList();
//                    max = files.Count;

//                    foreach (var file in files)
//                    {
//                        await Task.Run(() =>
//                        {
//                            try
//                            {
//                                // TODO: Make an image export utility function out of this code
//                                //TODO
//                                var kfi = fileManager.LoadFile(new KoreLoadInfo(File.Open(file, FileMode.Open), file) { TrackFile = false });
//                                var adapter = (IImageAdapter)kfi.Adapter;

//                                foreach (var info in adapter.BitmapInfos)
//                                {
//                                    if (info.MipMapCount <= 1)
//                                        info.Image.Save(file + $".{info.Name.Replace(" ", "_")}.{adapter.BitmapInfos.IndexOf(info)}.png", ImageFormat.Png);
//                                    else
//                                        foreach (var bitmap in info.MipMaps)
//                                            bitmap.Save(file + $".{info.Name.Replace(" ", "_")}.{adapter.BitmapInfos.IndexOf(info)}.{info.MipMaps.IndexOf(bitmap)}.png", ImageFormat.Png);
//                                }
//                                current++;
//                                progress.Report($"Exported {Path.GetFileName(file)}.png ...", current / max * 100);
//                            }
//                            catch (Exception e)
//                            {
//                                current++;
//                                progress.Report($"Exported {Path.GetFileName(file)}.png ...", current / max * 100);
//                            }
//                        });
//                    }
//                    break;

//                default:
//                    progress.Report($"Exported {Path.GetFileName(file)}.png ...", current / max * 100);
//                    progress.Report(new ProgressReport { Message = $"The type {typeof(T).Name} is not a valid plugin interface or is not supported by this batch processor.", Percentage = 0 });
//                    return false;
//            }

//            return true;
//        }
//    }
//}
