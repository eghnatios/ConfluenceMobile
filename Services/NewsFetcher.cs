using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConfluenceAIBot.Services
{
    public static class NewsFetcher
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public static async Task<List<string>> GetCryptoNewsAsync(string symbol)
        {
            try
            {
                // We use CoinTelegraph's public RSS feed — it updates constantly and is free.
                string rssUrl = "https://cointelegraph.com/rss";
                
                string xml = await _http.GetStringAsync(rssUrl);
                var doc = XDocument.Parse(xml);
                
                var allHeadlines = doc.Descendants("item")
                                      .Select(x => x.Element("title")?.Value)
                                      .Where(x => !string.IsNullOrWhiteSpace(x))
                                      .ToList();

                // Extract the base coin (e.g., 'BTC' from 'BTCUSDT') to look for specific news
                string baseCoin = symbol.Replace("USDT", "").Replace("BUSD", "").Replace("USDC", "");

                // Try to find news directly mentioning our coin
                var coinNews = allHeadlines
                               .Where(h => h.IndexOf(baseCoin, StringComparison.OrdinalIgnoreCase) >= 0)
                               .Take(3)
                               .ToList();

                // If no specific news is found, just grab the top 3 overall market headlines to judge general market sentiment
                if (coinNews.Count == 0)
                {
                    coinNews = allHeadlines.Take(3).ToList();
                }

                return coinNews!;
            }
            catch
            {
                // Fail silently and return an empty list so it doesn't crash the bot if the news site is down
                return new List<string>();
            }
        }
    }
}