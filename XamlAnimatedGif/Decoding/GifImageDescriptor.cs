namespace XamlAnimatedGif.Decoding
{
    internal class GifImageDescriptor : IGifRect
    {
        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool HasLocalColorTable { get; private set; }
        public bool Interlace { get; private set; }
        public bool IsLocalColorTableSorted { get; private set; }
        public int LocalColorTableSize { get; private set; }

        private GifImageDescriptor()
        {
        }

        internal static GifImageDescriptor Read(GifBufferReader reader)
        {
            var descriptor = new GifImageDescriptor();
            descriptor.ReadInternal(reader);
            return descriptor;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            Left = reader.ReadUInt16();
            Top = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();

            byte packedFields = reader.ReadByte();
            ReadPackedFields(packedFields);
        }

        private void ReadPackedFields(byte packedFields)
        {
            HasLocalColorTable = (packedFields & 0x80) != 0;
            Interlace = (packedFields & 0x40) != 0;
            IsLocalColorTableSorted = (packedFields & 0x20) != 0;
            LocalColorTableSize = 1 << ((packedFields & 0x07) + 1);
        }
    }
}
