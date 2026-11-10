using System;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ShowUnexpected(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => AppLog.Write(e.ExceptionObject as Exception);

            Money.Symbol = AppSettings.CurrencySymbol;

            Application.Run(new FrmLogin());
        }

        private static void ShowUnexpected(Exception ex)
        {
            AppLog.Write(ex);
            MessageBox.Show(
                "An unexpected error occurred. The details were saved to:\n" + AppLog.LogFolder,
                Theme.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
