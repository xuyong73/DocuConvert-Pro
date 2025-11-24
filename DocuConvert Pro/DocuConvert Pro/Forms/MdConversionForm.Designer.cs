namespace DocuConvert_Pro.Forms
{
    partial class MdConversionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MdConversionForm));
            panel1 = new Panel();
            cancelButton = new Button();
            htmlButton = new Button();
            wordButton = new Button();
            label1 = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.White;
            panel1.Controls.Add(cancelButton);
            panel1.Controls.Add(htmlButton);
            panel1.Controls.Add(wordButton);
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(20);
            panel1.Size = new Size(450, 200);
            panel1.TabIndex = 0;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom;
            cancelButton.BackColor = Color.Gray;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.Font = new Font("Microsoft YaHei UI", 9F);
            cancelButton.ForeColor = Color.White;
            cancelButton.Location = new Point(307, 80);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(120, 40);
            cancelButton.TabIndex = 4;
            cancelButton.Text = "取消";
            cancelButton.UseVisualStyleBackColor = false;
            cancelButton.Click += cancelButton_Click;
            // 
            // htmlButton
            // 
            htmlButton.Anchor = AnchorStyles.Top;
            htmlButton.BackColor = Color.MediumSeaGreen;
            htmlButton.FlatStyle = FlatStyle.Flat;
            htmlButton.Font = new Font("Microsoft YaHei UI", 9F);
            htmlButton.ForeColor = Color.White;
            htmlButton.Location = new Point(168, 80);
            htmlButton.Name = "htmlButton";
            htmlButton.Size = new Size(120, 40);
            htmlButton.TabIndex = 3;
            htmlButton.Text = "转换为HTML";
            htmlButton.UseVisualStyleBackColor = false;
            htmlButton.Click += htmlButton_Click;
            // 
            // wordButton
            // 
            wordButton.Anchor = AnchorStyles.Top;
            wordButton.BackColor = Color.RoyalBlue;
            wordButton.FlatStyle = FlatStyle.Flat;
            wordButton.Font = new Font("Microsoft YaHei UI", 9F);
            wordButton.ForeColor = Color.White;
            wordButton.Location = new Point(25, 80);
            wordButton.Name = "wordButton";
            wordButton.Size = new Size(120, 40);
            wordButton.TabIndex = 1;
            wordButton.Text = "转换为Word";
            wordButton.UseVisualStyleBackColor = false;
            wordButton.Click += wordButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            label1.Location = new Point(25, 25);
            label1.Name = "label1";
            label1.Size = new Size(263, 22);
            label1.TabIndex = 0;
            label1.Text = "请选择MD文件要转换的目标格式：";
            // 
            // MdConversionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(450, 200);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MdConversionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "MD文件转换";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button htmlButton;
        private System.Windows.Forms.Button wordButton;
        private System.Windows.Forms.Label label1;
    }
}