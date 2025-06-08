using System.IO;
using LiteDB;
using Tf2TradingBotIv1.Data;
using Tf2TradingBotv1.Models;

namespace Tf2TradingBotv1.Data
{
    public class LiteDbService : IDisposable
    {
        private readonly LiteDatabase _db;
        public ILiteCollection<PriceRecord> Prices { get; }
        public ILiteCollection<ListingRecord> Listings { get; }

        public LiteDbService(string databasePath = "Data/botdata.db")
        {
            // **Ensure the folder exists**
            var folder = Path.GetDirectoryName(databasePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Now safely open or create the .db file
            _db = new LiteDatabase(databasePath);
            Prices = _db.GetCollection<PriceRecord>("prices");
            Listings = _db.GetCollection<ListingRecord>("listings");

            Prices.EnsureIndex(x => x.Id, true);
            Listings.EnsureIndex(x => x.ListingId);
        }

        public void Dispose() => _db?.Dispose();
    }
}
