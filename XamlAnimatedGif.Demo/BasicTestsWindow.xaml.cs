using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Win32;
using XamlAnimatedGif.Decoding;
using XamlAnimatedGif.Decompression;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif.Demo
{
    public partial class BasicTestsWindow
    {
        public BasicTestsWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {Filter = "GIF files|*.gif"};
            if (dlg.ShowDialog() == true)
            {
                txtFileName.Text = dlg.FileName;
            }
        }

        private async void BtnTestLzw_OnClick(object sender, RoutedEventArgs e)
        {
            string fileName = txtFileName.Text;
            if (string.IsNullOrEmpty(fileName))
                return;

            btnTestLzw.IsEnabled = false;
            try
            {
                await DecompressAllFramesAsync(fileName);
            }
            finally
            {
                btnTestLzw.IsEnabled = true;
            }
        }

        private void BtnTestStream_OnClick(object sender, RoutedEventArgs e)
        {
            string fileName = txtFileName.Text;
            if (string.IsNullOrEmpty(fileName))
                return;

            var img = new Image { Stretch = Stretch.None };
            var wnd = new Window { Content = img };

            using var fileStream = File.OpenRead(fileName);
            AnimationBehavior.SetSourceStream(img, fileStream);
            wnd.ShowDialog();
            wnd.Close();
            AnimationBehavior.SetSourceStream(img, null);
        }

        private static async Task DecompressAllFramesAsync(string path)
        {
            var data = await File.OpenRead(path).ReadAllAsync(true);
            var reader = new GifBufferReader(data);
            var gif = GifDataStream.Read(reader);
            for (int i = 0; i < gif.Frames.Count; i++)
            {
                var frame = gif.Frames[i];
                using var ms = new MemoryStream();
                Decompress(data, frame.ImageData, ms);
                using var indOutStream = File.OpenWrite($"{path}.{i}.ind");
                ms.Seek(0, SeekOrigin.Begin);
                await ms.CopyToAsync(indOutStream);
            }
        }

        private static void Decompress(byte[] buffer, GifImageData data, Stream destination)
        {
            Lzw.Decompress(buffer.AsSpan((int)data.CompressedDataStartOffset, data.Length), destination, data.LzwMinimumCodeSize);
        }

        private async void BtnTestBrush_OnClick(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("pack://application:,,,/images/earth.gif");
            using var animator = await BrushAnimator.CreateAsync(uri, RepeatBehavior.Forever);
            var window = new Window
            {
                Width = 500,
                Height = 250,
                Content = new Ellipse
                {
                    Width = 400,
                    Height = 200,
                    Fill = animator.Brush
                }
            };
            animator.Play();
            window.ShowDialog();
        }
    }
}
