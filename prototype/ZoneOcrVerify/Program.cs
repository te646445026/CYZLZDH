using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CYZLZDH.Core.Services;

namespace ZoneOcrVerify;

/// <summary>
/// 验证 ZoneOcrParser 的字段抽取是否正确。
/// 用法：ZoneOcrVerify <json文件路径>
/// 会读取工作目录下的 OcrZoneConfig.json，并解析指定的 OCR 返回 JSON。
/// </summary>
internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0)
            {
                Console.WriteLine("用法: ZoneOcrVerify <GeneralAccurateOCR返回的JSON文件路径>");
                return 1;
            }

            string jsonPath = args[0];
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"[错误] JSON文件不存在: {jsonPath}");
                return 1;
            }

            // 工作目录需能找到 OcrZoneConfig.json
            string configPath = Path.Combine(System.Environment.CurrentDirectory, "OcrZoneConfig.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[错误] 配置文件不存在: {configPath}");
                Console.WriteLine("请把 src/CYZLZDH.App/OcrZoneConfig.json 复制到本程序运行目录。");
                return 1;
            }

            string json = File.ReadAllText(jsonPath);

            // 用一个空 logger 构造 parser
            var logger = new EmptyLogger<ZoneOcrParser>();
            var parser = new ZoneOcrParser(logger);

            var result = parser.Parse(json);

            Console.WriteLine("=== ZoneOcrParser 解析结果 ===");
            Console.WriteLine($"原始JSON长度: {result.RawJsonResult.Length}");
            Console.WriteLine();

            // 反射打印所有 string 字段
            var props = typeof(CYZLZDH.Core.Models.OcrResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType == typeof(string) && p.Name != "RawJsonResult")
                .OrderBy(p => p.Name)
                .ToList();

            int filled = 0;
            foreach (var prop in props)
            {
                string value = (string)prop.GetValue(result) ?? "";
                string mark = string.IsNullOrEmpty(value) ? "  [空]" : "";
                if (!string.IsNullOrEmpty(value)) filled++;
                Console.WriteLine($"  {prop.Name,-22} = {value}{mark}");
            }

            Console.WriteLine();
            Console.WriteLine($"字段填充: {filled}/{props.Count}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[异常] {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 99;
        }
    }
}

/// <summary>
/// 空 ILogger 实现，仅用于测试
/// </summary>
internal class EmptyLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoopDisposable();
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId, TState state,
        System.Exception? exception, Func<TState, System.Exception?, string> formatter) { }

    private class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
