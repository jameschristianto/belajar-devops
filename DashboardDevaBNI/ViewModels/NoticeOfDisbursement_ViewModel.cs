using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.ViewModels
{
    public class NoticeOfDisbursement_ViewModel
    {
        public Int64? Number { get; set; }

        public int? Id { get; set; }

        public int? FileUploadNodid { get; set; }

        public string? NodNo { get; set; }

        public DateTime? NodDate { get; set; }

        public DateTime? ValueDate { get; set; }

        public string? Cur { get; set; }

        public string? Beneficiary { get; set; }

        public DateTime? LastSentDate { get; set; }

        public int? NodId { get; set; }

        public int? CreditorRefId { get; set; }

        public string? CreditorRef { get; set; }

        public string? Amount { get; set; }

        public string? AmountIDR { get; set; }

        public DateTime? DisbursementDate { get; set; }

        public string? Apdplno { get; set; }

        public DateTime? Apdpldate { get; set; }

        public string? RealisasiNo { get; set; }

        public DateTime? RealisasiDate { get; set; }

        public string? ContractNo { get; set; }

        public DateTime? ContractDate { get; set; }

        public DateTime? CreatedTime { get; set; }

        public int? CreatedById { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public int? UpdatedById { get; set; }

        public int? IsActive { get; set; }

        public int? IsDeleted { get; set; }

        public string? Status { get; set; }
        public string? RandomString { get; set; }
        public IFormFile File { get; set; }

        public virtual TblFileUploadNod? FileUploadNod { get; set; }

        public virtual ICollection<TblNoticeOfDisbursementDetail> TblNoticeOfDisbursementDetails { get; set; } = new List<TblNoticeOfDisbursementDetail>();
    }

    public class NoticeOfDisbursementFile_ViewModel
    {
        public Int64? Number { get; set; }
        public int? Id { get; set; }
        public string? FileName { get; set; }
        public int? FileSize { get; set; }
        public string? FilePath { get; set; }
        public string? FileExt { get; set; }
        public int? IsActive { get; set; }
        public int? IsDeleted { get; set; }
        public string? RandomString { get; set; }
    }

    public class NoticeOfDisbursementFileUpload_ViewModel
    {
        public IFormFile File { get; set; }
        public string? uniq { get; set; }
    }

    public class NoticeOfDisbursementToAPI_ViewModels
    {
        public string NodNo { get; set; }
        public DateTime? NodDate { get; set; }
        public DateTime? ValueDate { get; set; }
        public string Cur { get; set; }
        public List<TblNoticeOfDisbursementDetail> NodDetail { get; set; }
    }
    public class NoticeOfDisbursementJOB_ViewModel
    {
        public string Id { get; set; }
        public string Status { get; set; }
    }
}
