using System;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Services;

public class KeyService : IKeyService
{
    private readonly ILogger<KeyService> _logger;

    public KeyService(ILogger<KeyService> logger)
    {
        _logger = logger;
    }

    public KEY CheckKey()
    {
        _logger.LogInformation("开始加载API密钥");
        
        KEY myKey = new KEY();
        string keyPath = System.Environment.CurrentDirectory + @"\key.json";
        
        if (!File.Exists(keyPath))
        {
            _logger.LogError("密钥文件不存在: {KeyPath}", keyPath);
            throw new FileNotFoundException($"密钥文件缺失: {keyPath}");
        }
        
        try
        {
            string keyJson = File.ReadAllText(keyPath);
            myKey = JsonConvert.DeserializeObject<KEY>(keyJson) ?? throw new InvalidOperationException("密钥文件格式错误");
            
            _logger.LogInformation("API密钥加载成功");
            return myKey;
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            _logger.LogError(ex, "读取密钥文件失败: {KeyPath}", keyPath);
            throw;
        }
    }

    public string GetProviderType()
    {
        _logger.LogInformation("正在获取OCR供应商类型");
        
        string keyPath = System.Environment.CurrentDirectory + @"\key.json";
        
        if (!File.Exists(keyPath))
        {
            _logger.LogWarning("密钥文件不存在，使用默认供应商: tencent");
            return "tencent";
        }
        
        try
        {
            string keyJson = File.ReadAllText(keyPath);
            var key = JsonConvert.DeserializeObject<KEY>(keyJson);
            
            var provider = key?.OCR_PROVIDER?.ToLowerInvariant() ?? "tencent";
            
            _logger.LogInformation("OCR供应商类型: {Provider}", provider);
            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取供应商配置失败，使用默认: tencent");
            return "tencent";
        }
    }
}
