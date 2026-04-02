using System;
using System.Drawing;
using System.Windows.Forms;
using PasswordGenLocal.UI;

namespace PasswordGenLocal.UI.Dialogs
{
    public enum PassphraseMode { Enter, Create }

    public class PassphraseDialog : Form
    {
        private readonly PassphraseMode _mode;

        private readonly Label lblPassword;
        private readonly TextBox txtPassword;
        private readonly Label lblConfirm;
        private readonly TextBox txtConfirm;
        private readonly CheckBox chkShow;
        private readonly Label lblEntropy;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public string Passphrase => txtPassword.Text;

        public PassphraseDialog(PassphraseMode mode)
        {
            _mode = mode;

            Text            = mode == PassphraseMode.Create ? "Create Password" : "Enter Password";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ShowInTaskbar   = false;
            ClientSize      = new Size(380, mode == PassphraseMode.Create ? 230 : 145);
            Font            = new Font("Segoe UI", 9f);

            lblPassword = new Label
            {
                Text     = "Password:",
                Location = new Point(12, 20),
                AutoSize = true
            };
            txtPassword = new TextBox
            {
                Location     = new Point(12, 40),
                Width        = 356,
                PasswordChar = '●',
                Font         = new Font("Consolas", 9.5f)
            };
            txtPassword.TextChanged += TxtPassword_TextChanged;

            chkShow = new CheckBox
            {
                Text     = "Show password",
                Location = new Point(12, 68),
                AutoSize = true
            };
            chkShow.CheckedChanged += (s, e) =>
            {
                txtPassword.PasswordChar = chkShow.Checked ? '\0' : '●';
                if (_mode == PassphraseMode.Create)
                    txtConfirm.PasswordChar = chkShow.Checked ? '\0' : '●';
            };

            lblConfirm = new Label
            {
                Text     = "Confirm password:",
                Location = new Point(12, 96),
                AutoSize = true,
                Visible  = mode == PassphraseMode.Create
            };
            txtConfirm = new TextBox
            {
                Location     = new Point(12, 116),
                Width        = 356,
                PasswordChar = '●',
                Font         = new Font("Consolas", 9.5f),
                Visible      = mode == PassphraseMode.Create
            };
            txtConfirm.TextChanged += TxtPassword_TextChanged;

            lblEntropy = new Label
            {
                Text     = "",
                Location = new Point(12, 148),
                AutoSize = true,
                Visible  = mode == PassphraseMode.Create
            };

            btnOk = new Button
            {
                Text         = "OK",
                DialogResult = DialogResult.OK,
                Location     = new Point(210, mode == PassphraseMode.Create ? 196 : 110),
                Width        = 75
            };
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button
            {
                Text         = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(293, mode == PassphraseMode.Create ? 196 : 110),
                Width        = 75
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(chkShow);
            Controls.Add(lblConfirm);
            Controls.Add(txtConfirm);
            Controls.Add(lblEntropy);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            Load += (s, e) => ApplyTheme();
        }

        private void TxtPassword_TextChanged(object? sender, EventArgs e)
        {
            if (_mode != PassphraseMode.Create) return;
            int len = txtPassword.TextLength;
            if (len == 0)
            {
                lblEntropy.Text = "";
                return;
            }
            double bits = len * Math.Log2(95);
            lblEntropy.Text = $"Estimated entropy: {bits:F0} bits";
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Password cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            if (_mode == PassphraseMode.Create && txtPassword.Text != txtConfirm.Text)
            {
                MessageBox.Show("Passwords do not match.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
        }

        private void ApplyTheme()
        {
            BackColor = Theme.FormBg;
            ForeColor = Theme.PrimaryText;
            Theme.SetTitleBarDark(Handle, Theme.IsDark);

            lblPassword.BackColor = Theme.FormBg;
            lblPassword.ForeColor = Theme.PrimaryText;
            txtPassword.BackColor = Theme.InputBg;
            txtPassword.ForeColor = Theme.InputFg;
            chkShow.BackColor     = Theme.FormBg;
            chkShow.ForeColor     = Theme.PrimaryText;
            lblConfirm.BackColor  = Theme.FormBg;
            lblConfirm.ForeColor  = Theme.PrimaryText;
            txtConfirm.BackColor  = Theme.InputBg;
            txtConfirm.ForeColor  = Theme.InputFg;
            lblEntropy.BackColor  = Theme.FormBg;
            lblEntropy.ForeColor  = Theme.MutedText;
            btnOk.BackColor       = Theme.AccentBtn;
            btnOk.ForeColor       = Color.White;
            btnOk.FlatStyle       = FlatStyle.Flat;
            btnCancel.BackColor   = Theme.ToolbarBg;
            btnCancel.ForeColor   = Theme.PrimaryText;
            btnCancel.FlatStyle   = FlatStyle.Flat;
        }
    }
}
