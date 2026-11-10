using System;

namespace EnterprisePOS.Domain
{
    public sealed class Product
    {
        public int Id;
        public string Sku;
        public string Barcode;
        public string Name;
        public int CategoryId;
        public decimal CostPrice;
        public decimal SellingPrice;
        public int StockQuantity;
        public int ReorderLevel;
        public string ImagePath;      // kept for compatibility with the tutorial model
        public byte[] Image;          // Products.Image (varbinary) - only loaded by GetById
        public bool IsActive;
    }
}
