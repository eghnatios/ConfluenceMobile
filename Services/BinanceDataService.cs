using Binance.Net.Clients;
using Binance.Net.Enums;
using ConfluenceAIBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfluenceAIBot.Services
{
    public sealed class BinanceDataService : IDisposable
    {
        private readonly BinanceRestClient _client;

        public BinanceDataService()
        {
            _client = new BinanceRestClient();
        }

        public async Task<List<CandleData>> GetKlinesAsync(
            string symbol, 
            KlineInterval interval, 
            int limit = 500, 
            CancellationToken ct = default)
        {
            // UPGRADE: Switched from SpotApi to UsdFuturesApi for accurate SMC/Liquidity sweep data
            var result = await _client.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                symbol, 
                interval, 
                limit: limit, 
                ct: ct);

            if (!result.Success)
                throw new Exception($"Binance API error: {result.Error?.Message}");

            return result.Data.Select(k => new CandleData
            {
                OpenTime = k.OpenTime,
                Open     = k.OpenPrice,
                High     = k.HighPrice,
                Low      = k.LowPrice,
                Close    = k.ClosePrice,
                Volume   = k.Volume
            }).ToList();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}