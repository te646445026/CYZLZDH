using System;
using System.IO;

namespace table_OCRV41ForCsharp_net
{

    /// <summary>
    /// 文件日志记录实现类
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly StreamWriter _writer;
        private readonly object _lock = new object();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logFilePath">日志文件路径</param>
        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
            var logDir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDir) && !string.IsNullOrEmpty(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            _writer = new StreamWriter(logFilePath, true);
        }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息（可选）</param>
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            lock (_lock)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] [{level}] {message}";
                
                if (exception != null)
                {
                    logEntry += $"\r\nException: {exception.GetType().Name}: {exception.Message}";
                    logEntry += $"\r\nStackTrace: {exception.StackTrace}";
                }
                
                _writer.WriteLine(logEntry);
                _writer.Flush();
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}