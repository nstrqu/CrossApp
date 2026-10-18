using Core.Abstractions;

namespace Cli.Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(string message) => Console.WriteLine($"[sink] {message}");
}