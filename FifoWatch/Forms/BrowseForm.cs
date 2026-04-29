using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using FifoWatch.Services;
using S7CommPlusDriver;

namespace FifoWatch.Forms
{
    public partial class BrowseForm : Form
    {
        private readonly PlcService _plcService;
        public VarInfo SelectedVar { get; private set; }

        public BrowseForm(PlcService plcService)
        {
            InitializeComponent();
            _plcService = plcService;
            LoadDatablocks();
        }

        private List<S7CommPlusConnection.DatablockInfo> _allDatablocks;

        private async void LoadDatablocks()
        {
            try
            {
                _allDatablocks = _plcService.GetDatablocks();
                _allDatablocks.Sort((a, b) => string.Compare(a.db_name, b.db_name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _allDatablocks = new List<S7CommPlusConnection.DatablockInfo>();
                treeView.Nodes.Add($"Error loading datablocks: {ex.Message}");
                return;
            }
            PopulateTree(string.Empty);

            // Warm the type-info cache so node expansion works for all DBs, including
            // optimised ones whose types can't be fetched individually from the PLC.
            lblStatus.Text = "Loading variable type info...";
            bool ok = await Task.Run(() => _plcService.PreloadTypeInfoCache());
            lblStatus.Text = ok
                ? "Expand a datablock to browse its variables."
                : "Type info preload failed — some nodes may not expand.";
        }

        private void PopulateTree(string filter)
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            foreach (var db in _allDatablocks)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    db.db_name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var tn = treeView.Nodes.Add(db.db_name);
                tn.Nodes.Add("...");
                tn.Tag = db;
            }
            treeView.EndUpdate();
        }

        private void treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            // Only expand nodes that still have the placeholder child
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "...")
                ExpandNode(node);
        }

        private void ExpandNode(TreeNode node)
        {
            treeView.BeginUpdate();
            node.Nodes.Clear();
            try
            {
                uint relId = GetRelIdFromNode(node);
                if (relId == 0)
                {
                    treeView.EndUpdate();
                    return;
                }

                var pObj = _plcService.GetTypeInfo(relId);
                if (pObj?.VarnameList == null)
                {
                    // Individual type-info request failed; try the full container fetch once.
                    lblStatus.Text = "Loading type info from PLC...";
                    lblStatus.Refresh();
                    if (_plcService.PreloadTypeInfoCache())
                        pObj = _plcService.GetTypeInfo(relId);
                    lblStatus.Text = "";
                }
                if (pObj?.VarnameList == null)
                {
                    node.Nodes.Add("(type info not available)");
                    treeView.EndUpdate();
                    return;
                }

                for (int i = 0; i < pObj.VarnameList.Names.Count; i++)
                {
                    string name = pObj.VarnameList.Names[i];
                    var vt = pObj.VartypeList.Elements[i];
                    AddTypeNode(node, name, vt);
                }
            }
            catch (Exception ex)
            {
                node.Nodes.Clear();
                node.Nodes.Add($"Error: {ex.Message}");
            }
            treeView.EndUpdate();
        }

        private static uint GetRelIdFromNode(TreeNode node)
        {
            if (node.Tag is S7CommPlusConnection.DatablockInfo db)
                return db.db_block_ti_relid;
            if (node.Tag is uint uid && uid != 0)
                return uid;
            return 0;
        }

        private static void AddTypeNode(TreeNode parent, string name, PVartypeListElement vt)
        {
            if (vt.OffsetInfoType.Is1Dim())
            {
                var dim = (IOffsetInfoType_1Dim)vt.OffsetInfoType;
                int lb = dim.GetArrayLowerBounds();
                int elemCount = (int)dim.GetArrayElementCount();

                var arrNode = parent.Nodes.Add(name);
                arrNode.Tag = (uint)0; // array container — not selectable

                for (int j = 0; j < elemCount; j++)
                {
                    string elemName = $"{name}[{lb + j}]";
                    if (vt.OffsetInfoType.HasRelation())
                    {
                        var rel = (IOffsetInfoType_Relation)vt.OffsetInfoType;
                        var elemNode = arrNode.Nodes.Add(elemName);
                        elemNode.Nodes.Add("...");
                        elemNode.Tag = rel.GetRelationId();
                    }
                    else
                    {
                        arrNode.Nodes.Add(elemName); // scalar leaf, Tag = null
                    }
                }
            }
            else if (vt.OffsetInfoType.HasRelation())
            {
                // Struct field — expandable
                var rel = (IOffsetInfoType_Relation)vt.OffsetInfoType;
                var structNode = parent.Nodes.Add(name);
                structNode.Nodes.Add("...");
                structNode.Tag = rel.GetRelationId();
            }
            else
            {
                // Scalar leaf
                parent.Nodes.Add(name); // Tag = null
            }
        }

        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
            => AcceptSelection();

        private void btnOK_Click(object sender, EventArgs e) => AcceptSelection();

        private void AcceptSelection()
        {
            var node = treeView.SelectedNode;
            if (node == null) return;

            // DB root and unexpanded struct nodes are not selectable
            if (node.Tag is S7CommPlusConnection.DatablockInfo) return;
            if (node.Tag is uint relId && relId != 0) return;

            string symbol = BuildSymbol(node);
            if (string.IsNullOrEmpty(symbol)) return;

            // Array container node — store just the name; ReadFifo will enumerate elements via type info
            if (node.Tag is uint u && u == 0)
            {
                var vi = new VarInfo();
                vi.Name = symbol;
                lblStatus.Text = $"Array: {symbol}";
                SelectedVar = vi;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            // Scalar leaf
            lblStatus.Text = $"Resolving {symbol}...";
            lblStatus.Refresh();

            VarInfo varInfo;
            try
            {
                varInfo = _plcService.GetVarInfoBySymbol(symbol);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                return;
            }

            if (varInfo == null)
            {
                lblStatus.Text = "Not a readable tag — select a scalar variable or an array node.";
                return;
            }

            SelectedVar = varInfo;
            DialogResult = DialogResult.OK;
            Close();
        }

        // Builds the full PLC symbol for a tree node by walking up to the root.
        // Result: "DB_CounterFifo".Buffer[0].CounterValue
        private static string BuildSymbol(TreeNode node)
        {
            var parts = new List<string>();
            TreeNode current = node;

            while (current != null)
            {
                TreeNode parent = current.Parent;

                if (parent == null)
                {
                    // Root = DB name, needs quotes
                    parts.Add('"' + current.Text + '"');
                    break;
                }

                if (parent.Tag is uint pu && pu == 0)
                {
                    // Parent is an array container — current.Text already contains "[N]"
                    parts.Add(current.Text); // e.g., "Buffer[0]"
                    current = parent.Parent; // skip the array container
                    continue;
                }

                parts.Add(current.Text);
                current = parent;
            }

            parts.Reverse();
            if (parts.Count == 0) return null;

            string result = parts[0];
            for (int i = 1; i < parts.Count; i++)
            {
                string part = parts[i];
                result += part.StartsWith("[") ? part : "." + part;
            }
            return result;
        }

        // ---- Search ----

        private void txtSearch_TextChanged(object sender, EventArgs e)
            => PopulateTree(txtSearch.Text.Trim());

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }
    }
}
