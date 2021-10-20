namespace XamlAnimatedGif.Decoding
{
    internal class GifImageData
    {
        public byte LzwMinimumCodeSize { get; set; }
        public long CompressedDataStartOffset { get; set; }
        public int Length { get; set; }

        private GifImageData()
        {
        }

        internal static GifImageData Read(GifBufferReader reader)
        {
            var imgData = new GifImageData();
            imgData.ReadInternal(reader);
            return imgData;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            long start = reader.Position;
            LzwMinimumCodeSize = reader.ReadByte();
            CompressedDataStartOffset = reader.Position;
            reader.SkipDataBlocks();

            Length = (int)(reader.Position - start);
        }
    }
}
