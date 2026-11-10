using System;
using System.Configuration;
using System.Globalization;

namespace EnterprisePOS.Services
{
    /// <summary>Reads optional business settings from the &lt;appSettings&gt; section of App.config.</summary>
    public static class AppSettings
    {
        public static decimal TaxRatePercent
        {
            get
            {
                decimal value;
                string text = ConfigurationManager.AppSettings["TaxRatePercent"];
                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                    && value >= 0m && value <= 100m)
                    return value;
                return 8m;
            }
        }

        public static string BusinessName { get { return Read("BusinessName", "JIMIN-POS"); } }
        public static string BusinessAddress { get { return Read("BusinessAddress", "123 Main Street, City"); } }
        public static string BusinessPhone { get { return Read("BusinessPhone", ""); } }
        public static string CurrencySymbol { get { return Read("CurrencySymbol", "\u20B1"); } }

        private static string Read(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
