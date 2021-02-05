using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Common;
using Services.Abstractions;
using Utils.Abstractions;
using Utils.Converters;

namespace Services
{
    public class BinanceCandleService : ICandlesService
    {
        private readonly ICurrencyClientFactory m_currencyClientFactory;
        
        public BinanceCandleService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
        }

        public async Task<Memory<MyCandle>> GetOneMinuteCandles(string desiredSymbol, int candlesAmount, DateTime currentTime)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            string symbol = desiredSymbol;
            KlineInterval interval = KlineInterval.OneMinute;
            var response = await client.Spot.Market.GetKlinesAsync(symbol, interval, limit: candlesAmount);
            IBinanceKline[] binanceKlinesArr = response.Data as IBinanceKline[] ?? response.Data.ToArray();
            Memory<MyCandle> candlesDescription = BinanceKlineToMyCandleConverter.ConvertByCandleSize(binanceKlinesArr, 1, candlesAmount);
            return candlesDescription;
        }
    }
}