using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Spire.Doc;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Services;

public class WordService : IWordService
{
    private readonly ILogger<WordService> _logger;
    
    // 同时匹配半角 [] 和全角 【】
    private static readonly Regex MarkerRegex = new(@"\[(\d+)\]|【(\d+)】", RegexOptions.Compiled);

    public WordService(ILogger<WordService> logger)
    {
        _logger = logger;
    }

    public DocumentInfo LoadDocument(string filePath)
    {
        _logger.LogInformation("开始加载文档: {FilePath}", filePath);
        
        var docInfo = new DocumentInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        var extension = Path.GetExtension(filePath).ToLower();
        if (extension == ".docx" || extension == ".doc")
        {
            var document = new Document(filePath);
            docInfo.Title = GetDocumentTitle(document);
            docInfo.Markers = FindAllMarkers(document);
            document.Close();
            
            _logger.LogInformation("文档加载成功，找到 {MarkerCount} 个标记", docInfo.Markers.Count);
        }
        else
        {
            _logger.LogError("不支持的文档格式: {Extension}", extension);
            throw new NotSupportedException($"不支持的文档格式: {extension}。支持的格式：.docx, .doc");
        }

        return docInfo;
    }

    public List<MarkerInfo> FindMarkers(DocumentInfo doc)
    {
        return doc.Markers;
    }

    public void ReplaceMarker(DocumentInfo doc, string markerId, string value)
    {
        var replacements = new Dictionary<string, string> { { markerId, value } };
        ReplaceMarkers(doc, replacements);
    }

    public void ReplaceMarkers(DocumentInfo doc, Dictionary<string, string> replacements)
    {
        if (replacements == null || replacements.Count == 0)
        {
            _logger.LogWarning("替换标记列表为空");
            return;
        }

        _logger.LogInformation("开始替换文档标记，共 {Count} 个", replacements.Count);

        var document = new Document(doc.FilePath);

        // 特殊标记处理列表
        var specialMarkers = new HashSet<int> { 13, 14, 15, 16 };

        // 1. 先处理特殊标记（需要替换整个单元格内容）
        foreach (var markerId in new[] { 13, 14, 15, 16 })
        {
            // 构建标记字符串
            var halfWidth = $"[{markerId}]";
            var fullWidth = $"【{markerId}】";
            
            // 确定替换值和格式
            string replaceValue;
            string fontName;
            float fontSize = 12f; // 小四
            HorizontalAlignment alignment;

            if (markerId == 13)
            {
                replaceValue = "符合要求";
                fontName = "宋体";
                alignment = HorizontalAlignment.Left;
            }
            else
            {
                replaceValue = "/";
                fontName = "Times New Roman";
                alignment = HorizontalAlignment.Center;
            }

            // 在文档中查找并替换包含这些标记的单元格
            ReplaceCellContent(document, halfWidth, replaceValue, fontName, fontSize, alignment);
            ReplaceCellContent(document, fullWidth, replaceValue, fontName, fontSize, alignment);
        }

        // 2. 处理常规标记
        foreach (var kvp in replacements)
        {
            try
            {
                var markerNum = ExtractMarkerNumber(kvp.Key);
                
                // 跳过特殊标记，因为已经处理过了
                if (specialMarkers.Contains(markerNum)) continue;

                var value = kvp.Value ?? string.Empty;

                // 尝试替换半角格式 [1]
                var halfWidthPattern = $"[{markerNum}]";
                document.Replace(halfWidthPattern, value, false, false);

                // 尝试替换全角格式 【1】
                var fullWidthPattern = $"【{markerNum}】";
                document.Replace(fullWidthPattern, value, false, false);
                
                _logger.LogDebug("替换标记 {MarkerId} 为 {Value}", kvp.Key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "替换标记 {MarkerId} 失败", kvp.Key);
            }
        }

        document.SaveToFile(doc.FilePath);
        document.Close();
        
        _logger.LogInformation("文档标记替换完成: {FilePath}", doc.FilePath);
    }

    private void ReplaceCellContent(Document document, string marker, string newValue, string fontName, float fontSize, HorizontalAlignment alignment)
    {
        foreach (Section section in document.Sections)
        {
            foreach (DocumentObject obj in section.Body.ChildObjects)
            {
                if (obj.DocumentObjectType == DocumentObjectType.Table)
                {
                    var table = (Table)obj;
                    foreach (TableRow row in table.Rows)
                    {
                        foreach (TableCell cell in row.Cells)
                        {
                            // 检查单元格文本是否包含标记
                            string cellText = GetCellText(cell);
                            if (cellText.Contains(marker))
                            {
                                // 清空单元格内容
                                cell.Paragraphs.Clear();
                                // 添加新段落和文本
                                var para = cell.AddParagraph();
                                var txtRange = para.AppendText(newValue);
                                
                                // 设置格式
                                txtRange.CharacterFormat.FontName = fontName;
                                txtRange.CharacterFormat.FontSize = fontSize;
                                para.Format.HorizontalAlignment = alignment;
                            }
                        }
                    }
                }
            }
        }
    }

    private string GetCellText(TableCell cell)
    {
        string text = "";
        foreach (Paragraph p in cell.Paragraphs)
        {
            text += p.Text;
        }
        return text;
    }

    public void SaveAs(DocumentInfo doc, string outputPath)
    {
        var document = new Document(doc.FilePath);
        document.SaveToFile(outputPath);
        document.Close();
    }

    public void ReplaceTitle(DocumentInfo doc, string newTitle)
    {
        var document = new Document(doc.FilePath);

        var firstSection = document.Sections.Count > 0 ? document.Sections[0] : null;
        if (firstSection != null)
        {
            var body = firstSection.Body;
            if (body.ChildObjects.Count > 0)
            {
                var firstObj = body.ChildObjects[0];
                if (firstObj.DocumentObjectType == DocumentObjectType.Paragraph)
                {
                    var firstPara = (Paragraph)firstObj;
                    var fullText = firstPara.Text;

                    // 使用正则匹配旧标题，例如 RTE-JC123456 或 BTE-JCxxxx
                    var regex = new Regex(@"(RTE|BTE)-JC\w*", RegexOptions.IgnoreCase);
                    var match = regex.Match(fullText);
                    
                    if (match.Success)
                    {
                        firstPara.Replace(match.Value, newTitle, false, false);
                    }
                    else if (fullText.Contains("RTE-JC") || fullText.Contains("BTE-JC")) 
                    {
                         firstPara.Text = newTitle;
                    }
                }
            }
        }

        document.SaveToFile(doc.FilePath);
        document.Close();
    }

    public void CropImages(DocumentInfo doc, float keepRatio)
    {
        var document = new Document(doc.FilePath);

        foreach (Section section in document.Sections)
        {
            // 处理 Body 中的图片
            CropImagesInContainer(section.Body, keepRatio);
        }

        document.SaveToFile(doc.FilePath);
        document.Close();
    }

    private void CropImagesInContainer(Body container, float keepRatio)
    {
        foreach (DocumentObject obj in container.ChildObjects)
        {
            if (obj.DocumentObjectType == DocumentObjectType.Paragraph)
            {
                CropImagesInParagraph((Paragraph)obj, keepRatio);
            }
            else if (obj.DocumentObjectType == DocumentObjectType.Table)
            {
                CropImagesInTable((Table)obj, keepRatio);
            }
        }
    }

    private void CropImagesInTable(Table table, float keepRatio)
    {
        foreach (TableRow row in table.Rows)
        {
            foreach (TableCell cell in row.Cells)
            {
                foreach (DocumentObject obj in cell.ChildObjects)
                {
                    if (obj.DocumentObjectType == DocumentObjectType.Paragraph)
                    {
                        CropImagesInParagraph((Paragraph)obj, keepRatio);
                    }
                }
            }
        }
    }

    private void CropImagesInParagraph(Paragraph paragraph, float keepRatio)
    {
        foreach (DocumentObject obj in paragraph.ChildObjects)
        {
            if (obj.DocumentObjectType == DocumentObjectType.Picture)
            {
                ProcessPicture((DocPicture)obj, keepRatio);
            }
        }
    }

    private void ProcessPicture(DocPicture pic, float keepRatio)
    {
        try
        {
            var originalImage = pic.Image;
            if (originalImage == null) return;

            using (var bitmap = new Bitmap(originalImage))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                
                // 简单的过滤：如果图片太小，可能不是波形图，跳过
                if (height < 100) return; 

                int targetHeight = (int)(height * keepRatio);

                if (targetHeight <= 0 || targetHeight >= height) return;

                var target = new Bitmap(width, targetHeight);

                using (var g = Graphics.FromImage(target))
                {
                    g.DrawImage(bitmap, new Rectangle(0, 0, target.Width, target.Height),
                                new Rectangle(0, 0, width, targetHeight),
                                GraphicsUnit.Pixel);
                }

                // 记录原图在文档中的显示宽度
                float displayWidth = pic.Width;
                float displayHeight = pic.Height;

                // 替换图片
                pic.LoadImage(target);

                // 调整显示大小
                pic.Width = displayWidth;
                pic.Height = displayHeight * keepRatio;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"裁剪图片失败: {ex.Message}");
        }
    }

    private string GetDocumentTitle(Document document)
    {
        if (document.Sections.Count > 0)
        {
            var firstSection = document.Sections[0];
            if (firstSection.Body.ChildObjects.Count > 0)
            {
                var firstObj = firstSection.Body.ChildObjects[0];
                if (firstObj.DocumentObjectType == DocumentObjectType.Paragraph)
                {
                    return ((Paragraph)firstObj).Text;
                }
            }
        }
        return string.Empty;
    }

    private List<MarkerInfo> FindAllMarkers(Document document)
    {
        var markers = new List<MarkerInfo>();
        int markerIndex = 1;

        for (int i = 0; i < document.Sections.Count; i++)
        {
            var section = document.Sections[i];
            FindMarkersInSection(section, markers, ref markerIndex);
        }

        return markers;
    }

    private void FindMarkersInSection(Section section, List<MarkerInfo> markers, ref int markerIndex)
    {
        foreach (DocumentObject obj in section.Body.ChildObjects)
        {
            if (obj.DocumentObjectType == DocumentObjectType.Paragraph)
            {
                FindMarkersInParagraph((Paragraph)obj, markers, ref markerIndex);
            }
            else if (obj.DocumentObjectType == DocumentObjectType.Table)
            {
                FindMarkersInTable((Table)obj, markers, ref markerIndex);
            }
        }
    }

    private void FindMarkersInParagraph(Paragraph paragraph, List<MarkerInfo> markers, ref int markerIndex)
    {
        foreach (DocumentObject obj in paragraph.ChildObjects)
        {
            if (obj.DocumentObjectType == DocumentObjectType.TextRange)
            {
                var textRange = (TextRange)obj;
                var matches = MarkerRegex.Matches(textRange.Text);
                foreach (Match match in matches)
                {
                    // 组1是半角匹配，组2是全角匹配
                    var id = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                    
                    markers.Add(new MarkerInfo
                    {
                        MarkerId = match.Value, // 保存原始标记，如 [1] 或 【1】
                        ParagraphIndex = markerIndex,
                        RunIndex = 0
                    });
                    markerIndex++;
                }
            }
        }
    }

    private void FindMarkersInTable(Table table, List<MarkerInfo> markers, ref int markerIndex)
    {
        foreach (TableRow row in table.Rows)
        {
            foreach (TableCell cell in row.Cells)
            {
                foreach (DocumentObject obj in cell.ChildObjects)
                {
                    if (obj.DocumentObjectType == DocumentObjectType.Paragraph)
                    {
                        FindMarkersInParagraph((Paragraph)obj, markers, ref markerIndex);
                    }
                }
            }
        }
    }

    private int ExtractMarkerNumber(string markerId)
    {
        // 移除半角和全角方括号
        var cleanId = markerId.Trim('[', ']', '【', '】');
        if (int.TryParse(cleanId, out int result))
        {
            return result;
        }
        // 如果解析失败，尝试用正则提取数字
        var match = Regex.Match(markerId, @"\d+");
        if (match.Success && int.TryParse(match.Value, out result))
        {
            return result;
        }
        
        throw new ArgumentException($"无效的标记符ID: {markerId}");
    }
}
