namespace XamlAnimatedGif.Decoding
{
    internal class GifCommentExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xFE;

        public string Text { get; private set; }

        private GifCommentExtension()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.SpecialPurpose;

        internal static GifCommentExtension Read(GifBufferReader reader)
        {
            var comment = new GifCommentExtension();
            comment.ReadInternal(reader);
            return comment;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            // Note: at this point, the label (0xFE) has already been read

            Text = reader.ReadStringFromDataBlocks();
        }
    }
}
