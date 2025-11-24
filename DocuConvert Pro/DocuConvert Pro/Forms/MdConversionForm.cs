namespace DocuConvert_Pro.Forms
{
    public partial class MdConversionForm : Form
    {
        public string SelectedFormat { get; private set; } = string.Empty;

        public MdConversionForm()
        {
            InitializeComponent();
        }

        private void wordButton_Click(object sender, EventArgs e)
        {
            SelectedFormat = "docx";
            DialogResult = DialogResult.OK;
            Close();
        }

        private void htmlButton_Click(object sender, EventArgs e)
        {
            SelectedFormat = "html";
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}