using WishLister.Models.Services;

namespace WishLister.Services;
public class HybridProductSearchService
{
    public Task<List<ProductSearchResult>> SearchProductsAsync(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return Task.FromResult(new List<ProductSearchResult>());
        }

        return Task.FromResult(GenerateSearchLinks(productName));
    }

    private List<ProductSearchResult> GenerateSearchLinks(string productName)
    {
        var encodedName = Uri.EscapeDataString(productName);

        return new List<ProductSearchResult>
        {
            new() {
                Title = $"Найти '{productName}' на Ozon",
                Url = $"https://www.ozon.ru/search/?text={encodedName}",
                Source = "Ozon",
                Description = "Поиск на Ozon",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' на Wildberries",
                Url = $"https://www.wildberries.ru/catalog/0/search.aspx?search={encodedName}",
                Source = "Wildberries",
                Description = "Поиск на Wildberries",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' на Яндекс.Маркет",
                Url = $"https://market.yandex.ru/search?text={encodedName}",
                Source = "Яндекс.Маркет",
                Description = "Поиск на Яндекс.Маркет",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' в Ситилинк",
                Url = $"https://www.citilink.ru/search/?text={encodedName}",
                Source = "Ситилинк",
                Description = "Поиск в Ситилинк",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' в DNS",
                Url = $"https://www.dns-shop.ru/search/?q={encodedName}",
                Source = "DNS",
                Description = "Поиск в DNS",
                IsDirectLink = false
            }
        };
    }
}