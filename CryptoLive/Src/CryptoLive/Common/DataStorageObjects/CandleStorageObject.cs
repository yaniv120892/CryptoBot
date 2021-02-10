using System;

namespace Common.DataStorageObjects
{
    public class CandleStorageObject : StorageObjectBase, IEquatable<CandleStorageObject>
    {
        public MyCandle Candle { get; set; }

        public CandleStorageObject()
        {
            // For csv
        }
        
        public CandleStorageObject(MyCandle candle)
            : base(candle.CloseTime)
        {
            Candle = candle;
        }
        
        public bool Equals(CandleStorageObject other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) && Equals(Candle, other.Candle);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CandleStorageObject);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Candle);
        }
    }
}