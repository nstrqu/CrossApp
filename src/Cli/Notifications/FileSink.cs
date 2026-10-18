using Core.Abstractions;

namespace Cli.Notifications;

public sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = path;

    public void Notify(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.AppendAllText(_path, message + Environment.NewLine);
    }
}