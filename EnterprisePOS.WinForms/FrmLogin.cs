using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    internal sealed class FrmLogin : Form
    {
        private readonly AuthService auth = new AuthService();
        private readonly TextBox txtUsername = Theme.Input(50);
        private readonly TextBox txtPassword = Theme.Input(64);
        private readonly Button btnLogin = Theme.Btn("Sign in", ButtonKind.Primary, 320);
        private readonly Label lblStatus = new Label();

        public FrmLogin()
        {
            Text = Theme.AppName + " | Sign in";
            ClientSize = new Size(420, 440);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Theme.Regular;
            BackColor = Theme.Page;

            Panel header = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Theme.Sidebar };
            Label brand = new Label
            {
                Text = Theme.AppName,
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 78
            };
            Label tagline = new Label
            {
                Text = "POS & Inventory System",
                Font = Theme.Regular,
                ForeColor = Theme.SidebarText,
                AutoSize = false,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Top,
                Height = 28
            };
            header.Padding = new Padding(0, 14, 0, 0);
            header.Controls.Add(tagline);   // added first so the brand docks above it
            header.Controls.Add(brand);

            Label lblUser = Theme.Lbl("Username", Theme.Bold, Theme.Text);
            lblUser.Location = new Point(50, 154);
            txtUsername.Font = Theme.Entry;
            txtUsername.SetBounds(50, 178, 320, 30);

            Label lblPass = Theme.Lbl("Password", Theme.Bold, Theme.Text);
            lblPass.Location = new Point(50, 220);
            txtPassword.Font = Theme.Entry;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.SetBounds(50, 244, 320, 30);

            btnLogin.Height = 40;
            btnLogin.Location = new Point(50, 296);
            btnLogin.Click += (s, e) => DoLogin();
            AcceptButton = btnLogin;

            lblStatus.ForeColor = Theme.Red;
            lblStatus.Font = Theme.Regular;
            lblStatus.AutoSize = false;
            lblStatus.TextAlign = ContentAlignment.TopCenter;
            lblStatus.SetBounds(40, 348, 340, 56);

            Controls.Add(header);
            Controls.Add(lblUser);
            Controls.Add(txtUsername);
            Controls.Add(lblPass);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
            Controls.Add(lblStatus);

            Shown += (s, e) => txtUsername.Focus();
        }

        private void DoLogin()
        {
            btnLogin.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                string failure;
                AppUser user = auth.Login(txtUsername.Text, txtPassword.Text, out failure);

                if (user == null)
                {
                    lblStatus.Text = failure;
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    return;
                }

                lblStatus.Text = string.Empty;
                Hide();

                using (FrmDashboard shell = new FrmDashboard())
                {
                    shell.ShowDialog();
                }

                auth.Logout();
                txtPassword.Clear();
                Show();
                txtUsername.Focus();
            }
            catch (SqlException ex)
            {
                AppLog.Write(ex);
                lblStatus.Text = "Cannot reach the database.\nCheck the connection string in the .config file.";
            }
            catch (Exception ex)
            {
                AppLog.Write(ex);
                lblStatus.Text = "Sign-in failed unexpectedly.\nDetails were written to the log.";
            }
            finally
            {
                btnLogin.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }
}
