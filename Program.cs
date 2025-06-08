using Newtonsoft.Json;
using Tf2TradingBotIv1.Services;
using Tf2TradingBotv1.Data;
using Tf2TradingBotv1.Services;  

namespace Tf2TradingBotv1
{
    public class BotSettings
    {
        public string backpackTfApiKey { get; set; }
        public int priceRefreshIntervalMinutes { get; set; }
        public Dictionary<string, int> buyLimits { get; set; }
        public List<string> trackedItems { get; set; }
    }

    class Program
    {
        // holds your deserialized JSON
        private static BotSettings _settings;

        static async Task Main(string[] args)
        {
            LoadConfig();
            using var db = new LiteDbService();

            // 1) Compute your cost floor (in scrap) from your stored PriceRecords or a fixed value
            //    e.g. if you paid 2 refined for the item: costScrap = 2 * ScrapUtils.ScrapPerRef
            int costFloor = 2 * ScrapUtils.ScrapPerRef;

            // 2) Instantiate the ClassifiedPriceService
            var priceService = new ClassifiedPriceService(_settings.backpackTfApiKey);

            // 3) Fetch undercut prices
            var livePrices = await priceService.GetPricesAsync(_settings.trackedItems, costFloor);

            // 4) Inspect or persist these prices
            foreach (var kv in livePrices)
            {
                var item = kv.Key;
                var (buyScrap, sellScrap) = kv.Value;
                var (k, r, rec, s) = ScrapUtils.FromScrap(buyScrap, refPerKey: /* your dynamic key→ref rate */0);
                Console.WriteLine($"{item} → Buy: {k}k {r}r {rec}rec {s}s; Sell: …");

                // e.g. upsert into LiteDB.Prices as before
            }

            Console.WriteLine("Done fetching undercut prices. Press ENTER to exit.");
            Console.ReadLine();
        }

        // ← This is the missing method! Add this below Main:
        private static void LoadConfig()
        {
            var configPath = Path.Combine("Config", "botsettings.json");
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Could not find {configPath}");

            var json = File.ReadAllText(configPath);
            _settings = JsonConvert.DeserializeObject<BotSettings>(json);
            Console.WriteLine($"Loaded {_settings.trackedItems.Count} items, refresh every {_settings.priceRefreshIntervalMinutes} min.");
        }
    }
}
