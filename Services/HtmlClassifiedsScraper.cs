using System.Globalization;
using HtmlAgilityPack;
using static Tf2TradingBotv1.Services.ScrapUtils;


namespace Tf2TradingBotv1.Services
{
    public class HtmlClassifiedsScraper
    {
        private readonly HttpClient _http = new();


        /// Scrapes the public “Sell Orders” and “Buy Orders” for the given item.
        /// Returns two lists of scrap‐values: sells and buys.

        public async Task<(List<int> Sells, List<int> Buys)> FetchAsync(
            string itemName,
            decimal refPerKey)
        {
            // 1) Build the public stats URL
            var urlName = Uri.EscapeDataString(itemName);
            var url = $"https://backpack.tf/stats/Unique/{urlName}/Tradable/Craftable";

            // 2) Download & parse HTML
            var html = await _http.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 3) Helper to parse one column (sell_orders or buy_orders)
            List<int> ParseNode(string id)
            {
                var node = doc.DocumentNode.SelectSingleNode($"//div[@id='{id}']//ul");
                if (node == null) return new List<int>();

                var scraps = new List<int>();
                foreach (var li in node.SelectNodes(".//li") ?? Enumerable.Empty<HtmlNode>())
                {
                    var txt = li.InnerText.Trim();
                    if (string.IsNullOrEmpty(txt)) continue;

                    int total = 0;
                    if (txt.Contains("key"))
                    {
                        // e.g. "1 key + 8.66 ref"
                        var parts = txt.Split('+');
                        var keys = int.Parse(parts[0].Split(' ')[0], CultureInfo.InvariantCulture);
                        var metal = decimal.Parse(parts[1].Split(' ')[0], CultureInfo.InvariantCulture);

                        total = ScrapUtils.ToScrap(
                            keys: keys,
                            refined: (int)Math.Floor(metal),
                            reclaimed: 0,
                            scrap: (int)Math.Round((metal - Math.Floor(metal)) * ScrapPerRef),
                            refPerKey: refPerKey
                        );
                    }
                    else if (txt.Contains("ref"))
                    {
                        // e.g. "67.55 ref"
                        var metal = decimal.Parse(txt.Split(' ')[0], CultureInfo.InvariantCulture);
                        total = ScrapUtils.ToScrap(
                            keys: 0,
                            refined: (int)Math.Floor(metal),
                            reclaimed: 0,
                            scrap: (int)Math.Round((metal - Math.Floor(metal)) * ScrapPerRef),
                            refPerKey: refPerKey
                        );
                    }

                    if (total > 0)
                        scraps.Add(total);
                }
                return scraps;
            }

            var sells = ParseNode("sell_orders");
            var buys = ParseNode("buy_orders");
            return (sells, buys);
        }
    }
}
