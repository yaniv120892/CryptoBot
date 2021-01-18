using System;
using System.Globalization;
using Common;
using CsvHelper.Configuration;

namespace DemoCryptoLive
{
    public sealed class MyCandleMap : ClassMap<MyCandle>
    {
        public MyCandleMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.Open).Name("Open");
            Map(m => m.Close).Name("Close");
            Map(m => m.High).Name("High");
            Map(m => m.Low).Name("Low");
            Map(m => m.OpenTime).Name("OpenTime");
            Map(m => m.CloseTime).Name("CloseTime");
        }
    }
}