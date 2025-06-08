using Newtonsoft.Json.Linq;
using Tf2TradingBotIv1.Data;
using Tf2TradingBotv1.Data;

namespace Tf2TradingBotv1.Services
{
    public class PriceFetcher : IDisposable
    {
        private readonly string _apiKey;
        private readonly List<string> _items;
        private readonly Timer _timer;
        private readonly HttpClient _http = new HttpClient();
        private readonly LiteDbService _db;

        // Public cache of computed prices (in refined metal units)
        public event Func<object, Task> OnPricesUpdated;
        public Dictionary<string, (decimal Buy, decimal Sell)> PriceCache { get; }
            = new Dictionary<string, (decimal, decimal)>();

        public PriceFetcher(LiteDbService db, string apiKey, IEnumerable<string> items, int intervalMinutes)
        {
            _db = db;
            _apiKey = apiKey;
            _items = items.ToList();

            // Fire immediately, then every intervalMinutes
            _timer = new Timer(async _ => await RefreshAllPrices(),
                               null,
                               TimeSpan.Zero,
                               TimeSpan.FromMinutes(intervalMinutes));
        }

        private async Task RefreshAllPrices()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Refreshing prices for {_items.Count} items…");

            foreach (var item in _items)
            {
                try
                {
                    var sellList = await FetchListings(item, intent: 1);
                    var buyList = await FetchListings(item, intent: 0);

                    var medianSell = ComputeMedian(sellList);
                    var medianBuy = ComputeMedian(buyList);

                    // 5% spread
                    var buyPrice = Math.Round(medianSell * 0.95m, 2);
                    var sellPrice = Math.Round(medianBuy * 1.05m, 2);

                    PriceCache[item] = (Buy: buyPrice, Sell: sellPrice);

                    _db.Prices.Upsert(new PriceRecord
                    {
                        Id = item,
                        Buy = buyPrice,
                        Sell = sellPrice,
                        LastUpdated = DateTime.UtcNow
                    });

                    Console.WriteLine($" • {item}: Buy={buyPrice} ref, Sell={sellPrice} ref");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" ! Error with {item}: {ex.Message}");
                }
            }
            if (OnPricesUpdated != null)
                await OnPricesUpdated(this);
        }

        private async Task<List<decimal>> FetchListings(string itemName, int intent)
        {
            var baseUrl = "https://backpack.tf/api/IGetClassifieds/v1";
            var query = $"?key={_apiKey}&intent={intent}&item_name={Uri.EscapeDataString(itemName)}";
            var url = baseUrl + query;

            Console.WriteLine($"   → Fetching {itemName} (intent={intent}) from:\n     {url}");

            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"   ! got {resp.StatusCode} fetching classifieds for {itemName}");
                return new List<decimal>();
            }

            var json = await resp.Content.ReadAsStringAsync();
            var listings = JObject.Parse(json)["response"]["classifieds"];

            var prices = listings
                .Select(c => {
                    int keys = (int?)c["currencies"]["keys"] ?? 0;
                    decimal m = (decimal?)c["currencies"]["metal"] ?? 0m;
                    return keys * 50m + m;
                })
                .OrderBy(x => x)
                .ToList();

            int drop = (int)(prices.Count * 0.05);
            return prices.Skip(drop).Take(prices.Count - 2 * drop).ToList();
        }


        private decimal ComputeMedian(List<decimal> sorted)
        {
            if (sorted.Count == 0) return 0m;
            int mid = sorted.Count / 2;
            return (sorted.Count % 2 == 1)
                ? sorted[mid]
                : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _http.Dispose();
        }
    }
}
