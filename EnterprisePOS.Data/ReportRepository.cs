using System;
using System.Data;
using System.Data.SqlClient;

namespace EnterprisePOS.Data
{
    public sealed class ReportRepository
    {
        /// <summary>Columns: SaleDate, OrderCount, Revenue.</summary>
        public DataTable DailySales(DateTime from, DateTime to)
        {
            return Run("dbo.usp_Report_DailySales", from, to, null);
        }

        /// <summary>Columns: ProductName, QtySold, Revenue.</summary>
        public DataTable TopProducts(DateTime from, DateTime to, int top)
        {
            return Run("dbo.usp_Report_TopProducts", from, to, top);
        }

        /// <summary>Columns: CategoryName, Revenue.</summary>
        public DataTable SalesByCategory(DateTime from, DateTime to)
        {
            return Run("dbo.usp_Report_SalesByCategory", from, to, null);
        }

        private static DataTable Run(string procedure, DateTime from, DateTime to, int? top)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(procedure, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.AddParam("@From", SqlDbType.DateTime, from.Date);
                cmd.AddParam("@To", SqlDbType.DateTime, to.Date);
                if (top.HasValue) cmd.AddParam("@Top", SqlDbType.Int, top.Value);
                return Db.Fill(cmd);
            }
        }
    }
}
