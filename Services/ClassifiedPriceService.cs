using Newtonsoft.Json.Linq;
using static Tf2TradingBotv1.Services.ScrapUtils;

namespace Tf2TradingBotv1.Services
{
    public class ClassifiedPriceService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey;

        public ClassifiedPriceService(string backpackTfApiKey)
        {
            _apiKey = backpackTfApiKey;
        }

        
        public async Task<Dictionary<string, (int BuyScrap, int SellScrap)>>
            GetPricesAsync(IEnumerable<string> items, int costScrap = 0)
        {
            // 0) Fetch a fresh key→ref rate
            decimal refPerKey = await FetchKeyRefRateAsync();

            var result = new Dictionary<string, (int, int)>();

            foreach (var item in items)
            {
                // 1) Pull raw classifieds
                var sells = await FetchListingsAsync(item, intent: 1, refPerKey);
                var buys = await FetchListingsAsync(item, intent: 0, refPerKey);

                if (sells.Count == 0 || buys.Count == 0)
                    continue;  // skip if we have no market data

                // 2) Trim top/bottom 10%
                var goodSells = TrimOutliers(sells, 0.10);
                var goodBuys = TrimOutliers(buys, 0.10);

                // 3) Compute market points
                int minSell = goodSells.Min();
                int maxBuy = goodBuys.Max();

                // 4) Undercut/overbid by 1 scrap, respecting your floor
                int sellScrap = Math.Max(minSell - 1, costScrap);
                int buyScrap = maxBuy + 1;

                result[item] = (buyScrap, sellScrap);
            }

            return result;
        }

        
        private async Task<decimal> FetchKeyRefRateAsync()
        {
            const string keyName = "Mann Co. Supply Crate Key";

            // intent=1 pulls sell orders
            var sells = await FetchListingsAsync(keyName, intent: 1, refPerKey: 9m/*dummy*/);
            // Convert each scrap→ref ( scrap / 9 )
            var sellRefs = sells
                .Select(s => Math.Round(s / (decimal)ScrapPerRef, 2))
                .OrderBy(r => r)
                .ToList();

            if (sellRefs.Count == 0)
                throw new InvalidOperationException("No key sell listings found.");

            int mid = sellRefs.Count / 2;
            return (sellRefs.Count % 2 == 1)
                ? sellRefs[mid]
                : (sellRefs[mid - 1] + sellRefs[mid]) / 2;
        }

       
        private async Task<List<int>> FetchListingsAsync(
            string itemName,
            int intent,
            decimal refPerKey)
        {
            var url = $"https://backpack.tf/api/IGetClassifieds/v1?" +
                      $"key={_apiKey}&intent={intent}" +
                      $"&item_name={Uri.EscapeDataString(itemName)}";
            Console.WriteLine("→ Testing key URL: " + url);

            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return new List<int>();

            var text = await resp.Content.ReadAsStringAsync();
            var cls = JObject.Parse(text)["response"]["classifieds"];

            var prices = new List<int>();
            foreach (var c in cls)
            {
                int keys = (int?)c["currencies"]["keys"] ?? 0;
                decimal metalRef = (decimal?)c["currencies"]["metal"] ?? 0m;

                // Convert everything → scrap
                int scrap = ToScrap(
                    keys: keys,
                    refined: 0,
                    reclaimed: 0,
                    scrap: (int)Math.Round(metalRef * ScrapPerRef),
                    refPerKey: refPerKey
                );

                // Skip weird “spell” or “effect” listings
                var details = (string)c["details"] ?? "";
                if (details.Contains("spell", StringComparison.OrdinalIgnoreCase) ||
                    details.Contains("effect", StringComparison.OrdinalIgnoreCase))
                    continue;

                prices.Add(scrap);
            }
            return prices;
        }

        private static List<int> TrimOutliers(List<int> data, double pct)
        {
            data.Sort();
            int drop = (int)(data.Count * pct);
            return data.Skip(drop).Take(data.Count - 2 * drop).ToList();
        }
    }
}
