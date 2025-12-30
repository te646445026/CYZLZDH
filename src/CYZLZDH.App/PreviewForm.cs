using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CYZLZDH.Core.Models;

namespace CYZLZDH.App;

public partial class PreviewForm : Form
{
    public PreviewForm(DocumentInfo originalDoc, DocumentInfo reportDoc, List<OCRField> ocrFields)
    {
        InitializeComponent();

        DisplayPreview(rtbOriginal, originalDoc, ocrFields);
        DisplayPreview(rtbReport, reportDoc, ocrFields);
    }

    private void DisplayPreview(RichTextBox rtb, DocumentInfo doc, List<OCRField> ocrFields)
    {
        rtb.Clear();
        
        // 标题
        rtb.SelectionFont = new Font("宋体", 14, FontStyle.Bold);
        rtb.SelectionColor = Color.DarkBlue;
        rtb.AppendText($"文档: {doc.FileName}\n");
        rtb.AppendText($"路径: {doc.FilePath}\n\n");

        // 字段映射标题
        rtb.SelectionFont = new Font("微软雅黑", 12, FontStyle.Bold);
        rtb.SelectionColor = Color.DarkGreen;
        rtb.AppendText("═══════════════════════════════════════════\n");
        rtb.AppendText("           OCR 字段映射预览\n");
        rtb.AppendText("═══════════════════════════════════════════\n\n");

        // 字段列表
        rtb.SelectionFont = new Font("微软雅黑", 10, FontStyle.Regular);
        rtb.SelectionColor = Color.Black;

        if (ocrFields != null && ocrFields.Any())
        {
            foreach (var field in ocrFields.OrderBy(f => f.Order))
            {
                var markerId = $"[{field.Order}]";
                var value = string.IsNullOrEmpty(field.Value) ? "(未识别)" : field.Value;
                
                // 标记ID
                rtb.SelectionColor = Color.Purple;
                rtb.SelectionFont = new Font("微软雅黑", 10, FontStyle.Bold);
                rtb.AppendText($"{markerId,-5}");
                
                // 字段名
                rtb.SelectionColor = Color.DarkBlue;
                rtb.SelectionFont = new Font("微软雅黑", 10, FontStyle.Regular);
                rtb.AppendText($"{field.Name,-15}");
                
                // 箭头
                rtb.SelectionColor = Color.Gray;
                rtb.AppendText(" → ");
                
                // 值
                rtb.SelectionColor = string.IsNullOrEmpty(field.Value) ? Color.Red : Color.DarkGreen;
                rtb.SelectionFont = new Font("微软雅黑", 10, FontStyle.Bold);
                rtb.AppendText($"{value}\n");
            }
        }
        else
        {
            rtb.SelectionColor = Color.Red;
            rtb.AppendText("没有OCR数据\n");
        }

        rtb.SelectionFont = new Font("微软雅黑", 10, FontStyle.Regular);
        rtb.SelectionColor = Color.Gray;
        rtb.AppendText($"\n\n总计 {ocrFields?.Count ?? 0} 个字段\n");
        rtb.AppendText("═══════════════════════════════════════════\n");
    }
}
