namespace plugin_nintendo.Font.DataClasses
{
    struct CfntTglpSection
    {
        public byte cellWidth;
        public byte cellHeight;
        public byte baseline;
        public byte maxCharWidth;
        public int sheetSize;
        public short sheetCount;
        public short sheetFormat;
        public short columnCount;
        public short rowCount;
        public short sheetWidth;
        public short sheetHeight;
        public int sheetDataOffset;
        public byte[][] sheetData;
    }
}
