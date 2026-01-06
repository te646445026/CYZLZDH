using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using CYZLZDH.Core.Models;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.Core.Services;

public class TencentOcrParser : IOcrParser
{
    private readonly ILogger<TencentOcrParser> _logger;

    public TencentOcrParser(ILogger<TencentOcrParser> logger)
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
            // 统一设置为监督检验
            result.JianyanOrjiance = "检验";

            bool isContain;
            int indexj, indexi;

            // 提取设备代码
            try
            {
                bool foundDeviceCode = false;
                
                // 方式1: 查找"设备代码"标签
                ObjsIndex("设备代码", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string labelCell = objs["Response"]["TableDetections"][indexj]["Cells"][indexi]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    
                    // 尝试获取右侧单元格的值
                    if (indexi + 1 < objs["Response"]["TableDetections"][indexj]["Cells"].Count())
                    {
                        string valueCell = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                            .ToString().Replace("\n", "").Replace("\r", "").Replace(" ", "");
                        
                        if (!string.IsNullOrEmpty(valueCell) && valueCell != ":")
                        {
                            result.DeviceCode = valueCell;
                            foundDeviceCode = true;
                        }
                    }
                    
                    // 如果右侧单元格为空，尝试从同一单元格提取
                    if (!foundDeviceCode)
                    {
                        // 处理"序号. 设备代码: 值"格式或"设备代码: 值"格式
                        var match = Regex.Match(labelCell, @"设备代码[:：]?\s*(.+)$");
                        if (match.Success)
                        {
                            string extractedValue = match.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(extractedValue))
                            {
                                result.DeviceCode = extractedValue;
                                foundDeviceCode = true;
                            }
                        }
                    }
                }
                
                // 方式2: 遍历所有单元格，查找包含"设备代码"的行
                if (!foundDeviceCode)
                {
                    var tableDetections = objs["Response"]["TableDetections"];
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 检查是否包含设备代码标签
                            if (cellText.Contains("设备代码"))
                            {
                                // 尝试提取冒号后的值
                                var match = Regex.Match(cellText, @"设备代码[:：]?\s*(.+)$");
                                if (match.Success)
                                {
                                    string value = match.Groups[1].Value.Trim();
                                    if (!string.IsNullOrEmpty(value) && value != ":")
                                    {
                                        result.DeviceCode = value;
                                        foundDeviceCode = true;
                                        break;
                                    }
                                }
                                
                                // 尝试从同一行相邻单元格获取值
                                if (cell.I + 1 < table.Table["Cells"].Count())
                                {
                                    string rightCell = table.Table["Cells"][cell.I + 1]["Text"].ToString()
                                        .Replace("\n", "").Replace("\r", "").Replace(" ", "");
                                    if (!string.IsNullOrEmpty(rightCell) && rightCell != ":")
                                    {
                                        result.DeviceCode = rightCell;
                                        foundDeviceCode = true;
                                        break;
                                    }
                                }
                            }
                            
                            // 查找纯设备代码（20位以上数字）
                            if (!foundDeviceCode && Regex.IsMatch(cellText, @"^\d{20,}$"))
                            {
                                result.DeviceCode = cellText;
                                foundDeviceCode = true;
                                break;
                            }
                            
                            // 查找类似设备代码格式（序号. 设备代码: 值）
                            if (!foundDeviceCode)
                            {
                                var match = Regex.Match(cellText, @"^\d+\.\s*设备代码[:：]?\s*(.+)$");
                                if (match.Success)
                                {
                                    string value = match.Groups[1].Value.Trim();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        result.DeviceCode = value;
                                        foundDeviceCode = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (foundDeviceCode) break;
                    }
                }
                
                // 提取设备代码中的数字部分
                if (foundDeviceCode && result.DeviceCode.Length > 20 && Regex.IsMatch(result.DeviceCode, @"\d{20,}"))
                {
                    Match match = Regex.Match(result.DeviceCode, @"\d{20,}");
                    if (match.Success)
                    {
                        result.DeviceCode = match.Value;
                    }
                }
                
                if (!foundDeviceCode)
                {
                    result.DeviceCode = "/";
                }
            }
            catch
            {
                result.DeviceCode = "/";
            }

            // 提取产品型号
            try
            {
                ObjsIndex("产品型号", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string modelText = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    
                    // 如果产品型号中包含其他文本，提取型号部分
                    if (modelText.Contains("产品型号") || modelText.Contains("型号"))
                    {
                        Match match = Regex.Match(modelText, @"[A-Za-z0-9\-\.]+");
                        if (match.Success && match.Value.Length > 2)
                        {
                            result.Model = match.Value;
                        }
                        else
                        {
                            result.Model = modelText.Replace("产品型号", "").Trim();
                        }
                    }
                    else
                    {
                        result.Model = modelText;
                    }
                }
            }
            catch
            {
                result.Model = "/";
            }

            // 提取设备品种
            try
            {
                ObjsIndex("设备品种", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string deviceTypeText = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    result.DeviceType = deviceTypeText;
                }
                else
                {
                    result.DeviceType = "/";
                }
            }
            catch
            {
                result.DeviceType = "/";
            }

            // 提取产品编号
            try
            {
                ObjsIndex("产品编号", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string serialNumText = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    
                    // 如果产品编号中包含其他文本，提取编号部分
                    if (serialNumText.Contains("产品编号") || serialNumText.Contains("编号"))
                    {
                        Match match = Regex.Match(serialNumText, @"[A-Za-z0-9\-]+");
                        if (match.Success && match.Value.Length > 1)
                        {
                            result.SerialNum = match.Value;
                        }
                        else
                        {
                            result.SerialNum = serialNumText.Replace("产品编号", "").Replace("编号", "").Trim();
                        }
                    }
                    else
                    {
                        result.SerialNum = serialNumText;
                    }
                }
            }
            catch
            {
                result.SerialNum = "/";
            }

            // 提取制造单位名称
            try
            {
                ObjsIndex("制造单位名称", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    result.ManufacturingUnit = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                }
            }
            catch
            {
                result.ManufacturingUnit = "/";
            }

            // 提取使用单位名称
            try
            {
                ObjsIndex("使用单位名称", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    result.UserName = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                }
            }
            catch
            {
                result.UserName = "/";
            }

            // 提取安装地点
            try
            {
                ObjsIndex("安装地点", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    result.UsingAddress = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                }
            }
            catch
            {
                result.UsingAddress = "/";
            }

            // 提取维护保养单位名称
            try
            {
                ObjsIndex("维护保养单位名称", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    result.MaintenanceUnit = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                }
            }
            catch
            {
                result.MaintenanceUnit = "/";
            }

            // 提取施工单位名称
            try
            {
                ObjsIndex("施工单位名称", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    result.ConstructionUnit = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                }
                else
                {
                    result.ConstructionUnit = "/";
                }
            }
            catch
            {
                result.ConstructionUnit = "/";
            }

            // 提取额定速度
            try
            {
                ObjsIndex("额定速度", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    result.Speed = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    var speedMatches = Regex.Matches(result.Speed, @"(\d+(\.\d+)?)");
                    if (speedMatches.Count > 0)
                    {
                        result.Speed = speedMatches[0].ToString();
                    }
                    else
                    {
                        result.Speed = "/";
                    }
                }
            }
            catch
            {
                result.Speed = "/";
            }

            // 提取额定载重量
            try
            {
                ObjsIndex("额定载重量", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string ratedLoadText = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    
                    if (ratedLoadText.Contains("额定速度") || ratedLoadText.Contains("kg") || ratedLoadText.Contains("载重量"))
                    {
                        Match match = Regex.Match(ratedLoadText, @"(\d+)");
                        if (match.Success)
                        {
                            result.RatedLoad = match.Value + "kg";
                        }
                        else
                        {
                            result.RatedLoad = ratedLoadText.Replace("额定速度", "").Replace("载重量", "").Trim();
                        }
                    }
                    else
                    {
                        result.RatedLoad = ratedLoadText;
                    }
                }
            }
            catch
            {
                result.RatedLoad = "/";
            }

            // 提取层站门数 - 增强版多种识别方式
            try
            {
                bool found = false;

                // 方式1: 精确匹配"层站门数"标签
                ObjsIndex("层站门数", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string layerText = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    result.LayerStationDoor = ParseLayerStationDoor(layerText, out var parsed1);
                    if (parsed1) found = true;
                }

                // 方式2: 遍历所有单元格，查找包含"层站门"的行
                if (!found)
                {
                    var tableDetections = objs["Response"]["TableDetections"];
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 检查是否包含层站门相关标签
                            if (cellText.Contains("层站门数") || 
                                cellText.Contains("层站门") || 
                                (cellText.Contains("层") && cellText.Contains("站") && cellText.Contains("门")))
                            {
                                result.LayerStationDoor = ParseLayerStationDoor(cellText, out var parsed2);
                                if (parsed2)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            
                            // 检查相邻单元格
                            if (cell.I + 1 < table.Table["Cells"].Count())
                            {
                                string rightCell = table.Table["Cells"][cell.I + 1]["Text"].ToString()
                                    .Replace("\n", "").Replace("\r", "");
                                
                                // 如果当前单元格是"层站门数"标签
                                if (cellText.Trim() == "层站门数" || cellText.Trim() == "层站门")
                                {
                                    result.LayerStationDoor = ParseLayerStationDoor(rightCell, out var parsed3);
                                    if (parsed3)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (found) break;
                    }
                }

                // 方式3: 查找"X层X站X门"格式（完整格式）
                if (!found)
                {
                    var tableDetections = objs["Response"]["TableDetections"];
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 匹配 "7层7站7门" 格式
                            var match = Regex.Match(cellText, @"(\d+)\s*层\s*(\d+)\s*站\s*(\d+)\s*门");
                            if (match.Success)
                            {
                                result.LayerStationDoor = $"{match.Groups[1].Value}层{match.Groups[2].Value}站{match.Groups[3].Value}门";
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                // 方式4: 查找"/"分隔格式（如"7/7/7"）
                if (!found)
                {
                    var tableDetections = objs["Response"]["TableDetections"];
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 匹配 "7/7/7" 格式
                            var match = Regex.Match(cellText, @"^(\d+)/(\d+)/(\d+)$");
                            if (match.Success)
                            {
                                result.LayerStationDoor = $"{match.Groups[1].Value}层{match.Groups[2].Value}站{match.Groups[3].Value}门";
                                found = true;
                                break;
                            }
                            
                            // 匹配 "7/7/7门" 或 "7层/7站/7门" 格式
                            match = Regex.Match(cellText, @"(\d+).*?(\d+).*?(\d+).*?门");
                            if (match.Success)
                            {
                                result.LayerStationDoor = $"{match.Groups[1].Value}层{match.Groups[2].Value}站{match.Groups[3].Value}门";
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                // 方式5: 查找连续三个数字（可能OCR识别丢失了单位）
                if (!found)
                {
                    var tableDetections = objs["Response"]["TableDetections"];
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 查找连续三个数字，可能用空格、连字符或其他字符分隔
                            var matches = Regex.Matches(cellText, @"(\d+)");
                            if (matches.Count >= 3)
                            {
                                // 检查这些数字是否在合理范围内（1-100）
                                int num1 = int.Parse(matches[0].Value);
                                int num2 = int.Parse(matches[1].Value);
                                int num3 = int.Parse(matches[2].Value);
                                
                                if (num1 <= 100 && num2 <= 100 && num3 <= 100)
                                {
                                    result.LayerStationDoor = $"{matches[0].Value}层{matches[1].Value}站{matches[2].Value}门";
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (found) break;
                    }
                }

                // 方式6: 查找"X层X门"格式（省略站数，与层数相同）
                if (!found)
                {
                    var tableDetections = objs["Response"]["TableDetections"];
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 匹配 "7层7门" 格式
                            var match = Regex.Match(cellText, @"(\d+)\s*层\s*(\d+)\s*门");
                            if (match.Success)
                            {
                                result.LayerStationDoor = $"{match.Groups[1].Value}层{match.Groups[1].Value}站{match.Groups[2].Value}门";
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                if (!found)
                {
                    result.LayerStationDoor = "/";
                }
            }
            catch
            {
                result.LayerStationDoor = "/";
            }

            // 提取报告编号
            try
            {
                bool foundReportNum = false;
                
                // 优先方式: 遍历所有单元格，查找"编号:RTJ-xxx"格式的真正报告编号
                var tableDetections = objs["Response"]["TableDetections"];
                foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                {
                    foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                    {
                        string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                        
                        // 优先查找"编号:RTJ-xxx"格式的真正报告编号（排除质量体系文件编号）
                        var reportMatch = Regex.Match(cellText, @"编号:\s*RT[JDN]\-([A-Za-z0-9\-]+)");
                        if (reportMatch.Success && !cellText.Contains("质量体系"))
                        {
                            string fullReportNum = reportMatch.Groups[1].Value;
                            // 提取RTJ-J后面的数字部分
                            var numMatch = Regex.Match(fullReportNum, @"J(\d+)");
                            if (numMatch.Success)
                            {
                                result.ReportNum = numMatch.Groups[1].Value;
                            }
                            else
                            {
                                result.ReportNum = fullReportNum;
                            }
                            foundReportNum = true;
                            break;
                        }
                    }
                    if (foundReportNum) break;
                }
                
                // 方式1: 查找"报告编号"标签
                if (!foundReportNum)
                {
                    ObjsIndex("报告编号", objs, out indexj, out indexi, out isContain);
                    if (isContain)
                    {
                        string labelCell = objs["Response"]["TableDetections"][indexj]["Cells"][indexi]["Text"]
                            .ToString().Replace("\n", "").Replace("\r", "");
                        
                        // 尝试获取右侧单元格的值
                        if (indexi + 1 < objs["Response"]["TableDetections"][indexj]["Cells"].Count())
                        {
                            string valueCell = objs["Response"]["TableDetections"][indexj]["Cells"][indexi + 1]["Text"]
                                .ToString().Replace("\n", "").Replace("\r", "").Replace(" ", "");
                            
                            // 排除无效值如"上次检验"
                            if (!string.IsNullOrEmpty(valueCell) && 
                                valueCell != ":" && 
                                valueCell != "上次检验" &&
                                !valueCell.Contains("检验"))
                            {
                                var match = Regex.Match(valueCell, @"[A-Za-z0-9\-]+");
                                if (match.Success && match.Value.Length >= 5)
                                {
                                    // 提取RTJ-J后面的数字部分
                                    var numMatch = Regex.Match(match.Value, @"J(\d+)");
                                    if (numMatch.Success)
                                    {
                                        result.ReportNum = numMatch.Groups[1].Value;
                                    }
                                    else
                                    {
                                        result.ReportNum = match.Value;
                                    }
                                    foundReportNum = true;
                                }
                            }
                        }
                        
                        // 如果右侧单元格为空或无效，尝试从同一单元格提取（排除"上次检验"）
                        if (!foundReportNum)
                        {
                            var match = Regex.Match(labelCell, @"报告编号[:：]?\s*(.+)$");
                            if (match.Success)
                            {
                                string extractedValue = match.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(extractedValue) && 
                                    extractedValue != "上次检验" &&
                                    !extractedValue.Contains("检验"))
                                {
                                    // 提取RTJ-J后面的数字部分
                                    var numMatch = Regex.Match(extractedValue, @"J(\d+)");
                                    if (numMatch.Success)
                                    {
                                        result.ReportNum = numMatch.Groups[1].Value;
                                    }
                                    else
                                    {
                                        result.ReportNum = extractedValue;
                                    }
                                    foundReportNum = true;
                                }
                            }
                        }
                    }
                }
                
                // 方式2: 遍历所有单元格，查找包含"编号:"前缀的真正报告编号
                if (!foundReportNum)
                {
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 查找"编号:RTJ-xxx"格式的真正报告编号
                            var reportMatch = Regex.Match(cellText, @"编号:\s*([A-Za-z0-9\-]+)");
                            if (reportMatch.Success && !cellText.Contains("质量体系"))
                            {
                                string fullReportNum = reportMatch.Groups[1].Value;
                                // 提取RTJ-J后面的数字部分
                                var numMatch = Regex.Match(fullReportNum, @"J(\d+)");
                                if (numMatch.Success)
                                {
                                    result.ReportNum = numMatch.Groups[1].Value;
                                }
                                else
                                {
                                    result.ReportNum = fullReportNum;
                                }
                                foundReportNum = true;
                                break;
                            }
                            
                            // 查找纯RTJ、RTD等格式的报告编号
                            if (!foundReportNum && Regex.IsMatch(cellText, @"^RT[JDN]\-[A-Za-z0-9\-]+$"))
                            {
                                var numMatch = Regex.Match(cellText, @"J(\d+)");
                                if (numMatch.Success)
                                {
                                    result.ReportNum = numMatch.Groups[1].Value;
                                }
                                else
                                {
                                    result.ReportNum = cellText;
                                }
                                foundReportNum = true;
                                break;
                            }
                            
                            // 查找单元格本身包含"报告"和"编号"，但值不是"上次检验"
                            if (!foundReportNum && cellText.Contains("报告") && cellText.Contains("编号"))
                            {
                                // 尝试提取冒号后的值
                                var match = Regex.Match(cellText, @"报告编号[:：]?\s*(.+)$");
                                if (match.Success)
                                {
                                    string value = match.Groups[1].Value.Trim();
                                    if (!string.IsNullOrEmpty(value) && 
                                        value != ":" && 
                                        value != "上次检验" &&
                                        !value.Contains("检验"))
                                    {
                                        result.ReportNum = value;
                                        foundReportNum = true;
                                        break;
                                    }
                                }
                                
                                // 尝试从同一行相邻单元格获取值
                                if (cell.I + 1 < table.Table["Cells"].Count())
                                {
                                    string rightCell = table.Table["Cells"][cell.I + 1]["Text"].ToString()
                                        .Replace("\n", "").Replace("\r", "").Replace(" ", "");
                                    if (!string.IsNullOrEmpty(rightCell) && 
                                        rightCell != ":" && 
                                        rightCell != "上次检验" &&
                                        !rightCell.Contains("检验"))
                                    {
                                        var rightMatch = Regex.Match(rightCell, @"[A-Za-z0-9\-]+");
                                        if (rightMatch.Success && rightMatch.Value.Length >= 5)
                                        {
                                            // 提取RTJ-J后面的数字部分
                                            var numMatch = Regex.Match(rightMatch.Value, @"J(\d+)");
                                            if (numMatch.Success)
                                            {
                                                result.ReportNum = numMatch.Groups[1].Value;
                                            }
                                            else
                                            {
                                                result.ReportNum = rightMatch.Value;
                                            }
                                            foundReportNum = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            // 如果单元格本身包含RTD/RTJ等前缀的编号
                            if (!foundReportNum && 
                                (cellText.Contains("RTD") || cellText.Contains("RTJ") || cellText.Contains("RTN")))
                            {
                                var match = Regex.Match(cellText, @"[A-Za-z0-9\-]+");
                                if (match.Success && match.Value.Length >= 5)
                                {
                                    // 提取RTJ-J后面的数字部分
                                    var numMatch = Regex.Match(match.Value, @"J(\d+)");
                                    if (numMatch.Success)
                                    {
                                        result.ReportNum = numMatch.Groups[1].Value;
                                    }
                                    else
                                    {
                                        result.ReportNum = match.Value;
                                    }
                                    foundReportNum = true;
                                    break;
                                }
                            }
                            
                            // 查找"序号. 报告编号: 值"格式
                            if (!foundReportNum)
                            {
                                var match = Regex.Match(cellText, @"^\d+\.\s*报告编号[:：]?\s*(.+)$");
                                if (match.Success)
                                {
                                    string value = match.Groups[1].Value.Trim();
                                    if (!string.IsNullOrEmpty(value) && 
                                        value != "上次检验" &&
                                        !value.Contains("检验"))
                                    {
                                        result.ReportNum = value;
                                        foundReportNum = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (foundReportNum) break;
                    }
                }
                
                // 方式3: 直接搜索表格中的所有单元格找报告编号模式
                if (!foundReportNum)
                {
                    foreach (var table in tableDetections.Select((t, j) => new { Table = t, J = j }))
                    {
                        foreach (var cell in table.Table["Cells"].Select((c, i) => new { Cell = c, I = i }))
                        {
                            string cellText = cell.Cell["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            
                            // 检查是否是报告编号单元格（日期格式的编号）
                            if (Regex.IsMatch(cellText, @"\d{4}[-]?\d{2}[-]?\d{2}[-]?[A-Z0-9]+") ||
                                Regex.IsMatch(cellText, @"[A-Z]{2,}[-]?\d{4,}"))
                            {
                                var match = Regex.Match(cellText, @"[A-Za-z0-9\-]+");
                                if (match.Success)
                                {
                                    // 提取RTJ-J后面的数字部分
                                    var numMatch = Regex.Match(match.Value, @"J(\d+)");
                                    if (numMatch.Success)
                                    {
                                        result.ReportNum = numMatch.Groups[1].Value;
                                    }
                                    else
                                    {
                                        result.ReportNum = match.Value;
                                    }
                                    foundReportNum = true;
                                    break;
                                }
                            }
                        }
                        if (foundReportNum) break;
                    }
                }
                
                if (!foundReportNum)
                {
                    result.ReportNum = "/";
                }
            }
            catch
            {
                result.ReportNum = "/";
            }

            // 提取日期信息
            try
            {
                ObjsIndex("安装监检日期", objs, out indexj, out indexi, out isContain);
                if (isContain)
                {
                    string cellText = objs["Response"]["TableDetections"][indexj]["Cells"][indexi]["Text"]
                        .ToString().Replace("\n", "").Replace("\r", "");
                    
                    var datePattern = @"\d{4}年\d{1,2}[\u4e00-\u9fa5]\d{0,}日|\d{4}年\d{1,2}[\u4e00-\u9fa5]";
                    var dateMatches = Regex.Matches(cellText, datePattern);
                    
                    if (dateMatches.Count > 0)
                    {
                        // 情况1：同一单元格包含关键字和日期
                        result.Date = dateMatches[0].Value;
                        var dateParts = Regex.Matches(result.Date, @"\d+");
                        if (dateParts.Count >= 3)
                        {
                            result.Date = dateParts[0].Value + "年" + dateParts[1].Value + "月" + dateParts[2].Value + "日";
                        }
                    }
                    else
                    {
                        // 情况2：关键字和日期分开在相邻单元格
                        // 尝试查找相邻单元格中的日期
                        var cells = objs["Response"]["TableDetections"][indexj]["Cells"];
                        string adjacentText = "";
                        
                        // 查找右侧相邻单元格
                        if (indexi + 1 < cells.Count())
                        {
                            adjacentText = cells[indexi + 1]["Text"].ToString().Replace("\n", "").Replace("\r", "");
                            dateMatches = Regex.Matches(adjacentText, datePattern);
                            if (dateMatches.Count > 0)
                            {
                                result.Date = dateMatches[0].Value;
                                var dateParts = Regex.Matches(result.Date, @"\d+");
                                if (dateParts.Count >= 3)
                                {
                                    result.Date = dateParts[0].Value + "年" + dateParts[1].Value + "月" + dateParts[2].Value + "日";
                                }
                            }
                        }
                        
                        // 如果右侧没找到，查找下方相邻单元格
                        if (dateMatches.Count == 0)
                        {
                            int nextRowStart = indexi;
                            var currentRow = int.Parse(cells[indexi]["RowIndex"].ToString());
                            
                            for (int i = indexi; i < cells.Count(); i++)
                            {
                                if (int.Parse(cells[i]["RowIndex"].ToString()) > currentRow)
                                {
                                    adjacentText = cells[i]["Text"].ToString().Replace("\n", "").Replace("\r", "");
                                    dateMatches = Regex.Matches(adjacentText, datePattern);
                                    if (dateMatches.Count > 0)
                                    {
                                        result.Date = dateMatches[0].Value;
                                        var dateParts = Regex.Matches(result.Date, @"\d+");
                                        if (dateParts.Count >= 3)
                                        {
                                            result.Date = dateParts[0].Value + "年" + dateParts[1].Value + "月" + dateParts[2].Value + "日";
                                        }
                                        break;
                                    }
                                    break;
                                }
                            }
                        }
                        
                        if (dateMatches.Count == 0)
                        {
                            result.Date = "   年   月   日";
                        }
                    }
                }
                else
                {
                    result.Date = "   年   月   日";
                }
            }
            catch
            {
                result.Date = "   年   月   日";
            }
        }
        catch (Exception ex)
        {
            // 如果解析失败，返回默认结果
            _logger.LogError(ex, "OCR解析错误");
        }

        _logger.LogInformation("OCR解析完成");
        return result;
    }

    private static string ParseLayerStationDoor(string input, out bool success)
    {
        success = false;
        if (string.IsNullOrWhiteSpace(input))
            return "/";

        string cleanedInput = input.Replace("\n", "").Replace("\r", "").Trim();

        // 匹配 "X层X站X门" 格式（如 "7层7站7门"）
        var match = Regex.Match(cleanedInput, @"(\d+)\s*层\s*(\d+)\s*站\s*(\d+)\s*门");
        if (match.Success)
        {
            success = true;
            return $"{match.Groups[1].Value}层{match.Groups[2].Value}站{match.Groups[3].Value}门";
        }

        // 匹配 "X/X/X" 格式（如 "7/7/7"）
        match = Regex.Match(cleanedInput, @"^(\d+)/(\d+)/(\d+)$");
        if (match.Success)
        {
            success = true;
            return $"{match.Groups[1].Value}层{match.Groups[2].Value}站{match.Groups[3].Value}门";
        }

        // 提取所有数字并组合
        var matches = Regex.Matches(cleanedInput, @"(\d+)");
        if (matches.Count >= 3)
        {
            int num1 = int.Parse(matches[0].Value);
            int num2 = int.Parse(matches[1].Value);
            int num3 = int.Parse(matches[2].Value);

            if (num1 <= 100 && num2 <= 100 && num3 <= 100)
            {
                success = true;
                return $"{matches[0].Value}层{matches[1].Value}站{matches[2].Value}门";
            }
        }
        else if (matches.Count == 2)
        {
            int num1 = int.Parse(matches[0].Value);
            int num2 = int.Parse(matches[1].Value);

            if (num1 <= 100 && num2 <= 100)
            {
                success = true;
                return $"{matches[0].Value}层{matches[0].Value}站{matches[1].Value}门";
            }
        }
        else if (matches.Count == 1)
        {
            int num1 = int.Parse(matches[0].Value);
            if (num1 <= 100)
            {
                success = true;
                return $"{matches[0].Value}层{matches[0].Value}站{matches[0].Value}门";
            }
        }

        return "/";
    }

    private static void ObjsIndex(string str, JObject objs, out int indexj, out int indexi, out bool isContain)
    {
        indexi = 0;
        indexj = 0;
        isContain = false;

        var tableDetections = objs["Response"]["TableDetections"];

        var result = tableDetections
            .Select((table, j) => new { Table = table, J = j })
            .SelectMany(x => x.Table["Cells"]
                .Select((cell, i) => new { Cell = cell, I = i, J = x.J }))
            .FirstOrDefault(x =>
            {
                string cellText = x.Cell["Text"].ToString();
                return cellText.Contains(str);
            });

        if (result != null)
        {
            indexi = result.I;
            indexj = result.J;
            isContain = true;
        }
    }
}