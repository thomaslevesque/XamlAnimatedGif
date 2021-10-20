using System.Collections.Generic;
using System.Linq;

namespace XamlAnimatedGif.Decoding
{
    internal class GifDataStream
    {
        public GifHeader Header { get; private set; }
        public GifColor[] GlobalColorTable { get; set; }
        public IReadOnlyList<GifFrame> Frames { get; set; }
        public IReadOnlyList<GifExtension> Extensions { get; set; }
        public ushort RepeatCount { get; set; }

        private GifDataStream()
        {
        }

        internal static GifDataStream Read(GifBufferReader reader)
        {
            var file = new GifDataStream();
            file.ReadInternal(reader);
            return file;
        }

        private void ReadInternal(GifBufferReader reader)
        {
            Header = GifHeader.Read(reader);

            if (Header.LogicalScreenDescriptor.HasGlobalColorTable)
            {
                GlobalColorTable = reader.ReadColorTable(Header.LogicalScreenDescriptor.GlobalColorTableSize);
            }

            ReadFrames(reader);

            var netscapeExtension =
                Extensions
                    .OfType<GifApplicationExtension>()
                    .FirstOrDefault(GifHelpers.IsNetscapeExtension);

            RepeatCount = netscapeExtension != null
                ? GifHelpers.GetRepeatCount(netscapeExtension)
                : (ushort)1;
        }

        private void ReadFrames(GifBufferReader reader)
        {
            var frames = new List<GifFrame>();
            var controlExtensions = new List<GifExtension>();
            var specialExtensions = new List<GifExtension>();
            while (true)
            {
                try
                {
                    var block = GifBlock.Read(reader, controlExtensions);

                    if (block.Kind == GifBlockKind.GraphicRendering)
                        controlExtensions = new List<GifExtension>();

                    if (block is GifFrame frame)
                    {
                        frames.Add(frame);
                    }
                    else if (block is GifExtension extension)
                    {
                        switch (extension.Kind)
                        {
                            case GifBlockKind.Control:
                                controlExtensions.Add(extension);
                                break;
                            case GifBlockKind.SpecialPurpose:
                                specialExtensions.Add(extension);
                                break;

                            // Just discard plain text extensions for now, since we have no use for it
                        }
                    }
                    else if (block is GifTrailer)
                    {
                        break;
                    }
                }
                // Follow the same approach as Firefox:
                // If we find extraneous data between blocks, just assume the stream
                // was successfully terminated if we have some successfully decoded frames
                // https://dxr.mozilla.org/firefox/source/modules/libpr0n/decoders/gif/nsGIFDecoder2.cpp#894-909
                catch (UnknownBlockTypeException) when (frames.Count > 0)
                {
                    break;
                }
            }

            Frames = frames;
            Extensions = specialExtensions;
        }
    }
}