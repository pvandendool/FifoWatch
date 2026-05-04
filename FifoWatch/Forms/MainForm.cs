using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using FifoWatch.Models;
using FifoWatch.Services;
using S7CommPlusDriver;

namespace FifoWatch.Forms
{
    public partial class MainForm : Form
    {
        private readonly PlcService _plcService = new PlcService();
        private readonly List<FifoMonitorState> _monitors = new List<FifoMonitorState>();
        private int _pollIntervalMs = 500;

        private System.Threading.Timer _sharedTimer;
        private volatile bool _timerRunning;
        private volatile bool _globalPollingSuspended;
        private readonly object _tickLock = new object();

        private FifoMonitorState _selectedMonitor;

        public MainForm()
        {
            InitializeComponent();
            txtIp.Text = Properties.Settings.Default.LastIpAddress;
            UpdateUiState();
            InitGridContextMenu();
            Load += (s, e) =>
            {
                splitMain.Panel1MinSize    = 160;
                splitMain.Panel2MinSize    = 300;
                splitMain.SplitterDistance = 220;
                LoadConfig();
            };
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
            Clipboard.SetText(row.Cells[columnName]?.Value?.ToString() ?? string.Empty);
        }

        // ---- Connection ----

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            SetStatus("Connecting...");

            string ip   = txtIp.Text.Trim();
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text;

            Properties.Settings.Default.LastIpAddress = ip;
            Properties.Settings.Default.Save();

            int res = await Task.Run(() => _plcService.Connect(ip, user, pass));

            if (res == 0)
                SetStatus("Connected — click + Add to configure a FIFO monitor.");
            else
                SetStatus($"Connection failed (error {res}).");

            UpdateUiState();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            foreach (var m in _monitors)
                m.IsPolling = false;

            bool gotLock = Monitor.TryEnter(_tickLock, 2000);
            if (gotLock) Monitor.Exit(_tickLock);

            _plcService.Disconnect();

            foreach (ListViewItem item in listMonitors.Items)
                SetListItemPolling(item, false);

            UpdateRightPane(null);
            SetStatus("Disconnected.");
            UpdateUiState();
        }

        // ---- Monitor list ----

        private void listMonitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedMonitor = listMonitors.SelectedItems.Count > 0
                ? listMonitors.SelectedItems[0].Tag as FifoMonitorState
                : null;
            UpdateRightPane(_selectedMonitor);
            UpdateUiState();
        }

        private void btnAddMonitor_Click(object sender, EventArgs e)
        {
            OpenConfigureDialog(null);
        }

        private void btnEditMonitor_Click(object sender, EventArgs e)
        {
            if (_selectedMonitor == null) return;
            OpenConfigureDialog(_selectedMonitor);
        }

        private void OpenConfigureDialog(FifoMonitorState existing)
        {
            bool wasPolling = existing != null && existing.IsPolling;
            if (wasPolling) existing.IsPolling = false;

            btnAddMonitor.Enabled  = false;
            btnEditMonitor.Enabled = false;
            try
            {
                using (var dlg = new ConfigureFifoForm(_plcService, existing))
                {
                    dlg.RequestPollSuspendAsync = async () =>
                    {
                        _globalPollingSuspended = true;
                        await _plcService.WaitForIdleAsync();
                    };
                    dlg.NotifyPollResumed = () => { _globalPollingSuspended = false; };

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        if (existing == null)
                        {
                            var monitor = new FifoMonitorState
                            {
                                Name       = dlg.MonitorName,
                                Definition = dlg.Definition
                            };
                            _monitors.Add(monitor);
                            var item = new ListViewItem(new[] { "○", monitor.Name });
                            item.Tag = monitor;
                            listMonitors.Items.Add(item);
                            item.Selected = true;
                        }
                        else
                        {
                            string oldArrayTag = existing.Definition.ArrayTag?.Name;
                            existing.Name       = dlg.MonitorName;
                            existing.Definition = dlg.Definition;
                            existing.LastEntries    = null;
                            existing.DisplayIsStale = false;
                            existing.LastHead = existing.LastTail = existing.LastCount = existing.LastMaxRec = -1;
                            existing.LastError = null;
                            existing.NextPollDue = DateTime.MinValue;
                            _plcService.ResetArrayCache(oldArrayTag);
                            RefreshListItem(existing);
                            UpdateRightPane(existing);
                            if (wasPolling) existing.IsPolling = true;
                        }
                    }
                    else if (wasPolling)
                    {
                        existing.IsPolling = true;
                    }
                }
            }
            finally
            {
                _globalPollingSuspended = false;
                UpdateUiState();
            }
        }

        private void btnRemoveMonitor_Click(object sender, EventArgs e)
        {
            if (_selectedMonitor == null) return;
            _selectedMonitor.IsPolling = false;
            _monitors.Remove(_selectedMonitor);

            foreach (ListViewItem item in listMonitors.Items)
            {
                if (item.Tag == _selectedMonitor)
                {
                    listMonitors.Items.Remove(item);
                    break;
                }
            }

            _selectedMonitor = null;
            UpdateRightPane(null);
            UpdateUiState();
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            if (!_plcService.IsConnected || _monitors.Count == 0) return;
            if (int.TryParse(txtInterval.Text, out int ms) && ms >= 100)
                _pollIntervalMs = ms;
            else
                _pollIntervalMs = 500;

            foreach (var m in _monitors)
            {
                m.NextPollDue = DateTime.MinValue;
                m.IsPolling   = true;
                RefreshListItem(m);
            }
            EnsureTimerRunning();
            UpdateUiState();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            foreach (var m in _monitors)
            {
                m.IsPolling = false;
                RefreshListItem(m);
            }
            UpdateUiState();
        }

        // ---- Shared timer ----

        private void EnsureTimerRunning()
        {
            if (_timerRunning) return;
            _timerRunning = true;
            _sharedTimer  = new System.Threading.Timer(OnSharedTick, null, 100, Timeout.Infinite);
        }

        private void StopTimer()
        {
            _timerRunning = false;
            _sharedTimer?.Dispose();
            _sharedTimer = null;
        }

        private void OnSharedTick(object state)
        {
            if (!Monitor.TryEnter(_tickLock)) return;
            try
            {
                if (_globalPollingSuspended) return;

                List<FifoMonitorState> due = null;
                try
                {
                    due = (List<FifoMonitorState>)Invoke(new Func<List<FifoMonitorState>>(() =>
                    {
                        var now = DateTime.UtcNow;
                        return _monitors.FindAll(m =>
                            m.IsPolling && m.Definition.IsValid && now >= m.NextPollDue);
                    }));
                }
                catch { return; }

                foreach (var monitor in due)
                {
                    if (_globalPollingSuspended) break;
                    PollMonitor(monitor);
                }
            }
            finally
            {
                Monitor.Exit(_tickLock);
                if (_timerRunning)
                    _sharedTimer?.Change(100, Timeout.Infinite);
            }
        }

        private void PollMonitor(FifoMonitorState monitor)
        {
            int head = -1, tail = -1, count = -1, maxRec = -1;
            List<FifoEntry> entries = null;
            string errorMsg = null;

            try
            {
                entries = _plcService.ReadFifo(monitor.Definition, out head, out tail, out count, out maxRec);
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            monitor.NextPollDue = DateTime.UtcNow.AddMilliseconds(_pollIntervalMs);

            BeginInvoke(new Action(() =>
            {
                if (errorMsg != null)
                {
                    monitor.LastError = errorMsg;
                }
                else if (entries != null)
                {
                    monitor.LastError = null;

                    if (entries.Count > 0 && count != 0)
                    {
                        monitor.LastEntries    = entries;
                        monitor.DisplayIsStale = false;
                    }
                    else
                    {
                        if (entries.Count > 0)
                            monitor.LastEntries = entries;

                        if (monitor.LastEntries != null)
                            monitor.DisplayIsStale = true;
                        else
                            monitor.LastEntries = entries;
                    }

                    monitor.LastHead   = head;
                    monitor.LastTail   = tail;
                    monitor.LastCount  = count;
                    monitor.LastMaxRec = maxRec;
                }

                if (monitor == _selectedMonitor)
                {
                    if (monitor.LastError != null)
                        SetStatus($"Read error: {monitor.LastError}");
                    else
                        UpdateRightPane(monitor);
                }
            }));
        }

        // ---- Right pane ----

        private void UpdateRightPane(FifoMonitorState monitor)
        {
            dgvFifo.DataSource = null;

            if (monitor == null)
            {
                lblLivePointers.Text = "No monitor selected.";
                lblLastRead.Text     = string.Empty;
                return;
            }

            if (monitor.LastError != null)
            {
                lblLivePointers.Text = $"Error: {monitor.LastError}";
                lblLastRead.Text     = string.Empty;
                return;
            }

            var display = monitor.LastEntries ?? new List<FifoEntry>();
            dgvFifo.DataSource = display;

            if (monitor.DisplayIsStale)
            {
                foreach (DataGridViewRow row in dgvFifo.Rows)
                    row.DefaultCellStyle.ForeColor = Color.Gray;
            }

            string fmt(int v) => v >= 0 ? v.ToString() : "—";
            lblLivePointers.Text =
                $"NextIndexToRead: {fmt(monitor.LastHead)}     " +
                $"NextIndexToWrite: {fmt(monitor.LastTail)}     " +
                $"RecordsStored: {fmt(monitor.LastCount)}     " +
                $"MaxNrOfRecords: {fmt(monitor.LastMaxRec)}";

            string staleTag = monitor.DisplayIsStale ? " [last]" : string.Empty;
            lblLastRead.Text = monitor.LastEntries != null
                ? $"Last read: {DateTime.Now:HH:mm:ss.fff}   Rows: {display.Count}{staleTag}"
                : string.Empty;

            if (monitor.IsPolling && monitor.LastError == null)
                SetStatus($"OK | head={fmt(monitor.LastHead)} tail={fmt(monitor.LastTail)} count={fmt(monitor.LastCount)} max={fmt(monitor.LastMaxRec)} | rows={display.Count}{staleTag}");
        }

        // ---- ListView helpers ----

        private void RefreshListItem(FifoMonitorState monitor)
        {
            foreach (ListViewItem item in listMonitors.Items)
            {
                if (item.Tag != monitor) continue;
                item.SubItems[0].Text = monitor.IsPolling ? "●" : "○";
                item.SubItems[1].Text = monitor.Name;
                item.ForeColor = monitor.IsPolling ? Color.Green : SystemColors.WindowText;
                break;
            }
        }

        private static void SetListItemPolling(ListViewItem item, bool polling)
        {
            item.SubItems[0].Text = polling ? "●" : "○";
            item.ForeColor = polling ? Color.Green : SystemColors.WindowText;
        }

        // ---- Config persistence ----

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FifoWatch", "monitors.xml");

        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                var file = new FifoConfigFile { PollIntervalMs = _pollIntervalMs };
                foreach (var m in _monitors)
                {
                    file.Monitors.Add(new FifoMonitorConfig
                    {
                        Name          = m.Name,
                        ArrayTag      = ToTagConfig(m.Definition.ArrayTag),
                        HeadTag       = ToTagConfig(m.Definition.HeadTag),
                        TailTag       = ToTagConfig(m.Definition.TailTag),
                        CountTag      = ToTagConfig(m.Definition.CountTag),
                        MaxRecordsTag = ToTagConfig(m.Definition.MaxRecordsTag),
                    });
                }
                using (var writer = new StreamWriter(ConfigPath))
                    new XmlSerializer(typeof(FifoConfigFile)).Serialize(writer, file);
            }
            catch { }
        }

        private void LoadConfig()
        {
            if (!File.Exists(ConfigPath)) return;
            try
            {
                FifoConfigFile file;
                using (var reader = new StreamReader(ConfigPath))
                    file = (FifoConfigFile)new XmlSerializer(typeof(FifoConfigFile)).Deserialize(reader);

                if (file.PollIntervalMs >= 100)
                {
                    _pollIntervalMs  = file.PollIntervalMs;
                    txtInterval.Text = file.PollIntervalMs.ToString();
                }

                foreach (var cfg in file.Monitors)
                {
                    var monitor = new FifoMonitorState
                    {
                        Name       = cfg.Name ?? "FIFO",
                        Definition = new FifoDefinition
                        {
                            ArrayTag      = ToVarInfo(cfg.ArrayTag),
                            HeadTag       = ToVarInfo(cfg.HeadTag),
                            TailTag       = ToVarInfo(cfg.TailTag),
                            CountTag      = ToVarInfo(cfg.CountTag),
                            MaxRecordsTag = ToVarInfo(cfg.MaxRecordsTag),
                        }
                    };
                    _monitors.Add(monitor);
                    var item = new ListViewItem(new[] { "○", monitor.Name });
                    item.Tag = monitor;
                    listMonitors.Items.Add(item);
                }
            }
            catch { }
        }

        private static FifoTagConfig ToTagConfig(VarInfo vi)
        {
            if (vi == null) return null;
            return new FifoTagConfig
            {
                Name           = vi.Name,
                AccessSequence = vi.AccessSequence,
                Softdatatype   = vi.Softdatatype,
            };
        }

        private static VarInfo ToVarInfo(FifoTagConfig cfg)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.Name)) return null;
            var vi = new VarInfo();
            vi.Name           = cfg.Name;
            vi.AccessSequence = cfg.AccessSequence;
            vi.Softdatatype   = cfg.Softdatatype;
            return vi;
        }

        // ---- Helpers ----

        private void SetStatus(string message) => sslStatus.Text = message;

        private void UpdateUiState()
        {
            bool connected  = _plcService.IsConnected;
            bool hasMonitor = _selectedMonitor != null;
            bool anyPolling = _monitors.Exists(m => m.IsPolling);

            btnConnect.Enabled    = !connected;
            btnDisconnect.Enabled = connected;
            txtIp.Enabled         = !connected;
            txtUser.Enabled       = !connected;
            txtPass.Enabled       = !connected;

            btnAddMonitor.Enabled    = connected;
            btnEditMonitor.Enabled   = connected && hasMonitor;
            btnRemoveMonitor.Enabled = hasMonitor;
            btnStartAll.Enabled      = connected && _monitors.Count > 0 && !anyPolling;
            btnStopAll.Enabled       = anyPolling;

            lblConnectionStatus.Text      = connected ? "Connected" : "Disconnected";
            lblConnectionStatus.ForeColor = connected ? Color.Green : Color.Red;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveConfig();
            foreach (var m in _monitors)
                m.IsPolling = false;
            StopTimer();
            _plcService.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
