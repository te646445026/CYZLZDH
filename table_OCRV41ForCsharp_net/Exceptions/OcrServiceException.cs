using System;

namespace table_OCRV41ForCsharp_net.Exceptions
{
    /// <summary>
    /// OCR服务异常类
    /// </summary>
    public class OcrServiceException : Exception
    {
        /// <summary>
        /// 使用指定的错误消息初始化异常
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public OcrServiceException(string message) : base(message) { }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化异常
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public OcrServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}