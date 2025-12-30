using System;
using System.IO;
using System.Threading;

namespace table_OCRV41ForCsharp_net
{
    /// <summary>
    /// Logger类的测试程序
    /// </summary>
    public class LoggerTest
    {
        public static void RunLoggerTest()
        {
            Console.WriteLine("开始测试Logger类...");
            
            // 创建日志文件路径
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string logFilePath = Path.Combine(logDirectory, $"test_log_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            
            Console.WriteLine($"日志将被写入: {logFilePath}");
            
            // 创建FileLogger实例
            using (ILogger logger = new FileLogger(logFilePath))
            {
                // 测试不同级别的日志
                logger.Log(LogLevel.Debug, "这是一条调试信息");
                logger.Log(LogLevel.Info, "这是一条普通信息");
                logger.Log(LogLevel.Warning, "这是一条警告信息");
                logger.Log(LogLevel.Error, "这是一条错误信息");
                
                // 测试带异常的日志
                try
                {
                    // 制造一个异常
                    int zero = 0;
                    int result = 10 / zero; // 将引发DivideByZeroException
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, "计算过程中发生异常", ex);
                }
                
                // 测试多线程环境下的日志记录
                Thread[] threads = new Thread[5];
                for (int i = 0; i < threads.Length; i++)
                {
                    int threadId = i;
                    threads[i] = new Thread(() =>
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            logger.Log(LogLevel.Info, $"线程 {threadId} 的消息 {j}");
                            Thread.Sleep(10); // 短暂延迟
                        }
                    });
                    threads[i].Start();
                }
                
                // 等待所有线程完成
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
            
            Console.WriteLine("日志测试完成，请检查日志文件内容。");
            Console.WriteLine($"日志文件位置: {logFilePath}");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}