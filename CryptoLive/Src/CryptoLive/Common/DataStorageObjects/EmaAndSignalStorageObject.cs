namespace Common.DataStorageObjects
{
    public class EmaAndSignalStorageObject
    {
        public EmaAndSignalStorageObject(decimal fastEma, decimal slowEma, decimal signal)
        {
            FastEma = fastEma;
            SlowEma = slowEma;
            Signal = signal;
        }

        public decimal FastEma { get; }
        public decimal SlowEma { get; }
        public decimal Signal { get; }
    }
}