using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace SkImageResizer
{
    public class SKImageProcess
    {
        /// <summary>
        /// 進行圖片的縮放作業
        /// </summary>
        /// <param name="sourcePath">圖片來源目錄路徑</param>
        /// <param name="destPath">產生圖片目的目錄路徑</param>
        /// <param name="scale">縮放比例</param>
        public void ResizeImages(string sourcePath, string destPath, double scale)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            var allFiles = FindImages(sourcePath);
            foreach (var filePath in allFiles)
            {
                var bitmap = SKBitmap.Decode(filePath);
                var imgPhoto = SKImage.FromBitmap(bitmap);
                var imgName = Path.GetFileNameWithoutExtension(filePath);

                var sourceWidth = imgPhoto.Width;
                var sourceHeight = imgPhoto.Height;

                var destinationWidth = (int) (sourceWidth * scale);
                var destinationHeight = (int) (sourceHeight * scale);

                using var scaledBitmap = bitmap.Resize(
                    new SKImageInfo(destinationWidth, destinationHeight),
                    SKFilterQuality.High);
                using var scaledImage = SKImage.FromBitmap(scaledBitmap);
                using var data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
                data.SaveTo(s);
            }
        }

        public async Task ResizeImagesAsync(string sourcePath, string destPath, double scale)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            await Task.Yield();

            var allFiles = FindImages(sourcePath);

            var allTasks = new List<Task>();
            var allResult = new List<ResizeResult>();

            foreach (string filePath in allFiles)
            {
                string path = filePath;
                allTasks.Add(Task.Run(() =>
                {
                    var resizeResult = DoResize(scale, path);
                    allResult.Add(resizeResult);
                }));
            }

            await Task.WhenAll(allTasks);

            foreach (var resizeResult in allResult)
            {
                SaveFile(destPath, resizeResult.ImgName, resizeResult.Data);
            }
        }

        private ResizeResult DoResize(double scale, string filePath)
        {
            var resizeResult = new ResizeResult();

            var bitmap = SKBitmap.Decode(filePath);
            var imgPhoto = SKImage.FromBitmap(bitmap);
            resizeResult.ImgName = Path.GetFileNameWithoutExtension(filePath);

            var sourceWidth = imgPhoto.Width;
            var sourceHeight = imgPhoto.Height;

            var destinationWidth = (int) (sourceWidth * scale);
            var destinationHeight = (int) (sourceHeight * scale);

            using var scaledBitmap = bitmap.Resize(
                new SKImageInfo(destinationWidth, destinationHeight),
                SKFilterQuality.High);

            using var scaledImage = SKImage.FromBitmap(scaledBitmap);
            resizeResult.Data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 100);
            return resizeResult;
        }

        private void SaveFile(string destPath, string? imgName, SKData data)
        {
            using var s = File.OpenWrite(Path.Combine(destPath, imgName + ".jpg"));
            data.SaveTo(s);
        }

        private Task SaveFileAsync(string destPath, string? imgName, SKData data)
        {
            return Task.Run(() => { SaveFile(destPath, imgName, data); });
        }

        /// <summary>
        /// 清空目的目錄下的所有檔案與目錄
        /// </summary>
        /// <param name="destPath">目錄路徑</param>
        public void Clean(string destPath)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                var allImageFiles = Directory.GetFiles(destPath, "*", SearchOption.AllDirectories);

                foreach (var item in allImageFiles)
                {
                    File.Delete(item);
                }
            }
        }

        /// <summary>
        /// 找出指定目錄下的圖片
        /// </summary>
        /// <param name="srcPath">圖片來源目錄路徑</param>
        /// <returns></returns>
        public List<string> FindImages(string srcPath)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(srcPath, "*.png", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpg", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(srcPath, "*.jpeg", SearchOption.AllDirectories));
            return files;
        }
    }

    internal class ResizeResult
    {
        public string? ImgName { get; set; }
        public SKData Data { get; set; }
    }
}