using System;

namespace Common.DataStorageObjects
{
    public class WsmaStorageObject : StorageObjectBase , IEquatable<WsmaStorageObject>
    {
        public decimal UpAverage { get; set; }
        public decimal DownAverage { get; set; }

        public WsmaStorageObject()
        {
            // For csvHelper
        }
        
        public WsmaStorageObject(decimal upAverage, decimal downAverage, DateTime time)
            :base(time)
        {
            UpAverage = upAverage;
            DownAverage = downAverage;
        }

        public bool Equals(WsmaStorageObject other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) && UpAverage == other.UpAverage && DownAverage == other.DownAverage;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WsmaStorageObject);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), UpAverage, DownAverage);
        }
    }
}