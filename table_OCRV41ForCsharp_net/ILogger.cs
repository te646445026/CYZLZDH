using System;

namespace table_OCRV41ForCsharp_net
{
    /// <summary>
    /// 日志记录器接口
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息（可选）</param>
        void Log(LogLevel level, string message, Exception exception = null);
    }

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}