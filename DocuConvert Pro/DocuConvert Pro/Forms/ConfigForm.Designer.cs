namespace DocuConvert_Pro.Forms
{
    partial class ConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
            tableLayoutPanel1 = new TableLayoutPanel();
            label1 = new Label();
            linkLabel1 = new LinkLabel();
            label2 = new Label();
            apiUrlTextBox = new TextBox();
            label3 = new Label();
            tokenTextBox = new TextBox();
            panel1 = new Panel();
            okButton = new Button();
            cancelButton = new Button();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(linkLabel1, 0, 1);
            tableLayoutPanel1.Controls.Add(label2, 0, 2);
            tableLayoutPanel1.Controls.Add(apiUrlTextBox, 0, 3);
            tableLayoutPanel1.Controls.Add(label3, 0, 4);
            tableLayoutPanel1.Controls.Add(tokenTextBox, 0, 5);
            tableLayoutPanel1.Controls.Add(panel1, 0, 6);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(20);
            tableLayoutPanel1.RowCount = 7;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(500, 261);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 9F);
            label1.Location = new Point(23, 20);
            label1.Margin = new Padding(3, 0, 3, 5);
            label1.Name = "label1";
            label1.Size = new Size(316, 17);
            label1.TabIndex = 0;
            label1.Text = "请前往百度飞桨平台获取PaddleOCR-VL模型的API配置：";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Font = new Font("Microsoft YaHei UI", 9F);
            linkLabel1.LinkBehavior = LinkBehavior.HoverUnderline;
            linkLabel1.Location = new Point(23, 42);
            linkLabel1.Margin = new Padding(3, 0, 3, 15);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(225, 17);
            linkLabel1.TabIndex = 1;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "https://aistudio.baidu.com/paddleocr";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            label2.Location = new Point(23, 74);
            label2.Margin = new Padding(3, 0, 3, 5);
            label2.Name = "label2";
            label2.Size = new Size(69, 17);
            label2.TabIndex = 2;
            label2.Text = "API URL：";
            // 
            // apiUrlTextBox
            // 
            apiUrlTextBox.Dock = DockStyle.Fill;
            apiUrlTextBox.Font = new Font("Microsoft YaHei UI", 9F);
            apiUrlTextBox.Location = new Point(23, 96);
            apiUrlTextBox.Margin = new Padding(3, 0, 3, 15);
            apiUrlTextBox.Name = "apiUrlTextBox";
            apiUrlTextBox.Size = new Size(454, 23);
            apiUrlTextBox.TabIndex = 0;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            label3.Location = new Point(23, 134);
            label3.Margin = new Padding(3, 0, 3, 5);
            label3.Name = "label3";
            label3.Size = new Size(58, 17);
            label3.TabIndex = 4;
            label3.Text = "Token：";
            // 
            // tokenTextBox
            // 
            tokenTextBox.Dock = DockStyle.Fill;
            tokenTextBox.Font = new Font("Microsoft YaHei UI", 9F);
            tokenTextBox.Location = new Point(23, 156);
            tokenTextBox.Margin = new Padding(3, 0, 3, 15);
            tokenTextBox.Name = "tokenTextBox";
            tokenTextBox.Size = new Size(454, 23);
            tokenTextBox.TabIndex = 1;
            // 
            // panel1
            // 
            panel1.Controls.Add(okButton);
            panel1.Controls.Add(cancelButton);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(23, 197);
            panel1.Name = "panel1";
            panel1.Size = new Size(454, 41);
            panel1.TabIndex = 6;
            // 
            // okButton
            // 
            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(63, 15);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 23);
            okButton.TabIndex = 2;
            okButton.Text = "保存";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += okButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(288, 15);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.TabIndex = 3;
            cancelButton.Text = "取消";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // ConfigForm
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(500, 261);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConfigForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PaddleOCR-VL 配置";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox apiUrlTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tokenTextBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}