using System;

namespace Common.DataStorageObjects
{
    public class RsiStorageObject : StorageObjectBase
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
    }
}