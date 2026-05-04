using System.Drawing;
using System.Windows.Forms;

namespace FifoWatch.Forms
{
    partial class ConfigureFifoForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label   lblName;
        private TextBox txtName;
        private Label   lblArray;
        private TextBox txtArrayTag;
        private Button  btnBrowseArray;
        private Button  btnClearArray;
        private Label   lblHead;
        private TextBox txtHeadTag;
        private Button  btnBrowseHead;
        private Button  btnClearHead;
        private Label   lblTail;
        private TextBox txtTailTag;
        private Button  btnBrowseTail;
        private Button  btnClearTail;
        private Label   lblCount;
        private TextBox txtCountTag;
        private Button  btnBrowseCount;
        private Button  btnClearCount;
        private Label   lblMaxRecords;
        private TextBox txtMaxRecordsTag;
        private Button  btnBrowseMaxRecords;
        private Button  btnClearMaxRecords;
        private Button  btnAutoDetect;
        private Button  btnOK;
        private Button  btnCancel;
        private Panel   pnlRows;

        private void InitializeComponent()
        {
            this.lblName            = new Label();
            this.txtName            = new TextBox();
            this.lblArray           = new Label();
            this.txtArrayTag        = new TextBox();
            this.btnBrowseArray     = new Button();
            this.btnClearArray      = new Button();
            this.lblHead            = new Label();
            this.txtHeadTag         = new TextBox();
            this.btnBrowseHead      = new Button();
            this.btnClearHead       = new Button();
            this.lblTail            = new Label();
            this.txtTailTag         = new TextBox();
            this.btnBrowseTail      = new Button();
            this.btnClearTail       = new Button();
            this.lblCount           = new Label();
            this.txtCountTag        = new TextBox();
            this.btnBrowseCount     = new Button();
            this.btnClearCount      = new Button();
            this.lblMaxRecords      = new Label();
            this.txtMaxRecordsTag   = new TextBox();
            this.btnBrowseMaxRecords = new Button();
            this.btnClearMaxRecords  = new Button();
            this.btnAutoDetect = new Button();
            this.btnOK         = new Button();
            this.btnCancel          = new Button();
            this.pnlRows            = new Panel();

            this.pnlRows.SuspendLayout();
            this.SuspendLayout();

            // Panel for tag rows (used as GroupBox interior without border)
            this.pnlRows.Location = new Point(0, 0);
            this.pnlRows.Size     = new Size(680, 200);

            // Name row (above panel)
            this.lblName.Text     = "Name:";
            this.lblName.AutoSize = true;
            this.lblName.Location = new Point(12, 16);

            this.txtName.Location = new Point(156, 13);
            this.txtName.Width    = 240;

            // Tag rows inside pnlRows
            FormHelpers.AddSelectionRow(this.pnlRows,
                this.lblArray,    "Array (required):",
                this.txtArrayTag, this.btnBrowseArray, this.btnClearArray,
                row: 0,
                browseHandler: this.btnBrowseArray_Click,
                clearHandler:  this.btnClearArray_Click);

            FormHelpers.AddSelectionRow(this.pnlRows,
                this.lblHead,    "NextIndexToRead:",
                this.txtHeadTag, this.btnBrowseHead, this.btnClearHead,
                row: 1,
                browseHandler: this.btnBrowseHead_Click,
                clearHandler:  this.btnClearHead_Click);

            FormHelpers.AddSelectionRow(this.pnlRows,
                this.lblTail,    "NextIndexToWrite:",
                this.txtTailTag, this.btnBrowseTail, this.btnClearTail,
                row: 2,
                browseHandler: this.btnBrowseTail_Click,
                clearHandler:  this.btnClearTail_Click);

            FormHelpers.AddSelectionRow(this.pnlRows,
                this.lblCount,    "RecordsStored:",
                this.txtCountTag, this.btnBrowseCount, this.btnClearCount,
                row: 3,
                browseHandler: this.btnBrowseCount_Click,
                clearHandler:  this.btnClearCount_Click);

            FormHelpers.AddSelectionRow(this.pnlRows,
                this.lblMaxRecords,    "MaxNrOfRecords:",
                this.txtMaxRecordsTag, this.btnBrowseMaxRecords, this.btnClearMaxRecords,
                row: 4,
                browseHandler: this.btnBrowseMaxRecords_Click,
                clearHandler:  this.btnClearMaxRecords_Click);

            // Auto-detect button (right-aligned, below last row)
            this.btnAutoDetect.Text     = "Auto-detect header";
            this.btnAutoDetect.Width    = 140;
            this.btnAutoDetect.Height   = 22;
            this.btnAutoDetect.Location = new Point(520, 22 + 5 * 30);
            this.btnAutoDetect.Click   += new System.EventHandler(this.btnAutoDetect_Click);
            this.pnlRows.Controls.Add(this.btnAutoDetect);

            // Place panel below name row
            this.pnlRows.Location = new Point(0, 44);

            // OK / Cancel
            this.btnOK.Text     = "OK";
            this.btnOK.Width    = 80;
            this.btnOK.Height   = 26;
            this.btnOK.Location = new Point(510, 262);
            this.btnOK.Click   += new System.EventHandler(this.btnOK_Click);

            this.btnCancel.Text     = "Cancel";
            this.btnCancel.Width    = 80;
            this.btnCancel.Height   = 26;
            this.btnCancel.Location = new Point(596, 262);
            this.btnCancel.Click   += new System.EventHandler(this.btnCancel_Click);

            // Form
            this.Text            = "Add FIFO Monitor";
            this.ClientSize      = new Size(690, 302);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.AcceptButton    = this.btnOK;
            this.CancelButton    = this.btnCancel;

            this.Controls.AddRange(new Control[] {
                this.lblName, this.txtName,
                this.pnlRows,
                this.btnOK, this.btnCancel
            });

            this.pnlRows.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
