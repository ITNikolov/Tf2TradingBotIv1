using System;

namespace Tf2TradingBotv1.Services
{
    public static class ScrapUtils
    {
        // Constants for metal-only conversions
        public const int ScrapPerScrap = 1;
        public const int ScrapPerRec = 3;     
        public const int ScrapPerRef = 9;    

        public static int ToScrap(
            int keys,
            int refined,
            int reclaimed,
            int scrap,
            decimal refPerKey)
        {
            int scrapPerKey = (int)Math.Round(refPerKey * ScrapPerRef);

            return keys * scrapPerKey
                 + refined * ScrapPerRef
                 + reclaimed * ScrapPerRec
                 + scrap * ScrapPerScrap;
        }

        public static (int Keys, int Refined, int Reclaimed, int Scrap) FromScrap(
            int totalScrap,
            decimal refPerKey)
        {
            int scrapPerKey = (int)Math.Round(refPerKey * ScrapPerRef);

            int keys = totalScrap / scrapPerKey;
            int rem1 = totalScrap % scrapPerKey;

            int refined = rem1 / ScrapPerRef;
            int rem2 = rem1 % ScrapPerRef;

            int reclaimed = rem2 / ScrapPerRec;

            int scrap = rem2 % ScrapPerRec;

            return (keys, refined, reclaimed, scrap);
        }
    }
}
