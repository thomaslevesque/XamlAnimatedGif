namespace XamlAnimatedGif.Decoding
{
    internal class GifLogicalScreenDescriptor : IGifRect
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool HasGlobalColorTable { get; private set; }
        public int ColorResolution { get; private set; }
        public bool IsGlobalColorTableSorted { get; private set; }
        public int GlobalColorTableSize { get; private set; }
        public int BackgroundColorIndex { get; private set; }
        public double PixelAspectRatio { get; private set; }

        public static GifLogicalScreenDescriptor Read(GifBufferReader reader)
        {
            var descriptor = new GifLogicalScreenDescriptor();
            descriptor.ReadInternal(reader);
            return descriptor;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            byte packedFields = reader.ReadByte();
            ReadPackedFields(packedFields);
            BackgroundColorIndex = reader.ReadByte();
            var rawAspectRatio = reader.ReadByte();
            PixelAspectRatio = GetPixelAspectRatio(rawAspectRatio);
        }

        private void ReadPackedFields(byte packedFields)
        {
            HasGlobalColorTable = (packedFields & 0x80) != 0;
            ColorResolution = ((packedFields & 0x70) >> 4) + 1;
            IsGlobalColorTableSorted = (packedFields & 0x08) != 0;
            GlobalColorTableSize = 1 << ((packedFields & 0x07) + 1);
        }

        private static double GetPixelAspectRatio(byte rawAspectRatio) =>
            rawAspectRatio == 0
                ? 0.0
                : (15 + rawAspectRatio) / 64.0;

        int IGifRect.Left => 0;

        int IGifRect.Top => 0;
    }
}
