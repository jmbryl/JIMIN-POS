using System;
using System.Collections.Generic;
using System.Globalization;

namespace EnterprisePOS.Domain
{
    /// <summary>Actions that are protected by role. Enforced in the service layer.</summary>
    public enum Permission
    {
        SellProducts,
        EditProducts,
        AdjustInventory,
        ViewAnalytics,
        VoidOrders,
        ManageUsers
    }

    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Cashier = "Cashier";
    }

    public sealed class AppUser
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>The signed-in user for this workstation.</summary>
    public static class Session
    {
        public static AppUser CurrentUser { get; private set; }

        public static bool IsAuthenticated { get { return CurrentUser != null; } }

        public static void SignIn(AppUser user) { CurrentUser = user; }

        public static void SignOut() { CurrentUser = null; }

        public static bool Can(Permission permission)
        {
            if (CurrentUser == null) return false;

            string role = CurrentUser.RoleName ?? string.Empty;
            bool admin = string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
            bool manager = admin || string.Equals(role, RoleNames.Manager, StringComparison.OrdinalIgnoreCase);

            switch (permission)
            {
                case Permission.SellProducts: return true;     // every signed-in role can sell
                case Permission.ManageUsers: return admin;     // admin only
                default: return manager;                       // manager + admin
            }
        }

        public static void RequireSignedIn()
        {
            if (CurrentUser == null)
                throw new UnauthorizedAccessException("You must sign in first.");
        }

        public static void Require(Permission permission)
        {
            RequireSignedIn();
            if (!Can(permission))
                throw new UnauthorizedAccessException("You do not have permission to perform this action.");
        }
    }

    public sealed class Customer
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public sealed class CartLine
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal LineTotal { get { return Quantity * UnitPrice; } }
    }

    public sealed class ReceiptLine
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class ReceiptData
    {
        public string BusinessName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime Date { get; set; }
        public string CashierName { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        private readonly List<ReceiptLine> lines = new List<ReceiptLine>();
        public List<ReceiptLine> Lines { get { return lines; } }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public decimal AmountTendered { get; set; }
        public decimal Change { get; set; }
    }

    public static class Money
    {
        /// <summary>Currency symbol shown in the UI. Set at startup from App.config.</summary>
        public static string Symbol = "\u20B1"; // Philippine peso

        public static string Format(decimal value)
        {
            return Symbol + value.ToString("N2", CultureInfo.CurrentCulture);
        }
    }
}
