using System.Collections.Generic;
using System.Linq;

namespace XamlAnimatedGif.Decoding
{
    internal class GifFrame : GifBlock
    {
        internal const int ImageSeparator = 0x2C;

        public GifImageDescriptor Descriptor { get; private set; }
        public GifColor[] LocalColorTable { get; private set; }
        public IReadOnlyList<GifExtension> Extensions { get; private set; }
        public GifImageData ImageData { get; private set; }
        public GifGraphicControlExtension GraphicControl { get; set; }

        private GifFrame()
        {
        }

        internal override GifBlockKind Kind => GifBlockKind.GraphicRendering;

        internal new static GifFrame Read(GifBufferReader reader, IEnumerable<GifExtension> controlExtensions)
        {
            var frame = new GifFrame();
            frame.ReadInternal(reader, controlExtensions);
            return frame;
        }

        private void ReadInternal(GifBufferReader reader, IEnumerable<GifExtension> controlExtensions)
        {
            // Note: at this point, the Image Separator (0x2C) has already been read

            Descriptor = GifImageDescriptor.Read(reader);
            if (Descriptor.HasLocalColorTable)
            {
                LocalColorTable = reader.ReadColorTable(Descriptor.LocalColorTableSize);
            }

            ImageData = GifImageData.Read(reader);
            Extensions = controlExtensions.ToList();
            GraphicControl = Extensions.OfType<GifGraphicControlExtension>().LastOrDefault();
        }
    }
}
