using System.Diagnostics;
using Utils.Abstractions;

namespace Utils.StopWatches
{
    public class StopWatchWrapper : IStopWatch
    {
        private readonly Stopwatch m_stopwatch;

        public StopWatchWrapper()
        {
            m_stopwatch = new Stopwatch();
        }

        public void Restart()
        {
            m_stopwatch.Restart();
        }

        public void Stop()
        {
            m_stopwatch.Stop();
        }

        public int ElapsedSeconds => m_stopwatch.Elapsed.Seconds;
    }
}