namespace Tf2TradingBotv1.Models
{
    public class ListingRecord
    {
        public int Id { get; set; }            
        public int ListingId { get; set; }     
        public string ItemName { get; set; }
        public int Intent { get; set; }        
        public int PriceInScrap { get; set; }  
        public DateTime LastUpdated { get; set; }
        public bool Active { get; set; }
    }
}
