using System;

namespace Storage.Repository
{
    public class RepositoryKeyConverter
    {
        public static DateTime AlignTimeToRepositoryKeyFormat(DateTime currentTime) =>
            currentTime.Second != 59 ?
                currentTime.Subtract(TimeSpan.FromSeconds(currentTime.Second + 1)) :
                currentTime;
    }
}