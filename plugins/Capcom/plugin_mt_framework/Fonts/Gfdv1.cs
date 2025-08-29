using Kaligraphy.Contract.DataClasses.Generation;
using Kaligraphy.Contract.DataClasses;
using Kaligraphy.Contract.DataClasses.Generation.Packing;
using Komponent.IO;
using Konnect.Contract.DataClasses.FileSystem;
using Konnect.Contract.DataClasses.Plugin.File.Font;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Contract.FileSystem;
using Konnect.Contract.Management.Dialog;
using Konnect.Contract.Plugin.File.Image;
using Konnect.Extensions;
using Konnect.Plugin.File.Image;
using plugin_mt_framework.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace plugin_mt_framework.Fonts
{
    class Gfdv1
    {
        private readonly MtTex _mtTex = new();

        private GfdHeaderv1 _header;
        private float[] _floats;
        private string _name;

        private IList<IImageFile> _imageFiles;
        private IList<string> _suffixes;

        public async Task<List<CharacterInfo>> Load(Stream input, IFileSystem fileSystem, UPath basePath, IDialogManager dialogManager)
        {
            using var br = new BinaryReaderX(input);

            _header = ReadHeader(br);

            _floats = ReadFloats(br, _header.FCount);

            // HINT: Explicit name length given, but name is null-terminated
            int nameLength = br.ReadInt32();
            _name = br.ReadString(nameLength);

            br.BaseStream.Position++;
            GfdEntryv1[] entries = ReadEntries(br, _header.CharCount);

            // Load images
            _imageFiles = new List<IImageFile>();
            _suffixes = new List<string>();

            foreach (int imageId in entries.Select(e => e.ImageId).Distinct().Order())
            {
                var fileStart = $"{Path.GetFileName(_name)}_{imageId:00}";

                UPath fontTexFilePath = fileSystem.EnumerateFiles(basePath, $"{fileStart}*.tex").FirstOrDefault();
                if (fontTexFilePath.IsNull || fontTexFilePath.IsEmpty)
                    throw new InvalidOperationException($"Could not find font texture file with name pattern '{fileStart}*.tex'");

                _suffixes.Add(fontTexFilePath.GetNameWithoutExtension()[fileStart.Length..]);

                await using Stream fontTexFileStream = await fileSystem.OpenFileAsync(fontTexFilePath);

                MtTexPlatform texPlatform = await MtTexSupport.DeterminePlatform(fontTexFileStream, dialogManager);
                ImageFileInfo? imageFileInfo = _mtTex.Load(fontTexFileStream, texPlatform).FirstOrDefault();
                if (imageFileInfo is null)
                    throw new InvalidOperationException($"Font texture file '{fontTexFilePath}' could not be loaded.");

                _imageFiles.Add(new ImageFile(imageFileInfo, texPlatform == MtTexPlatform.Mobile, MtTexSupport.GetEncodingDefinition(texPlatform)));
            }

            var result = new List<CharacterInfo>();

            foreach (GfdEntryv1 entry in entries)
            {
                Image<Rgba32>? glyph = null;
                if (entry is { GlyphWidth: > 0, GlyphHeight: > 0 })
                    glyph = _imageFiles[entry.ImageId].GetImage().Clone(context => context.Crop(new Rectangle(entry.GlyphPositionX, entry.GlyphPositionY, entry.GlyphWidth, entry.GlyphHeight)));

                result.Add(new CharacterInfo
                {
                    CodePoint = (char)entry.codePoint,
                    GlyphPosition = new Point(entry.posX, entry.posY),
                    BoundingBox = new Size(entry.charWidth, entry.GlyphHeight),
                    Glyph = glyph
                });
            }

            return result;
        }

        public async Task Save(IList<CharacterInfo> characterInfos, Stream output, IFileSystem fileSystem, UPath basePath)
        {
            // Create entries
            IList<GfdEntryv1> entries = CreateEntries(characterInfos, out IList<Image<Rgba32>> images);

            // Create tex files
            for (var i = 0; i < images.Count; i++)
            {
                IImageFile imageFile = i >= _imageFiles.Count ? _imageFiles[^1].Clone() : _imageFiles[i];
                imageFile.SetImage(images[i]);

                string suffix = i >= _suffixes.Count ? _suffixes[^1] : _suffixes[i];
                var fileName = $"{Path.GetFileName(_name)}_{i:00}{suffix}.tex";

                await using Stream imageStream = await fileSystem.OpenFileAsync(basePath / fileName, FileMode.Create, FileAccess.Write);
                _mtTex.Save(imageStream, [imageFile.ImageInfo]);
            }

            // Write font data
            await using var bw = new BinaryWriterX(output);

            _header.CharCount = characterInfos.Count;
            _header.FontTexCount = images.Count;

            WriteHeader(_header, bw);
            WriteFloats(_floats, bw);

            bw.Write(_name.Length);
            bw.WriteString(_name);

            WriteEntries(entries, bw);
        }

        private IList<GfdEntryv1> CreateEntries(IList<CharacterInfo> characterInfos, out IList<Image<Rgba32>> images)
        {
            images = new List<Image<Rgba32>>();

            // Create font textures
            var generator = new Kaligraphy.Generation.FontTextureGenerator(_imageFiles[0].ImageInfo.ImageSize, 1);

            GlyphData[] glyphData = characterInfos
                .Where(c => c.Glyph is not null)
                .Select(c => new GlyphData
                {
                    Character = c.CodePoint,
                    Glyph = c.Glyph!,
                    Description = new GlyphDescriptionData
                    {
                        Position = Point.Empty,
                        Size = c.Glyph!.Size
                    }
                })
                .ToArray();
            IList<PackedGlyphsData> glyphImages = generator.Generate(glyphData);

            // Create entries
            var entries = new List<GfdEntryv1>(characterInfos.Count);

            IDictionary<char,CharacterInfo> characterLookup = characterInfos.ToDictionary(c => c.CodePoint);
            for (var i = 0; i < glyphImages.Count; i++)
            {
                images.Add(glyphImages[i].Image);

                foreach (PackedGlyphData glyph in glyphImages[i].Glyphs)
                {
                    entries.Add(new GfdEntryv1
                    {
                        codePoint = glyph.Element.Character,
                        charWidth = (byte)characterLookup[glyph.Element.Character].BoundingBox.Width,
                        posX = (byte)characterLookup[glyph.Element.Character].GlyphPosition.X,
                        posY = (byte)characterLookup[glyph.Element.Character].GlyphPosition.Y
                    });

                    entries[^1].GlyphPositionX = (short)glyph.Position.X;
                    entries[^1].GlyphPositionY = (short)glyph.Position.Y;
                    entries[^1].GlyphWidth = (short)glyph.Element.Glyph.Width;
                    entries[^1].GlyphHeight = (short)glyph.Element.Glyph.Height;
                    entries[^1].ImageId = (byte)i;
                }
            }

            // Set glyphs without representation on image 0
            foreach (CharacterInfo character in characterInfos.Where(c => c.Glyph is null))
            {
                entries.Add(new GfdEntryv1
                {
                    codePoint = character.CodePoint,
                    charWidth = (byte)character.BoundingBox.Width,
                    posX = (byte)character.GlyphPosition.X,
                    posY = (byte)character.GlyphPosition.Y
                });

                entries[^1].GlyphPositionX = 0;
                entries[^1].GlyphPositionY = 0;
                entries[^1].GlyphWidth = 0;
                entries[^1].GlyphHeight = 0;
                entries[^1].ImageId = 0;
            }

            return entries.OrderBy(e => e.codePoint).ToArray();
        }

        private GfdHeaderv1 ReadHeader(BinaryReaderX reader)
        {
            return new GfdHeaderv1
            {
                Magic = reader.ReadString(4),
                Version = reader.ReadUInt32(),
                unk0 = reader.ReadInt32(),
                unk1 = reader.ReadInt32(),
                unk2 = reader.ReadInt32(),
                FontSize = reader.ReadInt32(),
                FontTexCount = reader.ReadInt32(),
                CharCount = reader.ReadInt32(),
                FCount = reader.ReadInt32(),
                BaseLine = reader.ReadSingle(),
                DescentLine = reader.ReadSingle()
            };
        }

        private GfdEntryv1[] ReadEntries(BinaryReaderX reader, int count)
        {
            var result = new GfdEntryv1[count];

            for (var i = 0; i < count; i++)
                result[i] = ReadEntry(reader);

            return result;
        }

        private GfdEntryv1 ReadEntry(BinaryReaderX reader)
        {
            return new GfdEntryv1
            {
                codePoint = reader.ReadUInt32(),
                tmp1 = reader.ReadUInt32(),
                tmp2 = reader.ReadUInt32(),
                charWidth = reader.ReadByte(),
                posX = reader.ReadByte(),
                posY = reader.ReadByte(),
                padding = reader.ReadByte()
            };
        }

        private float[] ReadFloats(BinaryReaderX reader, int count)
        {
            var result = new float[count];

            for (var i = 0; i < count; i++)
                result[i] = reader.ReadSingle();

            return result;
        }

        private void WriteHeader(GfdHeaderv1 header, BinaryWriterX writer)
        {
            writer.WriteString(header.Magic, writeNullTerminator: false);
            writer.Write(header.Version);
            writer.Write(header.unk0);
            writer.Write(header.unk1);
            writer.Write(header.unk2);
            writer.Write(header.FontSize);
            writer.Write(header.FontTexCount);
            writer.Write(header.CharCount);
            writer.Write(header.FCount);
            writer.Write(header.BaseLine);
            writer.Write(header.DescentLine);
        }

        private void WriteEntries(IList<GfdEntryv1> entries, BinaryWriterX writer)
        {
            foreach (GfdEntryv1 entry in entries)
                WriteEntry(entry, writer);
        }

        private void WriteEntry(GfdEntryv1 entry, BinaryWriterX writer)
        {
            writer.Write(entry.codePoint);
            writer.Write(entry.tmp1);
            writer.Write(entry.tmp2);
            writer.Write(entry.charWidth);
            writer.Write(entry.posX);
            writer.Write(entry.posY);
            writer.Write(entry.padding);
        }

        private void WriteFloats(float[] values, BinaryWriterX writer)
        {
            foreach (float value in values)
                writer.Write(value);
        }
    }
}
