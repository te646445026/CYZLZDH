using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using CYZLZDH.Core.Exceptions;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Models;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

public class BaiduOcrService : IOcrService
{
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _tokenUrl;
    private readonly string _ocrUrl;
    private static readonly HttpClient Client = new HttpClient();

    private readonly IOcrParser _ocrParser;
    private readonly ILogger<BaiduOcrService> _logger;
    private string _accessToken;
    private DateTime _tokenExpiry;

    public BaiduOcrService(string apiKey, string secretKey, IOcrParser ocrParser, ILogger<BaiduOcrService> logger)
    {
        _apiKey = apiKey;
        _secretKey = secretKey;
        _ocrParser = ocrParser;
        _logger = logger;
        _tokenUrl = "https://aip.baidubce.com/oauth/2.0/token";
        _ocrUrl = "https://aip.baidubce.com/rest/2.0/ocr/v1/table";
        _accessToken = string.Empty;
        _tokenExpiry = DateTime.MinValue;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiry)
        {
            return _accessToken;
        }

        _logger.LogInformation("正在获取百度OCR访问令牌");

        try
        {
            var parameters = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _apiKey),
                new KeyValuePair<string, string>("client_secret", _secretKey)
            });

            var response = await Client.PostAsync(_tokenUrl, parameters);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseContent);

            if (json["error"] != null)
            {
                var error = json["error_description"]?.ToString() ?? json["error"]?.ToString();
                _logger.LogError("获取访问令牌失败: {Error}", error);
                throw new OcrServiceException($"获取百度OCR访问令牌失败: {error}");
            }

            _accessToken = json["access_token"]?.ToString() ?? string.Empty;
            var expiresIn = json["expires_in"]?.Value<int>() ?? 3600;
            _tokenExpiry = DateTime.Now.AddSeconds(expiresIn - 300);

            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new OcrServiceException("获取访问令牌返回为空");
            }

            _logger.LogInformation("获取访问令牌成功");
            return _accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取百度OCR访问令牌时发生错误");
            throw new OcrServiceException("获取百度OCR访问令牌时发生错误", ex);
        }
    }

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
        _logger.LogInformation("开始调用百度云OCR服务");

        try
        {
            var token = GetAccessTokenAsync().GetAwaiter().GetResult();

            var body = new StringContent($"access_token={token}&image={Uri.EscapeDataString(imageBase64)}&detect_direction=true&column_layout=true&xml=False");
            body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            int maxRetries = 3;
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogDebug("尝试第 {RetryCount} 次OCR请求", retryCount + 1);
                    var response = Client.PostAsync(_ocrUrl, body).GetAwaiter().GetResult();
                    var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("OCR请求返回HTTP错误: {StatusCode}, 内容: {Content}",
                            response.StatusCode, responseContent);
                        throw new OcrServiceException($"OCR请求失败: HTTP {response.StatusCode}");
                    }

                    var json = JObject.Parse(responseContent);

                    if (json["error_code"] != null)
                    {
                        var errorCode = json["error_code"]?.ToString();
                        var errorMsg = json["error_msg"]?.ToString();
                        _logger.LogError("OCR API返回错误: {ErrorCode} - {ErrorMsg}", errorCode, errorMsg);
                        throw new OcrServiceException($"百度OCR API错误: {errorCode} - {errorMsg}");
                    }

                    _logger.LogInformation("OCR服务调用成功");

                    var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "DebugOutput");
                    try
                    {
                        Directory.CreateDirectory(debugPath);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                        var debugFile = Path.Combine(debugPath, $"BaiduOcr_Response_{timestamp}.json");
                        File.WriteAllText(debugFile, responseContent);
                        _logger.LogInformation("已保存百度OCR响应到: {File}", debugFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "保存百度OCR响应文件失败");
                    }

                    return responseContent;
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
                    System.Threading.Thread.Sleep(delayMs);
                }
            }

            _logger.LogError(lastException, "OCR服务调用失败，已达到最大重试次数");
            throw new OcrServiceException("OCR服务调用失败，已达到最大重试次数", lastException);
        }
        catch (OcrServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR服务调用过程中发生未知错误");
            throw new OcrServiceException("OCR服务调用过程中发生未知错误", ex);
        }
    }
}
