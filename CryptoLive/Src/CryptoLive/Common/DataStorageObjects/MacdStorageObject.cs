using System;

namespace Common.DataStorageObjects
{
    public class MacdStorageObject : StorageObjectBase, IEquatable<MacdStorageObject>
    {
        public decimal MacdHistogram { get; set; }

        public MacdStorageObject()
        {
            // For csvHelper
        }
        
        public MacdStorageObject(decimal macdHistogram, DateTime time) : base(time)
        {
            MacdHistogram = macdHistogram;
        }

        public bool Equals(MacdStorageObject other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) && MacdHistogram == other.MacdHistogram;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MacdStorageObject);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), MacdHistogram);
        }
    }
}