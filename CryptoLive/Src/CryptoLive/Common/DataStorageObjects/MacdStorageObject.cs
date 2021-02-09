using System;

namespace Common.DataStorageObjects
{
    public class MacdStorageObject : StorageObjectBase
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
    }
}