using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DocuConvertProRefactored.Services
{
    public class PaddleOCRService : IPaddleOCRService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private readonly PdfSplitterService _pdfSplitterService;

        private string _apiUrl = "";
        private string _token = "";
        
        // 可配置参数
        public long LargeFileThresholdBytes { get; set; } = 10L * 1024 * 1024; // 默认10MB
        public int ApiRequestTimeoutSeconds { get; set; } = 600; // 默认10分钟
        public int ImageDownloadTimeoutSeconds { get; set; } = 180; // 默认3分钟
        public int BufferSize { get; set; } = 81920; // 80KB缓冲区
        public int MaxRetryAttempts { get; set; } = 3; // 默认最多重试3次
        public int RetryDelaySeconds { get; set; } = 5; // 默认重试间隔5秒

        public PaddleOCRService(HttpClient httpClient, ILogService logService, IConfigService configService)
        {
            _httpClient = httpClient;
            _logService = logService;
            _configService = configService;
            _pdfSplitterService = new PdfSplitterService();
            
            // 加载配置
            _configService.LoadConfig();
            _apiUrl = _configService.ApiUrl;
            _token = _configService.Token;
        }

        public async Task<ProcessingResult> ProcessDocumentAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                AppUtility.EnsureDirectoryExists(outputDirectory, _logService);
                
                var inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                var outputMdFilePath = Path.Combine(outputDirectory, inputFileName + ".md");
                
                // 检查是否为PDF文件且需要分割处理
                if (_pdfSplitterService.IsPdfFile(inputFilePath))
                {
                    int totalPages = PdfSplitterService.GetPdfPageCount(inputFilePath);
                    long fileSize = new FileInfo(inputFilePath).Length;
                    
                    // 如果PDF页数超过100页或文件大小超过阈值，进行分割处理
                    if (totalPages > PdfSplitterService.MaxPagesPerSplit || fileSize > LargeFileThresholdBytes)
                    {
                        _logService.LogInfo($"PDF文件需要分割处理：共 {totalPages} 页，大小 {fileSize / (1024 * 1024):F1} MB");
                        return await ProcessLargePdfAsync(inputFilePath, outputDirectory, outputMdFilePath, stopwatch, cancellationToken);
                    }
                }
                
                return await ProcessSingleFileAsync(inputFilePath, outputDirectory, outputMdFilePath, stopwatch, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return HandleProcessingError("处理已取消", null, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return HandleProcessingError("文档处理失败", ex, stopwatch.Elapsed);
            }
        }
        
        /// <summary>
        /// 处理大型PDF文件，通过分割后分批处理
        /// </summary>
        /// <param name="inputFilePath">输入PDF文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="finalMdFilePath">最终合并后的Markdown文件路径</param>
        /// <param name="stopwatch">计时秒表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含处理结果的对象</returns>
        private async Task<ProcessingResult> ProcessLargePdfAsync(string inputFilePath, string outputDirectory, string finalMdFilePath, System.Diagnostics.Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            var splitFilePaths = new List<string>(); // 分割后的PDF文件路径列表
            var splitMdContents = new List<StringBuilder>(); // 各个分割文件生成的Markdown内容
            string? tempDirectory = null; // 临时目录，用于存放分割后的PDF文件
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 创建唯一的临时目录
                tempDirectory = Path.Combine(Path.GetTempPath(), $"DocuConvert_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDirectory);
                
                // 分割PDF文件，同时考虑页数和文件大小阈值
                splitFilePaths = _pdfSplitterService.SplitPdfByPages(inputFilePath, tempDirectory, LargeFileThresholdBytes);
                
                // 处理每个分割后的PDF文件
                for (int i = 0; i < splitFilePaths.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    string splitFilePath = splitFilePaths[i];
                    _logService.LogInfo($"处理分割文件 {i + 1}/{splitFilePaths.Count}: {Path.GetFileName(splitFilePath)}");
                    
                    // 处理单个分割文件，返回Markdown内容但不生成文件
                    var splitProcessingResult = await ProcessSingleFileContentAsync(splitFilePath, outputDirectory, cancellationToken);
                    
                    if (!splitProcessingResult.Success)
                    {
                        return HandleProcessingError($"处理分割文件 {i + 1} 失败: {splitProcessingResult.ErrorMessage}", null, stopwatch.Elapsed);
                    }
                    
                    // 保存分割文件的Markdown内容，供后续合并
                    if (splitProcessingResult.MarkdownContent != null)
                    {
                        splitMdContents.Add(splitProcessingResult.MarkdownContent);
                    }
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // 合并所有分割文件的Markdown内容到最终文件
                await MergeMarkdownContentsAsync(splitMdContents, finalMdFilePath);
                
                _logService.LogInfo($"大型PDF文件处理成功！输出文件: {Path.GetFileName(finalMdFilePath)}");
                return new ProcessingResult { Success = true, OutputFilePath = finalMdFilePath, ProcessingTime = stopwatch.Elapsed };
            }
            catch (OperationCanceledException)
            {
                _logService.LogInfo("处理已取消");
                return HandleProcessingError("处理已取消", null, stopwatch.Elapsed);
            }
            finally
            {
                // 清理临时目录及其所有文件
                if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }
        
        /// <summary>
        /// 处理单个文件的核心逻辑，返回Markdown内容和布局信息
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="attemptInfo">重试尝试信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含Markdown内容和布局信息的元组</returns>
        private async Task<(StringBuilder MarkdownContent, Newtonsoft.Json.Linq.JArray Layout)> ProcessSingleFileCoreAsync(string inputFilePath, string outputDirectory, string? attemptInfo = null, CancellationToken cancellationToken = default)
        {
            HttpContent? httpContent = null;
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                int fileType = GetFileType(inputFilePath);
                
                int retryCount = 0;
                bool isProcessingSuccess = false;
                StringBuilder? markdownContent = null;
                Newtonsoft.Json.Linq.JArray? layoutInfo = null;
                string? errorMessage = null;
                
                while (retryCount < MaxRetryAttempts && !isProcessingSuccess)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        httpContent?.Dispose();
                        httpContent = await CreateByteArrayContentAsync(inputFilePath, fileType) as HttpContent;
                        
                        using var httpClient = AppUtility.CreateNewClient(TimeSpan.FromSeconds(ApiRequestTimeoutSeconds));
                      
                        using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                        {
                            Content = httpContent
                        };
                        request.Headers.ExpectContinue = false;
                        
                        if (!string.IsNullOrEmpty(_token)) 
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("token", _token);
                        }

                        _logService.LogInfo($"发送请求到OCR API ...... ");
                        
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        linkedCts.CancelAfter(TimeSpan.FromSeconds(ApiRequestTimeoutSeconds));
                        
                        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                        _logService.LogInfo($"API响应已接收，状态码: {(int)response.StatusCode} ({GetStatusCodeDescription(response.StatusCode)})");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseText = await response.Content.ReadAsStringAsync();
                            Newtonsoft.Json.Linq.JObject? responseJson = null;
                            
                            try 
                            {
                                responseJson = Newtonsoft.Json.Linq.JObject.Parse(responseText);
                            } 
                            catch (Exception ex) 
                            {
                                throw new Exception("解析OCR API响应失败", ex);
                            }
                            
                            if (responseJson == null)
                            {
                                throw new Exception($"OCR API未返回有效JSON响应: {responseText}");
                            }

                            var apiResult = responseJson["result"] as Newtonsoft.Json.Linq.JObject;
                            if (apiResult == null)
                            {
                                throw new Exception($"OCR响应中缺少result字段: {responseText}");
                            }

                            layoutInfo = apiResult["layoutParsingResults"] as Newtonsoft.Json.Linq.JArray;
                            if (layoutInfo == null)
                            {
                                throw new Exception("OCR结果中未找到layoutParsingResults字段");
                            }

                            markdownContent = new StringBuilder();
                            for (int i = 0; i < layoutInfo.Count; i++)
                            {
                                var page = layoutInfo[i] as Newtonsoft.Json.Linq.JObject;
                                var pageMarkdown = page?["markdown"]?["text"]?.ToString() ?? string.Empty;
                                
                                pageMarkdown = Regex.Replace(pageMarkdown, "<div style=\"text-align: center;\">|</div>", "", RegexOptions.IgnoreCase);
                                pageMarkdown = Regex.Replace(pageMarkdown, @"\n{3,}", "\n\n");
                                
                                markdownContent.AppendLine(pageMarkdown);
                                if (i < layoutInfo.Count - 1)
                                {
                                    markdownContent.AppendLine();
                                }
                            }

                            isProcessingSuccess = true;
                        }
                        else
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            
                            if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
                            {
                                retryCount++;
                                
                                int delaySeconds = (int)(RetryDelaySeconds * Math.Pow(2, retryCount - 1));
                                
                                _logService.LogWarning($"OCR API 服务器错误 ({(int)response.StatusCode} {GetStatusCodeDescription(response.StatusCode)}) ，等{delaySeconds}秒后重新发起请求");
                                
                                if (retryCount < MaxRetryAttempts)
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                                }
                                else
                                {
                                    errorMessage = "OCR API 响应多次失败，请稍后再试";
                                    break;
                                }
                            }
                            else
                            {
                                if ((int)response.StatusCode == 429)
                                {
                                    errorMessage = "请求过多，请过一段时间后再试";
                                }
                                else
                                {
                                    errorMessage = $"OCR API 返回错误: {(int)response.StatusCode} {response.ReasonPhrase} - {responseContent}";
                                }
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        retryCount++;
                        
                        int delaySeconds = (int)(RetryDelaySeconds * Math.Pow(2, retryCount - 1));
                        
                        _logService.LogWarning($"OCR API 响应超时，等{delaySeconds}秒后重新发起请求");
                        
                        if (retryCount < MaxRetryAttempts)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        }
                        else
                        {
                            errorMessage = "OCR API 响应多次失败，请稍后再试";
                            break;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        errorMessage = $"网络连接错误: {ex.Message}，请先检查网络连接，确保网络正常后再重试";
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        
                        int delaySeconds = (int)(RetryDelaySeconds * Math.Pow(2, retryCount - 1));
                        
                        _logService.LogWarning($"OCR API 请求失败: {ex.Message}，等{delaySeconds}秒后重新发起请求");
                        
                        if (retryCount < MaxRetryAttempts)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        }
                        else
                        {
                            errorMessage = "OCR API 响应多次失败，请稍后再试";
                            break;
                        }
                    }
                }

                if (!isProcessingSuccess || markdownContent == null || layoutInfo == null)
                {
                    throw new Exception(errorMessage ?? "OCR API请求失败，未收到有效响应");
                }

                return (markdownContent, layoutInfo);
            }
            finally
            {
                try { httpContent?.Dispose(); } catch { }
            }
        }
        
        private async Task<ProcessingResult> ProcessSingleFileAsync(string inputFilePath, string outputDirectory, string mdFilePath, System.Diagnostics.Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                var (markdownContent, layoutInfo) = await ProcessSingleFileCoreAsync(inputFilePath, outputDirectory, $"(尝试 1/{MaxRetryAttempts})", cancellationToken);
                
                await File.WriteAllTextAsync(mdFilePath, markdownContent.ToString(), Encoding.UTF8);
                
                try
                {
                    await DownloadImagesInParallelAsync(layoutInfo, outputDirectory);
                }
                catch (Exception ex)
                {
                    _logService.LogWarning($"图片下载失败，但不影响文档转换: {ex.Message}");
                }

                _logService.LogInfo($"文件处理成功！输出文件: {Path.GetFileName(mdFilePath)}");
                return new ProcessingResult { Success = true, OutputFilePath = mdFilePath, ProcessingTime = stopwatch.Elapsed };
            }
            catch (OperationCanceledException)
            {
                _logService.LogInfo("处理已取消");
                return HandleProcessingError("处理已取消", null, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                return HandleProcessingError(ex.Message, null, stopwatch.Elapsed);
            }
        }
        
        private async Task<SplitProcessingResult> ProcessSingleFileContentAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken)
        {
            try
            {
                var (markdownContent, layoutInfo) = await ProcessSingleFileCoreAsync(inputFilePath, outputDirectory, cancellationToken: cancellationToken);
                
                try
                {
                    await DownloadImagesInParallelAsync(layoutInfo, outputDirectory);
                }
                catch { }
                
                return new SplitProcessingResult { Success = true, MarkdownContent = markdownContent };
            }
            catch (OperationCanceledException)
            {
                _logService.LogInfo("处理已取消");
                return new SplitProcessingResult { Success = false, ErrorMessage = "处理已取消" };
            }
            catch (Exception ex)
            {
                return new SplitProcessingResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        
        private async Task MergeMarkdownContentsAsync(List<StringBuilder> contents, string outputFilePath)
        {
            var finalContent = new StringBuilder();
            
            foreach (var content in contents)
            {
                finalContent.Append(content.ToString());
                finalContent.AppendLine();
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
        
        private ProcessingResult HandleProcessingError(string message, Exception? ex = null, TimeSpan processingTime = default)
        {
            var errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
            return new ProcessingResult 
            {
                Success = false, 
                OutputFilePath = null, 
                ErrorMessage = errorMessage,
                ProcessingTime = processingTime
            };
        }
        
        private int GetFileType(string inputFilePath)
        {
            var ext = Path.GetExtension(inputFilePath).ToLowerInvariant();
            return ext == ".pdf" ? 0 : 1;
        }
        
        private string GetStatusCodeDescription(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.OK => "成功",
                System.Net.HttpStatusCode.BadRequest => "请求错误",
                System.Net.HttpStatusCode.Unauthorized => "未授权",
                System.Net.HttpStatusCode.Forbidden => "禁止访问",
                System.Net.HttpStatusCode.NotFound => "资源未找到",
                System.Net.HttpStatusCode.TooManyRequests => "请求过多",
                System.Net.HttpStatusCode.InternalServerError => "服务器内部错误",
                System.Net.HttpStatusCode.BadGateway => "网关错误",
                System.Net.HttpStatusCode.ServiceUnavailable => "服务不可用",
                System.Net.HttpStatusCode.GatewayTimeout => "网关超时",
                _ => statusCode.ToString()
            };
        }

        private async Task<ByteArrayContent> CreateByteArrayContentAsync(string inputFilePath, int fileType)
        {
            var bytes = await File.ReadAllBytesAsync(inputFilePath);
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
            using var httpClient = AppUtility.CreateNewClient(TimeSpan.FromSeconds(ImageDownloadTimeoutSeconds));
            var imageList = new List<Newtonsoft.Json.Linq.JProperty>();
            
            foreach (var page in layout)
            {
                var pageObj = page as Newtonsoft.Json.Linq.JObject;
                var images = pageObj?["markdown"]?["images"] as Newtonsoft.Json.Linq.JObject;
                if (images == null) continue;
                
                foreach (var imageProp in images.Properties())
                {
                    imageList.Add(imageProp);
                }
            }
            
            int totalImages = imageList.Count;
            if (totalImages == 0) return;
            
            _logService.LogInfo($"开始并行下载 {totalImages} 张图片...");
            
            const int maxConcurrentDownloads = 10;
            var semaphore = new SemaphoreSlim(maxConcurrentDownloads);
            var downloadTasks = new List<Task>();
            
            foreach (var imageProp in imageList)
            {
                await semaphore.WaitAsync();
                downloadTasks.Add(DownloadSingleImageWithSemaphoreAsync(httpClient, imageProp, outputDirectory, semaphore));
            }
            
            await Task.WhenAll(downloadTasks);
            
            _logService.LogInfo("图片下载已完成");
        }
        
        private async Task DownloadSingleImageWithSemaphoreAsync(HttpClient httpClient, Newtonsoft.Json.Linq.JProperty imageProp, string outputDirectory, SemaphoreSlim semaphore)
        {
            try
            {
                await DownloadSingleImageAsync(httpClient, imageProp, outputDirectory);
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        private async Task DownloadSingleImageAsync(HttpClient httpClient, Newtonsoft.Json.Linq.JProperty imageProp, string outputDirectory)
        {
            try
            {
                string relativePath = imageProp.Name.Replace('/', Path.DirectorySeparatorChar);
                string imageUrl = imageProp.Value?.ToString() ?? string.Empty;
                
                if (string.IsNullOrEmpty(imageUrl)) 
                {
                    _logService.LogWarning($"图片URL为空: {relativePath}");
                    return;
                }

                string savePath = Path.Combine(outputDirectory, relativePath);
                string saveDirectory = Path.GetDirectoryName(savePath) ?? outputDirectory;
                
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                using var response = await httpClient.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(savePath, imageData);
                    _logService.LogInfo($"图片已保存: {Path.GetFileName(savePath)}");
                }
                else
                {
                    _logService.LogWarning($"图片下载失败 {imageUrl} 状态码: {(int)response.StatusCode} ({GetStatusCodeDescription(response.StatusCode)})");
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"处理图片 {imageProp.Name} 时出错: {ex.Message}");
            }
        }

        public void SetApiUrl(string apiUrl) => _apiUrl = apiUrl;
        public void SetToken(string token) => _token = token;
    }
}