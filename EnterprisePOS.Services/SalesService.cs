using System;
using System.Collections.Generic;
using System.Data;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    public sealed class CartTotals
    {
        public decimal Subtotal;
        public decimal Discount;
        public decimal Tax;
        public decimal Total;
    }

    public sealed class SalesService
    {
        public static readonly string[] PaymentMethods = { "Cash", "Card", "Mobile" };

        private readonly OrderRepository orders = new OrderRepository();

        public decimal TaxRatePercent { get { return AppSettings.TaxRatePercent; } }

        /// <summary>
        /// Mirrors the arithmetic in dbo.usp_Order_Create (round half away from zero) so the
        /// on-screen total matches the total the database will store.
        /// </summary>
        public CartTotals Calculate(IEnumerable<CartLine> lines, decimal discount)
        {
            decimal subtotal = 0m;
            if (lines != null)
            {
                foreach (CartLine line in lines) subtotal += line.LineTotal;
            }

            if (discount < 0m) discount = 0m;
            if (discount > subtotal) discount = subtotal;

            decimal tax = Math.Round((subtotal - discount) * TaxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);

            return new CartTotals
            {
                Subtotal = subtotal,
                Discount = discount,
                Tax = tax,
                Total = subtotal - discount + tax
            };
        }

        public int Checkout(int? customerId, string paymentMethod, decimal discount, decimal amountPaid, IList<CartLine> lines)
        {
            Session.Require(Permission.SellProducts);

            if (lines == null || lines.Count == 0)
                throw new InvalidOperationException("The cart is empty.");
            if (Array.IndexOf(PaymentMethods, paymentMethod) < 0)
                throw new InvalidOperationException("Choose a valid payment method.");

            CartTotals totals = Calculate(lines, discount);
            if (amountPaid < totals.Total)
                throw new InvalidOperationException("The amount paid is less than the order total.");

            return orders.Create(Session.CurrentUser.UserId, customerId, paymentMethod,
                                 totals.Discount, TaxRatePercent, amountPaid, lines);
        }

        public ReceiptData GetReceipt(int orderId)
        {
            Session.RequireSignedIn();

            ReceiptData receipt = orders.GetReceipt(orderId);
            receipt.BusinessName = AppSettings.BusinessName;
            receipt.Address = AppSettings.BusinessAddress;
            receipt.Phone = AppSettings.BusinessPhone;
            return receipt;
        }

        public DataTable SearchOrders(DateTime from, DateTime to, string orderNumberText)
        {
            Session.Require(Permission.ViewAnalytics);
            if (from.Date > to.Date) throw new ArgumentException("The 'from' date must be on or before the 'to' date.");
            return orders.Search(from, to, orderNumberText);
        }

        public void VoidOrder(int orderId)
        {
            Session.Require(Permission.VoidOrders);
            orders.Void(orderId, Session.CurrentUser.UserId);
        }
    }
}
