using System.Collections.Generic;

namespace XamlAnimatedGif.Decoding
{
    internal abstract class GifExtension : GifBlock
    {
        internal const int ExtensionIntroducer = 0x21;

        internal new static GifExtension Read(GifBufferReader reader, IEnumerable<GifExtension> controlExtensions)
        {
            // Note: at this point, the Extension Introducer (0x21) has already been read

            int label = reader.ReadByte();
            return label switch
            {
                GifGraphicControlExtension.ExtensionLabel => GifGraphicControlExtension.Read(reader),
                GifCommentExtension.ExtensionLabel => GifCommentExtension.Read(reader),
                GifPlainTextExtension.ExtensionLabel => GifPlainTextExtension.Read(reader, controlExtensions),
                GifApplicationExtension.ExtensionLabel => GifApplicationExtension.Read(reader),
                _ => throw GifHelpers.UnknownExtensionTypeException(label),
            };
        }
    }
}
