namespace DocuConvert_Pro.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            mainPanel = new Panel();
            Option = new PictureBox();
            logGroupBox = new GroupBox();
            logTextBox = new TextBox();
            processButton = new Button();
            outputDirButton = new Button();
            outputDirTextBox = new TextBox();
            outputDirLabel = new Label();
            inputFileButton = new Button();
            inputFileTextBox = new TextBox();
            inputFileLabel = new Label();
            titleLabel = new Label();
            progressBar = new ProgressBar();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            statusProgressBar = new ToolStripProgressBar();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)Option).BeginInit();
            logGroupBox.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(Option);
            mainPanel.Controls.Add(logGroupBox);
            mainPanel.Controls.Add(processButton);
            mainPanel.Controls.Add(outputDirButton);
            mainPanel.Controls.Add(outputDirTextBox);
            mainPanel.Controls.Add(outputDirLabel);
            mainPanel.Controls.Add(inputFileButton);
            mainPanel.Controls.Add(inputFileTextBox);
            mainPanel.Controls.Add(inputFileLabel);
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.Padding = new Padding(20);
            mainPanel.Size = new Size(800, 578);
            mainPanel.TabIndex = 0;
            // 
            // Option
            // 
            Option.Cursor = Cursors.Hand;
            Option.Image = (Image)resources.GetObject("Option.Image");
            Option.Location = new Point(217, 28);
            Option.Name = "Option";
            Option.Size = new Size(20, 20);
            Option.SizeMode = PictureBoxSizeMode.StretchImage;
            Option.TabIndex = 10;
            Option.TabStop = false;
            Option.Click += Option_Click;
            // 
            // logGroupBox
            // 
            logGroupBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logGroupBox.Controls.Add(logTextBox);
            logGroupBox.Location = new Point(23, 218);
            logGroupBox.Name = "logGroupBox";
            logGroupBox.Size = new Size(754, 337);
            logGroupBox.TabIndex = 9;
            logGroupBox.TabStop = false;
            logGroupBox.Text = "日志输出";
            // 
            // logTextBox
            // 
            logTextBox.BackColor = SystemColors.Window;
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Font = new Font("Consolas", 9F);
            logTextBox.Location = new Point(3, 19);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(748, 315);
            logTextBox.TabIndex = 0;
            // 
            // processButton
            // 
            processButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            processButton.BackColor = Color.DodgerBlue;
            processButton.FlatStyle = FlatStyle.Flat;
            processButton.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            processButton.ForeColor = Color.White;
            processButton.Location = new Point(623, 160);
            processButton.Name = "processButton";
            processButton.Size = new Size(154, 40);
            processButton.TabIndex = 7;
            processButton.Text = "开始处理";
            processButton.UseVisualStyleBackColor = false;
            processButton.Click += ProcessButton_Click;
            // 
            // outputDirButton
            // 
            outputDirButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            outputDirButton.Location = new Point(723, 120);
            outputDirButton.Name = "outputDirButton";
            outputDirButton.Size = new Size(54, 25);
            outputDirButton.TabIndex = 6;
            outputDirButton.Text = "浏览";
            outputDirButton.UseVisualStyleBackColor = true;
            outputDirButton.Click += OutputDirButton_Click;
            // 
            // outputDirTextBox
            // 
            outputDirTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            outputDirTextBox.Location = new Point(120, 120);
            outputDirTextBox.Name = "outputDirTextBox";
            outputDirTextBox.ReadOnly = true;
            outputDirTextBox.Size = new Size(597, 23);
            outputDirTextBox.TabIndex = 5;
            // 
            // outputDirLabel
            // 
            outputDirLabel.AutoSize = true;
            outputDirLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            outputDirLabel.Location = new Point(23, 122);
            outputDirLabel.Name = "outputDirLabel";
            outputDirLabel.Size = new Size(68, 17);
            outputDirLabel.TabIndex = 4;
            outputDirLabel.Text = "输出目录：";
            // 
            // inputFileButton
            // 
            inputFileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            inputFileButton.Location = new Point(723, 80);
            inputFileButton.Name = "inputFileButton";
            inputFileButton.Size = new Size(54, 25);
            inputFileButton.TabIndex = 3;
            inputFileButton.Text = "浏览";
            inputFileButton.UseVisualStyleBackColor = true;
            inputFileButton.Click += InputFileButton_Click;
            // 
            // inputFileTextBox
            // 
            inputFileTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            inputFileTextBox.Location = new Point(120, 80);
            inputFileTextBox.Name = "inputFileTextBox";
            inputFileTextBox.ReadOnly = true;
            inputFileTextBox.Size = new Size(597, 23);
            inputFileTextBox.TabIndex = 2;
            // 
            // inputFileLabel
            // 
            inputFileLabel.AutoSize = true;
            inputFileLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            inputFileLabel.Location = new Point(23, 82);
            inputFileLabel.Name = "inputFileLabel";
            inputFileLabel.Size = new Size(68, 17);
            inputFileLabel.TabIndex = 1;
            inputFileLabel.Text = "输入文件：";
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
            titleLabel.ForeColor = Color.DodgerBlue;
            titleLabel.Location = new Point(23, 20);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(193, 30);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "文件OCR转换工具";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(23, 210);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(754, 23);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.TabIndex = 8;
            progressBar.Visible = false;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, statusProgressBar });
            statusStrip.Location = new Point(0, 578);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(800, 22);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Padding = new Padding(0, 0, 20, 0);
            statusLabel.Size = new Size(76, 17);
            statusLabel.Text = "准备就绪";
            // 
            // statusProgressBar
            // 
            statusProgressBar.Name = "statusProgressBar";
            statusProgressBar.Size = new Size(300, 16);
            statusProgressBar.Style = ProgressBarStyle.Marquee;
            statusProgressBar.Visible = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(800, 600);
            Controls.Add(mainPanel);
            Controls.Add(statusStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(600, 500);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "文件OCR转换工具 v2.5";
            mainPanel.ResumeLayout(false);
            mainPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)Option).EndInit();
            logGroupBox.ResumeLayout(false);
            logGroupBox.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private Panel mainPanel;
        private StatusStrip statusStrip;
        private ToolStripProgressBar statusProgressBar;
        private ToolStripStatusLabel statusLabel;
        private Label titleLabel;
        private Label inputFileLabel;
        private TextBox inputFileTextBox;
        private Button inputFileButton;
        private Button outputDirButton;
        private TextBox outputDirTextBox;
        private Label outputDirLabel;
        private Button processButton;
        private ProgressBar progressBar;
        private GroupBox logGroupBox;
        private TextBox logTextBox;
        private PictureBox Option;
    }
}