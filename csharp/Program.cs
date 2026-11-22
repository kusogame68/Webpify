using System;
using System.IO;
using ImageMagick;

namespace Webpify
{
    internal class Program
    {
        // SettingQuality: 0 - 100, depends on the use case (lower = higher compression).
        // SettingMethod: 0 - 6, 6 is maximum compression, but takes more time (~100 ms).
        // MaxImagePixels: images larger than this are rejected before decoding.
        private const uint SettingQuality = 10;
        private const int SettingMethod = 6;
        private const string OutputFolder = "output";
        private static readonly MagickFormat[] AllowedFormats = [MagickFormat.Jpeg, MagickFormat.Png, MagickFormat.WebP];
        private const long MaxImagePixels = 25000000L;
        private static readonly string[] CheckExts = [".jpg", ".jpeg", ".webp"];
        private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        static void Main(string[] args)
        {
            Console.Write("If source image size is better than after convert, do you still want to convert? (Y/N): ");
            bool isForceConvert = Console.ReadLine()?.Trim().ToLower() == "y";

            Directory.CreateDirectory(OutputFolder);
            Parallel.ForEach(ExpandArgs(args), path => ConvertToWebp(path, isForceConvert));

            Console.ReadKey();
        }

        private static List<string> ExpandArgs(string[] args) =>
            args.SelectMany(ExpandPattern)
                .DistinctBy(Path.GetFullPath)
                .ToList();

        private static string[] ExpandPattern(string arg)
        {
            if (File.Exists(arg)) return [arg];

            string? rawDir = Path.GetDirectoryName(arg);
            string dir = !string.IsNullOrEmpty(rawDir) ? rawDir : ".";
            string[] matched = Directory.Exists(dir)
                ? Directory.GetFiles(dir, Path.GetFileName(arg))
                : [];

            if (matched.Length == 0) Console.WriteLine($"{arg} is not found.");
            return matched;
        }

        // ref: https://en.wikipedia.org/wiki/List_of_file_signatures
        // Check the file signature (magic bytes) before ImageMagick reads the file.
        // ImageMagick falls back to the file extension when it cannot recognize the content,
        // so a disguised file could reach a decoder outside the whitelist (e.g. SVG).
        // This check ensures only JPEG / PNG / WebP files are ever passed to ImageMagick.
        //
        // The header buffer is allocated on the stack (stackalloc), so it needs no heap
        // allocation or garbage collection and is released when the method returns.
        private static bool HasAllowedSignature(string path)
        {
            Span<byte> header = stackalloc byte[12];
            using FileStream stream = File.OpenRead(path);
            int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
            header = header[..read];

            return header.StartsWith(JpegSignature)
                || header.StartsWith(PngSignature)
                || (read == 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..].SequenceEqual("WEBP"u8));
        }

        private static void ConvertToWebp(string path, bool isForceConvert)
        {
            byte[] data;

            try
            {
                if (!HasAllowedSignature(path))
                {
                    Console.WriteLine($"Failed: {path} (unsupported format)");
                    return;
                }

                MagickImageInfo info = new MagickImageInfo(path);
                if (!AllowedFormats.Contains(info.Format))
                {
                    Console.WriteLine($"Failed: {path} (unsupported format: {info.Format})");
                    return;
                }

                long pixels = (long) info.Width * info.Height;
                if (pixels > MaxImagePixels)
                {
                    Console.WriteLine($"Failed: {path} (Image size ({pixels} pixels) exceeds limit of {MaxImagePixels} pixels)");
                    return;
                }

                using MagickImage image = new MagickImage(path);
                image.AutoOrient();
                image.TransformColorSpace(ColorProfiles.SRGB);
                image.Strip();
                image.Quality = SettingQuality;
                image.Settings.SetDefine(MagickFormat.WebP, "method", SettingMethod.ToString());
                data = image.ToByteArray(MagickFormat.WebP);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed: {path} ({e.Message})");
                return;
            }

            string ext = Path.GetExtension(path).ToLower();

            if (CheckExts.Contains(ext) && !isForceConvert)
            {
                long sourceSize = new FileInfo(path).Length;
                if (data.Length >= sourceSize)
                {
                    Console.WriteLine($"Skip: {path} (webp) is not smaller.");
                    return;
                }
            }

            string webpPath = Path.Combine(OutputFolder, $"{Path.GetFileNameWithoutExtension(path)}.webp");

            try
            {
                File.WriteAllBytes(webpPath, data);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Failed: {path} ({e.Message})");
            }
        }
    }
}