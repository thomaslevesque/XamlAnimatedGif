using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using XamlAnimatedGif.Decoding;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif
{
    internal class ImageAnimator : Animator
    {
        private readonly Image _image;

        public ImageAnimator(byte[] sourceBuffer, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior, Image image) : base(sourceBuffer, sourceUri, metadata, repeatBehavior)
        {
            _image = image;
            OnRepeatBehaviorChanged(); // in case the value has changed during creation
        }

        protected override RepeatBehavior GetSpecifiedRepeatBehavior() => AnimationBehavior.GetRepeatBehavior(_image);

        protected override object AnimationSource => _image;

        public static Task<ImageAnimator> CreateAsync(Uri sourceUri, RepeatBehavior repeatBehavior, IProgress<int> progress, Image image)
        {
            return CreateAsyncCore(
                sourceUri,
                progress,
                (buffer, metadata) => new ImageAnimator(buffer, sourceUri, metadata, repeatBehavior, image));
        }

        public static async Task<ImageAnimator> CreateAsync(Stream sourceStream, RepeatBehavior repeatBehavior, Image image)
        {
            return await CreateAsyncCore(
                sourceStream,
                (buffer, metadata) => new ImageAnimator(buffer, null, metadata, repeatBehavior, image));
        }
    }
}