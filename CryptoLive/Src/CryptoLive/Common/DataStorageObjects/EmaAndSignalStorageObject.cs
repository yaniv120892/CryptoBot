using System;

namespace Common.DataStorageObjects
{
    public class EmaAndSignalStorageObject : StorageObjectBase
    {
        
        public decimal FastEma { get; set; }
        public decimal SlowEma { get; set; }
        public decimal Signal { get; set; }
        
        public EmaAndSignalStorageObject()
        {
            // For csvHelper
        }
        
        public EmaAndSignalStorageObject(decimal fastEma, decimal slowEma, decimal signal, DateTime time) 
            :base(time)
        {
            FastEma = fastEma;
            SlowEma = slowEma;
            Signal = signal;
        }
    }
}