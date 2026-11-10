using System;
using System.Drawing;
using System.Windows.Forms;

namespace EnterprisePOS.WinForms
{
    /// <summary>Base for all pop-up dialogs: navy title band, caption/field rows and a right-aligned button bar.</summary>
    internal abstract class ThemedDialog : Form
    {
        protected readonly TableLayoutPanel Table = new TableLayoutPanel();
        protected readonly FlowLayoutPanel Buttons = new FlowLayoutPanel();
        private int rows;

        protected ThemedDialog(string title, string subtitle, int width, int height)
        {
            Text = Theme.AppName + " | " + title;
            ClientSize = new Size(width, height);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = Theme.Regular;
            BackColor = Theme.Page;

            Panel header = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Theme.Navy };
            header.Controls.Add(new Label
            {
                Text = title,
                Font = Theme.Heading,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 10),
                BackColor = Color.Transparent
            });
            header.Controls.Add(new Label
            {
                Text = subtitle,
                Font = Theme.Small,
                ForeColor = Theme.SidebarText,
                AutoSize = true,
                Location = new Point(21, 38),
                BackColor = Color.Transparent
            });

            Panel body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 14, 20, 12), BackColor = Theme.Page };

            Table.Dock = DockStyle.Top;
            Table.AutoSize = true;
            Table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Table.ColumnCount = 2;
            Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Buttons.Dock = DockStyle.Bottom;
            Buttons.Height = 48;
            Buttons.FlowDirection = FlowDirection.RightToLeft;
            Buttons.Padding = new Padding(0, 8, 0, 0);

            body.Controls.Add(Table);
            body.Controls.Add(Buttons);

            Controls.Add(body);
            Controls.Add(header);
        }

        /// <summary>Adds a "caption : control" row.</summary>
        protected void AddRow(string caption, Control control)
        {
            Table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Table.RowCount = ++rows;

            Label label = Theme.Lbl(caption);
            label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(0, 0, 8, 0);

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(3, 5, 3, 5);

            Table.Controls.Add(label, 0, rows - 1);
            Table.Controls.Add(control, 1, rows - 1);
        }

        /// <summary>Adds a control that spans both columns.</summary>
        protected void AddWide(Control control)
        {
            Table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Table.RowCount = ++rows;

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 4, 0, 4);

            Table.Controls.Add(control, 0, rows - 1);
            Table.SetColumnSpan(control, 2);
        }

        /// <summary>Buttons flow right-to-left, so add the primary action first.</summary>
        protected void AddButton(Button button)
        {
            button.Margin = new Padding(8, 0, 0, 0);
            Buttons.Controls.Add(button);
        }
    }
}
