using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DocuConvert_Pro.Services
{
    public class LogService : ILogService
    {
        private readonly StringBuilder _logBuilder;
        private readonly object _lockObject;

        public event EventHandler<string> LogMessage = delegate { };
        public event EventHandler<string> ErrorOccurred = delegate { };

        public LogService()
        {
            _logBuilder = new StringBuilder();
            _lockObject = new object();
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
            LogMessage?.Invoke(this, message);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
            LogMessage?.Invoke(this, message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
            ErrorOccurred?.Invoke(this, message);
        }

        private void Log(string level, string message)
        {
            lock (_lockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"[{timestamp}] [{level}] {message}\n";
                _logBuilder.Append(logEntry);

                if (_logBuilder.Length > 100000)
                {
                    var lines = _logBuilder.ToString().Split('\n');
                    if (lines.Length > 500)
                    {
                        var recentLines = lines[^500..];
                        _logBuilder.Clear();
                        _logBuilder.Append(string.Join("\n", recentLines));
                    }
                }
            }
        }
    }

    public static class AppUtility
    {
        public static string GetConfigFilePath()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appFolder = Path.Combine(documentsFolder, "DocuConvert Pro");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "config.ini");
        }

        public static void EnsureDirectoryExists(string directoryPath, ILogService? logService = null)
        {
            if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }

        public static string GetOutputFilePath(string inputFilePath, string outputDirectory, string extension, ILogService? logService = null)
        {
            EnsureDirectoryExists(outputDirectory, logService);
            var inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
            var outputFileName = $"{inputFileName}.{extension}";
            return Path.Combine(outputDirectory, outputFileName);
        }

        public static bool CheckFileExists(string filePath, ILogService? logService = null, string? errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                if (errorMessage != null) logService?.LogError(errorMessage);
                return false;
            }
            return true;
        }

        public static HttpClient CreateNewClient(TimeSpan? timeout = null)
        {
            var client = new HttpClient();
            if (timeout.HasValue) client.Timeout = timeout.Value;
            return client;
        }
    }

    internal static class TempJsonHelper
    {
        public static async Task<string> CreateTemporaryJsonFileAsync(string sourceFilePath, int fileType)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"DocuConvert_json_{Guid.NewGuid():N}.tmp");

            using (var outFs = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var encoding = Encoding.UTF8;
                var prefix = encoding.GetBytes("{\"file\":\"");
                await outFs.WriteAsync(prefix, 0, prefix.Length);

                const int readSize = 1024 * 1024;
                byte[] buffer = new byte[readSize];
                byte[] leftover = Array.Empty<byte>();

                using (var inFs = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int read;
                    while ((read = await inFs.ReadAsync(buffer, 0, readSize)) > 0)
                    {
                        byte[] toEncode;
                        if (leftover.Length > 0)
                        {
                            toEncode = new byte[leftover.Length + read];
                            Buffer.BlockCopy(leftover, 0, toEncode, 0, leftover.Length);
                            Buffer.BlockCopy(buffer, 0, toEncode, leftover.Length, read);
                        }
                        else
                        {
                            toEncode = new byte[read];
                            Buffer.BlockCopy(buffer, 0, toEncode, 0, read);
                        }

                        int encodeLen = (toEncode.Length / 3) * 3;
                        int remainder = toEncode.Length - encodeLen;

                        if (encodeLen > 0)
                        {
                            var b64 = Convert.ToBase64String(toEncode, 0, encodeLen);
                            var b64Bytes = encoding.GetBytes(b64);
                            await outFs.WriteAsync(b64Bytes, 0, b64Bytes.Length);
                        }

                        if (remainder > 0)
                        {
                            leftover = new byte[remainder];
                            Buffer.BlockCopy(toEncode, encodeLen, leftover, 0, remainder);
                        }
                        else
                        {
                            leftover = Array.Empty<byte>();
                        }
                    }

                    if (leftover.Length > 0)
                    {
                        var lastB64 = Convert.ToBase64String(leftover, 0, leftover.Length);
                        var lastBytes = encoding.GetBytes(lastB64);
                        await outFs.WriteAsync(lastBytes, 0, lastBytes.Length);
                    }
                }

                var finalSuffix = encoding.GetBytes("\",\"fileType\":" + fileType + "}");
                await outFs.WriteAsync(finalSuffix, 0, finalSuffix.Length);
                await outFs.FlushAsync();
            }

            return tempFilePath;
        }
    }
}


