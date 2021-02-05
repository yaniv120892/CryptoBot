namespace Common.DataStorageObjects
{
    public class MacdStorageObject
    {
        public decimal MacdHistogram { get; }
        
        public MacdStorageObject(decimal macdHistogram)
        {
            MacdHistogram = macdHistogram;
        }
    }
}