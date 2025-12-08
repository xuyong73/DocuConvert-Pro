using System.Windows;
using System.Windows.Navigation;
using DocuConvertProRefactored.ViewModels;

namespace DocuConvertProRefactored.Views
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow(ConfigWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // 订阅ViewModel的事件
            viewModel.SaveRequested += OnSaveRequested;
            viewModel.CancelRequested += OnCancelRequested;
        }

        private void OnSaveRequested(object? sender, EventArgs e)
        {
            // 设置DialogResult为true并关闭窗口
            DialogResult = true;
            Close();
        }

        private void OnCancelRequested(object? sender, EventArgs e)
        {
            // 设置DialogResult为false并关闭窗口
            DialogResult = false;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // 打开浏览器访问指定URL
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}