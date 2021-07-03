using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Configuration;
using Kompression.Implementations;
using Kompression.Implementations.Decoders.Headerless;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base
{
    abstract class CompressionExtensionDialog : BaseExtensionsDialog<ICompression, bool>
    {
        protected abstract void ProcessCompression(ICompression compression, Stream input, Stream output);

        #region Localization Keys

        private const string CompressionNotSupportedKey_ = "CompressionNotSupported";

        private const string FileFailedKey_ = "FileFailed";
        private const string ProcessFinishedTitleKey_ = "ProcessFinishedTitle";

        #endregion

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
                Logger.Error(Localize(FileFailedKey_, filePath, e.Message));
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
            // Report finish
            Logger.Information(Localize(ProcessFinishedTitleKey_));
        }

        protected override IList<ExtensionType> LoadExtensionTypes()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("Lz10 Headerless",true),
                new ExtensionType("Lz10 (Nintendo)",true),
                new ExtensionType("Lz11 (Nintendo)",true),
                new ExtensionType("Lz40 (Nintendo)",true),
                new ExtensionType("Lz60 (Nintendo)",true),
                new ExtensionType("Backwards Lz77 (Nintendo)",true),
                new ExtensionType("Huffman 4Bit (Nintendo)",true),
                new ExtensionType("Huffman 8Bit (Nintendo)",true),
                new ExtensionType("Rle (Nintendo)",true),
                new ExtensionType("Mio0",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),
                new ExtensionType("Yay0",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),
                new ExtensionType("Yaz0",true,
                    new ExtensionTypeParameter("LittleEndian",typeof(bool))),

                new ExtensionType("Lz10 (Level5)",true),
                new ExtensionType("Huffman 4Bit (Level5)",true),
                new ExtensionType("Huffman 8Bit (Level5)",true),
                new ExtensionType("Rle (Level5)",true),
                new ExtensionType("Inazuma3Lzss (Level5)",true),

                new ExtensionType("Lz77",true),
                new ExtensionType("LzEcd",true),
                new ExtensionType("Lze",true),
                new ExtensionType("Lzss",true),
                new ExtensionType("LzssVlc",true),
                new ExtensionType("TaikoLz",true,
                    new ExtensionTypeParameter("Version",typeof(TaikoLzVersion))),
                new ExtensionType("Wp16",true),
                new ExtensionType("TalesOfLz",true,
                    new ExtensionTypeParameter("Version",typeof(TalesOfVersion))),
                new ExtensionType("LzEnc",true),
                new ExtensionType("Shade Lz",true),
                new ExtensionType("Shade Headerless Lz",true),
                new ExtensionType("PsLz",true),
                new ExtensionType("IrLz",true),
                new ExtensionType("Crilayla",true),
                new ExtensionType("Iecp",true),
                new ExtensionType("Lz4 Headerless",true),
                new ExtensionType("Danganronpa 3",true),
                new ExtensionType("StingLz",true),

                new ExtensionType("Deflate",true),
                new ExtensionType("ZLib",true)
            };
        }

        protected override ICompression CreateExtensionType(ExtensionType selectedExtension)
        {
            switch (selectedExtension.Name)
            {
                case "Lz10 Headerless":
                    return new KompressionConfiguration()
                        .EncodeWith(() => new Lz10HeaderlessEncoder())
                        .DecodeWith(() => new Lz10HeaderlessDecoder())
                        .Build();

                case "Lz10 (Nintendo)":
                    return Compressions.Nintendo.Lz10.Build();

                case "Lz11 (Nintendo)":
                    return Compressions.Nintendo.Lz11.Build();

                case "Lz40 (Nintendo)":
                    return Compressions.Nintendo.Lz40.Build();

                case "Lz60 (Nintendo)":
                    return Compressions.Nintendo.Lz60.Build();

                case "Backwards Lz77 (Nintendo)":
                    return Compressions.Nintendo.BackwardLz77.Build();

                case "Huffman 4Bit (Nintendo)":
                    return Compressions.Nintendo.Huffman4Bit.Build();

                case "Huffman 8Bit (Nintendo)":
                    return Compressions.Nintendo.Huffman8Bit.Build();

                case "Rle (Nintendo)":
                    return Compressions.Nintendo.Rle.Build();

                case "Mio0":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.Nintendo.Mio0Le.Build() :
                        Compressions.Nintendo.Mio0Be.Build();

                case "Yay0":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.Nintendo.Yay0Le.Build() :
                        Compressions.Nintendo.Yay0Be.Build();

                case "Yaz0":
                    return selectedExtension.GetParameterValue<bool>("LittleEndian") ?
                        Compressions.Nintendo.Yaz0Le.Build() :
                        Compressions.Nintendo.Yaz0Be.Build();

                case "Lz10 (Level5)":
                    return Compressions.Level5.Lz10.Build();

                case "Huffman 4Bit (Level5)":
                    return Compressions.Level5.Huffman4Bit.Build();

                case "Huffman 8Bit (Level5)":
                    return Compressions.Level5.Huffman8Bit.Build();

                case "Rle (Level5)":
                    return Compressions.Level5.Rle.Build();

                case "Inazuma3Lzss (Level5)":
                    return Compressions.Level5.Inazuma3Lzss.Build();

                case "Lz77":
                    return Compressions.Lz77.Build();

                case "LzEcd":
                    return Compressions.LzEcd.Build();

                case "Lze":
                    return Compressions.Lze.Build();

                case "Lzss":
                    return Compressions.Lzss.Build();

                case "LzssVlc":
                    return Compressions.LzssVlc.Build();

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

                case "Shade Lz":
                    return Compressions.ShadeLz.Build();

                case "Shade Headerless Lz":
                    return Compressions.ShadeLzHeaderless.Build();

                case "PsLz":
                    return Compressions.PsLz.Build();

                case "IrLz":
                    return Compressions.IrLz.Build();

                case "Crilayla":
                    return Compressions.Crilayla.Build();

                case "Iecp":
                    return Compressions.Iecp.Build();

                case "Lz4 Headerless":
                    return Compressions.Lz4Headerless.Build();

                case "Danganronpa 3":
                    return Compressions.Danganronpa3.Build();

                case "StingLz":
                    return Compressions.StingLz.Build();

                case "Deflate":
                    return Compressions.Deflate.Build();

                case "ZLib":
                    return Compressions.ZLib.Build();

                // TODO: Plugin extensibility?
                default:
                    throw new InvalidOperationException(Localize(CompressionNotSupportedKey_, selectedExtension.Name));
            }
        }
    }

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
}
