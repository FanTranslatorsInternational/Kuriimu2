using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eto.Forms;
using Kryptography.Hash;
using Kryptography.Hash.Crc;
using Kryptography.Hash.Fnv;
using Kuriimu2.EtoForms.Forms.Dialogs.Extensions.Base;
using Kuriimu2.EtoForms.Forms.Models;

namespace Kuriimu2.EtoForms.Forms.Dialogs.Extensions
{
    class HashExtensionDialog : BaseExtensionsDialog<IHash, byte[]>
    {
        protected override string TypeExtensionName => "Hash";

        protected override byte[] ProcessFile(IHash extensionType, string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            return extensionType.Compute(data);
        }

        protected override void FinalizeProcess(IList<(string, byte[])> results, string rootDir)
        {
            var reportFilePath = Path.Combine(rootDir, "hashReport.txt");

            // Write hashes to text file
            var reportFile = File.CreateText(reportFilePath);
            foreach (var (file, hash) in results)
                reportFile.WriteLine($"{file}: {hash.Aggregate("", (a, b) => $"{a}{b:x2}")}");

            reportFile.Close();

            // Report finish
            MessageBox.Show($"The results are written to '{reportFilePath}'.", "Done", MessageBoxButtons.OK);
        }

        protected override IList<ExtensionType> LoadExtensionTypes()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("Crc32", true),
                new ExtensionType("Crc32 Namco", true),
                new ExtensionType("Crc32 Custom", true,
                    new ExtensionTypeParameter("Polynomial", typeof(uint))),
                new ExtensionType("Crc16 X25", true),
                new ExtensionType("Fnv1", true),
                new ExtensionType("Fnv1a", true),
                new ExtensionType("SimpleHash", true,
                    new ExtensionTypeParameter("Seed", typeof(uint))),
                new ExtensionType("Xbb", true),
                new ExtensionType("Sha256", true)
            };
        }

        protected override IHash CreateExtensionType(ExtensionType extensionType)
        {
            switch (extensionType.Name)
            {
                case "Crc32":
                    return Crc32.Default;

                case "Crc32 Namco":
                    return Crc32Namco.Create();

                case "Crc32 Custom":
                    return Crc32.Create(Crc32Formula.Normal, extensionType.GetParameterValue<uint>("Polynomial"));

                case "Crc16 X25":
                    return Crc16.X25;

                case "Fnv1":
                    return Fnv1.Create();

                case "Fnv1a":
                    return Fnv1a.Create();

                case "SimpleHash":
                    return new SimpleHash(extensionType.GetParameterValue<uint>("Seed"));

                case "Xbb":
                    return new XbbHash();

                case "Sha256":
                    return new Sha256();

                // TODO: Plugin extensibility?
                default:
                    return null;
            }
        }
    }
}
