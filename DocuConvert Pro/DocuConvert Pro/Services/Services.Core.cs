using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace DocuConvert_Pro.Services
{
    #region 接口定义

    public interface IConfigService
    {
        string ApiEndpoint { get; set; }
        string ApiKey { get; set; }
        bool LoadConfig();

        Task<bool> ValidateApiConfiguration();
        
        bool SaveConfig(string apiUrl, string apiKey);
    }

    public interface ILogService
    {
        event EventHandler<string> LogMessage;
        event EventHandler<string> ErrorOccurred;
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    public interface IPaddleOCRService
    {
        Task<ProcessingResult> ProcessDocumentAsync(string inputFilePath, string outputDirectory);
        void SetApiEndpoint(string apiEndpoint);
        void SetApiKey(string apiKey);
    }

    public interface IMDConvService
    {
        Task<bool> ConvertMarkdownAsync(string inputFilePath, string outputFilePath, string format);
    }

    #endregion

    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string? OutputFilePath { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan? ProcessingTime { get; set; }
        public bool IsAuthenticationError { get; set; }
        public int StatusCode { get; set; }
        public bool IsFinalError { get; set; }
    }

    public class ConfigService : IConfigService
    {
        private readonly string _configIniPath;

        public string ApiEndpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public ConfigService()
        {
            _configIniPath = AppUtility.GetConfigFilePath();
            LoadConfig();
        }

        public bool LoadConfig()
        {
            try
            {
                if (File.Exists(_configIniPath))
                {
                    var lines = File.ReadAllLines(_configIniPath);
                    foreach (var line in lines)
                    {
                        var t = line.Trim();
                        if (t.StartsWith("API_URL", StringComparison.OrdinalIgnoreCase)) ApiEndpoint = t.Split('=', 2).ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                        if (t.StartsWith("TOKEN", StringComparison.OrdinalIgnoreCase)) ApiKey = t.Split('=', 2).ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                    }
                    return !string.IsNullOrEmpty(ApiEndpoint) && !string.IsNullOrEmpty(ApiKey);
                }
            }
            catch { }
            return false;
        }

        public async Task<bool> ValidateApiConfiguration()
        {
            if (string.IsNullOrEmpty(ApiEndpoint) || string.IsNullOrEmpty(ApiKey)) return false;
            try
            {
                using var c = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
                c.DefaultRequestHeaders.Add("Authorization", $"token {ApiKey}");
                var r = new HttpRequestMessage(HttpMethod.Head, ApiEndpoint);
                var resp = await c.SendAsync(r);
                return resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.MethodNotAllowed || resp.StatusCode == HttpStatusCode.BadRequest;
            }
            catch { return false; }
        }
        
        public bool SaveConfig(string apiUrl, string apiKey)
        {
            try
            {
                // 写入INI配置文件，仅保留 PaddleOCR 部分
                var configContent = $"# PaddleOCR-VL 配置文件\n" +
                                   "# 请根据您的实际配置修改以下参数\n\n" +
                                   "[PaddleOCR]\n" +
                                   "# PaddleOCR API端点URL和API密钥\n" +
                                   "# 请前往 https://aistudio.baidu.com/paddleocr 获取您的API_URL和TOKEN\n" +
                                   $"API_URL = {apiUrl}\n" +
                                   $"TOKEN = {apiKey}\n";

                File.WriteAllText(_configIniPath, configContent);
                
                // 更新内存状态
                ApiEndpoint = apiUrl;
                ApiKey = apiKey;
                
                return true;
            }
            catch 
            {
                return false;
            }
        }
    }

    public partial class PaddleOCRService : IPaddleOCRService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogService _logService;
        private readonly PdfSplitterService _pdfSplitterService;

        private string _apiEndpoint = "";
        private string _apiKey = "";
        
        // 可配置参数
        public long LargeFileThresholdBytes { get; set; } = 20L * 1024 * 1024; // 默认20MB
        public int ApiRequestTimeoutSeconds { get; set; } = 600; // 默认10分钟
        public int ImageDownloadTimeoutSeconds { get; set; } = 60; // 默认1分钟
        public int BufferSize { get; set; } = 81920; // 80KB缓冲区

        public PaddleOCRService(HttpClient httpClient, ILogService logService)
        {
            _httpClient = httpClient;
            _logService = logService;
            _pdfSplitterService = new PdfSplitterService();
        }

        public async Task<ProcessingResult> ProcessDocumentAsync(string inputFilePath, string outputDirectory)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                if (!AppUtility.CheckFileExists(inputFilePath, _logService, "输入文件不存在"))
                    return HandleProcessingError("输入文件不存在", null, sw.Elapsed);

                AppUtility.EnsureDirectoryExists(outputDirectory, _logService);
                var inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                var mdFilePath = Path.Combine(outputDirectory, inputFileName + ".md");
                
                // 检查是否为PDF文件且需要分割
                if (_pdfSplitterService.IsPdfFile(inputFilePath))
                {
                    int totalPages = PdfSplitterService.GetPdfPageCount(inputFilePath);
                    
                    // 如果PDF页数超过100页，进行分割处理
                    if (totalPages > PdfSplitterService.MaxPagesPerSplit)
                    {
                        _logService.LogInfo($"文件过大，需要分割处理：共 {totalPages} 页");
                        return await ProcessLargePdfAsync(inputFilePath, outputDirectory, mdFilePath, sw);
                    }
                }
                
                // 处理单文件（非大型PDF或其他格式）
                return await ProcessSingleFileAsync(inputFilePath, outputDirectory, mdFilePath, sw);
            }
            catch (Exception ex)
            {
                return HandleProcessingError("文档处理失败", ex, sw.Elapsed);
            }
        }
        
        /// <summary>
        /// 处理大型PDF文件，通过分割后分批处理
        /// </summary>
        private async Task<ProcessingResult> ProcessLargePdfAsync(string inputFilePath, string outputDirectory, string finalMdFilePath, System.Diagnostics.Stopwatch sw)
        {
            var tempSplitFiles = new List<string>();
            var tempMdContents = new List<StringBuilder>();
            string? tempDir = null;
            
            try
            {
                // 创建临时目录用于存放分割后的文件
                tempDir = Path.Combine(Path.GetTempPath(), $"DocuConvert_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                
                // 分割PDF文件
                tempSplitFiles = PdfSplitterService.SplitPdfByPages(inputFilePath, tempDir);
                
                // 处理每个分割后的文件
                for (int i = 0; i < tempSplitFiles.Count; i++)
                {
                    _logService.LogInfo($"处理分割文件 {i + 1}/{tempSplitFiles.Count}");
                    
                    // 处理单个分割文件，但不生成临时MD文件
                    var tempResult = await ProcessSingleFileContentAsync(tempSplitFiles[i], outputDirectory);
                    
                    if (!tempResult.Success)
                    {
                        return HandleProcessingError($"处理分割文件 {i + 1} 失败: {tempResult.ErrorMessage}", null, sw.Elapsed);
                    }
                    
                    // 保存内容供后续合并
                    if (tempResult.MarkdownContent != null)
                    {
                        tempMdContents.Add(tempResult.MarkdownContent);
                    }
                }
                
                // 合并所有分割文件的处理结果
                await MergeMarkdownContentsAsync(tempMdContents, finalMdFilePath);
                
                _logService.LogInfo($"文件处理成功！输出文件: {Path.GetFileName(finalMdFilePath)}");
                return new ProcessingResult { Success = true, OutputFilePath = finalMdFilePath, ProcessingTime = sw.Elapsed };
            }
            finally
            {
                // 清理临时目录（会自动删除目录中的所有文件）
                if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        _logService.LogWarning($"删除临时目录失败: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理单个文件并返回结果
        /// </summary>
        private async Task<ProcessingResult> ProcessSingleFileAsync(string inputFilePath, string outputDirectory, string mdFilePath, System.Diagnostics.Stopwatch sw)
        {
            string? tempJsonPath = null;
            HttpContent? content = null;
            
            try
            {
                int fileType = GetFileType(inputFilePath);
                var fi = new FileInfo(inputFilePath);
                
                // 创建内容
                content = fi.Length > LargeFileThresholdBytes ? (HttpContent)CreateStreamContent(inputFilePath, fileType, ref tempJsonPath) : (HttpContent)CreateByteArrayContent(inputFilePath, fileType);
                
                // 创建HTTP客户端，使用更长的超时时间
                using var client = AppUtility.CreateNewClient(TimeSpan.FromSeconds(ApiRequestTimeoutSeconds));
                
                // 为大文件增加上传超时设置
                if (fi.Length > LargeFileThresholdBytes)
                {
                    client.Timeout = TimeSpan.FromSeconds(ApiRequestTimeoutSeconds * 2);
                }
                
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
                {
                    Content = content,
                    Version = new Version(1, 1)
                };
                request.Headers.ExpectContinue = false;
                if (!string.IsNullOrEmpty(_apiKey)) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", _apiKey);

                // 发送请求到OCR API
                _logService.LogInfo("发送请求到OCR API ......");
                HttpResponseMessage resp;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ApiRequestTimeoutSeconds));
                    resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    _logService.LogInfo($"API响应已接收，状态码: {(int)resp.StatusCode}");
                }
                catch (OperationCanceledException)
                {
                    return HandleProcessingError($"OCR API请求超时 (超时时间: {ApiRequestTimeoutSeconds}秒)", null, sw.Elapsed);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Error while copying content to a stream"))
                    {
                        return HandleProcessingError($"调用OCR API失败: {ex.Message}，建议检查网络连接并增加超时时间", ex, sw.Elapsed);
                    }
                    return HandleProcessingError($"调用OCR API失败: {ex.Message}", ex, sw.Elapsed);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var txt = await resp.Content.ReadAsStringAsync();
                    return HandleProcessingError($"OCR API返回错误: {(int)resp.StatusCode} {resp.ReasonPhrase} - {txt}", null, sw.Elapsed, (int)resp.StatusCode);
                }

                // 解析响应并生成Markdown
                var respText = await resp.Content.ReadAsStringAsync();
                Newtonsoft.Json.Linq.JObject? jo = null;
                try { jo = Newtonsoft.Json.Linq.JObject.Parse(respText); } catch (Exception ex) { return HandleProcessingError("解析OCR响应失败", ex, sw.Elapsed); }
                if (jo == null)
                {
                    return HandleProcessingError($"OCR API未返回有效JSON: {respText}", null, sw.Elapsed);
                }

                var result = jo["result"] as Newtonsoft.Json.Linq.JObject;
                if (result == null)
                {
                    return HandleProcessingError($"OCR响应中缺少 result 字段: {respText}", null, sw.Elapsed);
                }

                // 生成Markdown
                var layout = result["layoutParsingResults"] as Newtonsoft.Json.Linq.JArray;
                if (layout == null)
                {
                    return HandleProcessingError("OCR结果中未找到 layoutParsingResults", null, sw.Elapsed);
                }

                var sbAll = new StringBuilder();
                for (int i = 0; i < layout.Count; i++)
                {
                    var page = layout[i] as Newtonsoft.Json.Linq.JObject;
                    var md = page?["markdown"]?["text"]?.ToString() ?? string.Empty;
                    md = Regex.Replace(md, "<div style=\"text-align: center;\">|</div>", "", RegexOptions.IgnoreCase);
                    md = Regex.Replace(md, @"\n{3,}", "\n\n");
                    sbAll.AppendLine(md);
                    if (i < layout.Count - 1)
                    {
                        sbAll.AppendLine();
                    }
                }

                await File.WriteAllTextAsync(mdFilePath, sbAll.ToString(), Encoding.UTF8);
                
                // 下载图片
                try
                {
                    await DownloadImagesInParallelAsync(layout, outputDirectory);
                }
                catch (Exception ex)
                {
                    _logService.LogWarning($"图片下载失败，但不影响文档转换: {ex.Message}");
                }

                _logService.LogInfo($"文件处理成功！输出文件: {Path.GetFileName(mdFilePath)}");
                return new ProcessingResult { Success = true, OutputFilePath = mdFilePath, ProcessingTime = sw.Elapsed };
            }
            finally
            {
                try { content?.Dispose(); } catch { }
                try { if (!string.IsNullOrEmpty(tempJsonPath) && File.Exists(tempJsonPath)) File.Delete(tempJsonPath); } catch { }
            }
        }
        
        /// <summary>
        /// 处理单个文件并返回内容（不生成文件）
        /// </summary>
        private async Task<SplitProcessingResult> ProcessSingleFileContentAsync(string inputFilePath, string outputDirectory)
        {
            string? tempJsonPath = null;
            HttpContent? content = null;
            
            try
            {
                int fileType = GetFileType(inputFilePath);
                var fi = new FileInfo(inputFilePath);
                
                // 创建内容
                content = fi.Length > LargeFileThresholdBytes ? (HttpContent)CreateStreamContent(inputFilePath, fileType, ref tempJsonPath) : (HttpContent)CreateByteArrayContent(inputFilePath, fileType);
                
                // 创建HTTP客户端
                using var client = AppUtility.CreateNewClient(TimeSpan.FromSeconds(ApiRequestTimeoutSeconds));
                
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
                {
                    Content = content,
                    Version = new Version(1, 1)
                };
                request.Headers.ExpectContinue = false;
                if (!string.IsNullOrEmpty(_apiKey)) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", _apiKey);

                // 发送请求
                _logService.LogInfo("发送请求到OCR API ......");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ApiRequestTimeoutSeconds));
                var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                _logService.LogInfo($"API响应已接收，状态码: {(int)resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    var txt = await resp.Content.ReadAsStringAsync();
                    return new SplitProcessingResult { Success = false, ErrorMessage = $"OCR API返回错误: {(int)resp.StatusCode} {resp.ReasonPhrase} - {txt}" };
                }

                // 解析响应
                var respText = await resp.Content.ReadAsStringAsync();
                var jo = Newtonsoft.Json.Linq.JObject.Parse(respText);
                var result = jo["result"] as Newtonsoft.Json.Linq.JObject;
                if (result == null)
                {
                    return new SplitProcessingResult { Success = false, ErrorMessage = "OCR响应中缺少 result 字段" };
                }
                var layout = result["layoutParsingResults"] as Newtonsoft.Json.Linq.JArray;
                if (layout == null)
                {
                    return new SplitProcessingResult { Success = false, ErrorMessage = "OCR结果中未找到 layoutParsingResults" };
                }
                
                // 生成Markdown内容
                var sb = new StringBuilder();
                for (int i = 0; i < layout.Count; i++)
                {
                    var page = layout[i] as Newtonsoft.Json.Linq.JObject;
                    var md = page?["markdown"]?["text"]?.ToString() ?? string.Empty;
                    md = Regex.Replace(md, "<div style=\"text-align: center;\">|</div>", "", RegexOptions.IgnoreCase);
                    md = Regex.Replace(md, @"\n{3,}", "\n\n");
                    sb.AppendLine(md);
                    if (i < layout.Count - 1)
                    {
                        sb.AppendLine();
                    }
                }
                
                // 下载图片
                try
                {
                    await DownloadImagesInParallelAsync(layout, outputDirectory);
                }
                catch { }
                
                return new SplitProcessingResult { Success = true, MarkdownContent = sb };
            }
            catch (Exception ex)
            {
                return new SplitProcessingResult { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                try { content?.Dispose(); } catch { }
                try { if (!string.IsNullOrEmpty(tempJsonPath) && File.Exists(tempJsonPath)) File.Delete(tempJsonPath); } catch { }
            }
        }
        
        /// <summary>
        /// 合并多个Markdown内容
        /// </summary>
        private async Task MergeMarkdownContentsAsync(List<StringBuilder> contents, string outputFilePath)
        {
            var finalContent = new StringBuilder();
            
            foreach (var content in contents)
            {
                finalContent.Append(content.ToString());
                finalContent.AppendLine(); // 确保部分之间有空行
            }
            
            await File.WriteAllTextAsync(outputFilePath, finalContent.ToString(), Encoding.UTF8);
        }
        
        /// <summary>
        /// 分割处理结果类
        /// </summary>
        private class SplitProcessingResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public StringBuilder? MarkdownContent { get; set; }
        }

        private StreamContent CreateStreamContent(string inputFilePath, int fileType, ref string? tempJsonPath)
        {
            try
            {
                var fi = new FileInfo(inputFilePath);
                _logService.LogInfo($"大文件 ({fi.Length} 字节)，使用磁盘临时 JSON 分块上传");
                
                // 使用异步方式创建临时文件，但避免使用GetAwaiter().GetResult()阻塞
                tempJsonPath = Task.Run(() => TempJsonHelper.CreateTemporaryJsonFileAsync(inputFilePath, fileType)).GetAwaiter().GetResult();
                
                // 使用using语句确保文件流正确关闭
                var tempStream = new FileStream(tempJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
                
                // 创建自定义的StreamContent包装类，确保资源正确释放
                var streamContent = new DisposableStreamContent(tempStream, BufferSize, tempJsonPath);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                streamContent.Headers.ContentLength = tempStream.Length;
                
                return streamContent;
            }
            catch (Exception ex)
            {
                _logService.LogError($"创建流内容时出错: {ex.Message}");
                // 清理临时文件
                if (!string.IsNullOrEmpty(tempJsonPath) && File.Exists(tempJsonPath))
                {
                    try { File.Delete(tempJsonPath); }
                    catch { }
                    tempJsonPath = null;
                }
                throw;
            }
        }
        
        // 自定义StreamContent，确保资源正确释放
        private class DisposableStreamContent : StreamContent
        {
            private readonly string _tempFilePath;
            private readonly Stream _contentStream;
            private bool _isDisposed = false;
            
            public DisposableStreamContent(Stream content, int bufferSize, string tempFilePath)
                : base(content, bufferSize)
            {
                _contentStream = content;
                _tempFilePath = tempFilePath;
            }
            
            protected override void Dispose(bool disposing)
            {
                if (!_isDisposed && disposing)
                {
                    // 确保内部流关闭
                    try { _contentStream.Dispose(); } catch { }
                    
                    // 删除临时文件
                    try 
                    {
                        if (File.Exists(_tempFilePath))
                            File.Delete(_tempFilePath); 
                    }
                    catch { }
                    
                    _isDisposed = true;
                }
                
                base.Dispose(disposing);
            }
        }
        
        // 统一的异常处理辅助方法
        private ProcessingResult HandleProcessingError(string message, Exception? ex = null, TimeSpan processingTime = default, int statusCode = 500)
        {
            var errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
            _logService.LogError(errorMessage);
            return new ProcessingResult 
            {
                Success = false, 
                OutputFilePath = null, 
                ErrorMessage = errorMessage,
                ProcessingTime = processingTime,
                StatusCode = statusCode
            };
        }
        
        private int GetFileType(string inputFilePath)
        {
            var ext = Path.GetExtension(inputFilePath).ToLowerInvariant();
            // 0表示PDF，1表示其他格式（如图片、文档等）
            return ext == ".pdf" ? 0 : 1;
        }

        private ByteArrayContent CreateByteArrayContent(string inputFilePath, int fileType)
        {
            var bytes = File.ReadAllBytesAsync(inputFilePath).GetAwaiter().GetResult();
            var b64 = Convert.ToBase64String(bytes);
            var payload = new
            {
                file = b64,
                fileType = fileType,
                useDocOrientationClassify = false,
                useDocUnwarping = false,
                useChartRecognition = false
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var bytesJson = Encoding.UTF8.GetBytes(json);
            var byteArrayContent = new ByteArrayContent(bytesJson);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            byteArrayContent.Headers.ContentLength = bytesJson.Length;
            return byteArrayContent;
        }
        
        private async Task DownloadImagesInParallelAsync(Newtonsoft.Json.Linq.JArray layout, string outputDirectory)
        {
            using var imgClient = AppUtility.CreateNewClient(TimeSpan.FromSeconds(ImageDownloadTimeoutSeconds));
            var downloadTasks = new List<Task>();
            
            // 收集所有图片下载任务
            foreach (var page in layout)
            {
                var pageObj = page as Newtonsoft.Json.Linq.JObject;
                var images = pageObj?["markdown"]?["images"] as Newtonsoft.Json.Linq.JObject;
                if (images == null) continue;
                
                foreach (var prop in images.Properties())
                {
                    // 为每个图片创建一个异步下载任务
                    downloadTasks.Add(DownloadSingleImageAsync(imgClient, prop, outputDirectory));
                }
            }
            
            // 并行执行所有下载任务
            if (downloadTasks.Count > 0)
            {
                _logService.LogInfo($"开始并行下载 {downloadTasks.Count} 张图片...");
                await Task.WhenAll(downloadTasks);
                _logService.LogInfo("图片下载已完成");
            }
        }
        
        private async Task DownloadSingleImageAsync(HttpClient client, Newtonsoft.Json.Linq.JProperty property, string outputDirectory)
        {
            try
            {
                var relativePath = property.Name.Replace('/', Path.DirectorySeparatorChar);
                var url = property.Value?.ToString() ?? string.Empty;
                
                if (string.IsNullOrEmpty(url)) 
                {
                    _logService.LogWarning($"图片URL为空: {relativePath}");
                    return;
                }

                var savePath = Path.Combine(outputDirectory, relativePath);
                var saveDir = Path.GetDirectoryName(savePath) ?? outputDirectory;
                
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                // 下载图片
                using var respImg = await client.GetAsync(url);
                if (respImg.IsSuccessStatusCode)
                {
                    var data = await respImg.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(savePath, data);
                    _logService.LogInfo($"图片已保存: {Path.GetFileName(savePath)}");
                }
                else
                {
                    _logService.LogWarning($"图片下载失败 {url} 状态码: {(int)respImg.StatusCode}");
                }
            }
            catch (Exception ex)
                {
                    _logService.LogWarning($"处理图片 {property.Name} 时出错: {ex.Message}");
                }
        }
        
        public void SetApiEndpoint(string apiEndpoint) => _apiEndpoint = apiEndpoint;
        public void SetApiKey(string apiKey) => _apiKey = apiKey;
    }
}