using System;
using System.Data;
using System.Data.SqlClient;

namespace EnterprisePOS.Data
{
    public sealed class DashboardSummary
    {
        public decimal TodaySales;
        public int TodayOrders;
        public int LowStockCount;
        public int ActiveProducts;
    }

    public sealed class DashboardRepository
    {
        public DashboardSummary GetSummary()
        {
            using (SqlConnection connection = Db.GetConnection())
            using (SqlCommand command = new SqlCommand("dbo.usp_Report_Dashboard", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new InvalidOperationException("Dashboard data could not be loaded.");

                    return new DashboardSummary
                    {
                        TodaySales = Convert.ToDecimal(reader["TodaySales"]),
                        TodayOrders = Convert.ToInt32(reader["TodayOrders"]),
                        LowStockCount = Convert.ToInt32(reader["LowStockCount"]),
                        ActiveProducts = Convert.ToInt32(reader["ActiveProducts"])
                    };
                }
            }
        }

        // Kept for compatibility with the original code.
        public decimal GetTodaySales() { return GetSummary().TodaySales; }
        public int GetTodayOrders() { return GetSummary().TodayOrders; }
        public int GetLowStockCount() { return GetSummary().LowStockCount; }
        public int GetActiveProducts() { return GetSummary().ActiveProducts; }

        public DataTable GetLowStock()
        {
            using (SqlConnection connection = Db.GetConnection())
            using (SqlCommand command = new SqlCommand("dbo.usp_Report_LowStock", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                return Db.Fill(command);
            }
        }
    }
}
