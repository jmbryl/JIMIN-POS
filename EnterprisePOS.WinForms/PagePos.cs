using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    /// <summary>Point of sale: barcode scan / product search, cart, customer, discount and checkout.</summary>
    internal sealed class PagePos : PageBase
    {
        private readonly InventoryService inventory = new InventoryService();
        private readonly SalesService sales = new SalesService();
        private readonly CustomerService customers = new CustomerService();
        private readonly BindingList<CartLine> cart = new BindingList<CartLine>();

        private readonly TextBox txtScan = Theme.Input(50);
        private readonly TextBox txtFind = Theme.Input(100);
        private readonly NumericUpDown numQty = Theme.Num(1m, 999m, 0);
        private readonly DataGridView gridProducts = Theme.Grid();
        private readonly DataGridView gridCart = Theme.Grid();
        private readonly Label lblStatus = new Label();

        private readonly ComboBox cmbCustomer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Regular };
        private readonly NumericUpDown numDiscount = Theme.Num(0m, 10000000m, 2);
        private readonly Label lblSubtotal = new Label();
        private readonly Label lblTaxCaption = new Label();
        private readonly Label lblTax = new Label();
        private readonly Label lblTotal = new Label();
        private readonly Button btnCheckout = Theme.Btn("CHECKOUT  (F9)", ButtonKind.Success, 200);

        public PagePos()
        {
            TableLayoutPanel root = CreateRoot("Point of Sale", "Scan or search products, build the cart, then check out.");

            TableLayoutPanel split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            split.Controls.Add(BuildLeft(), 0, 0);
            split.Controls.Add(BuildRight(), 1, 0);
            root.Controls.Add(split, 0, 1);

            numQty.Value = 1;
            RecalculateTotals();
        }

        // ================================================================ layout

        private Control BuildLeft()
        {
            TableLayoutPanel left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 0, 14, 0)
            };
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Scan row
            Panel scan = new Panel { Dock = DockStyle.Fill };
            Label lblScan = Theme.Lbl("Scan barcode / enter SKU, then press Enter:", Theme.Bold, Theme.Text);
            lblScan.Location = new Point(0, 0);
            txtScan.Font = Theme.Entry;
            txtScan.SetBounds(0, 24, 400, 30);
            txtScan.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtScan.KeyDown += ScanKeyDown;
            lblStatus.AutoSize = true;
            lblStatus.Font = Theme.Small;
            lblStatus.ForeColor = Theme.Muted;
            lblStatus.Location = new Point(0, 62);
            scan.Controls.Add(lblScan);
            scan.Controls.Add(txtScan);
            scan.Controls.Add(lblStatus);

            // Search row
            FlowLayoutPanel search = CreateToolbar();
            Label lblFind = Theme.Lbl("Find:");
            lblFind.Margin = new Padding(0, 9, 4, 0);
            txtFind.Width = 190;
            txtFind.Margin = new Padding(0, 5, 8, 0);
            txtFind.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadProducts(); }
            };
            Button btnSearch = Theme.Btn("Search", ButtonKind.Primary, 80);
            btnSearch.Click += (s, e) => LoadProducts();
            Label lblQty = Theme.Lbl("Qty:");
            lblQty.Margin = new Padding(8, 9, 4, 0);
            numQty.Width = 60;
            numQty.Margin = new Padding(0, 5, 10, 0);
            Button btnAdd = Theme.Btn("Add to cart", ButtonKind.Success, 110);
            btnAdd.Click += (s, e) => AddSelectedProduct();
            search.Controls.Add(lblFind);
            search.Controls.Add(txtFind);
            search.Controls.Add(btnSearch);
            search.Controls.Add(lblQty);
            search.Controls.Add(numQty);
            search.Controls.Add(btnAdd);

            // Product grid
            Theme.Col(gridProducts, "Name", "Product", null, false, 62f);
            Theme.Col(gridProducts, "Price", "Price", "N2", true, 20f);
            Theme.Col(gridProducts, "StockQty", "Stock", "N0", true, 18f);
            gridProducts.DataBindingComplete += (s, e) => Theme.TintLowStock(gridProducts);
            gridProducts.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AddSelectedProduct(); };

            left.Controls.Add(scan, 0, 0);
            left.Controls.Add(search, 0, 1);
            left.Controls.Add(gridProducts, 0, 2);
            return left;
        }

        private Control BuildRight()
        {
            TableLayoutPanel right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 258));

            // Cart toolbar
            FlowLayoutPanel bar = CreateToolbar();
            Button btnPlus = Theme.Btn("+", ButtonKind.Primary, 42);
            Button btnMinus = Theme.Btn("\u2212", ButtonKind.Primary, 42);
            Button btnRemove = Theme.Btn("Remove", ButtonKind.Neutral, 90);
            Button btnClear = Theme.Btn("Clear cart", ButtonKind.Danger, 100);
            btnPlus.Click += (s, e) => ChangeQuantity(1);
            btnMinus.Click += (s, e) => ChangeQuantity(-1);
            btnRemove.Click += (s, e) => RemoveSelected();
            btnClear.Click += (s, e) => ClearCart(true);
            bar.Controls.Add(btnPlus);
            bar.Controls.Add(btnMinus);
            bar.Controls.Add(btnRemove);
            bar.Controls.Add(btnClear);

            // Cart grid
            Theme.Col(gridCart, "ProductName", "Item", null, false, 46f);
            Theme.Col(gridCart, "UnitPrice", "Price", "N2", true, 18f);
            Theme.Col(gridCart, "Quantity", "Qty", "N0", true, 12f);
            Theme.Col(gridCart, "LineTotal", "Total", "N2", true, 24f);
            gridCart.DataSource = cart;

            // Totals panel
            Panel totals = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(14, 10, 14, 10) };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 6 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            t.Controls.Add(Caption("Customer"), 0, 0);
            cmbCustomer.Dock = DockStyle.Fill;
            cmbCustomer.Margin = new Padding(0, 4, 4, 4);
            t.Controls.Add(cmbCustomer, 1, 0);
            Button btnNewCustomer = Theme.Btn("+", ButtonKind.Success, 34);
            btnNewCustomer.Dock = DockStyle.Fill;
            btnNewCustomer.Margin = new Padding(0, 3, 0, 3);
            btnNewCustomer.Click += (s, e) => NewCustomer();
            t.Controls.Add(btnNewCustomer, 2, 0);

            t.Controls.Add(Caption("Discount"), 0, 1);
            numDiscount.Dock = DockStyle.Fill;
            numDiscount.Margin = new Padding(0, 4, 4, 4);
            numDiscount.ValueChanged += (s, e) => RecalculateTotals();
            t.Controls.Add(numDiscount, 1, 1);

            t.Controls.Add(Caption("Subtotal"), 0, 2);
            StyleValue(lblSubtotal, Theme.Regular);
            t.Controls.Add(lblSubtotal, 1, 2);
            t.SetColumnSpan(lblSubtotal, 2);

            lblTaxCaption.AutoSize = true;
            lblTaxCaption.Font = Theme.Regular;
            lblTaxCaption.ForeColor = Theme.Muted;
            lblTaxCaption.Anchor = AnchorStyles.Left;
            t.Controls.Add(lblTaxCaption, 0, 3);
            StyleValue(lblTax, Theme.Regular);
            t.Controls.Add(lblTax, 1, 3);
            t.SetColumnSpan(lblTax, 2);

            Label totalCaption = Caption("TOTAL");
            totalCaption.Font = Theme.Bold;
            totalCaption.ForeColor = Theme.Text;
            t.Controls.Add(totalCaption, 0, 4);
            StyleValue(lblTotal, new Font("Segoe UI", 22f, FontStyle.Bold));
            lblTotal.ForeColor = Theme.Navy;
            t.Controls.Add(lblTotal, 1, 4);
            t.SetColumnSpan(lblTotal, 2);

            btnCheckout.Dock = DockStyle.Fill;
            btnCheckout.Height = 44;
            btnCheckout.Font = Theme.Heading;
            btnCheckout.Margin = new Padding(0, 6, 0, 0);
            btnCheckout.Click += (s, e) => Checkout();
            t.Controls.Add(btnCheckout, 0, 5);
            t.SetColumnSpan(btnCheckout, 3);

            totals.Controls.Add(t);

            right.Controls.Add(bar, 0, 0);
            right.Controls.Add(gridCart, 0, 1);
            right.Controls.Add(totals, 0, 2);
            return right;
        }

        private static Label Caption(string text)
        {
            Label l = new Label { Text = text, AutoSize = true, Font = Theme.Regular, ForeColor = Theme.Muted, Anchor = AnchorStyles.Left };
            return l;
        }

        private static void StyleValue(Label l, Font font)
        {
            l.AutoSize = false;
            l.Dock = DockStyle.Fill;
            l.TextAlign = ContentAlignment.MiddleRight;
            l.Font = font;
            l.ForeColor = Theme.Text;
        }

        // ================================================================ lifecycle

        public override void OnPageShown()
        {
            LoadCustomers(0);
            LoadProducts();
            FocusScan();
        }

        public void FocusScan()
        {
            ActiveControl = txtScan;
            if (IsHandleCreated)
                BeginInvoke(new Action(() => { txtScan.Focus(); txtScan.SelectAll(); }));
        }

        private void LoadProducts()
        {
            try
            {
                gridProducts.DataSource = inventory.Search(txtFind.Text.Trim(), false);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadCustomers(int selectId)
        {
            try
            {
                List<Customer> list = new List<Customer>();
                list.Add(new Customer { CustomerId = 0, FullName = "Walk-in customer" });
                list.AddRange(customers.GetAll());

                cmbCustomer.DataSource = null;
                cmbCustomer.DisplayMember = "FullName";
                cmbCustomer.ValueMember = "CustomerId";
                cmbCustomer.DataSource = list;

                int index = list.FindIndex(c => c.CustomerId == selectId);
                cmbCustomer.SelectedIndex = index >= 0 ? index : 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private int SelectedCustomerId
        {
            get
            {
                Customer c = cmbCustomer.SelectedItem as Customer;
                return c == null ? 0 : c.CustomerId;
            }
        }

        private void NewCustomer()
        {
            using (FrmCustomerEditor dialog = new FrmCustomerEditor())
            {
                if (dialog.ShowDialog(FindForm()) == DialogResult.OK) LoadCustomers(dialog.CreatedId);
            }
        }

        // ================================================================ cart

        private void SetStatus(string message, bool isError)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = isError ? Theme.Red : Theme.Teal;
        }

        private void ScanKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            string code = txtScan.Text.Trim();
            if (code.Length == 0) return;

            try
            {
                Product p = inventory.GetByCode(code);
                if (p == null)
                {
                    SetStatus("No product found for '" + code + "'.", true);
                    System.Media.SystemSounds.Beep.Play();
                }
                else
                {
                    AddToCart(p, (int)numQty.Value);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                numQty.Value = 1;
                FocusScan();
            }
        }

        private void AddSelectedProduct()
        {
            if (gridProducts.CurrentRow == null) { SetStatus("Select a product first.", true); return; }

            DataRowView row = gridProducts.CurrentRow.DataBoundItem as DataRowView;
            if (row == null) return;

            try
            {
                // Re-read the product so price and stock are current.
                Product p = inventory.GetByCode(Convert.ToString(row["Sku"]));
                if (p == null) { SetStatus("That product is no longer available.", true); LoadProducts(); return; }

                AddToCart(p, (int)numQty.Value);
                numQty.Value = 1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddToCart(Product p, int qty)
        {
            if (p.StockQuantity <= 0)
            {
                SetStatus(p.Name + " is out of stock.", true);
                return;
            }

            CartLine line = cart.FirstOrDefault(l => l.ProductId == p.Id);
            int wanted = (line == null ? 0 : line.Quantity) + qty;

            if (wanted > p.StockQuantity)
            {
                SetStatus("Only " + p.StockQuantity + " of " + p.Name + " in stock.", true);
                return;
            }

            if (line == null)
            {
                cart.Add(new CartLine
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    ProductName = p.Name,
                    UnitPrice = p.SellingPrice,
                    Quantity = qty,
                    AvailableStock = p.StockQuantity
                });
            }
            else
            {
                line.Quantity = wanted;
                line.UnitPrice = p.SellingPrice;
                line.AvailableStock = p.StockQuantity;
                cart.ResetBindings();
            }

            SetStatus("Added: " + p.Name, false);
            RecalculateTotals();
        }

        private CartLine SelectedLine()
        {
            return gridCart.CurrentRow == null ? null : gridCart.CurrentRow.DataBoundItem as CartLine;
        }

        private void ChangeQuantity(int delta)
        {
            CartLine line = SelectedLine();
            if (line == null) return;

            int next = line.Quantity + delta;
            if (next <= 0)
            {
                cart.Remove(line);
            }
            else if (next > line.AvailableStock)
            {
                SetStatus("Only " + line.AvailableStock + " of " + line.ProductName + " in stock.", true);
                return;
            }
            else
            {
                line.Quantity = next;
                cart.ResetBindings();
            }

            RecalculateTotals();
        }

        private void RemoveSelected()
        {
            CartLine line = SelectedLine();
            if (line == null) return;
            cart.Remove(line);
            RecalculateTotals();
        }

        private void ClearCart(bool confirm)
        {
            if (cart.Count == 0) return;
            if (confirm && !Theme.Confirm(FindForm(), "Remove every item from the cart?")) return;

            cart.Clear();
            numDiscount.Value = 0;
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            CartTotals totals = sales.Calculate(cart, numDiscount.Value);

            // The discount can never exceed the subtotal.
            if (numDiscount.Value != totals.Discount)
            {
                numDiscount.Value = totals.Discount;
                return; // ValueChanged runs this method again with the corrected value
            }

            lblSubtotal.Text = Theme.Money(totals.Subtotal);
            lblTaxCaption.Text = "Tax (" + sales.TaxRatePercent.ToString("0.##") + "%)";
            lblTax.Text = Theme.Money(totals.Tax);
            lblTotal.Text = Theme.Money(totals.Total);
            btnCheckout.Enabled = cart.Count > 0;
        }

        // ================================================================ checkout

        public void Checkout()
        {
            if (cart.Count == 0)
            {
                SetStatus("The cart is empty.", true);
                return;
            }

            CartTotals totals = sales.Calculate(cart, numDiscount.Value);
            string method;
            decimal paid;

            using (FrmPayment payment = new FrmPayment(totals.Total))
            {
                if (payment.ShowDialog(FindForm()) != DialogResult.OK) return;
                method = payment.PaymentMethod;
                paid = payment.AmountPaid;
            }

            int orderId;
            try
            {
                int? customerId = SelectedCustomerId > 0 ? (int?)SelectedCustomerId : null;
                orderId = sales.Checkout(customerId, method, totals.Discount, paid, cart.ToList());
            }
            catch (Exception ex)
            {
                ShowError(ex);
                LoadProducts(); // stock may have changed on another terminal
                return;
            }

            // The sale is saved: clear the cart straight away so it can never be submitted twice.
            ClearCart(false);
            cmbCustomer.SelectedIndex = 0;
            LoadProducts();
            SetStatus("Sale completed.", false);

            try
            {
                ReceiptData receipt = sales.GetReceipt(orderId);
                using (FrmOrderComplete done = new FrmOrderComplete(receipt))
                {
                    done.ShowDialog(FindForm());
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            FocusScan();
        }
    }
}
