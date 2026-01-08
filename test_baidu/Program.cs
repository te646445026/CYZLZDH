using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

class TestOutputHelper : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}

class Program
{
    private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 百度OCR API测试 ===\n");

        string imagePath = "e:\\Code\\C#\\CYZLZDH\\江门市蓬江区龙华酒店有限公司.png";

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"错误: 图片文件不存在: {imagePath}");
            return;
        }

        Console.WriteLine($"读取图片文件: {Path.GetFileName(imagePath)}");
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string imageBase64 = Convert.ToBase64String(imageBytes);
        Console.WriteLine($"图片大小: {imageBytes.Length} bytes\n");

        string apiKey = "";
        string secretKey = "";

        string keyPath = "e:\\Code\\C#\\CYZLZDH\\key.json";
        if (File.Exists(keyPath))
        {
            try
            {
                string keyJson = File.ReadAllText(keyPath);
                var keyObj = JObject.Parse(keyJson);
                apiKey = keyObj["BAIDU_API_KEY"]?.ToString() ?? "";
                secretKey = keyObj["BAIDU_SECRET_KEY"]?.ToString() ?? "";
                Console.WriteLine($"已从 {keyPath} 读取API密钥");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取API密钥失败: {ex.Message}");
                return;
            }
        }
        else
        {
            Console.WriteLine($"错误: API密钥文件不存在: {keyPath}");
            return;
        }

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secretKey))
        {
            Console.WriteLine("错误: API密钥为空");
            return;
        }

        Console.WriteLine($"API Key: {apiKey.Substring(0, 8)}...");
        Console.WriteLine($"Secret Key: {secretKey.Substring(0, 8)}...\n");

        try
        {
            Console.WriteLine("步骤1: 获取访问令牌...");
            string accessToken = await GetAccessTokenAsync(apiKey, secretKey);
            Console.WriteLine($"访问令牌获取成功: {accessToken.Substring(0, 20)}...\n");

            Console.WriteLine("步骤2: 调用OCR识别API...");
            string ocrResult = await CallOcrApiAsync(accessToken, imageBase64);
            Console.WriteLine($"OCR识别成功，返回JSON长度: {ocrResult.Length} characters\n");

            var json = JObject.Parse(ocrResult);

            if (json["error_code"] != null)
            {
                var errorCode = json["error_code"]?.ToString();
                var errorMsg = json["error_msg"]?.ToString();
                Console.WriteLine($"错误: OCR API返回错误 - {errorCode}: {errorMsg}");
                return;
            }

            Console.WriteLine("步骤3: 保存JSON结果...");
            string outputDir = Path.Combine(Path.GetDirectoryName(imagePath) ?? ".", "OCR_JSON");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputFile = Path.Combine(outputDir, $"BaiduOcr_Response_{timestamp}.json");
            File.WriteAllText(outputFile, ocrResult);
            Console.WriteLine($"JSON结果已保存: {outputFile}\n");

            Console.WriteLine("=== 测试成功 ===");
            Console.WriteLine($"JSON文件: {outputFile}");
            Console.WriteLine($"文件大小: {ocrResult.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n错误: {ex.Message}");
            Console.WriteLine($"详细信息: {ex}");
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    private static async Task<string> GetAccessTokenAsync(string apiKey, string secretKey)
    {
        string tokenUrl = "https://aip.baidubce.com/oauth/2.0/token";

        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", apiKey),
            new KeyValuePair<string, string>("client_secret", secretKey)
        });

        var response = await Client.PostAsync(tokenUrl, parameters);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(responseContent);

        if (json["error"] != null)
        {
            var error = json["error_description"]?.ToString() ?? json["error"]?.ToString();
            throw new Exception($"获取访问令牌失败: {error}");
        }

        string accessToken = json["access_token"]?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("获取访问令牌返回为空");
        }

        return accessToken;
    }

    private static async Task<string> CallOcrApiAsync(string accessToken, string imageBase64)
    {
        string ocrUrl = "https://aip.baidubce.com/rest/2.0/ocr/v1/table";

        var body = new StringContent($"access_token={accessToken}&image={Uri.EscapeDataString(imageBase64)}&detect_direction=true&column_layout=true&xml=False");
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await Client.PostAsync(ocrUrl, body);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(responseContent);

        if (json["error_code"] != null)
        {
            var errorCode = json["error_code"]?.ToString();
            var errorMsg = json["error_msg"]?.ToString();
            throw new Exception($"百度OCR API错误: {errorCode} - {errorMsg}");
        }

        return responseContent;
    }
}
