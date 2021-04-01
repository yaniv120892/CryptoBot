using System;

namespace Common
{
    public class BuyAndSellTradeInfo : IEquatable<BuyAndSellTradeInfo>
    {
        public BuyAndSellTradeInfo(decimal buyPrice,
            decimal sellPrice,
            decimal stopLossLimitPrice,
            decimal quantity,
            DateTime placeBuyTradeTime,
            DateTime placeSellOrderTime) 
            : this(buyPrice, quantity, placeBuyTradeTime, placeSellOrderTime)
        {
            SellPrice = sellPrice;
            StopLossLimitPrice = stopLossLimitPrice;
            PlaceBuyTradeTime = placeBuyTradeTime;
            PlaceSellOrderTime = placeSellOrderTime;
        }

        public BuyAndSellTradeInfo(decimal buyPrice, 
            decimal quantity, 
            DateTime placeBuyTradeTime, 
            DateTime endTradeTime)
        {
            BuyPrice = buyPrice;
            Quantity = quantity;
            PlaceBuyTradeTime = placeBuyTradeTime;
            EndTradeTime = endTradeTime;
        }

        public decimal BuyPrice { get; }
        public decimal SellPrice { get; }
        public decimal StopLossLimitPrice { get; }
        public DateTime PlaceBuyTradeTime { get; }
        public DateTime PlaceSellOrderTime { get; }
        public DateTime EndTradeTime { get; }
        public decimal Quantity { get; }
        public decimal QuoteOrderQuantityOnWin => decimal.Parse((Quantity * SellPrice).ToString("F"));
        public decimal QuoteOrderQuantityOnLoss => decimal.Parse((Quantity * StopLossLimitPrice).ToString("F"));
        public decimal QuoteOrderQuantityPaid => decimal.Parse((Quantity * BuyPrice).ToString("F"));
        public bool IsDoneBuyAndSell => PlaceSellOrderTime != default;
        
        public bool Equals(BuyAndSellTradeInfo other)
        {
            if (other is null)
            {
                return false;
            }
            return BuyPrice == other.BuyPrice 
                   && SellPrice == other.SellPrice
                   && StopLossLimitPrice == other.StopLossLimitPrice 
                   && Quantity == other.Quantity;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BuyAndSellTradeInfo);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BuyPrice, SellPrice, StopLossLimitPrice, Quantity);
        }
    }
}