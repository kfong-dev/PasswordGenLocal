using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PasswordGenLocal.Core;

namespace PasswordGenLocal.UI
{
    public class SessionView : UserControl
    {
        private readonly DataGridView gridSession;
        private readonly DataGridViewTextBoxColumn colTs;
        private readonly DataGridViewTextBoxColumn colSettings;
        private readonly DataGridViewTextBoxColumn colPassword;
        // Hidden columns store the raw mode and length values so GetEntries() never has to
        // parse them back out of the human-readable "Settings" label.
        private readonly DataGridViewTextBoxColumn colMode;
        private readonly DataGridViewTextBoxColumn colLength;

        // Fired when the user presses Delete or chooses "Delete Row(s)" from the context menu.
        // MainForm handles the confirmation dialog and calls DeleteSelectedRows() if confirmed.
        public event EventHandler? DeleteRequested;

        public int SelectedRowCount => gridSession.SelectedRows.Count;

        public SessionView()
        {
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
                Width      = 200,
                ReadOnly   = true,
                Name       = "colSettings"
            };
            colPassword = new DataGridViewTextBoxColumn
            {
                HeaderText    = "Password",
                AutoSizeMode  = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly      = true,
                Name          = "colPassword",
                DefaultCellStyle = { Font = new Font("Consolas", 10.5f, FontStyle.Regular) }
            };
            colMode   = new DataGridViewTextBoxColumn { Name = "colMode",   Visible = false };
            colLength = new DataGridViewTextBoxColumn { Name = "colLength", Visible = false };

            gridSession = new DataGridView
            {
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                BorderStyle           = BorderStyle.None,
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight   = 28,
                AutoSizeRowsMode      = DataGridViewAutoSizeRowsMode.None
            };
            // Two-line timestamp rows: date on line 1, time on line 2
            gridSession.RowTemplate.Height = 36;

            gridSession.Columns.Add(colTs);
            gridSession.Columns.Add(colSettings);
            gridSession.Columns.Add(colPassword);
            gridSession.Columns.Add(colMode);
            gridSession.Columns.Add(colLength);

            // Delete key
            gridSession.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && gridSession.SelectedRows.Count > 0)
                {
                    e.Handled = true;
                    DeleteRequested?.Invoke(this, EventArgs.Empty);
                }
            };

            // Context menu
            var ctxMenu   = new ContextMenuStrip();
            var menuDelete = new ToolStripMenuItem("Delete Row(s)");
            menuDelete.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);
            ctxMenu.Opening  += (s, e) => menuDelete.Enabled = gridSession.SelectedRows.Count > 0;
            ctxMenu.Items.Add(menuDelete);
            gridSession.ContextMenuStrip = ctxMenu;

            Dock = DockStyle.Fill;
            Controls.Add(gridSession);
        }

        public void AddEntry(DateTime ts, CharsetMode mode, int length, string password)
        {
            gridSession.Rows.Add(
                ts.ToString("yyyy-MM-dd\nHH:mm:ss.fff"),
                PasswordGenerator.ColumnLabel(mode, length),
                password,
                (int)mode,
                length);
            if (gridSession.Rows.Count > 0)
                gridSession.FirstDisplayedScrollingRowIndex = gridSession.Rows.Count - 1;
        }

        public void Clear()
        {
            gridSession.Rows.Clear();
        }

        /// <summary>
        /// Deletes all currently selected rows. Call only after confirmation has been obtained.
        /// </summary>
        public void DeleteSelectedRows()
        {
            var indices = new List<int>();
            foreach (DataGridViewRow row in gridSession.SelectedRows)
                if (!row.IsNewRow) indices.Add(row.Index);
            // Remove from bottom to top so indices stay valid
            indices.Sort((a, b) => b.CompareTo(a));
            foreach (int idx in indices)
                gridSession.Rows.RemoveAt(idx);
        }

        public IEnumerable<(DateTime ts, CharsetMode mode, int length, string password)> GetEntries()
        {
            var list = new List<(DateTime, CharsetMode, int, string)>();
            foreach (DataGridViewRow row in gridSession.Rows)
            {
                if (row.IsNewRow) continue;
                string tsStr = row.Cells["colTs"].Value?.ToString() ?? "";
                string pwd   = row.Cells["colPassword"].Value?.ToString() ?? "";

                // Timestamp is stored as "yyyy-MM-dd\nHH:mm:ss.fff" — parse just the date+time
                string tsFlat = tsStr.Replace("\n", " ");
                DateTime ts = DateTime.TryParse(tsFlat, out var dt) ? dt : DateTime.Now;

                int.TryParse(row.Cells["colMode"].Value?.ToString(),   out int modeInt);
                int.TryParse(row.Cells["colLength"].Value?.ToString(), out int length);
                CharsetMode mode = (CharsetMode)modeInt;

                list.Add((ts, mode, length, pwd));
            }
            return list;
        }

        public string? SelectedPassword =>
            gridSession.SelectedRows.Count > 0
                ? gridSession.SelectedRows[0].Cells["colPassword"].Value?.ToString()
                : null;

        public void SelectAll() => gridSession.SelectAll();

        public void ApplyTheme()
        {
            BackColor = Theme.GridBg;
            Theme.StyleGrid(gridSession);
        }
    }
}
