using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CYZLZDH.Core.Models;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

/// <summary>
/// 区域OCR解析器：按 OcrZoneConfig.json 中预定义的坐标区域，
/// 从 GeneralAccurateOCR 返回的 TextDetections[] 中抽取字段值。
///
/// 相比依赖表格结构的 TencentOcrParser，本解析器仅依赖坐标匹配，
/// 不受 Cells 排列漂移、合并单元格识别偏差等问题影响。
/// </summary>
public class ZoneOcrParser : IOcrParser
{
    private readonly ILogger<ZoneOcrParser> _logger;
    private readonly OcrZoneConfig _config;
    private const string ConfigFileName = "OcrZoneConfig.json";

    public ZoneOcrParser(ILogger<ZoneOcrParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = LoadConfig();
    }

    /// <summary>
    /// 解析 GeneralAccurateOCR 返回的 JSON
    /// </summary>
    public OcrResult Parse(string json)
    {
        var result = new OcrResult { RawJsonResult = json };

        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning("ZoneOcrParser: 输入JSON为空");
            return result;
        }

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZoneOcrParser: JSON解析失败");
            return result;
        }

        var response = root["Response"];
        if (response == null)
        {
            _logger.LogWarning("ZoneOcrParser: Response节点缺失");
            return result;
        }

        // 检查API错误
        var error = response["Error"];
        if (error != null)
        {
            _logger.LogError("ZoneOcrParser: API返回错误 - Code={Code}, Message={Message}",
                error["Code"], error["Message"]);
            return result;
        }

        var textDetections = response["TextDetections"] as JArray;
        if (textDetections == null || textDetections.Count == 0)
        {
            _logger.LogWarning("ZoneOcrParser: TextDetections为空");
            return result;
        }

        _logger.LogDebug("ZoneOcrParser: 共 {Count} 行文字，开始按区域抽取字段", textDetections.Count);

        // 预解析所有文字行，提取坐标和文本
        var lines = new List<TextLine>(textDetections.Count);
        foreach (var td in textDetections)
        {
            var ip = td["ItemPolygon"];
            if (ip == null) continue;
            int x = ip["X"]?.Value<int>() ?? 0;
            int y = ip["Y"]?.Value<int>() ?? 0;
            int w = ip["Width"]?.Value<int>() ?? 0;
            int h = ip["Height"]?.Value<int>() ?? 0;
            string text = td["DetectedText"]?.ToString() ?? string.Empty;
            lines.Add(new TextLine
            {
                X = x,
                Y = y,
                Width = w,
                Height = h,
                CenterX = x + w / 2,
                CenterY = y + h / 2,
                Text = text
            });
        }

        // 遍历字段配置，按区域抽取
        foreach (var field in _config.Fields)
        {
            try
            {
                string value = ExtractField(lines, field);
                SetFieldValue(result, field.Field, value);
                _logger.LogDebug("ZoneOcrParser: 字段 {Field} = '{Value}'", field.Field, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZoneOcrParser: 抽取字段 {Field} 失败", field.Field);
            }
        }

        return result;
    }

    /// <summary>
    /// 从文字行中按区域抽取字段值
    /// </summary>
    private string ExtractField(List<TextLine> lines, FieldZone fieldZone)
    {
        if (fieldZone.Zones == null || fieldZone.Zones.Count == 0)
            return string.Empty;

        // 单zone：取该区域所有文字
        // 多zone：每个zone独立取文字，然后合并
        var zoneTexts = new List<ZoneText>(fieldZone.Zones.Count);
        foreach (var zone in fieldZone.Zones)
        {
            var matched = lines
                .Where(l => IsInZone(l.CenterX, l.CenterY, zone))
                .OrderBy(l => l.X)  // 同区域按X排序
                .ThenBy(l => l.Y)
                .Select(l => l.Text)
                .ToList();

            zoneTexts.Add(new ZoneText
            {
                Rect = zone,
                Text = string.Join("", matched)
            });
        }

        // 多zone合并：按 (X, Y) 排序后合并
        string combined;
        if (zoneTexts.Count == 1)
        {
            combined = zoneTexts[0].Text;
        }
        else
        {
            // combineSlash: 多zone用 / 拼接
            if (string.Equals(fieldZone.PostProcess, "combineSlash", StringComparison.OrdinalIgnoreCase))
            {
                var ordered = zoneTexts.OrderBy(z => z.Rect.X1).ThenBy(z => z.Rect.Y1).ToList();
                combined = string.Join("/", ordered.Select(z => z.Text));
            }
            else
            {
                // 默认：按 (X, Y) 排序后直接拼接
                var ordered = zoneTexts.OrderBy(z => z.Rect.X1).ThenBy(z => z.Rect.Y1).ToList();
                combined = string.Join("", ordered.Select(z => z.Text));
            }
        }

        // 后处理
        return PostProcess(combined, fieldZone);
    }

    /// <summary>
    /// 判断文字行中心点是否落在矩形区域内
    /// </summary>
    private static bool IsInZone(int centerX, int centerY, ZoneRect zone)
    {
        return centerX >= zone.X1 && centerX <= zone.X2
            && centerY >= zone.Y1 && centerY <= zone.Y2;
    }

    /// <summary>
    /// 字段值后处理
    /// </summary>
    private string PostProcess(string raw, FieldZone fieldZone)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        string value = raw.Trim();

        var mode = (fieldZone.PostProcess ?? "trim").ToLowerInvariant();
        switch (mode)
        {
            case "trim":
                // 仅去前后空白
                return value;

            case "extractdigits":
                // 提取所有数字字符
                return new string(value.Where(char.IsDigit).ToArray());

            case "extractnumberwithunit":
                // 合并后已是"数字+单位"形式，仅去内部多余空格
                return value.Replace(" ", "");

            case "combineslash":
                // 已在 ExtractField 中处理
                return value;

            case "removeprefix":
                if (!string.IsNullOrEmpty(fieldZone.RemovePrefix)
                    && value.StartsWith(fieldZone.RemovePrefix, StringComparison.Ordinal))
                {
                    return value.Substring(fieldZone.RemovePrefix.Length).Trim();
                }
                return value;

            default:
                _logger.LogWarning("ZoneOcrParser: 未知 postProcess 类型 {Mode}，按 trim 处理", mode);
                return value;
        }
    }

    /// <summary>
    /// 通过反射将字段值设置到 OcrResult 对应属性
    /// </summary>
    private void SetFieldValue(OcrResult result, string fieldName, string value)
    {
        if (string.IsNullOrEmpty(fieldName)) return;

        var prop = typeof(OcrResult).GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null && prop.PropertyType == typeof(string))
        {
            prop.SetValue(result, value ?? string.Empty);
        }
        else
        {
            _logger.LogWarning("ZoneOcrParser: OcrResult 未找到字符串属性 {Field}", fieldName);
        }
    }

    /// <summary>
    /// 加载区域配置文件（从工作目录读取，与 key.json 同位置）
    /// </summary>
    private OcrZoneConfig LoadConfig()
    {
        string path = Path.Combine(System.Environment.CurrentDirectory, ConfigFileName);

        if (!File.Exists(path))
        {
            _logger.LogError("ZoneOcrParser: 配置文件不存在 {Path}", path);
            throw new FileNotFoundException($"区域OCR配置文件缺失: {path}");
        }

        try
        {
            string json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<OcrZoneConfig>(json);
            if (config == null || config.Fields == null || config.Fields.Count == 0)
            {
                throw new InvalidOperationException("配置文件为空或字段列表为空");
            }
            _logger.LogInformation("ZoneOcrParser: 配置加载成功，共 {Count} 个字段定义", config.Fields.Count);
            return config;
        }
        catch (Exception ex) when (!(ex is FileNotFoundException) && !(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "ZoneOcrParser: 配置文件解析失败 {Path}", path);
            throw new InvalidOperationException($"区域OCR配置文件解析失败: {path}", ex);
        }
    }

    // 辅助结构
    private class TextLine
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    private class ZoneText
    {
        public ZoneRect Rect { get; set; } = new ZoneRect();
        public string Text { get; set; } = string.Empty;
    }
}
