using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Implementations;
using Kontract.Kompression;
using Kontract.Models.Logging;
using Kuriimu2.WinForms.ExtensionForms.Models;

namespace Kuriimu2.WinForms.ExtensionForms
{
    enum TaikoLzVersion
    {
        Lz80,
        Lz81
    }

    enum TalesOfVersion
    {
        Lz01,
        Lz03
    }

    abstract class CompressionTypeExtensionForm : TypeExtensionForm<ICompression, bool>
    {
        protected abstract void ProcessCompression(ICompression compression, Stream input, Stream output);

        protected override string TypeExtensionName => "Compression";

        protected override bool ProcessFile(ICompression extensionType, string filePath)
        {
            Stream input = null;
            Stream output = null;

            try
            {
                input = File.Open(filePath, FileMode.Open, FileAccess.Read);
                output = File.Create(filePath + ".out");

                ProcessCompression(extensionType, input, output);
            }
            catch (Exception e)
            {
                Logger.QueueMessage(LogLevel.Error, e.Message);

                return false;
            }
            finally
            {
                input?.Close();
                output?.Close();
            }

            return true;
        }

        protected override void FinalizeProcess(IList<(string, bool)> results, string rootDir)
        {
            //var reportFilePath = Path.Combine(rootDir, "compressionReport.txt");

            // Write errors to log
            //var reportFile = File.CreateText(reportFilePath);
            foreach (var result in results)
                if (!result.Item2)
                    Logger.QueueMessage(LogLevel.Error, $"Not processed successfully: {result.Item1}");

            //reportFile.Close();

            // Report finish
            Logger.QueueMessage(LogLevel.Information, "Done!");
        }

        protected override IList<ExtensionType> LoadExtensionTypes()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("Lz10",true),
                new ExtensionType("Lz11",true),
                new ExtensionType("Lz40",true),
                new ExtensionType("Lz60",true),
                new ExtensionType("Lz77",true),
                new ExtensionType("Backwards Lz77",true),
                new ExtensionType("LzEcd",true),
                new ExtensionType("Lze",true),
                new ExtensionType("Lzss",true),
                new ExtensionType("LzssVlc",true),
                new ExtensionType("Huffman 4Bit (Nintendo)",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),
                new ExtensionType("Huffman 8Bit (Nintendo)",true),
                new ExtensionType("Rle (Nintendo)",true),
                new ExtensionType("Mio0",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),
                new ExtensionType("Yay0",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),
                new ExtensionType("Yaz0",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),
                new ExtensionType("TaikoLz",true,
                    new ExtensionTypeParameter("Version",typeof(TaikoLzVersion))),
                new ExtensionType("Wp16",true),
                new ExtensionType("TalesOfLz",true,
                    new ExtensionTypeParameter("Version",typeof(TalesOfVersion))),
                new ExtensionType("LzEnc",true),
                new ExtensionType("Spike Chunsoft Lz",true),
                new ExtensionType("PsLz",true)
            };
        }

        protected override ICompression CreateExtensionType(ExtensionType selectedExtension)
        {
            switch (selectedExtension.Name)
            {
                case "Lz10":
                    return Compressions.Lz10.Build();

                case "Lz11":
                    return Compressions.Lz11.Build();

                case "Lz40":
                    return Compressions.Lz40.Build();

                case "Lz60":
                    return Compressions.Lz60.Build();

                case "Lz77":
                    return Compressions.Lz77.Build();

                case "Backwards Lz77":
                    return Compressions.BackwardLz77.Build();

                case "LzEcd":
                    return Compressions.LzEcd.Build();

                case "Lze":
                    return Compressions.Lze.Build();

                case "Lzss":
                    return Compressions.Lzss.Build();

                case "LzssVlc":
                    return Compressions.LzssVlc.Build();

                case "Huffman 4Bit (Nintendo)":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.NintendoHuffman4BitLe.Build() :
                        Compressions.NintendoHuffman4BitBe.Build();

                case "Huffman 8Bit (Nintendo)":
                    return Compressions.NintendoHuffman8Bit.Build();

                case "Rle (Nintendo)":
                    return Compressions.NintendoRle.Build();

                case "Mio0":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.Mio0Le.Build() :
                        Compressions.Mio0Be.Build();

                case "Yay0":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.Yay0Le.Build() :
                        Compressions.Yay0Be.Build();

                case "Yaz0":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.Yaz0Le.Build() :
                        Compressions.Yaz0Be.Build();

                case "TaikoLz":
                    switch (selectedExtension.GetParameterValue<TaikoLzVersion>("Version"))
                    {
                        case TaikoLzVersion.Lz80:
                            return Compressions.TaikoLz80.Build();

                        case TaikoLzVersion.Lz81:
                            return Compressions.TaikoLz81.Build();

                        default:
                            return default;
                    }

                case "Wp16":
                    return Compressions.Wp16.Build();

                case "TalesOfLz":
                    switch (selectedExtension.GetParameterValue<TalesOfVersion>("Version"))
                    {
                        case TalesOfVersion.Lz01:
                            return Compressions.TalesOf01.Build();

                        case TalesOfVersion.Lz03:
                            return Compressions.TalesOf03.Build();

                        default:
                            return default;
                    }

                case "LzEnc":
                    return Compressions.LzEnc.Build();

                case "Soike Chunsoft Lz":
                    return Compressions.SpikeChunsoft.Build();

                case "PsLz":
                    return Compressions.PsLz.Build();

                // TODO: Plugin extensibility?
                default:
                    return default;
            }
        }
    }
}
