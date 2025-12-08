using System.Windows;
using DocuConvertProRefactored.ViewModels;

namespace DocuConvertProRefactored.Views
{
    /// <summary>
    /// Interaction logic for MdConversionForm.xaml
    /// </summary>
    public partial class MdConversionForm : Window
    {
        public string SelectedFormat => ((MdConversionFormViewModel)DataContext).SelectedFormat;

        public MdConversionForm(MdConversionFormViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // 订阅ViewModel的事件
            viewModel.ConvertRequested += OnConvertRequested;
            viewModel.CancelRequested += OnCancelRequested;
        }

        private void OnConvertRequested(object? sender, EventArgs e)
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
    }
}