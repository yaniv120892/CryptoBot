using System;
using System.Globalization;

namespace Common.DataStorageObjects
{
    public class MeanAverageStorageObject : StorageObjectBase, IEquatable<MeanAverageStorageObject>
    {
        public decimal MeanAverage { get; set; }
        
        public MeanAverageStorageObject()
        {
            // For csvHelper
        }
        
        public MeanAverageStorageObject(decimal meanAverage, DateTime time)
            : base(time)
        {
            MeanAverage = meanAverage;
        }

        public bool Equals(MeanAverageStorageObject other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) && MeanAverage == other.MeanAverage;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MeanAverageStorageObject);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), MeanAverage);
        }

        public override string ToString()
        {
            return MeanAverage.ToString(CultureInfo.InvariantCulture);
        }
    }
}