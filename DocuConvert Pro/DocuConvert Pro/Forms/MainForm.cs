using System;
using System.IO;
using DocuConvert_Pro.Services;
using System.ComponentModel;

namespace DocuConvert_Pro.Forms
{
    public partial class MainForm : Form
    {
        private readonly IPaddleOCRService _ocrService;
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private readonly IMDConvService _mdConvService;

        private BackgroundWorker? _backgroundWorker;
        private BackgroundWorker? _mdConversionWorker;
        private bool _isProcessing = false;
        private bool _isMdConverting = false;
        private bool _apiConfigured = false;

        public MainForm(IPaddleOCRService ocrService, ILogService logService, IConfigService configService, IMDConvService mdConvService)
        {
            _ocrService = ocrService;
            _logService = logService;
            _configService = configService;
            _mdConvService = mdConvService;

            InitializeComponent();
            InitializeBackgroundWorker();
            InitializeMdConversionWorker();
            SetupEventHandlers();
            InitializeLogging();
            CheckAndPromptConfiguration();
        }

        private void InitializeBackgroundWorker()
        {
            _backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        private void InitializeMdConversionWorker()
        {
            _mdConversionWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _mdConversionWorker.DoWork += MdConversionWorker_DoWork;
            _mdConversionWorker.ProgressChanged += MdConversionWorker_ProgressChanged;
            _mdConversionWorker.RunWorkerCompleted += MdConversionWorker_RunWorkerCompleted;
        }

        private void SetupEventHandlers()
        {
            // 日志服务事件
            _logService.LogMessage += OnLogMessageReceived;
            _logService.ErrorOccurred += OnLogErrorReceived;
        }

        private void InitializeLogging()
        {
            // 日志文本框属性已在Designer中设置，无需额外初始化
        }

        

         private void CheckAndPromptConfiguration()
        {
            if (string.IsNullOrEmpty(_configService.ApiEndpoint) || string.IsNullOrEmpty(_configService.ApiKey))
            {
                ShowConfigurationForm();
            }
            else if (!_apiConfigured)
            {
                // 只在第一次设置时调用，避免重复设置
                _ocrService.SetApiEndpoint(_configService.ApiEndpoint);
                _ocrService.SetApiKey(_configService.ApiKey);
                _apiConfigured = true;
            }
        }

        private void ShowConfigurationForm()
        {
            using var configForm = new ConfigForm(_configService.ApiEndpoint, _configService.ApiKey, _configService);

            if (configForm.ShowDialog() == DialogResult.OK)
            {
                // 配置已通过ConfigForm更新到ConfigService，这里只需要设置到OCR服务
                _ocrService.SetApiEndpoint(_configService.ApiEndpoint);
                _ocrService.SetApiKey(_configService.ApiKey);
                _apiConfigured = true;
                _logService.LogInfo("配置已更新并生效");
            }
            else
            {
                // 用户取消配置，如果仍缺少必要配置则退出
                if (string.IsNullOrEmpty(_configService.ApiEndpoint) || string.IsNullOrEmpty(_configService.ApiKey))
                {
                    MessageBox.Show("未配置API服务，程序将无法正常工作。", "配置未完成",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                }
            }
        }

        private void ShowMdConversionForm()
        {
            using var mdConversionForm = new MdConversionForm();
            var result = mdConversionForm.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrEmpty(mdConversionForm.SelectedFormat))
            {
                // 用户选择了转换格式，开始MD文件转换
                StartMdConversion(mdConversionForm.SelectedFormat);
            }
            else
            {
                // 用户取消选择，不进行转换
                _logService.LogInfo("取消MD文件转换");
            }
        }

        private void OnLogMessageReceived(object? sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AddLogMessage), message);
            }
            else
            {
                AddLogMessage(message);
            }
        }

        private void OnLogErrorReceived(object? sender, string errorMessage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowErrorMessage), errorMessage);
            }
            else
            {
                ShowErrorMessage(errorMessage);
            }
        }

        private void InputFileButton_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "支持的文件格式|*.pdf;*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.md;*.markdown|PDF文件|*.pdf|图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|Markdown文件|*.md;*.markdown|所有文件|*.*",
                Title = "选择要处理的文件",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                inputFileTextBox.Text = openFileDialog.FileName;
                _logService.LogInfo($"选择文件: {Path.GetFileName(openFileDialog.FileName)}");

                // 根据文件类型设置输出目录
                string? fileDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                string fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                string fileExtension = Path.GetExtension(openFileDialog.FileName).ToLowerInvariant();

                if (!string.IsNullOrEmpty(fileDirectory) && Directory.Exists(fileDirectory))
                {
                    if (fileExtension == ".md" || fileExtension == ".markdown")
                    {
                        // 如果是Markdown文件，输出目录设置为文件所在目录
                        outputDirTextBox.Text = fileDirectory;
                        _logService.LogInfo($"文件将转换为 Word 或 Html 文件");
                    }
                    else
                    {
                        // 对于PDF和图片文件，输出目录设置为文件所在目录下的同名子目录
                        string outputDirectory = Path.Combine(fileDirectory, fileName);
                        outputDirTextBox.Text = outputDirectory;
                        _logService.LogInfo($"文件将转换为 Markdown 文件");
                    }
                }
            }
        }

        private void OutputDirButton_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择输出目录",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                outputDirTextBox.Text = folderDialog.SelectedPath;
                _logService.LogInfo($"选择输出目录: {folderDialog.SelectedPath}");
            }
        }

        private void StartMdConversion(string targetFormat)
        {
            if (_isMdConverting)
            {
                MessageBox.Show("正在转换中，请稍候...", "转换中", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateMdInputs())
            {
                return;
            }

            _isMdConverting = true;
            SetMdConversionState(true);

            _mdConversionWorker?.RunWorkerAsync(new MdConversionParameters
            {
                InputFilePath = inputFileTextBox.Text,
                OutputDirectory = outputDirTextBox.Text,
                TargetFormat = targetFormat
            });
        }

        private bool ValidateMdInputs()
        {
            if (!ValidateBasicInputs())
                return false;

            // 检查是否为MD文件
            string extension = Path.GetExtension(inputFileTextBox.Text).ToLower();
            if (extension != ".md" && extension != ".markdown")
            {
                MessageBox.Show("请选择Markdown文件(.md或.markdown)", "文件格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateInputs()
        {
            return ValidateBasicInputs();
        }

        private bool ValidateBasicInputs()
        {
            if (string.IsNullOrEmpty(inputFileTextBox.Text))
            {
                MessageBox.Show("请选择要处理的文件", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!File.Exists(inputFileTextBox.Text))
            {
                MessageBox.Show("选择的文件不存在，请重新选择", "文件不存在", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(outputDirTextBox.Text))
            {
                MessageBox.Show("请选择输出目录", "输出错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Directory.Exists(outputDirTextBox.Text))
            {
                try
                {
                    Directory.CreateDirectory(outputDirTextBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法创建输出目录: {ex.Message}", "目录创建失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            if (_isProcessing)
            {
                // 取消处理
                _backgroundWorker?.CancelAsync();
                SetProcessingState(false);
            }
            else
            {
                // 开始处理
                if (ValidateInputs())
                {
                    // 检查是否为MD文件
                    string extension = Path.GetExtension(inputFileTextBox.Text).ToLower();
                    if (extension == ".md" || extension == ".markdown")
                    {
                        // 如果是MD文件，弹出转换选择窗体
                        ShowMdConversionForm();
                    }
                    else
                    {
                        // 其他文件类型，执行OCR处理
                        StartProcessing();
                    }
                }
            }
        }

        private async void StartProcessing()
        {
            if (_isProcessing)
            {
                ShowErrorMessage("正在处理中，请稍候...");
                return;
            }

            if (!ValidateInputs())
            {
                return;
            }

            // 检查配置是否为空
            if (string.IsNullOrEmpty(_configService.ApiEndpoint) || string.IsNullOrEmpty(_configService.ApiKey))
            {
                MessageBox.Show(
                    "API配置为空，请先配置API_URL和TOKEN。",
                    "API配置为空",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                ShowConfigurationForm();
                return;
            }

            // 简化的配置格式验证
            bool isValid = await _configService.ValidateApiConfiguration();

            if (!isValid)
            {
                MessageBox.Show(
                    "API配置格式不正确，请检查API_URL和TOKEN的格式。",
                    "配置格式错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                ShowConfigurationForm();
                return;
            }

            _isProcessing = true;
            SetProcessingState(true);

            _backgroundWorker?.RunWorkerAsync(new ProcessingParameters
            {
                InputFilePath = inputFileTextBox.Text,
                OutputDirectory = outputDirTextBox.Text
            });
        }

        private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (sender is not BackgroundWorker worker || e.Argument is not ProcessingParameters parameters)
            {
                e.Result = null;
                return;
            }

            try
            {
                _logService.LogInfo("开始处理...");
                worker.ReportProgress(10);

                var result = Task.Run(async () => await _ocrService.ProcessDocumentAsync(parameters.InputFilePath, parameters.OutputDirectory)).GetAwaiter().GetResult();

                worker.ReportProgress(90);

                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                e.Result = result;
                worker.ReportProgress(100);
            }
            catch (AggregateException aggEx)
            {
                var innerException = aggEx.GetBaseException();
                _logService.LogError($"处理错误: {innerException.Message}");
                e.Result = null;
            }
            catch (Exception ex)
            {
                _logService.LogError($"处理错误: {ex.Message}");
                e.Result = null;
            }
        }

        private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            // 使用Marquee样式进度条，不显示具体百分比
            statusLabel.Text = "处理中...";
        }

        private void CleanupTempFiles()
        {
            try
            {
                _logService.LogInfo("清理临时文件");

                // 获取所有可能的PaddleOCR-VL临时目录
                var tempDirs = Directory.GetDirectories(Path.GetTempPath(), "DocuConvert_Pro_*");

                foreach (var tempDir in tempDirs)
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch
                    {
                        // 忽略单个目录清理失败
                    }
                }
            }
            catch
            {
                // 忽略清理过程错误
            }
        }

        private string GetTimeInfo(TimeSpan? processingTime)
        {
            return processingTime.HasValue ? $"处理文件耗时: {processingTime.Value.TotalSeconds:F2} 秒" : "时间统计不可用";
        }

        private void BackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            _isProcessing = false;
            SetProcessingState(false);

            if (e.Cancelled)
            {
                _logService.LogInfo("处理已取消");
                statusLabel.Text = "已取消";
                CleanupTempFiles();
            }
            else if (e.Error != null)
            {
                _logService.LogError($"处理失败: {e.Error.Message}");
                statusLabel.Text = "失败";
                MessageBox.Show($"处理失败: {e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanupTempFiles();
            }
            else
            {
                // 解决类型冲突：窗口层有一个本地 ProcessingResult 类型，而服务层返回的是 DocuConvert_Pro.Services.ProcessingResult。
                // 兼容两种类型以正确显示输出路径与耗时。
                if (e.Result is ProcessingResult result)
                {
                    if (result.Success)
                    {
                        var timeInfo = GetTimeInfo(result.ProcessingTime);
                        var outputPath = result.OutputFilePath ?? "未知路径";
                        _logService.LogInfo($"处理完成({timeInfo})");
                        statusLabel.Text = "处理完成";
                        MessageBox.Show($"处理完成！\n{timeInfo}", "处理成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (result.IsFinalError)
                    {
                        statusLabel.Text = "处理终止";
                        MessageBox.Show($"处理过程中发生严重错误，转换程序已终止:\n{result.ErrorMessage}", "处理终止", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        CleanupTempFiles();
                    }
                    else if (result.IsAuthenticationError)
                    {
                        _logService.LogError($"API认证失败，状态码: {result.StatusCode}");
                        statusLabel.Text = "API认证失败";

                        var dialogResult = MessageBox.Show(
                            $"API认证失败（状态码: {result.StatusCode}），可能是API密钥无效或过期。\n\n是否立即修改API配置？",
                            "API认证失败",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (dialogResult == DialogResult.Yes)
                        {
                            ShowConfigurationForm();

                            var retryResult = MessageBox.Show(
                                "配置已更新，是否重新尝试处理文件？",
                                "重新处理",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (retryResult == DialogResult.Yes)
                            {
                                Task.Delay(500).ContinueWith(_ =>
                                {
                                    if (InvokeRequired)
                                    {
                                        Invoke(new Action(StartProcessing));
                                    }
                                    else
                                    {
                                        StartProcessing();
                                    }
                                });
                            }
                        }
                    }
                    else
                    {
                        _logService.LogError("处理完成但结果为空");
                        statusLabel.Text = "处理失败";
                    }
                }
                else if (e.Result is DocuConvert_Pro.Services.ProcessingResult svc)
                {
                    if (svc.Success)
                    {
                        var timeInfo = GetTimeInfo(svc.ProcessingTime);
                        var outputPath = svc.OutputFilePath ?? "未知路径";
                        _logService.LogInfo($"处理完成: {outputPath} ({timeInfo})");
                        statusLabel.Text = "处理完成";
                        MessageBox.Show($"文档处理完成！\n输出文件: {outputPath}\n{timeInfo}", "处理成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        _logService.LogError("处理失败（服务层结果）");
                        statusLabel.Text = "处理失败";
                    }
                }
                else
                {
                    _logService.LogError("处理完成但结果为空");
                    statusLabel.Text = "处理失败";
                }
            }
        }

        private void SetProcessingState(bool isProcessing)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetProcessingState), isProcessing);
                return;
            }

            processButton.Text = isProcessing ? "取消处理" : "开始处理";
            processButton.BackColor = isProcessing ? Color.OrangeRed : Color.DodgerBlue;
            statusProgressBar.Visible = isProcessing;
            statusProgressBar.Style = ProgressBarStyle.Marquee; // 始终使用Marquee样式

            inputFileButton.Enabled = !isProcessing;
            outputDirButton.Enabled = !isProcessing;
        }

        private void AddLogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}\r\n";

            logTextBox.AppendText(logEntry);

            // 限制日志长度
            if (logTextBox.Lines.Length > 1000)
            {
                var lines = logTextBox.Lines;
                var newLines = new string[500];
                Array.Copy(lines, lines.Length - 500, newLines, 0, 500);
                logTextBox.Lines = newLines;
            }

            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.ScrollToCaret();
        }

        private void ShowErrorMessage(string errorMessage)
        {
            AddLogMessage($"[错误] {errorMessage}");
            statusLabel.Text = $"错误: {errorMessage}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isProcessing)
            {
                var result = MessageBox.Show("处理正在进行中，确定要退出吗？", "确认退出",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (_isMdConverting)
            {
                var result = MessageBox.Show("Markdown 文件转换正在进行中，确定要退出吗？", "确认退出",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        private void MdConversionWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (sender is not BackgroundWorker worker || e.Argument is not MdConversionParameters parameters)
            {
                e.Result = null;
                return;
            }

            try
            {
                var startTime = parameters.StartTime;
                _logService.LogInfo($"开始转换 Markdown 到{parameters.TargetFormat.ToUpper()}...");
                worker.ReportProgress(10);

                // 调用MD转换服务
                var result = Task.Run(async () => await _mdConvService.ConvertMarkdownAsync(
                    parameters.InputFilePath,
                    parameters.OutputDirectory,
                    parameters.TargetFormat)).GetAwaiter().GetResult();

                worker.ReportProgress(90);

                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // 计算处理时间并返回服务层的 ProcessingResult
                var processingTime = DateTime.Now - startTime;
                var outputPath = AppUtility.GetOutputFilePath(parameters.InputFilePath, parameters.OutputDirectory, parameters.TargetFormat, _logService);
                e.Result = new DocuConvert_Pro.Services.ProcessingResult
                {
                    Success = result,
                    ProcessingTime = processingTime,
                    OutputFilePath = outputPath
                };
                worker.ReportProgress(100);
            }
            catch (Exception ex)
            {
                _logService.LogError($"转换错误: {ex.Message}");
                e.Result = null;
            }
        }

        private void MdConversionWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            statusLabel.Text = "Markdown 文件转换中...";
        }

        private void MdConversionWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            _isMdConverting = false;
            SetMdConversionState(false);

            if (e.Cancelled)
            {
                _logService.LogInfo("转换已取消");
                statusLabel.Text = "已取消";
            }
            else if (e.Error != null)
            {
                _logService.LogError($"转换失败: {e.Error.Message}");
                statusLabel.Text = "失败";
                MessageBox.Show($"转换失败: {e.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // 处理新的结果格式，包含处理时间
                var result = e.Result;
                if (result != null)
                {
                    // 使用动态类型访问结果属性
                    dynamic dynamicResult = result;
                    bool success = dynamicResult.Success;
                    TimeSpan processingTime = dynamicResult.ProcessingTime;

                    if (success)
                    {
                        var timeInfo = GetTimeInfo(processingTime);
                        _logService.LogInfo($"转换完成 ({timeInfo})");
                        statusLabel.Text = "完成";
                        MessageBox.Show($"转换完成！\n{timeInfo}", "转换成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        _logService.LogError("转换失败");
                        statusLabel.Text = "失败";
                    }
                }
                else
                {
                    _logService.LogError("转换失败");
                    statusLabel.Text = "失败";
                }
            }
        }

        private void SetMdConversionState(bool isConverting)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetMdConversionState), isConverting);
                return;
            }

            statusProgressBar.Visible = isConverting;
            statusProgressBar.Style = ProgressBarStyle.Marquee;

            inputFileButton.Enabled = !isConverting;
            outputDirButton.Enabled = !isConverting;
            processButton.Enabled = !isConverting;
        }

        public void SetInputFile(string filePath)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetInputFile), filePath);
                return;
            }

            inputFileTextBox.Text = filePath;
            _logService.LogInfo($"设置输入文件: {Path.GetFileName(filePath)}");
        }

        public void SetOutputDirectory(string directoryPath)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetOutputDirectory), directoryPath);
                return;
            }

            outputDirTextBox.Text = directoryPath;
            _logService.LogInfo($"设置输出目录: {directoryPath}");
        }

        public void AutoStartProcessing()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(AutoStartProcessing));
                return;
            }

            // 延迟一点时间，确保窗体完全加载
            var timer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            timer.Tick += (sender, e) =>
            {
                timer.Stop();
                if (ValidateInputs())
                {
                    // 检查是否为MD文件
                    string extension = Path.GetExtension(inputFileTextBox.Text).ToLower();
                    if (extension == ".md" || extension == ".markdown")
                    {
                        // 如果是MD文件，默认转换为HTML格式
                        StartMdConversion("html");
                    }
                    else
                    {
                        // 其他文件类型，执行OCR处理
                        StartProcessing();
                    }
                }
            };
            timer.Start();
        }

        private void Option_Click(object sender, EventArgs e)
        {
            ShowConfigurationForm();
        }
    }

    public class MdConversionParameters
    {
        public string InputFilePath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public string TargetFormat { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
    }

    public class ProcessingParameters
    {
        public string InputFilePath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
    }

}