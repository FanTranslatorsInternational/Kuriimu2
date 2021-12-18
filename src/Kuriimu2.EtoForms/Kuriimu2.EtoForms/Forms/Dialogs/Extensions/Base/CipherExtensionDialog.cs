using System;
using System.Collections.Generic;
using System.IO;
using Eto.Forms;
using Komponent.Extensions;
using Kryptography;
using Kryptography.AES;
using Kryptography.Blowfish;
using Kryptography.IntiCreates;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base
{
    abstract class CipherExtensionDialog : BaseExtensionsDialog<CipherStreamFactory, bool>
    {
        protected abstract void ProcessCipher(CipherStreamFactory cipherStreamFactory, Stream input, Stream output);

        #region Localization Keys

        private const string CipherNotSupportedKey_ = "CipherNotSupported";

        private const string ProcessFinishedTitleKey_ = "ProcessFinishedTitle";
        private const string ProcessFinishedCaptionKey_ = "ProcessFinishedTitle";
        private const string FileFailedKey_ = "FileFailed";

        #endregion

        protected override bool ProcessFile(CipherStreamFactory extensionType, string filePath)
        {
            Stream fileStream = null;
            Stream newFileStream = null;

            try
            {
                fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
                newFileStream = File.Create(filePath + ".out");

                ProcessCipher(extensionType, fileStream, newFileStream);
            }
            catch (Exception e)
            {
                // HINT: Log messages will be localized here, since they are shown to the user directly
                Logger.Error(Localize(FileFailedKey_,filePath,e.Message));
                return false;
            }
            finally
            {
                fileStream?.Close();
                newFileStream?.Close();
            }

            return true;
        }

        protected override void FinalizeProcess(IList<(string, bool)> results, string rootDir)
        {
            var reportFilePath = Path.Combine(rootDir, "cipherReport.txt");

            // Write hashes to text file
            var reportFile = File.CreateText(reportFilePath);
            foreach (var result in results)
                reportFile.WriteLine($"{result.Item1}: {result.Item2}");

            reportFile.Close();

            // Report finish
            MessageBox.Show(Localize(ProcessFinishedCaptionKey_,reportFilePath), Localize(ProcessFinishedTitleKey_), MessageBoxButtons.OK);
        }

        protected override IList<ExtensionType> LoadExtensionTypes()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("Xor",true,
                    new ExtensionTypeParameter("Key",typeof(string))),
                new ExtensionType("Positional Xor",true,
                    new ExtensionTypeParameter("Key",typeof(string))),
                new ExtensionType("Sequential Xor",true,
                    new ExtensionTypeParameter("Key",typeof(string)),
                    new ExtensionTypeParameter("Step",typeof(string))),
                new ExtensionType("Rot",true,
                    new ExtensionTypeParameter("Rotation",typeof(byte))),
                new ExtensionType("AES ECB",true,
                    new ExtensionTypeParameter("Key", typeof(string))),
                new ExtensionType("AES CBC",true,
                    new ExtensionTypeParameter("Key", typeof(string)),
                    new ExtensionTypeParameter("IV", typeof(string))),
                new ExtensionType("AES CTR",true,
                    new ExtensionTypeParameter("Key", typeof(string)),
                    new ExtensionTypeParameter("Ctr", typeof(string)),
                    new ExtensionTypeParameter("LECtr", typeof(bool))),
                new ExtensionType("AES XTS",true,
                    new ExtensionTypeParameter("Key", typeof(string)),
                    new ExtensionTypeParameter("SectorId", typeof(string)),
                    new ExtensionTypeParameter("AdvanceSector", typeof(bool)),
                    new ExtensionTypeParameter("LESectorId", typeof(bool)),
                    new ExtensionTypeParameter("SectorSize", typeof(int))),
                new ExtensionType("Blowfish",true,
                    new ExtensionTypeParameter("Key", typeof(string))),
                new ExtensionType("IntiCreates",false,
                    new ExtensionTypeParameter("Password",typeof(string)))
            };
        }

        protected override CipherStreamFactory CreateExtensionType(ExtensionType selectedExtension)
        {
            return new CipherStreamFactory(selectedExtension, CreateExtensionTypeInternal);
        }

        private Stream CreateExtensionTypeInternal(Stream input, ExtensionType selectedExtension)
        {
            switch (selectedExtension.Name)
            {
                case "Xor":
                    return new XorStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify());

                case "Positional Xor":
                    return new PositionalXorStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify());

                case "Sequential Xor":
                    var keyBuffer = selectedExtension.GetParameterValue<string>("Key").Hexlify();
                    var stepBuffer = selectedExtension.GetParameterValue<string>("Step").Hexlify();

                    var key = keyBuffer.Length >= 1 ? keyBuffer[0] : (byte)0;
                    var step = stepBuffer.Length >= 1 ? stepBuffer[0] : (byte)0;

                    return new SequentialXorStream(input, key, step);

                case "Rot":
                    return new RotStream(input,
                        selectedExtension.GetParameterValue<byte>("Rotation"));

                case "AES ECB":
                    return new EcbStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify());

                case "AES CBC":
                    return new CbcStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify(),
                        selectedExtension.GetParameterValue<string>("IV").Hexlify());

                case "AES CTR":
                    return new CtrStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify(),
                        selectedExtension.GetParameterValue<string>("Ctr").Hexlify(),
                        selectedExtension.GetParameterValue<bool>("LECtr"));

                case "AES XTS":
                    return new XtsStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify(),
                        selectedExtension.GetParameterValue<string>("SectorId").Hexlify(),
                        selectedExtension.GetParameterValue<bool>("AdvanceSector"),
                        selectedExtension.GetParameterValue<bool>("LESectorId"),
                        selectedExtension.GetParameterValue<int>("SectorSize"));

                case "Blowfish":
                    return new BlowfishStream(input,
                        selectedExtension.GetParameterValue<string>("Key").Hexlify());

                case "IntiCreates":
                    return new IntiCreatesStream(input,
                        selectedExtension.GetParameterValue<string>("Password"));

                // TODO: Plugin extensibility?
                // TODO: Add nintendo NCA stream stuff
                default:
                    throw new InvalidOperationException(Localize(CipherNotSupportedKey_,selectedExtension.Name));
            }
        }
    }

    class CipherStreamFactory
    {
        private readonly Func<Stream, ExtensionType, Stream> _createStreamDelegate;
        private readonly ExtensionType _extensionType;

        public CipherStreamFactory(ExtensionType extensionType, Func<Stream, ExtensionType, Stream> createStreamAction)
        {
            _createStreamDelegate = createStreamAction;
            _extensionType = extensionType;
        }

        public Stream CreateCipherStream(Stream input)
        {
            return _createStreamDelegate(input, _extensionType);
        }
    }
}
