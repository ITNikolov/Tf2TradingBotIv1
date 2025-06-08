using System;
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
        static ClassifiedPriceService _priceSvc;
        static ClassifiedManager _classMgr;
        static Timer _timer;

        static async Task Main(string[] args)
        {
            LoadConfig();

            // 1) Initialize services
            _db = new LiteDbService();
            _priceSvc = new ClassifiedPriceService(_settings.backpackTfApiKey);
            _classMgr = new ClassifiedManager(_db, _settings.backpackTfApiKey);

            // 2) Kick off immediately, then every N minutes
            _timer = new Timer(async _ => await RefreshCycle(),
                               null,
                               dueTime: TimeSpan.Zero,
                               period: TimeSpan.FromMinutes(_settings.priceRefreshIntervalMinutes));

            Console.WriteLine("Bot is running. Press ENTER to stop.");
            Console.ReadLine();

            // 3) Clean up
            _timer.Dispose();
            _db.Dispose();
        }

        static async Task RefreshCycle()
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm}] Starting pricing cycle…");

                // a) Get undercut prices
                //    Pass your cost floor here (e.g. 50 ref = 50*9=450 scrap)
                var prices = await _priceSvc.GetPricesAsync(
                    _settings.trackedItems,
                    costScrap: 450
                );

                // b) Persist to LiteDB
                foreach (var kv in prices)
                {
                    _db.Prices.Upsert(new PriceRecord
                    {
                        Id = kv.Key,
                        Buy = kv.Value.BuyScrap,
                        Sell = kv.Value.SellScrap,
                        LastUpdated = DateTime.UtcNow
                    });
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm}] Prices saved to DB.");

                // c) Sync classifieds on backpack.tf
                await _classMgr.SyncAllAsync();
                Console.WriteLine($"[{DateTime.Now:HH:mm}] Classifieds sync complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error during refresh] {ex.Message}");
            }
        }

        static void LoadConfig()
        {
            var text = System.IO.File.ReadAllText("Config/botsettings.json");
            _settings = Newtonsoft.Json.JsonConvert.DeserializeObject<BotSettings>(text);
            Console.WriteLine($"Tracking {_settings.trackedItems.Count} items, interval = {_settings.priceRefreshIntervalMinutes} min.");
        }
    }
}
