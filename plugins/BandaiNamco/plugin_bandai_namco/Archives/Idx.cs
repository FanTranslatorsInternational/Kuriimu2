﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Komponent.Extensions;
using Komponent.IO;
using Kontract.Extensions;
using Kontract.Models.Archive;
using Kontract.Models.IO;

namespace plugin_bandai_namco.Archives
{
    class Idx
    {
        private IList<ApkPackHeader> _apkHeaders;
        private IList<ApkTocEntry> _entries;
        private IList<string> _strings;

        public UPath[] ApkPaths => _apkHeaders.Select(x => (UPath)_strings[x.stringIndex]).ToArray();

        public IList<IArchiveFileInfo> Load(Stream input, IList<Stream> apkStreams)
        {
            using var br = new BinaryReaderX(input, true);

            // Validate APK streams
            var tempApkStreams=new List<Stream>();
            foreach (var apkStream in apkStreams)
            {
                using var apkBr=new BinaryReaderX(apkStream,true);
                if (apkBr.ReadString(8) == ApkSection.StartSection)
                {
                    apkStream.Position = 0;
                    tempApkStreams.Add(apkStream);
                }
            }

            apkStreams = tempApkStreams;

            // Read sections
            var sections = ApkSection.ReadAll(input);

            // Read pack headers
            _apkHeaders = sections
                .Where(x => x.Type == ApkSection.PackHeader)
                .Select(x => x.As<ApkPackHeader>())
                .ToArray();

            // Order streams to pack headers
            var combinedHeaderStreams = new List<(ApkPackHeader, Stream)>();
            foreach (var apkStream in apkStreams)
            {
                var apkHeader = ApkSection.ReadAll(apkStream).FirstOrDefault(x => x.Type == ApkSection.PackHeader).As<ApkPackHeader>();
                combinedHeaderStreams.Add((apkHeader, apkStream));
            }

            apkStreams = new List<Stream>();
            foreach (var apkHeader in _apkHeaders)
                apkStreams.Add(combinedHeaderStreams.FirstOrDefault(x => x.Item1.headerIdent.SequenceEqual(apkHeader.headerIdent)).Item2);

            // Read entries
            _entries = sections.FirstOrDefault(x => x.Type == ApkSection.PackToc).As<ApkToc>().entries;

            // Read strings
            using var stringBr = new BinaryReaderX(sections.FirstOrDefault(x => x.Type == ApkSection.StringTable).Data, true);

            var stringHeader = stringBr.ReadType<ApkStringHeader>();

            stringBr.BaseStream.Position = stringHeader.tableOffset;
            var stringOffsets = stringBr.ReadMultiple<int>(stringHeader.stringCount);

            _strings = new List<string>();
            foreach (var stringOffset in stringOffsets)
            {
                stringBr.BaseStream.Position = stringHeader.dataOffset + stringOffset;
                _strings.Add(stringBr.ReadCStringASCII());
            }

            return ApkSupport.EnumerateFiles(apkStreams, _entries[0], UPath.Root, _apkHeaders, _strings, _entries).ToArray();
        }

        public void Save(Stream output, IList<(UPath, Stream)> apkStreams, IList<IArchiveFileInfo> files)
        {
            // Save APKs
            var changedFiles = files.Where(x => x.ContentChanged).ToArray();
            foreach (var apkHeader in _apkHeaders.Where(x => changedFiles.Any(y => y.FilePath.ToRelative().IsInDirectory(_strings[x.stringIndex], true))))
            {
                var apkStream = apkStreams.FirstOrDefault(x => x.Item1 == _strings[apkHeader.stringIndex]).Item2;
                ApkSupport.Save(apkStream, files.Where(x => x.FilePath.ToRelative().IsInDirectory(_strings[apkHeader.stringIndex], true)).ToArray(), _strings[apkHeader.stringIndex], apkHeader.headerIdent);

                using var br = new BinaryReaderX(apkStream, true);
                apkStream.Position = 0x28;
                apkHeader.dataOffset = br.ReadInt32();
            }

            // Save pack.idx
            SaveInternal(output, files);
        }

        private void SaveInternal(Stream output, IList<IArchiveFileInfo> files)
        {
            using var bw = new BinaryWriterX(output, true);

            // Prepare file tree
            var fileTree = files.ToTree();
            fileTree = fileTree.Directories[0].Directories[0];

            var root = new DirectoryEntry("");
            foreach (var dir in fileTree.Directories.SelectMany(x => x.Directories))
                root.AddDirectory(dir);
            foreach (var file in fileTree.Directories.SelectMany(x => x.Files))
                root.Files.Add(file);

            // Calculate offsets
            var packHeaderOffset = 0x10;
            var entryOffset = packHeaderOffset + _apkHeaders.Count * 0x30;
            var packFslsOffset = (entryOffset + 0x20 + ApkSupport.CountEntries(root) * 0x28 + 0xF) & ~0xF;
            var stringOffset = packFslsOffset + 0x20;

            // Distinct strings
            var strings = new List<string>();
            foreach (var apkHeader in _apkHeaders)
                strings.Add(_strings[apkHeader.stringIndex]);
            strings.Add("");

            strings.AddRange(CollectStrings(root).Distinct());

            // Write strings
            output.Position = stringOffset;
            ApkSupport.WriteStringTable(output, strings);

            var endSectionOffset = output.Length;
            var dataOffset = (endSectionOffset + 0x10 + 0x7FF) & ~0x7FF;

            // Write end section
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.EndSection });

            // Write pack file section
            output.Position = packFslsOffset;
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.PackFiles, sectionSize = 0x10 });
            bw.WriteType(new ApkPackFilesHeader());

            // Write entries
            output.Position = entryOffset;
            ApkSupport.WriteEntrySection(output, root, strings, _apkHeaders.Select(x => (long)x.dataOffset).ToArray(), false);

            // Write start section
            output.Position = 0;
            bw.WriteType(new ApkSectionHeader { magic = ApkSection.StartSection });

            // Write pack headers
            foreach (var apkHeader in _apkHeaders)
            {
                bw.WriteType(new ApkSectionHeader { magic = ApkSection.PackHeader, sectionSize = 0x20 });
                bw.WriteType(new ApkPackHeader { dataOffset = apkHeader.dataOffset, stringIndex = apkHeader.stringIndex, headerIdent = apkHeader.headerIdent });
            }
        }

        IEnumerable<string> CollectStrings(DirectoryEntry entry)
        {
            foreach (var dir in entry.Directories)
            {
                yield return dir.Name;
                foreach (var name in CollectStrings(dir))
                    yield return name;
            }

            foreach (var file in entry.Files)
                yield return file.FilePath.GetName();
        }
    }
}
