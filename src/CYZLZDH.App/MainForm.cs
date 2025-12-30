using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Models;

namespace CYZLZDH.App;

public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWordService _wordService;
    private readonly IOcrService _ocrService;
    private readonly IGetFileContentAsBase64Service _base64Service;

    private DocumentInfo? _originalDocInfo;
    private DocumentInfo? _reportDocInfo;
    private string _currentImagePath = string.Empty;
    private List<OCRField> _ocrFields = new();

    private readonly string[] FieldDescriptions = new[]
    {
        "报告编号", "使用单位", "施工单位", "检验日期",
        "设备使用地点", "设备品种", "型号", "制造单位",
        "额定载重量", "产品编号", "层站门数", "额定速度", "设备代码"
    };

    public MainForm(
        IServiceProvider serviceProvider,
        IWordService wordService,
        IOcrService ocrService,
        IGetFileContentAsBase64Service base64Service)
    {
        _serviceProvider = serviceProvider;
        _wordService = wordService;
        _ocrService = ocrService;
        _base64Service = base64Service;

        InitializeComponent();
    }

    private void BtnOriginalDoc_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "Word文档|*.docx;*.doc";
        dialog.Title = "选择原始记录文档";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var fileName = Path.GetFileName(dialog.FileName);
                if (!fileName.Contains("原始记录"))
                {
                    MessageBox.Show("请选择包含“原始记录”字样的文档！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                txtOriginalDoc.Text = dialog.FileName;
                _originalDocInfo = _wordService.LoadDocument(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文档失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnReportDoc_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "Word文档|*.docx;*.doc";
        dialog.Title = "选择测试报告文档";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var fileName = Path.GetFileName(dialog.FileName);
                if (!fileName.Contains("测试报告"))
                {
                    MessageBox.Show("请选择包含“测试报告”字样的文档！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                txtReportDoc.Text = dialog.FileName;
                _reportDocInfo = _wordService.LoadDocument(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文档失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnSelectImage_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
        dialog.Title = "选择图片";

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _currentImagePath = dialog.FileName;
            lblCurrentImage.Text = $"当前图片: {Path.GetFileName(dialog.FileName)}";
            btnRecognize.Enabled = true;
        }
    }

    private void BtnRecognize_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentImagePath))
        {
            MessageBox.Show("请先选择图片！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnRecognize.Enabled = false;
            btnRecognize.Text = "识别中...";

            var imageBase64 = _base64Service.GetFileContentAsBase64(_currentImagePath);
            
            var jsonResult = _ocrService.RecognizeTable(imageBase64);
            
            var imageDir = Path.GetDirectoryName(_currentImagePath);
            var jsonDir = Path.Combine(imageDir ?? ".", "OCR_JSON");
            if (!Directory.Exists(jsonDir))
            {
                Directory.CreateDirectory(jsonDir);
            }

            var imageName = Path.GetFileNameWithoutExtension(_currentImagePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var jsonFileName = $"OCR_{imageName}_{timestamp}.json";
            var jsonFilePath = Path.Combine(jsonDir, jsonFileName);
            File.WriteAllText(jsonFilePath, jsonResult);
            
            var ocrResult = _ocrService.RecognizeTableAndParse(imageBase64);

            // 自动提取报告编号后7位
            if (!string.IsNullOrEmpty(ocrResult.ReportNum))
            {
                var digits = new string(ocrResult.ReportNum.Where(char.IsDigit).ToArray());
                if (digits.Length > 0)
                {
                    ocrResult.ReportNum = digits.Length > 7 ? digits.Substring(digits.Length - 7) : digits;
                }
            }

            UpdateOcrFieldsFromResult(ocrResult);
            UpdateMappingList();
            txtOcrResult.ReadOnly = false;
            txtOcrResult.Text = FormatOcrResult();
            txtOcrResult.ReadOnly = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"OCR识别失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRecognize.Enabled = true;
            btnRecognize.Text = "开始识别";
        }
    }

    private void BtnProcess_Click(object? sender, EventArgs e)
    {
        if (_originalDocInfo == null || _reportDocInfo == null)
        {
            MessageBox.Show("请先加载文档！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (gridMapping.Rows.Count == 0)
        {
            MessageBox.Show("没有可用的映射数据！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            // 获取报告编号 (从第一行 [1] 获取)
            string reportNumber = "";
            if (gridMapping.Rows.Count > 0 && gridMapping.Rows[0].Cells[2].Value != null)
            {
                reportNumber = gridMapping.Rows[0].Cells[2].Value.ToString() ?? "";
            }

            if (string.IsNullOrEmpty(reportNumber))
            {
                MessageBox.Show("无法获取报告编号（标记 [1]），请检查映射数据。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 确保报告编号是7位
            reportNumber = reportNumber.PadLeft(7, '0');

            var replacements = new Dictionary<string, string>();

            // 构建替换字典
            foreach (DataGridViewRow row in gridMapping.Rows)
            {
                if (row.IsNewRow) continue;

                var markerId = row.Cells[0].Value?.ToString();
                var value = row.Cells[2].Value?.ToString();

                if (!string.IsNullOrEmpty(markerId) && value != null && markerId != "[13]")
                {
                    replacements[markerId] = value;
                }
            }

            // --- 重命名逻辑 ---
            
            // 原始记录文档重命名
            var originalDir = Path.GetDirectoryName(_originalDocInfo.FilePath);
            if (string.IsNullOrEmpty(originalDir)) originalDir = AppDomain.CurrentDomain.BaseDirectory;
            
            var newOriginalName = $"RTE-JC{reportNumber}{Path.GetExtension(_originalDocInfo.FilePath)}";
            var newOriginalPath = Path.Combine(originalDir, newOriginalName);

            if (!string.Equals(_originalDocInfo.FilePath, newOriginalPath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newOriginalPath))
                {
                    // 如果目标文件已存在，询问是否覆盖
                    var result = MessageBox.Show($"文件 {newOriginalName} 已存在，是否覆盖？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;
                    File.Delete(newOriginalPath);
                }
                
                File.Move(_originalDocInfo.FilePath, newOriginalPath);
                
                // 更新文档信息和UI
                _originalDocInfo = _wordService.LoadDocument(newOriginalPath);
                txtOriginalDoc.Text = newOriginalPath;
            }

            // 测试报告文档重命名
            var reportDir = Path.GetDirectoryName(_reportDocInfo.FilePath);
            if (string.IsNullOrEmpty(reportDir)) reportDir = AppDomain.CurrentDomain.BaseDirectory;

            var newReportName = $"BTE-JC{reportNumber}{Path.GetExtension(_reportDocInfo.FilePath)}";
            var newReportPath = Path.Combine(reportDir, newReportName);

            if (!string.Equals(_reportDocInfo.FilePath, newReportPath, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(newReportPath))
                {
                    var result = MessageBox.Show($"文件 {newReportName} 已存在，是否覆盖？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;
                    File.Delete(newReportPath);
                }

                File.Move(_reportDocInfo.FilePath, newReportPath);

                // 更新文档信息和UI
                _reportDocInfo = _wordService.LoadDocument(newReportPath);
                txtReportDoc.Text = newReportPath;
            }

            // --- 替换逻辑 ---

            // 批量替换并保存
            if (replacements.Count > 0)
            {
                _wordService.ReplaceMarkers(_originalDocInfo, replacements);
                _wordService.ReplaceMarkers(_reportDocInfo, replacements);
            }

            // 替换标题
            _wordService.ReplaceTitle(_originalDocInfo, $"RTE-JC{reportNumber}");
            _wordService.ReplaceTitle(_reportDocInfo, $"BTE-JC{reportNumber}");

            // 裁剪图片 (保留高度比例 535/727 ≈ 0.7359)
            float keepRatio = 535f / 727f;
            _wordService.CropImages(_originalDocInfo, keepRatio);
            _wordService.CropImages(_reportDocInfo, keepRatio);

            MessageBox.Show($"处理完成！\n文件已重命名并更新：\n{newOriginalPath}\n{newReportPath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"处理失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        // 清空文档路径
        txtOriginalDoc.Text = string.Empty;
        txtReportDoc.Text = string.Empty;
        _originalDocInfo = null;
        _reportDocInfo = null;

        // 清空图片和OCR结果
        _currentImagePath = string.Empty;
        lblCurrentImage.Text = "当前图片: 未选择";
        btnRecognize.Enabled = false;
        txtOcrResult.Text = string.Empty;
        _ocrFields.Clear();

        // 清空映射表
        gridMapping.Rows.Clear();

        MessageBox.Show("所有数据已清空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateMappingList()
    {
        if (_ocrFields.Count == 0) return;

        gridMapping.Rows.Clear();

        for (int i = 1; i <= 13; i++)
        {
            var marker = $"[{i}]";
            var fieldName = FieldDescriptions[i - 1];
            var value = i <= _ocrFields.Count ? _ocrFields[i - 1].Value : "";
            
            gridMapping.Rows.Add(marker, fieldName, value);
        }
    }

    private void UpdateOcrFieldsFromResult(OcrResult result)
    {
        _ocrFields.Clear();

        _ocrFields.Add(new OCRField { Name = "报告编号", Value = result.ReportNum, Order = 1 });
        _ocrFields.Add(new OCRField { Name = "使用单位", Value = result.UserName, Order = 2 });
        _ocrFields.Add(new OCRField { Name = "施工单位", Value = result.ConstructionUnit, Order = 3 });
        _ocrFields.Add(new OCRField { Name = "检验日期", Value = result.Date, Order = 4 });
        _ocrFields.Add(new OCRField { Name = "设备使用地点", Value = result.UsingAddress, Order = 5 });
        _ocrFields.Add(new OCRField { Name = "设备品种", Value = result.DeviceType, Order = 6 });
        _ocrFields.Add(new OCRField { Name = "型号", Value = result.Model, Order = 7 });
        _ocrFields.Add(new OCRField { Name = "制造单位", Value = result.ManufacturingUnit, Order = 8 });
        _ocrFields.Add(new OCRField { Name = "额定载重量", Value = result.RatedLoad, Order = 9 });
        _ocrFields.Add(new OCRField { Name = "产品编号", Value = result.SerialNum, Order = 10 });
        _ocrFields.Add(new OCRField { Name = "层站门数", Value = result.LayerStationDoor, Order = 11 });
        _ocrFields.Add(new OCRField { Name = "额定速度", Value = result.Speed, Order = 12 });
        _ocrFields.Add(new OCRField { Name = "设备代码", Value = result.DeviceCode, Order = 13 });
    }

    private string FormatOcrResult()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═════════════════════════════════════════════════════════════════════");
        sb.AppendLine("                          OCR 识别结果");
        sb.AppendLine("═════════════════════════════════════════════════════════════════════");
        sb.AppendLine();
        
        foreach (var field in _ocrFields.OrderBy(f => f.Order))
        {
            var value = string.IsNullOrEmpty(field.Value) ? "/" : field.Value;
            sb.AppendLine($"  {field.Order,2}. {field.Name,-12}: {value}");
        }
        
        sb.AppendLine();
        sb.AppendLine("═════════════════════════════════════════════════════════════════════");
        return sb.ToString();
    }
}

public class OCRField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Order { get; set; }
}
