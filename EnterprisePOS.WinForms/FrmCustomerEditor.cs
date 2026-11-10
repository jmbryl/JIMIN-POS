using System;
using System.Windows.Forms;
using EnterprisePOS.Domain;
using EnterprisePOS.Services;

namespace EnterprisePOS.WinForms
{
    internal sealed class FrmCustomerEditor : ThemedDialog
    {
        private readonly TextBox txtName = Theme.Input(100);
        private readonly TextBox txtPhone = Theme.Input(30);
        private readonly TextBox txtEmail = Theme.Input(120);
        private readonly TextBox txtAddress = Theme.Input(250);

        public int CreatedId { get; private set; }

        public FrmCustomerEditor()
            : base("New customer", "Add a customer to attach to sales", 460, 380)
        {
            AddRow("Full name *", txtName);
            AddRow("Phone", txtPhone);
            AddRow("Email", txtEmail);
            AddRow("Address", txtAddress);

            Button save = Theme.Btn("Save customer", ButtonKind.Success, 130);
            Button cancel = Theme.Btn("Cancel", ButtonKind.Neutral, 90);
            cancel.DialogResult = DialogResult.Cancel;
            save.Click += (s, e) => Save();

            AddButton(save);
            AddButton(cancel);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void Save()
        {
            try
            {
                CreatedId = new CustomerService().Create(new Customer
                {
                    FullName = txtName.Text,
                    Phone = txtPhone.Text,
                    Email = txtEmail.Text,
                    Address = txtAddress.Text
                });
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Theme.Warn(this, ex.Message);
            }
        }
    }
}
