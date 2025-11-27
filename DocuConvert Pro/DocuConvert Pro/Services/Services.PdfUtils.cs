using System;
using System.Collections.Generic;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DocuConvert_Pro.Services
{
    public class PdfSplitterService
    {
        // 每页限制，根据API要求设置为100页
        public const int MaxPagesPerSplit = 100;
        
        public static List<string> SplitPdfByPages(string inputPdfPath, string outputDirectory)
        {
            if (!File.Exists(inputPdfPath))
                throw new FileNotFoundException("源PDF文件不存在", inputPdfPath);
            
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            
            var resultFiles = new List<string>();
            string baseFileName = Path.GetFileNameWithoutExtension(inputPdfPath);
            int fileIndex = 1;
            
            // 获取PDF页数
            int pageCount = GetPdfPageCount(inputPdfPath);
            
            // 打开源PDF文件
            using (PdfDocument inputDocument = PdfReader.Open(inputPdfPath, PdfDocumentOpenMode.Import))
            {
                
                // 每 MaxPagesPerSplit 页循环一次
                for (int startPageIndex = 0; startPageIndex < pageCount; startPageIndex += MaxPagesPerSplit)
                {
                    // 创建新的输出文档
                    using (PdfDocument outputDocument = new PdfDocument())
                    {
                        // 计算当前分割文件的结束页
                        int endPageIndex = Math.Min(startPageIndex + MaxPagesPerSplit, pageCount);
                        
                        // 将对应页面加入到新文档
                        for (int i = startPageIndex; i < endPageIndex; i++)
                        {
                            outputDocument.AddPage(inputDocument.Pages[i]);
                        }
                        
                        // 保存新文档
                        string outputFilePath = Path.Combine(outputDirectory, $"{baseFileName}_part_{fileIndex}.pdf");
                        outputDocument.Save(outputFilePath);
                        resultFiles.Add(outputFilePath);
                        fileIndex++;
                    }
                }
            }
            
            return resultFiles;
        }
        
        public static int GetPdfPageCount(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("PDF文件不存在", filePath);
            
            using (PdfDocument pdfDoc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import))
            {
                return pdfDoc.PageCount;
            }
        }
        
        public bool IsPdfFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;
            
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".pdf";
        }
    }
}