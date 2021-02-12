using System;

namespace Utils.Abstractions
{
    public interface INotificationHandler
    {
        bool NotifyIfNeeded(Func<bool> condition, string currency);
    }
}