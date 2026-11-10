using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    internal sealed class PageUsers : PageBase
    {
        private readonly UserService users = new UserService();
        private readonly DataGridView grid = Theme.Grid();

        public PageUsers()
        {
            TableLayoutPanel root = CreateRoot("Users & Roles", "Create accounts, assign roles and reset passwords.");

            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel bar = CreateToolbar();
            Button btnAdd = Theme.Btn("Add user", ButtonKind.Success, 100);
            Button btnEdit = Theme.Btn("Edit / reset password", ButtonKind.Primary, 170);
            Button btnToggle = Theme.Btn("Activate / deactivate", ButtonKind.Neutral, 160);
            btnAdd.Click += (s, e) => AddUser();
            btnEdit.Click += (s, e) => EditUser();
            btnToggle.Click += (s, e) => ToggleActive();
            bar.Controls.Add(btnAdd);
            bar.Controls.Add(btnEdit);
            bar.Controls.Add(btnToggle);

            Theme.Col(grid, "Username", "Username", null, false, 16f);
            Theme.Col(grid, "FullName", "Full name", null, false, 26f);
            Theme.Col(grid, "RoleName", "Role", null, false, 12f);
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsActive",
                DataPropertyName = "IsActive",
                HeaderText = "Active",
                FillWeight = 8f
            });
            Theme.Col(grid, "CreatedAt", "Created", "g", false, 17f);
            Theme.Col(grid, "LastLoginAt", "Last login", "g", false, 17f);
            grid.DataBindingComplete += (s, e) => TintInactive();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditUser(); };

            body.Controls.Add(bar, 0, 0);
            body.Controls.Add(grid, 0, 1);
            root.Controls.Add(body, 0, 1);
        }

        public override void OnPageShown()
        {
            LoadUsers(0);
        }

        private void LoadUsers(int selectId)
        {
            try
            {
                grid.DataSource = users.GetAll();

                if (selectId > 0)
                {
                    foreach (DataGridViewRow r in grid.Rows)
                    {
                        DataRowView v = r.DataBoundItem as DataRowView;
                        if (v != null && Convert.ToInt32(v["UserId"]) == selectId)
                        {
                            grid.CurrentCell = r.Cells[0];
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void TintInactive()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                DataRowView v = row.DataBoundItem as DataRowView;
                if (v == null) continue;
                row.DefaultCellStyle.ForeColor = Convert.ToBoolean(v["IsActive"]) ? Theme.Text : Theme.Muted;
            }
        }

        private DataRowView Selected()
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
        }

        private void AddUser()
        {
            using (FrmUserEditor dialog = new FrmUserEditor(null))
            {
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LoadUsers(0);
            }
        }

        private void EditUser()
        {
            DataRowView row = Selected();
            if (row == null) { Theme.Warn(FindForm(), "Select a user first."); return; }

            try
            {
                int id = Convert.ToInt32(row["UserId"]);
                AppUser user = users.GetById(id);
                if (user == null) { Theme.Warn(FindForm(), "That user no longer exists."); LoadUsers(0); return; }

                using (FrmUserEditor dialog = new FrmUserEditor(user))
                {
                    if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LoadUsers(id);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ToggleActive()
        {
            DataRowView row = Selected();
            if (row == null) { Theme.Warn(FindForm(), "Select a user first."); return; }

            try
            {
                int id = Convert.ToInt32(row["UserId"]);
                AppUser user = users.GetById(id);
                if (user == null) return;

                bool makeActive = !user.IsActive;
                string verb = makeActive ? "Activate" : "Deactivate";
                if (!Theme.Confirm(FindForm(), verb + " the account '" + user.Username + "'?")) return;

                users.Update(user.UserId, user.FullName, user.RoleId, makeActive, null);
                LoadUsers(id);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
