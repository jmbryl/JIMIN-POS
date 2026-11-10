using System;
using System.IO;

namespace EnterprisePOS.WinForms
{
    internal static class AppLog
    {
        public static string LogFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JIMIN-POS", "logs");
            }
        }

        public static void Write(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(LogFolder);
                string file = Path.Combine(LogFolder, "error.log");
                File.AppendAllText(file,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    (ex == null ? "(unknown error)" : ex.ToString()) + Environment.NewLine +
                    new string('-', 60) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
