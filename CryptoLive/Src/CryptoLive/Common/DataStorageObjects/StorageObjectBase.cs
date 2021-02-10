using System;
using System.Globalization;

namespace Common.DataStorageObjects
{
    public abstract class StorageObjectBase : IComparable<StorageObjectBase>, IEquatable<StorageObjectBase>
    {
        public DateTime Time { get; set; }

        protected StorageObjectBase()
        {
            // For csvHelper
        }

        protected StorageObjectBase(DateTime time)
        {
            Time = time;
        }

        public int CompareTo(StorageObjectBase obj) =>
            CompareToByStr(Time, obj.Time);

        public int CompareToByStr(DateTime time1, DateTime time2)
        {
            return String.Compare(time1.ToString(CultureInfo.InvariantCulture), 
                time2.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        public bool Equals(StorageObjectBase other)
        {
            if (other is null)
            {
                return false;
            }

            return Time.Day.Equals(other.Time.Day) &&
                   Time.Hour.Equals(other.Time.Hour) &&
                   Time.Minute.Equals(other.Time.Minute) &&
                   Time.Second.Equals(other.Time.Second);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StorageObjectBase);
        }

        public override int GetHashCode()
        {
            return Time.GetHashCode();
        }
    }
}