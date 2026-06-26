using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZoneOcrPrototype;

/// <summary>
/// 区域OCR原型验证程序
/// 调用腾讯云 GeneralAccurateOCR（通用文字识别高精度版）
/// 输出 TextDetections 结构 + 生成可视化HTML报告
/// </summary>
internal class Program
{
    // 默认路径（相对项目根目录的上级CYZLZDH目录）
    private const string DefaultKeyPath = @"f:\C#\CYZLZDH\CYZLZDH\key.json";
    private const string DefaultImagePath = @"f:\C#\CYZLZDH\CYZLZDH\XODTK22254.png";

    private const string Service = "ocr";
    private const string Version = "2018-11-19";
    private const string Action = "GeneralAccurateOCR";
    private const string Region = "ap-guangzhou";
    private const string Host = "ocr.tencentcloudapi.com";

    private static readonly HttpClient Client = new HttpClient();

    private static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;

            // 命令行参数：[keyPath] [imagePath]
            string keyPath = args.Length > 0 ? args[0] : DefaultKeyPath;
            string imagePath = args.Length > 1 ? args[1] : DefaultImagePath;

            Console.WriteLine("=== 区域OCR原型验证 (GeneralAccurateOCR) ===");
            Console.WriteLine($"密钥文件: {keyPath}");
            Console.WriteLine($"图片文件: {imagePath}");
            Console.WriteLine();

            // 1. 读取密钥
            if (!File.Exists(keyPath))
            {
                Console.WriteLine($"[错误] 密钥文件不存在: {keyPath}");
                return 1;
            }
            var keyJson = File.ReadAllText(keyPath);
            var key = JObject.Parse(keyJson);
            string secretId = key["API_KEY"]?.ToString() ?? "";
            string secretKey = key["SECRET_KEY"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(secretId) || string.IsNullOrEmpty(secretKey))
            {
                Console.WriteLine("[错误] 密钥文件缺少 API_KEY 或 SECRET_KEY 字段");
                return 1;
            }
            Console.WriteLine($"SecretId: {secretId.Substring(0, Math.Min(8, secretId.Length))}...");

            // 2. 读取图片
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"[错误] 图片不存在: {imagePath}");
                return 1;
            }
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string imageBase64 = Convert.ToBase64String(imageBytes);
            Console.WriteLine($"图片大小: {imageBytes.Length / 1024.0:F1} KB, Base64长度: {imageBase64.Length}");

            // 获取图片原始尺寸（用于HTML可视化缩放）
            int imgWidth = 0, imgHeight = 0;
            using (var img = Image.FromFile(imagePath))
            {
                imgWidth = img.Width;
                imgHeight = img.Height;
            }
            Console.WriteLine($"图片尺寸: {imgWidth} x {imgHeight}");
            Console.WriteLine();

            // 3. 调用 GeneralAccurateOCR
            Console.WriteLine("正在调用 GeneralAccurateOCR...");
            string result = CallGeneralAccurateOCR(secretId, secretKey, imageBase64);
            Console.WriteLine("调用成功！");
            Console.WriteLine($"返回JSON长度: {result.Length} 字符");
            Console.WriteLine();

            // 4. 保存完整JSON
            string outDir = Path.GetDirectoryName(imagePath) ?? ".";
            string baseName = Path.GetFileNameWithoutExtension(imagePath);
            string jsonPath = Path.Combine(outDir, $"GeneralAccurateOCR_{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(jsonPath, FormatJson(result));
            Console.WriteLine($"完整JSON已保存: {jsonPath}");
            Console.WriteLine();

            // 5. 解析并打印 TextDetections 摘要
            var objs = JObject.Parse(result);
            var textDetections = objs["Response"]?["TextDetections"] as JArray;
            if (textDetections == null)
            {
                Console.WriteLine("[警告] Response.TextDetections 为空");
                Console.WriteLine("原始返回:");
                Console.WriteLine(FormatJson(result));
                return 2;
            }

            Console.WriteLine($"=== TextDetections 共 {textDetections.Count} 行 ===");
            Console.WriteLine($"{"#",-4}{"X",-6}{"Y",-6}{"W",-6}{"H",-6}{"Conf",-6} DetectedText");
            Console.WriteLine(new string('-', 80));
            for (int i = 0; i < textDetections.Count; i++)
            {
                var td = textDetections[i];
                var ip = td["ItemPolygon"];
                int x = ip?["X"]?.Value<int>() ?? 0;
                int y = ip?["Y"]?.Value<int>() ?? 0;
                int w = ip?["Width"]?.Value<int>() ?? 0;
                int h = ip?["Height"]?.Value<int>() ?? 0;
                int conf = td["Confidence"]?.Value<int>() ?? 0;
                string text = td["DetectedText"]?.ToString() ?? "";
                Console.WriteLine($"{i + 1,-4}{x,-6}{y,-6}{w,-6}{h,-6}{conf,-6} {text}");
            }
            Console.WriteLine();

            // 6. 生成可视化HTML
            string htmlPath = Path.Combine(outDir, $"GeneralAccurateOCR_{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            string html = BuildVisualizationHtml(imageBase64, imgWidth, imgHeight, textDetections);
            File.WriteAllText(htmlPath, html, Encoding.UTF8);
            Console.WriteLine($"可视化HTML已生成: {htmlPath}");
            Console.WriteLine("用浏览器打开即可查看文字框分布，方便规划区域坐标。");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[异常] {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 99;
        }
    }

    /// <summary>
    /// 调用腾讯云 GeneralAccurateOCR 接口
    /// 签名算法复用自 TencentOcrService
    /// </summary>
    private static string CallGeneralAccurateOCR(string secretId, string secretKey, string imageBase64)
    {
        var jsonBody = "{\"ImageBase64\":\"" + imageBase64 + "\"}";
        var request = BuildRequest(secretId, secretKey, jsonBody);
        var response = Client.SendAsync(request).Result;
        var result = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {result}");
        }
        return result;
    }

    private static HttpRequestMessage BuildRequest(string secretId, string secretKey, string jsonBody)
    {
        var contentType = "application/json; charset=utf-8";
        var timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        var auth = GetAuth(secretId, secretKey, Host, contentType, timestamp, jsonBody);

        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.Headers.Add("Host", Host);
        request.Headers.Add("X-TC-Timestamp", timestamp);
        request.Headers.Add("X-TC-Version", Version);
        request.Headers.Add("X-TC-Action", Action);
        request.Headers.Add("X-TC-Region", Region);
        request.Headers.Add("X-TC-Token", "");
        request.Headers.Add("X-TC-RequestClient", "SDK_NET_BAREBONE");
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        request.RequestUri = new Uri("https://" + Host);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return request;
    }

    private static string GetAuth(string secretId, string secretKey, string host, string contentType, string timestamp, string body)
    {
        var canonicalURI = "/";
        var canonicalHeaders = "content-type:" + contentType + "\nhost:" + host + "\n";
        var signedHeaders = "content-type;host";
        var hashedRequestPayload = Sha256Hex(body);
        var canonicalRequest = "POST" + "\n"
                                          + canonicalURI + "\n"
                                          + "\n"
                                          + canonicalHeaders + "\n"
                                          + signedHeaders + "\n"
                                          + hashedRequestPayload;

        var algorithm = "TC3-HMAC-SHA256";
        var date = DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0), DateTimeKind.Utc).AddSeconds(int.Parse(timestamp))
                .ToString("yyyy-MM-dd");
        var service = host.Split('.')[0];
        var credentialScope = date + "/" + service + "/" + "tc3_request";
        var hashedCanonicalRequest = Sha256Hex(canonicalRequest);
        var stringToSign = algorithm + "\n"
                                         + timestamp + "\n"
                                         + credentialScope + "\n"
                                         + hashedCanonicalRequest;

        var tc3SecretKey = Encoding.UTF8.GetBytes("TC3" + secretKey);
        var secretDate = HmacSha256(tc3SecretKey, Encoding.UTF8.GetBytes(date));
        var secretService = HmacSha256(secretDate, Encoding.UTF8.GetBytes(service));
        var secretSigning = HmacSha256(secretService, Encoding.UTF8.GetBytes("tc3_request"));
        var signatureBytes = HmacSha256(secretSigning, Encoding.UTF8.GetBytes(stringToSign));
        var signature = BitConverter.ToString(signatureBytes).Replace("-", string.Empty).ToLower();

        return algorithm + " "
                             + "Credential=" + secretId + "/" + credentialScope + ", "
                             + "SignedHeaders=" + signedHeaders + ", "
                             + "Signature=" + signature;
    }

    private static string Sha256Hex(string s)
    {
        using (SHA256 algo = SHA256.Create())
        {
            byte[] hashbytes = algo.ComputeHash(Encoding.UTF8.GetBytes(s));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashbytes.Length; ++i)
            {
                builder.Append(hashbytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    private static byte[] HmacSha256(byte[] key, byte[] msg)
    {
        using (HMACSHA256 mac = new HMACSHA256(key))
        {
            return mac.ComputeHash(msg);
        }
    }

    private static string FormatJson(string json)
    {
        try
        {
            var parsed = JObject.Parse(json);
            return parsed.ToString(Formatting.Indented);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// 生成可视化HTML：图片为背景，每个文字行用红框+序号标注
    /// 浏览器打开即可直观查看GeneralAccurateOCR返回的文字框分布
    /// </summary>
    private static string BuildVisualizationHtml(string imageBase64, int imgWidth, int imgHeight, JArray textDetections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='zh-CN'><head><meta charset='UTF-8'>");
        sb.AppendLine("<title>GeneralAccurateOCR 可视化报告</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin:20px; background:#f0f0f0; font-family: 'Microsoft YaHei', sans-serif; }");
        sb.AppendLine("  .container { position:relative; display:inline-block; margin:10px; }");
        sb.AppendLine("  .img { display:block; border:1px solid #333; }");
        sb.AppendLine("  .box { position:absolute; border:2px solid #ff3b30; box-sizing:border-box; }");
        sb.AppendLine("  .box .lbl { position:absolute; top:-18px; left:-2px; background:#ff3b30; color:#fff; font-size:11px; padding:1px 4px; white-space:nowrap; border-radius:2px; }");
        sb.AppendLine("  .box .conf { position:absolute; bottom:-16px; right:-2px; background:rgba(0,0,0,0.6); color:#0f0; font-size:10px; padding:1px 3px; }");
        sb.AppendLine("  .info { margin:10px 0; padding:10px; background:#fff; border-radius:4px; }");
        sb.AppendLine("  table { border-collapse:collapse; width:100%; font-size:12px; background:#fff; }");
        sb.AppendLine("  th,td { border:1px solid #ddd; padding:4px 6px; text-align:left; }");
        sb.AppendLine("  th { background:#4a90d9; color:#fff; }");
        sb.AppendLine("  tr:nth-child(even){ background:#f9f9f9; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h2>GeneralAccurateOCR 可视化报告</h2>");
        sb.AppendLine($"<div class='info'>图片尺寸: {imgWidth} x {imgHeight} | 文字行数: {textDetections.Count}</div>");

        // 图片+框
        sb.AppendLine($"<div class='container'>");
        sb.AppendLine($"<img class='img' src='data:image/png;base64,{imageBase64}' width='{imgWidth}' height='{imgHeight}' />");
        for (int i = 0; i < textDetections.Count; i++)
        {
            var td = textDetections[i];
            var ip = td["ItemPolygon"];
            int x = ip?["X"]?.Value<int>() ?? 0;
            int y = ip?["Y"]?.Value<int>() ?? 0;
            int w = ip?["Width"]?.Value<int>() ?? 0;
            int h = ip?["Height"]?.Value<int>() ?? 0;
            int conf = td["Confidence"]?.Value<int>() ?? 0;
            string text = (td["DetectedText"]?.ToString() ?? "").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
            sb.AppendLine($"<div class='box' style='left:{x}px; top:{y}px; width:{w}px; height:{h}px;'>");
            sb.AppendLine($"<span class='lbl'>#{i + 1} {text}</span>");
            sb.AppendLine($"<span class='conf'>{conf}%</span>");
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</div>");

        // 明细表
        sb.AppendLine("<h3>文字行明细</h3>");
        sb.AppendLine("<table><tr><th>#</th><th>X</th><th>Y</th><th>W</th><th>H</th><th>Conf</th><th>DetectedText</th></tr>");
        for (int i = 0; i < textDetections.Count; i++)
        {
            var td = textDetections[i];
            var ip = td["ItemPolygon"];
            int x = ip?["X"]?.Value<int>() ?? 0;
            int y = ip?["Y"]?.Value<int>() ?? 0;
            int w = ip?["Width"]?.Value<int>() ?? 0;
            int h = ip?["Height"]?.Value<int>() ?? 0;
            int conf = td["Confidence"]?.Value<int>() ?? 0;
            string text = (td["DetectedText"]?.ToString() ?? "").Replace("<", "&lt;").Replace(">", "&gt;");
            sb.AppendLine($"<tr><td>{i + 1}</td><td>{x}</td><td>{y}</td><td>{w}</td><td>{h}</td><td>{conf}</td><td>{text}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
