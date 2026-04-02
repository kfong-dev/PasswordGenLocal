using System;
using System.Drawing;
using System.Windows.Forms;
using PasswordGenLocal.Core;
using PasswordGenLocal.UI;

namespace PasswordGenLocal.UI.Dialogs
{
    public class ExportWarningDialog : Form
    {
        private readonly Label lblWarning;
        private readonly CheckBox chkNeverShow;
        private readonly Button btnAccept;
        private readonly Button btnCancel;

        private readonly AppSettings _settings;

        public bool Accepted { get; private set; } = false;

        public ExportWarningDialog(AppSettings settings)
        {
            _settings = settings;

            Text            = "Export Warning";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(480, 200);
            Font            = new Font("Segoe UI", 9f);

            lblWarning = new Label
            {
                Text = "\u26A0 You are about to export passwords as plain CSV.\n\n" +
                       "This file will be unencrypted and readable by anyone.\n\n" +
                       "Only proceed if you understand and accept this risk.",
                Location  = new Point(12, 12),
                Size      = new Size(456, 100),
                Font      = new Font("Segoe UI", 9.5f)
            };

            chkNeverShow = new CheckBox
            {
                Text     = "Do not show this warning again",
                Location = new Point(12, 120),
                AutoSize = true
            };

            btnAccept = new Button
            {
                Text     = "I Accept",
                Location = new Point(300, 158),
                Width    = 80
            };
            btnAccept.Click += BtnAccept_Click;

            btnCancel = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(388, 158),
                Width        = 80
            };

            AcceptButton = btnAccept;
            CancelButton = btnCancel;

            Controls.Add(lblWarning);
            Controls.Add(chkNeverShow);
            Controls.Add(btnAccept);
            Controls.Add(btnCancel);

            Load += (s, e) => ApplyTheme();
        }

        private void BtnAccept_Click(object? sender, EventArgs e)
        {
            if (chkNeverShow.Checked)
            {
                _settings.CsvExportWarningAccepted = true;
                _settings.Save();
            }
            Accepted = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.FormBg;
            ForeColor = Theme.PrimaryText;
            Theme.SetTitleBarDark(Handle, Theme.IsDark);

            lblWarning.BackColor   = Theme.FormBg;
            lblWarning.ForeColor   = Theme.PrimaryText;
            chkNeverShow.BackColor = Theme.FormBg;
            chkNeverShow.ForeColor = Theme.MutedText;

            btnAccept.BackColor = Theme.AccentBtn;
            btnAccept.ForeColor = Color.White;
            btnAccept.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Theme.ToolbarBg;
            btnCancel.ForeColor = Theme.PrimaryText;
            btnCancel.FlatStyle = FlatStyle.Flat;
        }
    }
}
