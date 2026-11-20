using System;
using System.IO;
using ImageMagick;

namespace Webpify
{
    internal class Program
    {
        // SettingQuality: 0 - 100, depends on the use case (lower = higher compression).
        // SettingMethod: 0 - 6, 6 is maximum compression, but takes more time (~100 ms).
        private const uint SettingQuality = 10;
        private const int SettingMethod = 6;
        private const string OutputFolder = "output";
        private static readonly string[] CheckExts = [".jpg", ".jpeg", ".webp"];

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

        private static void ConvertToWebp(string path, bool isForceConvert)
        {
            byte[] data;

            try
            {
                using MagickImage image = new MagickImage(path);
                image.AutoOrient();
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