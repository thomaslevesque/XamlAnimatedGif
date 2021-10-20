namespace XamlAnimatedGif.Decoding
{
    // label 0xF9
    internal class GifGraphicControlExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xF9;

        public int BlockSize { get; private set; }
        public GifFrameDisposalMethod DisposalMethod { get; private set; }
        public bool UserInput { get; private set; }
        public bool HasTransparency { get; private set; }
        public int Delay { get; private set; }
        public int TransparencyIndex { get; private set; }

        private GifGraphicControlExtension()
        {

        }

        internal override GifBlockKind Kind => GifBlockKind.Control;

        internal static GifGraphicControlExtension Read(GifBufferReader reader)
        {
            var ext = new GifGraphicControlExtension();
            ext.ReadInternal(reader);
            return ext;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            BlockSize = reader.ReadByte();
            if (BlockSize != 4)
                throw GifHelpers.InvalidBlockSizeException("Graphic Control Extension", 4, BlockSize);
            var packedFields = reader.ReadByte();
            ReadPackedFields(packedFields);
            Delay = reader.ReadUInt16() * 10; // milliseconds
            TransparencyIndex = reader.ReadByte();
            reader.ReadByte(); // Read block terminator
        }

        private void ReadPackedFields(byte packedFields)
        {
            DisposalMethod = (GifFrameDisposalMethod) ((packedFields & 0x1C) >> 2);
            UserInput = (packedFields & 0x02) != 0;
            HasTransparency = (packedFields & 0x01) != 0;
        }
    }
}
