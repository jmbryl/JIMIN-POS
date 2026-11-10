using System;
using System.Data;
using System.Data.SqlClient;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Data
{
    public sealed class ProductRepository
    {
        private const string SelectColumns =
            "p.ProductId, p.Sku, p.Barcode, p.Name, p.CategoryId, p.Price, p.Cost, " +
            "p.StockQty, p.ReorderLevel, p.IsActive";

        // ---------------------------------------------------------------- queries

        /// <summary>Active products joined with their category. Text matches SKU, barcode or name.</summary>
        public DataTable Search(string text, bool lowStockOnly, bool includeInactive)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT " + SelectColumns + @", c.Name AS CategoryName
                  FROM dbo.Products p
                  JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
                  WHERE (p.IsActive = 1 OR @IncludeInactive = 1)
                    AND (@Like IS NULL
                         OR p.Sku  LIKE @Like ESCAPE '\'
                         OR p.Barcode LIKE @Like ESCAPE '\'
                         OR p.Name LIKE @Like ESCAPE '\')
                    AND (@LowOnly = 0 OR p.StockQty <= p.ReorderLevel)
                  ORDER BY p.Name;", con))
            {
                cmd.AddParam("@Like", SqlDbType.NVarChar, Db.LikePattern(text), 220);
                cmd.AddParam("@LowOnly", SqlDbType.Bit, lowStockOnly);
                cmd.AddParam("@IncludeInactive", SqlDbType.Bit, includeInactive);
                return Db.Fill(cmd);
            }
        }

        public DataTable GetAllProducts()
        {
            return Search(null, false, false);
        }

        /// <summary>Exact SKU or barcode match (used by the scanner). Returns null when not found.</summary>
        public Product GetByCode(string code)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TOP (1) " + SelectColumns.Replace("p.", "") +
                " FROM dbo.Products WHERE IsActive = 1 AND (Sku = @Code OR Barcode = @Code);", con))
            {
                cmd.AddParam("@Code", SqlDbType.NVarChar, code, 50);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    return r.Read() ? ReadProduct(r) : null;
                }
            }
        }

        /// <summary>Loads one product including its image bytes.</summary>
        public Product GetById(int productId)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "SELECT " + SelectColumns.Replace("p.", "") + ", Image FROM dbo.Products WHERE ProductId = @Id;", con))
            {
                cmd.AddParam("@Id", SqlDbType.Int, productId);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    Product p = ReadProduct(r);
                    if (!(r["Image"] is DBNull)) p.Image = (byte[])r["Image"];
                    return p;
                }
            }
        }

        public DataTable GetCategories()
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "SELECT CategoryId, Name FROM dbo.Categories ORDER BY Name;", con))
            {
                return Db.Fill(cmd);
            }
        }

        public DataTable GetInventoryLog(int productId, int top)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT TOP (@Top) l.CreatedAt AS [When], l.ChangeQty AS [Change], l.Reason, u.Username AS [By]
                  FROM dbo.InventoryLog l
                  JOIN dbo.Users u ON u.UserId = l.UserId
                  WHERE l.ProductId = @ProductId
                  ORDER BY l.CreatedAt DESC, l.LogId DESC;", con))
            {
                cmd.AddParam("@Top", SqlDbType.Int, top);
                cmd.AddParam("@ProductId", SqlDbType.Int, productId);
                return Db.Fill(cmd);
            }
        }

        // ---------------------------------------------------------------- commands

        public int Add(Product p, int userId)
        {
            try
            {
                using (SqlConnection con = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Product_Insert", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.AddParam("@Sku", SqlDbType.NVarChar, p.Sku, 30);
                    cmd.AddParam("@Barcode", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode, 50);
                    cmd.AddParam("@Name", SqlDbType.NVarChar, p.Name, 150);
                    cmd.AddParam("@CategoryId", SqlDbType.Int, p.CategoryId);
                    cmd.AddMoney("@Price", p.SellingPrice);
                    cmd.AddMoney("@Cost", p.CostPrice);
                    cmd.AddParam("@StockQty", SqlDbType.Int, p.StockQuantity);
                    cmd.AddParam("@ReorderLevel", SqlDbType.Int, p.ReorderLevel);
                    cmd.AddParam("@Image", SqlDbType.VarBinary, p.Image, -1);
                    cmd.AddParam("@UserId", SqlDbType.Int, userId);
                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (SqlException ex)
            {
                string friendly = Db.FriendlyMessage(ex);
                if (friendly != null) throw new InvalidOperationException(friendly, ex);
                throw;
            }
        }

        public void Update(Product p, bool changeImage)
        {
            try
            {
                using (SqlConnection con = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Product_Update", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.AddParam("@ProductId", SqlDbType.Int, p.Id);
                    cmd.AddParam("@Sku", SqlDbType.NVarChar, p.Sku, 30);
                    cmd.AddParam("@Barcode", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode, 50);
                    cmd.AddParam("@Name", SqlDbType.NVarChar, p.Name, 150);
                    cmd.AddParam("@CategoryId", SqlDbType.Int, p.CategoryId);
                    cmd.AddMoney("@Price", p.SellingPrice);
                    cmd.AddMoney("@Cost", p.CostPrice);
                    cmd.AddParam("@ReorderLevel", SqlDbType.Int, p.ReorderLevel);
                    cmd.AddParam("@IsActive", SqlDbType.Bit, p.IsActive);
                    cmd.AddParam("@Image", SqlDbType.VarBinary, p.Image, -1);
                    cmd.AddParam("@ChangeImage", SqlDbType.Bit, changeImage);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                string friendly = Db.FriendlyMessage(ex);
                if (friendly != null) throw new InvalidOperationException(friendly, ex);
                throw;
            }
        }

        public void DeleteProduct(int productId)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Product_Delete", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.AddParam("@ProductId", SqlDbType.Int, productId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static DataTable AdjustInventory(int productId, int changeQty, string reason, int userId)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Inventory_Adjust", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.AddParam("@ProductId", SqlDbType.Int, productId);
                cmd.AddParam("@ChangeQty", SqlDbType.Int, changeQty);
                cmd.AddParam("@Reason", SqlDbType.NVarChar, reason, 200);
                cmd.AddParam("@UserId", SqlDbType.Int, userId);
                return Db.Fill(cmd);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static Product ReadProduct(SqlDataReader r)
        {
            return new Product
            {
                Id = Convert.ToInt32(r["ProductId"]),
                Sku = Convert.ToString(r["Sku"]),
                Barcode = r["Barcode"] is DBNull ? null : Convert.ToString(r["Barcode"]),
                Name = Convert.ToString(r["Name"]),
                CategoryId = Convert.ToInt32(r["CategoryId"]),
                CostPrice = Convert.ToDecimal(r["Cost"]),
                SellingPrice = Convert.ToDecimal(r["Price"]),
                StockQuantity = Convert.ToInt32(r["StockQty"]),
                ReorderLevel = Convert.ToInt32(r["ReorderLevel"]),
                IsActive = Convert.ToBoolean(r["IsActive"])
            };
        }
    }
}
