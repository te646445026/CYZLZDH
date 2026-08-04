using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Exceptions;
using CYZLZDH.Core.Models;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

/// <summary>
/// 区域OCR服务：基于腾讯云通用文字识别（高精度版）GeneralAccurateOCR
/// 返回行级 TextDetections（带坐标），由 ZoneOcrParser 按预定义区域抽取字段。
/// 相比表格识别V3，本服务不依赖表格结构识别，结构稳定性更高。
/// </summary>
public class ZoneOcrService : IOcrService
{
    private readonly string _secretId;
    private readonly string _secretKey;
    private readonly IOcrParser _ocrParser;
    private readonly ILogger<ZoneOcrService> _logger;

    // 腾讯云公共参数（与 TencentOcrService 一致，仅 Action 不同）
    private const string Service = "ocr";
    private const string Version = "2018-11-19";
    private const string Action = "GeneralAccurateOCR"; // 通用文字识别（高精度版）
    private const string Region = "ap-guangzhou";
    private const string Host = "ocr.tencentcloudapi.com";

    private static readonly HttpClient Client = new HttpClient();

    public ZoneOcrService(string secretId, string secretKey, IOcrParser ocrParser, ILogger<ZoneOcrService> logger)
    {
        _secretId = secretId ?? throw new ArgumentNullException(nameof(secretId));
        _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        _ocrParser = ocrParser ?? throw new ArgumentNullException(nameof(ocrParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步识别并解析
    /// </summary>
    public async Task<OcrResult> RecognizeTableAndParseAsync(string imageBase64)
    {
        _logger.LogInformation("ZoneOcr: 开始识别与解析");
        try
        {
            var jsonResult = await RecognizeTableAsync(imageBase64).ConfigureAwait(false);
            var result = _ocrParser.Parse(jsonResult);

            // 兜底：层站门数三格存在空段（OCR 漏检小数字）时，裁剪该行放大后用 EnableDetectSplit 局部重试
            if (NeedLayerStationDoorRetry(result))
            {
                var retryTexts = TryRetryLayerStationDoor(imageBase64);
                if (retryTexts != null)
                {
                    _logger.LogInformation("ZoneOcr: 层站门数局部重试成功: {Old} -> {New}",
                        result.LayerStationDoor, string.Join("/", retryTexts));
                    result.LayerStationDoor = string.Join("/", retryTexts);
                }
            }

            _logger.LogInformation("ZoneOcr: 识别与解析成功");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZoneOcr: 识别与解析过程中发生错误");
            throw new OcrServiceException("ZoneOcr识别与解析过程中发生错误", ex);
        }
    }

    /// <summary>
    /// 同步识别并解析
    /// </summary>
    public OcrResult RecognizeTableAndParse(string imageBase64)
    {
        _logger.LogInformation("ZoneOcr: 开始识别与解析");
        try
        {
            var jsonResult = RecognizeTable(imageBase64);
            var result = _ocrParser.Parse(jsonResult);

            // 兜底：层站门数三格存在空段（OCR 漏检小数字）时，裁剪该行放大后用 EnableDetectSplit 局部重试
            if (NeedLayerStationDoorRetry(result))
            {
                var retryTexts = TryRetryLayerStationDoor(imageBase64);
                if (retryTexts != null)
                {
                    _logger.LogInformation("ZoneOcr: 层站门数局部重试成功: {Old} -> {New}",
                        result.LayerStationDoor, string.Join("/", retryTexts));
                    result.LayerStationDoor = string.Join("/", retryTexts);
                }
            }

            _logger.LogInformation("ZoneOcr: 识别与解析成功");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZoneOcr: 识别与解析过程中发生错误");
            throw new OcrServiceException("ZoneOcr识别与解析过程中发生错误", ex);
        }
    }

    /// <summary>
    /// 判断层站门数是否需要局部重试：三格中任意一格为空段（OCR 漏检）
    /// </summary>
    private static bool NeedLayerStationDoorRetry(OcrResult result)
    {
        var parts = (result.LayerStationDoor ?? string.Empty).Split('/');
        return parts.Length == 0 || parts.Any(p => string.IsNullOrWhiteSpace(p));
    }

    /// <summary>
    /// 层站门数局部裁剪重试：对原始图像中层站门数所在行
    /// （x 195-425, y 625-680，与 OcrZoneConfig 的 LayerStationDoor 行同坐标系）裁剪并放大 3 倍，
    /// 以 EnableDetectSplit=true 重新调用 OCR，返回三格文本（按原始图像坐标匹配区域）。
    /// 失败返回 null（保持原结果）。
    /// </summary>
    private string[] TryRetryLayerStationDoor(string imageBase64)
    {
        // 重要：ZoneOcrParser 并不对坐标做缩放——检测框坐标即"送入 OCR 的图像原始坐标"，
        // OcrZoneConfig 中的 zone 也是直接应用在这套原始坐标上（imageWidth/Height 仅作标注，未参与计算）。
        // 因此本重试裁剪也必须使用原始图像坐标，不能按 800x1100 归一化映射，
        // 否则裁剪框会落到错误的行（实测曾误框到额定速度/载重量那一行）。
        // 层站门数行在原始图像坐标中的范围（与 OcrZoneConfig 的 LayerStationDoor 行一致）
        const int zx1 = 195, zy1 = 625, zx2 = 425, zy2 = 680;
        // 三格区域（原始图像坐标）
        var zones = new[]
        {
            new { X1 = 205, X2 = 250 },
            new { X1 = 270, X2 = 325 },
            new { X1 = 345, X2 = 410 },
        };

        try
        {
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(imageBase64);
            }
            catch (FormatException)
            {
                _logger.LogWarning("ZoneOcr: 层站门数重试失败，图片Base64解码失败");
                return null;
            }

            using (var ms = new System.IO.MemoryStream(imageBytes))
            using (var bitmap = new System.Drawing.Bitmap(ms))
            {
                int imgW = bitmap.Width;
                int imgH = bitmap.Height;
                if (imgW <= 0 || imgH <= 0)
                {
                    _logger.LogWarning("ZoneOcr: 层站门数重试失败，图片尺寸无效 {W}x{H}", imgW, imgH);
                    return null;
                }

                // 直接使用原始图像坐标（与 ZoneOcrParser 坐标约定一致，不做归一化缩放）
                int sx1 = zx1;
                int sy1 = zy1;
                int sx2 = zx2;
                int sy2 = zy2;
                int cw = sx2 - sx1;
                int ch = sy2 - sy1;
                if (cw <= 0 || ch <= 0)
                {
                    _logger.LogWarning("ZoneOcr: 层站门数重试失败，裁剪区域无效");
                    return null;
                }

                // 裁剪 + 放大 3 倍
                const int scale = 3;
                using (var crop = new System.Drawing.Bitmap(cw, ch))
                {
                    using (var g = System.Drawing.Graphics.FromImage(crop))
                    {
                        g.DrawImage(bitmap,
                            new System.Drawing.Rectangle(0, 0, cw, ch),
                            new System.Drawing.Rectangle(sx1, sy1, cw, ch),
                            System.Drawing.GraphicsUnit.Pixel);
                    }

                    using (var scaled = new System.Drawing.Bitmap(crop, cw * scale, ch * scale))
                    using (var outMs = new System.IO.MemoryStream())
                    {
                        scaled.Save(outMs, System.Drawing.Imaging.ImageFormat.Png);
                        string cropBase64 = Convert.ToBase64String(outMs.ToArray());

                        // 带 EnableDetectSplit 局部重试
                        string retryJson = DoRequest(cropBase64, enableDetectSplit: true);
                        var root = Newtonsoft.Json.Linq.JObject.Parse(retryJson);
                        var response = root["Response"];
                        if (response == null) return null;
                        var tds = response["TextDetections"] as Newtonsoft.Json.Linq.JArray;
                        if (tds == null || tds.Count == 0) return null;

                        // 提取三格：把裁剪放大图坐标映射回原始图像坐标后按区域匹配
                        var texts = new string[3];
                        for (int i = 0; i < 3; i++) texts[i] = string.Empty;

                        foreach (var td in tds)
                        {
                            var ip = td["ItemPolygon"];
                            if (ip == null) continue;
                            int x = ip["X"]?.ToObject<int>() ?? 0;
                            int y = ip["Y"]?.ToObject<int>() ?? 0;
                            int w = ip["Width"]?.ToObject<int>() ?? 0;
                            int h = ip["Height"]?.ToObject<int>() ?? 0;
                            string text = td["DetectedText"]?.ToString() ?? string.Empty;
                            if (string.IsNullOrEmpty(text)) continue;

                        // 裁剪放大图坐标 -> 原图坐标（原始坐标，无需再转基准坐标，与 ZoneOcrParser 一致）
                        int origX = x / scale + sx1;
                        int origY = y / scale + sy1;
                        int centerX = origX + (w / scale) / 2;
                        int centerY = origY + (h / scale) / 2;

                            // 匹配三格
                            for (int i = 0; i < 3; i++)
                            {
                                if (centerX >= zones[i].X1 && centerX <= zones[i].X2
                                    && centerY >= zy1 && centerY <= zy2)
                                {
                                    texts[i] += text;
                                    break;
                                }
                            }
                        }

                        // 若三格均非空则视为成功
                        if (texts.All(t => !string.IsNullOrEmpty(t)))
                        {
                            return texts;
                        }
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ZoneOcr: 层站门数局部重试失败，保持原结果");
            return null;
        }
    }

    /// <summary>
    /// 异步调用 GeneralAccurateOCR（带3次指数退避重试）
    /// </summary>
    public async Task<string> RecognizeTableAsync(string imageBase64)
    {
        _logger.LogInformation("ZoneOcr: 开始调用腾讯云 GeneralAccurateOCR");

        int maxRetries = 3;
        int retryCount = 0;
        Exception lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogDebug("ZoneOcr: 第 {RetryCount} 次请求", retryCount + 1);
                var result = await DoRequestAsync(imageBase64).ConfigureAwait(false);
                _logger.LogInformation("ZoneOcr: 调用成功");
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                _logger.LogWarning(ex, "ZoneOcr: 第 {RetryCount} 次失败", retryCount);
                if (retryCount >= maxRetries) break;
                int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }

        throw new OcrServiceException($"ZoneOcr调用失败，已重试{maxRetries}次", lastException);
    }

    /// <summary>
    /// 同步调用 GeneralAccurateOCR（带3次指数退避重试）
    /// </summary>
    public string RecognizeTable(string imageBase64)
    {
        _logger.LogInformation("ZoneOcr: 开始调用腾讯云 GeneralAccurateOCR");

        int maxRetries = 3;
        int retryCount = 0;
        Exception lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogDebug("ZoneOcr: 第 {RetryCount} 次请求", retryCount + 1);
                var result = DoRequest(imageBase64);
                _logger.LogInformation("ZoneOcr: 调用成功");
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                _logger.LogWarning(ex, "ZoneOcr: 第 {RetryCount} 次失败", retryCount);
                if (retryCount >= maxRetries) break;
                int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                Thread.Sleep(delayMs);
            }
        }

        throw new OcrServiceException($"ZoneOcr调用失败，已重试{maxRetries}次", lastException);
    }

    private async Task<string> DoRequestAsync(string imageBase64, bool enableDetectSplit = false)
    {
        var request = BuildRequest(imageBase64, enableDetectSplit);
        var response = await Client.SendAsync(request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogError("ZoneOcr: HTTP {StatusCode} {ReasonPhrase}: {Body}", response.StatusCode, response.ReasonPhrase, errBody);
            throw new OcrServiceException($"HTTP请求失败，状态码: {response.StatusCode}，原因: {response.ReasonPhrase}");
        }

        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        _logger.LogDebug("ZoneOcr: 收到响应，长度 {Length} 字符", result.Length);
        return result;
    }

    private string DoRequest(string imageBase64, bool enableDetectSplit = false)
    {
        var request = BuildRequest(imageBase64, enableDetectSplit);
        var response = Client.SendAsync(request).Result;

        if (!response.IsSuccessStatusCode)
        {
            var errBody = response.Content.ReadAsStringAsync().Result;
            _logger.LogError("ZoneOcr: HTTP {StatusCode} {ReasonPhrase}: {Body}", response.StatusCode, response.ReasonPhrase, errBody);
            throw new OcrServiceException($"HTTP请求失败，状态码: {response.StatusCode}，原因: {response.ReasonPhrase}");
        }

        var result = response.Content.ReadAsStringAsync().Result;
        _logger.LogDebug("ZoneOcr: 收到响应，长度 {Length} 字符", result.Length);
        return result;
    }

    private HttpRequestMessage BuildRequest(string imageBase64, bool enableDetectSplit = false)
    {
        var contentType = "application/json; charset=utf-8";
        var timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        // 默认请求不带 EnableDetectSplit：该参数是整图级切分优化，会改变全局检测方式，
        // 实测会把 "1.75" 切成 "75"、把部分图片的日期切碎，破坏其他字段。
        // 仅在层站门数三格空段漏检时，对该行裁剪放大后用 EnableDetectSplit 局部重试（见 TryRetryLayerStationDoor）。
        var jsonBody = enableDetectSplit
            ? "{\"ImageBase64\":\"" + imageBase64 + "\",\"EnableDetectSplit\":true}"
            : "{\"ImageBase64\":\"" + imageBase64 + "\"}";
        var auth = GetAuth(_secretId, _secretKey, Host, contentType, timestamp, jsonBody);

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

    /// <summary>
    /// 腾讯云 TC3-HMAC-SHA256 签名算法（与 TencentOcrService 一致）
    /// </summary>
    private string GetAuth(string secretId, string secretKey, string host, string contentType, string timestamp, string body)
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
        var date = DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0), DateTimeKind.Utc)
                .AddSeconds(int.Parse(timestamp)).ToString("yyyy-MM-dd");
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
}
