namespace plugin_nintendo.Font.DataClasses
{
    struct CfntCmapSection
    {
        public ushort codeBegin;
        public ushort codeEnd;
        public short mappingMethod;
        public short reserved;
        public int nextCmapOffset;
        public object indexData;
    }
}
