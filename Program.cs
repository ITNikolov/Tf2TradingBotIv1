using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tf2TradingBotIv1.Data;
using Tf2TradingBotIv1.Models;
using Tf2TradingBotv1.Data;
using Tf2TradingBotv1.Models;
using Tf2TradingBotv1.Services;

namespace Tf2TradingBotv1
{
    class Program
    {
        static BotSettings _settings;
        static LiteDbService _db;
        static ClassifiedManager _classMgr;
        static Timer _timer;

        static async Task Main(string[] args)
        {
            // 1) Load JSON config
            var json = System.IO.File.ReadAllText("Config/botsettings.json");
            _settings = Newtonsoft.Json.JsonConvert.DeserializeObject<BotSettings>(json);

            // 2) Init DB & ClassifiedManager
            _db = new LiteDbService();
            _classMgr = new ClassifiedManager(_db, _settings.backpackTfApiKey);

            // 3) Kick off the 15-min cycle
            _timer = new Timer(async _ => await RefreshCycle(),
                               null,
                               TimeSpan.Zero,
                               TimeSpan.FromMinutes(_settings.priceRefreshIntervalMinutes));

            Console.WriteLine("Bot running. Press ENTER to stop.");
            Console.ReadLine();
            _timer.Dispose();
            _db.Dispose();
        }

        static async Task RefreshCycle()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm}] Starting pricing cycle…");

                // A) Fetch live key→ref guide rate (to value keys)
                var guide = new GuidePriceService(_settings.backpackTfApiKey);
                decimal refPerKey = await guide.FetchKeyRefRateAsync();

                // B) Scraper & undercut logic
                var scraper = new HtmlClassifiedsScraper();
                var undercutPrices = new Dictionary<string, (int Buy, int Sell)>();

                foreach (var item in _settings.trackedItems)
                {
                    var (sells, buys) = await scraper.FetchAsync(item, refPerKey);
                    if (sells.Count == 0 || buys.Count == 0)
                    {
                        Console.WriteLine($"   ! no data for {item}");
                        continue;
                    }

                    // trim outliers
                    var goodSells = ScrapUtils.TrimOutliers(sells, 0.10);
                    var goodBuys = ScrapUtils.TrimOutliers(buys, 0.10);

                    int minSell = goodSells.Min();
                    int maxBuy = goodBuys.Max();
                    int costFloor = 9;  // e.g. floor at 1 ref = 9 scrap

                    int sellScrap = Math.Max(minSell - 1, costFloor);
                    int buyScrap = maxBuy + 1;

                    undercutPrices[item] = (buyScrap, sellScrap);
                    Console.WriteLine($"   • {item}: Buy@{buyScrap}s Sell@{sellScrap}s");
                }

                // C) Persist and sync classifieds
                foreach (var kv in undercutPrices)
                {
                    _db.Prices.Upsert(new PriceRecord
                    {
                        Id = kv.Key,
                        Buy = kv.Value.Buy,
                        Sell = kv.Value.Sell,
                        LastUpdated = DateTime.UtcNow
                    });
                }
                await _classMgr.SyncAllAsync();

                Console.WriteLine($"[{DateTime.Now:HH:mm}] Cycle complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }
        }
    }
}
