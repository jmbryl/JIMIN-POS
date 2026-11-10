using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

namespace EnterprisePOS.Services
{
    public static class CsvExporter
    {
        /// <summary>Writes several tables to one UTF-8 CSV file, each preceded by its title.</summary>
        public static void Write(string path, string[] titles, DataTable[] tables)
        {
            if (titles == null || tables == null || titles.Length != tables.Length)
                throw new ArgumentException("Each table needs a title.");

            using (StreamWriter w = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                for (int i = 0; i < tables.Length; i++)
                {
                    w.WriteLine(Cell(titles[i], true));

                    string[] header = new string[tables[i].Columns.Count];
                    for (int c = 0; c < header.Length; c++) header[c] = Cell(tables[i].Columns[c].ColumnName, true);
                    w.WriteLine(string.Join(",", header));

                    foreach (DataRow row in tables[i].Rows)
                    {
                        string[] cells = new string[header.Length];
                        for (int c = 0; c < cells.Length; c++) cells[c] = Cell(row[c], false);
                        w.WriteLine(string.Join(",", cells));
                    }

                    w.WriteLine();
                }
            }
        }

        private static string Cell(object value, bool forceText)
        {
            if (value == null || value is DBNull) return string.Empty;

            string s;
            if (value is DateTime)
            {
                DateTime d = (DateTime)value;
                s = d.TimeOfDay == TimeSpan.Zero ? d.ToString("yyyy-MM-dd") : d.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (value is IFormattable && !(value is string))
            {
                s = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            }
            else
            {
                s = Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            // Stop spreadsheet apps from running text that starts with a formula character.
            if ((value is string || forceText) && s.Length > 0 && "=+-@\t\r".IndexOf(s[0]) >= 0)
                s = "'" + s;

            if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                s = "\"" + s.Replace("\"", "\"\"") + "\"";

            return s;
        }
    }
}
