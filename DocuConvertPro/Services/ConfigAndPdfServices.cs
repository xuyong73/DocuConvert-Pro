using System.IO;
using System.Text;

namespace DocuConvertProRefactored.Services
{
    public class ConfigService : IConfigService
    {
        private readonly string _configIniPath;

        public string ApiUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

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
                        if (t.StartsWith("API_URL", StringComparison.OrdinalIgnoreCase))
                            ApiUrl = t.Split('=', 2).ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                        if (t.StartsWith("TOKEN", StringComparison.OrdinalIgnoreCase))
                            Token = t.Split('=', 2).ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                    }
                    return !string.IsNullOrEmpty(ApiUrl) && !string.IsNullOrEmpty(Token);
                }
            }
            catch { }
            return false;
        }

        public bool SaveConfig(string apiUrl, string token)
        {
            try
            {
                // 确保配置目录存在
                var configDir = Path.GetDirectoryName(_configIniPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // 写入INI配置文件，仅保留 PaddleOCR 部分
                var configContent = $"# PaddleOCR-VL 配置文件\n" +
                                   "# 请根据您的实际配置修改以下参数\n\n" +
                                   "[PaddleOCR]\n" +
                                   "# PaddleOCR API端点URL和API密钥\n" +
                                   "# 请前往 https://aistudio.baidu.com/paddleocr 获取您的API_URL和TOKEN\n" +
                                   $"API_URL = {apiUrl}\n" +
                                   $"TOKEN = {token}\n";

                File.WriteAllText(_configIniPath, configContent);
                
                // 更新内存状态
                ApiUrl = apiUrl;
                Token = token;
                
                return true;
            }
            catch 
            {
                return false;
            }
        }
    }

    public class PdfSplitterService
    {
        public const int MaxPagesPerSplit = 100;

        public bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public static int GetPdfPageCount(string filePath)
        {
            try
            {
                using (var document = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                {
                    return document.PageCount;
                }
            }
            catch
            {
                return 0;
            }
        }

        public List<string> SplitPdfByPages(string inputFilePath, string outputDirectory, long maxFileSizeBytes = -1)
        {
            var splitFiles = new List<string>();
            try
            {
                using (var document = PdfSharp.Pdf.IO.PdfReader.Open(inputFilePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                {
                    int totalPages = document.PageCount;
                    int currentPage = 0;
                    int splitIndex = 1;

                    while (currentPage < totalPages)
                    {
                        int initialPagesToTake = Math.Min(MaxPagesPerSplit, totalPages - currentPage);
                        int pagesToTake = initialPagesToTake;
                        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_part{splitIndex}.pdf";
                        var outputFilePath = Path.Combine(outputDirectory, outputFileName);

                        // 如果设置了文件大小阈值，需要动态调整分割页数
                        if (maxFileSizeBytes > 0)
                        {
                            // 先尝试分割指定页数
                            using (var newDocument = new PdfSharp.Pdf.PdfDocument())
                            {
                                for (int i = 0; i < pagesToTake; i++)
                                {
                                    newDocument.AddPage(document.Pages[currentPage + i]);
                                }
                                newDocument.Save(outputFilePath);
                            }

                            // 检查生成的文件大小
                            FileInfo fileInfo = new FileInfo(outputFilePath);
                            while (fileInfo.Length > maxFileSizeBytes && pagesToTake > 1)
                            {
                                // 减小分割页数，直到文件大小符合要求
                                pagesToTake = pagesToTake / 2;
                                if (pagesToTake < 1)
                                    pagesToTake = 1;

                                // 重新分割
                                using (var newDocument = new PdfSharp.Pdf.PdfDocument())
                                {
                                    for (int i = 0; i < pagesToTake; i++)
                                    {
                                        newDocument.AddPage(document.Pages[currentPage + i]);
                                    }
                                    newDocument.Save(outputFilePath);
                                }
                                fileInfo = new FileInfo(outputFilePath);
                            }
                        }
                        else
                        {
                            // 未设置文件大小阈值，直接按页数分割
                            using (var newDocument = new PdfSharp.Pdf.PdfDocument())
                            {
                                for (int i = 0; i < pagesToTake; i++)
                                {
                                    newDocument.AddPage(document.Pages[currentPage + i]);
                                }
                                newDocument.Save(outputFilePath);
                            }
                        }

                        splitFiles.Add(outputFilePath);
                        currentPage += pagesToTake;
                        splitIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF分割失败: {ex.Message}", ex);
            }
            return splitFiles;
        }
    }
}