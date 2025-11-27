using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.ViewModels
{
    public class NoDView_ViewModel
    {
        public int? Id { get; set; }
        public string? NodNo { get; set; }
        public string? Cur { get; set; }
        public string? Beneficiary { get; set; }
        public DateTime? NodDate { get; set; }
        public DateTime? ValueDate { get; set; }
        public int? IsActive { get; set; }
        public TblNoticeOfDisbursement NoticeOfDisbursement { get; set; }
        public List<TblNoticeOfDisbursementDetail> tblNoticeOfDisbursementDetailsList { get; set; }
    }
}
