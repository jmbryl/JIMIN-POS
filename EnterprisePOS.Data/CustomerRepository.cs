using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Data
{
    public sealed class CustomerRepository
    {
        public List<Customer> GetAll()
        {
            List<Customer> list = new List<Customer>();

            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "SELECT CustomerId, FullName, Phone, Email, Address FROM dbo.Customers ORDER BY FullName;", con))
            {
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Customer
                        {
                            CustomerId = Convert.ToInt32(r["CustomerId"]),
                            FullName = Convert.ToString(r["FullName"]),
                            Phone = r["Phone"] is DBNull ? null : Convert.ToString(r["Phone"]),
                            Email = r["Email"] is DBNull ? null : Convert.ToString(r["Email"]),
                            Address = r["Address"] is DBNull ? null : Convert.ToString(r["Address"])
                        });
                    }
                }
            }

            return list;
        }

        public int Create(Customer c)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"INSERT dbo.Customers (FullName, Phone, Email, Address)
                  OUTPUT INSERTED.CustomerId
                  VALUES (@FullName, @Phone, @Email, @Address);", con))
            {
                cmd.AddParam("@FullName", SqlDbType.NVarChar, c.FullName, 100);
                cmd.AddParam("@Phone", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone, 30);
                cmd.AddParam("@Email", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(c.Email) ? null : c.Email, 120);
                cmd.AddParam("@Address", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(c.Address) ? null : c.Address, 250);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
