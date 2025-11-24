using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DocuConvert_Pro.Forms;
using DocuConvert_Pro.Services;

namespace DocuConvert_Pro
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            var host = CreateHostBuilder().Build();
            var mainForm = host.Services.GetRequiredService<MainForm>();

            // 简化启动逻辑：只在有有效输入文件时才设置参数
            if (args.Length > 0)
            {
                mainForm.SetInputFile(args[0]);

                // 简化输出目录设置逻辑
                string outputDir = args.Length > 1 ? args[1] :
                    Path.GetDirectoryName(args[0]) ?? Environment.CurrentDirectory;

                mainForm.SetOutputDirectory(outputDir);
                mainForm.AutoStartProcessing();
            }

            Application.Run(mainForm);
        }

        static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 注册服务 - 使用合并后的服务类
                    services.AddSingleton<HttpClient>();
                    services.AddSingleton<ILogService, Services.LogService>();
                    services.AddSingleton<IConfigService, Services.ConfigService>();
                    services.AddSingleton<IPaddleOCRService, Services.PaddleOCRService>();
                    services.AddSingleton<IMDConvService, Services.MDConvServices>();

                    // 注册主窗体
                    services.AddSingleton<MainForm>(provider => new MainForm(
                        provider.GetRequiredService<IPaddleOCRService>(),
                        provider.GetRequiredService<ILogService>(),
                        provider.GetRequiredService<IConfigService>(),
                        provider.GetRequiredService<IMDConvService>()));
                });
        }
    }
}