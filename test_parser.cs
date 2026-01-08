using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string jsonPath = @"e:\Code\C#\CYZLZDH\OCR_JSON\OCR_江门市蓬江区龙华酒店有限公司_20260107_165849.json";
        string json = File.ReadAllText(jsonPath);
        var objs = JObject.Parse(json);

        Console.WriteLine("=== 分析JSON数据 ===\n");

        var allCells = new List<JObject>();

        if (objs["tables_result"] != null)
        {
            foreach (var table in objs["tables_result"].Children<JObject>())
            {
                if (table["header"] != null)
                {
                    foreach (var item in table["header"].Children<JObject>())
                    {
                        allCells.Add(item);
                    }
                }

                if (table["body"] != null)
                {
                    foreach (var item in table["body"].Children<JObject>())
                    {
                        allCells.Add(item);
                    }
                }
            }
        }

        var keyPositions = new Dictionary<string, (int row, int col)>();

        Console.WriteLine("=== 查找关键字位置 ===");
        foreach (var cell in allCells)
        {
            string text = CleanText(cell["words"]?.ToString() ?? "");
            if (string.IsNullOrEmpty(text))
                continue;

            int rowStart = cell["row_start"]?.Value<int>() ?? 0;
            int colStart = cell["col_start"]?.Value<int>() ?? 0;

            if (text == "额定载重里" || text == "额定载重量")
            {
                keyPositions["额定载重量"] = (rowStart, colStart);
                Console.WriteLine($"找到关键字: '{text}' 在 row={rowStart}, col={colStart}");
            }
            else if (text == "设备代码")
            {
                keyPositions["设备代码"] = (rowStart, colStart);
                Console.WriteLine($"找到关键字: '{text}' 在 row={rowStart}, col={colStart}");
            }
        }

        Console.WriteLine("\n=== 查找对应的值 ===");
        foreach (var cell in allCells)
        {
            string originalText = cell["words"]?.ToString() ?? "";
            string text = CleanText(originalText);
            if (string.IsNullOrEmpty(text))
                continue;

            int rowStart = cell["row_start"]?.Value<int>() ?? 0;
            int colStart = cell["col_start"]?.Value<int>() ?? 0;

            if (keyPositions.TryGetValue("额定载重量", out var loadPos))
            {
                if (rowStart == loadPos.row && colStart > loadPos.col)
                {
                    Console.WriteLine($"额定载重量值: row={rowStart}, col={colStart}");
                    Console.WriteLine($"  原始文本: '{originalText}'");
                    Console.WriteLine($"  清理后: '{text}'");
                    
                    var match = Regex.Match(text, @"(\d+)");
                    if (match.Success)
                    {
                        Console.WriteLine($"  正则匹配: '{match.Groups[1].Value}'");
                        Console.WriteLine($"  最终结果: '{match.Groups[1].Value}kg'");
                    }
                }
            }

            if (keyPositions.TryGetValue("设备代码", out var codePos))
            {
                if (rowStart == codePos.row && colStart > codePos.col)
                {
                    Console.WriteLine($"设备代码值: row={rowStart}, col={colStart}");
                    Console.WriteLine($"  原始文本: '{originalText}'");
                    Console.WriteLine($"  清理后: '{text}'");
                    Console.WriteLine($"  是否为空: {string.IsNullOrEmpty(text)}");
                }
            }
        }

        Console.WriteLine("\n=== 查找报告编号 ===");
        bool foundReportNo = false;
        foreach (var cell in allCells)
        {
            string text = CleanText(cell["words"]?.ToString() ?? "");
            if (text.Contains("报告编号"))
            {
                foundReportNo = true;
                Console.WriteLine($"找到'报告编号': '{text}'");
            }
        }
        if (!foundReportNo)
        {
            Console.WriteLine("JSON中没有'报告编号'字段");
        }
    }

    static string CleanText(string text)
    {
        return text.Replace("\n", "").Replace("\r", "").Trim();
    }
}
