namespace DashboardDevaBNI.ViewModels
{
    public class NoDHeaderDetail_ViewModel
    {
        public int? Id { get; set; }
        public string? NodNo { get; set; }
        public string? Cur { get; set; }
        public string? Beneficiary { get; set; }
        public DateTime? NodDate { get; set; }
        public DateTime? ValueDate { get; set; }
        public int? IsActive { get; set; }
        public List<NoticeOfDisbursementDetail_ViewModels>? NodDetails { get; set; }
    }
}
