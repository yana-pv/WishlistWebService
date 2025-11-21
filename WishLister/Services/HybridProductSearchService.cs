using System.Net;
using HtmlAgilityPack;
using WishLister.Models;

namespace WishLister.Services;
public class HybridProductSearchService
{
    private readonly HttpClient _httpClient;

    public HybridProductSearchService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }


    public async Task<List<ProductSearchResult>> SearchProductsAsync(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return GenerateSmartLinks(productName);

        var parsedResults = await TryParseRealProductsAsync(productName);

        if (parsedResults.Any())
        {
            return parsedResults.Take(5).ToList();
        }

        return GenerateSmartLinks(productName);
    }

    private async Task<List<ProductSearchResult>> TryParseRealProductsAsync(string productName)
    {
        var results = new List<ProductSearchResult>();

        var tasks = new List<Task<List<ProductSearchResult>>>
        {
            TryParseCitilinkAsync(productName),
            TryParseDNSAsync(productName),
            TryParseWildberriesAsync(productName)
        };

        var timeoutTask = Task.Delay(5000);
        var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);

        if (completedTask == timeoutTask)
        {
            return results;
        }

        var allResults = await Task.WhenAll(tasks);
        results = allResults.SelectMany(x => x).ToList();
             
        return results;
    }


    private async Task<List<ProductSearchResult>> TryParseCitilinkAsync(string productName)
    {
        var results = new List<ProductSearchResult>();

        try
        {
            var url = $"https://www.citilink.ru/search/?text={Uri.EscapeDataString(productName)}";
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'ProductCardHorizontal')]") ??
                              doc.DocumentNode.SelectNodes("//div[contains(@class, 'product_data')]");

            if (productNodes != null)
            {
                foreach (var node in productNodes.Take(3))
                {
                    try
                    {
                        var titleNode = node.SelectSingleNode(".//a[contains(@class, 'ProductCardHorizontal__title')]") ??
                                      node.SelectSingleNode(".//a[contains(@class, 'product_name')]");
                        var priceNode = node.SelectSingleNode(".//span[contains(@class, 'ProductCardHorizontal__price_current-price')]") ??
                                      node.SelectSingleNode(".//span[contains(@class, 'price')]");

                        if (titleNode != null)
                        {
                            var title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
                            var href = titleNode.GetAttributeValue("href", "");
                            var productUrl = href.StartsWith("http") ? href : "https://www.citilink.ru" + href;
                            var price = ExtractPrice(priceNode?.InnerText);

                            if (productUrl.Contains("/product/"))
                            {
                                results.Add(new ProductSearchResult
                                {
                                    Title = title.Length > 100 ? title.Substring(0, 100) + "..." : title,
                                    Url = productUrl,
                                    Price = price,
                                    Source = "Citilink",
                                    IsDirectLink = true
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка парсинга карточки Citilink: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга Citilink: {ex.Message}");
        }

        return results;
    }


    private async Task<List<ProductSearchResult>> TryParseDNSAsync(string productName)
    {
        var results = new List<ProductSearchResult>();

        try
        {
            var url = $"https://www.dns-shop.ru/search/?q={Uri.EscapeDataString(productName)}&stock=now";
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'catalog-product')]") ??
                              doc.DocumentNode.SelectNodes("//a[contains(@class, 'catalog-product__name')]");

            if (productNodes != null)
            {
                foreach (var node in productNodes.Take(3))
                {
                    try
                    {
                        var titleNode = node.SelectSingleNode(".//a[contains(@class, 'catalog-product__name')]") ??
                                      node;
                        var priceNode = node.SelectSingleNode(".//div[contains(@class, 'product-buy__price')]");

                        if (titleNode != null)
                        {
                            var title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
                            var href = titleNode.GetAttributeValue("href", "");
                            var productUrl = href.StartsWith("http") ? href : "https://www.dns-shop.ru" + href;
                            var price = ExtractPrice(priceNode?.InnerText);

                            if (productUrl.Contains("/product/"))
                            {
                                results.Add(new ProductSearchResult
                                {
                                    Title = title.Length > 100 ? title.Substring(0, 100) + "..." : title,
                                    Url = productUrl,
                                    Price = price,
                                    Source = "DNS",
                                    IsDirectLink = true
                                });
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка парсинга карточки DNS: {ex.Message}");
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга DNS: {ex.Message}");
        }

        return results;
    }


    private async Task<List<ProductSearchResult>> TryParseWildberriesAsync(string productName)
    {
        var results = new List<ProductSearchResult>();

        try
        {
            var url = $"https://www.wildberries.ru/catalog/0/search.aspx?search={Uri.EscapeDataString(productName)}";
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var productNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-card')]") ??
                              doc.DocumentNode.SelectNodes("//a[contains(@class, 'product-card__link')]");

            if (productNodes != null)
            {
                foreach (var node in productNodes.Take(3))
                {
                    try
                    {
                        var titleNode = node.SelectSingleNode(".//span[contains(@class, 'goods-name')]");
                        var linkNode = node.SelectSingleNode(".//a[contains(@class, 'product-card__link')]") ?? node;

                        if (titleNode != null && linkNode != null)
                        {
                            var title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
                            var href = linkNode.GetAttributeValue("href", "");
                            var productUrl = href.StartsWith("http") ? href : "https://www.wildberries.ru" + href;

                            if (productUrl.Contains("/catalog/") && productUrl.Contains("/detail.aspx"))
                            {
                                results.Add(new ProductSearchResult
                                {
                                    Title = title.Length > 100 ? title.Substring(0, 100) + "..." : title,
                                    Url = productUrl,
                                    Source = "Wildberries",
                                    IsDirectLink = true
                                });
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка парсинга карточки Wildberries: {ex.Message}");
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга Wildberries: {ex.Message}");
        }

        return results;
    }


    private List<ProductSearchResult> GenerateSmartLinks(string productName)
    {
        var encodedName = Uri.EscapeDataString(productName);

        return new List<ProductSearchResult>
        {
            new() {
                Title = $"Найти '{productName}' на Ozon",
                Url = $"https://www.ozon.ru/search/?text={encodedName}",
                Source = "Ozon",
                Description = "Откроет страницу поиска на Ozon",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' на Wildberries",
                Url = $"https://www.wildberries.ru/catalog/0/search.aspx?search={encodedName}",
                Source = "Wildberries",
                Description = "Откроет страницу поиска на Wildberries",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' на Яндекс.Маркет",
                Url = $"https://market.yandex.ru/search?text={encodedName}",
                Source = "Яндекс.Маркет",
                Description = "Откроет страницу поиска на Яндекс.Маркет",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' в Ситилинк",
                Url = $"https://www.citilink.ru/search/?text={encodedName}",
                Source = "Ситилинк",
                Description = "Откроет страницу поиска в Ситилинк",
                IsDirectLink = false
            },
            new() {
                Title = $"Найти '{productName}' в DNS",
                Url = $"https://www.dns-shop.ru/search/?q={encodedName}",
                Source = "DNS",
                Description = "Откроет страницу поиска в DNS",
                IsDirectLink = false
            }
        };
    }


    private decimal? ExtractPrice(string priceText)
    {
        if (string.IsNullOrEmpty(priceText))
            return null;

        try
        {
            var cleanPrice = new string(priceText.Where(c => char.IsDigit(c)).ToArray());
            if (long.TryParse(cleanPrice, out long priceLong) && priceLong > 0)
            {
                return priceLong / 100m; 
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}