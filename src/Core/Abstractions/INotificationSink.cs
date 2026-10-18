namespace Core.Abstractions;

public interface INotificationSink
{
    void Notify(string message);
}