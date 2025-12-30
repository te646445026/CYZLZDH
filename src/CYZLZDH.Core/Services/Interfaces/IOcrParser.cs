using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Services.Interfaces;

public interface IOcrParser
{
    /// <summary>
    /// 解析OCR JSON结果
    /// </summary>
    /// <param name="json">OCR API返回的JSON字符串</param>
    /// <returns>解析后的结构化结果</returns>
    OcrResult Parse(string json);
}