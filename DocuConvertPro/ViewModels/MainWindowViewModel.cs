using System.IO;
using System.Windows.Input;
using DocuConvertProRefactored.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace DocuConvertProRefactored.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IPaddleOCRService _ocrService;
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private readonly IMDConvService _mdConvService;
        private readonly IServiceProvider _serviceProvider;

        private string _inputFilePath = string.Empty;
        private string _outputDirectory = string.Empty;
        private bool _isProcessing = false;
        private bool _isMdConverting = false;
        private bool _apiConfigured = false;
        private int _progressValue = 0;
        private ObservableCollection<LogMessage> _logMessages = new ObservableCollection<LogMessage>();
        private CancellationTokenSource? _cts;

        public string InputFilePath
        {
            get => _inputFilePath;
            set 
            {
                if (SetField(ref _inputFilePath, value))
                {
                    OnPropertyChanged(nameof(CanStartProcessing));
                    OnPropertyChanged(nameof(CanStartMdConversion));
                    
                    // 根据文件类型设置输出目录
                    if (!string.IsNullOrEmpty(value))
                    {
                        string? fileDirectory = Path.GetDirectoryName(value);
                        if (!string.IsNullOrEmpty(fileDirectory) && Directory.Exists(fileDirectory))
                        {
                            // 使用Contains方法简化条件判断，更简洁易读
                            if (new[] { ".md", ".markdown" }.Contains(Path.GetExtension(value).ToLowerInvariant()))
                            {
                                // 如果是Markdown文件，输出目录设置为文件所在目录
                                OutputDirectory = fileDirectory;
                                _logService.LogInfo($"选择文件: {Path.GetFileName(value)}");
                                _logService.LogInfo("文件将转换为 Word 或 Html 文件");
                            }
                            else
                            {
                                // 对于PDF和图片文件，输出目录设置为文件所在目录下的同名子目录
                                OutputDirectory = Path.Combine(fileDirectory, Path.GetFileNameWithoutExtension(value));
                                _logService.LogInfo($"选择文件: {Path.GetFileName(value)}");
                                _logService.LogInfo("文件将转换为 Markdown 文件");
                            }
                        }
                    }
                }
            }
        }

        public string OutputDirectory
        {
            get => _outputDirectory;
            set 
            {
                if (SetField(ref _outputDirectory, value))
                {
                    OnPropertyChanged(nameof(CanStartProcessing));
                    OnPropertyChanged(nameof(CanStartMdConversion));
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set 
            {
                if (SetField(ref _isProcessing, value))
                {
                    OnPropertyChanged(nameof(CanStartProcessing));
                    OnPropertyChanged(nameof(ProcessingButtonText));
                }
            }
        }

        public bool IsMdConverting
        {
            get => _isMdConverting;
            set 
            {
                if (SetField(ref _isMdConverting, value))
                {
                    OnPropertyChanged(nameof(CanStartMdConversion));
                    OnPropertyChanged(nameof(ProcessingButtonText));
                }
            }
        }

        public bool ApiConfigured
        {
            get => _apiConfigured;
            set => SetField(ref _apiConfigured, value);
        }

        public int ProgressValue
        {
            get => _progressValue;
            set => SetField(ref _progressValue, value);
        }

        public ObservableCollection<LogMessage> LogMessages
        {
            get => _logMessages;
            set => SetField(ref _logMessages, value);
        }

        public bool CanStartProcessing => !IsProcessing && !string.IsNullOrEmpty(InputFilePath) && !string.IsNullOrEmpty(OutputDirectory);

        public bool CanStartMdConversion => !IsMdConverting && !string.IsNullOrEmpty(InputFilePath) && !string.IsNullOrEmpty(OutputDirectory);

        public string ProcessingButtonText => (IsProcessing || IsMdConverting) ? "取消处理" : "开始处理";

        public ICommand SelectInputFileCommand { get; }
        public ICommand SelectOutputDirectoryCommand { get; }
        public ICommand StartProcessingCommand { get; }
        public ICommand CancelProcessingCommand { get; }
        public ICommand StartMdToDocxCommand { get; }
        public ICommand StartMdToHtmlCommand { get; }
        public ICommand ShowMdConversionFormCommand { get; }
        public ICommand ShowConfigCommand { get; }

        public MainWindowViewModel(
            IPaddleOCRService ocrService,
            ILogService logService,
            IConfigService configService,
            IMDConvService mdConvService,
            IServiceProvider serviceProvider)
        {
            _ocrService = ocrService;
            _logService = logService;
            _configService = configService;
            _mdConvService = mdConvService;
            _serviceProvider = serviceProvider;

            // 初始化命令
            SelectInputFileCommand = new RelayCommand(SelectInputFile);
            SelectOutputDirectoryCommand = new RelayCommand(SelectOutputDirectory);
            StartProcessingCommand = new RelayCommand(StartProcessing);
            CancelProcessingCommand = new RelayCommand(CancelProcessing);
            StartMdToDocxCommand = new RelayCommand(() => StartMdConversion("docx"));
            StartMdToHtmlCommand = new RelayCommand(() => StartMdConversion("html"));
            ShowMdConversionFormCommand = new RelayCommand(ShowMdConversionForm);
            ShowConfigCommand = new RelayCommand(ShowConfig);

            // 订阅日志事件
            _logService.LogMessage += OnLogMessage;

            // 检查API配置
            CheckAndPromptConfiguration();
        }

        private void SelectInputFile()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "支持的文件格式|*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.md;*.markdown|PDF文件|*.pdf|图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|Markdown文件|*.md;*.markdown|所有文件|*.*",
                    Title = "选择要处理的文件",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    InputFilePath = openFileDialog.FileName;
                    // 输出目录和日志显示的逻辑已移到InputFilePath属性的setter中
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"选择文件时发生错误: {ex.Message}");
                _logService.LogError($"错误详情: {ex.StackTrace}");
            }
        }

        private void SelectOutputDirectory()
        {
            try
            {
                // 使用OpenFolderDialog来选择文件夹
                var dialog = new OpenFolderDialog
                {
                    Title = "选择输出目录"
                };

                // 设置初始目录
                if (!string.IsNullOrEmpty(OutputDirectory) && Directory.Exists(OutputDirectory))
                {
                    dialog.InitialDirectory = OutputDirectory;
                }

                if (dialog.ShowDialog() == true)
                {
                    // 获取选择的文件夹路径
                    string selectedPath = dialog.FolderName;
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        OutputDirectory = selectedPath;
                         _logService.LogInfo($"选择输出目录: {Path.GetFileName(selectedPath)}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"选择输出目录时发生错误: {ex.Message}");
                _logService.LogError($"错误详情: {ex.StackTrace}");
            }
        }

        private async void StartProcessing()
        {
            if (!CanStartProcessing)
                return;

            try
            {
                // 检查是否为MD文件
                // 使用Contains方法简化条件判断，更简洁易读
                if (new[] { ".md", ".markdown" }.Contains(Path.GetExtension(InputFilePath).ToLower()))
                {
                    // 如果是MD文件，弹出转换选择窗体
                    ShowMdConversionForm();
                }
                else
                {
                    // 其他文件类型，执行OCR处理
                    IsProcessing = true;
                    ProgressValue = 0;
                    _logService.LogInfo("开始处理文件");

                    // 创建CancellationTokenSource
                    _cts = new CancellationTokenSource();

                    // 确保输出目录存在
                    if (!Directory.Exists(OutputDirectory))
                    {
                        Directory.CreateDirectory(OutputDirectory);
                        _logService.LogInfo($"创建输出目录");
                    }

                    // 执行OCR处理，传递CancellationToken
                    var result = await _ocrService.ProcessDocumentAsync(InputFilePath, OutputDirectory, _cts.Token);

                    if (result.Success)
                    {
                        if (result.ProcessingTime.HasValue)
                        {
                            _logService.LogInfo($"处理时间: {GetTimeInfo(result.ProcessingTime.Value)}");
                        }
                    }
                    else
                    {
                        _logService.LogError($"处理失败: {result.ErrorMessage}");
                    }

                    ProgressValue = 100;
                }
            }
            catch (OperationCanceledException)
            {
                _logService.LogInfo("处理已取消");
            }
            catch (Exception ex)
            {
                _logService.LogError($"处理异常: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                // 清理CancellationTokenSource
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void StartMdConversion(string targetFormat)
        {
            if (!CanStartMdConversion)
                return;

            try
            {
                IsMdConverting = true;
                ProgressValue = 0;
                _logService.LogInfo($"开始转换为{targetFormat}");

                // 创建CancellationTokenSource
                _cts = new CancellationTokenSource();

                // 确保输出目录存在
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                    _logService.LogInfo($"创建输出目录");
                }

                // 执行Markdown转换，并记录处理时间
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var success = await _mdConvService.ConvertMarkdownAsync(InputFilePath, OutputDirectory, targetFormat);
                stopwatch.Stop();

                if (success)
                {
                    string outputFileName = $"{Path.GetFileNameWithoutExtension(InputFilePath)}.{targetFormat}".ToLowerInvariant();
                    _logService.LogInfo($"处理时间: {GetTimeInfo(stopwatch.Elapsed)}");
                }
                else
                {
                    _logService.LogError($"转换失败");
                }

                ProgressValue = 100;
            }
            catch (Exception ex)
            {
                _logService.LogError($"转换异常: {ex.Message}");
            }
            finally
            {
                IsMdConverting = false;
                // 清理CancellationTokenSource
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void ShowConfig()
        {
            try
            {
                // 创建配置窗口
                var configWindowViewModel = _serviceProvider.GetRequiredService<ConfigWindowViewModel>();
                var configWindow = new DocuConvertProRefactored.Views.ConfigWindow(configWindowViewModel);
                
                // 设置所有者窗口，确保窗口居中显示
                configWindow.Owner = App.Current.MainWindow;
                
                // 显示窗口并等待结果
                if (configWindow.ShowDialog() == true)
                {
                    // 配置已更新，重新检查API配置状态
                    CheckAndPromptConfiguration();
                }
            }
            catch (Exception ex)
            {
                _logService.LogInfo($"显示配置窗口失败: {ex.Message}");
            }
        }

        private void ShowMdConversionForm()
        {
            try
            {
                // 创建MD转换窗口
                var mdConversionFormViewModel = _serviceProvider.GetRequiredService<MdConversionFormViewModel>();
                var mdConversionForm = new DocuConvertProRefactored.Views.MdConversionForm(mdConversionFormViewModel);
                
                // 设置所有者窗口，确保窗口居中显示
                mdConversionForm.Owner = App.Current.MainWindow;
                
                // 显示窗口并等待结果
                if (mdConversionForm.ShowDialog() == true)
                {
                    // 根据用户选择的格式执行转换
                    string selectedFormat = mdConversionForm.SelectedFormat;
                    if (!string.IsNullOrEmpty(selectedFormat))
                    {
                        StartMdConversion(selectedFormat);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogInfo($"显示MD转换窗口失败: {ex.Message}");
            }
        }

        private void CheckAndPromptConfiguration()
        {
            try
            {
                // 加载配置
                _configService.LoadConfig();
                ApiConfigured = !string.IsNullOrEmpty(_configService.ApiUrl) && !string.IsNullOrEmpty(_configService.Token);
                
                if (!ApiConfigured)
                {
                    _logService.LogInfo("提示: API配置未完成，请在设置中配置");
                }
            }
            catch (Exception ex)
            {
                _logService.LogInfo($"配置检查异常: {ex.Message}");
                ApiConfigured = false;
            }
        }

        private string GetTimeInfo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMinutes > 1)
            {
                return $"{timeSpan.TotalMinutes:F1}分钟";
            }
            else if (timeSpan.TotalSeconds > 1)
            {
                return $"{timeSpan.TotalSeconds:F1}秒";
            }
            else
            {
                return $"{timeSpan.TotalMilliseconds:F0}毫秒";
            }
        }

        private void OnLogMessage(object? sender, LogMessage logMessage)
        {
            LogMessages.Add(logMessage);
        }

        private void CancelProcessing()
        {
            try
            {
                // 取消处理
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                
                // 更新状态
                IsProcessing = false;
                IsMdConverting = false;
                ProgressValue = 0;
                
                // 添加日志
                _logService.LogInfo("处理已取消");
            }
            catch (Exception ex)
            {
                _logService.LogInfo($"取消处理失败: {ex.Message}");
            }
        }
    }
}