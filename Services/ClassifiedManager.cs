using Tf2TradingBotv1.Data;
using Tf2TradingBotv1.Models;
using Newtonsoft.Json.Linq;

namespace Tf2TradingBotv1.Services
{
    public class ClassifiedManager
    {
        private readonly LiteDbService _db;
        private readonly string _apiKey;
        private readonly HttpClient _http = new HttpClient();

        public ClassifiedManager(LiteDbService db, string apiKey)
        {
            _db = db;
            _apiKey = apiKey;
        }

        public async Task SyncAllAsync()
        {
            var prices = _db.Prices.FindAll();
            foreach (var pr in prices)
            {
                // Skip if price is zero
                if (pr.Buy == 0 && pr.Sell == 0) continue;

                // Sync both intents: 0 = buy, 1 = sell
                await SyncListing(pr.Id, intent: 0, priceRef: pr.Buy);
                await SyncListing(pr.Id, intent: 1, priceRef: pr.Sell);
            }
        }

        private async Task SyncListing(string itemName, int intent, decimal priceRef)
        {
            // lookup existing record
            var rec = _db.Listings.FindOne(x => x.ItemName == itemName && x.Intent == intent);

            // convert priceRef to keys+metal (example: 50 ref = 1 key)
            int keys = (int)(priceRef / 50m);
            int metal = (int)(priceRef % 50m);

            // build form data
            var form = new MultipartFormDataContent {
                { new StringContent(_apiKey), "key" },
                { new StringContent(intent.ToString()), "intent" },
                { new StringContent(itemName), "item_name" },
                { new StringContent(keys.ToString()), "price_keys" },
                { new StringContent(metal.ToString()), "price_metal" },
                { new StringContent($"Type !{(intent==0?"sell":"buy")} {itemName}"), "details" }
            };

            if (rec != null && rec.Active)
            {
                form.Add(new StringContent(rec.ListingId.ToString()), "listing_id");
            }

            var resp = await _http.PostAsync("https://backpack.tf/api/ISetClassifieds/v1/", form);
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to sync {itemName} ({intent}) – {resp.StatusCode}");
                return;
            }

            var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
            int listingId = (int)json["response"]["listing"]["id"];

            // upsert our local record
            if (rec == null)
            {
                rec = new ListingRecord { ItemName = itemName, Intent = intent };
            }
            rec.ListingId = listingId;
            rec.PriceInScrap = (keys * 50 + metal);
            rec.LastUpdated = DateTime.UtcNow;
            rec.Active = true;
            _db.Listings.Upsert(rec);

            Console.WriteLine($"Synced listing {itemName} ({intent}) → id {listingId}");
        }
    }
}
