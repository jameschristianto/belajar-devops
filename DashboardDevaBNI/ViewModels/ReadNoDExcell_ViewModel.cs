using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.ViewModels
{
    public class ReadNoDExcell_ViewModel
    {
        public int? FileUploadNodid { get; set; }

        public string? NodNo { get; set; }

        public DateTime? NodDate { get; set; }

        public DateTime? ValueDate { get; set; }

        public string? Cur { get; set; }

        public string? Beneficiary { get; set; }

        public DateTime? LastSentDate { get; set; }

        public DateTime? CreatedTime { get; set; }

        public int? CreatedById { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public int? UpdatedById { get; set; }

        public int? IsActive { get; set; }

        public int? IsDeleted { get; set; }

        public string? Status { get; set; }

        public virtual TblFileUploadNod? FileUploadNod { get; set; }

        public virtual List<TblNoticeOfDisbursementDetail> NodDetails { get; set; } = new List<TblNoticeOfDisbursementDetail>();
    }
}
