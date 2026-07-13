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
        // Our local offline catalog data
        private readonly List<Product> _catalog = new()
        {
            new Product(101, "RGB Mechanical Keyboard", "Electronics", 79.99m, 12),
            new Product(102, "Wireless Gaming Mouse", "Electronics", 49.99m, 0), // Out of stock
            new Product(103, "4K UltraWide 32\" Monitor", "Electronics", 349.99m, 5),
            new Product(104, "Noise Cancelling Headphones", "Audio", 129.99m, 20),
            new Product(105, "Leather Waterproof Backpack", "Apparel", 65.00m, 8)
        };

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