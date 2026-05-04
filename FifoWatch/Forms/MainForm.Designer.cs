using System.Drawing;
using System.Windows.Forms;

namespace FifoWatch.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Connection controls
        private GroupBox             grpConnection;
        private Label                lblIp;
        private TextBox              txtIp;
        private Label                lblUser;
        private TextBox              txtUser;
        private Label                lblPass;
        private TextBox              txtPass;
        private Button               btnConnect;
        private Button               btnDisconnect;
        private Label                lblConnectionStatus;

        // Layout
        private SplitContainer       splitMain;

        // Left panel
        private ListView             listMonitors;
        private ColumnHeader         colStatus;
        private ColumnHeader         colName;
        private Panel                pnlListButtons;
        private Panel                pnlListButtonsRow1;
        private Panel                pnlListButtonsRow2;
        private Button               btnAddMonitor;
        private Button               btnEditMonitor;
        private Button               btnRemoveMonitor;
        private Button               btnStartAll;
        private Button               btnStopAll;
        private Label                lblIntervalLabel;
        private TextBox              txtInterval;

        // Right panel
        private Label                lblLivePointers;
        private Label                lblLastRead;
        private DataGridView         dgvFifo;

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
            // Connection
            this.lblIp               = new Label();
            this.txtIp               = new TextBox();
            this.lblUser             = new Label();
            this.txtUser             = new TextBox();
            this.lblPass             = new Label();
            this.txtPass             = new TextBox();
            this.btnConnect          = new Button();
            this.btnDisconnect       = new Button();
            this.lblConnectionStatus = new Label();
            this.grpConnection       = new GroupBox();

            // Layout
            this.splitMain           = new SplitContainer();

            // Left panel
            this.colStatus           = new ColumnHeader();
            this.colName             = new ColumnHeader();
            this.listMonitors        = new ListView();
            this.pnlListButtons      = new Panel();
            this.pnlListButtonsRow1  = new Panel();
            this.pnlListButtonsRow2  = new Panel();
            this.btnAddMonitor    = new Button();
            this.btnEditMonitor   = new Button();
            this.btnRemoveMonitor = new Button();
            this.btnStartAll      = new Button();
            this.btnStopAll       = new Button();
            this.lblIntervalLabel = new Label();
            this.txtInterval      = new TextBox();

            // Right panel
            this.lblLivePointers     = new Label();
            this.lblLastRead         = new Label();
            this.dgvFifo             = new DataGridView();

            // Status strip
            this.sslStatus           = new ToolStripStatusLabel();
            this.statusStrip         = new StatusStrip();

            this.grpConnection.SuspendLayout();
            this.pnlListButtons.SuspendLayout();
            this.pnlListButtonsRow1.SuspendLayout();
            this.pnlListButtonsRow2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.dgvFifo).BeginInit();
            this.SuspendLayout();

            // ===== Connection GroupBox =====
            this.grpConnection.Text    = "Connection";
            this.grpConnection.Dock    = DockStyle.Top;
            this.grpConnection.Height  = 78;
            this.grpConnection.Padding = new Padding(8, 4, 8, 4);

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

            // ===== SplitContainer =====
            this.splitMain.Dock      = DockStyle.Fill;
            this.splitMain.FixedPanel = FixedPanel.Panel1;

            // ===== Left Panel — ListView =====
            this.colStatus.Text  = string.Empty;
            this.colStatus.Width = 20;

            this.colName.Text  = "Monitor";
            this.colName.Width = 180;

            this.listMonitors.Columns.AddRange(new ColumnHeader[] {
                this.colStatus, this.colName
            });
            this.listMonitors.View           = View.Details;
            this.listMonitors.FullRowSelect  = true;
            this.listMonitors.MultiSelect    = false;
            this.listMonitors.HeaderStyle    = ColumnHeaderStyle.None;
            this.listMonitors.Dock           = DockStyle.Fill;
            this.listMonitors.SelectedIndexChanged += new System.EventHandler(this.listMonitors_SelectedIndexChanged);

            // Row 1: Add | Edit | Remove
            this.btnAddMonitor.Text     = "+ Add";
            this.btnAddMonitor.Width    = 60;
            this.btnAddMonitor.Height   = 24;
            this.btnAddMonitor.Location = new Point(2, 2);
            this.btnAddMonitor.Enabled  = false;
            this.btnAddMonitor.Click   += new System.EventHandler(this.btnAddMonitor_Click);

            this.btnEditMonitor.Text     = "Edit";
            this.btnEditMonitor.Width    = 50;
            this.btnEditMonitor.Height   = 24;
            this.btnEditMonitor.Location = new Point(64, 2);
            this.btnEditMonitor.Enabled  = false;
            this.btnEditMonitor.Click   += new System.EventHandler(this.btnEditMonitor_Click);

            this.btnRemoveMonitor.Text     = "Remove";
            this.btnRemoveMonitor.Width    = 64;
            this.btnRemoveMonitor.Height   = 24;
            this.btnRemoveMonitor.Location = new Point(116, 2);
            this.btnRemoveMonitor.Enabled  = false;
            this.btnRemoveMonitor.Click   += new System.EventHandler(this.btnRemoveMonitor_Click);

            this.pnlListButtonsRow1.Height = 28;
            this.pnlListButtonsRow1.Dock   = DockStyle.Top;
            this.pnlListButtonsRow1.Controls.AddRange(new Control[] {
                this.btnAddMonitor, this.btnEditMonitor, this.btnRemoveMonitor
            });

            // Row 2: Start All | Stop All | interval
            this.btnStartAll.Text     = "▶ Start All";
            this.btnStartAll.Width    = 78;
            this.btnStartAll.Height   = 24;
            this.btnStartAll.Location = new Point(2, 2);
            this.btnStartAll.Enabled  = false;
            this.btnStartAll.Click   += new System.EventHandler(this.btnStartAll_Click);

            this.btnStopAll.Text     = "■ Stop All";
            this.btnStopAll.Width    = 72;
            this.btnStopAll.Height   = 24;
            this.btnStopAll.Location = new Point(82, 2);
            this.btnStopAll.Enabled  = false;
            this.btnStopAll.Click   += new System.EventHandler(this.btnStopAll_Click);

            this.lblIntervalLabel.Text     = "ms:";
            this.lblIntervalLabel.AutoSize = true;
            this.lblIntervalLabel.Location = new Point(158, 6);

            this.txtInterval.Text     = "500";
            this.txtInterval.Width    = 46;
            this.txtInterval.Height   = 22;
            this.txtInterval.Location = new Point(178, 3);

            this.pnlListButtonsRow2.Height = 28;
            this.pnlListButtonsRow2.Dock   = DockStyle.Top;
            this.pnlListButtonsRow2.Controls.AddRange(new Control[] {
                this.btnStartAll, this.btnStopAll, this.lblIntervalLabel, this.txtInterval
            });

            this.pnlListButtons.Dock   = DockStyle.Bottom;
            this.pnlListButtons.Height = 56;
            this.pnlListButtons.Controls.AddRange(new Control[] {
                this.pnlListButtonsRow2, this.pnlListButtonsRow1
            });

            this.splitMain.Panel1.Controls.Add(this.listMonitors);
            this.splitMain.Panel1.Controls.Add(this.pnlListButtons);

            // ===== Right Panel =====
            this.lblLivePointers.Text      = "No monitor selected.";
            this.lblLivePointers.AutoSize  = false;
            this.lblLivePointers.Dock      = DockStyle.Top;
            this.lblLivePointers.Height    = 22;
            this.lblLivePointers.ForeColor = Color.DarkBlue;
            this.lblLivePointers.Font      = new Font("Consolas", 9f, FontStyle.Bold);
            this.lblLivePointers.Padding   = new Padding(4, 2, 0, 0);

            this.lblLastRead.AutoSize  = false;
            this.lblLastRead.Dock      = DockStyle.Top;
            this.lblLastRead.Height    = 18;
            this.lblLastRead.Text      = string.Empty;
            this.lblLastRead.Padding   = new Padding(4, 0, 0, 0);

            this.dgvFifo.Dock                  = DockStyle.Fill;
            this.dgvFifo.ReadOnly              = true;
            this.dgvFifo.AllowUserToAddRows    = false;
            this.dgvFifo.AllowUserToDeleteRows = false;
            this.dgvFifo.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvFifo.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            this.dgvFifo.RowHeadersVisible     = false;
            this.dgvFifo.BackgroundColor       = SystemColors.Window;
            this.dgvFifo.BorderStyle           = BorderStyle.None;

            // Controls are added bottom-to-top for DockStyle.Top to layer correctly
            this.splitMain.Panel2.Controls.Add(this.dgvFifo);
            this.splitMain.Panel2.Controls.Add(this.lblLastRead);
            this.splitMain.Panel2.Controls.Add(this.lblLivePointers);

            // ===== Status Strip =====
            this.sslStatus.Spring    = true;
            this.sslStatus.Text      = "Ready";
            this.sslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.statusStrip.Items.Add(this.sslStatus);
            this.statusStrip.SizingGrip = false;

            // ===== MainForm =====
            this.Text          = "FifoWatch — PLC FIFO Viewer";
            this.ClientSize    = new Size(960, 680);
            this.MinimumSize   = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.grpConnection);
            this.Controls.Add(this.statusStrip);

            this.grpConnection.ResumeLayout(false);
            this.pnlListButtons.ResumeLayout(false);
            this.pnlListButtonsRow1.ResumeLayout(false);
            this.pnlListButtonsRow2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)this.dgvFifo).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
