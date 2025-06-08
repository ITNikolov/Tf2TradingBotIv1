namespace Tf2TradingBotIv1.Services
{
    public static class ScrapUtils
    {
        public const int ScrapPerRef = 9;
        public const int ScrapPerRec = 3;
        public const int ScrapPerScrap = 1;

        /// <summary>
        /// Convert amounts into total scrap units, using a dynamic key→ref rate.
        /// </summary>
        public static int ToScrap(
            int keys,
            int refined,
            int reclaimed,
            int scrap,
            decimal refPerKey)
        {
            // scrap per key = refPerKey * ScrapPerRef
            var scrapPerKey = (int)Math.Round(refPerKey * ScrapPerRef);
            return keys * scrapPerKey
                 + refined * ScrapPerRef
                 + reclaimed * ScrapPerRec
                 + scrap * ScrapPerScrap;
        }

        /// <summary>
        /// Break total scrap back into (keys, refined, reclaimed, scrap),
        /// given the same refPerKey rate.
        /// </summary>
        public static (int Keys, int Ref, int Rec, int Sc) FromScrap(
            int totalScrap,
            decimal refPerKey)
        {
            var scrapPerKey = (int)Math.Round(refPerKey * ScrapPerRef);
            int keys = totalScrap / scrapPerKey;
            int rem1 = totalScrap % scrapPerKey;
            int @ref = rem1 / ScrapPerRef;
            int rem2 = rem1 % ScrapPerRef;
            int rec = rem2 / ScrapPerRec;
            int sc = rem2 % ScrapPerRec;
            return (keys, @ref, rec, sc);
        }
    }
}
