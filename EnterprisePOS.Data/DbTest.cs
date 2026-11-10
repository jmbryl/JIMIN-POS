using System;

namespace EnterprisePOS.Data
{
    public static class DbTest
    {
        public static bool TestConnection(out string message)
        {
            try
            {
                using (var connection = Db.GetConnection())
                {
                    connection.Open();
                    message = "Connected successfully to the Inoc database!";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }
}
