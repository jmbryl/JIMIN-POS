using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Domain;

namespace EnterprisePOS.WinForms
{
    internal sealed class FrmDashboard : Form
    {
        private sealed class NavItem
        {
            public string Key;
            public string Caption;
            public Button Button;
            public Func<PageBase> Create;
        }

        private readonly Panel content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Page };
        private readonly List<NavItem> items = new List<NavItem>();
        private PageBase current;

        public FrmDashboard()
        {
            Text = Theme.AppName + " | POS & Inventory";
            MinimumSize = new Size(1180, 720);
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Theme.Regular;
            BackColor = Theme.Page;
            KeyPreview = true;

            Panel sidebar = BuildSidebar();
            Controls.Add(content);
            Controls.Add(sidebar);

            Load += (s, e) => ShowPage("dashboard");
        }

        private Panel BuildSidebar()
        {
            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Sidebar };

            FlowLayoutPanel nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Theme.Sidebar,
                Padding = new Padding(0, 8, 0, 0)
            };

            AddNav(nav, "dashboard", "Dashboard", true, () => new PageDashboard());
            AddNav(nav, "pos", "Point of Sale  (F2)", Session.Can(Permission.SellProducts), () => new PagePos());
            AddNav(nav, "inventory", "Inventory", Session.Can(Permission.EditProducts), () => new PageInventory());
            AddNav(nav, "analytics", "Sales Analytics", Session.Can(Permission.ViewAnalytics), () => new PageAnalytics());
            AddNav(nav, "history", "Sales History", Session.Can(Permission.ViewAnalytics), () => new PageHistory());
            AddNav(nav, "users", "Users & Roles", Session.Can(Permission.ManageUsers), () => new PageUsers());

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 118, BackColor = Theme.Sidebar };
            Button logout = MakeNavButton("Log out");
            logout.Location = new Point(0, 0);
            logout.Click += (s, e) =>
            {
                if (Theme.Confirm(this, "Are you sure you want to log out?")) Close();
            };

            Label who = new Label
            {
                Text = Session.CurrentUser.FullName + "\n" + Session.CurrentUser.RoleName,
                ForeColor = Theme.SidebarText,
                Font = Theme.Small,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 52, 230, 54)
            };
            bottom.Controls.Add(logout);
            bottom.Controls.Add(who);

            Label subtitle = new Label
            {
                Text = "POS & Inventory System",
                ForeColor = Theme.SidebarText,
                Font = Theme.Small,
                AutoSize = false,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Top,
                Height = 30
            };
            Label brand = new Label
            {
                Text = Theme.AppName,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 64
            };

            sidebar.Controls.Add(nav);
            sidebar.Controls.Add(bottom);
            sidebar.Controls.Add(subtitle);
            sidebar.Controls.Add(brand);
            return sidebar;
        }

        private Button MakeNavButton(string caption)
        {
            Button b = new Button
            {
                Text = caption,
                Width = 230,
                Height = 46,
                Margin = new Padding(0),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(22, 0, 0, 0),
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Theme.SidebarText,
                BackColor = Theme.Sidebar,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
            b.FlatAppearance.MouseDownBackColor = Theme.SidebarActive;
            return b;
        }

        private void AddNav(FlowLayoutPanel nav, string key, string caption, bool allowed, Func<PageBase> create)
        {
            if (!allowed) return;

            NavItem item = new NavItem { Key = key, Caption = caption, Create = create, Button = MakeNavButton(caption) };
            item.Button.Click += (s, e) => ShowPage(key);
            items.Add(item);
            nav.Controls.Add(item.Button);
        }

        public void ShowPage(string key)
        {
            NavItem target = items.Find(i => i.Key == key);
            if (target == null) return;

            foreach (NavItem i in items)
            {
                bool active = i == target;
                i.Button.BackColor = active ? Theme.SidebarActive : Theme.Sidebar;
                i.Button.ForeColor = active ? Color.White : Theme.SidebarText;
                i.Button.Font = new Font("Segoe UI", 10.5f, active ? FontStyle.Bold : FontStyle.Regular);
            }

            PageBase old = current;
            content.SuspendLayout();
            content.Controls.Clear();

            PageBase page = target.Create();
            page.NavigateTo = ShowPage;
            content.Controls.Add(page);
            content.ResumeLayout(true);

            current = page;
            if (old != null) old.Dispose();

            page.OnPageShown();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F2 && items.Exists(i => i.Key == "pos"))
            {
                PagePos open = current as PagePos;
                if (open != null) open.FocusScan(); 
                else ShowPage("pos");
                return true;
            }

            if (keyData == Keys.F9)
            {
                PagePos pos = current as PagePos;
                if (pos != null)
                {
                    pos.Checkout();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
