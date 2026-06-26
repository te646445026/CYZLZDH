using System.Collections.Generic;
using Newtonsoft.Json;

namespace CYZLZDH.Core.Models;

/// <summary>
/// 区域OCR配置：定义图片版式与字段坐标映射关系
/// </summary>
public class OcrZoneConfig
{
    /// <summary>
    /// 基准图片宽度（用于坐标缩放校验）
    /// </summary>
    [JsonProperty("imageWidth")]
    public int ImageWidth { get; set; }

    /// <summary>
    /// 基准图片高度
    /// </summary>
    [JsonProperty("imageHeight")]
    public int ImageHeight { get; set; }

    /// <summary>
    /// 字段区域定义列表
    /// </summary>
    [JsonProperty("fields")]
    public List<FieldZone> Fields { get; set; } = new List<FieldZone>();
}

/// <summary>
/// 单个字段的区域定义
/// </summary>
public class FieldZone
{
    /// <summary>
    /// OcrResult 的属性名（如 UserName、DeviceType）
    /// </summary>
    [JsonProperty("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 字段中文标签（仅作注释，不参与解析）
    /// </summary>
    [JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 该字段对应的坐标区域列表。
    /// 单zone：直接取该区域文字。
    /// 多zone：按 (Y, X) 排序后拼接（用于"层/站/门"、"数字+单位"等被拆分的情况）。
    /// </summary>
    [JsonProperty("zones")]
    public List<ZoneRect> Zones { get; set; } = new List<ZoneRect>();

    /// <summary>
    /// 后处理类型：
    /// trim                  - 去前后空白（默认）
    /// extractNumberWithUnit - 提取数字+单位（如 3000kg、1.0m/s）
    /// extractDigits         - 仅提取数字
    /// combineSlash          - 多zone用 / 拼接（如层/站/门 9/8/8）
    /// removePrefix          - 去掉前缀（如 "编号:RTJ-xxx" → "RTJ-xxx"）
    /// </summary>
    [JsonProperty("postProcess")]
    public string PostProcess { get; set; } = "trim";

    /// <summary>
    /// 仅当 postProcess=removePrefix 时使用，指定要去掉的前缀（如 "编号:"）
    /// </summary>
    [JsonProperty("removePrefix")]
    public string RemovePrefix { get; set; } = string.Empty;
}

/// <summary>
/// 矩形区域（像素坐标，原点左上）
/// </summary>
public class ZoneRect
{
    [JsonProperty("x1")]
    public int X1 { get; set; }

    [JsonProperty("y1")]
    public int Y1 { get; set; }

    [JsonProperty("x2")]
    public int X2 { get; set; }

    [JsonProperty("y2")]
    public int Y2 { get; set; }
}
