using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tf2TradingBotIv1.Models
{
    public class BotSettings
    {
        public string backpackTfApiKey { get; set; }
        public int priceRefreshIntervalMinutes { get; set; }
        public Dictionary<string, int> buyLimits { get; set; }
        public List<string> trackedItems { get; set; }
    }
}
