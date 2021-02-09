using System;

namespace Common.DataStorageObjects
{
    public class WsmaStorageObject : StorageObjectBase
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
    }
}