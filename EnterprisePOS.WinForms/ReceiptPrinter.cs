using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    /// <summary>Prints or previews a receipt (sized for an 80 mm thermal roll) from committed order data.</summary>
    internal sealed class ReceiptPrinter : IReceiptPrinter
    {
        private readonly PrintDocument document = new PrintDocument();
        private readonly Font normal = new Font("Consolas", 8.5f);
        private readonly Font bold = new Font("Consolas", 8.5f, FontStyle.Bold);
        private ReceiptData current;

        public ReceiptPrinter()
        {
            document.DocumentName = "JIMIN-POS Receipt";
            document.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 315, 1000);
            document.DefaultPageSettings.Margins = new Margins(12, 12, 12, 12);
            document.PrintPage += PrintPage;
        }

        public void Print(ReceiptData receipt)
        {
            if (receipt == null) throw new ArgumentNullException("receipt");
            current = receipt;

            using (PrintDialog dialog = new PrintDialog { Document = document, AllowSomePages = false })
            {
                if (dialog.ShowDialog() == DialogResult.OK) document.Print();
            }
        }

        public void Preview(ReceiptData receipt)
        {
            if (receipt == null) throw new ArgumentNullException("receipt");
            current = receipt;

            using (PrintPreviewDialog preview = new PrintPreviewDialog { Document = document, Width = 520, Height = 760 })
            {
                preview.ShowDialog();
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            int x = e.MarginBounds.Left;
            int y = e.MarginBounds.Top;
            int width = e.MarginBounds.Width;
            string rule = new string('-', 40);

            Center(g, current.BusinessName, bold, ref y, x, width);
            if (!string.IsNullOrWhiteSpace(current.Address)) Center(g, current.Address, normal, ref y, x, width);
            if (!string.IsNullOrWhiteSpace(current.Phone)) Center(g, "Tel: " + current.Phone, normal, ref y, x, width);
            if (string.Equals(current.Status, "Voided", StringComparison.OrdinalIgnoreCase))
                Center(g, "*** VOIDED ***", bold, ref y, x, width);

            Left(g, rule, normal, ref y, x);
            Left(g, "Order  : " + current.OrderNumber, normal, ref y, x);
            Left(g, "Date   : " + current.Date.ToString("yyyy-MM-dd HH:mm"), normal, ref y, x);
            Left(g, "Cashier: " + current.CashierName, normal, ref y, x);
            if (!string.IsNullOrWhiteSpace(current.CustomerName))
                Left(g, "Customer: " + current.CustomerName, normal, ref y, x);
            Left(g, rule, normal, ref y, x);
            Left(g, string.Format("{0,-21} {1,3} {2,13}", "Product", "Qty", "Amount"), bold, ref y, x);

            foreach (ReceiptLine line in current.Lines)
            {
                string name = line.ProductName ?? string.Empty;
                if (name.Length > 21) name = name.Substring(0, 21);
                Left(g, string.Format("{0,-21} {1,3} {2,13}", name, line.Quantity, Money.Format(line.Amount)), normal, ref y, x);
            }

            Left(g, rule, normal, ref y, x);
            Right(g, "Subtotal: " + Money.Format(current.Subtotal), normal, ref y, x, width);
            Right(g, "Tax: " + Money.Format(current.Tax), normal, ref y, x, width);
            Right(g, "Discount: " + Money.Format(current.Discount), normal, ref y, x, width);
            Right(g, "TOTAL: " + Money.Format(current.Total), bold, ref y, x, width);
            Left(g, rule, normal, ref y, x);
            Left(g, "Payment: " + current.PaymentMethod, normal, ref y, x);
            Right(g, "Paid: " + Money.Format(current.AmountTendered), normal, ref y, x, width);
            Right(g, "Change: " + Money.Format(current.Change), normal, ref y, x, width);

            y += 12;
            Center(g, "Thank you for your purchase!", bold, ref y, x, width);
            e.HasMorePages = false;
        }

        private static void Left(Graphics g, string text, Font font, ref int y, int x)
        {
            g.DrawString(text, font, Brushes.Black, x, y);
            y += font.Height + 2;
        }

        private static void Center(Graphics g, string text, Font font, ref int y, int x, int width)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Black, x + (width - size.Width) / 2f, y);
            y += font.Height + 2;
        }

        private static void Right(Graphics g, string text, Font font, ref int y, int x, int width)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Black, x + width - size.Width, y);
            y += font.Height + 2;
        }
    }
}
