using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Domain;

namespace EnterprisePOS.WinForms
{
    internal enum ButtonKind { Primary, Success, Danger, Neutral }

    /// <summary>Single source of truth for colours, fonts and control styling.</summary>
    internal static class Theme
    {
        public const string AppName = "JIMIN-POS";

        // Palette
        public static readonly Color Sidebar = Color.FromArgb(25, 39, 65);
        public static readonly Color SidebarHover = Color.FromArgb(36, 54, 88);
        public static readonly Color SidebarActive = Color.FromArgb(45, 92, 160);
        public static readonly Color SidebarText = Color.FromArgb(190, 204, 225);
        public static readonly Color Navy = Color.FromArgb(30, 60, 115);
        public static readonly Color Teal = Color.FromArgb(0, 128, 104);
        public static readonly Color Red = Color.FromArgb(196, 40, 40);
        public static readonly Color Purple = Color.FromArgb(94, 53, 177);
        public static readonly Color Page = Color.FromArgb(244, 245, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color Text = Color.FromArgb(33, 37, 41);
        public static readonly Color Muted = Color.FromArgb(108, 117, 125);
        public static readonly Color Border = Color.FromArgb(222, 226, 230);
        public static readonly Color LowStockRow = Color.FromArgb(253, 232, 232);
        public static readonly Color AltRow = Color.FromArgb(248, 249, 252);

        // Fonts (created once and shared)
        public static readonly Font Regular = new Font("Segoe UI", 9.5f);
        public static readonly Font Bold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font Small = new Font("Segoe UI", 9f);
        public static readonly Font Heading = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static readonly Font Title = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static readonly Font Big = new Font("Segoe UI", 24f, FontStyle.Bold);
        public static readonly Font Entry = new Font("Segoe UI", 12f);

        public static string Money(decimal value) { return EnterprisePOS.Domain.Money.Format(value); }

        // ------------------------------------------------------------ controls

        public static Button Btn(string text, ButtonKind kind, int width)
        {
            Button b = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = Bold,
                Margin = new Padding(0, 0, 8, 0),
                UseVisualStyleBackColor = false
            };

            Color back;
            Color fore = Color.White;
            switch (kind)
            {
                case ButtonKind.Success: back = Teal; break;
                case ButtonKind.Danger: back = Red; break;
                case ButtonKind.Neutral: back = Color.White; fore = Text; break;
                default: back = Navy; break;
            }

            b.BackColor = back;
            b.ForeColor = fore;
            b.FlatAppearance.BorderSize = kind == ButtonKind.Neutral ? 1 : 0;
            b.FlatAppearance.BorderColor = Border;
            return b;
        }

        public static Label Lbl(string text, Font font, Color color)
        {
            return new Label
            {
                Text = text,
                Font = font ?? Regular,
                ForeColor = color,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(3, 8, 3, 0)
            };
        }

        public static Label Lbl(string text)
        {
            return Lbl(text, Regular, Text);
        }

        public static TextBox Input(int maxLength)
        {
            return new TextBox { MaxLength = maxLength, Font = Regular, BorderStyle = BorderStyle.FixedSingle };
        }

        public static NumericUpDown Num(decimal min, decimal max, int decimals)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                ThousandsSeparator = true,
                Font = Regular,
                TextAlign = HorizontalAlignment.Right
            };
        }

        public static Panel Card(string title, Color back, out Label value)
        {
            Panel p = new Panel { BackColor = back, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            Label t = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = Regular,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 14)
            };
            value = new Label
            {
                Text = "-",
                ForeColor = Color.White,
                Font = Big,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(14, 40)
            };
            p.Controls.Add(t);
            p.Controls.Add(value);
            return p;
        }

        // ------------------------------------------------------------ grids

        public static DataGridView Grid()
        {
            DataGridView g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Surface,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Border,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 34,
                Font = Regular,
                AutoGenerateColumns = false
            };

            g.RowTemplate.Height = 28;
            g.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Navy;
            g.ColumnHeadersDefaultCellStyle.Font = Bold;
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            g.DefaultCellStyle.ForeColor = Text;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = AltRow;
            return g;
        }

        public static DataGridViewTextBoxColumn Col(DataGridView g, string dataProperty, string header,
                                                    string format, bool alignRight, float weight)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn
            {
                Name = dataProperty,
                DataPropertyName = dataProperty,
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            if (!string.IsNullOrEmpty(format)) c.DefaultCellStyle.Format = format;
            if (alignRight)
            {
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            g.Columns.Add(c);
            return c;
        }

        public static DataGridViewTextBoxColumn Col(DataGridView g, string dataProperty, string header)
        {
            return Col(g, dataProperty, header, null, false, 100f);
        }

        /// <summary>Shades rows whose StockQty is at or below ReorderLevel.</summary>
        public static void TintLowStock(DataGridView g)
        {
            foreach (DataGridViewRow row in g.Rows)
            {
                DataRowView view = row.DataBoundItem as DataRowView;
                if (view == null) continue;

                int stock = Convert.ToInt32(view["StockQty"]);
                int reorder = Convert.ToInt32(view["ReorderLevel"]);
                row.DefaultCellStyle.BackColor = stock <= reorder ? LowStockRow : Color.Empty;
            }
        }

        // ------------------------------------------------------------ messages

        public static void Error(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void Warn(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Info(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static bool Confirm(IWin32Window owner, string message)
        {
            return MessageBox.Show(owner, message, AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
