namespace Tf2TradingBotIv1.Data
{
    public class PriceRecord
    {
        public string Id { get; set; } 
        public decimal Buy { get; set; }
        public decimal Sell { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}
