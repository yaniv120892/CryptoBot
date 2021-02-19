namespace Utils.Abstractions
{
    public interface IStopWatch
    {
        void Restart();
        void Stop();
        int ElapsedSeconds { get; }
    }
}