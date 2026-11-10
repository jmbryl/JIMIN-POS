using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Label = System.Windows.Forms.Label;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    internal sealed class PageAnalytics : PageBase
    {
        private readonly ReportingService reports = new ReportingService();

        private readonly DateTimePicker dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today.AddDays(-29) };
        private readonly DateTimePicker dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
        private readonly Label lblRevenue = new Label();
        private readonly Label lblOrders = new Label();
        private readonly Label lblAverage = new Label();
        private readonly Chart chartDaily;
        private readonly Chart chartTop;
        private readonly Chart chartCategory;
        private AnalyticsResult last;

        public PageAnalytics()
        {
            TableLayoutPanel root = CreateRoot("Sales Analytics", "Revenue includes tax and excludes voided orders.");

            TableLayoutPanel body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Toolbar
            FlowLayoutPanel bar = CreateToolbar();
            Label lblFrom = Theme.Lbl("From");
            lblFrom.Margin = new Padding(0, 9, 4, 0);
            dtFrom.Margin = new Padding(0, 5, 12, 0);
            Label lblTo = Theme.Lbl("To");
            lblTo.Margin = new Padding(0, 9, 4, 0);
            dtTo.Margin = new Padding(0, 5, 12, 0);
            Button btnRun = Theme.Btn("Run report", ButtonKind.Success, 110);
            Button btnExport = Theme.Btn("Export CSV", ButtonKind.Primary, 110);
            btnRun.Click += (s, e) => RunReport();
            btnExport.Click += (s, e) => ExportCsv();
            bar.Controls.Add(lblFrom);
            bar.Controls.Add(dtFrom);
            bar.Controls.Add(lblTo);
            bar.Controls.Add(dtTo);
            bar.Controls.Add(btnRun);
            bar.Controls.Add(btnExport);

            // KPI strip
            FlowLayoutPanel kpi = CreateToolbar();
            foreach (Label l in new[] { lblRevenue, lblOrders, lblAverage })
            {
                l.AutoSize = true;
                l.Font = Theme.Heading;
                l.ForeColor = Theme.Text;
                l.Margin = new Padding(0, 6, 36, 0);
                kpi.Controls.Add(l);
            }

            // Charts
            chartDaily = NewChart("Daily revenue", SeriesChartType.Column);
            chartTop = NewChart("Top products by revenue", SeriesChartType.Bar);
            chartTop.ChartAreas[0].AxisX.IsReversed = true;
            chartCategory = NewChart("Revenue by category", SeriesChartType.Doughnut);

            TableLayoutPanel charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            charts.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            charts.Controls.Add(chartDaily, 0, 0);
            charts.SetColumnSpan(chartDaily, 2);
            charts.Controls.Add(chartTop, 0, 1);
            charts.Controls.Add(chartCategory, 1, 1);

            body.Controls.Add(bar, 0, 0);
            body.Controls.Add(kpi, 0, 1);
            body.Controls.Add(charts, 0, 2);
            root.Controls.Add(body, 0, 1);

            ShowTotals(0m, 0, 0m);
        }

        public override void OnPageShown()
        {
            RunReport();
        }

        private static Chart NewChart(string title, SeriesChartType type)
        {
            Chart chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 8, 8),
                Palette = ChartColorPalette.None,
                PaletteCustomColors = new[]
                {
                    Theme.Navy, Theme.Teal, Theme.Purple, Theme.Red,
                    Color.FromArgb(230, 140, 0), Color.FromArgb(0, 150, 200), Color.FromArgb(120, 120, 120)
                }
            };

            ChartArea area = new ChartArea("main") { BackColor = Color.White };
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
            area.AxisX.LabelStyle.Font = Theme.Small;
            area.AxisY.LabelStyle.Font = Theme.Small;
            area.AxisY.LabelStyle.Format = "N0";
            chart.ChartAreas.Add(area);

            chart.Titles.Add(new Title(title, Docking.Top, Theme.Bold, Theme.Text));

            Series series = new Series("Revenue") { ChartType = type, Color = Theme.Navy };
            chart.Series.Add(series);

            if (type == SeriesChartType.Doughnut)
            {
                chart.Legends.Add(new Legend("legend") { Docking = Docking.Right, Font = Theme.Small });
                series.IsValueShownAsLabel = false;
            }
            else
            {
                series.IsValueShownAsLabel = type == SeriesChartType.Bar;
                series.LabelFormat = "N0";
            }

            return chart;
        }

        private void RunReport()
        {
            try
            {
                AnalyticsResult result = reports.GetAnalytics(dtFrom.Value.Date, dtTo.Value.Date);
                last = result;

                ShowTotals(result.Revenue, result.Orders, result.AverageOrder);

                Fill(chartDaily, "Daily revenue", result.Daily, "SaleDate", "Revenue", "MMM d");
                Fill(chartTop, "Top products by revenue", result.TopProducts, "ProductName", "Revenue", null);
                Fill(chartCategory, "Revenue by category", result.ByCategory, "CategoryName", "Revenue", null);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ShowTotals(decimal revenue, int orders, decimal average)
        {
            lblRevenue.Text = "Revenue: " + Theme.Money(revenue);
            lblOrders.Text = "Orders: " + orders.ToString("N0");
            lblAverage.Text = "Avg order: " + Theme.Money(average);
        }

        private static void Fill(Chart chart, string title, DataTable table, string labelColumn, string valueColumn, string dateFormat)
        {
            Series series = chart.Series[0];
            series.Points.Clear();

            foreach (DataRow row in table.Rows)
            {
                string label = dateFormat != null
                    ? Convert.ToDateTime(row[labelColumn]).ToString(dateFormat)
                    : Convert.ToString(row[labelColumn]);
                series.Points.AddXY(label, Convert.ToDouble(row[valueColumn]));
            }

            chart.Titles[0].Text = table.Rows.Count == 0 ? title + " (no sales in this period)" : title;
        }

        private void ExportCsv()
        {
            if (last == null) { Theme.Warn(FindForm(), "Run the report first."); return; }

            using (SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "sales-report-" + dtFrom.Value.ToString("yyyyMMdd") + "-" + dtTo.Value.ToString("yyyyMMdd") + ".csv"
            })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

                try
                {
                    CsvExporter.Write(dialog.FileName,
                        new[] { "Daily sales", "Top products", "Sales by category" },
                        new[] { last.Daily, last.TopProducts, last.ByCategory });
                    Theme.Info(FindForm(), "The report was exported.");
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }
    }
}
