using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    internal sealed class PageDashboard : PageBase
    {
        private readonly ReportingService reports = new ReportingService();
        private readonly Label lblSales;
        private readonly Label lblOrders;
        private readonly Label lblLow;
        private readonly Label lblProducts;
        private readonly DataGridView grid = Theme.Grid();

        public PageDashboard()
        {
            TableLayoutPanel root = CreateRoot(
                "Welcome, " + Session.CurrentUser.FullName,
                "Here is how the store is doing today.");

            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            TableLayoutPanel cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            for (int i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cards.Controls.Add(Theme.Card("Today's sales", Theme.Teal, out lblSales), 0, 0);
            cards.Controls.Add(Theme.Card("Orders today", Theme.Navy, out lblOrders), 1, 0);
            cards.Controls.Add(Theme.Card("Low-stock items", Theme.Red, out lblLow), 2, 0);
            cards.Controls.Add(Theme.Card("Active products", Theme.Purple, out lblProducts), 3, 0);

            Label section = Theme.Lbl("Low-stock items", Theme.Heading, Theme.Text);
            section.Margin = new Padding(0, 16, 0, 0);

            Theme.Col(grid, "Sku", "SKU", null, false, 18f);
            Theme.Col(grid, "Name", "Product", null, false, 62f);
            Theme.Col(grid, "StockQty", "In stock", "N0", true, 10f);
            Theme.Col(grid, "ReorderLevel", "Reorder level", "N0", true, 10f);

            body.Controls.Add(cards, 0, 0);
            body.Controls.Add(section, 0, 1);
            body.Controls.Add(grid, 0, 2);
            root.Controls.Add(body, 0, 1);
        }

        public override void OnPageShown()
        {
            try
            {
                DashboardSummary summary = reports.GetDashboard();
                lblSales.Text = Theme.Money(summary.TodaySales);
                lblOrders.Text = summary.TodayOrders.ToString("N0");
                lblLow.Text = summary.LowStockCount.ToString("N0");
                lblProducts.Text = summary.ActiveProducts.ToString("N0");

                grid.DataSource = reports.GetLowStock();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }
    }
}
