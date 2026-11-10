using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace EnterprisePOS.WinForms
{
    /// <summary>Base for every screen hosted inside the main shell, so all pages share one layout.</summary>
    internal abstract class PageBase : UserControl
    {
        /// <summary>Set by the shell so a page can ask to open another page by key.</summary>
        public Action<string> NavigateTo;

        protected PageBase()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.Page;
            Font = Theme.Regular;
            DoubleBuffered = true;
        }

        /// <summary>Called by the shell after the page is added. Load data here, not in the constructor.</summary>
        public virtual void OnPageShown() { }

        /// <summary>Standard page frame: title + subtitle on row 0, content on row 1.</summary>
        protected TableLayoutPanel CreateRoot(string title, string subtitle)
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(24, 14, 24, 18),
                BackColor = Theme.Page
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Panel header = new Panel { Dock = DockStyle.Fill };
            Label lblTitle = new Label
            {
                Text = title,
                Font = Theme.Title,
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Font = Theme.Regular,
                ForeColor = Theme.Muted,
                AutoSize = true,
                Location = new Point(2, 40)
            };
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSubtitle);

            root.Controls.Add(header, 0, 0);
            Controls.Add(root);
            return root;
        }

        protected FlowLayoutPanel CreateToolbar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Theme.Page,
                Margin = new Padding(0)
            };
        }

        protected void ShowError(Exception ex)
        {
            if (ex is UnauthorizedAccessException || ex is ArgumentException || ex is InvalidOperationException)
            {
                Theme.Warn(FindForm(), ex.Message);
                return;
            }

            AppLog.Write(ex);
            SqlException sql = ex as SqlException;
            string message = sql != null && sql.Number >= 50000
                ? sql.Message
                : "Something went wrong while talking to the database.\n\n" + ex.Message;
            Theme.Error(FindForm(), message);
        }
    }
}
