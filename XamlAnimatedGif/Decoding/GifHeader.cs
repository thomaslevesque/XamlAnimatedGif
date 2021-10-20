namespace XamlAnimatedGif.Decoding
{
    internal class GifHeader : GifBlock
    {
        public string Signature { get; private set; }
        public string Version { get; private set; }
        public GifLogicalScreenDescriptor LogicalScreenDescriptor { get; private set; }

        private GifHeader()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.Other;

        public static GifHeader Read(GifBufferReader reader)
        {
            var header = new GifHeader();
            header.ReadInternal(reader);
            return header;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            Signature = reader.ReadString(3);
            if (Signature != "GIF")
                throw GifHelpers.InvalidSignatureException(Signature);
            Version = reader.ReadString(3);
            if (Version != "87a" && Version != "89a")
                throw GifHelpers.UnsupportedVersionException(Version);

            LogicalScreenDescriptor = GifLogicalScreenDescriptor.Read(reader);
        }
    }
}
