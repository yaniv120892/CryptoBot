using System;
using Binance.Net.Interfaces;
using Common;
using Common.DataStorageObjects;

namespace Utils.Converters
{
    public class BinanceKlineToMyCandleConverter
    {
        public static Memory<MyCandle> ConvertByCandleSize(Span<IBinanceKline> binanceKlines, int candleSizeInMinutes, int candlesAmount)
        {
            Span<MyCandle> candles = ConvertBinanceKlineToCandle(binanceKlines);
            return ConvertByCandleSize(candles, candleSizeInMinutes, candlesAmount);
        }

        private static Span<MyCandle> ConvertBinanceKlineToCandle(Span<IBinanceKline> binanceKlines)
        {
            Span<MyCandle> ans = new MyCandle[binanceKlines.Length];
            for (int i = 0; i < ans.Length; i++)
            {
                ans[i] = new MyCandle(binanceKlines[i].Open,
                    binanceKlines[i].Close,
                    binanceKlines[i].OpenTime,
                    binanceKlines[i].CloseTime,
                    binanceKlines[i].Low,
                    binanceKlines[i].High);
            }

            return ans;
        }

        public static Memory<MyCandle> ConvertByCandleSize(Span<MyCandle> candles, int candleSizeInMinutes, int candlesAmount)
        {
            Memory<MyCandle> ans = new MyCandle[candlesAmount];
            for (int i = 0; i < ans.Length; i++)
            {
                int start = i * candleSizeInMinutes;
                int end = start + candleSizeInMinutes - 1;
                (decimal low, decimal high) = GetHighAndLow(candles, start, end);
                ans.Span[i] = new MyCandle(candles[start].Open, candles[end].Close, candles[start].OpenTime, candles[end].CloseTime, low, high);
            }

            return ans;
        }
        
        public static CandleStorageObject ConvertByCandleSize(Span<MyCandle> candles, int candleSizeInMinutes)
        {
            const int start = 0;
            int end = start + candleSizeInMinutes - 1;
            (decimal low, decimal high) = GetHighAndLow(candles, start, end);
            var candle = new MyCandle(candles[start].Open, candles[end].Close, candles[start].OpenTime, candles[end].CloseTime, low, high);
            return new CandleStorageObject(candle);
        }
        
        private static (decimal low, decimal high) GetHighAndLow(Span<MyCandle> candles, int start, int end)
        {
            decimal high = Decimal.MinValue;
            decimal low = Decimal.MaxValue;
            for (int i = start; i <= end; i++)
            {
                low = Math.Min(low, candles[i].Low);
                high = Math.Max(high, candles[i].High);
            }

            return (low,high);
        }
    }
}