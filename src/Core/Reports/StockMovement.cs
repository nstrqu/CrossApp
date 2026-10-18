namespace Core.Reports;

// Проста демонстраційна модель "переміщення по товару" — не зберігається в сховищі,
// лише для показу GroupJoin.
public sealed record StockMovement(string ProductId, int Delta, string Kind);