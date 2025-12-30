using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
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

            var services = new ServiceCollection();

            services.AddSingleton<IKeyService>(provider =>
            {
                return new KeyService();
            });

            services.AddSingleton<IGetFileContentAsBase64Service>(provider =>
            {
                return new GetFileContentAsBase64Service();
            });

            services.AddSingleton<IOcrParser>(provider =>
            {
                return new TencentOcrParser();
            });

            services.AddSingleton<IOcrService>(provider =>
            {
                var keyService = provider.GetRequiredService<IKeyService>();
                var key = keyService.CheckKey();
                var ocrParser = provider.GetRequiredService<IOcrParser>();
                return new TencentOcrService(key.API_KEY, key.SECRET_KEY, ocrParser);
            });

            services.AddSingleton<IWordService>(provider =>
            {
                return new WordService();
            });

            services.AddScoped<MainForm>();

            using var serviceProvider = services.BuildServiceProvider();
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"程序启动失败:\n{ex.Message}\n\n详细信息:\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
