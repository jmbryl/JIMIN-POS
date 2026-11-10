using System;
using System.Data;
using System.Data.SqlClient;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Data
{
    public sealed class UserRepository
    {
        public DataTable GetAll()
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT u.UserId, u.Username, u.FullName, u.RoleId, r.RoleName,
                         u.IsActive, u.CreatedAt, u.LastLoginAt
                  FROM dbo.Users u
                  JOIN dbo.Roles r ON r.RoleId = u.RoleId
                  ORDER BY u.Username;", con))
            {
                return Db.Fill(cmd);
            }
        }

        public DataTable GetRoles()
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "SELECT RoleId, RoleName, Description FROM dbo.Roles ORDER BY RoleId;", con))
            {
                return Db.Fill(cmd);
            }
        }

        public AppUser GetById(int userId)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT u.UserId, u.Username, u.FullName, u.RoleId, r.RoleName, u.IsActive, u.LastLoginAt
                  FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId = u.RoleId
                  WHERE u.UserId = @Id;", con))
            {
                cmd.AddParam("@Id", SqlDbType.Int, userId);
                con.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new AppUser
                    {
                        UserId = Convert.ToInt32(r["UserId"]),
                        Username = Convert.ToString(r["Username"]),
                        FullName = Convert.ToString(r["FullName"]),
                        RoleId = Convert.ToInt32(r["RoleId"]),
                        RoleName = Convert.ToString(r["RoleName"]),
                        IsActive = Convert.ToBoolean(r["IsActive"]),
                        LastLoginAt = r["LastLoginAt"] is DBNull ? (DateTime?)null : Convert.ToDateTime(r["LastLoginAt"])
                    };
                }
            }
        }

        public int CountActiveAdmins(int excludeUserId)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.Users u
                  JOIN dbo.Roles r ON r.RoleId = u.RoleId
                  WHERE r.RoleName = N'Admin' AND u.IsActive = 1 AND u.UserId <> @Id;", con))
            {
                cmd.AddParam("@Id", SqlDbType.Int, excludeUserId);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int Create(string username, string fullName, int roleId, string password)
        {
            string salt = PasswordHasher.NewSalt();
            string hash = PasswordHasher.Hash(salt, password);

            try
            {
                using (SqlConnection con = Db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT dbo.Users (Username, PasswordHash, Salt, FullName, RoleId, IsActive)
                      OUTPUT INSERTED.UserId
                      VALUES (@Username, @Hash, @Salt, @FullName, @RoleId, 1);", con))
                {
                    cmd.AddParam("@Username", SqlDbType.NVarChar, username, 50);
                    cmd.AddParam("@Hash", SqlDbType.VarChar, hash, 64);
                    cmd.AddParam("@Salt", SqlDbType.VarChar, salt, 32);
                    cmd.AddParam("@FullName", SqlDbType.NVarChar, fullName, 100);
                    cmd.AddParam("@RoleId", SqlDbType.Int, roleId);
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

        public void Update(int userId, string fullName, int roleId, bool isActive)
        {
            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.Users SET FullName = @FullName, RoleId = @RoleId, IsActive = @IsActive WHERE UserId = @Id;", con))
            {
                cmd.AddParam("@FullName", SqlDbType.NVarChar, fullName, 100);
                cmd.AddParam("@RoleId", SqlDbType.Int, roleId);
                cmd.AddParam("@IsActive", SqlDbType.Bit, isActive);
                cmd.AddParam("@Id", SqlDbType.Int, userId);
                con.Open();
                if (cmd.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("User not found.");
            }
        }

        public void ResetPassword(int userId, string newPassword)
        {
            string salt = PasswordHasher.NewSalt();
            string hash = PasswordHasher.Hash(salt, newPassword);

            using (SqlConnection con = Db.GetConnection())
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.Users SET PasswordHash = @Hash, Salt = @Salt WHERE UserId = @Id;", con))
            {
                cmd.AddParam("@Hash", SqlDbType.VarChar, hash, 64);
                cmd.AddParam("@Salt", SqlDbType.VarChar, salt, 32);
                cmd.AddParam("@Id", SqlDbType.Int, userId);
                con.Open();
                if (cmd.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException("User not found.");
            }
        }
    }
}
