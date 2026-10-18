namespace Core.Reports;

public sealed record ProductActivity(string Sku, string Name, int MovementsCount, int TotalDelta);