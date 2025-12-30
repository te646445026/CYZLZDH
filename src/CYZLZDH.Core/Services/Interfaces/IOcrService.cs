using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Interfaces;

public interface IOcrService
{
    string RecognizeTable(string imageBase64);
    
    /// <summary>
    /// 识别表格并解析结果
    /// </summary>
    /// <param name="imageBase64">Base64编码的图片数据</param>
    /// <returns>解析后的OCR结果</returns>
    OcrResult RecognizeTableAndParse(string imageBase64);
}
