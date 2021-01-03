using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Progress;
using Kontract.Kompression.Configuration;
using Kontract.Models.Archive;
using Kontract.Models.Dialog;
#pragma warning disable 649

namespace plugin_capcom.Archives
{
    class AAPackFileEntry
    {
        public uint offset;
        public uint flags;
        public uint uncompSize;
        public uint compSize;
        public uint hash;
    }

    class AAPackArchiveFileInfo:ArchiveFileInfo
    {
        public AAPackFileEntry Entry { get; }

        public AAPackArchiveFileInfo(Stream fileData, string filePath, AAPackFileEntry entry) : 
            base(fileData, filePath)
        {
            Entry = entry;
        }

        public AAPackArchiveFileInfo(Stream fileData, string filePath, IKompressionConfiguration configuration, long decompressedSize, AAPackFileEntry entry) : 
            base(fileData, filePath, configuration, decompressedSize)
        {
            Entry = entry;
        }

        public override long SaveFileData(Stream output, bool compress, IProgressContext progress = null)
        {
            var writtenSize= base.SaveFileData(output, compress, progress);

            while(output.Position%4!=0)
                output.WriteByte(0);

            return writtenSize;
        }
    }

    partial class AAPackSupport
    {
        public static string GetVersion(IDialogManager dialogManager)
        {
            var dialogField = new DialogField(DialogFieldType.DropDown, "Game Version:", "None", "None", "Ace Attorney Trilogy", "Apollo Justice");
            dialogManager.ShowDialog(new[] { dialogField });

            return dialogField.Result;
        }

        public static IDictionary<uint, string> GetMapping(string version)
        {
            switch (version)
            {
                case "Ace Attorney Trilogy":
                    return AaTriMapping;

                case "Apollo Justice":
                    return AjMapping;
            }

            return new Dictionary<uint, string>();
        }

        public static uint CreateHash(string input)
        {
            var hashResult = 0u;

            input = input.ToUpper();
            for (var position = 0; position < input.Length; position++)
            {
                var seed = GetSeed(position, input.Length);
                hashResult = (uint)(input[position] * seed + hashResult);
            }

            return hashResult;
        }

        private static int GetSeed(int position, int length)
        {
            var leastBit = GetLeastBit(position, length);
            var seed = leastBit == 1 ? 0x1F : 1;

            while (length - position - 1 > leastBit)
            {
                leastBit += 2;
                seed *= 0x3c1;
            }

            return seed;
        }

        private static int GetLeastBit(int position, int length)
        {
            if (position < length - 1)
                return ~(length - position) & 1;

            return 0;
        }
    }
}
