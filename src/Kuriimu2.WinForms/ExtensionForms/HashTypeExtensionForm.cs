using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Kryptography.Hash;
using Kryptography.Hash.Crc;
using Kuriimu2.WinForms.ExtensionForms.Models;

namespace Kuriimu2.WinForms.ExtensionForms
{
    class HashTypeExtensionForm : TypeExtensionForm<IHash, byte[]>
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
            foreach (var result in results)
                reportFile.WriteLine($"{result.Item1}: {result.Item2.Aggregate("", (a, b) => $"{a}{b:x2}")}");

            reportFile.Close();

            // Report finish
            MessageBox.Show($"The results are written to '{reportFilePath}'.", "Done", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override IList<ExtensionType> LoadExtensionTypes()
        {
            return new List<ExtensionType>
            {
                new ExtensionType("Crc32", true),
                new ExtensionType("Crc32 Custom", true,
                    new ExtensionTypeParameter("Polynomial", typeof(uint)))
            };
        }

        protected override IHash CreateExtensionType(ExtensionType extensionType)
        {
            switch (extensionType.Name)
            {
                case "Crc32":
                    return Crc32.Create(Crc32Formula.Normal);

                case "Crc32 Custom":
                    return Crc32.Create(Crc32Formula.Normal, extensionType.GetParameterValue<uint>("Polynomial"));

                // TODO: Plugin extensibility?
                default:
                    return null;
            }
        }
    }
}
