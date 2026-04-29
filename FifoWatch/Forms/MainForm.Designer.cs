using System.Drawing;
using System.Windows.Forms;

namespace FifoWatch.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Connection controls
        private GroupBox     grpConnection;
        private Label        lblIp;
        private TextBox      txtIp;
        private Label        lblUser;
        private TextBox      txtUser;
        private Label        lblPass;
        private TextBox      txtPass;
        private Button       btnConnect;
        private Button       btnDisconnect;
        private Label        lblConnectionStatus;

        // FIFO selection controls
        private GroupBox     grpFifo;
        private Label        lblArray;
        private TextBox      txtArrayTag;
        private Button       btnBrowseArray;
        private Button       btnClearArray;
        private Label        lblHead;
        private TextBox      txtHeadTag;
        private Button       btnBrowseHead;
        private Button       btnClearHead;
        private Label        lblTail;
        private TextBox      txtTailTag;
        private Button       btnBrowseTail;
        private Button       btnClearTail;
        private Label        lblCount;
        private TextBox      txtCountTag;
        private Button       btnBrowseCount;
        private Button       btnClearCount;
        private Label        lblMaxRecords;
        private TextBox      txtMaxRecordsTag;
        private Button       btnBrowseMaxRecords;
        private Button       btnClearMaxRecords;
        private Button       btnAutoDetect;

        // Live pointer display
        private Label        lblLivePointers;

        // Grid
        private DataGridView dgvFifo;

        // Bottom panel
        private Panel        pnlBottom;
        private Button       btnStart;
        private Button       btnStop;
        private Label        lblIntervalLabel;
        private TextBox      txtInterval;
        private Label        lblLastRead;

        // Status strip
        private StatusStrip          statusStrip;
        private ToolStripStatusLabel sslStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ---- Connection GroupBox ----
            this.lblIp                = new Label();
            this.txtIp                = new TextBox();
            this.lblUser              = new Label();
            this.txtUser              = new TextBox();
            this.lblPass              = new Label();
            this.txtPass              = new TextBox();
            this.btnConnect           = new Button();
            this.btnDisconnect        = new Button();
            this.lblConnectionStatus  = new Label();
            this.grpConnection        = new GroupBox();

            // ---- FIFO Selection GroupBox ----
            this.lblArray     = new Label();
            this.txtArrayTag  = new TextBox();
            this.btnBrowseArray = new Button();
            this.btnClearArray  = new Button();
            this.lblHead      = new Label();
            this.txtHeadTag   = new TextBox();
            this.btnBrowseHead  = new Button();
            this.btnClearHead   = new Button();
            this.lblTail      = new Label();
            this.txtTailTag   = new TextBox();
            this.btnBrowseTail  = new Button();
            this.btnClearTail   = new Button();
            this.lblCount     = new Label();
            this.txtCountTag  = new TextBox();
            this.btnBrowseCount = new Button();
            this.btnClearCount  = new Button();
            this.lblMaxRecords     = new Label();
            this.txtMaxRecordsTag  = new TextBox();
            this.btnBrowseMaxRecords = new Button();
            this.btnClearMaxRecords  = new Button();
            this.btnAutoDetect       = new Button();
            this.grpFifo      = new GroupBox();

            // ---- Grid ----
            this.dgvFifo = new DataGridView();

            // ---- Bottom panel ----
            this.btnStart        = new Button();
            this.btnStop         = new Button();
            this.lblIntervalLabel = new Label();
            this.txtInterval     = new TextBox();
            this.lblLastRead     = new Label();
            this.pnlBottom       = new Panel();

            // ---- Status strip ----
            this.sslStatus   = new ToolStripStatusLabel();
            this.statusStrip = new StatusStrip();

            this.grpConnection.SuspendLayout();
            this.grpFifo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.dgvFifo).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();

            // ===== Connection GroupBox =====
            this.grpConnection.Text     = "Connection";
            this.grpConnection.Dock     = DockStyle.Top;
            this.grpConnection.Height   = 78;
            this.grpConnection.Padding  = new Padding(8, 4, 8, 4);

            int cx = 8;

            this.lblIp.Text     = "IP / Host:";
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new Point(cx, 24);

            this.txtIp.Text     = "10.20.8.5";
            this.txtIp.Width    = 130;
            this.txtIp.Location = new Point(cx + 64, 21);

            cx += 64 + 130 + 8;

            this.lblUser.Text     = "User:";
            this.lblUser.AutoSize = true;
            this.lblUser.Location = new Point(cx, 24);

            this.txtUser.Width    = 90;
            this.txtUser.Location = new Point(cx + 40, 21);

            cx += 40 + 90 + 8;

            this.lblPass.Text     = "Password:";
            this.lblPass.AutoSize = true;
            this.lblPass.Location = new Point(cx, 24);

            this.txtPass.UseSystemPasswordChar = true;
            this.txtPass.Width    = 90;
            this.txtPass.Location = new Point(cx + 68, 21);

            cx += 68 + 90 + 12;

            this.btnConnect.Text     = "Connect";
            this.btnConnect.Width    = 80;
            this.btnConnect.Height   = 26;
            this.btnConnect.Location = new Point(cx, 20);
            this.btnConnect.Click   += new System.EventHandler(this.btnConnect_Click);

            cx += 80 + 6;

            this.btnDisconnect.Text     = "Disconnect";
            this.btnDisconnect.Width    = 80;
            this.btnDisconnect.Height   = 26;
            this.btnDisconnect.Location = new Point(cx, 20);
            this.btnDisconnect.Enabled  = false;
            this.btnDisconnect.Click   += new System.EventHandler(this.btnDisconnect_Click);

            cx += 80 + 12;

            this.lblConnectionStatus.Text      = "Disconnected";
            this.lblConnectionStatus.AutoSize  = true;
            this.lblConnectionStatus.ForeColor = Color.Red;
            this.lblConnectionStatus.Font      = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
            this.lblConnectionStatus.Location  = new Point(cx, 24);

            this.grpConnection.Controls.AddRange(new Control[] {
                this.lblIp, this.txtIp,
                this.lblUser, this.txtUser,
                this.lblPass, this.txtPass,
                this.btnConnect, this.btnDisconnect,
                this.lblConnectionStatus
            });

            // ===== FIFO Selection GroupBox =====
            this.grpFifo.Text    = "FIFO Selection";
            this.grpFifo.Dock    = DockStyle.Top;
            this.grpFifo.Height  = 240;
            this.grpFifo.Padding = new Padding(8, 4, 8, 4);

            AddSelectionRow(this.grpFifo,
                this.lblArray,    "Array (required):",
                this.txtArrayTag, this.btnBrowseArray, this.btnClearArray,
                row: 0,
                browseHandler: this.btnBrowseArray_Click,
                clearHandler:  this.btnClearArray_Click);

            AddSelectionRow(this.grpFifo,
                this.lblHead,    "NextIndexToRead:",
                this.txtHeadTag, this.btnBrowseHead, this.btnClearHead,
                row: 1,
                browseHandler: this.btnBrowseHead_Click,
                clearHandler:  this.btnClearHead_Click);

            AddSelectionRow(this.grpFifo,
                this.lblTail,    "NextIndexToWrite:",
                this.txtTailTag, this.btnBrowseTail, this.btnClearTail,
                row: 2,
                browseHandler: this.btnBrowseTail_Click,
                clearHandler:  this.btnClearTail_Click);

            AddSelectionRow(this.grpFifo,
                this.lblCount,    "RecordsStored:",
                this.txtCountTag, this.btnBrowseCount, this.btnClearCount,
                row: 3,
                browseHandler: this.btnBrowseCount_Click,
                clearHandler:  this.btnClearCount_Click);

            AddSelectionRow(this.grpFifo,
                this.lblMaxRecords,    "MaxNrOfRecords:",
                this.txtMaxRecordsTag, this.btnBrowseMaxRecords, this.btnClearMaxRecords,
                row: 4,
                browseHandler: this.btnBrowseMaxRecords_Click,
                clearHandler:  this.btnClearMaxRecords_Click);

            // Auto-detect button — fills header fields from same DB as selected array
            this.btnAutoDetect.Text     = "Auto-detect header";
            this.btnAutoDetect.Width    = 140;
            this.btnAutoDetect.Height   = 22;
            this.btnAutoDetect.Location = new Point(608, 22 + 5 * 30);
            this.btnAutoDetect.Enabled  = false;
            this.btnAutoDetect.Click   += new System.EventHandler(this.btnAutoDetect_Click);
            this.grpFifo.Controls.Add(this.btnAutoDetect);

            // Live pointer values label (below all rows, read-only display)
            this.lblLivePointers = new Label();
            this.lblLivePointers.Text      = "NextIndexToRead: —   NextIndexToWrite: —   RecordsStored: —   MaxNrOfRecords: —";
            this.lblLivePointers.AutoSize  = false;
            this.lblLivePointers.Location  = new Point(10, 195);
            this.lblLivePointers.Size      = new Size(920, 20);
            this.lblLivePointers.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblLivePointers.Font      = new Font("Consolas", 9f, FontStyle.Bold);
            this.grpFifo.Controls.Add(this.lblLivePointers);

            // ===== DataGridView =====
            this.dgvFifo.Dock                  = DockStyle.Fill;
            this.dgvFifo.ReadOnly              = true;
            this.dgvFifo.AllowUserToAddRows    = false;
            this.dgvFifo.AllowUserToDeleteRows = false;
            this.dgvFifo.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvFifo.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            this.dgvFifo.RowHeadersVisible     = false;
            this.dgvFifo.BackgroundColor       = SystemColors.Window;
            this.dgvFifo.BorderStyle           = BorderStyle.None;

            // ===== Bottom Panel =====
            this.btnStart.Text     = "▶ Start";
            this.btnStart.Width    = 80;
            this.btnStart.Height   = 26;
            this.btnStart.Location = new Point(8, 7);
            this.btnStart.Click   += new System.EventHandler(this.btnStart_Click);

            this.btnStop.Text     = "■ Stop";
            this.btnStop.Width    = 70;
            this.btnStop.Height   = 26;
            this.btnStop.Enabled  = false;
            this.btnStop.Location = new Point(94, 7);
            this.btnStop.Click   += new System.EventHandler(this.btnStop_Click);

            this.lblIntervalLabel.Text     = "Interval (ms):";
            this.lblIntervalLabel.AutoSize = true;
            this.lblIntervalLabel.Location = new Point(174, 12);

            this.txtInterval.Text     = "500";
            this.txtInterval.Width    = 60;
            this.txtInterval.Location = new Point(268, 9);

            this.lblLastRead.AutoSize = true;
            this.lblLastRead.Location = new Point(340, 12);
            this.lblLastRead.Text     = string.Empty;

            this.pnlBottom.Dock   = DockStyle.Bottom;
            this.pnlBottom.Height = 40;
            this.pnlBottom.Controls.AddRange(new Control[] {
                this.btnStart, this.btnStop,
                this.lblIntervalLabel, this.txtInterval,
                this.lblLastRead
            });

            // ===== Status Strip =====
            this.sslStatus.Spring = true;
            this.sslStatus.Text   = "Ready";
            this.sslStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.statusStrip.Items.Add(this.sslStatus);
            this.statusStrip.SizingGrip = false;

            // ===== MainForm =====
            this.Text         = "FifoWatch — PLC FIFO Viewer";
            this.ClientSize   = new Size(960, 680);
            this.MinimumSize  = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Controls.Add(this.dgvFifo);
            this.Controls.Add(this.grpFifo);
            this.Controls.Add(this.grpConnection);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.statusStrip);

            this.grpConnection.ResumeLayout(false);
            this.grpFifo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.dgvFifo).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private static void AddSelectionRow(
            GroupBox group,
            Label lbl,       string labelText,
            TextBox txt,
            Button btnBrowse, Button btnClear,
            int row,
            System.EventHandler browseHandler,
            System.EventHandler clearHandler)
        {
            const int rowH = 30;
            const int top  = 22;
            int y = top + row * rowH;

            lbl.Text     = labelText;
            lbl.AutoSize = true;
            lbl.Location = new Point(8, y + 4);
            lbl.Width    = 110;

            txt.Location = new Point(122, y);
            txt.Width    = 480;
            txt.ReadOnly = true;
            txt.BackColor = System.Drawing.SystemColors.Window;

            btnBrowse.Text     = "Browse...";
            btnBrowse.Width    = 75;
            btnBrowse.Height   = 22;
            btnBrowse.Location = new Point(608, y);
            btnBrowse.Click   += browseHandler;

            btnClear.Text     = "Clear";
            btnClear.Width    = 50;
            btnClear.Height   = 22;
            btnClear.Location = new Point(689, y);
            btnClear.Click   += clearHandler;

            group.Controls.AddRange(new Control[] { lbl, txt, btnBrowse, btnClear });
        }
    }
}
