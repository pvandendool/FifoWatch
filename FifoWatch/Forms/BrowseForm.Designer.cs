using System.Drawing;
using System.Windows.Forms;

namespace FifoWatch.Forms
{
    partial class BrowseForm
    {
        private System.ComponentModel.IContainer components = null;

        private TreeView treeView;
        private TextBox  txtSearch;
        private Label    lblStatus;
        private Button   btnOK;
        private Button   btnCancel;

        private void InitializeComponent()
        {
            this.treeView  = new TreeView();
            this.txtSearch = new TextBox();
            this.lblStatus = new Label();
            this.btnOK     = new Button();
            this.btnCancel = new Button();

            this.SuspendLayout();

            // Search panel at the top
            var pnlSearch = new Panel();
            pnlSearch.Dock    = DockStyle.Top;
            pnlSearch.Height  = 32;
            pnlSearch.Padding = new Padding(4, 4, 4, 0);

            var lblSearch = new Label();
            lblSearch.Text     = "Search:";
            lblSearch.AutoSize = true;
            lblSearch.Location = new Point(4, 8);

            this.txtSearch.Location    = new Point(56, 4);
            this.txtSearch.Width       = 430;
            this.txtSearch.Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(this.txtSearch);

            // treeView — fills the centre
            this.treeView.Dock         = DockStyle.Fill;
            this.treeView.Font         = new Font("Consolas", 9f);
            this.treeView.HideSelection = false;
            this.treeView.BeforeExpand += new TreeViewCancelEventHandler(this.treeView_BeforeExpand);
            this.treeView.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);

            // lblStatus — single-line status at the bottom, above the buttons
            this.lblStatus.Dock      = DockStyle.Bottom;
            this.lblStatus.Height    = 20;
            this.lblStatus.ForeColor = Color.Gray;
            this.lblStatus.Font      = new Font(SystemFonts.DefaultFont.FontFamily, 8f);
            this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.lblStatus.Padding   = new Padding(2, 0, 0, 0);
            this.lblStatus.Text      = "Expand a datablock to browse its variables.";

            // Buttons
            this.btnOK.Text     = "OK";
            this.btnOK.Width    = 75;
            this.btnOK.Height   = 28;
            this.btnOK.Anchor   = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnOK.Location = new Point(319, 7);
            this.btnOK.Click   += new System.EventHandler(this.btnOK_Click);

            this.btnCancel.Text        = "Cancel";
            this.btnCancel.Width       = 75;
            this.btnCancel.Height      = 28;
            this.btnCancel.Anchor      = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnCancel.Location    = new Point(238, 7);
            this.btnCancel.DialogResult = DialogResult.Cancel;

            var pnlButtons = new Panel();
            pnlButtons.Dock   = DockStyle.Bottom;
            pnlButtons.Height = 42;
            pnlButtons.Controls.Add(this.btnOK);
            pnlButtons.Controls.Add(this.btnCancel);

            // BrowseForm
            this.Text          = "Select PLC Variable";
            this.ClientSize    = new Size(500, 580);
            this.MinimumSize   = new Size(380, 380);
            this.MinimizeBox   = false;
            this.AcceptButton  = this.btnOK;
            this.CancelButton  = this.btnCancel;
            this.StartPosition = FormStartPosition.CenterParent;

            this.Controls.Add(this.treeView);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(pnlButtons);

            this.ResumeLayout(false);
        }
    }
}
