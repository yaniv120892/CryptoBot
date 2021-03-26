using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class PriceAndRsiPollingResponse : PollingResponseBase, IEquatable<PriceAndRsiPollingResponse>
    {
        public PriceAndRsi NewPriceAndRsi { get; }
        public PriceAndRsi OldPriceAndRsi { get; }

        public PriceAndRsiPollingResponse(DateTime time, 
            PriceAndRsi oldPriceAndRsi,
            PriceAndRsi newPriceAndRsi,
            bool isCancelled=false,
            Exception gotException=null)
            : base(time, isCancelled, gotException)
        {
            NewPriceAndRsi = newPriceAndRsi;
            OldPriceAndRsi = oldPriceAndRsi;
        }
        
        public override string ToString()
        {
            return $"{base.ToString()}, New: {NewPriceAndRsi}, Old: {OldPriceAndRsi}";
        }

        public bool Equals(PriceAndRsiPollingResponse other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) &&
                   Equals(NewPriceAndRsi, other.NewPriceAndRsi) && Equals(OldPriceAndRsi, other.OldPriceAndRsi);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PriceAndRsiPollingResponse);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), NewPriceAndRsi, OldPriceAndRsi);
        }
    }
}