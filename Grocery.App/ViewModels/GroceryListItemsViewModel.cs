using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Globalization;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;
        private string searchText = "";

        [ObservableProperty] private string? selectedCategory;
        [ObservableProperty] private decimal? maxPrice;
        [ObservableProperty] private bool onlyInStock;
        [ObservableProperty] private DateOnly? maxShelfLife;
        [ObservableProperty] private DateTime? maxShelfLifeDate;

        [ObservableProperty]
        private ObservableCollection<string> categories =
            new(["Alle", "Zuivel", "Bakkerij", "Conserven", "Overig"]);

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        [ObservableProperty]
        string myMessage;

        public GroceryListItemsViewModel(
            IGroceryListItemsService groceryListItemsService,
            IProductService productService,
            IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;
            Load(groceryList.Id);
        }

        partial void OnMaxShelfLifeDateChanged(DateTime? value)
        {
            MaxShelfLife = value.HasValue ? DateOnly.FromDateTime(value.Value.Date) : null;
            GetAvailableProducts();
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id))
                MyGroceryListItems.Add(item);

            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            var allProducts = _productService.GetAll();
            var filtered = new List<Product>();

            foreach (Product p in allProducts)
            {
                bool notInList = MyGroceryListItems.All(g => g.ProductId != p.Id);
                bool nameMatch = string.IsNullOrWhiteSpace(searchText)
                    || p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                bool categoryMatch = string.IsNullOrEmpty(SelectedCategory)
                    || SelectedCategory == "Alle"
                    || p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);
                bool priceMatch = !MaxPrice.HasValue || p.Price <= MaxPrice.Value;
                bool stockMatch = !OnlyInStock || p.Stock > 0;

                bool shelfLifeMatch = true;
                if (MaxShelfLife.HasValue)
                    shelfLifeMatch = p.ShelfLife <= MaxShelfLife.Value;

                if (notInList && nameMatch && categoryMatch && priceMatch && stockMatch && shelfLifeMatch)
                    filtered.Add(p);
            }

            AvailableProducts.Clear();
            foreach (var item in filtered)
                AvailableProducts.Add(item);

            if (AvailableProducts.Count == 0)
                MyMessage = "Geen producten voldoen aan de ingestelde filters.";
            else
                MyMessage = string.Empty;
        }

        partial void OnGroceryListChanged(GroceryList value) => Load(value.Id);

        [RelayCommand]
        public void ApplyFilters() => GetAvailableProducts();

        [RelayCommand]
        public async Task ChangeColor()
        {
            var param = new Dictionary<string, object> { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, param);
        }

        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);
            AvailableProducts.Remove(product);
            OnGroceryListChanged(GroceryList);
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string json = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", json, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }

        [RelayCommand]
        public void PerformSearch(string searchText)
        {
            this.searchText = searchText;
            GetAvailableProducts();
        }

        [RelayCommand]
        public void IncreaseAmount(int productId)
        {
            GroceryListItem? item = MyGroceryListItems.FirstOrDefault(x => x.ProductId == productId);
            if (item == null) return;
            if (item.Amount >= item.Product.Stock) return;

            item.Amount++;
            _groceryListItemsService.Update(item);
            item.Product.Stock--;
            _productService.Update(item.Product);
            OnGroceryListChanged(GroceryList);
        }

        [RelayCommand]
        public void DecreaseAmount(int productId)
        {
            GroceryListItem? item = MyGroceryListItems.FirstOrDefault(x => x.ProductId == productId);
            if (item == null) return;
            if (item.Amount <= 0) return;

            item.Amount--;
            _groceryListItemsService.Update(item);
            item.Product.Stock++;
            _productService.Update(item.Product);
            OnGroceryListChanged(GroceryList);
        }
    }
}
