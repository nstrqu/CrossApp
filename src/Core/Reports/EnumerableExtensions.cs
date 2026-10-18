namespace Core.Reports;

public static class EnumerableExtensions
{
    public static IReadOnlyList<T> TopBy<T>(this IEnumerable<T> source, Func<T, int> keySelector, int n) =>
        source.OrderByDescending(keySelector).Take(n).ToList();
}