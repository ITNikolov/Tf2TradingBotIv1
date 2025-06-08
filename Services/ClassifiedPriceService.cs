using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tf2TradingBotIv1.Services;

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
            var result = new Dictionary<string, (int, int)>();

            foreach (var item in items)
            {
                // 1) Pull all sell orders (intent=1) and buy orders (intent=0)
                var sells = await FetchListingsAsync(item, intent: 1);
                var buys = await FetchListingsAsync(item, intent: 0);

                if (sells.Count == 0 || buys.Count == 0)
                    continue;  // no data

                // 2) Trim out top/bottom 10% to remove anomalies
                var goodSells = TrimOutliers(sells, 0.10);
                var goodBuys = TrimOutliers(buys, 0.10);

                // 3) Compute market points
                int minSell = goodSells.Min();
                int maxBuy = goodBuys.Max();

                // 4) Undercut/overbid by exactly 1 scrap, but never sell below costScrap
                int sellScrap = Math.Max(minSell - 1, costScrap);
                int buyScrap = maxBuy + 1;

                result[item] = (buyScrap, sellScrap);
            }

            return result;
        }

        private static List<int> TrimOutliers(List<int> data, double pct)
        {
            data.Sort();
            int drop = (int)(data.Count * pct);
            return data.Skip(drop)
                       .Take(data.Count - 2 * drop)
                       .ToList();
        }

        private async Task<List<int>> FetchListingsAsync(string itemName, int intent)
        {
            // Build URL
            var url = $"https://backpack.tf/api/IGetClassifieds/v1" +
                      $"?key={_apiKey}" +
                      $"&intent={intent}" +
                      $"&item_name={Uri.EscapeDataString(itemName)}";

            var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return new List<int>();

            var text = await resp.Content.ReadAsStringAsync();
            var cls = JObject.Parse(text)["response"]["classifieds"];

            var prices = new List<int>();
            foreach (var c in cls)
            {
                // Convert keys + metal → scrap
                int keys = (int?)c["currencies"]["keys"] ?? 0;
                decimal m = (decimal?)c["currencies"]["metal"] ?? 0m;
                int scrap = ScrapUtils.ToScrap(keys, 0, 0, (int)Math.Round(m),  // metal→scrap via ScrapUtils
                                              refPerKey: 0 /*not used here*/);

                // Optionally skip “spell”/“effect” listings
                var details = (string)c["details"] ?? "";
                if (details.Contains("spell", StringComparison.OrdinalIgnoreCase) ||
                    details.Contains("effect", StringComparison.OrdinalIgnoreCase))
                    continue;

                prices.Add(scrap);
            }
            return prices;
        }
    }
}
