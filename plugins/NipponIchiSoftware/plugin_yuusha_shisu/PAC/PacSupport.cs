//using Komponent.IO.Attributes;

//namespace plugin_yuusha_shisu.PAC
//{
//    /// <summary>
//    /// Represents the header of a PAC archive.
//    /// </summary>
//    public class FileHeader
//    {
//        [FixedLength(4)]
//        public string Magic;
//        public int Unk1;
//        public int FileCount;
//        public int Null1;
//        [FixedLength(0x20)]
//        public string ArchiveName;
//    }

//    /// <summary>
//    /// Represents an individual file entry in a PAC archive.
//    /// </summary>
//    [Alignment(0x20)]
//    public class FileEntry
//    {
//        [FixedLength(4)]
//        public string Extension;
//        public short Unk1;
//        public short FileNumbers;
//        public int Checksum;
//        public short Unk2;
//        public short StringLength;
//        public int Null2;
//        [VariableLength("StringLength")]
//        public string FileName;
//    }
//}
