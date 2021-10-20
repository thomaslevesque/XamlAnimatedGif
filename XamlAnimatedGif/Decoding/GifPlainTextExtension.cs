using System.Collections.Generic;
using System.Linq;

namespace XamlAnimatedGif.Decoding
{
    // label 0x01
    internal class GifPlainTextExtension : GifExtension
    {
        internal const int ExtensionLabel = 0x01;

        public int BlockSize { get; private set; }
        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }
        public int ForegroundColorIndex { get; private set; }
        public int BackgroundColorIndex { get; private set; }
        public string Text { get; private set; }

        public IReadOnlyList<GifExtension> Extensions { get; private set; }

        private GifPlainTextExtension()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.GraphicRendering;

        internal new static GifPlainTextExtension Read(GifBufferReader reader, IEnumerable<GifExtension> controlExtensions)
        {
            var plainText = new GifPlainTextExtension();
            plainText.ReadInternal(reader, controlExtensions);
            return plainText;
        }

        private void ReadInternal(GifBufferReader reader, IEnumerable<GifExtension> controlExtensions)
        {
            // Note: at this point, the label (0x01) has already been read

            BlockSize = reader.ReadByte();
            if (BlockSize != 12)
                throw GifHelpers.InvalidBlockSizeException("Plain Text Extension", 12, BlockSize);

            Left = reader.ReadUInt16();
            Top = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            CellWidth = reader.ReadByte();
            CellHeight = reader.ReadByte();
            ForegroundColorIndex = reader.ReadByte();
            BackgroundColorIndex = reader.ReadByte();

            Text = reader.ReadStringFromDataBlocks();

            Extensions = controlExtensions.ToList();
        }
    }
}
