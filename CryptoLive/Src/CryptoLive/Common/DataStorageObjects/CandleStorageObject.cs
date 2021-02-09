namespace Common.DataStorageObjects
{
    public class CandleStorageObject : StorageObjectBase
    {
        public MyCandle Candle { get; set; }

        public CandleStorageObject()
        {
            // For csv
        }
        
        public CandleStorageObject(MyCandle candle)
            : base(candle.CloseTime)
        {
            Candle = candle;
        }
    }
}