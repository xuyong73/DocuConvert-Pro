using System.Windows.Input;

namespace DocuConvertProRefactored.ViewModels
{
    public class MdConversionFormViewModel : ViewModelBase
    {
        private string _selectedFormat = "docx";
        private bool _isConverting = false;

        public string SelectedFormat
        {
            get => _selectedFormat;
            set => SetField(ref _selectedFormat, value);
        }

        public bool IsConverting
        {
            get => _isConverting;
            set => SetField(ref _isConverting, value);
        }

        public ICommand ConvertCommand { get; }
        public ICommand CancelCommand { get; }

        public MdConversionFormViewModel()
        {
            // 初始化命令
            ConvertCommand = new RelayCommand(Convert);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Convert()
        {
            IsConverting = true;
            // 触发转换事件，让View处理窗口关闭和DialogResult设置
            OnConvertRequested();
        }

        private void Cancel()
        {
            // 触发取消事件，让View处理窗口关闭和DialogResult设置
            OnCancelRequested();
        }

        // 定义事件，让View订阅并处理窗口关闭逻辑
        public event EventHandler? ConvertRequested;
        public event EventHandler? CancelRequested;

        // 触发事件的保护方法
        protected virtual void OnConvertRequested()
        {
            ConvertRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnCancelRequested()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}