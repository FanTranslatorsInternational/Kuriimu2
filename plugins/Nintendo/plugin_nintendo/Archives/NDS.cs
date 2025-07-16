using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.Plugin.File.Archive;
using Konnect.Contract.Plugin.File.Archive;
using Konnect.Extensions;
using Konnect.Plugin.File.Archive;

namespace plugin_nintendo.Archives
{
    class Nds
    {
        private const int OverlayEntrySize_ = 0x20;
        private const int FatEntrySize_ = 0x8;

        private NdsHeader _ndsHeader;
        private DsiHeader _dsiHeader;
        private Arm9Footer _arm9Footer;

        public List<IArchiveFile> Load(Stream input)
        {
            var result = new List<IArchiveFile>();

            using var br = new BinaryReaderX(input, true);

            // Read unit code
            input.Position = 0x12;
            var unitCode = (UnitCode)br.ReadByte();

            // Read header
            input.Position = 0;
            if (unitCode == UnitCode.NDS)
                _ndsHeader = ReadNdsHeader(br);
            else
                _dsiHeader = ReadDsiHeader(br);

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
                _arm9Footer = ReadArm9Footer(br);
            }

            // Read ARM9 Overlays
            var arm9OvlOffset = _ndsHeader?.arm9OverlayOffset ?? _dsiHeader.arm9OverlayOffset;
            var arm9OvlSize = _ndsHeader?.arm9OverlaySize ?? _dsiHeader.arm9OverlaySize;
            var arm9OvlEntryCount = arm9OvlSize / OverlayEntrySize_;

            input.Position = arm9OvlOffset;
            IList<OverlayEntry> arm9OverlayEntries = Array.Empty<OverlayEntry>();
            if (arm9OvlOffset != 0)
                arm9OverlayEntries = ReadOverlayEntries(br, arm9OvlEntryCount);

            // Read ARM7
            var arm7Offset = _ndsHeader?.arm7Offset ?? _dsiHeader.arm7Offset;
            var arm7Size = _ndsHeader?.arm7Size ?? _dsiHeader.arm7Size;
            result.Add(NdsSupport.CreateAfi(input, arm7Offset, arm7Size, "sys/arm7.bin"));

            // Read ARM7 Overlays
            var arm7OvlOffset = _ndsHeader?.arm7OverlayOffset ?? _dsiHeader.arm7OverlayOffset;
            var arm7OvlSize = _ndsHeader?.arm7OverlaySize ?? _dsiHeader.arm7OverlaySize;
            var arm7OvlEntryCount = arm7OvlSize / OverlayEntrySize_;

            input.Position = arm7OvlOffset;
            IList<OverlayEntry> arm7OverlayEntries = Array.Empty<OverlayEntry>();
            if (arm7OvlOffset != 0)
                arm7OverlayEntries = ReadOverlayEntries(br, arm7OvlEntryCount);

            // Read FAT
            var fatOffset = _ndsHeader?.fatOffset ?? _dsiHeader.fatOffset;
            var fatSize = _ndsHeader?.fatSize ?? _dsiHeader.fatSize;
            var fatCount = fatSize / FatEntrySize_;

            input.Position = fatOffset;
            var fileEntries = ReadFatEntries(br, fatCount);

            // Read FNT
            var fntOffset = _ndsHeader?.fntOffset ?? _dsiHeader.fntOffset;
            foreach (var file in NdsSupport.ReadFnt(br, fntOffset, 0, fileEntries))
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

        public void Save(Stream output, List<IArchiveFile> files)
        {
            var arm9File = files.First(x => x.FilePath.ToRelative() == Path.Combine("sys", "arm9.bin"));
            var arm7File = files.First(x => x.FilePath.ToRelative() == Path.Combine("sys", "arm7.bin"));
            var iconFile = files.FirstOrDefault(x => x.FilePath.ToRelative() == Path.Combine("sys", "banner.bin"));

            var arm9Overlays = files.Where(x => x.FilePath.ToRelative().IsInDirectory(Path.Combine("sys", "ovl"), false) &&
                                                x.FilePath.GetName().StartsWith("overlay9"))
                .Cast<OverlayArchiveFile>().ToArray();
            var arm7Overlays = files.Where(x => x.FilePath.ToRelative().IsInDirectory(Path.Combine("sys", "ovl"), false) &&
                                                x.FilePath.GetName().StartsWith("overlay7"))
                .Cast<OverlayArchiveFile>().ToArray();

            var arm9OverlayEntries = new List<OverlayEntry>();
            var arm7OverlayEntries = new List<OverlayEntry>();
            var fatEntries = new List<FatEntry>();

            using var bw = new BinaryWriterX(output, true);

            // Write ARM9
            var arm9Offset = output.Position = 0x4000;
            var arm9Size = arm9File.WriteFileData(output);
            if (_arm9Footer != null)
                WriteArm9Footer(_arm9Footer, bw);
            bw.WriteAlignment(0x200, 0xFF);

            // Write ARM9 Overlays
            var arm9OverlayOffset = output.Position;
            var arm9OverlaySize = arm9Overlays.Length * OverlayEntrySize_;
            var arm9OverlayPosition = (arm9OverlayOffset + arm9OverlaySize + 0x1FF) & ~0x1FF;
            foreach (var arm9Overlay in arm9Overlays.OrderBy(x => x.Entry.id))
            {
                output.Position = arm9OverlayPosition;
                var writtenSize = arm9Overlay.WriteFileData(output, true);
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
            WriteOverlayEntries(arm9OverlayEntries, bw);
            bw.WriteAlignment(0x200, 0xFF);
            output.Position = arm9OverlayPosition;

            // Write ARM7
            var arm7Offset = output.Position = arm9OverlayPosition;
            var arm7Size = arm7File.WriteFileData(output);

            // Write ARM7 Overlays
            var arm7OverlayOffset = output.Position = arm7Offset + arm7Size;
            var arm7OverlaySize = arm7Overlays.Length * OverlayEntrySize_;
            var arm7OverlayPosition = (arm7OverlayOffset + arm7OverlaySize + 0x1FF) & ~0x1FF;
            foreach (var arm7Overlay in arm7Overlays)
            {
                output.Position = arm7OverlayPosition;
                var writtenSize = arm7Overlay.WriteFileData(output, true);
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
            WriteOverlayEntries(arm7OverlayEntries, bw);
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
            var fatSize = (files.Count - 3) * FatEntrySize_;     // Not counting arm9.bin, arm7.bin, banner.bin

            // Write icon
            var iconOffset = (fatOffset + fatSize + 0x1FF) & ~0x1FF;
            var iconSize = 0;
            if (iconFile != null)
            {
                output.Position = iconOffset;
                iconSize = (int)iconFile.WriteFileData(output);

                bw.WriteAlignment(0x200, 0xFF);
            }

            // Write rom files
            var filePosition = (iconOffset + iconSize + 0x1FF) & ~0x1FF;
            foreach (var romFile in romFiles.Cast<FileIdArchiveFile>().OrderBy(x => x.FileId))
            {
                output.Position = filePosition;

                var romFileSize = romFile.WriteFileData(output, true);

                fatEntries.Add(new FatEntry
                {
                    offset = (int)filePosition,
                    endOffset = (int)(filePosition + romFileSize)
                });

                filePosition += (romFileSize + 0x1FF) & ~0x1FF;
            }

            // Write FAT
            bw.BaseStream.Position = fatOffset;
            WriteFatEntries(fatEntries, bw);
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

                WriteNdsHeader(_ndsHeader, bw);
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

                WriteDsiHeader(_dsiHeader, bw);
            }
        }

        private IArchiveFile ReadIcon(BinaryReaderX br, int iconOffset)
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

            return new ArchiveFile(new ArchiveFileInfo
            {
                FilePath = "sys/banner.bin",
                FileData = new SubStream(br.BaseStream, iconOffset, iconSize)
            });
        }

        #region Reading

        private NdsHeader ReadNdsHeader(BinaryReaderX reader)
        {
            return new NdsHeader
            {
                gameTitle = reader.ReadString(0xC),
                gameCode = reader.ReadString(4),
                makerCode = reader.ReadString(2),
                unitCode = (UnitCode)reader.ReadByte(),
                encryptionSeed = reader.ReadByte(),
                deviceCapacity = reader.ReadByte(),
                reserved1 = reader.ReadBytes(7),
                reserved2 = reader.ReadByte(),
                consoleRegion = reader.ReadByte(),
                romVer = reader.ReadByte(),
                internalFlag = reader.ReadByte(),
                arm9Offset = reader.ReadInt32(),
                arm9EntryAddress = reader.ReadInt32(),
                arm9LoadAddress = reader.ReadInt32(),
                arm9Size = reader.ReadInt32(),
                arm7Offset = reader.ReadInt32(),
                arm7EntryAddress = reader.ReadInt32(),
                arm7LoadAddress = reader.ReadInt32(),
                arm7Size = reader.ReadInt32(),
                fntOffset = reader.ReadInt32(),
                fntSize = reader.ReadInt32(),
                fatOffset = reader.ReadInt32(),
                fatSize = reader.ReadInt32(),
                arm9OverlayOffset = reader.ReadInt32(),
                arm9OverlaySize = reader.ReadInt32(),
                arm7OverlayOffset = reader.ReadInt32(),
                arm7OverlaySize = reader.ReadInt32(),
                normalRegisterSettings = reader.ReadInt32(),
                secureRegisterSettings = reader.ReadInt32(),
                iconOffset = reader.ReadInt32(),
                secureAreaCrc = reader.ReadInt16(),
                secureTransferTimeout = reader.ReadInt16(),
                arm9AutoLoad = reader.ReadInt32(),
                arm7AutoLoad = reader.ReadInt32(),
                secureDisable = reader.ReadInt64(),
                ntrRegionSize = reader.ReadInt32(),
                headerSize = reader.ReadInt32(),
                reserved3 = reader.ReadBytes(0x38),
                nintendoLogo = reader.ReadBytes(0x9C),
                nintendoLogoCrc = reader.ReadInt16(),
                headerCrc = reader.ReadInt16(),
                dbgRomOffset = reader.ReadInt32(),
                dbgSize = reader.ReadInt32(),
                dbgLoadAddress = reader.ReadInt32(),
                reserved4 = reader.ReadInt32(),
                reservedDbg = reader.ReadBytes(0x90)
            };
        }

        private DsiHeader ReadDsiHeader(BinaryReaderX reader)
        {
            return new DsiHeader
            {
                gameTitle = reader.ReadString(0xC),
                gameCode = reader.ReadString(4),
                makerCode = reader.ReadString(2),
                unitCode = (UnitCode)reader.ReadByte(),
                encryptionSeed = reader.ReadByte(),
                deviceCapacity = reader.ReadByte(),
                reserved1 = reader.ReadBytes(7),
                systemFlags = reader.ReadByte(),
                permitJump = reader.ReadByte(),
                romVer = reader.ReadByte(),
                internalFlag = reader.ReadByte(),
                arm9Offset = reader.ReadInt32(),
                arm9EntryAddress = reader.ReadInt32(),
                arm9LoadAddress = reader.ReadInt32(),
                arm9Size = reader.ReadInt32(),
                arm7Offset = reader.ReadInt32(),
                arm7EntryAddress = reader.ReadInt32(),
                arm7LoadAddress = reader.ReadInt32(),
                arm7Size = reader.ReadInt32(),
                fntOffset = reader.ReadInt32(),
                fntSize = reader.ReadInt32(),
                fatOffset = reader.ReadInt32(),
                fatSize = reader.ReadInt32(),
                arm9OverlayOffset = reader.ReadInt32(),
                arm9OverlaySize = reader.ReadInt32(),
                arm7OverlayOffset = reader.ReadInt32(),
                arm7OverlaySize = reader.ReadInt32(),
                normalRegisterSettings = reader.ReadInt32(),
                secureRegisterSettings = reader.ReadInt32(),
                iconOffset = reader.ReadInt32(),
                secureAreaCrc = reader.ReadInt16(),
                secureTransferTimeout = reader.ReadInt16(),
                arm9AutoLoad = reader.ReadInt32(),
                arm7AutoLoad = reader.ReadInt32(),
                secureDisable = reader.ReadInt64(),
                ntrRegionSize = reader.ReadInt32(),
                headerSize = reader.ReadInt32(),
                arm9ParametersOffset = reader.ReadInt32(),
                arm7ParametersOffset = reader.ReadInt32(),
                ntrRegionEnd = reader.ReadInt16(),
                twlRegionStart = reader.ReadInt16(),
                reserved3 = reader.ReadBytes(0x2C),
                nintendoLogo = reader.ReadBytes(0x9C),
                nintendoLogoCrc = reader.ReadInt16(),
                headerCrc = reader.ReadInt16(),
                dbgRomOffset = reader.ReadInt32(),
                dbgSize = reader.ReadInt32(),
                dbgLoadAddress = reader.ReadInt32(),
                reserved4 = reader.ReadInt32(),
                reservedDbg = reader.ReadBytes(0x90),
                extendedEntries = ReadExtendedEntries(reader)
            };
        }

        private DsiExtendedEntries ReadExtendedEntries(BinaryReaderX reader)
        {
            return new DsiExtendedEntries
            {
                mbkSettings = reader.ReadBytes(0x14),
                arm9MbkSettings = reader.ReadBytes(0xC),
                arm7MbkSettings = reader.ReadBytes(0xC),
                mbk9Setting = reader.ReadBytes(0x3),
                wramNctSettings = reader.ReadByte(),
                regionFlags = reader.ReadInt32(),
                accessControl = reader.ReadInt32(),
                arm7ScfgSetting = reader.ReadInt32(),
                reserved1 = reader.ReadBytes(0x3),
                flags = reader.ReadByte(),
                arm9iOffset = reader.ReadInt32(),
                reserved2 = reader.ReadInt32(),
                arm9iLoadAddress = reader.ReadInt32(),
                arm9iSize = reader.ReadInt32(),
                arm7iOffset = reader.ReadInt32(),
                reserved3 = reader.ReadInt32(),
                arm7iLoadAddress = reader.ReadInt32(),
                arm7iSize = reader.ReadInt32(),
                digestNtrOffset = reader.ReadInt32(),
                digestNtrSize = reader.ReadInt32(),
                digestTwlOffset = reader.ReadInt32(),
                digestTwlSize = reader.ReadInt32(),
                digestSectorHashtableOffset = reader.ReadInt32(),
                digestSectorHashtableSize = reader.ReadInt32(),
                digestBlockHashtableOffset = reader.ReadInt32(),
                digestBlockHashtableSize = reader.ReadInt32(),
                digestSectorSize = reader.ReadInt32(),
                digestBlockSectorCount = reader.ReadInt32(),
                iconSize = reader.ReadInt32(),
                sdmmcSize1 = reader.ReadByte(),
                sdmmcSize2 = reader.ReadByte(),
                eulaVersion = reader.ReadByte(),
                useRatings = reader.ReadBoolean(),
                totalRomSize = reader.ReadInt32(),
                sdmmcSize3 = reader.ReadByte(),
                sdmmcSize4 = reader.ReadByte(),
                sdmmcSize5 = reader.ReadByte(),
                sdmmcSize6 = reader.ReadByte(),
                arm9iParametersOffset = reader.ReadInt32(),
                arm7iParametersOffset = reader.ReadInt32(),
                modCryptArea1Offset = reader.ReadInt32(),
                modCryptArea1Size = reader.ReadInt32(),
                modCryptArea2Offset = reader.ReadInt32(),
                modCryptArea2Size = reader.ReadInt32(),
                gameCode = reader.ReadInt32(),
                fileType = reader.ReadByte(),
                titleIdZero0 = reader.ReadByte(),
                titleIdZeroThree = reader.ReadByte(),
                titleIdZero1 = reader.ReadByte(),
                sdmmcPublicSaveSize = reader.ReadInt32(),
                sdmmcPrivateSaveSize = reader.ReadInt32(),
                reserved4 = reader.ReadBytes(0xB0),
                parentalControl = ReadParentalControl(reader),
                sha1Section = ReadSha1Section(reader)
            };
        }

        private DsiParentalControl ReadParentalControl(BinaryReaderX reader)
        {
            return new DsiParentalControl
            {
                ageRatings = reader.ReadBytes(0x10),
                cero = reader.ReadByte(),
                esrb = reader.ReadByte(),
                reserved1 = reader.ReadByte(),
                usk = reader.ReadByte(),
                pegiEur = reader.ReadByte(),
                reserved2 = reader.ReadByte(),
                pegiPrt = reader.ReadByte(),
                bbfc = reader.ReadByte(),
                agcb = reader.ReadByte(),
                grb = reader.ReadByte(),
                reserved3 = reader.ReadBytes(0x6)
            };
        }

        private Sha1Section ReadSha1Section(BinaryReaderX reader)
        {
            return new Sha1Section
            {
                arm9HmacHash = reader.ReadBytes(0x14),
                arm7HmacHash = reader.ReadBytes(0x14),
                digestMasterHmacHash = reader.ReadBytes(0x14),
                iconHmacHash = reader.ReadBytes(0x14),
                arm9iHmacHash = reader.ReadBytes(0x14),
                arm7iHmacHash = reader.ReadBytes(0x14),
                reserved1 = reader.ReadBytes(0x14),
                reserved2 = reader.ReadBytes(0x14),
                arm9HmacHashWithoutSecureArea = reader.ReadBytes(0x14),
                reserved3 = reader.ReadBytes(0xA4C),
                dbgVariableStorage = reader.ReadBytes(0x180),
                headerSectionRsa = reader.ReadBytes(0x80)
            };
        }

        private Arm9Footer ReadArm9Footer(BinaryReaderX reader)
        {
            return new Arm9Footer
            {
                nitroCode = reader.ReadUInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32()
            };
        }

        private OverlayEntry[] ReadOverlayEntries(BinaryReaderX reader, int count)
        {
            var result = new OverlayEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadOverlayEntry(reader);

            return result;
        }

        private OverlayEntry ReadOverlayEntry(BinaryReaderX reader)
        {
            return new OverlayEntry
            {
                id = reader.ReadInt32(),
                ramAddress = reader.ReadInt32(),
                ramSize = reader.ReadInt32(),
                bssSize = reader.ReadInt32(),
                staticInitStartAddress = reader.ReadInt32(),
                staticInitEndAddress = reader.ReadInt32(),
                fileId = reader.ReadInt32(),
                reserved1 = reader.ReadInt32(),
            };
        }

        private FatEntry[] ReadFatEntries(BinaryReaderX reader, int count)
        {
            var result = new FatEntry[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadFatEntry(reader);

            return result;
        }

        private FatEntry ReadFatEntry(BinaryReaderX reader)
        {
            return new FatEntry
            {
                offset = reader.ReadInt32(),
                endOffset = reader.ReadInt32()
            };
        }

        #endregion

        #region Writing

        private void WriteNdsHeader(NdsHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.gameTitle, writeNullTerminator: false);
            writer.WriteString(header.gameCode, writeNullTerminator: false);
            writer.WriteString(header.makerCode, writeNullTerminator: false);
            writer.Write((byte)header.unitCode);
            writer.Write(header.encryptionSeed);
            writer.Write(header.deviceCapacity);
            writer.Write(header.reserved1);
            writer.Write(header.reserved2);
            writer.Write(header.consoleRegion);
            writer.Write(header.romVer);
            writer.Write(header.internalFlag);
            writer.Write(header.arm9Offset);
            writer.Write(header.arm9EntryAddress);
            writer.Write(header.arm9LoadAddress);
            writer.Write(header.arm9Size);
            writer.Write(header.arm7Offset);
            writer.Write(header.arm7EntryAddress);
            writer.Write(header.arm7LoadAddress);
            writer.Write(header.arm7Size);
            writer.Write(header.fntOffset);
            writer.Write(header.fntSize);
            writer.Write(header.fatOffset);
            writer.Write(header.fatSize);
            writer.Write(header.arm9OverlayOffset);
            writer.Write(header.arm9OverlaySize);
            writer.Write(header.arm7OverlayOffset);
            writer.Write(header.arm7OverlaySize);
            writer.Write(header.normalRegisterSettings);
            writer.Write(header.secureRegisterSettings);
            writer.Write(header.iconOffset);
            writer.Write(header.secureAreaCrc);
            writer.Write(header.secureTransferTimeout);
            writer.Write(header.arm9AutoLoad);
            writer.Write(header.arm7AutoLoad);
            writer.Write(header.secureDisable);
            writer.Write(header.ntrRegionSize);
            writer.Write(header.headerSize);
            writer.Write(header.reserved3);
            writer.Write(header.nintendoLogo);
            writer.Write(header.nintendoLogoCrc);
            writer.Write(header.headerCrc);
            writer.Write(header.dbgRomOffset);
            writer.Write(header.dbgSize);
            writer.Write(header.dbgLoadAddress);
            writer.Write(header.reserved4);
            writer.Write(header.reservedDbg);
        }

        private void WriteDsiHeader(DsiHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.gameTitle, writeNullTerminator: false);
            writer.WriteString(header.gameCode, writeNullTerminator: false);
            writer.WriteString(header.makerCode, writeNullTerminator: false);
            writer.Write((byte)header.unitCode);
            writer.Write(header.encryptionSeed);
            writer.Write(header.deviceCapacity);
            writer.Write(header.reserved1);
            writer.Write(header.systemFlags);
            writer.Write(header.permitJump);
            writer.Write(header.romVer);
            writer.Write(header.internalFlag);
            writer.Write(header.arm9Offset);
            writer.Write(header.arm9EntryAddress);
            writer.Write(header.arm9LoadAddress);
            writer.Write(header.arm9Size);
            writer.Write(header.arm7Offset);
            writer.Write(header.arm7EntryAddress);
            writer.Write(header.arm7LoadAddress);
            writer.Write(header.arm7Size);
            writer.Write(header.fntOffset);
            writer.Write(header.fntSize);
            writer.Write(header.fatOffset);
            writer.Write(header.fatSize);
            writer.Write(header.arm9OverlayOffset);
            writer.Write(header.arm9OverlaySize);
            writer.Write(header.arm7OverlayOffset);
            writer.Write(header.arm7OverlaySize);
            writer.Write(header.normalRegisterSettings);
            writer.Write(header.secureRegisterSettings);
            writer.Write(header.iconOffset);
            writer.Write(header.secureAreaCrc);
            writer.Write(header.secureTransferTimeout);
            writer.Write(header.arm9AutoLoad);
            writer.Write(header.arm7AutoLoad);
            writer.Write(header.secureDisable);
            writer.Write(header.ntrRegionSize);
            writer.Write(header.headerSize);
            writer.Write(header.arm9ParametersOffset);
            writer.Write(header.arm7ParametersOffset);
            writer.Write(header.ntrRegionEnd);
            writer.Write(header.twlRegionStart);
            writer.Write(header.reserved3);
            writer.Write(header.nintendoLogo);
            writer.Write(header.nintendoLogoCrc);
            writer.Write(header.headerCrc);
            writer.Write(header.dbgRomOffset);
            writer.Write(header.dbgSize);
            writer.Write(header.dbgLoadAddress);
            writer.Write(header.reserved4);
            writer.Write(header.reservedDbg);

            WriteExtendedEntries(header.extendedEntries, writer);
        }

        private void WriteExtendedEntries(DsiExtendedEntries entries, BinaryWriterX writer)
        {
            writer.Write(entries.mbkSettings);
            writer.Write(entries.arm9MbkSettings);
            writer.Write(entries.arm7MbkSettings);
            writer.Write(entries.mbk9Setting);
            writer.Write(entries.wramNctSettings);
            writer.Write(entries.regionFlags);
            writer.Write(entries.accessControl);
            writer.Write(entries.arm7ScfgSetting);
            writer.Write(entries.reserved1);
            writer.Write(entries.flags);
            writer.Write(entries.arm9iOffset);
            writer.Write(entries.reserved2);
            writer.Write(entries.arm9iLoadAddress);
            writer.Write(entries.arm9iSize);
            writer.Write(entries.arm7iOffset);
            writer.Write(entries.reserved3);
            writer.Write(entries.arm7iLoadAddress);
            writer.Write(entries.arm7iSize);
            writer.Write(entries.digestNtrOffset);
            writer.Write(entries.digestNtrSize);
            writer.Write(entries.digestTwlOffset);
            writer.Write(entries.digestTwlSize);
            writer.Write(entries.digestSectorHashtableOffset);
            writer.Write(entries.digestSectorHashtableSize);
            writer.Write(entries.digestBlockHashtableOffset);
            writer.Write(entries.digestBlockHashtableSize);
            writer.Write(entries.digestSectorSize);
            writer.Write(entries.digestBlockSectorCount);
            writer.Write(entries.iconSize);
            writer.Write(entries.sdmmcSize1);
            writer.Write(entries.sdmmcSize2);
            writer.Write(entries.eulaVersion);
            writer.Write(entries.useRatings);
            writer.Write(entries.totalRomSize);
            writer.Write(entries.sdmmcSize3);
            writer.Write(entries.sdmmcSize4);
            writer.Write(entries.sdmmcSize5);
            writer.Write(entries.sdmmcSize6);
            writer.Write(entries.arm9iParametersOffset);
            writer.Write(entries.arm7iParametersOffset);
            writer.Write(entries.modCryptArea1Offset);
            writer.Write(entries.modCryptArea1Size);
            writer.Write(entries.modCryptArea2Offset);
            writer.Write(entries.modCryptArea2Size);
            writer.Write(entries.gameCode);
            writer.Write(entries.fileType);
            writer.Write(entries.titleIdZero0);
            writer.Write(entries.titleIdZeroThree);
            writer.Write(entries.titleIdZero1);
            writer.Write(entries.sdmmcPublicSaveSize);
            writer.Write(entries.sdmmcPrivateSaveSize);
            writer.Write(entries.reserved4);

            WriteParentalControl(entries.parentalControl, writer);
            WriteSha1Section(entries.sha1Section, writer);
        }

        private void WriteParentalControl(DsiParentalControl parentalControl, BinaryWriterX writer)
        {
            writer.Write(parentalControl.ageRatings);
            writer.Write(parentalControl.cero);
            writer.Write(parentalControl.esrb);
            writer.Write(parentalControl.reserved1);
            writer.Write(parentalControl.usk);
            writer.Write(parentalControl.pegiEur);
            writer.Write(parentalControl.reserved2);
            writer.Write(parentalControl.pegiPrt);
            writer.Write(parentalControl.bbfc);
            writer.Write(parentalControl.agcb);
            writer.Write(parentalControl.grb);
            writer.Write(parentalControl.reserved3);
        }

        private void WriteSha1Section(Sha1Section sha1, BinaryWriterX writer)
        {
            writer.Write(sha1.arm9HmacHash);
            writer.Write(sha1.arm7HmacHash);
            writer.Write(sha1.digestMasterHmacHash);
            writer.Write(sha1.iconHmacHash);
            writer.Write(sha1.arm9iHmacHash);
            writer.Write(sha1.arm7iHmacHash);
            writer.Write(sha1.reserved1);
            writer.Write(sha1.reserved2);
            writer.Write(sha1.arm9HmacHashWithoutSecureArea);
            writer.Write(sha1.reserved3);
            writer.Write(sha1.dbgVariableStorage);
            writer.Write(sha1.headerSectionRsa);
        }

        private void WriteArm9Footer(Arm9Footer footer, BinaryWriterX writer)
        {
            writer.Write(footer.nitroCode);
            writer.Write(footer.unk1);
            writer.Write(footer.unk2);
        }

        private void WriteOverlayEntries(IList<OverlayEntry> entries, BinaryWriterX writer)
        {
            foreach (OverlayEntry entry in entries)
                WriteOverlayEntry(entry, writer);
        }

        private void WriteOverlayEntry(OverlayEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.id);
            writer.Write(entry.ramAddress);
            writer.Write(entry.ramSize);
            writer.Write(entry.bssSize);
            writer.Write(entry.staticInitStartAddress);
            writer.Write(entry.staticInitEndAddress);
            writer.Write(entry.fileId);
            writer.Write(entry.reserved1);
        }

        private void WriteFatEntries(IList<FatEntry> entries, BinaryWriterX writer)
        {
            foreach (FatEntry entry in entries)
                WriteFatEntry(entry, writer);
        }

        private void WriteFatEntry(FatEntry entry, BinaryWriterX writer)
        {
            writer.Write(entry.offset);
            writer.Write(entry.endOffset);
        }

        #endregion
    }
}
