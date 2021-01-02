using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Extensions;
using Kontract.Models.Archive;

namespace plugin_nintendo.Archives
{
    class NDS
    {
        private readonly int OverlayEntrySize = Tools.MeasureType(typeof(OverlayEntry));
        private readonly int FatEntrySize = Tools.MeasureType(typeof(FatEntry));

        private NDSHeader _ndsHeader;
        private DSiHeader _dsiHeader;
        private Arm9Footer _arm9Footer;

        public IList<IArchiveFileInfo> Load(Stream input)
        {
            var result = new List<IArchiveFileInfo>();
            using var br = new BinaryReaderX(input, true);

            // Read unit code
            input.Position = 0x12;
            var unitCode = br.ReadType<UnitCode>();

            // Read header
            input.Position = 0;
            if (unitCode == UnitCode.NDS)
                _ndsHeader = br.ReadType<NDSHeader>();
            else
                _dsiHeader = br.ReadType<DSiHeader>();

            // Read ARM9
            var arm9Offset = _ndsHeader?.arm9Offset ?? _dsiHeader.arm9Offset;
            var arm9Size = _ndsHeader?.arm9Size ?? _dsiHeader.arm9Size;
            result.Add(NdsSupport.CreateAfi(input, arm9Offset, arm9Size, "sys/arm9.bin"));

            // Read ARM9 Footer
            input.Position = arm9Offset + arm9Size;
            var nitroCode = br.ReadUInt32();
            if (nitroCode == 0xDEC00621)
            {
                input.Position -= 4;
                _arm9Footer = br.ReadType<Arm9Footer>();
            }

            // Read ARM9 Overlays
            var arm9OvlOffset = _ndsHeader?.arm9OverlayOffset ?? _dsiHeader.arm9OverlayOffset;
            var arm9OvlSize = _ndsHeader?.arm9OverlaySize ?? _dsiHeader.arm9OverlaySize;
            var arm9OvlEntryCount = arm9OvlSize / OverlayEntrySize;

            input.Position = arm9OvlOffset;
            IList<OverlayEntry> arm9OverlayEntries = Array.Empty<OverlayEntry>();
            if (arm9OvlOffset != 0)
                arm9OverlayEntries = br.ReadMultiple<OverlayEntry>(arm9OvlEntryCount);

            // Read ARM7
            var arm7Offset = _ndsHeader?.arm7Offset ?? _dsiHeader.arm7Offset;
            var arm7Size = _ndsHeader?.arm7Size ?? _dsiHeader.arm7Size;
            result.Add(NdsSupport.CreateAfi(input, arm7Offset, arm7Size, "sys/arm7.bin"));

            // Read ARM7 Overlays
            var arm7OvlOffset = _ndsHeader?.arm7OverlayOffset ?? _dsiHeader.arm7OverlayOffset;
            var arm7OvlSize = _ndsHeader?.arm7OverlaySize ?? _dsiHeader.arm7OverlaySize;
            var arm7OvlEntryCount = arm7OvlSize / OverlayEntrySize;

            input.Position = arm7OvlOffset;
            IList<OverlayEntry> arm7OverlayEntries = Array.Empty<OverlayEntry>();
            if (arm7OvlOffset != 0)
                arm7OverlayEntries = br.ReadMultiple<OverlayEntry>(arm7OvlEntryCount);

            // Read FAT
            var fatOffset = _ndsHeader?.fatOffset ?? _dsiHeader.fatOffset;
            var fatSize = _ndsHeader?.fatSize ?? _dsiHeader.fatSize;
            var fatCount = fatSize / FatEntrySize;

            input.Position = fatOffset;
            var fileEntries = br.ReadMultiple<FatEntry>(fatCount);

            // Read FNT
            var fntOffset = _ndsHeader?.fntOffset ?? _dsiHeader.fntOffset;
            foreach (var file in NdsSupport.ReadFnt(br, fntOffset, fileEntries))
                result.Add(file);

            // Add banner
            var iconOffset = _ndsHeader?.iconOffset ?? _dsiHeader.iconOffset;
            var iconAfi = ReadIcon(br, iconOffset);
            if (iconAfi != null)
                result.Add(iconAfi);

            // Add overlay files
            foreach (var file in arm9OverlayEntries)
                result.Add(NdsSupport.CreateAfi(input, fileEntries[file.fileId].offset, fileEntries[file.fileId].Length, Path.Combine("sys", "ovl", $"overlay9_{file.id:000}"), file));
            foreach (var file in arm7OverlayEntries)
                result.Add(NdsSupport.CreateAfi(input, fileEntries[file.fileId].offset, fileEntries[file.fileId].Length, Path.Combine("sys", "ovl", $"overlay7_{file.id:000}"), file));

            return result;
        }

        public void Save(Stream output, IList<IArchiveFileInfo> files)
        {
            var arm9File = (ArchiveFileInfo)files.First(x => x.FilePath.ToRelative() == Path.Combine("sys", "arm9.bin"));
            var arm7File = (ArchiveFileInfo)files.First(x => x.FilePath.ToRelative() == Path.Combine("sys", "arm7.bin"));
            var iconFile = (ArchiveFileInfo)files.FirstOrDefault(x => x.FilePath.ToRelative() == Path.Combine("sys", "banner.bin"));

            var arm9Overlays = files.Where(x => x.FilePath.ToRelative().IsInDirectory(Path.Combine("sys", "ovl"), false) &&
                                                x.FilePath.GetName().StartsWith("overlay9"))
                .Cast<OverlayArchiveFileInfo>().ToArray();
            var arm7Overlays = files.Where(x => x.FilePath.ToRelative().IsInDirectory(Path.Combine("sys", "ovl"), false) &&
                                                x.FilePath.GetName().StartsWith("overlay7"))
                .Cast<OverlayArchiveFileInfo>().ToArray();

            var arm9OverlayEntries = new List<OverlayEntry>();
            var arm7OverlayEntries = new List<OverlayEntry>();
            var fatEntries = new List<FatEntry>();

            using var bw = new BinaryWriterX(output, true);

            // Write ARM9
            var arm9Offset = output.Position = 0x4000;
            var arm9Size = arm9File.SaveFileData(output);
            if (_arm9Footer != null)
                bw.WriteType(_arm9Footer);
            bw.WriteAlignment(0x200, 0xFF);

            // Write ARM9 Overlays
            var arm9OverlayOffset = output.Position;
            var arm9OverlaySize = arm9Overlays.Length * OverlayEntrySize;
            var arm9OverlayPosition = (arm9OverlayOffset + arm9OverlaySize + 0x1FF) & ~0x1FF;
            foreach (var arm9Overlay in arm9Overlays.OrderBy(x => x.Entry.id))
            {
                output.Position = arm9OverlayPosition;
                var writtenSize = arm9Overlay.SaveFileData(output);
                bw.WriteAlignment(0x200, 0xFF);

                arm9Overlay.Entry.fileId = fatEntries.Count;
                arm9OverlayEntries.Add(arm9Overlay.Entry);

                fatEntries.Add(new FatEntry
                {
                    offset = (int)arm9OverlayPosition,
                    endOffset = (int)(arm9OverlayPosition + writtenSize)
                });

                arm9OverlayPosition += (writtenSize + 0x1FF) & ~0x1FF;
            }

            output.Position = arm9OverlayOffset;
            bw.WriteMultiple(arm9OverlayEntries);
            bw.WriteAlignment(0x200, 0xFF);
            output.Position = arm9OverlayPosition;

            // Write ARM7
            var arm7Offset = output.Position = arm9OverlayPosition;
            var arm7Size = arm7File.SaveFileData(output);

            // Write ARM7 Overlays
            var arm7OverlayOffset = output.Position = arm7Offset + arm7Size;
            var arm7OverlaySize = arm7Overlays.Length * OverlayEntrySize;
            var arm7OverlayPosition = (arm7OverlayOffset + arm7OverlaySize + 0x1FF) & ~0x1FF;
            foreach (var arm7Overlay in arm7Overlays)
            {
                output.Position = arm7OverlayPosition;
                var writtenSize = arm7Overlay.SaveFileData(output);
                bw.WriteAlignment(0x200, 0xFF);

                arm7Overlay.Entry.fileId = fatEntries.Count;
                arm7OverlayEntries.Add(arm7Overlay.Entry);

                fatEntries.Add(new FatEntry
                {
                    offset = (int)arm7OverlayPosition,
                    endOffset = (int)(arm7OverlayPosition + writtenSize)
                });

                arm7OverlayPosition += (writtenSize + 0x1FF) & ~0x1FF;
            }

            output.Position = arm7OverlayOffset;
            bw.WriteMultiple(arm7OverlayEntries);
            bw.WriteAlignment(0x200, 0xFF);
            output.Position = arm7OverlayPosition;

            // Write FAT and FNT
            var romFiles = files.Where(x => !x.FilePath.ToRelative().IsInDirectory(Path.Combine("sys"), true)).ToArray();

            // Write FNT
            var fntOffset = arm7OverlayPosition;
            NdsSupport.WriteFnt(bw, (int)fntOffset, romFiles, arm9Overlays.Length + arm7Overlays.Length);

            var fntSize = bw.BaseStream.Position - fntOffset;
            bw.WriteAlignment(0x200, 0xFF);

            var fatOffset = bw.BaseStream.Position;
            var fatSize = (files.Count - 3) * FatEntrySize;     // Not counting arm9.bin, arm7.bin, banner.bin

            // Write icon
            var iconOffset = (fatOffset + fatSize + 0x1FF) & ~0x1FF;
            var iconSize = 0;
            if (iconFile != null)
            {
                output.Position = iconOffset;
                iconSize = (int)iconFile.SaveFileData(output);

                bw.WriteAlignment(0x200, 0xFF);
            }

            // Write rom files
            var filePosition = (iconOffset + iconSize + 0x1FF) & ~0x1FF;
            foreach (var romFile in romFiles.Cast<FileIdArchiveFileInfo>().OrderBy(x => x.FileId))
            {
                output.Position = filePosition;

                var romFileSize = romFile.SaveFileData(output);

                fatEntries.Add(new FatEntry
                {
                    offset = (int)filePosition,
                    endOffset = (int)(filePosition + romFileSize)
                });

                filePosition += (romFileSize + 0x1FF) & ~0x1FF;
            }

            // Write FAT
            bw.BaseStream.Position = fatOffset;
            bw.WriteMultiple(fatEntries);
            bw.WriteAlignment(0x200, 0xFF);

            // Write header
            output.Position = 0;

            if (_ndsHeader != null)
            {
                _ndsHeader.arm9Offset = (int)arm9Offset;
                _ndsHeader.arm7Offset = (int)arm7Offset;
                _ndsHeader.arm9OverlayOffset = (int)(arm9Overlays.Length > 0 ? arm9OverlayOffset : 0);
                _ndsHeader.arm7OverlayOffset = (int)(arm7Overlays.Length > 0 ? arm7OverlayOffset : 0);
                _ndsHeader.fntOffset = (int)fntOffset;
                _ndsHeader.fatOffset = (int)fatOffset;
                _ndsHeader.iconOffset = (int)iconOffset;

                _ndsHeader.arm9Size = (int)arm9Size;
                _ndsHeader.arm7Size = (int)arm7Size;
                _ndsHeader.arm9OverlaySize = (int)(arm9Overlays.Length > 0 ? arm9OverlaySize : 0);
                _ndsHeader.arm7OverlaySize = (int)(arm7Overlays.Length > 0 ? arm7OverlaySize : 0);
                _ndsHeader.fntSize = (int)fntSize;
                _ndsHeader.fatSize = (int)fatSize;

                bw.WriteType(_ndsHeader);
            }
            else
            {
                _dsiHeader.arm9Offset = (int)arm9Offset;
                _dsiHeader.arm7Offset = (int)arm7Offset;
                _dsiHeader.arm9OverlayOffset = (int)(arm9Overlays.Length > 0 ? arm9OverlayOffset : 0);
                _dsiHeader.arm7OverlayOffset = (int)(arm7Overlays.Length > 0 ? arm7OverlayOffset : 0);
                _dsiHeader.fntOffset = (int)fntOffset;
                _dsiHeader.fatOffset = (int)fatOffset;
                _dsiHeader.iconOffset = (int)iconOffset;

                _dsiHeader.arm9Size = (int)arm9Size;
                _dsiHeader.arm7Size = (int)arm7Size;
                _dsiHeader.arm9OverlaySize = (int)(arm9Overlays.Length > 0 ? arm9OverlaySize : 0);
                _dsiHeader.arm7OverlaySize = (int)(arm7Overlays.Length > 0 ? arm7OverlaySize : 0);
                _dsiHeader.fntSize = (int)fntSize;
                _dsiHeader.fatSize = (int)fatSize;
                _dsiHeader.extendedEntries.iconSize = iconSize;

                bw.WriteType(_dsiHeader);
            }
        }

        private ArchiveFileInfo ReadIcon(BinaryReaderX br, int iconOffset)
        {
            if (iconOffset == 0)
                return null;

            br.BaseStream.Position = iconOffset;
            var version = br.ReadInt16();

            int iconSize;
            switch (version)
            {
                case 1:
                case 2:
                    iconSize = 0xA00;
                    break;

                case 3:
                    iconSize = 0xC00;
                    break;

                case 0x103:
                    if (_dsiHeader == null)
                        throw new InvalidOperationException("Icon version 0x103 is only supported on DSi cards.");

                    iconSize = _dsiHeader.extendedEntries.iconSize;
                    break;

                default:
                    throw new InvalidOperationException($"Invalid icon version '{version}'.");
            }

            return new ArchiveFileInfo(new SubStream(br.BaseStream, iconOffset, iconSize), "sys/banner.bin");
        }
    }
}
