using System;

namespace Common.DataStorageObjects
{
    public abstract class StorageObjectBase : IComparable<StorageObjectBase>
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

        public int CompareTo(StorageObjectBase obj) => Time.CompareTo(obj.Time);
    }
}