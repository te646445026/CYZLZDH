using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 百度OCR 测试 ===\n");

        string keyPath = "e:\\Code\\C#\\CYZLZDH\\key.json";

        Console.WriteLine($"读取配置文件: {keyPath}");
        string keyJson = File.ReadAllText(keyPath);
        var key = JObject.Parse(keyJson);

        string apiKey = key["API_KEY"]?.ToString();
        string secretKey = key["SECRET_KEY"]?.ToString();

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey))
        {
            Console.WriteLine("错误: 未配置百度OCR密钥");
            return;
        }

        Console.WriteLine("API Key: " + apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "...");
        Console.WriteLine("Secret Key: " + secretKey.Substring(0, Math.Min(10, secretKey.Length)) + "...\n");

        Console.WriteLine("步骤1: 获取Access Token...");
        using var httpClient = new HttpClient();

        var tokenParams = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", apiKey),
            new KeyValuePair<string, string>("client_secret", secretKey)
        });

        var tokenResponse = await httpClient.PostAsync("https://aip.baidubce.com/oauth/2.0/token", tokenParams);
        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        Console.WriteLine("Token响应: " + tokenContent);

        var tokenJson = JObject.Parse(tokenContent);
        string accessToken = tokenJson["access_token"]?.ToString();

        if (string.IsNullOrEmpty(accessToken))
        {
            Console.WriteLine("获取Token失败!");
            return;
        }

        Console.WriteLine("获取Token成功!\n");

        string imagePath = @"e:\Code\C#\CYZLZDH\江门市蓬江区龙华酒店有限公司.png";
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"错误: 图片文件不存在: {imagePath}");
            return;
        }

        Console.WriteLine($"步骤2: 读取图片: {Path.GetFileName(imagePath)}");
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string imageBase64 = Convert.ToBase64String(imageBytes);
        Console.WriteLine($"图片大小: {imageBytes.Length} bytes, Base64长度: {imageBase64.Length}\n");

        Console.WriteLine("步骤3: 调用百度OCR表格识别...");
        var ocrUrl = $"https://aip.baidubce.com/rest/2.0/ocr/v1/table?access_token={accessToken}";

        var ocrParams = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("image", imageBase64),
            new KeyValuePair<string, string>("table_type", "advance")
        });

        var ocrResponse = await httpClient.PostAsync(ocrUrl, ocrParams);
        var ocrContent = await ocrResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"OCR响应状态: {ocrResponse.StatusCode}");
        Console.WriteLine($"OCR响应长度: {ocrContent.Length} characters\n");

        string outputDir = "e:\\Code\\C#\\CYZLZDH\\ocr_extract\\OCR_JSON";
        Directory.CreateDirectory(outputDir);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputPath = Path.Combine(outputDir, $"BaiduOcr_Response_{timestamp}.json");
        File.WriteAllText(outputPath, ocrContent);
        Console.WriteLine($"JSON文件已保存: {outputPath}");

        Console.WriteLine("\n=== 响应内容预览 ===");
        if (ocrContent.Length > 2000)
        {
            Console.WriteLine(ocrContent.Substring(0, 2000) + "...");
        }
        else
        {
            Console.WriteLine(ocrContent);
        }

        Console.WriteLine("\n=== 测试完成 ===");
    }
}
