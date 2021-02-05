namespace Common.DataStorageObjects
{
    public class CandleStorageObject
    {
        public MyCandle Candle { get; }
        
        public CandleStorageObject(MyCandle candle)
        {
            Candle = candle;
        }
    }
}