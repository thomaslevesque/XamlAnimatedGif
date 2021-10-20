﻿using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif
{
    internal class UriLoader
    {
        public static Task<byte[]> GetDataFromUriAsync(Uri uri, IProgress<int> progress)
        {
            if (uri.IsAbsoluteUri && (uri.Scheme == "http" || uri.Scheme == "https"))
                return GetDataFromNetworkAsync(uri, progress);

            return GetDataFromUriCoreAsync(uri);
        }

        private static async Task<byte[]> GetDataFromNetworkAsync(Uri uri, IProgress<int> progress)
        {
            string cacheFileName = GetCacheFileName(uri);
            var cacheStream = OpenTempFileStream(cacheFileName);
            if (cacheStream == null)
            {
                await DownloadToCacheFileAsync(uri, cacheFileName, progress);
                cacheStream = OpenTempFileStream(cacheFileName);
            }
            progress.Report(100);

            return await cacheStream.ReadAllAsync(true);
        }
        
        private static async Task<byte[]> GetDataFromUriCoreAsync(Uri uri)
        {
            if (uri.Scheme == PackUriHelper.UriSchemePack)
            {
                var sri = uri.Authority == "siteoforigin:,,,"
                    ? Application.GetRemoteStream(uri)
                    : Application.GetResourceStream(uri);

                if (sri != null)
                    return await sri.Stream.ReadAllAsync(true);

                throw new FileNotFoundException("Cannot find file with the specified URI");
            }

            if (uri.Scheme == Uri.UriSchemeFile)
            {
                return await File.OpenRead(uri.LocalPath).ReadAllAsync(true);
            }

            throw new NotSupportedException("Only pack:, file:, http: and https: URIs are supported");
        }

        private static async Task DownloadToCacheFileAsync(Uri uri, string fileName, IProgress<int> progress)
        {
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                long length = response.Content.Headers.ContentLength ?? 0;
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = await CreateTempFileStreamAsync(fileName);
                IProgress<long> absoluteProgress = null;
                if (progress != null)
                {
                    absoluteProgress =
                        new Progress<long>(bytesCopied =>
                        {
                            if (length > 0)
                                progress.Report((int)(100 * bytesCopied / length));
                            else
                                progress.Report(-1);
                        });
                }
                await responseStream.CopyToAsync(fileStream, absoluteProgress);
            }
            catch
            {
                await DeleteTempFileAsync(fileName);
                throw;
            }
        }

        private static Stream OpenTempFileStream(string fileName)
        {
            string path = Path.Combine(Path.GetTempPath(), fileName);
            Stream stream = null;
            try
            {
                stream = File.OpenRead(path);
            }
            catch (FileNotFoundException)
            {
            }

            return stream;
        }

        private static Task<Stream> CreateTempFileStreamAsync(string fileName)
        {
            string path = Path.Combine(Path.GetTempPath(), fileName);
            Stream stream = File.OpenWrite(path);
            stream.SetLength(0);
            return Task.FromResult(stream);
        }

        private static Task DeleteTempFileAsync(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
            return Task.FromResult(fileName);
        }

        private static string GetCacheFileName(Uri uri)
        {
            using var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(uri.AbsoluteUri);
            var hash = sha1.ComputeHash(bytes);
            return ToHex(hash);
        }

        private static string ToHex(byte[] bytes)
        {
            return bytes.Aggregate(
                new StringBuilder(),
                (sb, b) => sb.Append(b.ToString("X2")),
                sb => sb.ToString());
        }
    }
}