using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using CYZLZDH.Core.Models;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

public class BaiduOcrParser : IOcrParser
{
    private readonly ILogger<BaiduOcrParser> _logger;

    public BaiduOcrParser(ILogger<BaiduOcrParser> logger)
    {
        _logger = logger;
    }

    public OcrResult Parse(string json)
    {
        _logger.LogInformation("开始解析OCR结果");
        
        var objs = JObject.Parse(json);
        var result = new OcrResult();

        try
        {
            result.JianyanOrjiance = "检验";

            if (!ParseBaiduResult(objs, result))
            {
                _logger.LogWarning("未能成功解析百度OCR结果，尝试备用解析方法");
                ParseBaiduResultFallback(objs, result);
            }

            _logger.LogInformation("OCR结果解析完成");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析OCR结果时发生错误");
            throw;
        }
    }

    private bool ParseBaiduResult(JObject objs, OcrResult result)
    {
        bool anyFieldParsed = false;

        if (objs["forms"] != null && objs["forms"].HasValues)
        {
            var forms = objs["forms"];
            foreach (var form in forms.Children<JObject>())
            {
                if (form["body"] != null && form["body"].HasValues)
                {
                    var body = form["body"];
                    foreach (var cell in body.Children<JObject>())
                    {
                        var text = cell["text"]?.ToString().Replace("\n", "").Replace("\r", "").Trim() ?? "";
                        var row = cell["row"]?.Value<int>() ?? -1;
                        var column = cell["column"]?.Value<int>() ?? -1;

                        if (string.IsNullOrEmpty(text))
                            continue;

                        anyFieldParsed |= ParseCellText(text, row, column, result);
                    }
                }
            }
        }

        if (!anyFieldParsed && objs["result"] != null && objs["result"].HasValues)
        {
            var resultObj = objs["result"];
            
            if (resultObj["table_header"] != null && resultObj["table_body"] != null)
            {
                var header = resultObj["table_header"];
                var body = resultObj["table_body"];
                
                var headerTexts = header.Select(h => h["text"]?.ToString() ?? "").ToList();
                var bodyRows = body.Select(row => row.Select(c => c["text"]?.ToString() ?? "").ToList()).ToList();

                for (int rowIdx = 0; rowIdx < bodyRows.Count; rowIdx++)
                {
                    var row = bodyRows[rowIdx];
                    for (int colIdx = 0; colIdx < row.Count && colIdx < headerTexts.Count; colIdx++)
                    {
                        var text = row[colIdx];
                        if (!string.IsNullOrEmpty(text))
                        {
                            anyFieldParsed |= ParseCellText(text, rowIdx, colIdx, result);
                        }
                    }
                }
            }
        }

        if (!anyFieldParsed)
        {
            anyFieldParsed = ParseBaiduResultFallback(objs, result);
        }

        return anyFieldParsed;
    }

    private bool ParseCellText(string text, int row, int column, OcrResult result)
    {
        bool parsed = false;

        if (string.IsNullOrEmpty(result.DeviceCode) && (text.Contains("设备代码") || text.Contains("设备编号") || text.Contains("编号")))
        {
            var match = Regex.Match(text, @"[:：\s]*(.+)$");
            if (match.Success)
            {
                var value = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(value) && value.Length <= 20)
                {
                    result.DeviceCode = value;
                    parsed = true;
                }
            }
        }

        if (string.IsNullOrEmpty(result.LayerStationDoor) && (text.Contains("层站门") || text.Contains("层站")))
        {
            var match = Regex.Match(text, @"[:：\s]*(.+)$");
            if (match.Success)
            {
                var value = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(value) && value.Length <= 10)
                {
                    result.LayerStationDoor = value;
                    parsed = true;
                }
            }
        }

        if (string.IsNullOrEmpty(result.Speed) && (text.Contains("速度") || text.Contains("额定速度")))
        {
            var match = Regex.Match(text, @"[:：\s]*([\d.]+)\s*m\/s");
            if (match.Success)
            {
                result.Speed = match.Groups[1].Value;
                parsed = true;
            }
        }

        if (string.IsNullOrEmpty(result.RatedLoad) && (text.Contains("载重") || text.Contains("载荷") || text.Contains("额定载重")))
        {
            var match = Regex.Match(text, @"[:：\s]*([\d.]+)\s*kg");
            if (match.Success)
            {
                result.RatedLoad = match.Groups[1].Value;
                parsed = true;
            }
        }

        return parsed;
    }

    private bool ParseBaiduResultFallback(JObject objs, OcrResult result)
    {
        bool parsed = false;

        var allText = objs.SelectToken("$..text")?.ToObject<string[]>();
        if (allText != null)
        {
            foreach (var text in allText)
            {
                if (string.IsNullOrEmpty(text))
                    continue;

                parsed |= ParseCellText(text, -1, -1, result);
            }
        }

        return parsed;
    }
}
