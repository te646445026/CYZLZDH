using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Exceptions;
using CYZLZDH.Core.Models;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

public class TencentOcrService : IOcrService
{
    private readonly string _secretId;
    private readonly string _secretKey;
    private readonly string _service;
    private readonly string _version;
    private readonly string _action;
    private readonly string _region;
    private static readonly HttpClient Client = new HttpClient();

    private readonly IOcrParser _ocrParser;
    private readonly ILogger<TencentOcrService> _logger;

    public TencentOcrService(string secretId, string secretKey, IOcrParser ocrParser, ILogger<TencentOcrService> logger)
    {
        _secretId = secretId;
        _secretKey = secretKey;
        _ocrParser = ocrParser;
        _logger = logger;
        _service = "ocr";
        _version = "2018-11-19";
        _action = "RecognizeTableAccurateOCR";
        _region = "ap-guangzhou";
    }

    /// <summary>
    /// 识别表格并解析结果
    /// </summary>
    /// <param name="imageBase64">Base64编码的图片数据</param>
    /// <returns>解析后的OCR结果</returns>
    /// <exception cref="OcrServiceException">OCR服务调用失败时抛出</exception>
    public OcrResult RecognizeTableAndParse(string imageBase64)
    {
        _logger.LogInformation("开始OCR识别和解析");
        
        try
        {
            var jsonResult = RecognizeTable(imageBase64);
            var result = _ocrParser.Parse(jsonResult);
            
            _logger.LogInformation("OCR识别和解析成功");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR识别和解析过程中发生错误");
            throw new OcrServiceException("OCR识别和解析过程中发生错误", ex);
        }
    }

    public string RecognizeTable(string imageBase64)
    {
        _logger.LogInformation("开始调用腾讯云OCR服务");
        
        try
        {
            var body = imageBase64;
            var token = "";
            
            int maxRetries = 3;
            int retryCount = 0;
            Exception lastException = null;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogDebug("尝试第 {RetryCount} 次OCR请求", retryCount + 1);
                    var result = DoRequest(_secretId, _secretKey, _service, _version, _action, body, _region, token);
                    
                    _logger.LogInformation("OCR服务调用成功");
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    
                    _logger.LogWarning(ex, "OCR请求失败，第 {RetryCount} 次重试", retryCount);
                    
                    if (retryCount >= maxRetries)
                        break;
                        
                    int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                    _logger.LogDebug("等待 {DelayMs}ms 后重试", delayMs);
                    Thread.Sleep(delayMs);
                }
            }
            
            _logger.LogError("OCR服务调用失败，已重试 {MaxRetries} 次", maxRetries);
            throw new OcrServiceException($"OCR服务调用失败，已重试{maxRetries}次", lastException);
        }
        catch (OcrServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OcrServiceException("OCR服务调用过程中发生错误", ex);
        }
    }

    private string DoRequest(string secretId, string secretKey, string service, string version, string action, string body, string region, string token)
    {
        try
        {
            var request = BuildRequest(secretId, secretKey, service, version, action, body, region, token);
            var response = Client.SendAsync(request).Result;
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HTTP请求失败，状态码: {StatusCode}，原因: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                throw new OcrServiceException($"HTTP请求失败，状态码: {response.StatusCode}，原因: {response.ReasonPhrase}");
            }
            
            var result = response.Content.ReadAsStringAsync().Result;
            _logger.LogDebug("收到OCR响应，长度: {Length} 字符", result.Length);
            return result;
        }
        catch (OcrServiceException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP请求异常");
            throw new OcrServiceException("HTTP请求异常", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "请求超时");
            throw new OcrServiceException("请求超时", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行请求时发生未知错误");
            throw new OcrServiceException("执行请求时发生未知错误", ex);
        }
    }

    private HttpRequestMessage BuildRequest(string secretId, string secretKey, string service, string version, string action, string body, string region, string token)
    {
        var host = "ocr.tencentcloudapi.com";
        var url = "https://" + host;
        var contentType = "application/json; charset=utf-8";
        var timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();
        var auth = GetAuth(secretId, secretKey, host, contentType, timestamp, body);
        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.Headers.Add("Host", host);
        request.Headers.Add("X-TC-Timestamp", timestamp);
        request.Headers.Add("X-TC-Version", version);
        request.Headers.Add("X-TC-Action", action);
        request.Headers.Add("X-TC-Region", region);
        request.Headers.Add("X-TC-Token", token);
        request.Headers.Add("X-TC-RequestClient", "SDK_NET_BAREBONE");
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        request.RequestUri = new Uri(url);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return request;
    }

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
}
