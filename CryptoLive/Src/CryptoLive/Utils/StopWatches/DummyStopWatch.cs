using Utils.Abstractions;

namespace Utils.StopWatches
{
    public class DummyStopWatch : IStopWatch
    {
        public void Restart()
        {
            
        }

        public void Stop()
        {
        }

        public int ElapsedSeconds => 0;
    }
}