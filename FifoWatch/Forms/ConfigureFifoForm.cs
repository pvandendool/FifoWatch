using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using FifoWatch.Models;
using FifoWatch.Services;
using S7CommPlusDriver;

namespace FifoWatch.Forms
{
    public partial class ConfigureFifoForm : Form
    {
        private readonly PlcService _plcService;
        private readonly FifoDefinition _definition = new FifoDefinition();

        public Func<Task> RequestPollSuspendAsync { get; set; }
        public Action     NotifyPollResumed       { get; set; }

        public string         MonitorName { get; private set; }
        public FifoDefinition Definition  { get; private set; }

        public ConfigureFifoForm(PlcService plcService, FifoMonitorState existing = null)
        {
            InitializeComponent();
            _plcService = plcService;

            if (existing != null)
            {
                Text             = $"Edit — {existing.Name}";
                txtName.Text     = existing.Name;
                _definition.ArrayTag      = existing.Definition.ArrayTag;
                _definition.HeadTag       = existing.Definition.HeadTag;
                _definition.TailTag       = existing.Definition.TailTag;
                _definition.CountTag      = existing.Definition.CountTag;
                _definition.MaxRecordsTag = existing.Definition.MaxRecordsTag;
                RefreshTagTextBoxes();
            }
        }

        private void RefreshTagTextBoxes()
        {
            txtArrayTag.Text      = _definition.ArrayTag?.Name      ?? string.Empty;
            txtHeadTag.Text       = _definition.HeadTag?.Name       ?? string.Empty;
            txtTailTag.Text       = _definition.TailTag?.Name       ?? string.Empty;
            txtCountTag.Text      = _definition.CountTag?.Name      ?? string.Empty;
            txtMaxRecordsTag.Text = _definition.MaxRecordsTag?.Name ?? string.Empty;
        }

        // ---- Browse helpers ----

        private async Task<VarInfo> ShowBrowseDialogAsync()
        {
            if (RequestPollSuspendAsync != null)
                await RequestPollSuspendAsync();
            try
            {
                using (var dlg = new BrowseForm(_plcService))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        return dlg.SelectedVar;
                }
                return null;
            }
            finally
            {
                NotifyPollResumed?.Invoke();
            }
        }

        private async void btnBrowseArray_Click(object sender, EventArgs e)
        {
            var v = await ShowBrowseDialogAsync();
            if (v != null) { _definition.ArrayTag = v; txtArrayTag.Text = v.Name; }
        }

        private void btnClearArray_Click(object sender, EventArgs e)
        {
            _definition.ArrayTag = null;
            txtArrayTag.Text = string.Empty;
        }

        private async void btnBrowseHead_Click(object sender, EventArgs e)
        {
            var v = await ShowBrowseDialogAsync();
            if (v != null) { _definition.HeadTag = v; txtHeadTag.Text = v.Name; }
        }

        private void btnClearHead_Click(object sender, EventArgs e)
        {
            _definition.HeadTag = null;
            txtHeadTag.Text = string.Empty;
        }

        private async void btnBrowseTail_Click(object sender, EventArgs e)
        {
            var v = await ShowBrowseDialogAsync();
            if (v != null) { _definition.TailTag = v; txtTailTag.Text = v.Name; }
        }

        private void btnClearTail_Click(object sender, EventArgs e)
        {
            _definition.TailTag = null;
            txtTailTag.Text = string.Empty;
        }

        private async void btnBrowseCount_Click(object sender, EventArgs e)
        {
            var v = await ShowBrowseDialogAsync();
            if (v != null) { _definition.CountTag = v; txtCountTag.Text = v.Name; }
        }

        private void btnClearCount_Click(object sender, EventArgs e)
        {
            _definition.CountTag = null;
            txtCountTag.Text = string.Empty;
        }

        private async void btnBrowseMaxRecords_Click(object sender, EventArgs e)
        {
            var v = await ShowBrowseDialogAsync();
            if (v != null) { _definition.MaxRecordsTag = v; txtMaxRecordsTag.Text = v.Name; }
        }

        private void btnClearMaxRecords_Click(object sender, EventArgs e)
        {
            _definition.MaxRecordsTag = null;
            txtMaxRecordsTag.Text = string.Empty;
        }

        // ---- Auto-detect ----

        private async void btnAutoDetect_Click(object sender, EventArgs e)
        {
            if (_definition.ArrayTag == null)
            {
                MessageBox.Show("Select an array tag first.", "No array selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAutoDetect.Enabled = false;
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

                if (found.TryGetValue("NextIndexToRead",  out var head))   { _definition.HeadTag       = head;   txtHeadTag.Text       = head.Name; }
                if (found.TryGetValue("NextIndexToWrite", out var tail))   { _definition.TailTag       = tail;   txtTailTag.Text       = tail.Name; }
                if (found.TryGetValue("RecordsStored",    out var cnt))    { _definition.CountTag      = cnt;    txtCountTag.Text      = cnt.Name; }
                if (found.TryGetValue("MaxNrOfRecords",   out var maxRec)) { _definition.MaxRecordsTag = maxRec; txtMaxRecordsTag.Text = maxRec.Name; }

                if (found.Count == 0)
                {
                    MessageBox.Show(
                        $"No standard FIFO header fields found.\n\n{diagnostics}\n\n" +
                        "Expected fields: NextIndexToRead, NextIndexToWrite, RecordsStored, MaxNrOfRecords\n" +
                        "in the same DB as the selected array tag.",
                        "Auto-detect result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Auto-detect error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnAutoDetect.Enabled = true;
            }
        }

        // ---- OK / Cancel ----

        private void btnOK_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Enter a monitor name.", "Name required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_definition.IsValid)
            {
                MessageBox.Show("Select an array tag first.", "Array required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MonitorName  = name;
            Definition   = _definition;
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }
    }
}
