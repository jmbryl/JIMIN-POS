using System;
using System.Data;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    public sealed class InventoryService
    {
        private readonly ProductRepository repository = new ProductRepository();

        public DataTable Search(string text, bool lowStockOnly)
        {
            Session.RequireSignedIn();
            return repository.Search(text, lowStockOnly, false);
        }

        public Product GetByCode(string code)
        {
            Session.RequireSignedIn();
            if (string.IsNullOrWhiteSpace(code)) return null;
            return repository.GetByCode(code.Trim());
        }

        public Product GetById(int productId)
        {
            Session.RequireSignedIn();
            return repository.GetById(productId);
        }

        public DataTable GetCategories()
        {
            Session.RequireSignedIn();
            return repository.GetCategories();
        }

        public DataTable GetLog(int productId)
        {
            Session.Require(Permission.EditProducts);
            return repository.GetInventoryLog(productId, 50);
        }

        /// <summary>Adds (Id = 0) or updates a product. Image is only written when changeImage is true (or on add).</summary>
        public int Save(Product p, byte[] image, bool changeImage)
        {
            Session.Require(Permission.EditProducts);
            Validate(p);

            p.Sku = p.Sku.Trim();
            p.Name = p.Name.Trim();
            p.Barcode = string.IsNullOrWhiteSpace(p.Barcode) ? null : p.Barcode.Trim();
            p.Image = image;

            if (p.Id == 0)
                return repository.Add(p, Session.CurrentUser.UserId);

            repository.Update(p, changeImage);
            return p.Id;
        }

        public void Deactivate(int productId)
        {
            Session.Require(Permission.EditProducts);
            repository.DeleteProduct(productId);
        }

        public int Adjust(int productId, int change, string reason)
        {
            Session.Require(Permission.AdjustInventory);

            if (change == 0)
                throw new ArgumentException("Enter a non-zero quantity. Use a positive number to add stock and a negative number to remove it.");
            if (Math.Abs((long)change) > 1000000)
                throw new ArgumentException("That adjustment is too large.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Please enter the reason for the adjustment.");
            if (reason.Trim().Length > 200)
                throw new ArgumentException("The reason must be 200 characters or fewer.");

            DataTable result = ProductRepository.AdjustInventory(productId, change, reason.Trim(), Session.CurrentUser.UserId);
            return result.Rows.Count > 0 ? Convert.ToInt32(result.Rows[0]["NewStock"]) : 0;
        }

        private static void Validate(Product p)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (string.IsNullOrWhiteSpace(p.Sku)) throw new ArgumentException("SKU is required.");
            if (p.Sku.Trim().Length > 30) throw new ArgumentException("SKU must be 30 characters or fewer.");
            if (string.IsNullOrWhiteSpace(p.Name)) throw new ArgumentException("Product name is required.");
            if (p.Name.Trim().Length > 150) throw new ArgumentException("Product name must be 150 characters or fewer.");
            if (!string.IsNullOrWhiteSpace(p.Barcode) && p.Barcode.Trim().Length > 50)
                throw new ArgumentException("Barcode must be 50 characters or fewer.");
            if (p.CategoryId <= 0) throw new ArgumentException("Choose a category.");
            if (p.SellingPrice < 0m) throw new ArgumentException("Selling price cannot be negative.");
            if (p.CostPrice < 0m) throw new ArgumentException("Cost price cannot be negative.");
            if (p.Id == 0 && p.StockQuantity < 0) throw new ArgumentException("Stock cannot be negative.");
            if (p.ReorderLevel < 0) throw new ArgumentException("Reorder level cannot be negative.");
        }
    }
}
