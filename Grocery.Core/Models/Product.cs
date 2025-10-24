using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Grocery.Core.Models
{
    public partial class Product : Model
    {
        [ObservableProperty]
        public int stock;

        public DateOnly ShelfLife { get; set; }

        [RegularExpression(@"^\\d+\\.\\d{0,2}$")]
        [Range(0, 999.99)]
        public decimal Price { get; set; } = 0;

        public string Category { get; set; } = "Overig";

        public Product(int id, string name, int stock)
            : this(id, name, stock, default, 0, "Overig") { }

        public Product(int id, string name, int stock, DateOnly shelfLife)
            : this(id, name, stock, shelfLife, 0, "Overig") { }

        public Product(int id, string name, int stock, DateOnly shelfLife, decimal price, string category)
            : base(id, name)
        {
            Stock = stock;
            ShelfLife = shelfLife;
            Price = price;
            Category = category;
        }

        public override string? ToString()
        {
            return $"{Name} - {Stock} op voorraad";
        }
    }
}
