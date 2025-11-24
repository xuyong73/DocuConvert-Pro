using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<PaddleOCRService>? _logger;

        private string _apiEndpoint = "";
        private string _apiKey = "";

        public PaddleOCRService(HttpClient httpClient, ILogService logService, ILogger<PaddleOCRService>? logger = null)
        {
            _httpClient = httpClient;
            _logService = logService;
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessDocumentAsync(string inputFilePath, string outputDirectory)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string? tempJsonPath = null;
            FileStream? tempStream = null;
            HttpContent? content = null;
            try
            {
                if (!AppUtility.CheckFileExists(inputFilePath, _logService, "输入文件不存在"))
                    return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = TimeSpan.Zero };

                AppUtility.EnsureDirectoryExists(outputDirectory, _logService);

                var ext = Path.GetExtension(inputFilePath).ToLowerInvariant();
                int fileType = ext == ".pdf" ? 0 : 1;

                var inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                var mdFilePath = Path.Combine(outputDirectory, inputFileName + ".md");

                var fi = new FileInfo(inputFilePath);
                const long largeThreshold = 20L * 1024 * 1024; // 20MB
                if (fi.Length > largeThreshold)
                {
                    _logService.LogInfo($"大文件 ({fi.Length} 字节)，使用磁盘临时 JSON 分块上传");
                    tempJsonPath = await TempJsonHelper.CreateTemporaryJsonFileAsync(inputFilePath, fileType);
                    tempStream = new FileStream(tempJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    content = new StreamContent(tempStream, 81920);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    content.Headers.ContentLength = tempStream.Length;
                }
                else
                {
                    var bytes = await File.ReadAllBytesAsync(inputFilePath);
                    var b64 = Convert.ToBase64String(bytes);
                    var payload = new
                    {
                        file = b64,
                        fileType = fileType,    // 必须保留，不能删除
                        useDocOrientationClassify = false,
                        useDocUnwarping = false,
                        useChartRecognition = false
                    };
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    var bytesJson = Encoding.UTF8.GetBytes(json);
                    var ba = new ByteArrayContent(bytesJson);
                    ba.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    ba.Headers.ContentLength = bytesJson.Length;
                    content = ba;
                }

                using var client = AppUtility.CreateNewClient(TimeSpan.FromSeconds(600));
                var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
                {
                    Content = content,
                    Version = new Version(1, 1)
                };
                request.Headers.ExpectContinue = false;
                if (!string.IsNullOrEmpty(_apiKey)) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", _apiKey);

                _logService.LogInfo($"发送请求到OCR API ......");

                HttpResponseMessage resp;
                try
                {
                    resp = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                }
                catch (Exception ex)
                {
                    _logService.LogError($"调用OCR API失败: {ex.Message}");
                    return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = sw.Elapsed };
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var txt = await resp.Content.ReadAsStringAsync();
                    _logService.LogError($"OCR API返回错误: {(int)resp.StatusCode} {resp.ReasonPhrase} - {txt}");
                    return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = sw.Elapsed };
                }

                var respText = await resp.Content.ReadAsStringAsync();
                Newtonsoft.Json.Linq.JObject? jo = null;
                try { jo = Newtonsoft.Json.Linq.JObject.Parse(respText); } catch (Exception ex) { _logService.LogError($"解析OCR响应失败: {ex.Message}"); }
                if (jo == null)
                {
                    _logService.LogError($"OCR API未返回有效JSON: {respText}");
                    return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = sw.Elapsed };
                }

                var result = jo["result"] as Newtonsoft.Json.Linq.JObject;
                if (result == null)
                {
                    _logService.LogError($"OCR响应中缺少 result 字段: {respText}");
                    return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = sw.Elapsed };
                }

                // 写 Markdown
                try
                {
                    var sbAll = new StringBuilder();
                    var layout = result["layoutParsingResults"] as Newtonsoft.Json.Linq.JArray;
                    if (layout == null)
                    {
                        _logService.LogError("OCR结果中未找到 layoutParsingResults");
                        return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = sw.Elapsed };
                    }

                    for (int i = 0; i < layout.Count; i++)
                    {
                        var page = layout[i] as Newtonsoft.Json.Linq.JObject;
                        _logService.LogInfo($"处理页面 {i + 1}/{layout.Count}");
                        var md = page?["markdown"]?["text"]?.ToString() ?? string.Empty;
                        md = Regex.Replace(md, "<div style=\"text-align: center;\">|</div>", "", RegexOptions.IgnoreCase);
                        // 压缩当前页面内部的多余空行
                        md = Regex.Replace(md, @"\n{3,}", "\n\n");
                        sbAll.AppendLine(md);
                        // 确保页面之间有一个空行分隔
                        if (i < layout.Count - 1)
                        {
                            sbAll.AppendLine();
                        }
                    }

                    await File.WriteAllTextAsync(mdFilePath, sbAll.ToString(), Encoding.UTF8);

                    // 下载OCR返回的 images 映射（如果存在），优先使用返回的URL来获取图片并保存到与markdown中相对路径一致的位置
                    try
                    {
                        using var imgClient = AppUtility.CreateNewClient(TimeSpan.FromSeconds(60));
                        // 遍历每页再次下载对应的 images（如果API在每页中返回images映射）
                        for (int i = 0; i < layout.Count; i++)
                        {
                            var page = layout[i] as Newtonsoft.Json.Linq.JObject;
                            var images = page? ["markdown"]?["images"] as Newtonsoft.Json.Linq.JObject;
                            if (images == null) continue;

                            foreach (var prop in images.Properties())
                            {
                                try
                                {
                                    var relativePath = prop.Name.Replace('/', Path.DirectorySeparatorChar);
                                    var url = prop.Value?.ToString() ?? string.Empty;
                                    if (string.IsNullOrEmpty(url)) { _logService.LogWarning($"图片URL为空: {relativePath}"); continue; }

                                    var savePath = Path.Combine(outputDirectory, relativePath);
                                    var saveDir = Path.GetDirectoryName(savePath) ?? outputDirectory;
                                    if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

                                    // 下载时不要使用 API 授权头，可能会导致图片托管服务拒绝
                                    try
                                    {
                                        using var respImg = await imgClient.GetAsync(url);
                                        if (respImg.IsSuccessStatusCode)
                                        {
                                            var data = await respImg.Content.ReadAsByteArrayAsync();
                                            await File.WriteAllBytesAsync(savePath, data);
                                            _logService.LogInfo($"图片已保存: {savePath}");
                                        }
                                        else
                                        {
                                            _logService.LogWarning($"图片下载失败 {url} 状态码: {(int)respImg.StatusCode}");
                                        }
                                    }
                                    catch (Exception exImg)
                                    {
                                        _logService.LogWarning($"下载图片异常 {url}: {exImg.Message}");
                                    }
                                }
                                catch (Exception exImgProp)
                                {
                                    _logService.LogWarning($"处理图片映射时出错: {exImgProp.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogWarning($"图片下载可能失败: {ex.Message}");
                    }

                    _logService.LogInfo($"已生成 Markdown: {mdFilePath}");
                    return new ProcessingResult { Success = true, OutputFilePath = mdFilePath, ProcessingTime = sw.Elapsed };
                }
                catch (Exception ex)
                {
                    _logService.LogError($"生成 Markdown 失败: {ex.Message}");
                    return new ProcessingResult { Success = false, OutputFilePath = null, ProcessingTime = sw.Elapsed };
                }
            }
            finally
            {
                try { content?.Dispose(); } catch { }
                try { tempStream?.Dispose(); } catch { }
                try { if (!string.IsNullOrEmpty(tempJsonPath) && File.Exists(tempJsonPath)) File.Delete(tempJsonPath); } catch { }
            }
        }

        public void SetApiEndpoint(string apiEndpoint) => _apiEndpoint = apiEndpoint;
        public void SetApiKey(string apiKey) => _apiKey = apiKey;
    }
}
