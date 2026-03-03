namespace SyndiceoWeb.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalEntries { get; set; }
        public int ActiveUsers { get; set; }
        public int NewReports { get; set; }
        public int PendingConfirmations { get; set; }
    }
}
