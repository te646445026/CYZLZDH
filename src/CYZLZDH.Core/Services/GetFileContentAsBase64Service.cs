using System;
using System.IO;
using Microsoft.Extensions.Logging;
using CYZLZDH.Core.Interfaces;

namespace CYZLZDH.Core.Services;

public class GetFileContentAsBase64Service : IGetFileContentAsBase64Service
{
    private readonly ILogger<GetFileContentAsBase64Service> _logger;

    public GetFileContentAsBase64Service(ILogger<GetFileContentAsBase64Service> logger)
    {
        _logger = logger;
    }

    public string GetFileContentAsBase64(string path)
    {
        _logger.LogInformation("开始读取文件: {FilePath}", path);
        
        try
        {
            using (FileStream filestream = new FileStream(path, FileMode.Open))
            {
                byte[] arr = new byte[filestream.Length];
                filestream.Read(arr, 0, (int)filestream.Length);
                string base64 = Convert.ToBase64String(arr);
                
                _logger.LogDebug("文件读取成功，大小: {Size} 字节", arr.Length);
                return base64;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文件失败: {FilePath}", path);
            throw;
        }
    }
}
