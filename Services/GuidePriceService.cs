using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Tf2TradingBotv1.Services
{
    public class GuidePriceService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey;

        public GuidePriceService(string backpackTfApiKey)
        {
            _apiKey = backpackTfApiKey;
        }

        /// Pulls the “sell” guide price for a TF2 key from the IGetPrices/v4 API
        /// and returns it in refined‐metal units (e.g. 67.55).
        public async Task<decimal> FetchKeyRefRateAsync()
        {
            var url = $"https://backpack.tf/api/IGetPrices/v4?key={_apiKey}&currency=metal";
            var json = await _http.GetStringAsync(url);
            var prices = JObject.Parse(json)["response"]["prices"];

            // Make sure this matches the exact name in your trackedItems
            const string keyName = "Mann Co. Supply Crate Key";
            var keyToken = prices[keyName];
            if (keyToken == null)
                throw new InvalidOperationException($"Guide price for '{keyName}' not found.");

            // “sell.value” is the price in refined metal
            return keyToken["sell"]["value"].Value<decimal>();
        }
    }
}
