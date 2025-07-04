using Komponent.IO;
using Komponent.Streams;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.Management.Files;
using Konnect.Contract.Plugin.File.Image;
using System.Text;
using Konnect.Contract.Enums.Management.Files;
using Konnect.Extensions;

namespace plugin_mercury_steam.Images
{
    class Mtxt(IPluginFileManager fileManager)
    {
        private IFileState _ctpkState;
        private MtxtHeader _header;

        public async Task<IReadOnlyList<IImageFile>> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            // Read header
            _header = ReadHeader(br);

            // Read name
            input.Position = _header.offset + _header.imgSize;
            var name = br.ReadNullTerminatedString();

            // Load CTPK
            var ctpkStream = new SubStream(input, _header.offset, _header.imgSize);
            var loadResult = await fileManager.LoadFile(new StreamFile
            {
                Path = name + ".ctpk",
                Stream = ctpkStream
            });
            if (loadResult.Status is not LoadStatus.Successful || loadResult.LoadedFileState?.PluginState is not IImageFilePluginState imageState)
                throw new InvalidOperationException(loadResult.Reason.ToString());

            // Read CTPK
            _ctpkState = loadResult.LoadedFileState;

            return imageState.Images;
        }

        public async Task Save(Stream output)
        {
            using var bw = new BinaryWriterX(output);

            // Save CTPK
            var saveResult = await fileManager.SaveStream(_ctpkState);
            if (!saveResult.IsSuccessful)
                throw new InvalidOperationException(saveResult.Reason.ToString());

            // Update header
            var imageState = (IImageFilePluginState)_ctpkState.PluginState;

            _header.width = imageState.Images[0].ImageInfo.ImageSize.Width;
            _header.height = imageState.Images[0].ImageInfo.ImageSize.Height;

            _header.imgSize = (int)saveResult.SavedStreams[0].Stream.Length;
            _header.nameOffset = _header.offset + _header.imgSize;

            // Write header
            WriteHeader(_header, bw);

            // Write CTPK
            output.Position = _header.offset;
            await saveResult.SavedStreams[0].Stream.CopyToAsync(output);

            // Write name
            bw.WriteString(saveResult.SavedStreams[0].Path.GetNameWithoutExtension(), Encoding.ASCII);
        }

        private MtxtHeader ReadHeader(BinaryReaderX reader)
        {
            return new MtxtHeader
            {
                magic = reader.ReadString(4),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                width = reader.ReadInt32(),
                height = reader.ReadInt32(),
                format = reader.ReadInt32(),
                nameOffset = reader.ReadInt32(),
                offset = reader.ReadInt32(),
                imgSize = reader.ReadInt32()
            };
        }

        private void WriteHeader(MtxtHeader header, BinaryWriterX writer)
        {
            writer.WriteString(header.magic, writeNullTerminator: false);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.width);
            writer.Write(header.height);
            writer.Write(header.format);
            writer.Write(header.nameOffset);
            writer.Write(header.offset);
            writer.Write(header.imgSize);
        }
    }
}
