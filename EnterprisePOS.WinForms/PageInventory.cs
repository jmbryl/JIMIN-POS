using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    /// <summary>Product list, stock movement history and stock adjustment in one screen.</summary>
    internal sealed class PageInventory : PageBase
    {
        private readonly InventoryService inventory = new InventoryService();

        private readonly TextBox txtSearch = Theme.Input(100);
        private readonly CheckBox chkLow = new CheckBox { Text = "Low stock only", AutoSize = true, Font = Theme.Regular };
        private readonly DataGridView grid = Theme.Grid();
        private readonly DataGridView gridLog = Theme.Grid();
        private readonly Label lblSelected = new Label();
        private readonly NumericUpDown numChange = Theme.Num(-1000000m, 1000000m, 0);
        private readonly ComboBox cmbReason = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Font = Theme.Regular };
        private bool loading;

        public PageInventory()
        {
            TableLayoutPanel root = CreateRoot("Inventory", "Search products, review stock movements and adjust stock.");

            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

            // Toolbar
            FlowLayoutPanel bar = CreateToolbar();
            Label lblSearch = Theme.Lbl("Search:");
            lblSearch.Margin = new Padding(0, 9, 4, 0);
            txtSearch.Width = 190;
            txtSearch.Margin = new Padding(0, 5, 8, 0);
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadProducts(0); }
            };
            Button btnSearch = Theme.Btn("Search", ButtonKind.Primary, 80);
            btnSearch.Click += (s, e) => LoadProducts(0);
            chkLow.Margin = new Padding(4, 8, 14, 0);
            chkLow.CheckedChanged += (s, e) => LoadProducts(0);
            Button btnAdd = Theme.Btn("Add product", ButtonKind.Success, 110);
            Button btnEdit = Theme.Btn("Edit", ButtonKind.Primary, 70);
            Button btnDelete = Theme.Btn("Deactivate", ButtonKind.Danger, 100);
            btnAdd.Click += (s, e) => AddProduct();
            btnEdit.Click += (s, e) => EditProduct();
            btnDelete.Click += (s, e) => DeactivateProduct();
            bar.Controls.Add(lblSearch);
            bar.Controls.Add(txtSearch);
            bar.Controls.Add(btnSearch);
            bar.Controls.Add(chkLow);
            bar.Controls.Add(btnAdd);
            bar.Controls.Add(btnEdit);
            bar.Controls.Add(btnDelete);

            // Product grid
            Theme.Col(grid, "Sku", "SKU", null, false, 12f);
            Theme.Col(grid, "Name", "Product", null, false, 36f);
            Theme.Col(grid, "CategoryName", "Category", null, false, 16f);
            Theme.Col(grid, "Price", "Price", "N2", true, 10f);
            Theme.Col(grid, "StockQty", "In stock", "N0", true, 10f);
            Theme.Col(grid, "ReorderLevel", "Reorder level", "N0", true, 12f);
            grid.DataBindingComplete += (s, e) => Theme.TintLowStock(grid);
            grid.SelectionChanged += (s, e) => { if (!loading) OnProductSelected(); };
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditProduct(); };

            // History
            Label lblHistory = Theme.Lbl("Stock movements for the selected product", Theme.Bold, Theme.Text);
            lblHistory.Margin = new Padding(0, 12, 0, 0);
            Theme.Col(gridLog, "When", "When", "g", false, 22f);
            Theme.Col(gridLog, "Change", "Change", "+#,0;-#,0;0", true, 10f);
            Theme.Col(gridLog, "Reason", "Reason", null, false, 52f);
            Theme.Col(gridLog, "By", "By", null, false, 16f);

            // Adjustment bar
            Panel adjust = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(14, 8, 14, 8) };
            lblSelected.Text = "Select a product";
            lblSelected.Font = Theme.Bold;
            lblSelected.AutoSize = true;
            lblSelected.Location = new Point(14, 8);

            FlowLayoutPanel controls = new FlowLayoutPanel
            {
                Location = new Point(10, 32),
                Size = new Size(820, 38),
                WrapContents = false,
                BackColor = Theme.Surface
            };
            numChange.Width = 100;
            numChange.Margin = new Padding(4, 3, 8, 0);
            cmbReason.Width = 250;
            cmbReason.Margin = new Padding(0, 3, 8, 0);
            cmbReason.Items.AddRange(new object[]
            {
                "Stock received", "Stock count correction", "Damaged", "Expired",
                "Returned to supplier", "Customer return", "Other"
            });
            Button btnApply = Theme.Btn("Apply adjustment", ButtonKind.Success, 150);
            btnApply.Margin = new Padding(0, 0, 8, 0);
            btnApply.Click += (s, e) => ApplyAdjustment();
            Label hint = Theme.Lbl("Use positive numbers to add stock, negative to remove.", Theme.Small, Theme.Muted);
            hint.Margin = new Padding(6, 9, 0, 0);
            controls.Controls.Add(numChange);
            controls.Controls.Add(cmbReason);
            controls.Controls.Add(btnApply);
            controls.Controls.Add(hint);
            adjust.Controls.Add(lblSelected);
            adjust.Controls.Add(controls);

            body.Controls.Add(bar, 0, 0);
            body.Controls.Add(grid, 0, 1);
            body.Controls.Add(lblHistory, 0, 2);
            body.Controls.Add(gridLog, 0, 3);
            body.Controls.Add(adjust, 0, 4);
            root.Controls.Add(body, 0, 1);
        }

        public override void OnPageShown()
        {
            LoadProducts(0);
        }

        // ------------------------------------------------------------ data

        private int SelectedProductId()
        {
            if (grid.CurrentRow == null) return 0;
            DataRowView row = grid.CurrentRow.DataBoundItem as DataRowView;
            return row == null ? 0 : Convert.ToInt32(row["ProductId"]);
        }

        private DataRowView SelectedRow()
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
        }

        /// <summary>Reloads the grid and re-selects selectId when it is still listed.</summary>
        private void LoadProducts(int selectId)
        {
            loading = true;
            try
            {
                grid.DataSource = inventory.Search(txtSearch.Text.Trim(), chkLow.Checked);

                if (selectId > 0)
                {
                    foreach (DataGridViewRow r in grid.Rows)
                    {
                        DataRowView v = r.DataBoundItem as DataRowView;
                        if (v != null && Convert.ToInt32(v["ProductId"]) == selectId)
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
            finally
            {
                loading = false;
            }

            OnProductSelected();
        }

        private void OnProductSelected()
        {
            DataRowView row = SelectedRow();
            if (row == null)
            {
                lblSelected.Text = "Select a product";
                gridLog.DataSource = null;
                return;
            }

            lblSelected.Text = Convert.ToString(row["Name"]) + "  (stock " + Convert.ToInt32(row["StockQty"]) + ")";

            try
            {
                gridLog.DataSource = inventory.GetLog(Convert.ToInt32(row["ProductId"]));
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ------------------------------------------------------------ actions

        private void AddProduct()
        {
            using (FrmProductEditor dialog = new FrmProductEditor(null))
            {
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LoadProducts(dialog.SavedProductId);
            }
        }

        private void EditProduct()
        {
            int id = SelectedProductId();
            if (id == 0) { Theme.Warn(FindForm(), "Please select a product to edit."); return; }

            using (FrmProductEditor dialog = new FrmProductEditor(id))
            {
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LoadProducts(id);
            }
        }

        private void DeactivateProduct()
        {
            DataRowView row = SelectedRow();
            if (row == null) { Theme.Warn(FindForm(), "Please select a product to deactivate."); return; }

            string name = Convert.ToString(row["Name"]);
            if (!Theme.Confirm(FindForm(),
                "Deactivate '" + name + "'?\n\nIt will no longer appear in the POS. Past sales are kept."))
                return;

            try
            {
                inventory.Deactivate(Convert.ToInt32(row["ProductId"]));
                LoadProducts(0);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ApplyAdjustment()
        {
            int id = SelectedProductId();
            if (id == 0) { Theme.Warn(FindForm(), "Please select a product first."); return; }

            try
            {
                int change = (int)numChange.Value;
                string reason = cmbReason.Text.Trim();

                int newStock = inventory.Adjust(id, change, reason);

                numChange.Value = 0;
                cmbReason.SelectedIndex = -1;
                cmbReason.Text = string.Empty;
                LoadProducts(id);
                Theme.Info(FindForm(), "Stock adjusted. New stock: " + newStock + ".");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
