using Voce.Core.Models;

namespace Voce.Core.Services;

public sealed record DashboardSummary(int Count, decimal TotalInvoiced, decimal TotalPaid, decimal Outstanding, int OverdueCount, decimal Overdue)
{
    public static DashboardSummary From(IEnumerable<Invoice> invoices, DateOnly today)
    {
        var list = invoices.ToList();
        var unpaid = list.Where(i => !i.IsPaid).ToList();
        var overdue = unpaid.Where(i => i.IsOverdue(today)).ToList();
        return new DashboardSummary(
            list.Count,
            list.Sum(i => i.Total),
            list.Where(i => i.IsPaid).Sum(i => i.Total),
            unpaid.Sum(i => i.Total),
            overdue.Count,
            overdue.Sum(i => i.Total));
    }
}
