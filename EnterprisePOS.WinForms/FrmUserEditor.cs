using System;
using System.Data;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    /// <summary>Creates a user (existing = null) or edits one, including an optional password reset.</summary>
    internal sealed class FrmUserEditor : ThemedDialog
    {
        private readonly UserService users = new UserService();
        private readonly AppUser existing;

        private readonly TextBox txtUsername = Theme.Input(50);
        private readonly TextBox txtFullName = Theme.Input(100);
        private readonly ComboBox cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Regular };
        private readonly TextBox txtPassword = Theme.Input(64);
        private readonly TextBox txtConfirm = Theme.Input(64);
        private readonly CheckBox chkActive = new CheckBox { Text = "Account is active", Checked = true, AutoSize = true, Font = Theme.Regular };

        public FrmUserEditor(AppUser existing)
            : base(existing == null ? "New user" : "Edit user",
                   existing == null ? "Create a sign-in account" : "Update the account or reset its password", 480, 500)
        {
            this.existing = existing;

            txtPassword.UseSystemPasswordChar = true;
            txtConfirm.UseSystemPasswordChar = true;

            AddRow("Username *", txtUsername);
            AddRow("Full name *", txtFullName);
            AddRow("Role *", cmbRole);
            AddRow(existing == null ? "Password *" : "New password", txtPassword);
            AddRow("Confirm password", txtConfirm);

            if (existing != null)
            {
                AddRow("Status", chkActive);
                AddWide(Theme.Lbl("Leave the password blank to keep the current one.", Theme.Small, Theme.Muted));
                txtUsername.Text = existing.Username;
                txtUsername.ReadOnly = true;
                txtFullName.Text = existing.FullName;
                chkActive.Checked = existing.IsActive;
            }
            else
            {
                AddWide(Theme.Lbl("Minimum 8 characters.", Theme.Small, Theme.Muted));
            }

            Button save = Theme.Btn(existing == null ? "Create user" : "Save changes", ButtonKind.Success, 130);
            Button cancel = Theme.Btn("Cancel", ButtonKind.Neutral, 90);
            cancel.DialogResult = DialogResult.Cancel;
            save.Click += (s, e) => Save();
            AddButton(save);
            AddButton(cancel);
            AcceptButton = save;
            CancelButton = cancel;

            Load += (s, e) => LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                cmbRole.DataSource = users.GetRoles();
                cmbRole.DisplayMember = "RoleName";
                cmbRole.ValueMember = "RoleId";

                if (existing != null) cmbRole.SelectedValue = existing.RoleId;
                else cmbRole.SelectedIndex = cmbRole.Items.Count - 1; // default to the least-privileged role
            }
            catch (Exception ex)
            {
                Theme.Error(this, ex.Message);
                DialogResult = DialogResult.Cancel;
            }
        }

        private void Save()
        {
            try
            {
                if (cmbRole.SelectedValue == null) throw new ArgumentException("Choose a role.");
                if (txtPassword.Text != txtConfirm.Text) throw new ArgumentException("The passwords do not match.");

                int roleId = Convert.ToInt32(cmbRole.SelectedValue);

                if (existing == null)
                    users.Create(txtUsername.Text, txtFullName.Text, roleId, txtPassword.Text);
                else
                    users.Update(existing.UserId, txtFullName.Text, roleId, chkActive.Checked, txtPassword.Text);

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Theme.Warn(this, ex.Message);
            }
        }
    }
}
