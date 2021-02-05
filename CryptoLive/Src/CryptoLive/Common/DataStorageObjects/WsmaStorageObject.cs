namespace Common.DataStorageObjects
{
    public class WsmaStorageObject
    {
        public decimal UpAverage { get; }
        public decimal DownAverage { get; }
        
        public WsmaStorageObject(decimal upAverage, decimal downAverage)
        {
            UpAverage = upAverage;
            DownAverage = downAverage;
        }
    }
}