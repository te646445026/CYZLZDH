using System;
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

    private async Task<string> DoRequestAsync(string imageBase64)
    {
        var request = BuildRequest(imageBase64);
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

    private string DoRequest(string imageBase64)
    {
        var request = BuildRequest(imageBase64);
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

    private HttpRequestMessage BuildRequest(string imageBase64)
    {
        var contentType = "application/json; charset=utf-8";
        var timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        var jsonBody = "{\"ImageBase64\":\"" + imageBase64 + "\"}";
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
