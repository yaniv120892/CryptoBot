using System;

namespace Common.DataStorageObjects
{
    public abstract class StorageObjectBase
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
    }
}