using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kompression;
using Kompression.Configuration;
using Kompression.Implementations;
using Kuriimu2.WinForms.ExtensionForms.Models;

namespace Kuriimu2.WinForms.ExtensionForms
{
    abstract class CompressionTypeExtensionForm : TypeExtensionForm<ICompression, bool>
    {
        protected abstract void ProcessCompression(ICompression compression, Stream input, Stream output);

        protected override string TypeExtensionName => "Compression";

        protected override bool ProcessFile(ICompression extensionType, string filePath)
        {
            try
            {
                var input = File.Open(filePath, FileMode.Open, FileAccess.Read);
                var output = File.Create(filePath + ".out");

                ProcessCompression(extensionType, input, output);

                input.Close();
                output.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }

        protected override void FinalizeProcess(IList<(string, bool)> results, string rootDir)
        {
            throw new NotImplementedException();
        }

        protected override IList<ExtensionType> LoadExtensionTypes()
        {
            var compressionTypes = typeof(Compressions).GetProperties(BindingFlags.Static | BindingFlags.Public);
            return compressionTypes.Select(p => new ExtensionType(p.Name, true)).ToList();
        }

        protected override ICompression CreateExtensionType(ExtensionType selectedExtension)
        {
            var compression = typeof(Compressions).GetProperty(selectedExtension.Name, BindingFlags.Static | BindingFlags.Public);
            if (compression != null)
                return ((KompressionConfiguration)compression.GetValue(null)).Build();

            switch (selectedExtension.Name)
            {
                // TODO: Plugin extensibility?
                default:
                    return default;
            }
        }
    }
}
