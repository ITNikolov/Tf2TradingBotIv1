using System;

namespace Tf2TradingBotv1.Services
{
    public static class ScrapUtils
    {
        public const int ScrapPerScrap = 1;
        public const int ScrapPerRec = 3;
        public const int ScrapPerRef = 9;

        /// Convert any mix of keys/ref/rec/scrap into total scrap.
        public static int ToScrap(int keys, int refined, int reclaimed, int scrap, decimal refPerKey)
        {
            // scrap-per-key = refPerKey (in ref) × ScrapPerRef
            int scrapPerKey = (int)Math.Round(refPerKey * ScrapPerRef);
            return keys * scrapPerKey
                 + refined * ScrapPerRef
                 + reclaimed * ScrapPerRec
                 + scrap * ScrapPerScrap;
        }

        /// Break total scrap back into keys/ref/rec/scrap.
        public static (int Keys, int Refined, int Reclaimed, int Scrap) FromScrap(int totalScrap, decimal refPerKey)
        {
            int scrapPerKey = (int)Math.Round(refPerKey * ScrapPerRef);
            int keys = totalScrap / scrapPerKey;
            int rem1 = totalScrap % scrapPerKey;
            int @ref = rem1 / ScrapPerRef;
            int rem2 = rem1 % ScrapPerRef;
            int rec = rem2 / ScrapPerRec;
            int sc = rem2 % ScrapPerRec;
            return (keys, @ref, rec, sc);
        }
        public static List<int> TrimOutliers(List<int> data, double pct)
        {
            data.Sort();
            int drop = (int)(data.Count * pct);
            return data.Skip(drop)
                       .Take(data.Count - 2 * drop)
                       .ToList();
        }

    }
}
