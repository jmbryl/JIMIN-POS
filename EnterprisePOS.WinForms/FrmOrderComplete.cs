using System;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Domain;

namespace EnterprisePOS.WinForms
{
    internal sealed class FrmOrderComplete : ThemedDialog
    {
        private readonly ReceiptData receipt;

        public FrmOrderComplete(ReceiptData receipt)
            : base("Sale completed", "The order was saved and stock was updated", 460, 400)
        {
            this.receipt = receipt;

            AddRow("Order number", Theme.Lbl(receipt.OrderNumber, Theme.Bold, Theme.Text));
            AddRow("Payment", Theme.Lbl(receipt.PaymentMethod, Theme.Regular, Theme.Text));
            AddRow("Total", Theme.Lbl(Theme.Money(receipt.Total), Theme.Bold, Theme.Text));
            AddRow("Amount paid", Theme.Lbl(Theme.Money(receipt.AmountTendered), Theme.Regular, Theme.Text));
            AddRow("Change due", Theme.Lbl(Theme.Money(receipt.Change), new Font("Segoe UI", 16f, FontStyle.Bold), Theme.Teal));

            Button done = Theme.Btn("New sale", ButtonKind.Success, 110);
            Button print = Theme.Btn("Print receipt", ButtonKind.Primary, 120);
            Button preview = Theme.Btn("Preview", ButtonKind.Neutral, 90);
            done.DialogResult = DialogResult.OK;
            print.Click += (s, e) => Run(true);
            preview.Click += (s, e) => Run(false);

            AddButton(done);
            AddButton(print);
            AddButton(preview);
            AcceptButton = done;
            CancelButton = done;
        }

        // A printer problem must never affect the saved order, so it is reported but not rethrown.
        private void Run(bool print)
        {
            try
            {
                ReceiptPrinter printer = new ReceiptPrinter();
                if (print) printer.Print(receipt); else printer.Preview(receipt);
            }
            catch (Exception ex)
            {
                AppLog.Write(ex);
                Theme.Warn(this, "The receipt could not be printed. The sale itself was saved.\n\n" + ex.Message);
            }
        }
    }
}
