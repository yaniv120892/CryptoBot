using System;

namespace Common.DataStorageObjects
{
    public class EmaAndSignalStorageObject : StorageObjectBase, IEquatable<EmaAndSignalStorageObject>
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

        public bool Equals(EmaAndSignalStorageObject other)
        {
            if (other is null)
            {
                return false;
            }
            return base.Equals(other) && 
                   FastEma == other.FastEma && 
                   SlowEma == other.SlowEma && 
                   Signal == other.Signal;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EmaAndSignalStorageObject);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), FastEma, SlowEma, Signal);
        }
    }
}