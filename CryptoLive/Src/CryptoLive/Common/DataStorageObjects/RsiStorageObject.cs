namespace Common.DataStorageObjects
{
    public class RsiStorageObject
    {
        public RsiStorageObject(decimal rsi)
        {
            Rsi = rsi;
        }

        public decimal Rsi { get; }
    }
}