using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Services;
using CYZLZDH.Core.Services.Interfaces;

namespace CYZLZDH.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(logPath, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var services = new ServiceCollection();

            services.AddLogging(configure =>
            {
                configure.AddSerilog();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            services.AddSingleton<IKeyService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<KeyService>>();
                return new KeyService(logger);
            });

            services.AddSingleton<IGetFileContentAsBase64Service>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<GetFileContentAsBase64Service>>();
                return new GetFileContentAsBase64Service(logger);
            });

            // OCR 引擎切换：
            //   原：TencentOcrService + TencentOcrParser（依赖表格识别V3的Cells结构，存在漂移问题）
            //   新：ZoneOcrService + ZoneOcrParser（基于通用高精度OCR的坐标区域匹配，结构稳定）
            // 切换回旧引擎时，注释掉 ZoneOcr 区段，取消注释 TencentOcr 区段即可。
            //
            // // ===== TencentOcr（原引擎，保留作为兜底/对比） =====
            // services.AddSingleton<IOcrParser>(provider =>
            // {
            //     var logger = provider.GetRequiredService<ILogger<TencentOcrParser>>();
            //     return new TencentOcrParser(logger);
            // });
            // services.AddSingleton<IOcrService>(provider =>
            // {
            //     var keyService = provider.GetRequiredService<IKeyService>();
            //     var key = keyService.CheckKey();
            //     var ocrParser = provider.GetRequiredService<IOcrParser>();
            //     var logger = provider.GetRequiredService<ILogger<TencentOcrService>>();
            //     return new TencentOcrService(key.API_KEY, key.SECRET_KEY, ocrParser, logger);
            // });

            // ===== ZoneOcr（新引擎，基于区域坐标匹配） =====
            services.AddSingleton<IOcrParser>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ZoneOcrParser>>();
                return new ZoneOcrParser(logger);
            });

            services.AddSingleton<IOcrService>(provider =>
            {
                var keyService = provider.GetRequiredService<IKeyService>();
                var key = keyService.CheckKey();
                var ocrParser = provider.GetRequiredService<IOcrParser>();
                var logger = provider.GetRequiredService<ILogger<ZoneOcrService>>();
                return new ZoneOcrService(key.API_KEY, key.SECRET_KEY, ocrParser, logger);
            });

            services.AddSingleton<IWordService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<WordService>>();
                return new WordService(logger);
            });

            services.AddScoped<MainForm>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<MainForm>>();
                return new MainForm(
                    provider,
                    provider.GetRequiredService<IWordService>(),
                    provider.GetRequiredService<IOcrService>(),
                    provider.GetRequiredService<IGetFileContentAsBase64Service>(),
                    logger);
            });

            using var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Program");
            logger.LogInformation("应用程序启动");
            
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
            
            logger.LogInformation("应用程序退出");
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"程序启动失败:\n{ex.Message}\n\n详细信息:\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log.CloseAndFlush();
        }
    }
}
