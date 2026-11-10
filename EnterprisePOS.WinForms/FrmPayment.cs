using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    internal sealed class FrmPayment : ThemedDialog
    {
        private readonly decimal total;
        private readonly ComboBox cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Regular };
        private readonly NumericUpDown numTendered = Theme.Num(0m, 100000000m, 2);
        private readonly Label lblChange = new Label { Font = Theme.Heading, ForeColor = Theme.Teal, AutoSize = true };
        private readonly FlowLayoutPanel quick = new FlowLayoutPanel { WrapContents = true, Height = 84 };
        private readonly Button btnOk = Theme.Btn("Complete sale", ButtonKind.Success, 140);

        public string PaymentMethod { get; private set; }
        public decimal AmountPaid { get; private set; }

        public FrmPayment(decimal total)
            : base("Payment", "Choose how the customer is paying", 460, 470)
        {
            this.total = total;

            Label due = new Label
            {
                Text = "Total due:  " + Theme.Money(total),
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Theme.Navy,
                AutoSize = true
            };
            AddWide(due);

            cmbMethod.Items.AddRange(SalesService.PaymentMethods);
            cmbMethod.SelectedIndex = 0;
            AddRow("Payment method", cmbMethod);

            numTendered.Value = Math.Min(total, numTendered.Maximum);
            AddRow("Amount tendered", numTendered);

            AddRow("Change due", lblChange);

            foreach (decimal amount in QuickAmounts(total))
            {
                decimal value = amount;
                Button b = Theme.Btn(value == total ? "Exact" : Theme.Money(value), ButtonKind.Neutral, 90);
                b.Margin = new Padding(0, 0, 6, 6);
                b.Click += (s, e) => numTendered.Value = Math.Min(value, numTendered.Maximum);
                quick.Controls.Add(b);
            }
            AddWide(quick);

            btnOk.Click += (s, e) => Complete();
            Button cancel = Theme.Btn("Cancel", ButtonKind.Neutral, 90);
            cancel.DialogResult = DialogResult.Cancel;
            AddButton(btnOk);
            AddButton(cancel);
            AcceptButton = btnOk;
            CancelButton = cancel;

            cmbMethod.SelectedIndexChanged += (s, e) => Refresh2();
            numTendered.ValueChanged += (s, e) => Refresh2();
            Refresh2();
        }

        private static IEnumerable<decimal> QuickAmounts(decimal total)
        {
            List<decimal> list = new List<decimal> { total };
            foreach (decimal step in new[] { 50m, 100m, 500m, 1000m })
            {
                decimal rounded = Math.Ceiling(total / step) * step;
                if (!list.Contains(rounded)) list.Add(rounded);
            }
            return list;
        }

        private bool IsCash { get { return cmbMethod.Text == "Cash"; } }

        private void Refresh2()
        {
            numTendered.Enabled = IsCash;
            quick.Enabled = IsCash;

            decimal paid = IsCash ? numTendered.Value : total;
            lblChange.Text = Theme.Money(Math.Max(0m, paid - total));
            btnOk.Enabled = paid >= total;
        }

        private void Complete()
        {
            PaymentMethod = cmbMethod.Text;
            AmountPaid = IsCash ? numTendered.Value : total;
            DialogResult = DialogResult.OK;
        }
    }
}
