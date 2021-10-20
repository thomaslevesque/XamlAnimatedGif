using System.Collections.Generic;

namespace XamlAnimatedGif.Decoding
{
    internal abstract class GifBlock
    {
        internal static GifBlock Read(GifBufferReader reader, IEnumerable<GifExtension> controlExtensions)
        {
            int blockId = reader.ReadByte();
            return blockId switch
            {
                GifExtension.ExtensionIntroducer => GifExtension.Read(reader, controlExtensions),
                GifFrame.ImageSeparator => GifFrame.Read(reader, controlExtensions),
                GifTrailer.TrailerByte => GifTrailer.Read(),
                _ => throw GifHelpers.UnknownBlockTypeException(blockId),
            };
        }

        internal abstract GifBlockKind Kind { get; }
    }
}
