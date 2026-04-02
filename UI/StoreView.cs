using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PasswordGenLocal.Core;

namespace PasswordGenLocal.UI
{
    public class StoreView : UserControl
    {
        // Header controls
        private readonly Panel pHeader;
        private readonly Label lblNameCaption;
        private readonly TextBox txtName;
        private readonly Label lblCreatedCaption;
        private readonly Label lblCreated;
        private readonly Label lblModifiedCaption;
        private readonly Label lblModified;
        private readonly Label lblDirtyStatus;
        private readonly Panel sepHeader;

        // Grid columns
        private readonly DataGridView gridStore;
        private readonly DataGridViewTextBoxColumn colUsername;  // backing: StoreEntry.Label
        private readonly DataGridViewTextBoxColumn colNote;
        private readonly DataGridViewTextBoxColumn colTs;
        private readonly DataGridViewTextBoxColumn colSettings;
        private readonly DataGridViewTextBoxColumn colPassword;

        private LpgeStore? _store;
        private readonly AppSettings _settings;

        public LpgeStore? Store => _store;
        public bool IsDirty => _store?.IsDirty ?? false;

        // Fired when the user presses Delete or chooses "Delete Row(s)" from the context menu.
        public event EventHandler? DeleteRequested;

        public int SelectedRowCount => gridStore.SelectedRows.Count;

        public StoreView(AppSettings settings)
        {
            _settings = settings;
            Dock = DockStyle.Fill;

            // ── Header panel (92px to fit three rows) ────────────────────────────
            pHeader = new Panel { Height = 92, Dock = DockStyle.Top, Padding = new Padding(6, 6, 6, 2) };

            lblNameCaption = new Label
            {
                Text     = "Name:",
                AutoSize = true,
                Location = new Point(6, 10),
                Font     = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            txtName = new TextBox
            {
                Location = new Point(52, 7),
                Width    = 220,
                Font     = new Font("Segoe UI", 9f)
            };
            txtName.TextChanged += TxtName_TextChanged;

            lblCreatedCaption = new Label
            {
                Text     = "Created:",
                AutoSize = true,
                Location = new Point(6, 38),
                Font     = new Font("Segoe UI", 8f)
            };
            lblCreated = new Label
            {
                Text     = "",
                AutoSize = true,
                Location = new Point(58, 38),
                Font     = new Font("Segoe UI", 8f)
            };
            lblModifiedCaption = new Label
            {
                Text     = "Modified:",
                AutoSize = true,
                Location = new Point(200, 38),
                Font     = new Font("Segoe UI", 8f)
            };
            lblModified = new Label
            {
                Text     = "",
                AutoSize = true,
                Location = new Point(258, 38),
                Font     = new Font("Segoe UI", 8f)
            };

            // Dirty indicator — shown below Created/Modified when store has unsaved changes
            lblDirtyStatus = new Label
            {
                Text     = "",
                AutoSize = true,
                Location = new Point(6, 62),
                Font     = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xCC, 0x6E, 0x00),   // amber — readable on both themes
                Visible  = false
            };

            pHeader.Controls.Add(lblNameCaption);
            pHeader.Controls.Add(txtName);
            pHeader.Controls.Add(lblCreatedCaption);
            pHeader.Controls.Add(lblCreated);
            pHeader.Controls.Add(lblModifiedCaption);
            pHeader.Controls.Add(lblModified);
            pHeader.Controls.Add(lblDirtyStatus);

            sepHeader = new Panel { Height = 1, Dock = DockStyle.Top };

            // ── Grid columns ─────────────────────────────────────────────────────
            colUsername = new DataGridViewTextBoxColumn
            {
                HeaderText = "Username",
                Width      = 160,
                ReadOnly   = false,
                Name       = "colUsername"
            };
            colNote = new DataGridViewTextBoxColumn
            {
                HeaderText = "Note",
                Width      = 200,
                ReadOnly   = false,
                Name       = "colNote"
            };
            colTs = new DataGridViewTextBoxColumn
            {
                HeaderText = "Timestamp",
                Width      = 120,
                ReadOnly   = true,
                Name       = "colTs",
                DefaultCellStyle = { WrapMode = DataGridViewTriState.True }
            };
            colSettings = new DataGridViewTextBoxColumn
            {
                HeaderText = "Settings",
                Width      = 190,
                ReadOnly   = true,
                Name       = "colSettings"
            };
            colPassword = new DataGridViewTextBoxColumn
            {
                HeaderText   = "Password",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly     = true,
                Name         = "colPassword",
                DefaultCellStyle = { Font = new Font("Consolas", 10.5f, FontStyle.Regular) }
            };

            gridStore = new DataGridView
            {
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                BorderStyle           = BorderStyle.None,
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight   = 28
            };
            // Two-line timestamp rows
            gridStore.RowTemplate.Height = 36;

            gridStore.Columns.Add(colUsername);
            gridStore.Columns.Add(colNote);
            gridStore.Columns.Add(colTs);
            gridStore.Columns.Add(colSettings);
            gridStore.Columns.Add(colPassword);

            gridStore.CellEndEdit += GridStore_CellEndEdit;

            // Delete key
            gridStore.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && gridStore.SelectedRows.Count > 0)
                {
                    e.Handled = true;
                    DeleteRequested?.Invoke(this, EventArgs.Empty);
                }
            };

            // Context menu
            var ctxMenu    = new ContextMenuStrip();
            var menuDelete = new ToolStripMenuItem("Delete Row(s)");
            menuDelete.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);
            ctxMenu.Opening  += (s, e) => menuDelete.Enabled = gridStore.SelectedRows.Count > 0;
            ctxMenu.Items.Add(menuDelete);
            gridStore.ContextMenuStrip = ctxMenu;

            // Layout: header on top, separator, grid fills rest
            Controls.Add(gridStore);
            Controls.Add(sepHeader);
            Controls.Add(pHeader);
        }

        private void TxtName_TextChanged(object? sender, EventArgs e)
        {
            if (_store == null) return;
            _store.StoreName = txtName.Text;
            _store.IsDirty   = true;
            _store.Modified  = DateTime.Now;
            UpdateModified();
        }

        private void UpdateModified()
        {
            if (_store != null)
                lblModified.Text = _store.Modified.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void GridStore_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (_store == null || e.RowIndex < 0 || e.RowIndex >= _store.Entries.Count) return;

            int usernameCol = gridStore.Columns["colUsername"]?.Index ?? -1;
            int noteCol     = gridStore.Columns["colNote"]?.Index     ?? -1;

            if (e.ColumnIndex == usernameCol)
            {
                string newLabel = gridStore.Rows[e.RowIndex].Cells["colUsername"].Value?.ToString() ?? "";
                _store.Entries[e.RowIndex].Label = newLabel;
                _store.IsDirty  = true;
                _store.Modified = DateTime.Now;
                UpdateModified();
                RefreshDirtyIndicator();
            }
            else if (e.ColumnIndex == noteCol)
            {
                string raw = gridStore.Rows[e.RowIndex].Cells["colNote"].Value?.ToString() ?? "";
                // Sanitise: printable ASCII (0x20-0x7E), max 95 characters
                var sb = new StringBuilder(95);
                foreach (char c in raw)
                {
                    if (c >= 0x20 && c <= 0x7E && sb.Length < 95)
                        sb.Append(c);
                }
                string note = sb.ToString();
                gridStore.Rows[e.RowIndex].Cells["colNote"].Value = note;
                _store.Entries[e.RowIndex].Note = note;
                _store.IsDirty  = true;
                _store.Modified = DateTime.Now;
                UpdateModified();
                RefreshDirtyIndicator();
            }
        }

        public void LoadStore(LpgeStore store)
        {
            _store = store;
            txtName.TextChanged -= TxtName_TextChanged;
            txtName.Text = store.StoreName;
            txtName.TextChanged += TxtName_TextChanged;

            lblCreated.Text  = store.Created.ToString("yyyy-MM-dd HH:mm:ss");
            lblModified.Text = store.Modified.ToString("yyyy-MM-dd HH:mm:ss");

            gridStore.Rows.Clear();
            foreach (var entry in store.Entries)
                AppendRow(entry);

            RefreshDirtyIndicator();
        }

        public void AddEntry(StoreEntry entry)
        {
            if (_store == null) return;
            _store.Entries.Add(entry);
            _store.IsDirty  = true;
            _store.Modified = DateTime.Now;
            UpdateModified();
            AppendRow(entry);
            RefreshDirtyIndicator();
        }

        private void AppendRow(StoreEntry entry)
        {
            gridStore.Rows.Add(
                entry.Label,
                entry.Note,
                entry.Timestamp.ToString("yyyy-MM-dd\nHH:mm:ss.fff"),
                PasswordGenerator.ColumnLabel(entry.Mode, entry.Length),
                entry.Password);
            if (gridStore.Rows.Count > 0)
                gridStore.FirstDisplayedScrollingRowIndex = gridStore.Rows.Count - 1;
        }

        /// <summary>
        /// Updates the dirty indicator label. Called by the MainForm store-check timer
        /// and immediately after any mutation that changes the dirty state.
        /// </summary>
        public void RefreshDirtyIndicator()
        {
            if (_store == null || !_store.IsDirty)
            {
                lblDirtyStatus.Visible = false;
                return;
            }
            lblDirtyStatus.Text    = $"\u25cf Unsaved changes  (modified {_store.Modified:HH:mm:ss})";
            lblDirtyStatus.Visible = true;
        }

        public void ClearDisplay()
        {
            if (_store == null) return;
            _store.Entries.Clear();
            _store.IsDirty  = true;
            _store.Modified = DateTime.Now;
            gridStore.Rows.Clear();
            UpdateModified();
            RefreshDirtyIndicator();
        }

        /// <summary>
        /// Deletes all currently selected rows and syncs the backing Entries list.
        /// Call only after confirmation has been obtained.
        /// </summary>
        public void DeleteSelectedRows()
        {
            if (_store == null) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in gridStore.SelectedRows)
                if (!row.IsNewRow) indices.Add(row.Index);
            // Remove bottom-to-top to keep indices valid
            indices.Sort((a, b) => b.CompareTo(a));
            foreach (int idx in indices)
            {
                if (idx < _store.Entries.Count)
                    _store.Entries.RemoveAt(idx);
                gridStore.Rows.RemoveAt(idx);
            }
            if (indices.Count > 0)
            {
                _store.IsDirty  = true;
                _store.Modified = DateTime.Now;
                UpdateModified();
                RefreshDirtyIndicator();
            }
        }

        public IEnumerable<StoreEntry> GetEntries() => _store?.Entries ?? new List<StoreEntry>();

        public string? SelectedPassword =>
            gridStore.SelectedRows.Count > 0
                ? gridStore.SelectedRows[0].Cells["colPassword"].Value?.ToString()
                : null;

        public void SelectAll() => gridStore.SelectAll();

        public void ApplyTheme()
        {
            BackColor                    = Theme.PanelBg;
            pHeader.BackColor            = Theme.PanelBg;
            sepHeader.BackColor          = Theme.SepColor;
            lblNameCaption.BackColor     = Theme.PanelBg;
            lblNameCaption.ForeColor     = Theme.PrimaryText;
            txtName.BackColor            = Theme.InputBg;
            txtName.ForeColor            = Theme.InputFg;
            lblCreatedCaption.BackColor  = Theme.PanelBg;
            lblCreatedCaption.ForeColor  = Theme.MutedText;
            lblCreated.BackColor         = Theme.PanelBg;
            lblCreated.ForeColor         = Theme.MutedText;
            lblModifiedCaption.BackColor = Theme.PanelBg;
            lblModifiedCaption.ForeColor = Theme.MutedText;
            lblModified.BackColor        = Theme.PanelBg;
            lblModified.ForeColor        = Theme.MutedText;
            lblDirtyStatus.BackColor     = Theme.PanelBg;
            // Keep the amber ForeColor — it works on both light and dark backgrounds
            Theme.StyleGrid(gridStore);
        }
    }
}
