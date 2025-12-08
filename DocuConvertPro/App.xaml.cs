using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DocuConvertProRefactored.Services;
using DocuConvertProRefactored.ViewModels;
using DocuConvertProRefactored.Views;

namespace DocuConvertProRefactored
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 配置依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 注册服务
            services.AddSingleton<HttpClient>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddSingleton<IPaddleOCRService, PaddleOCRService>();
            services.AddSingleton<IMDConvService, MDConvServices>();

            // 注册ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<ConfigWindowViewModel>();
            services.AddTransient<MdConversionFormViewModel>();
        }
    }
}