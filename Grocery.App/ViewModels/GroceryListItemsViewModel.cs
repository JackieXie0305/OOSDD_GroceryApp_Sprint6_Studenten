using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

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
        [ObservableProperty]
        private ObservableCollection<string> categories = new(["Alle", "Zuivel", "Bakkerij", "Conserven", "Overig"]);

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

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id))
                MyGroceryListItems.Add(item);

            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            foreach (Product p in _productService.GetAll())
            {
                bool notInList = MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null;
                bool nameMatch = string.IsNullOrWhiteSpace(searchText) || p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                bool categoryMatch = string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "Alle" ||
                                     p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);
                bool priceMatch = !MaxPrice.HasValue || p.Price <= MaxPrice.Value;
                bool stockMatch = !OnlyInStock || p.Stock > 0;

                if (notInList && nameMatch && categoryMatch && priceMatch && stockMatch)
                    AvailableProducts.Add(p);
            }
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public void ApplyFilters()
        {
            GetAvailableProducts();
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> parameter = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, parameter);
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
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
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
