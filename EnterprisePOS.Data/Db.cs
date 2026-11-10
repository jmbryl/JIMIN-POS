using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EnterprisePOS.Data
{
    public static class Db
    {
        public static SqlConnection GetConnection()
        {
            string connectionString =
                ConfigurationManager.ConnectionStrings["EnterprisePOS"].ConnectionString;

            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Converts common SQL Server constraint errors into messages a cashier can understand.
        /// Returns null when the error is not one we recognise.
        /// </summary>
        public static string FriendlyMessage(SqlException ex)
        {
            string m = ex.Message ?? string.Empty;

            if (ex.Number == 2627 || ex.Number == 2601)
            {
                if (m.IndexOf("UQ_Products_Sku", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "That SKU is already used by another product.";
                if (m.IndexOf("UX_Products_Barcode", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "That barcode is already used by another product.";
                if (m.IndexOf("UQ_Users_Username", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "That username is already taken.";
                return "A record with the same unique value already exists.";
            }

            if (ex.Number == 547)
                return "The value conflicts with a database rule (for example a negative price or stock).";

            return null;
        }

        internal static SqlParameter AddParam(this SqlCommand cmd, string name, SqlDbType type, object value, int size = 0)
        {
            SqlParameter p = size != 0 ? cmd.Parameters.Add(name, type, size) : cmd.Parameters.Add(name, type);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        internal static SqlParameter AddMoney(this SqlCommand cmd, string name, decimal value)
        {
            SqlParameter p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = 2;
            p.Value = value;
            return p;
        }

        internal static DataTable Fill(SqlCommand cmd)
        {
            DataTable table = new DataTable();
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                adapter.Fill(table);
            }
            return table;
        }

        /// <summary>Escapes % _ [ \ so user text is matched literally by LIKE ... ESCAPE '\'.</summary>
        internal static string LikePattern(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_")
                .Replace("[", "\\[");
            return "%" + t + "%";
        }
    }
}
