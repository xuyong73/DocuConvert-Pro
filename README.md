# DocuConvert Pro

## 项目介绍
DocuConvert Pro 是一款功能强大的文档转换工具，专注于将PDF、图片等文档格式转换为Markdown，同时支持Markdown文件转换为Word、HTML等常用格式。该工具利用 PaddleOCR_VL API 调用来提取图片和PDF中的文本、表格、插图等内容，并通过Pandoc实现高质量的文档格式转换。

## 功能特性

### 1. 文档OCR识别与转换
- 支持PDF文件文本识别与Markdown转换
- 支持多种图片格式（JPG、PNG、BMP、TIFF等）的OCR识别
- 保留文档中的表格、图片等结构信息
- 支持数学公式识别和转换

### 2. Markdown格式转换
- 将Markdown文件转换为Word（.docx）格式
- 将Markdown文件转换为HTML格式
- 保留表格、图片链接、数学公式等内容
- 自动优化输出格式，提升文档可读性

### 3. 用户友好的界面
- 简洁直观的Windows桌面应用界面
- 实时日志显示，便于监控转换进度和结果
- 支持拖放操作，简化文件选择过程
- 命令行参数支持，便于批处理和自动化操作

![](https://github.com/xuyong73/DocuConvert-Pro/blob/main/Main%20Interface.png)

## 系统要求

- **操作系统**：Windows 10/11
- **.NET Framework**：.NET 10.0 - [下载地址](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)
- **内存**：建议 4GB 或以上
- **硬盘空间**：至少 200MB 可用空间
- **依赖软件**：
  - Pandoc（用于Markdown转其他格式）- [下载地址](https://pandoc.org/installing.html)
  - 需要配置有效的OCR API服务端点和密钥

## 安装说明

### 1. 下载主程序
1. 下载最新版本的DocuConvert Pro程序
2. 程序为单文件打包，在任何位置都可以启动应用程序

### 2. 安装依赖软件
1. **安装Pandoc**：
   - 访问 [Pandoc官网](https://pandoc.org/installing.html)
   - 下载适合您系统的安装包
   - 完成安装并确保Pandoc已添加到系统PATH环境变量

### 3. 配置API服务
首次启动程序时，系统会提示您配置OCR API服务：
1. 输入API端点URL
2. 输入API密钥
3. 点击确定保存配置

> **注意**：API配置信息将保存在 `我的文档\DocuConvert Pro\config.ini` 文件中

## 使用方法

### 1. PDF/图片转Markdown
1. 在主界面点击「浏览」按钮选择要转换的PDF或图片文件
2. 系统会自动设置默认输出目录（原文件所在目录下的同名子目录）
3. 点击「开始处理」按钮开始转换
4. 转换完成后，结果将显示在日志窗口中

### 2. Markdown转Word/HTML
1. 在主界面点击「浏览」按钮选择要转换的Markdown文件
2. 点击「开始处理」按钮
3. 在弹出的对话框中选择目标格式（Word或HTML）
4. 点击确定开始转换

## 配置指南

### 编辑API配置
1. 在主界面点击菜单中的「配置」选项
2. 在配置对话框中更新API端点和密钥
3. 点击「确定」保存更改

### 配置文件位置
API配置信息保存在以下位置：
```
我的文档\DocuConvert Pro\config.ini
```

您也可以直接编辑此文件修改配置。配置文件格式：
```
API_URL=<您的API端点URL>
TOKEN=<您的API密钥>
```

## 常见问题

### 1. OCR转换失败
- 检查API配置是否正确
- 确保网络连接正常
- 检查API密钥是否有效

### 2. Markdown转换失败
- 确保已正确安装Pandoc
- 检查Pandoc是否已添加到系统PATH环境变量
- 重新启动应用程序

### 3. 图片资源丢失
- 确保输出目录有写入权限
- 检查原始文档中的图片链接是否有效
- 对于相对路径的图片，建议将图片与Markdown文件放在同一目录

## 错误处理与日志

程序会实时显示处理日志，包括：
- 操作步骤信息
- 错误消息和解决方案提示
- 处理时间统计

日志内容会自动限制大小，只保留最近的记录。
