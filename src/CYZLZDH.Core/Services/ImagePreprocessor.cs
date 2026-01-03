using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Extensions.Logging;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

public class ImagePreprocessor : IImagePreprocessor
{
    private readonly ILogger<ImagePreprocessor> _logger;

    public ImagePreprocessor(ILogger<ImagePreprocessor> logger)
    {
        _logger = logger;
    }

    public string ProcessImage(string imageBase64)
    {
        _logger.LogInformation("开始图像预处理");

        try
        {
            string base64Data = imageBase64;
            
            if (base64Data.StartsWith("{"))
            {
                var dataPrefixIndex = base64Data.IndexOf("data:image/");
                if (dataPrefixIndex >= 0)
                {
                    var commaIndex = base64Data.IndexOf(",", dataPrefixIndex);
                    var closingQuoteIndex = base64Data.IndexOf("\"", commaIndex + 1);
                    if (commaIndex >= 0 && closingQuoteIndex > commaIndex)
                    {
                        base64Data = base64Data.Substring(commaIndex + 1, closingQuoteIndex - commaIndex - 1);
                    }
                }
            }
            else if (base64Data.Contains(","))
            {
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
            }
            
            byte[] imageBytes = Convert.FromBase64String(base64Data);
            
            using (var ms = new MemoryStream(imageBytes))
            using (var originalImage = Image.FromStream(ms))
            using (var bitmap = new Bitmap(originalImage))
            {
                _logger.LogDebug("原始图像尺寸: {Width}x{Height}", bitmap.Width, bitmap.Height);

                var processedBitmap = bitmap;

                processedBitmap = ConvertToGrayscale(processedBitmap);
                _logger.LogDebug("完成灰度化处理");

                processedBitmap = EnhanceContrast(processedBitmap, 1.05);
                _logger.LogDebug("完成轻微对比度增强");

                processedBitmap = LightSharpenImage(processedBitmap);
                _logger.LogDebug("完成轻微锐化处理");

                using (var outputMs = new MemoryStream())
                {
                    processedBitmap.Save(outputMs, ImageFormat.Jpeg);
                    var processedBase64 = Convert.ToBase64String(outputMs.ToArray());
                    
                    _logger.LogInformation("图像预处理完成，输出大小: {Size} 字符", processedBase64.Length);
                    return processedBase64;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图像预处理过程中发生错误");
            throw;
        }
    }

    private Bitmap ConvertToGrayscale(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                result.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
            }
        }

        return result;
    }

    private Bitmap EnhanceContrast(Bitmap source, double contrast)
    {
        var result = new Bitmap(source.Width, source.Height);
        double factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                int r = Clamp((int)(factor * (pixel.R - 128) + 128));
                int g = Clamp((int)(factor * (pixel.G - 128) + 128));
                int b = Clamp((int)(factor * (pixel.B - 128) + 128));
                result.SetPixel(x, y, Color.FromArgb(r, g, b));
            }
        }

        return result;
    }

    private Bitmap LightSharpenImage(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height);
        double[,] kernel = {
            { 0, -0.5,  0 },
            { -0.5,  3, -0.5 },
            { 0, -0.5,  0 }
        };

        for (int y = 1; y < source.Height - 1; y++)
        {
            for (int x = 1; x < source.Width - 1; x++)
            {
                double sum = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        Color pixel = source.GetPixel(x + kx, y + ky);
                        sum += pixel.R * kernel[ky + 1, kx + 1];
                    }
                }

                int value = Clamp((int)sum);
                result.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
        }

        return result;
    }

    private int Clamp(int value)
    {
        return Math.Max(0, Math.Min(255, value));
    }
}
