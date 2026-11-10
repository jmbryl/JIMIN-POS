using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Data
{
    public sealed class OrderRepository
    {
        /// <summary>
        /// Creates the order through dbo.usp_Order_Create. The procedure runs its own transaction,
        /// takes prices from the database and checks stock, so the UI never decrements stock itself.
        /// </summary>
        public int Create(int userId, int? customerId, string paymentMethod, decimal discount,
                          decimal taxRatePercent, decimal amountPaid, IEnumerable<CartLine> lines)
        {
            XElement items = new XElement("Items");
            foreach (CartLine l in lines)
            {
                items.Add(new XElement("Item",
                    new XAttribute("ProductId", l.ProductId),
                    new XAttribute("Quantity", l.Quantity)));
            }

            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Order_Create", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.AddParam("@UserId", SqlDbType.Int, userId);
                cmd.AddParam("@CustomerId", SqlDbType.Int, customerId.HasValue ? (object)customerId.Value : null);
                cmd.AddParam("@PaymentMethod", SqlDbType.NVarChar, paymentMethod, 20);
                cmd.AddMoney("@DiscountAmount", discount);

                SqlParameter tax = cmd.Parameters.Add("@TaxRate", SqlDbType.Decimal);
                tax.Precision = 5;
                tax.Scale = 2;
                tax.Value = taxRatePercent;

                cmd.AddMoney("@AmountPaid", amountPaid);
                cmd.AddParam("@ItemsXml", SqlDbType.Xml, items.ToString(SaveOptions.DisableFormatting));

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Reads a committed order back from the database so receipts always show saved values.</summary>
        public ReceiptData GetReceipt(int orderId)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT o.OrderId, o.OrderNumber, o.OrderDate, o.Subtotal, o.TaxAmount, o.DiscountAmount,
                         o.Total, o.PaymentMethod, o.AmountPaid, o.ChangeDue, o.Status,
                         u.FullName AS Cashier, ISNULL(c.FullName, N'') AS Customer
                  FROM dbo.Orders o
                  JOIN dbo.Users u ON u.UserId = o.UserId
                  LEFT JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
                  WHERE o.OrderId = @Id;

                  SELECT p.Name, d.Quantity, d.UnitPrice, d.LineTotal
                  FROM dbo.OrderDetails d
                  JOIN dbo.Products p ON p.ProductId = d.ProductId
                  WHERE d.OrderId = @Id
                  ORDER BY d.OrderDetailId;", con))
            {
                cmd.AddParam("@Id", SqlDbType.Int, orderId);
                con.Open();

                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                        throw new InvalidOperationException("Order not found.");

                    ReceiptData receipt = new ReceiptData
                    {
                        OrderId = Convert.ToInt32(r["OrderId"]),
                        OrderNumber = Convert.ToString(r["OrderNumber"]),
                        Date = Convert.ToDateTime(r["OrderDate"]),
                        Subtotal = Convert.ToDecimal(r["Subtotal"]),
                        Tax = Convert.ToDecimal(r["TaxAmount"]),
                        Discount = Convert.ToDecimal(r["DiscountAmount"]),
                        Total = Convert.ToDecimal(r["Total"]),
                        PaymentMethod = Convert.ToString(r["PaymentMethod"]),
                        AmountTendered = Convert.ToDecimal(r["AmountPaid"]),
                        Change = Convert.ToDecimal(r["ChangeDue"]),
                        Status = Convert.ToString(r["Status"]),
                        CashierName = Convert.ToString(r["Cashier"]),
                        CustomerName = Convert.ToString(r["Customer"])
                    };

                    if (r.NextResult())
                    {
                        while (r.Read())
                        {
                            receipt.Lines.Add(new ReceiptLine
                            {
                                ProductName = Convert.ToString(r["Name"]),
                                Quantity = Convert.ToInt32(r["Quantity"]),
                                UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
                                Amount = Convert.ToDecimal(r["LineTotal"])
                            });
                        }
                    }

                    return receipt;
                }
            }
        }

        public DataTable Search(DateTime from, DateTime to, string orderNumberText)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT o.OrderId, o.OrderNumber, o.OrderDate, u.FullName AS Cashier,
                         ISNULL(c.FullName, N'Walk-in') AS Customer, o.PaymentMethod, o.Total, o.Status
                  FROM dbo.Orders o
                  JOIN dbo.Users u ON u.UserId = o.UserId
                  LEFT JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
                  WHERE o.OrderDate >= @From AND o.OrderDate < DATEADD(DAY, 1, @To)
                    AND (@Like IS NULL OR o.OrderNumber LIKE @Like ESCAPE '\')
                  ORDER BY o.OrderDate DESC, o.OrderId DESC;", con))
            {
                cmd.AddParam("@From", SqlDbType.DateTime, from.Date);
                cmd.AddParam("@To", SqlDbType.DateTime, to.Date);
                cmd.AddParam("@Like", SqlDbType.NVarChar, Db.LikePattern(orderNumberText), 40);
                return Db.Fill(cmd);
            }
        }

        /// <summary>Marks a completed order as Voided and puts its stock back, all in one transaction.</summary>
        public void Void(int orderId, int userId)
        {
            using (SqlConnection con = Db.GetConnection())
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        string orderNumber;

                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE dbo.Orders SET Status = N'Voided'
                              OUTPUT INSERTED.OrderNumber
                              WHERE OrderId = @Id AND Status = N'Completed';", con, tx))
                        {
                            cmd.AddParam("@Id", SqlDbType.Int, orderId);
                            object result = cmd.ExecuteScalar();
                            if (result == null)
                                throw new InvalidOperationException("The order was not found or has already been voided.");
                            orderNumber = Convert.ToString(result);
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            @"UPDATE p SET StockQty = p.StockQty + d.Quantity, UpdatedAt = GETDATE()
                              FROM dbo.Products p
                              JOIN dbo.OrderDetails d ON d.ProductId = p.ProductId
                              WHERE d.OrderId = @Id;

                              INSERT dbo.InventoryLog (ProductId, ChangeQty, Reason, UserId)
                              SELECT d.ProductId, d.Quantity, N'Void ' + @Number, @UserId
                              FROM dbo.OrderDetails d
                              WHERE d.OrderId = @Id;", con, tx))
                        {
                            cmd.AddParam("@Id", SqlDbType.Int, orderId);
                            cmd.AddParam("@Number", SqlDbType.NVarChar, orderNumber, 30);
                            cmd.AddParam("@UserId", SqlDbType.Int, userId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
