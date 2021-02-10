using System;

namespace Common.DataStorageObjects
{
    public class RsiStorageObject : StorageObjectBase, IEquatable<RsiStorageObject>
    {
        public decimal Rsi { get; set; }
        
        public RsiStorageObject()
        {
            // For csvHelper
        }
        
        public RsiStorageObject(decimal rsi, DateTime time)
            : base(time)
        {
            Rsi = rsi;
        }

        public bool Equals(RsiStorageObject other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) && Rsi == other.Rsi;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RsiStorageObject);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Rsi);
        }
    }
}