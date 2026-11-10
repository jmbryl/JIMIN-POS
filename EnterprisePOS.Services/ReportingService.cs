using System;
using System.Data;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    public sealed class AnalyticsResult
    {
        public DataTable Daily;
        public DataTable TopProducts;
        public DataTable ByCategory;
        public decimal Revenue;
        public int Orders;
        public decimal AverageOrder;
    }

    public sealed class ReportingService
    {
        private readonly DashboardRepository dashboard = new DashboardRepository();
        private readonly ReportRepository reports = new ReportRepository();

        public DashboardSummary GetDashboard()
        {
            Session.RequireSignedIn();
            return dashboard.GetSummary();
        }

        public DataTable GetLowStock()
        {
            Session.RequireSignedIn();
            return dashboard.GetLowStock();
        }

        public AnalyticsResult GetAnalytics(DateTime from, DateTime to)
        {
            Session.Require(Permission.ViewAnalytics);

            if (from.Date > to.Date)
                throw new ArgumentException("The 'from' date must be on or before the 'to' date.");

            AnalyticsResult result = new AnalyticsResult
            {
                Daily = reports.DailySales(from, to),
                TopProducts = reports.TopProducts(from, to, 10),
                ByCategory = reports.SalesByCategory(from, to)
            };

            foreach (DataRow row in result.Daily.Rows)
            {
                result.Revenue += Convert.ToDecimal(row["Revenue"]);
                result.Orders += Convert.ToInt32(row["OrderCount"]);
            }

            result.AverageOrder = result.Orders == 0 ? 0m : Math.Round(result.Revenue / result.Orders, 2);
            return result;
        }
    }
}
