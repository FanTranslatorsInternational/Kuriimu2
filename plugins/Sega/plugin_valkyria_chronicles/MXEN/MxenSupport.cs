using System.Collections.Generic;

namespace plugin_valkyria_chronicles.MXEN
{
    public sealed class MXECHeader
    {
        public int Unk1;
        public int Unk2;
        public int Table4Offset;
        public int Table2Offset;
        public int Unk3;
        public int Unk4;
        public int Unk5;
        public int Table6Offset;
        public int Unk6;
        public int Unk7;
        public int Unk8;
        public int Unk9;
        public int Unk10;
        public int Unk11;
        public int Unk12;
        public int Unk13;
        public int Unk14;
        public int Table1Count;
        public int Unk15;
        public int Padding1;
        public int Padding2;
        public int Padding3;
        public int Padding4;
        public int Padding5;
        public int Padding6;
        public int Padding7;
        public int Padding8;
        public int Padding9;
        public int Padding10;
        public int Padding11;
        public int Padding12;
        public int Padding13;
    }

    #region Table1

    public sealed class Table1Metadata
    {
        public int ID;
        public int TypeOffset;
        public int DataSize;
        public int DataOffset;
    }

    public sealed class Table1Object
    {
        public Table1Metadata Metadata;
        public string Type;
        public int TypeIndex;
        public object Data;

        public List<Table1ObjectText> Texts = new List<Table1ObjectText>();
    }

    public sealed class Table1ObjectText
    {
        public int DataOffset;
        public int TextIndex;
    }
    
    public sealed class Table1TextEntry
    {
        public int Offset;
        public string Text;
    }

    #endregion
}
