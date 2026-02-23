namespace Syndiceo.Utilities
{
    public static class SessionData
    {
        // Пазим последните плащания направени от SummaryPriceWindow
        public static List<PaymentRecord> LastPayments { get; } = new();
    }

    public class PaymentRecord
    {
        public int DebtId { get; set; }
        public int ApartmentId { get; set; }
        public decimal Amount { get; set; }
    }
}
