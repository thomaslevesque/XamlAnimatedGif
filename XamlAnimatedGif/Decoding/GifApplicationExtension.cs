namespace XamlAnimatedGif.Decoding
{
    // label 0xFF
    internal class GifApplicationExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xFF;

        public int BlockSize { get; private set; }
        public string ApplicationIdentifier { get; private set; }
        public byte[] AuthenticationCode { get; private set; }
        public byte[] Data { get; private set; }

        private GifApplicationExtension()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.SpecialPurpose;

        internal static GifApplicationExtension Read(GifBufferReader reader)
        {
            var ext = new GifApplicationExtension();
            ext.ReadInternal(reader);
            return ext;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            // Note: at this point, the label (0xFF) has already been read

            BlockSize = reader.ReadByte();
            if (BlockSize != 11)
                throw GifHelpers.InvalidBlockSizeException("Application Extension", 11, BlockSize);

            ApplicationIdentifier = reader.ReadString(8);
            AuthenticationCode = reader.ReadBytes(3);
            Data = reader.ReadDataBlocks();
        }
    }
}
