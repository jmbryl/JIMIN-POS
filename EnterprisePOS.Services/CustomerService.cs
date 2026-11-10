using System;
using System.Collections.Generic;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    public sealed class CustomerService
    {
        private readonly CustomerRepository repository = new CustomerRepository();

        public List<Customer> GetAll()
        {
            Session.RequireSignedIn();
            return repository.GetAll();
        }

        public int Create(Customer customer)
        {
            Session.Require(Permission.SellProducts);

            if (customer == null) throw new ArgumentNullException("customer");
            if (string.IsNullOrWhiteSpace(customer.FullName))
                throw new ArgumentException("Customer name is required.");
            if (customer.FullName.Trim().Length > 100)
                throw new ArgumentException("Customer name must be 100 characters or fewer.");
            if (!string.IsNullOrWhiteSpace(customer.Email) && customer.Email.IndexOf('@') < 1)
                throw new ArgumentException("Enter a valid email address or leave it blank.");

            customer.FullName = customer.FullName.Trim();
            return repository.Create(customer);
        }
    }
}
