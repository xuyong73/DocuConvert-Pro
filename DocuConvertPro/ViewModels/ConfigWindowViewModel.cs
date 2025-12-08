using System.Windows.Input;
using DocuConvertProRefactored.Services;

namespace DocuConvertProRefactored.ViewModels
{
    public class ConfigWindowViewModel : ViewModelBase
    {
        private readonly IConfigService _configService;
        private readonly ILogService _logService;

        private string _apiUrl = string.Empty;
        private string _token = string.Empty;

        public string ApiUrl
        {
            get => _apiUrl;
            set => SetField(ref _apiUrl, value);
        }

        public string Token
        {
            get => _token;
            set => SetField(ref _token, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ConfigWindowViewModel(IConfigService configService, ILogService logService)
        {
            _configService = configService;
            _logService = logService;

            // 加载现有配置
            _configService.LoadConfig();
            ApiUrl = _configService.ApiUrl;
            Token = _configService.Token;

            // 初始化命令
            SaveCommand = new RelayCommand(SaveConfig);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void SaveConfig()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ApiUrl))
                {
                    _logService.LogError("API URL不能为空");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Token))
                {
                    _logService.LogError("TOKEN不能为空");
                    return;
                }

                // 保存配置
                if (_configService.SaveConfig(ApiUrl, Token))
                {
                    _logService.LogInfo("配置已更新");
                    // 触发保存事件，让View处理窗口关闭
                    OnSaveRequested();
                }
                else
                {
                    _logService.LogError("配置保存失败");
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"保存配置时发生错误: {ex.Message}");
            }
        }

        private void Cancel()
        {
            // 触发取消事件，让View处理窗口关闭
            OnCancelRequested();
        }

        // 定义事件，让View订阅并处理窗口关闭逻辑
        public event EventHandler? SaveRequested;
        public event EventHandler? CancelRequested;

        // 触发事件的保护方法
        protected virtual void OnSaveRequested()
        {
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnCancelRequested()
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}