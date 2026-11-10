using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    /// <summary>Find past orders, reprint a receipt (read-only) or void an order.</summary>
    internal sealed class PageHistory : PageBase
    {
        private readonly SalesService sales = new SalesService();
        private readonly DateTimePicker dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today.AddDays(-6) };
        private readonly DateTimePicker dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
        private readonly TextBox txtOrder = Theme.Input(40);
        private readonly DataGridView grid = Theme.Grid();

        public PageHistory()
        {
            TableLayoutPanel root = CreateRoot("Sales History", "Find an order to reprint its receipt or void it.");

            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            FlowLayoutPanel bar = CreateToolbar();
            Label lblFrom = Theme.Lbl("From");
            lblFrom.Margin = new Padding(0, 9, 4, 0);
            dtFrom.Margin = new Padding(0, 5, 10, 0);
            Label lblTo = Theme.Lbl("To");
            lblTo.Margin = new Padding(0, 9, 4, 0);
            dtTo.Margin = new Padding(0, 5, 10, 0);
            Label lblOrder = Theme.Lbl("Order #");
            lblOrder.Margin = new Padding(0, 9, 4, 0);
            txtOrder.Width = 140;
            txtOrder.Margin = new Padding(0, 5, 10, 0);
            txtOrder.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadOrders(); }
            };

            Button btnSearch = Theme.Btn("Search", ButtonKind.Primary, 80);
            Button btnPreview = Theme.Btn("Preview", ButtonKind.Neutral, 80);
            Button btnPrint = Theme.Btn("Print", ButtonKind.Success, 70);
            Button btnVoid = Theme.Btn("Void order", ButtonKind.Danger, 100);
            btnVoid.Enabled = Session.Can(Permission.VoidOrders);
            btnSearch.Click += (s, e) => LoadOrders();
            btnPreview.Click += (s, e) => Receipt(false);
            btnPrint.Click += (s, e) => Receipt(true);
            btnVoid.Click += (s, e) => VoidSelected();

            bar.Controls.Add(lblFrom);
            bar.Controls.Add(dtFrom);
            bar.Controls.Add(lblTo);
            bar.Controls.Add(dtTo);
            bar.Controls.Add(lblOrder);
            bar.Controls.Add(txtOrder);
            bar.Controls.Add(btnSearch);
            bar.Controls.Add(btnPreview);
            bar.Controls.Add(btnPrint);
            bar.Controls.Add(btnVoid);

            Theme.Col(grid, "OrderNumber", "Order #", null, false, 20f);
            Theme.Col(grid, "OrderDate", "Date", "g", false, 18f);
            Theme.Col(grid, "Cashier", "Cashier", null, false, 17f);
            Theme.Col(grid, "Customer", "Customer", null, false, 17f);
            Theme.Col(grid, "PaymentMethod", "Payment", null, false, 10f);
            Theme.Col(grid, "Total", "Total", "N2", true, 12f);
            Theme.Col(grid, "Status", "Status", null, false, 10f);
            grid.DataBindingComplete += (s, e) => TintVoided();

            body.Controls.Add(bar, 0, 0);
            body.Controls.Add(grid, 0, 1);
            root.Controls.Add(body, 0, 1);
        }

        public override void OnPageShown()
        {
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                grid.DataSource = sales.SearchOrders(dtFrom.Value.Date, dtTo.Value.Date, txtOrder.Text.Trim());
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void TintVoided()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                DataRowView view = row.DataBoundItem as DataRowView;
                if (view == null) continue;

                bool voided = string.Equals(Convert.ToString(view["Status"]), "Voided", StringComparison.OrdinalIgnoreCase);
                row.DefaultCellStyle.ForeColor = voided ? Theme.Muted : Theme.Text;
                row.DefaultCellStyle.Font = voided ? new Font(Theme.Regular, FontStyle.Strikeout) : null;
            }
        }

        private DataRowView Selected()
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
        }

        private void Receipt(bool print)
        {
            DataRowView row = Selected();
            if (row == null) { Theme.Warn(FindForm(), "Select an order first."); return; }

            try
            {
                ReceiptData receipt = sales.GetReceipt(Convert.ToInt32(row["OrderId"]));
                ReceiptPrinter printer = new ReceiptPrinter();
                if (print) printer.Print(receipt); else printer.Preview(receipt);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void VoidSelected()
        {
            DataRowView row = Selected();
            if (row == null) { Theme.Warn(FindForm(), "Select an order first."); return; }

            string number = Convert.ToString(row["OrderNumber"]);
            if (string.Equals(Convert.ToString(row["Status"]), "Voided", StringComparison.OrdinalIgnoreCase))
            {
                Theme.Info(FindForm(), "Order " + number + " is already voided.");
                return;
            }

            if (!Theme.Confirm(FindForm(),
                "Void order " + number + "?\n\nThe items will be returned to stock. This cannot be undone."))
                return;

            try
            {
                sales.VoidOrder(Convert.ToInt32(row["OrderId"]));
                LoadOrders();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
