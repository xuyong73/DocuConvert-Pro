using System.ComponentModel;
using DocuConvert_Pro.Services;

namespace DocuConvert_Pro.Forms
{
    public partial class ConfigForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string ApiUrl { get; set; } = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string Token { get; set; } = string.Empty;

        private readonly IConfigService? _configService;

        public ConfigForm()
        {
            InitializeComponent();
        }

        public ConfigForm(string apiUrl, string token, IConfigService configService) : this()
        {
            ApiUrl = apiUrl;
            Token = token;
            _configService = configService;

            apiUrlTextBox?.Text = apiUrl;

            tokenTextBox?.Text = token;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "https://aistudio.baidu.com/paddleocr");
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            ApiUrl = apiUrlTextBox.Text.Trim();
            Token = tokenTextBox.Text.Trim();

            if (string.IsNullOrEmpty(ApiUrl) || string.IsNullOrEmpty(Token))
            {
                MessageBox.Show("API URL和Token不能为空", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            // 保存配置到文件
            try
            {
                // 使用ConfigService的SaveConfig方法保存配置
                if (_configService != null)
                {
                    if (!_configService.SaveConfig(ApiUrl, Token))
                    {
                        throw new Exception("保存配置失败");
                    }
                }
                else
                    throw new InvalidOperationException("ConfigService未正确初始化");

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置文件失败: {ex.Message}", "保存错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }
}