using System;
using System.Drawing;
using System.Windows.Forms;
using PasswordGenLocal.UI;

namespace PasswordGenLocal.UI.Dialogs
{
    public class NewStoreDialog : Form
    {
        private readonly Label lblStoreName;
        private readonly TextBox txtStoreName;
        private readonly Label lblPassword;
        private readonly TextBox txtPassword;
        private readonly Label lblConfirm;
        private readonly TextBox txtConfirm;
        private readonly CheckBox chkShow;
        private readonly Label lblEntropy;
        private readonly Button btnCreate;
        private readonly Button btnCancel;

        public string StoreName => txtStoreName.Text.Trim();
        public string Password  => txtPassword.Text;

        public NewStoreDialog()
        {
            Text            = "New Store";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(400, 290);
            Font            = new Font("Segoe UI", 9f);

            int y = 16;

            lblStoreName = new Label { Text = "Store name:", Location = new Point(12, y), AutoSize = true };
            y += 20;
            txtStoreName = new TextBox { Location = new Point(12, y), Width = 376 };
            y += 32;

            lblPassword = new Label { Text = "Password:", Location = new Point(12, y), AutoSize = true };
            y += 20;
            txtPassword = new TextBox
            {
                Location     = new Point(12, y),
                Width        = 376,
                PasswordChar = '●',
                Font         = new Font("Consolas", 9.5f)
            };
            txtPassword.TextChanged += OnPasswordChanged;
            y += 32;

            lblConfirm = new Label { Text = "Confirm password:", Location = new Point(12, y), AutoSize = true };
            y += 20;
            txtConfirm = new TextBox
            {
                Location     = new Point(12, y),
                Width        = 376,
                PasswordChar = '●',
                Font         = new Font("Consolas", 9.5f)
            };
            y += 32;

            chkShow = new CheckBox { Text = "Show password", Location = new Point(12, y), AutoSize = true };
            chkShow.CheckedChanged += (s, e) =>
            {
                txtPassword.PasswordChar = chkShow.Checked ? '\0' : '●';
                txtConfirm.PasswordChar  = chkShow.Checked ? '\0' : '●';
            };
            y += 28;

            lblEntropy = new Label { Text = "", Location = new Point(12, y), AutoSize = true };
            y += 28;

            btnCreate = new Button
            {
                Text     = "Create",
                Location = new Point(220, y),
                Width    = 80
            };
            btnCreate.Click += BtnCreate_Click;

            btnCancel = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(308, y),
                Width        = 80
            };

            AcceptButton = btnCreate;
            CancelButton = btnCancel;

            Controls.Add(lblStoreName);
            Controls.Add(txtStoreName);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblConfirm);
            Controls.Add(txtConfirm);
            Controls.Add(chkShow);
            Controls.Add(lblEntropy);
            Controls.Add(btnCreate);
            Controls.Add(btnCancel);

            Load += (s, e) => ApplyTheme();
        }

        private void OnPasswordChanged(object? sender, EventArgs e)
        {
            int len = txtPassword.TextLength;
            if (len == 0) { lblEntropy.Text = ""; return; }
            double bits = len * Math.Log2(95);
            lblEntropy.Text = $"Estimated entropy: {bits:F0} bits";
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStoreName.Text))
            {
                MessageBox.Show("Store name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Password cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (txtPassword.Text != txtConfirm.Text)
            {
                MessageBox.Show("Passwords do not match.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.FormBg;
            ForeColor = Theme.PrimaryText;
            Theme.SetTitleBarDark(Handle, Theme.IsDark);

            foreach (Control c in Controls)
            {
                if (c is Label l)    { l.BackColor = Theme.FormBg;  l.ForeColor = Theme.PrimaryText; }
                if (c is TextBox t)  { t.BackColor = Theme.InputBg; t.ForeColor = Theme.InputFg; }
                if (c is CheckBox ch){ ch.BackColor = Theme.FormBg; ch.ForeColor = Theme.PrimaryText; }
            }
            lblEntropy.ForeColor = Theme.MutedText;

            btnCreate.BackColor = Theme.AccentBtn;
            btnCreate.ForeColor = Color.White;
            btnCreate.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Theme.ToolbarBg;
            btnCancel.ForeColor = Theme.PrimaryText;
            btnCancel.FlatStyle = FlatStyle.Flat;
        }
    }
}
