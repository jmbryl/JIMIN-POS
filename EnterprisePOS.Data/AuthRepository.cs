using System;
using System.Data;
using System.Data.SqlClient;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Data
{
    public sealed class AuthRepository
    {
        private const string InvalidMessage = "Invalid username or password.";

        /// <summary>
        /// Verifies the credentials against dbo.usp_Login. Returns the user on success;
        /// otherwise returns null and sets a message that is safe to show on screen.
        /// </summary>
        public AppUser Authenticate(string username, string password, out string failure)
        {
            failure = null;

            using (SqlConnection con = Db.GetConnection())
            {
                con.Open();

                AppUser user;
                string storedHash;
                string salt;

                using (SqlCommand cmd = new SqlCommand("dbo.usp_Login", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.AddParam("@Username", SqlDbType.NVarChar, username, 50);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            failure = InvalidMessage;
                            return null;
                        }

                        user = new AppUser
                        {
                            UserId = Convert.ToInt32(r["UserId"]),
                            Username = Convert.ToString(r["Username"]),
                            FullName = Convert.ToString(r["FullName"]),
                            RoleId = Convert.ToInt32(r["RoleId"]),
                            RoleName = Convert.ToString(r["RoleName"]),
                            IsActive = Convert.ToBoolean(r["IsActive"])
                        };
                        storedHash = Convert.ToString(r["PasswordHash"]);
                        salt = Convert.ToString(r["Salt"]);
                    }
                }

                if (!PasswordHasher.Verify(salt, password, storedHash))
                {
                    failure = InvalidMessage;
                    return null;
                }

                // Only reveal "inactive" after the password has been proven correct.
                if (!user.IsActive)
                {
                    failure = "This account is inactive. Please contact the administrator.";
                    return null;
                }

                using (SqlCommand touch = new SqlCommand("dbo.usp_User_TouchLogin", con))
                {
                    touch.CommandType = CommandType.StoredProcedure;
                    touch.AddParam("@UserId", SqlDbType.Int, user.UserId);
                    touch.ExecuteNonQuery();
                }

                return user;
            }
        }
    }
}
