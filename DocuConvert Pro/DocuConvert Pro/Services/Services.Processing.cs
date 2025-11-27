using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DocuConvert_Pro.Services
{
    public partial class MDConvServices : IMDConvService
    {
        private readonly ILogService _logService;

        public MDConvServices(ILogService logService)
        {
            _logService = logService;
        }

        public async Task<bool> ConvertMarkdownAsync(string inputFilePath, string outputDirectory, string targetFormat)
        {
            if (!AppUtility.CheckFileExists(inputFilePath, _logService, "输入文件不存在")) return false;
            var outputFilePath = AppUtility.GetOutputFilePath(inputFilePath, outputDirectory, targetFormat, _logService);
            return await ConvertMarkdownCoreAsync(inputFilePath, outputFilePath, targetFormat);
        }

        private async Task<bool> ConvertMarkdownCoreAsync(string inputFilePath, string outputFilePath, string format)
        {
            var (pandocAvailable, pandocPath) = await IsPandocAvailableAsync();
            if (!pandocAvailable)
            {
                _logService.LogError("Pandoc未安装，无法进行格式转换");
                System.Windows.Forms.MessageBox.Show("格式转换失败：未检测到Pandoc。\n请下载并安装Pandoc以支持格式转换功能：\n下载地址: https://pandoc.org/installing.html\n安装后重启应用程序即可使用格式转换功能", "格式转换失败", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }

            try
            {
                var processedFilePath = await PreprocessMarkdownFileAsync(inputFilePath);

                // 为了让 pandoc 正确解析相对资源（图片、链接等），将工作目录设置为处理后 Markdown 的目录，并传入文件名（basename）。
                var processedDir = Path.GetDirectoryName(processedFilePath) ?? Environment.CurrentDirectory;
                var processedBaseName = Path.GetFileName(processedFilePath);

                string arguments;
                if (format.Equals("docx", StringComparison.OrdinalIgnoreCase))
                {
                    arguments = $"-f markdown+tex_math_dollars -t docx --mathml -o \"{outputFilePath}\" \"{processedBaseName}\"";

                    // 加载Word模板文件
                    var templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reference.docx");
                    if (File.Exists(templateFilePath))
                    {
                        arguments += $" --reference-doc=\"{templateFilePath}\"";
                    }

                    // resource-path：包含处理后文件目录与原始输入目录（若不同），使用 PATH 分隔符
                    var inputDir = Path.GetDirectoryName(inputFilePath);
                    if (!string.IsNullOrEmpty(inputDir))
                    {
                        var resourcePaths = processedDir;
                        if (!string.Equals(Path.GetFullPath(inputDir), Path.GetFullPath(processedDir), StringComparison.OrdinalIgnoreCase))
                            resourcePaths = processedDir + Path.PathSeparator + inputDir;
                        arguments += $" --resource-path=\"{resourcePaths}\"";
                    }

                    arguments += " --standalone --table-caption-position=above --list-tables --syntax-highlighting=tango";
                }
                else if (format.Equals("html", StringComparison.OrdinalIgnoreCase))
                {
                    arguments = $"-f markdown+tex_math_dollars -t html --mathml --standalone -o \"{outputFilePath}\" \"{processedBaseName}\"";
                    var cssTableStyle = "\n<style>\ntable {\n    border-collapse: collapse;\n    border: 2px solid black;\n    margin: 10px 0;\n}\nth, td {\n    border: 1px solid black;\n    padding: 8px;\n    text-align: left;\n}\nth {\n    background-color: #f2f2f2;\n    font-weight: bold;\n}\n</style>";
                    var processedContent = File.ReadAllText(processedFilePath);
                    if (!processedContent.Contains("<style>") && !processedContent.Contains("border-collapse"))
                    {
                        processedContent = processedContent.Replace("</head>", cssTableStyle + "\n</head>");
                        File.WriteAllText(processedFilePath, processedContent);
                    }
                }
                else
                {
                    _logService.LogError($"不支持的输出格式: {format}");
                    return false;
                }

                arguments += " --wrap=preserve";
                _logService.LogInfo($"开始转换: {Path.GetFileName(inputFilePath)} -> {format.ToUpper()}");

                // 增大超时时间以支持较大或复杂的文档
                var timeoutSeconds = 600;
                var (success, output, error) = await ExecuteCommandAsync("pandoc", arguments, timeoutSeconds, processedDir);
                if (success) { _logService.LogInfo($"转换成功！输出文件: {Path.GetFileName(outputFilePath)}"); return true; }
                else { return await HandleConversionError(format, error, outputFilePath); }
            }
            catch (Exception ex)
            {
                _logService.LogError($"转换失败: {ex.Message}");
                return false;
            }
        }

        private System.Diagnostics.ProcessStartInfo CreateProcessStartInfo(string fileName, string arguments, string? workingDirectory = null)
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

        private async Task<(bool success, string output, string error)> ExecuteCommandAsync(string fileName, string arguments, int timeoutSeconds = 10, string? workingDirectory = null)
        {
            try
            {
                var processStartInfo = CreateProcessStartInfo(fileName, arguments, workingDirectory);
                using var process = new Process { StartInfo = processStartInfo };
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

        private async Task<(bool available, string path)> CheckToolAvailabilityAsync(string toolName, string[] commandFormats, string[] commonPaths, string versionArgument = "--version")
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
                    if (AppUtility.CheckFileExists(path))
                    {
                        var (success, output, error) = await ExecuteCommandAsync(path, versionArgument);
                        if (success) return (true, path);
                    }
                }

                _logService.LogWarning($"{toolName}检测失败，请确保已正确安装并添加到PATH环境变量中");
                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                _logService.LogError($"{toolName}检测过程中发生异常: {ex.Message}");
                return (false, string.Empty);
            }
        }

        private async Task<(bool available, string path)> IsPandocAvailableAsync()
        {
            var commandFormats = new[] { "pandoc", "pandoc.exe" };
            var commonPaths = new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Pandoc", "pandoc.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Pandoc", "pandoc.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Pandoc", "pandoc.exe") };
            return await CheckToolAvailabilityAsync("pandoc", commandFormats, commonPaths, "--version");
        }

        private async Task<bool> HandleConversionError(string format, string error, string outputFilePath)
        {
            if (error.Contains("pandoc") && error.Contains("not found"))
            {
                _logService.LogError("pandoc未安装或未添加到系统PATH中");
                return false;
            }

            if (error.Contains("Resource not found") || error.Contains("Could not fetch resource") || error.Contains("replacing image with description"))
            {
                _logService.LogWarning($"图片资源缺失: {error}");
                if (File.Exists(outputFilePath) && new FileInfo(outputFilePath).Length > 0) { _logService.LogInfo($"转换完成: {Path.GetFileName(outputFilePath)}"); return true; }
                else { _logService.LogError("文件生成失败"); await LogDependencyInfo(format); return false; }
            }

            if (error.Contains("Permission denied") || error.Contains("Access denied")) { _logService.LogError("文件访问权限不足"); return false; }
            if (error.Contains("No space left") || error.Contains("disk full")) { _logService.LogError("磁盘空间不足"); return false; }
            if (error.Contains("timeout") || error.Contains("timed out")) { _logService.LogError("转换过程超时"); return false; }
            _logService.LogError($"转换失败: {error}"); await LogDependencyInfo(format); return false;
        }

        private async Task LogDependencyInfo(string format)
        {
            _logService.LogInfo("Pandoc下载地址: https://pandoc.org/installing.html");
            var (available, path) = await IsPandocAvailableAsync();
            _logService.LogInfo($"Pandoc: {(available ? "已安装" : "未安装")}");
        }

        private async Task<string> PreprocessMarkdownFileAsync(string inputFilePath)
        {
            try
            {
                if (!AppUtility.CheckFileExists(inputFilePath, _logService, "预处理文件不存在")) return inputFilePath;
                var content = await File.ReadAllTextAsync(inputFilePath);
                var fixedContent = content;
                fixedContent = FixMathFormulas(fixedContent);
                fixedContent = ConvertHtmlImagesToMarkdown(fixedContent);
                fixedContent = ConvertHtmlTablesToMarkdown(fixedContent);
                if (content == fixedContent) return inputFilePath;
                var tempFilePath = Path.GetTempFileName() + ".md";
                await File.WriteAllTextAsync(tempFilePath, fixedContent);
                _logService.LogInfo("已修复格式问题：数学公式、HTML图片、HTML表格");
                return tempFilePath;
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"预处理失败，使用原文件: {ex.Message}");
                return inputFilePath;
            }
        }

        private string FixMathFormulas(string content)
        {
            var fixedContent = content;
            fixedContent = MyRegex().Replace(fixedContent, m => $"$${m.Groups[1].Value.Trim()}$$");
            fixedContent = Regex.Replace(fixedContent, @"\$\$(.+?)\$\$", m => $"$${m.Groups[1].Value.Trim()}$$", RegexOptions.Singleline);
            fixedContent = FixInlineMathFormulas(fixedContent);
            fixedContent = Regex.Replace(fixedContent, @"\$(.+?)\$", m => $"${m.Groups[1].Value.Trim()}$", RegexOptions.Multiline);
            return fixedContent;
        }

        private string FixInlineMathFormulas(string text)
        {
            var blockMarkers = new List<string>();
            var tempText = Regex.Replace(text, @"\$\$[^$]+\$\$", m => { blockMarkers.Add(m.Value); return $"__BLOCK_MATH_{blockMarkers.Count - 1}__"; }, RegexOptions.Singleline);
            tempText = Regex.Replace(tempText, @"\$\s+([^$]+?)\s+\$", m => $"${m.Groups[1].Value.Trim()}$", RegexOptions.Multiline);
            tempText = Regex.Replace(tempText, @"\$([^$]+?)\$", m => $"${m.Groups[1].Value.Trim()}$", RegexOptions.Multiline);
            for (int i = 0; i < blockMarkers.Count; i++) tempText = tempText.Replace($"__BLOCK_MATH_{i}__", blockMarkers[i]);
            return tempText;
        }

        private string ConvertHtmlImagesToMarkdown(string content)
        {
            var imgPattern = @"<img\s+[^>]*src=[""']([^""']+)[""'][^>]*(?:alt=[""']([^""']*)[""'])?[^>]*(?:width=[""']([^""']*)[""'])?[^>]*?/?\s*>";
            return Regex.Replace(content, imgPattern, m =>
            {
                var src = m.Groups[1].Value;
                var alt = m.Groups[2].Value;
                var width = m.Groups[3].Value;
                if (!src.StartsWith("http") && !src.StartsWith("/") && !Path.IsPathRooted(src)) src = "./" + src;
                var markdownImage = $"![{alt}]({src})";
                if (!string.IsNullOrEmpty(width)) markdownImage = $"![{alt} (宽度: {width})]({src})";
                return markdownImage;
            }, RegexOptions.IgnoreCase);
        }

        private string ConvertHtmlTablesToMarkdown(string content)
        {
            var tablePattern = @"<table[^>]*>(.*?)</table>";
            return Regex.Replace(content, tablePattern, m =>
            {
                var tableHtml = m.Groups[1].Value;
                var rowPattern = @"<tr[^>]*>(.*?)</tr>";
                var rows = Regex.Matches(tableHtml, rowPattern, RegexOptions.Singleline);
                if (rows.Count == 0) return m.Value;
                var markdownRows = new List<string>();
                for (int i = 0; i < rows.Count; i++)
                {
                    var rowHtml = rows[i].Groups[1].Value;
                    var cellPattern = @"<t[dh][^>]*>(.*?)</t[dh]>";
                    var cells = Regex.Matches(rowHtml, cellPattern, RegexOptions.Singleline);
                    var cleanedCells = new List<string>();
                    foreach (Match cell in cells)
                    {
                        var cellContent = cell.Groups[1].Value;
                        cellContent = Regex.Replace(cellContent, @"<[^>]+>", "").Trim();
                        cellContent = System.Net.WebUtility.HtmlDecode(cellContent);
                        cellContent = cellContent.Replace("\n", " ").Replace("\r", "");
                        cleanedCells.Add(string.IsNullOrEmpty(cellContent) ? " " : cellContent);
                    }
                    if (cleanedCells.Count > 0)
                    {
                        var markdownRow = "| " + string.Join(" | ", cleanedCells) + " |";
                        markdownRows.Add(markdownRow);
                        if (i == 0) { var separator = "| " + string.Join(" | ", Enumerable.Repeat("---", cleanedCells.Count)) + " |"; markdownRows.Add(separator); }
                    }
                }
                var result = string.Join("\n", markdownRows);
                return result + "\n\n";
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        [GeneratedRegex(@"\$\$\s+([^$]+?)\s+\$\$", RegexOptions.Multiline)]
        private static partial Regex MyRegex();
    }
}
