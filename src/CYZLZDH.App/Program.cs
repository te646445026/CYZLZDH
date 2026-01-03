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

            services.AddSingleton<IOcrParser>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TencentOcrParser>>();
                return new TencentOcrParser(logger);
            });

            services.AddSingleton<IImagePreprocessor>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ImagePreprocessor>>();
                return new ImagePreprocessor(logger);
            });

            services.AddSingleton<IOcrService>(provider =>
            {
                var keyService = provider.GetRequiredService<IKeyService>();
                var key = keyService.CheckKey();
                var ocrParser = provider.GetRequiredService<IOcrParser>();
                var logger = provider.GetRequiredService<ILogger<TencentOcrService>>();
                var imagePreprocessor = provider.GetRequiredService<IImagePreprocessor>();
                return new TencentOcrService(key.API_KEY, key.SECRET_KEY, ocrParser, logger, imagePreprocessor, key.ENABLE_IMAGE_PREPROCESSING);
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
