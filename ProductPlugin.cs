using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace LocalShoppingAgent
{
    // Define a clean structural record for items
    public record Product(int Id, string Name, string Category, decimal Price, int Stock);

    public class ProductPlugin
    {
        // Removed 'readonly' so we can add new products dynamically to the list.
        private List<Product> _catalog = new()
        {
            new Product(101, "RGB Mechanical Keyboard", "Electronics", 79.99m, 12),
            new Product(102, "Wireless Gaming Mouse", "Electronics", 49.99m, 0), // Out of stock
            new Product(103, "4K UltraWide 32\" Monitor", "Electronics", 349.99m, 5),
            new Product(104, "Noise Cancelling Headphones", "Audio", 129.99m, 20),
            new Product(105, "Leather Waterproof Backpack", "Apparel", 65.00m, 8)
        };

        [KernelFunction, Description("Adds a new product to the catalog. The system automatically assigns a new unique product ID.")]
        public string AddProduct(
            [Description("The name of the product to add (e.g., 'Ergonomic Office Chair').")] string name,
            [Description("The category the product belongs to (e.g., 'Furniture', 'Electronics').")] string category,
            [Description("The unit price of the product.")] decimal price,
            [Description("The initial stock quantity of the product.")] int stock)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(name))
                return "Error: Product name cannot be empty.";
                
            if (string.IsNullOrWhiteSpace(category))
                return "Error: Category cannot be empty.";

            if (price < 0)
                return "Error: Price cannot be negative.";

            if (stock < 0)
                return "Error: Stock cannot be negative.";

            // Generate a new sequential ID
            int nextId = _catalog.Any() ? _catalog.Max(p => p.Id) + 1 : 101;

            var newProduct = new Product(nextId, name, category, price, stock);
            _catalog.Add(newProduct);

            return $"Success! Added new product to catalog:\n{JsonSerializer.Serialize(newProduct, new JsonSerializerOptions { WriteIndented = true })}";
        }

        [KernelFunction, Description("Searches the shopping catalog for products matching a generic keyword or category description.")]
        public string SearchCatalog(
            [Description("The keyword to search for in product titles or categories (e.g., 'keyboard', 'Audio')")] string query)
        {
            var matches = _catalog
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                            p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!matches.Any()) 
                return $"No products found matching term '{query}'.";

            return JsonSerializer.Serialize(matches, new JsonSerializerOptions { WriteIndented = true });
        }

        [KernelFunction, Description("Calculates promotional quotes by processing a subtotal value against a coupon code.")]
        public string ApplyDiscount(
            [Description("The total price of the items in the cart.")] decimal cartTotal, 
            [Description("The promo coupon code text string (e.g., 'SAVE10', 'WELCOME5')")] string promoCode)
        {
            decimal discount = promoCode.ToUpper() switch
            {
                "SAVE10" => 0.10m,
                "WELCOME5" => 0.05m,
                _ => 0.00m
            };

            if (discount == 0.00m)
                return $"Coupon '{promoCode}' is invalid. Total remains: ${cartTotal}";

            decimal savings = cartTotal * discount;
            decimal finalPrice = cartTotal - savings;

            return $"Success! Applied {discount * 100}% off. Saved: ${savings:F2}. New Total: ${finalPrice:F2}";
        }
    }
}