namespace Cli;

// Власна синхронна реалізація: Progress<T> у консолі викликає обробник у пулі потоків
// і рядки можуть з'явитись уже після підсумку.
public sealed class ConsoleProgress : IProgress<int>
{
    private int _next = 10;

    public void Report(int percent)
    {
        if (percent < _next) return;
        Console.WriteLine($"  прогрес: {percent}%");
        _next = (percent / 10 + 1) * 10;
    }
}