using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FifoWatch.Models;
using FifoWatch.Services;
using S7CommPlusDriver;

namespace FifoWatch.Forms
{
    public partial class MainForm : Form
    {
        private readonly PlcService _plcService = new PlcService();
        private readonly FifoDefinition _definition = new FifoDefinition();
        private System.Threading.Timer _pollTimer;
        private int _pollIntervalMs = 500;
        private bool _polling;
        private List<FifoEntry> _lastEntries;
        private bool _displayIsStale;

        public MainForm()
        {
            InitializeComponent();
            UpdateUiState();
            InitGridContextMenu();
        }

        private void InitGridContextMenu()
        {
            var menu = new ContextMenuStrip();
            var copyValue    = menu.Items.Add("Copy Value");
            var copyVariable = menu.Items.Add("Copy Variable");
            menu.Opening += (s, e) => e.Cancel = dgvFifo.CurrentRow == null;
            copyValue.Click    += (s, e) => CopyCell("Value");
            copyVariable.Click += (s, e) => CopyCell("Variable");
            dgvFifo.ContextMenuStrip = menu;
        }

        private void CopyCell(string columnName)
        {
            var row = dgvFifo.CurrentRow;
            if (row == null) return;
            var cell = row.Cells[columnName];
            Clipboard.SetText(cell?.Value?.ToString() ?? string.Empty);
        }

        // ---- Connection ----

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            SetStatus("Connecting...");

            string ip   = txtIp.Text.Trim();
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text;

            int res = await Task.Run(() => _plcService.Connect(ip, user, pass));

            if (res == 0)
                SetStatus("Connected — click Browse... to select variables.");
            else
                SetStatus($"Connection failed (error {res}).");

            UpdateUiState();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            StopPolling();
            _plcService.Disconnect();
            lblLivePointers.Text = "NextIndexToRead: —   NextIndexToWrite: —   RecordsStored: —   MaxNrOfRecords: —";
            UpdateUiState();
            SetStatus("Disconnected.");
        }

        // ---- FIFO selection ----

        private void btnBrowseArray_Click(object sender, EventArgs e)
        {
            var v = ShowBrowseDialog();
            if (v != null)
            {
                _definition.ArrayTag = v;
                txtArrayTag.Text = v.Name;
                _lastEntries = null;
                _displayIsStale = false;
                _plcService.ResetArrayCache();
            }
        }

        private void btnClearArray_Click(object sender, EventArgs e)
        {
            _definition.ArrayTag = null;
            txtArrayTag.Text = string.Empty;
            _lastEntries = null;
            _displayIsStale = false;
            _plcService.ResetArrayCache();
        }

        private void btnBrowseHead_Click(object sender, EventArgs e)
        {
            var v = ShowBrowseDialog();
            if (v != null) { _definition.HeadTag = v; txtHeadTag.Text = v.Name; }
        }

        private void btnClearHead_Click(object sender, EventArgs e)
        {
            _definition.HeadTag = null;
            txtHeadTag.Text = string.Empty;
        }

        private void btnBrowseTail_Click(object sender, EventArgs e)
        {
            var v = ShowBrowseDialog();
            if (v != null) { _definition.TailTag = v; txtTailTag.Text = v.Name; }
        }

        private void btnClearTail_Click(object sender, EventArgs e)
        {
            _definition.TailTag = null;
            txtTailTag.Text = string.Empty;
        }

        private void btnBrowseCount_Click(object sender, EventArgs e)
        {
            var v = ShowBrowseDialog();
            if (v != null) { _definition.CountTag = v; txtCountTag.Text = v.Name; }
        }

        private void btnClearCount_Click(object sender, EventArgs e)
        {
            _definition.CountTag = null;
            txtCountTag.Text = string.Empty;
        }

        private void btnBrowseMaxRecords_Click(object sender, EventArgs e)
        {
            var v = ShowBrowseDialog();
            if (v != null) { _definition.MaxRecordsTag = v; txtMaxRecordsTag.Text = v.Name; }
        }

        private void btnClearMaxRecords_Click(object sender, EventArgs e)
        {
            _definition.MaxRecordsTag = null;
            txtMaxRecordsTag.Text = string.Empty;
        }

        private async void btnAutoDetect_Click(object sender, EventArgs e)
        {
            if (_definition.ArrayTag == null)
            {
                MessageBox.Show("Select an array tag first.", "No array selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAutoDetect.Enabled = false;
            SetStatus("Auto-detecting header fields...");
            Cursor = Cursors.WaitCursor;
            try
            {
                string arrayTagName = _definition.ArrayTag.Name;
                Dictionary<string, VarInfo> found = null;
                string diagnostics = null;

                await Task.Run(() =>
                {
                    found = _plcService.AutoDetectFifoHeader(arrayTagName, out diagnostics);
                });

                if (found.TryGetValue("NextIndexToRead", out var head))
                { _definition.HeadTag = head; txtHeadTag.Text = head.Name; }

                if (found.TryGetValue("NextIndexToWrite", out var tail))
                { _definition.TailTag = tail; txtTailTag.Text = tail.Name; }

                if (found.TryGetValue("RecordsStored", out var cnt))
                { _definition.CountTag = cnt; txtCountTag.Text = cnt.Name; }

                if (found.TryGetValue("MaxNrOfRecords", out var maxRec))
                { _definition.MaxRecordsTag = maxRec; txtMaxRecordsTag.Text = maxRec.Name; }

                if (found.Count == 0)
                {
                    MessageBox.Show(
                        $"No standard FIFO header fields found.\n\n{diagnostics}\n\n" +
                        "Expected fields: NextIndexToRead, NextIndexToWrite, RecordsStored, MaxNrOfRecords\n" +
                        "in the same DB as the selected array tag.",
                        "Auto-detect result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetStatus("Auto-detect: no fields found — see dialog for details.");
                }
                else
                {
                    SetStatus($"Auto-detect: found {found.Count} of 4 header field(s).");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Auto-detect error: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Default;
                UpdateUiState();
            }
        }

        private VarInfo ShowBrowseDialog()
        {
            if (!_plcService.IsConnected)
            {
                MessageBox.Show("Connect to a PLC first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (_polling) StopPolling();

            SetStatus("Opening variable browser...");
            using (var dlg = new BrowseForm(_plcService))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SetStatus("Variable selected.");
                    return dlg.SelectedVar;
                }
            }
            SetStatus("Browse cancelled.");
            return null;
        }

        // ---- Polling ----

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!_plcService.IsConnected)
            {
                MessageBox.Show("Connect to a PLC first.", "Not connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_definition.IsValid)
            {
                MessageBox.Show("Select an array tag first using Browse...", "No array selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (int.TryParse(txtInterval.Text, out int ms) && ms >= 100)
                _pollIntervalMs = ms;
            else
                _pollIntervalMs = 500;

            StartPolling();
        }

        private void btnStop_Click(object sender, EventArgs e) => StopPolling();

        private void StartPolling()
        {
            _polling = true;
            UpdateUiState();
            _pollTimer = new System.Threading.Timer(OnPollTick, null, 0, Timeout.Infinite);
        }

        private void StopPolling()
        {
            _polling = false;
            _pollTimer?.Dispose();
            _pollTimer = null;
            _lastEntries = null;
            _displayIsStale = false;
            UpdateUiState();
        }

        private void OnPollTick(object state)
        {
            if (!_polling) return;

            List<FifoEntry> entries = null;
            int head = -1, tail = -1, count = -1, maxRec = -1;
            string errorMsg = null;

            try
            {
                entries = _plcService.ReadFifo(_definition, out head, out tail, out count, out maxRec);
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            BeginInvoke(new Action(() =>
            {
                if (errorMsg != null)
                    SetStatus($"Read error: {errorMsg}");
                else if (entries != null)
                {
                    List<FifoEntry> display;
                    if (entries.Count > 0 && count != 0)
                    {
                        // Active FIFO records — show fresh and update stale cache.
                        _lastEntries    = entries;
                        _displayIsStale = false;
                        display         = entries;
                    }
                    else
                    {
                        // FIFO is empty (count==0). If ReadFifo surfaced the last-written
                        // record, update _lastEntries so the display tracks head/tail changes.
                        if (entries.Count > 0)
                            _lastEntries = entries;

                        if (_lastEntries != null)
                        {
                            _displayIsStale = true;
                            display         = _lastEntries;
                        }
                        else
                        {
                            display = entries;
                        }
                    }
                    UpdateGrid(display, head, tail, count, maxRec, _displayIsStale);
                    string staleTag = _displayIsStale ? " [last]" : "";
                    SetStatus($"OK | head={head} tail={tail} count={count} max={maxRec} | rows={entries.Count}{staleTag}");
                }
                else
                    SetStatus("Read returned null — check connection.");

                if (_polling)
                    _pollTimer?.Change(_pollIntervalMs, Timeout.Infinite);
            }));
        }

        private void UpdateGrid(List<FifoEntry> entries, int nextIndexToRead, int nextIndexToWrite, int recordsStored, int maxNrOfRecords, bool isStale = false)
        {
            dgvFifo.DataSource = null;
            dgvFifo.DataSource = entries;

            if (isStale)
            {
                foreach (DataGridViewRow row in dgvFifo.Rows)
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.Gray;
            }

            string fmt(int v) => v >= 0 ? v.ToString() : "—";
            lblLivePointers.Text =
                $"NextIndexToRead: {fmt(nextIndexToRead)}     " +
                $"NextIndexToWrite: {fmt(nextIndexToWrite)}     " +
                $"RecordsStored: {fmt(recordsStored)}     " +
                $"MaxNrOfRecords: {fmt(maxNrOfRecords)}";

            lblLastRead.Text = $"Last read: {DateTime.Now:HH:mm:ss.fff}   Rows shown: {entries.Count}";
        }

        // ---- Helpers ----

        private void SetStatus(string message) => sslStatus.Text = message;

        private void UpdateUiState()
        {
            bool connected = _plcService.IsConnected;
            btnConnect.Enabled    = !connected;
            btnDisconnect.Enabled = connected;
            txtIp.Enabled         = !connected;
            txtUser.Enabled       = !connected;
            txtPass.Enabled       = !connected;

            btnBrowseArray.Enabled      = connected;
            btnBrowseHead.Enabled       = connected;
            btnBrowseTail.Enabled       = connected;
            btnBrowseCount.Enabled      = connected;
            btnBrowseMaxRecords.Enabled = connected;
            btnAutoDetect.Enabled       = connected;

            btnStart.Enabled = connected && !_polling;
            btnStop.Enabled  = _polling;

            lblConnectionStatus.Text = connected ? "Connected" : "Disconnected";
            lblConnectionStatus.ForeColor = connected
                ? System.Drawing.Color.Green
                : System.Drawing.Color.Red;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPolling();
            _plcService.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
