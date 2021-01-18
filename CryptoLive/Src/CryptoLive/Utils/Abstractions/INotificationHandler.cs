namespace Utils.Abstractions
{
    public interface INotificationHandler
    {
        bool NotifyIfNeeded(decimal indicator, string symbol);
    }
}