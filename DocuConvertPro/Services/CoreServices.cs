using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DocuConvertProRefactored.Services
{
    #region 接口定义

    public interface IConfigService
    {
        string ApiUrl { get; set; }
        string Token { get; set; }
        bool LoadConfig();
        bool SaveConfig(string apiUrl, string token);
    }

    public interface ILogService
    {
        event EventHandler<LogMessage> LogMessage;
        event EventHandler<string> ErrorOccurred;
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    public interface IPaddleOCRService
    {
        Task<ProcessingResult> ProcessDocumentAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default);
        void SetApiUrl(string apiUrl);
        void SetToken(string token);
    }

    public interface IMDConvService
    {
        Task<bool> ConvertMarkdownAsync(string inputFilePath, string outputFilePath, string format);
    }

    #endregion

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public class LogMessage
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        
        public string LevelString
        {
            get
            {
                return Level switch
                {
                    LogLevel.Info => "信息",
                    LogLevel.Warning => "警告",
                    LogLevel.Error => "错误",
                    _ => Level.ToString().ToUpper()
                };
            }
        }
        
        public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] {LevelString}: {Message}";

        public LogMessage(LogLevel level, string message)
        {
            Level = level;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string? OutputFilePath { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan? ProcessingTime { get; set; }
    }

    public class LogService : ILogService
    {
        public event EventHandler<LogMessage>? LogMessage;
        public event EventHandler<string>? ErrorOccurred;

        public void LogInfo(string message)
        {
            var logMessage = new LogMessage(LogLevel.Info, message);
            LogMessage?.Invoke(this, logMessage);
        }

        public void LogWarning(string message)
        {
            var logMessage = new LogMessage(LogLevel.Warning, message);
            LogMessage?.Invoke(this, logMessage);
        }

        public void LogError(string message)
        {
            var logMessage = new LogMessage(LogLevel.Error, message);
            LogMessage?.Invoke(this, logMessage);
            ErrorOccurred?.Invoke(this, message);
        }
    }

    public static class AppUtility
    {
        public static bool CheckFileExists(string filePath, ILogService logService, string errorMessage)
        {
            if (!File.Exists(filePath))
            {
                logService.LogError(errorMessage);
                return false;
            }
            return true;
        }

        public static void EnsureDirectoryExists(string directoryPath, ILogService logService)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                logService.LogInfo($"创建目录: {directoryPath}");
            }
        }

        public static string GetOutputFilePath(string inputFilePath, string outputDirectory, string extension, ILogService logService)
        {
            EnsureDirectoryExists(outputDirectory, logService);
            var inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
            var outputFileName = $"{inputFileName}.{extension}";
            return Path.Combine(outputDirectory, outputFileName);
        }

        public static HttpClient CreateNewClient(TimeSpan timeout)
        {
            var client = new HttpClient();
            client.Timeout = timeout;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public static string GetConfigFilePath()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appFolder = Path.Combine(documentsFolder, "DocuConvert Pro");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "config.ini");
        }

        /// <summary>
        /// 创建进程启动信息
        /// </summary>
        public static System.Diagnostics.ProcessStartInfo CreateProcessStartInfo(string fileName, string arguments, string? workingDirectory = null)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            if (!string.IsNullOrEmpty(workingDirectory)) psi.WorkingDirectory = workingDirectory;
            return psi;
        }

        /// <summary>
        /// 执行命令并返回结果
        /// </summary>
        public static async Task<(bool success, string output, string error)> ExecuteCommandAsync(string fileName, string arguments, int timeoutSeconds = 10, string? workingDirectory = null)
        {
            try
            {
                var processStartInfo = CreateProcessStartInfo(fileName, arguments, workingDirectory);
                using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
                process.Start();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                try
                {
                    // 开始并行读取输出和错误流，防止子进程写入大量日志时导致缓冲区阻塞
                    var stdOutTask = process.StandardOutput.ReadToEndAsync();
                    var stdErrTask = process.StandardError.ReadToEndAsync();

                    // 等待进程退出或超时
                    await process.WaitForExitAsync(cts.Token);

                    // 等待流读取完成
                    await Task.WhenAll(stdOutTask, stdErrTask);

                    var output = await stdOutTask;
                    var error = await stdErrTask;
                    return (process.HasExited && process.ExitCode == 0, output, error);
                }
                catch (OperationCanceledException)
                {
                    try { if (!process.HasExited) process.Kill(); } catch { }
                    return (false, string.Empty, "命令执行超时");
                }
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"命令执行异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查工具是否可用
        /// </summary>
        public static async Task<(bool available, string path)> CheckToolAvailabilityAsync(string toolName, string[] commandFormats, string[] commonPaths, string versionArgument = "--version")
        {
            try
            {
                foreach (var cmd in commandFormats.Select(format => new { FileName = format, Arguments = versionArgument }))
                {
                    var (success, output, error) = await ExecuteCommandAsync(cmd.FileName, cmd.Arguments);
                    if (success) return (true, cmd.FileName);
                }

                foreach (var path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        var (success, output, error) = await ExecuteCommandAsync(path, versionArgument);
                        if (success) return (true, path);
                    }
                }

                return (false, string.Empty);
            }
            catch (Exception)
            {
                return (false, string.Empty);
            }
        }
    }
}